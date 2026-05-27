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
        Func<HttpRequestMessage, HttpResponseMessage> httpHandler)
    {
        var handler = new StubHttpHandler(httpHandler);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://oauth2.googleapis.com/"),
            Timeout = TimeSpan.FromSeconds(5),
        };

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("Google")).Returns(httpClient);

        var settings = Options.Create(new GoogleAuthSettings { ClientId = TestClientId });
        var logger = Mock.Of<ILogger<GoogleTokenValidator>>();

        return new GoogleTokenValidator(factory.Object, settings, logger);
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

    private sealed class StubHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
    }
}
