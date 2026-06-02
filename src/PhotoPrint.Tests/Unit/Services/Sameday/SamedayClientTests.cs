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

    // ── Bolt-037: CreateAwbAsync / GetLabelPdfAsync / GetTrackingAsync ───────
    // The bolt-036 NotImplementedException stubs are replaced; full coverage
    // of the AWB + tracking surfaces lives in SamedayClientAwbTests and
    // SamedayClientTrackingTests. The three tests below are smoke checks
    // that the methods are wired and exercise the happy path.

    [Fact]
    public async Task CreateAwbAsync_returns_AwbCreationResult_on_200()
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(
                HttpStatusCode.OK,
                "{\"awbNumber\":\"RO12345678\",\"awbCost\":18.50,\"pdfLink\":\"https://sameday.cdn/labels/abc.pdf\"}"));

        var sut = Build(script);
        var result = await sut.CreateAwbAsync(new AwbCreationRequest(
            "1", "name", "phone", "addr", "city", "county", "00000", 0.1m, 1, 0m, null));

        result.AwbNumber.Should().Be("RO12345678");
        result.LabelUrl.Should().Be("https://sameday.cdn/labels/abc.pdf");
        result.CalculatedPrice.Should().Be(18.50m);
    }

    [Fact]
    public async Task GetLabelPdfAsync_returns_response_body_stream_on_200()
    {
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // "%PDF"
        var script = new ScriptedHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(pdfBytes),
            });

        var sut = Build(script);
        await using var stream = await sut.GetLabelPdfAsync("RO123");
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.ToArray().Should().Equal(pdfBytes);
    }

    [Fact]
    public async Task GetTrackingAsync_returns_normalised_state_for_delivered_vendor_code()
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(
                HttpStatusCode.OK,
                "{\"awbNumber\":\"RO123\",\"status\":\"delivered\",\"observedAt\":\"2026-06-02T10:00:00Z\",\"history\":[]}"));

        var sut = Build(script);
        var snapshot = await sut.GetTrackingAsync("RO123");

        snapshot.AwbNumber.Should().Be("RO123");
        snapshot.State.Should().Be(TrackingState.Delivered);
        snapshot.ObservedAt.Should().Be(new DateTimeOffset(2026, 6, 2, 10, 0, 0, TimeSpan.Zero));
    }
}
