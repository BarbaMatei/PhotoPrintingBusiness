using System.Diagnostics;
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhotoPrint.API.Data;
using PhotoPrint.API.Observability;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Integration;

[Collection(ObservabilityHostCollection.Name)]
public class MetricsEndpointIntegrationTests
{
    [Fact]
    public async Task Disabled_observability_makes_endpoint_absent()
    {
        using var factory = new ObservabilityDisabledFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/metrics");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Enabled_with_loopback_allowed_returns_prometheus_text()
    {
        using var factory = new ObservabilityEnabledLoopbackFactory();
        using var client  = factory.CreateClient();
        var response = await client.GetAsync("/metrics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().StartWith("text/plain");

        var body = await response.Content.ReadAsStringAsync();
        // Prometheus exposition format includes # HELP / # TYPE comment lines.
        body.Should().Contain("# HELP");
        body.Should().Contain("# TYPE");
    }

    [Fact]
    public async Task Enabled_with_disallowed_ip_returns_403()
    {
        using var factory = new ObservabilityEnabledNoLoopbackFactory();
        using var client  = factory.CreateClient();
        var response = await client.GetAsync("/metrics");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public void An_unparseable_allow_list_entry_aborts_boot()
    {
        // The runbook promises a typo can never silently disable scraping. That promise rests on
        // ValidateOnStart wiring, so it is proven against a real host boot, not the validator.
        using var factory = new ObservabilityInvalidAllowListFactory();

        var act = () => factory.CreateClient();

        act.Should().Throw<Exception>()
            .Which.ToString().Should().Contain("AllowedScrapeIps").And.Contain("not.an.ip");
    }

    [Fact]
    public async Task Scrape_port_configured_makes_metrics_absent_on_the_public_listener()
    {
        using var factory = new ObservabilityEnabledWrongListenerFactory();
        using var client  = factory.CreateClient();
        var response = await client.GetAsync("/metrics");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("# HELP");
    }

    [Fact]
    public async Task Scrape_port_configured_serves_metrics_on_the_scrape_listener()
    {
        using var factory = new ObservabilityEnabledScrapeListenerFactory();
        using var client  = factory.CreateClient();
        var response = await client.GetAsync("/metrics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("# HELP");
    }

    [Fact]
    public async Task A_business_metric_reaches_the_exposition()
    {
        // # HELP / # TYPE alone are emitted by the runtime and ASP.NET instrumentation, so they
        // prove nothing about FotoMetrics being wired into the exporter's meter list.
        using var factory = new ObservabilityEnabledLoopbackFactory();
        using var client  = factory.CreateClient();

        FotoMetrics.OrdersCreated.Add(1,
            new TagList
            {
                { MetricNames.Labels.Processor, MetricNames.ProcessorValues.Stripe },
                { MetricNames.Labels.Status,    MetricNames.OrderStatusValues.Created },
            });

        var body = await (await client.GetAsync("/metrics")).Content.ReadAsStringAsync();

        body.Should().Contain(MetricNames.Instruments.OrdersCreatedTotal);
    }
}

/// <summary>
/// TestServer reports <c>HttpContext.Connection.RemoteIpAddress</c> as null
/// and <c>LocalPort</c> as 0 by default. The metrics gate treats null as
/// disallowed (correct in production), so tests that need a specific source
/// IP or listener port must stamp them onto the connection before the gate
/// runs. A startup filter is the cleanest seam for this.
/// </summary>
internal sealed class SetConnectionStartupFilter : IStartupFilter
{
    private readonly IPAddress? _ip;
    private readonly int _localPort;

    public SetConnectionStartupFilter(IPAddress? ip, int localPort)
    {
        _ip        = ip;
        _localPort = localPort;
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (ctx, n) =>
        {
            if (_ip is not null) ctx.Connection.RemoteIpAddress = _ip;
            if (_localPort != 0) ctx.Connection.LocalPort = _localPort;
            await n();
        });
        next(app);
    };
}

internal abstract class ObservabilityFactoryBase : WebApplicationFactory<Program>
{
    protected abstract Dictionary<string, string?> ExtraConfig();
    protected abstract IPAddress? SimulatedRemoteIp();
    protected virtual int SimulatedLocalPort() => 0;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Program.cs reads Observability:Enabled and Sentry:Enabled from builder.Configuration
        // before the host is built, which is earlier than ConfigureAppConfiguration runs.
        // UseSetting travels as a command-line argument to the entry point, so it arrives in
        // time — and unlike an environment variable it belongs to this host alone.
        foreach (var (key, value) in ExtraConfig())
            builder.UseSetting(key, value);

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            var dict = new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = "https://test.example.com",
                ["RateLimit:WindowSeconds"] = "60",
                ["RateLimit:Public:PermitLimit"] = "10000",
                ["RateLimit:Auth:PermitLimit"] = "10",
                ["SecurityHeaders:ContentSecurityPolicy"] = "default-src 'self'",
                ["Email:Provider"] = "Smtp",
                ["Email:FromAddress"] = "test@fototipar.ro",
                ["Email:FromName"] = "FotoTipar Test",
                ["Email:OperatorBcc"] = "",
                ["Email:Smtp:Host"] = "localhost",
                ["Email:Smtp:Port"] = "1025",
                ["Email:Smtp:UseSsl"] = "false",
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["HealthCheck:UploadsPath"] = "uploads",
                ["JwtSettings:PrivateKeyPem"] = TestKeys.RsaPrivateKeyPem,
                ["JwtSettings:Issuer"] = "fototipar",
                ["JwtSettings:Audience"] = "fototipar-spa",
                ["JwtSettings:AccessTokenMinutes"] = "15",
                ["JwtSettings:RefreshTokenDays"] = "30",
                ["App:BaseUrl"] = "http://localhost:4200",
            };
            foreach (var kv in ExtraConfig()) dict[kv.Key] = kv.Value;
            cfg.AddInMemoryCollection(dict);
        });

        builder.ConfigureServices(services =>
        {
            var db = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PhotoPrintDbContext>));
            if (db is not null) services.Remove(db);
            services.AddDbContext<PhotoPrintDbContext>(o =>
                o.UseInMemoryDatabase($"MetricsTests_{Guid.NewGuid()}"));

            var simulatedIp   = SimulatedRemoteIp();
            var simulatedPort = SimulatedLocalPort();
            if (simulatedIp is not null || simulatedPort != 0)
                services.AddSingleton<IStartupFilter>(new SetConnectionStartupFilter(simulatedIp, simulatedPort));
        });
    }
}

internal sealed class ObservabilityDisabledFactory : ObservabilityFactoryBase
{
    protected override Dictionary<string, string?> ExtraConfig() => new()
    {
        ["Observability:Enabled"] = "false",
    };
    protected override IPAddress? SimulatedRemoteIp() => null;   // not needed
}

internal sealed class ObservabilityEnabledLoopbackFactory : ObservabilityFactoryBase
{
    protected override Dictionary<string, string?> ExtraConfig() => new()
    {
        ["Observability:Enabled"]                    = "true",
        ["Observability:Metrics:AllowedScrapeIps:0"] = "127.0.0.1",
    };
    protected override IPAddress? SimulatedRemoteIp() => IPAddress.Loopback;
}

internal sealed class ObservabilityEnabledNoLoopbackFactory : ObservabilityFactoryBase
{
    protected override Dictionary<string, string?> ExtraConfig() => new()
    {
        ["Observability:Enabled"]                    = "true",
        ["Observability:Metrics:AllowedScrapeIps:0"] = "10.99.99.99",
    };
    protected override IPAddress? SimulatedRemoteIp() => IPAddress.Parse("203.0.113.7");
}

/// <summary>
/// A scrape port is configured and the request arrives on the public listener —
/// the shape of a request proxied in from the TLS edge. Even with the peer IP
/// allow-listed, the metric store must not be served.
/// </summary>
internal sealed class ObservabilityEnabledWrongListenerFactory : ObservabilityFactoryBase
{
    protected override Dictionary<string, string?> ExtraConfig() => new()
    {
        ["Observability:Enabled"]                     = "true",
        ["Observability:Metrics:ScrapePort"]          = "9090",
        ["Observability:Metrics:AllowedScrapeIps:0"]  = "10.42.0.5",
    };
    protected override IPAddress? SimulatedRemoteIp() => IPAddress.Parse("10.42.0.5");
    protected override int SimulatedLocalPort() => 8080;
}

internal sealed class ObservabilityInvalidAllowListFactory : ObservabilityFactoryBase
{
    protected override Dictionary<string, string?> ExtraConfig() => new()
    {
        ["Observability:Enabled"]                    = "true",
        ["Observability:Metrics:AllowedScrapeIps:0"] = "not.an.ip",
    };
    protected override IPAddress? SimulatedRemoteIp() => null;
}

internal sealed class ObservabilityEnabledScrapeListenerFactory : ObservabilityFactoryBase
{
    protected override Dictionary<string, string?> ExtraConfig() => new()
    {
        ["Observability:Enabled"]                     = "true",
        ["Observability:Metrics:ScrapePort"]          = "9090",
        ["Observability:Metrics:AllowedScrapeIps:0"]  = "10.42.0.5",
    };
    protected override IPAddress? SimulatedRemoteIp() => IPAddress.Parse("10.42.0.5");
    protected override int SimulatedLocalPort() => 9090;
}

internal sealed class ObservabilityDefaultFactory : ObservabilityFactoryBase
{
    protected override Dictionary<string, string?> ExtraConfig() => new();
    protected override IPAddress? SimulatedRemoteIp() => IPAddress.Loopback;
}
