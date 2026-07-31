using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Middleware;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Unit.Middleware;

/// <summary>
/// Pins the rule that the /metrics endpoint is gated by network identity, not JWT.
/// </summary>
public class MetricsEndpointIpAllowListMiddlewareTests
{
    private static MetricsEndpointIpAllowListMiddleware Build(params string[] allowedIps)
        => BuildOnPort(0, allowedIps);

    private static MetricsEndpointIpAllowListMiddleware BuildOnPort(int scrapePort, params string[] allowedIps)
    {
        var settings = Options.Create(new ObservabilitySettings
        {
            Metrics = new ObservabilityMetricsSettings
            {
                AllowedScrapeIps = allowedIps,
                ScrapePort       = scrapePort,
            },
        });
        return new MetricsEndpointIpAllowListMiddleware(
            settings, NullLogger<MetricsEndpointIpAllowListMiddleware>.Instance);
    }

    [Fact]
    public async Task Allowed_ipv4_passes_through()
    {
        var sut = Build("127.0.0.1");
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = IPAddress.Loopback;

        var nextCalled = false;
        await sut.InvokeAsync(ctx, _ => { nextCalled = true; return Task.CompletedTask; });

        nextCalled.Should().BeTrue();
        ctx.Response.StatusCode.Should().NotBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Allowed_ipv6_passes_through()
    {
        var sut = Build("::1");
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = IPAddress.IPv6Loopback;

        var nextCalled = false;
        await sut.InvokeAsync(ctx, _ => { nextCalled = true; return Task.CompletedTask; });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Disallowed_ip_returns_403_with_empty_body()
    {
        var sut = Build("127.0.0.1");
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = IPAddress.Parse("10.20.30.40");

        var nextCalled = false;
        await sut.InvokeAsync(ctx, _ => { nextCalled = true; return Task.CompletedTask; });

        nextCalled.Should().BeFalse();
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Null_remote_ip_returns_403()
    {
        var sut = Build("127.0.0.1");
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = null;

        var nextCalled = false;
        await sut.InvokeAsync(ctx, _ => { nextCalled = true; return Task.CompletedTask; });

        nextCalled.Should().BeFalse();
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Invalid_ip_strings_in_config_are_ignored_not_thrown()
    {
        // "not.an.ip" + "" + a valid IP — must not throw, must use the valid one.
        var sut = Build("not.an.ip", "", "127.0.0.1");

        var allowedCtx = new DefaultHttpContext { Connection = { RemoteIpAddress = IPAddress.Loopback } };
        var disallowedCtx = new DefaultHttpContext { Connection = { RemoteIpAddress = IPAddress.Parse("8.8.8.8") } };

        await sut.InvokeAsync(allowedCtx, _ => Task.CompletedTask);
        await sut.InvokeAsync(disallowedCtx, _ => Task.CompletedTask);

        allowedCtx.Response.StatusCode.Should().NotBe(StatusCodes.Status403Forbidden);
        disallowedCtx.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Empty_allow_list_blocks_everything()
    {
        var sut = Build();   // no IPs
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = IPAddress.Loopback;

        await sut.InvokeAsync(ctx, _ => Task.CompletedTask);

        ctx.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Deny_logging_stops_at_the_cap_and_warns_once()
    {
        var capture  = new LogCapture();
        var settings = Options.Create(new ObservabilitySettings
        {
            Metrics = new ObservabilityMetricsSettings { AllowedScrapeIps = ["10.0.0.1"] },
        });
        var sut = new MetricsEndpointIpAllowListMiddleware(
            settings, capture.LoggerFor<MetricsEndpointIpAllowListMiddleware>());

        for (var i = 0; i < 600; i++)
        {
            var ctx = new DefaultHttpContext();
            ctx.Connection.RemoteIpAddress = new IPAddress(new byte[] { 203, 0, (byte)(i / 256), (byte)(i % 256) });
            await sut.InvokeAsync(ctx, _ => Task.CompletedTask);
        }

        capture.CountStartingWith("metrics.scrape.denied ip=", LogLevel.Information)
            .Should().BeLessThanOrEqualTo(512);
        capture.CountStartingWith("metrics.scrape.denied.log_cap_reached", LogLevel.Warning)
            .Should().Be(1);
    }

    [Fact]
    public async Task Ipv4_mapped_ipv6_peer_matches_an_ipv4_allow_list_entry()
    {
        // The dual-mode socket Kestrel binds for http://+:8080 delivers IPv4 peers this way.
        var sut = Build("10.42.0.5");
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:10.42.0.5");

        var nextCalled = false;
        await sut.InvokeAsync(ctx, _ => { nextCalled = true; return Task.CompletedTask; });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Ipv4_mapped_ipv6_peer_outside_the_list_is_still_denied()
    {
        var sut = Build("10.42.0.5");
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:10.42.0.6");

        await sut.InvokeAsync(ctx, _ => Task.CompletedTask);

        ctx.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Cidr_entry_admits_an_in_range_peer_and_denies_one_outside()
    {
        var sut = Build("10.42.0.0/16");

        var inRange  = new DefaultHttpContext { Connection = { RemoteIpAddress = IPAddress.Parse("10.42.7.9") } };
        var outRange = new DefaultHttpContext { Connection = { RemoteIpAddress = IPAddress.Parse("10.43.0.1") } };

        var inRangeAllowed = false;
        await sut.InvokeAsync(inRange, _ => { inRangeAllowed = true; return Task.CompletedTask; });
        await sut.InvokeAsync(outRange, _ => Task.CompletedTask);

        inRangeAllowed.Should().BeTrue();
        outRange.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Cidr_entry_admits_an_ipv4_mapped_ipv6_peer_in_range()
    {
        var sut = Build("10.42.0.0/16");
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:10.42.7.9");

        var nextCalled = false;
        await sut.InvokeAsync(ctx, _ => { nextCalled = true; return Task.CompletedTask; });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task An_ipv6_range_does_not_admit_an_ipv4_peer()
    {
        var sut = Build("2001:db8::/32");
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = IPAddress.Parse("10.42.0.5");

        await sut.InvokeAsync(ctx, _ => Task.CompletedTask);

        ctx.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task A_link_local_peer_with_a_scope_id_matches_the_unscoped_entry()
    {
        var sut = Build("fe80::1");
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = IPAddress.Parse("fe80::1%3");

        var nextCalled = false;
        await sut.InvokeAsync(ctx, _ => { nextCalled = true; return Task.CompletedTask; });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Whitespace_padded_entry_is_honoured()
    {
        var sut = Build("  10.42.0.5  ");
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = IPAddress.Parse("10.42.0.5");

        var nextCalled = false;
        await sut.InvokeAsync(ctx, _ => { nextCalled = true; return Task.CompletedTask; });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Request_on_a_listener_other_than_the_scrape_port_is_404_even_from_an_allowed_ip()
    {
        var sut = BuildOnPort(9090, "10.0.0.1");
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        ctx.Connection.LocalPort       = 8080;

        var nextCalled = false;
        await sut.InvokeAsync(ctx, _ => { nextCalled = true; return Task.CompletedTask; });

        nextCalled.Should().BeFalse();
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Request_on_the_configured_scrape_port_from_an_allowed_ip_passes_through()
    {
        var sut = BuildOnPort(9090, "10.0.0.1");
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        ctx.Connection.LocalPort       = 9090;

        var nextCalled = false;
        await sut.InvokeAsync(ctx, _ => { nextCalled = true; return Task.CompletedTask; });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Scrape_port_zero_serves_every_listener()
    {
        var sut = BuildOnPort(0, "10.0.0.1");
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        ctx.Connection.LocalPort       = 8080;

        var nextCalled = false;
        await sut.InvokeAsync(ctx, _ => { nextCalled = true; return Task.CompletedTask; });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Wrong_port_wins_over_the_allow_list_so_a_denied_ip_also_sees_404()
    {
        var sut = BuildOnPort(9090, "10.0.0.1");
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");
        ctx.Connection.LocalPort       = 8080;

        await sut.InvokeAsync(ctx, _ => Task.CompletedTask);

        ctx.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}
