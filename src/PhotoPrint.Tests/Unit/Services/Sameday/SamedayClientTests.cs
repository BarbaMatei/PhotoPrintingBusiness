using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

/// <summary>
/// Tests for the bolt-036 surface of <see cref="SamedayClient"/>:
///   - <see cref="SamedayClient.AuthenticateAsync"/> happy path + every error branch,
///   - the AWB/label/tracking methods all throw <c>NotImplementedException</c>
///     (declared-but-deferred-to-bolt-037 contract).
/// </summary>
public class SamedayClientTests
{
    private static SamedayClient Build(ScriptedHttpMessageHandler script)
    {
        var http = new HttpClient(script)
        {
            BaseAddress = new Uri("https://sameday-test/"),
        };
        return new SamedayClient(http, new LoggerFactory().CreateLogger<SamedayClient>());
    }

    private static readonly SamedayCredentials Creds = new("user", "pw");

    // ── AuthenticateAsync — happy path ────────────────────────────────────────

    [Fact]
    public async Task AuthenticateAsync_returns_token_on_200_with_valid_body()
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(
                HttpStatusCode.OK,
                "{\"token\":\"abc-token\",\"expire_at_utc\":\"2026-06-03T00:00:00Z\"}"));

        var sut = Build(script);
        var token = await sut.AuthenticateAsync(Creds);

        token.Value.Should().Be("abc-token");
        token.ExpiresAt.Should().Be(new DateTimeOffset(2026, 6, 3, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task AuthenticateAsync_posts_credentials_in_body()
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(
                HttpStatusCode.OK,
                "{\"token\":\"t\",\"expire_at_utc\":\"2026-06-03T00:00:00Z\"}"));

        var sut = Build(script);
        await sut.AuthenticateAsync(new SamedayCredentials("alice", "secret-pw"));

        script.Recorded.Should().HaveCount(1);
        script.Recorded[0].Method.Should().Be(HttpMethod.Post);
        var body = script.Recorded[0].BodyText();
        body.Should().Contain("alice");
        body.Should().Contain("secret-pw");
    }

    // ── AuthenticateAsync — error branches ────────────────────────────────────

    [Fact]
    public async Task AuthenticateAsync_throws_SamedayAuthException_on_401()
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Empty(HttpStatusCode.Unauthorized));

        var sut = Build(script);
        var act = () => sut.AuthenticateAsync(Creds);

        var ex = await act.Should().ThrowAsync<SamedayAuthException>();
        ex.Which.Endpoint.Should().Contain("/api/authenticate");
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    public async Task AuthenticateAsync_throws_Unreachable_on_5xx_and_408(HttpStatusCode status)
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Empty(status));

        var sut = Build(script);
        var act = () => sut.AuthenticateAsync(Creds);

        var ex = await act.Should().ThrowAsync<SamedayUnreachableException>();
        ex.Which.HttpStatus.Should().Be((int)status);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task AuthenticateAsync_throws_Validation_on_other_4xx(HttpStatusCode status)
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(status, "{\"error\":\"nope\"}"));

        var sut = Build(script);
        var act = () => sut.AuthenticateAsync(Creds);

        var ex = await act.Should().ThrowAsync<SamedayValidationException>();
        ex.Which.HttpStatus.Should().Be((int)status);
    }

    [Fact]
    public async Task AuthenticateAsync_throws_Protocol_on_2xx_with_missing_token()
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(
                HttpStatusCode.OK,
                "{\"expire_at_utc\":\"2026-06-03T00:00:00Z\"}"));

        var sut = Build(script);
        var act = () => sut.AuthenticateAsync(Creds);

        var ex = await act.Should().ThrowAsync<SamedayProtocolException>();
        ex.Which.Message.Should().Contain("token");
    }

    [Fact]
    public async Task AuthenticateAsync_throws_Protocol_on_2xx_with_missing_expiry()
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(
                HttpStatusCode.OK,
                "{\"token\":\"abc\"}"));

        var sut = Build(script);
        var act = () => sut.AuthenticateAsync(Creds);

        var ex = await act.Should().ThrowAsync<SamedayProtocolException>();
        ex.Which.Message.Should().Contain("expire_at_utc");
    }

    [Fact]
    public async Task AuthenticateAsync_throws_Protocol_on_malformed_json()
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(HttpStatusCode.OK, "not-json{"));

        var sut = Build(script);
        var act = () => sut.AuthenticateAsync(Creds);

        var ex = await act.Should().ThrowAsync<SamedayProtocolException>();
        ex.Which.Message.Should().Contain("JSON");
    }

    [Fact]
    public async Task AuthenticateAsync_throws_Unreachable_on_HttpRequestException()
    {
        var script = new ScriptedHttpMessageHandler(
            _ => throw new HttpRequestException("DNS failure"));

        var sut = Build(script);
        var act = () => sut.AuthenticateAsync(Creds);

        await act.Should().ThrowAsync<SamedayUnreachableException>();
    }

    // ── Bolt-037 stubs ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAwbAsync_throws_NotImplemented_in_bolt_036()
    {
        var sut = Build(new ScriptedHttpMessageHandler());
        var act = () => sut.CreateAwbAsync(new AwbCreationRequest(
            "1", "name", "phone", "addr", "city", "county", "00000", 0.1m, 1, 0m, null));
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task GetLabelPdfAsync_throws_NotImplemented_in_bolt_036()
    {
        var sut = Build(new ScriptedHttpMessageHandler());
        var act = () => sut.GetLabelPdfAsync("RO123");
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task GetTrackingAsync_throws_NotImplemented_in_bolt_036()
    {
        var sut = Build(new ScriptedHttpMessageHandler());
        var act = () => sut.GetTrackingAsync("RO123");
        await act.Should().ThrowAsync<NotImplementedException>();
    }
}
