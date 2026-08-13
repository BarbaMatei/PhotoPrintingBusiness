using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using PhotoPrint.API.Extensions;
using PhotoPrint.API.Middleware;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Unit.Observability;

/// <summary>
/// The scrape gate holds cross-request state (the parsed allow-list and the once-per-IP deny
/// log), so its DI lifetime is part of its contract. <c>UseMiddleware</c> resolves an
/// <c>IMiddleware</c> per request through <c>IMiddlewareFactory</c>; a per-request instance
/// silently empties that state. These tests go through the real <c>AddObservability</c>
/// registration and drive requests from separate scopes, the way the factory does.
/// </summary>
public class MetricsScrapeGateRegistrationTests
{
    private static ServiceProvider BuildProvider(LogCapture capture)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Observability:Enabled"]                        = "true",
                ["Observability:ServiceName"]                    = "PhotoPrint.API",
                ["Observability:Metrics:PrometheusEndpoint"]     = "/metrics",
                ["Observability:Metrics:AllowedScrapeIps:0"]     = "10.42.0.5",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(lb => lb.AddProvider(new LogCaptureProvider(capture)));
        services.AddObservability(configuration, Mock.Of<IHostEnvironment>(e => e.EnvironmentName == "Testing"));
        return services.BuildServiceProvider();
    }

    private static async Task DenyOnceFromNewScope(ServiceProvider provider, IPAddress peer)
    {
        using var scope = provider.CreateScope();
        var gate = scope.ServiceProvider.GetRequiredService<MetricsEndpointIpAllowListMiddleware>();
        var ctx  = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = peer;
        await gate.InvokeAsync(ctx, _ => Task.CompletedTask);
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public void Gate_resolves_to_the_same_instance_across_request_scopes()
    {
        using var provider = BuildProvider(new LogCapture());
        using var first    = provider.CreateScope();
        using var second   = provider.CreateScope();

        var a = first.ServiceProvider.GetRequiredService<MetricsEndpointIpAllowListMiddleware>();
        var b = second.ServiceProvider.GetRequiredService<MetricsEndpointIpAllowListMiddleware>();

        a.Should().BeSameAs(b);
    }

    [Fact]
    public async Task Repeated_denials_from_one_peer_log_a_single_line_across_requests()
    {
        var capture = new LogCapture();
        using var provider = BuildProvider(capture);
        var scanner = IPAddress.Parse("203.0.113.7");

        for (var i = 0; i < 5; i++)
            await DenyOnceFromNewScope(provider, scanner);

        capture.CountStartingWith("metrics.scrape.denied ip=", LogLevel.Information).Should().Be(1);
    }

    [Fact]
    public async Task Distinct_peers_each_log_once()
    {
        var capture = new LogCapture();
        using var provider = BuildProvider(capture);

        await DenyOnceFromNewScope(provider, IPAddress.Parse("203.0.113.7"));
        await DenyOnceFromNewScope(provider, IPAddress.Parse("203.0.113.8"));
        await DenyOnceFromNewScope(provider, IPAddress.Parse("203.0.113.7"));

        capture.CountStartingWith("metrics.scrape.denied ip=", LogLevel.Information).Should().Be(2);
    }
}
