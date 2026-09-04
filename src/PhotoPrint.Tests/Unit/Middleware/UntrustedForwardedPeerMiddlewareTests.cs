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
using PhotoPrint.API.Extensions;
using PhotoPrint.API.Middleware;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Unit.Middleware;

public class UntrustedForwardedPeerMiddlewareTests
{
    private const string TrustedProxy   = "172.28.0.10";
    private const string UntrustedPeer  = "172.28.0.99";
    private const string ForwardedFor   = "203.0.113.9";
    private const string WarningPrefix  = "forwarded_headers.untrusted_peer ip=";

    private readonly LogCapture _logs = new();
    private readonly UntrustedForwardedPeerMiddleware _sut;

    public UntrustedForwardedPeerMiddlewareTests() =>
        _sut = new UntrustedForwardedPeerMiddleware(_logs.LoggerFor<UntrustedForwardedPeerMiddleware>());

    [Fact]
    public async Task Untrusted_peer_sending_forwarded_for_is_warned_once()
    {
        var context = await Send(UntrustedPeer, ForwardedFor);
        await Send(UntrustedPeer, ForwardedFor);
        await Send(UntrustedPeer, ForwardedFor);

        Warnings().Should().Be(1);
        _logs.Records.Should().ContainSingle(r => r.Message.Contains(UntrustedPeer));
        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse(UntrustedPeer));
    }

    [Fact]
    public async Task Each_distinct_untrusted_peer_is_warned()
    {
        await Send(UntrustedPeer, ForwardedFor);
        await Send("172.28.0.98", ForwardedFor);

        Warnings().Should().Be(2);
    }

    [Fact]
    public async Task Trusted_peer_sending_forwarded_for_is_not_warned()
    {
        var context = await Send(TrustedProxy, ForwardedFor);

        Warnings().Should().Be(0);
        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse(ForwardedFor));
    }

    [Fact]
    public async Task Peer_sending_no_forwarded_for_is_not_warned()
    {
        await Send(UntrustedPeer, forwardedFor: null);
        await Send(TrustedProxy, forwardedFor: null);

        Warnings().Should().Be(0);
    }

    private int Warnings() => _logs.CountStartingWith(WarningPrefix, LogLevel.Warning);

    private async Task<HttpContext> Send(string peer, string? forwardedFor)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(peer);
        if (forwardedFor is not null)
            context.Request.Headers[ForwardedHeadersDefaults.XForwardedForHeaderName] = forwardedFor;

        var forwardedHeaders = new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask, NullLoggerFactory.Instance, TrustedProxyOptions());

        await _sut.InvokeAsync(context, forwardedHeaders.Invoke);
        return context;
    }

    private static IOptions<ForwardedHeadersOptions> TrustedProxyOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:TrustedProxies:0"] = TrustedProxy,
            })
            .Build();

        return new ServiceCollection()
            .AddTrustedProxyForwardedHeaders(configuration)
            .BuildServiceProvider()
            .GetRequiredService<IOptions<ForwardedHeadersOptions>>();
    }
}
