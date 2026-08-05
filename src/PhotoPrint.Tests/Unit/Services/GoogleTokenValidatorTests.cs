using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

public class GoogleTokenValidatorTests
{
    private const string TestClientId = "test-client-id.apps.googleusercontent.com";

    private static IGoogleTokenValidator CreateValidator(
        Func<HttpRequestMessage, HttpResponseMessage> httpHandler) =>
        CreateValidator(new StubHttpHandler(httpHandler));

    private static IGoogleTokenValidator CreateValidator(
        HttpMessageHandler handler, TimeSpan? deadline = null)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://oauth2.googleapis.com/"),
            Timeout = GoogleTokenValidator.HttpBackstop,
        };

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("Google")).Returns(httpClient);

        var settings = Options.Create(new GoogleAuthSettings { ClientId = TestClientId });
        var logger = Mock.Of<ILogger<GoogleTokenValidator>>();

        return new GoogleTokenValidator(factory.Object, settings, logger, deadline);
    }

    private static string ValidTokenInfoJson(string? aud = null) => $$"""
        {
            "sub": "110169484474386276334",
            "aud": "{{aud ?? TestClientId}}",
            "email": "john.doe@gmail.com",
            "given_name": "John",
            "family_name": "Doe",
            "picture": "https://lh3.example.com/photo.jpg"
        }
        """;

    [Fact]
    public async Task ValidateAsync_ValidToken_ReturnsPayloadWithCorrectFields()
    {
        var validator = CreateValidator(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ValidTokenInfoJson(), Encoding.UTF8, "application/json"),
        });

        var result = await validator.ValidateAsync("valid-token");

        result.Sub.Should().Be("110169484474386276334");
        result.Email.Should().Be("john.doe@gmail.com");
        result.GivenName.Should().Be("John");
        result.FamilyName.Should().Be("Doe");
        result.Picture.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateAsync_GoogleReturns400_ThrowsUnauthorizedException()
    {
        var validator = CreateValidator(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));

        await validator.Invoking(v => v.ValidateAsync("bad-token"))
            .Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ValidateAsync_WrongAud_ThrowsUnauthorizedException()
    {
        var validator = CreateValidator(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                ValidTokenInfoJson("other-app.apps.googleusercontent.com"),
                Encoding.UTF8, "application/json"),
        });

        await validator.Invoking(v => v.ValidateAsync("foreign-token"))
            .Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ValidateAsync_HttpRequestException_ThrowsBadGatewayException()
    {
        var validator = CreateValidator(_ => throw new HttpRequestException("Connection refused"));

        await validator.Invoking(v => v.ValidateAsync("any-token"))
            .Should().ThrowAsync<BadGatewayException>();
    }

    [Fact]
    public async Task ValidateAsync_TaskCanceledException_ThrowsBadGatewayException()
    {
        var validator = CreateValidator(_ => throw new TaskCanceledException("Timeout"));

        await validator.Invoking(v => v.ValidateAsync("any-token"))
            .Should().ThrowAsync<BadGatewayException>();
    }

    [Fact]
    public async Task ValidateAsync_MissingSubField_ThrowsUnauthorizedException()
    {
        const string json = """{ "aud": "test-client-id.apps.googleusercontent.com", "email": "a@b.com" }""";
        var validator = CreateValidator(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });

        await validator.Invoking(v => v.ValidateAsync("any-token"))
            .Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ValidateAsync_CallerCancelled_PropagatesCancellationInsteadOfBadGateway()
    {
        using var caller = new CancellationTokenSource();
        var validator = CreateValidator(new HangingHttpHandler());
        await caller.CancelAsync();

        await validator.Invoking(v => v.ValidateAsync("any-token", caller.Token))
            .Should().ThrowAsync<OperationCanceledException>(
                "a client abort must reach the middleware's own guard, not become a 502 and a Sentry issue");
    }

    [Fact]
    public async Task ValidateAsync_GoogleExceedsOurDeadline_ThrowsBadGatewayException()
    {
        var validator = CreateValidator(
            new HangingHttpHandler(), deadline: TimeSpan.FromMilliseconds(50));

        await validator.Invoking(v => v.ValidateAsync("any-token", CancellationToken.None))
            .Should().ThrowAsync<BadGatewayException>(
                "a dependency that never answers is an outage, not a cancellation");
    }

    [Fact]
    public async Task ValidateAsync_DeadlineElapsedThenTheCallerAborted_StillThrowsBadGatewayException()
    {
        using var caller = new CancellationTokenSource();
        var validator = CreateValidator(
            new AbortsTheCallerOnCancellationHttpHandler(caller),
            deadline: TimeSpan.FromMilliseconds(50));

        await validator.Invoking(v => v.ValidateAsync("any-token", caller.Token))
            .Should().ThrowAsync<BadGatewayException>(
                "Google had already failed, so a user who then gives up must not hide the outage "
                    + "from Sentry and the 5xx numerator");
    }

    private sealed class StubHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
    }

    private sealed class HangingHttpHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new UnreachableException();
        }
    }

    // Cancels the caller only once the deadline has already tripped, so both are set by the time
    // the catch filter runs — the ordering the real world only reaches as a race.
    private sealed class AbortsTheCallerOnCancellationHttpHandler : HttpMessageHandler
    {
        private readonly CancellationTokenSource _caller;

        public AbortsTheCallerOnCancellationHttpHandler(CancellationTokenSource caller) =>
            _caller = caller;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await _caller.CancelAsync();
                throw;
            }

            throw new UnreachableException();
        }
    }
}
