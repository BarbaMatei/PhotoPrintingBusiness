using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Integration;

public class ForwardedHeadersIntegrationTests
{
    private const string ProxyAddress    = "172.28.0.2";
    private const string ClientAddress   = "203.0.113.9";
    private const string UntrustedPeer   = "10.9.9.9";
    private const string UntrustedPrefix = "forwarded_headers.untrusted_peer ip=";

    [Fact]
    public async Task A_trusted_proxy_resolves_the_client_from_x_forwarded_for()
    {
        using var factory = new TrustedProxyFactory(ProxyAddress);

        var resolved = await factory.ResolveAsync(peer: ProxyAddress, forwardedFor: ClientAddress);

        resolved.ClientIp.Should().Be(ClientAddress);
    }

    [Fact]
    public async Task An_untrusted_peer_cannot_name_the_client()
    {
        using var factory = new TrustedProxyFactory(ProxyAddress);

        var resolved = await factory.ResolveAsync(peer: UntrustedPeer, forwardedFor: ClientAddress);

        resolved.ClientIp.Should().Be(UntrustedPeer);
        factory.Warnings(UntrustedPrefix).Should().Be(1);
    }

    [Fact]
    public async Task Trusted_proxy_with_multi_entry_forwarded_for_emits_no_untrusted_warning()
    {
        using var factory = new TrustedProxyFactory(ProxyAddress);

        var resolved = await factory.ResolveAsync(
            peer: ProxyAddress, forwardedFor: $"198.51.100.1, {ClientAddress}");

        resolved.ClientIp.Should().Be(ClientAddress);
        factory.Warnings(UntrustedPrefix).Should().Be(0);
    }

    [Fact]
    public async Task A_single_pair_cidr_entry_trusts_both_of_its_addresses()
    {
        using var factory = new TrustedProxyFactory("172.28.0.2/31");

        var resolved = await factory.ResolveAsync(peer: "172.28.0.3", forwardedFor: ClientAddress);

        resolved.ClientIp.Should().Be(ClientAddress);
        factory.Warnings(UntrustedPrefix).Should().Be(0);
    }

    [Fact]
    public async Task An_empty_trusted_proxy_list_leaves_the_peer_as_the_client()
    {
        using var factory = new TrustedProxyFactory();

        var resolved = await factory.ResolveAsync(peer: ProxyAddress, forwardedFor: ClientAddress);

        resolved.ClientIp.Should().Be(ProxyAddress);
    }

    [Fact]
    public async Task A_trusted_proxy_reporting_https_makes_the_request_secure()
    {
        using var factory = new TrustedProxyFactory(ProxyAddress);

        var resolved = await factory.ResolveAsync(
            peer: ProxyAddress, forwardedFor: ClientAddress, forwardedProto: "https");

        resolved.Scheme.Should().Be("https");
        resolved.IsHttps.Should().BeTrue();
    }

    [Fact]
    public async Task An_untrusted_peer_cannot_claim_https()
    {
        using var factory = new TrustedProxyFactory(ProxyAddress);

        var resolved = await factory.ResolveAsync(
            peer: "10.9.9.9", forwardedFor: ClientAddress, forwardedProto: "https");

        resolved.IsHttps.Should().BeFalse();
    }

    [Fact]
    public void An_unparseable_trusted_proxy_aborts_boot()
    {
        using var factory = new TrustedProxyFactory("not.an.ip");

        var act = () => factory.CreateClient();

        act.Should().Throw<OptionsValidationException>()
            .Which.ToString().Should().Contain("TrustedProxies").And.Contain("not.an.ip");
    }
}

[Collection(ObservabilityHostCollection.Name)]
public class ForwardedHeadersWithObservabilityTests
{
    [Fact]
    public async Task A_request_on_the_public_listener_still_resolves_its_client()
    {
        using var factory = new TrustedProxyWithScrapeListenerFactory();

        var resolved = await factory.ResolveAsync(peer: "172.28.0.2", forwardedFor: "203.0.113.9");

        resolved.ClientIp.Should().Be("203.0.113.9");
    }

    [Fact]
    public async Task A_non_metrics_path_on_the_scrape_port_still_resolves_its_client()
    {
        using var factory = new TrustedProxyOnScrapeListenerFactory();

        var resolved = await factory.ResolveAsync(peer: "172.28.0.2", forwardedFor: "203.0.113.9");

        resolved.ClientIp.Should().Be("203.0.113.9");
    }

    [Fact]
    public async Task The_metrics_path_on_the_public_port_still_resolves_its_client()
    {
        using var factory = new TrustedProxyWithScrapeListenerFactory();

        var resolved = await factory.ResolveAsync(
            peer: "172.28.0.2", forwardedFor: "203.0.113.9", path: "/metrics");

        resolved.ClientIp.Should().Be("203.0.113.9");
    }

    [Fact]
    public void Trusted_proxies_with_observability_on_and_no_scrape_listener_aborts_boot()
    {
        using var factory = new TrustedProxyWithoutScrapeListenerFactory();

        var act = () => factory.CreateClient();

        act.Should().Throw<OptionsValidationException>()
            .Which.ToString().Should().Contain("Observability:Metrics:ScrapePort");
    }
}

internal sealed record ResolvedRequest(string? ClientIp, string Scheme, bool IsHttps);

internal sealed class ClientIdentityProbe
{
    public IPAddress Peer { get; set; } = IPAddress.Loopback;

    public ResolvedRequest? Resolved { get; set; }
}

internal sealed class ClientIdentityProbeStartupFilter : IStartupFilter
{
    private readonly ClientIdentityProbe _probe;

    public ClientIdentityProbeStartupFilter(ClientIdentityProbe probe) => _probe = probe;

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            context.Connection.RemoteIpAddress = _probe.Peer;
            await nextMiddleware();
            _probe.Resolved = new ResolvedRequest(
                context.Connection.RemoteIpAddress?.ToString(),
                context.Request.Scheme,
                context.Request.IsHttps);
        });
        next(app);
    };
}

internal class TrustedProxyWithScrapeListenerFactory : TrustedProxyFactory
{
    public TrustedProxyWithScrapeListenerFactory() : base("172.28.0.2") { }

    protected override Dictionary<string, string?> ExtraConfig()
    {
        var config = base.ExtraConfig();
        config["Observability:Enabled"]                    = "true";
        config["Observability:Metrics:ScrapePort"]         = "9090";
        config["Observability:Metrics:AllowedScrapeIps:0"] = "10.42.0.5";
        return config;
    }

    protected override int SimulatedLocalPort() => 8080;
}

internal sealed class TrustedProxyOnScrapeListenerFactory : TrustedProxyWithScrapeListenerFactory
{
    protected override int SimulatedLocalPort() => 9090;
}

internal sealed class TrustedProxyWithoutScrapeListenerFactory : TrustedProxyFactory
{
    public TrustedProxyWithoutScrapeListenerFactory() : base("172.28.0.2") { }

    protected override Dictionary<string, string?> ExtraConfig()
    {
        var config = base.ExtraConfig();
        config["Observability:Enabled"]            = "true";
        config["Observability:Metrics:ScrapePort"] = "0";
        return config;
    }
}

internal class TrustedProxyFactory : ObservabilityFactoryBase
{
    private readonly string[] _trustedProxies;
    private readonly ClientIdentityProbe _probe = new();
    private readonly LogCapture _logs = new();

    public TrustedProxyFactory(params string[] trustedProxies) => _trustedProxies = trustedProxies;

    public int Warnings(string prefix) => _logs.CountStartingWith(prefix, LogLevel.Warning);

    protected override Dictionary<string, string?> ExtraConfig()
    {
        var config = new Dictionary<string, string?> { ["Observability:Enabled"] = "false" };
        for (var i = 0; i < _trustedProxies.Length; i++)
            config[$"ForwardedHeaders:TrustedProxies:{i}"] = _trustedProxies[i];
        return config;
    }

    protected override IPAddress? SimulatedRemoteIp() => null;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IStartupFilter>(new ClientIdentityProbeStartupFilter(_probe));
            _logs.OutRegisterTheHostsSerilogLoggerFactory(services);
        });
    }

    public async Task<ResolvedRequest> ResolveAsync(
        string peer,
        string forwardedFor,
        string? forwardedProto = null,
        string path = "/__probe/client-identity")
    {
        _probe.Peer     = IPAddress.Parse(peer);
        _probe.Resolved = null;
        using var client = CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Forwarded-For", forwardedFor);
        if (forwardedProto is not null)
            request.Headers.Add("X-Forwarded-Proto", forwardedProto);

        await client.SendAsync(request);

        _probe.Resolved.Should().NotBeNull("the probe middleware runs on every request");
        return _probe.Resolved!;
    }
}
