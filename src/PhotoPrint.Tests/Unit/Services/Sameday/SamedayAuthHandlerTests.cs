using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

/// <summary>
/// Behavioural tests for the auth handler (ADR-014):
///   - bearer is attached to every non-authenticate call,
///   - /api/authenticate is passed through without a token,
///   - 401 triggers token invalidate + re-fetch + retry exactly once,
///   - a second 401 raises <see cref="SamedayAuthException"/>,
///   - the retry uses the FRESH token, not the original.
/// </summary>
public class SamedayAuthHandlerTests
{
    private static HttpClient Build(
        Mock<ISamedayTokenProvider> tokenProvider,
        ScriptedHttpMessageHandler script)
    {
        var sut = new SamedayAuthHandler(
            tokenProvider.Object,
            new LoggerFactory().CreateLogger<SamedayAuthHandler>())
        {
            InnerHandler = script,
        };
        return new HttpClient(sut)
        {
            BaseAddress = new Uri("https://sameday-test/"),
        };
    }

    [Fact]
    public async Task Operational_call_receives_a_bearer_token()
    {
        var tokenProvider = new Mock<ISamedayTokenProvider>();
        tokenProvider
            .Setup(t => t.GetTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SamedayToken("ABC", DateTimeOffset.UtcNow.AddHours(1)));

        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(HttpStatusCode.OK, "{}"));

        var http = Build(tokenProvider, script);
        var response = await http.GetAsync("/api/awb/RO123/tracking");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        script.Recorded.Should().HaveCount(1);
        script.Recorded[0].Authorization.Should().NotBeNull();
        script.Recorded[0].Authorization!.Scheme.Should().Be("Bearer");
        script.Recorded[0].Authorization!.Parameter.Should().Be("ABC");
    }

    [Fact]
    public async Task Authenticate_path_is_passed_through_without_a_bearer()
    {
        var tokenProvider = new Mock<ISamedayTokenProvider>();
        // Critical: the auth handler must NEVER call GetTokenAsync for /api/authenticate
        // — that path is what fetches the token in the first place, so any token attach
        // is either circular or a stale value.
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(
                HttpStatusCode.OK,
                "{\"token\":\"new\",\"expire_at_utc\":\"2026-06-03T00:00:00Z\"}"));

        var http = Build(tokenProvider, script);
        await http.PostAsync("/api/authenticate", new StringContent("{}"));

        script.Recorded.Should().HaveCount(1);
        script.Recorded[0].Authorization.Should().BeNull();
        tokenProvider.Verify(
            t => t.GetTokenAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task On_401_invalidates_token_and_retries_with_fresh_one()
    {
        var tokenProvider = new Mock<ISamedayTokenProvider>();
        var fetchSeq = 0;
        tokenProvider
            .Setup(t => t.GetTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                fetchSeq++;
                return new SamedayToken($"TOK{fetchSeq}", DateTimeOffset.UtcNow.AddHours(1));
            });

        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Empty(HttpStatusCode.Unauthorized),
            _ => ScriptedHttpMessageHandler.Json(HttpStatusCode.OK, "{}"));

        var http = Build(tokenProvider, script);
        var response = await http.GetAsync("/api/awb/RO123/tracking");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        script.Recorded.Should().HaveCount(2);
        script.Recorded[0].Authorization!.Parameter.Should().Be("TOK1");
        script.Recorded[1].Authorization!.Parameter.Should().Be("TOK2");
        tokenProvider.Verify(t => t.Invalidate(), Times.Once);
    }

    [Fact]
    public async Task Second_401_raises_SamedayAuthException()
    {
        var tokenProvider = new Mock<ISamedayTokenProvider>();
        var fetchSeq = 0;
        tokenProvider
            .Setup(t => t.GetTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                fetchSeq++;
                return new SamedayToken($"TOK{fetchSeq}", DateTimeOffset.UtcNow.AddHours(1));
            });

        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Empty(HttpStatusCode.Unauthorized),
            _ => ScriptedHttpMessageHandler.Empty(HttpStatusCode.Unauthorized));

        var http = Build(tokenProvider, script);

        var act = () => http.GetAsync("/api/awb/RO123/tracking");
        var ex = await act.Should().ThrowAsync<SamedayAuthException>();
        ex.Which.Endpoint.Should().Contain("tracking");
        script.Recorded.Should().HaveCount(2);
    }

    [Fact]
    public async Task Retry_clones_body_so_inner_handler_can_read_it()
    {
        var tokenProvider = new Mock<ISamedayTokenProvider>();
        tokenProvider
            .Setup(t => t.GetTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SamedayToken("TOK", DateTimeOffset.UtcNow.AddHours(1)));

        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Empty(HttpStatusCode.Unauthorized),
            _ => ScriptedHttpMessageHandler.Json(HttpStatusCode.OK, "{}"));

        var http = Build(tokenProvider, script);
        var body = "{\"awb\":\"RO123\"}";
        await http.PostAsync("/api/awb", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        script.Recorded.Should().HaveCount(2);
        script.Recorded[0].BodyText().Should().Be(body);
        script.Recorded[1].BodyText().Should().Be(body);
    }
}
