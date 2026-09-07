using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Observability;

namespace PhotoPrint.API.Middleware;

public sealed class UntrustedForwardedPeerMiddleware : IMiddleware
{
    private const int LoggedPeerCap = 512;

    private readonly ILogger<UntrustedForwardedPeerMiddleware> _logger;
    private readonly TrustedProxyList _trustedProxies;
    private readonly PeerBudget _untrusted   = new();
    private readonly PeerBudget _unparseable = new();

    public UntrustedForwardedPeerMiddleware(
        ILogger<UntrustedForwardedPeerMiddleware> logger,
        TrustedProxyList trustedProxies)
    {
        _logger         = logger;
        _trustedProxies = trustedProxies;
    }

    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        JudgeForwardedValue(context);

        return next(context);
    }

    private void JudgeForwardedValue(HttpContext context)
    {
        var peer = context.Connection.RemoteIpAddress;
        if (peer is null
            || context.Request.Headers[ForwardedHeadersDefaults.XForwardedForHeaderName].Count == 0)
        {
            return;
        }

        var canonical = ScrapeIpAllowList.Canonicalize(peer);

        if (!_trustedProxies.Trusts(peer))
        {
            switch (_untrusted.Admit(canonical))
            {
                case Verdict.Log:
                    _logger.LogWarning(
                        "forwarded_headers.untrusted_peer ip={Ip} — this peer sent X-Forwarded-For but "
                        + "is not in ForwardedHeaders:TrustedProxies, so the header was ignored and the "
                        + "client identity is the peer itself. If this is the reverse proxy, its address "
                        + "has drifted from the configured one — see DEPLOYMENT.md §16",
                        canonical);
                    break;
                case Verdict.CapReached:
                    _logger.LogWarning(
                        "forwarded_headers.untrusted_peer.log_cap_reached distinct_ips={Cap} — further "
                        + "untrusted forwarding sources are not logged until restart",
                        LoggedPeerCap);
                    break;
            }

            return;
        }

        if (RightmostEntryParses(context))
            return;

        switch (_unparseable.Admit(canonical))
        {
            case Verdict.Log:
                _logger.LogWarning(
                    "forwarded_headers.unparseable_forwarded_for ip={Ip} — this trusted proxy sent an "
                    + "X-Forwarded-For value whose rightmost entry is not an address, so the header was "
                    + "ignored and the client identity is the proxy itself. Fix the proxy's header "
                    + "configuration — see DEPLOYMENT.md §16",
                    canonical);
                break;
            case Verdict.CapReached:
                _logger.LogWarning(
                    "forwarded_headers.unparseable_forwarded_for.log_cap_reached distinct_ips={Cap} — "
                    + "further trusted proxies sending unusable values are not logged until restart",
                    LoggedPeerCap);
                break;
        }
    }

    private static bool RightmostEntryParses(HttpContext context)
    {
        var entries = context.Request.Headers.GetCommaSeparatedValues(
            ForwardedHeadersDefaults.XForwardedForHeaderName);

        return entries.Length > 0 && IPEndPoint.TryParse(entries[^1], out _);
    }

    private enum Verdict
    {
        Silent,
        Log,
        CapReached,
    }

    private sealed class PeerBudget
    {
        private readonly ConcurrentDictionary<IPAddress, byte> _seen = new();
        private int _count;
        private int _capWarned;

        public Verdict Admit(IPAddress peer)
        {
            if (Volatile.Read(ref _count) >= LoggedPeerCap)
                return Interlocked.Exchange(ref _capWarned, 1) == 0 ? Verdict.CapReached : Verdict.Silent;

            if (!_seen.TryAdd(peer, 0))
                return Verdict.Silent;

            Interlocked.Increment(ref _count);
            return Verdict.Log;
        }
    }
}
