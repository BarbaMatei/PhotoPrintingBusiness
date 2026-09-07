using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Extensions;
using PhotoPrint.API.Middleware;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Unit.Middleware;

public class UntrustedForwardedPeerMiddlewareTests
{
    private const string TrustedProxy      = "172.28.0.10";
    private const string UntrustedPeer     = "10.9.9.9";
    private const string ForwardedFor      = "203.0.113.9";
    private const string UntrustedPrefix   = "forwarded_headers.untrusted_peer ip=";
    private const string UnparseablePrefix = "forwarded_headers.unparseable_forwarded_for ip=";
    private const int LoggedPeerCap        = 512;

    private readonly LogCapture _logs = new();
    private readonly ServiceProvider _services;
    private readonly UntrustedForwardedPeerMiddleware _sut;

    public UntrustedForwardedPeerMiddlewareTests()
    {
        _services = TrustedProxyServices();
        _sut = new UntrustedForwardedPeerMiddleware(
            _logs.LoggerFor<UntrustedForwardedPeerMiddleware>(),
            _services.GetRequiredService<TrustedProxyList>());
    }

    [Fact]
    public async Task Untrusted_peer_sending_forwarded_for_is_warned_once()
    {
        var context = await Send(UntrustedPeer, ForwardedFor);
        await Send(UntrustedPeer, ForwardedFor);
        await Send(UntrustedPeer, ForwardedFor);

        Warnings(UntrustedPrefix).Should().Be(1);
        _logs.Records.Should().ContainSingle(r => r.Message.Contains(UntrustedPeer));
        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse(UntrustedPeer));
    }

    [Fact]
    public async Task Each_distinct_untrusted_peer_is_warned()
    {
        await Send(UntrustedPeer, ForwardedFor);
        await Send("10.9.9.10", ForwardedFor);

        Warnings(UntrustedPrefix).Should().Be(2);
    }

    [Fact]
    public async Task Trusted_peer_sending_forwarded_for_is_not_warned()
    {
        var context = await Send(TrustedProxy, ForwardedFor);

        Warnings(UntrustedPrefix).Should().Be(0);
        Warnings(UnparseablePrefix).Should().Be(0);
        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse(ForwardedFor));
    }

    [Fact]
    public async Task Peer_sending_no_forwarded_for_is_not_warned()
    {
        await Send(UntrustedPeer, forwardedFor: null);
        await Send(TrustedProxy, forwardedFor: null);

        Warnings(UntrustedPrefix).Should().Be(0);
        Warnings(UnparseablePrefix).Should().Be(0);
    }

    [Fact]
    public async Task Trusted_peer_with_unparseable_forwarded_value_is_not_reported_untrusted()
    {
        var context = await Send(TrustedProxy, "unknown");

        Warnings(UntrustedPrefix).Should().Be(0);
        Warnings(UnparseablePrefix).Should().Be(1);
        _logs.Records.Should().ContainSingle(r => r.Message.Contains(TrustedProxy));
        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse(TrustedProxy));
    }

    [Fact]
    public async Task Trusted_peer_with_an_empty_forwarded_value_is_not_reported_untrusted()
    {
        await Send(TrustedProxy, string.Empty);

        Warnings(UntrustedPrefix).Should().Be(0);
        Warnings(UnparseablePrefix).Should().Be(1);
    }

    [Fact]
    public async Task Trusted_peer_whose_forwarded_entry_carries_a_port_is_honoured()
    {
        var context = await Send(TrustedProxy, $"{ForwardedFor}:5678");

        Warnings(UntrustedPrefix).Should().Be(0);
        Warnings(UnparseablePrefix).Should().Be(0);
        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse(ForwardedFor));
    }

    [Fact]
    public async Task An_exhausted_untrusted_cap_does_not_silence_parse_failures()
    {
        for (var i = 0; i < LoggedPeerCap; i++)
            await Send($"10.10.{i / 256}.{i % 256}", ForwardedFor);

        await Send(TrustedProxy, "unknown");

        Warnings(UntrustedPrefix).Should().Be(LoggedPeerCap);
        Warnings(UnparseablePrefix).Should().Be(1);
    }

    private int Warnings(string prefix) => _logs.CountStartingWith(prefix, LogLevel.Warning);

    private async Task<HttpContext> Send(string peer, string? forwardedFor)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(peer);
        if (forwardedFor is not null)
        {
            ((IDictionary<string, StringValues>)context.Request.Headers).Add(
                ForwardedHeadersDefaults.XForwardedForHeaderName, new StringValues(forwardedFor));
        }

        var forwardedHeaders = new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            _services.GetRequiredService<IOptions<ForwardedHeadersOptions>>());

        await _sut.InvokeAsync(context, forwardedHeaders.Invoke);
        return context;
    }

    private static ServiceProvider TrustedProxyServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:TrustedProxies:0"] = TrustedProxy,
            })
            .Build();

        return new ServiceCollection()
            .AddTrustedProxyForwardedHeaders(configuration)
            .BuildServiceProvider();
    }
}
