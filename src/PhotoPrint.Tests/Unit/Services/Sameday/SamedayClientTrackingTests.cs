using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

/// <summary>
/// Coverage of <see cref="SamedayClient.GetTrackingAsync"/>, in particular
/// the vendor-status-code → <see cref="TrackingState"/> mapping table —
/// the anti-corruption boundary for Sameday's wire vocabulary.
/// </summary>
public class SamedayClientTrackingTests
{
    private static SamedayClient Build(ScriptedHttpMessageHandler script)
    {
        var http = new HttpClient(script) { BaseAddress = new Uri("https://sameday-test/") };
        return new SamedayClient(http, new LoggerFactory().CreateLogger<SamedayClient>());
    }

    private static string TrackingJson(string status) =>
        $"{{\"awbNumber\":\"RO123\",\"status\":\"{status}\",\"observedAt\":\"2026-06-02T12:00:00Z\",\"history\":[]}}";

    [Theory]
    [InlineData("awb-issued",        TrackingState.Pending)]
    [InlineData("pickup-pending",    TrackingState.Pending)]
    [InlineData("picked-up",         TrackingState.InTransit)]
    [InlineData("in-transit",        TrackingState.InTransit)]
    [InlineData("arrived-at-sortation", TrackingState.InTransit)]
    [InlineData("out-for-pickup",    TrackingState.InTransit)]
    [InlineData("out-for-delivery",  TrackingState.OutForDelivery)]
    [InlineData("at-locker",         TrackingState.OutForDelivery)]
    [InlineData("delivered",         TrackingState.Delivered)]
    [InlineData("delivered-to-locker-with-pickup", TrackingState.Delivered)]
    [InlineData("failed-delivery",   TrackingState.Failed)]
    [InlineData("returned-to-sender",TrackingState.Failed)]
    [InlineData("lost",              TrackingState.Failed)]
    [InlineData("cancelled",         TrackingState.Cancelled)]
    [InlineData("something-new",     TrackingState.Unknown)]
    [InlineData("",                  TrackingState.Unknown)]
    [InlineData("DELIVERED",         TrackingState.Delivered)]   // case-insensitive
    public async Task Maps_vendor_status_to_TrackingState(string vendorStatus, TrackingState expected)
    {
        if (string.IsNullOrEmpty(vendorStatus))
        {
            // Empty status is rejected upstream as a protocol violation.
            var script = new ScriptedHttpMessageHandler(
                _ => ScriptedHttpMessageHandler.Json(HttpStatusCode.OK, "{\"awbNumber\":\"RO123\",\"status\":\"\"}"));
            var sutEmpty = Build(script);
            var actEmpty = () => sutEmpty.GetTrackingAsync("RO123");
            await actEmpty.Should().ThrowAsync<SamedayProtocolException>();
            return;
        }

        var scriptOk = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(HttpStatusCode.OK, TrackingJson(vendorStatus)));
        var sut = Build(scriptOk);

        var snapshot = await sut.GetTrackingAsync("RO123");
        snapshot.State.Should().Be(expected);
    }

    [Fact]
    public async Task GetTrackingAsync_propagates_observed_at_and_awb_number()
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(HttpStatusCode.OK, TrackingJson("in-transit")));

        var sut = Build(script);
        var snapshot = await sut.GetTrackingAsync("RO12345678");

        snapshot.AwbNumber.Should().Be("RO12345678");
        snapshot.ObservedAt.Should().Be(new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task GetTrackingAsync_leaves_observedAt_null_when_the_vendor_omits_it()
    {
        // the client must not fabricate a wall-clock "now" (which would land in DeliveredAt);
        // the poll caller supplies its own clock as the fallback.
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(HttpStatusCode.OK,
                "{\"awbNumber\":\"RO123\",\"status\":\"in-transit\"}"));

        var sut = Build(script);
        var snapshot = await sut.GetTrackingAsync("RO123");

        snapshot.ObservedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetTrackingAsync_throws_Auth_on_401()
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Empty(HttpStatusCode.Unauthorized));
        var sut = Build(script);
        var act = () => sut.GetTrackingAsync("RO123");
        await act.Should().ThrowAsync<SamedayAuthException>();
    }

    [Fact]
    public async Task GetTrackingAsync_throws_Unreachable_on_5xx()
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Empty(HttpStatusCode.ServiceUnavailable));
        var sut = Build(script);
        var act = () => sut.GetTrackingAsync("RO123");
        await act.Should().ThrowAsync<SamedayUnreachableException>();
    }

    [Fact]
    public async Task GetTrackingAsync_throws_Protocol_on_malformed_json()
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(HttpStatusCode.OK, "not-json{"));
        var sut = Build(script);
        var act = () => sut.GetTrackingAsync("RO123");
        await act.Should().ThrowAsync<SamedayProtocolException>();
    }
}
