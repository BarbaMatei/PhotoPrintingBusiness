using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace PhotoPrint.Tests.Integration;

public class ForwardedHeadersIntegrationTests
{
    private const string ProxyAddress  = "172.28.0.2";
    private const string ClientAddress = "203.0.113.9";

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

        var resolved = await factory.ResolveAsync(peer: "10.9.9.9", forwardedFor: ClientAddress);

        resolved.ClientIp.Should().Be("10.9.9.9");
    }

    [Fact]
    public async Task Only_the_rightmost_entry_the_proxy_appended_is_honoured()
    {
        using var factory = new TrustedProxyFactory(ProxyAddress);

        var resolved = await factory.ResolveAsync(
            peer: ProxyAddress, forwardedFor: $"198.51.100.1, {ClientAddress}");

        resolved.ClientIp.Should().Be(ClientAddress);
    }

    [Fact]
    public async Task A_cidr_entry_trusts_every_proxy_in_the_range()
    {
        using var factory = new TrustedProxyFactory("172.28.0.0/24");

        var resolved = await factory.ResolveAsync(peer: "172.28.0.7", forwardedFor: ClientAddress);

        resolved.ClientIp.Should().Be(ClientAddress);
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

internal sealed class TrustedProxyFactory : ObservabilityFactoryBase
{
    private readonly string[] _trustedProxies;
    private readonly ClientIdentityProbe _probe = new();

    public TrustedProxyFactory(params string[] trustedProxies) => _trustedProxies = trustedProxies;

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
            services.AddSingleton<IStartupFilter>(new ClientIdentityProbeStartupFilter(_probe)));
    }

    public async Task<ResolvedRequest> ResolveAsync(
        string peer, string forwardedFor, string? forwardedProto = null)
    {
        _probe.Peer     = IPAddress.Parse(peer);
        _probe.Resolved = null;
        using var client = CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/__probe/client-identity");
        request.Headers.Add("X-Forwarded-For", forwardedFor);
        if (forwardedProto is not null)
            request.Headers.Add("X-Forwarded-Proto", forwardedProto);

        await client.SendAsync(request);

        _probe.Resolved.Should().NotBeNull("the probe middleware runs on every request");
        return _probe.Resolved!;
    }
}
