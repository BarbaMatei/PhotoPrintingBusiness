using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using PhotoPrint.API.Observability;

namespace PhotoPrint.API.Middleware;

public sealed class UntrustedForwardedPeerMiddleware : IMiddleware
{
    private const int LoggedPeerCap = 512;

    private readonly ILogger<UntrustedForwardedPeerMiddleware> _logger;
    private readonly ConcurrentDictionary<IPAddress, byte> _loggedPeers = new();
    private int _loggedPeerCount;
    private int _capWarned;

    public UntrustedForwardedPeerMiddleware(ILogger<UntrustedForwardedPeerMiddleware> logger) =>
        _logger = logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var peer     = context.Connection.RemoteIpAddress;
        var declared = context.Request.Headers[ForwardedHeadersDefaults.XForwardedForHeaderName].Count > 0;

        await next(context);

        if (!declared || peer is null || !peer.Equals(context.Connection.RemoteIpAddress))
            return;

        LogUntrusted(peer);
    }

    private void LogUntrusted(IPAddress peer)
    {
        if (Volatile.Read(ref _loggedPeerCount) >= LoggedPeerCap)
        {
            if (Interlocked.Exchange(ref _capWarned, 1) == 0)
            {
                _logger.LogWarning(
                    "forwarded_headers.untrusted_peer.log_cap_reached distinct_ips={Cap} — further "
                    + "untrusted forwarding sources are not logged until restart",
                    LoggedPeerCap);
            }

            return;
        }

        var canonical = ScrapeIpAllowList.Canonicalize(peer);
        if (!_loggedPeers.TryAdd(canonical, 0))
            return;

        Interlocked.Increment(ref _loggedPeerCount);
        _logger.LogWarning(
            "forwarded_headers.untrusted_peer ip={Ip} — this peer sent X-Forwarded-For but is not in "
            + "ForwardedHeaders:TrustedProxies, so the header was ignored and the client identity is "
            + "the peer itself. If this is the reverse proxy, its address has drifted from the "
            + "configured one — see DEPLOYMENT.md §16",
            canonical);
    }
}
