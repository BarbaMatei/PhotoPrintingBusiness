using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Observability;

namespace PhotoPrint.API.Middleware;

// Two independent gates, because the peer address alone is not trustworthy behind a reverse
// proxy — every proxied request shares the proxy's IP, so an allow-listed proxy would open the
// endpoint to the whole internet. The scrape port is a listener the edge does not route.
// The endpoint deliberately does not participate in JWT bearer auth: the consumer is a scraper,
// not a user, so network identity is the primitive.
public sealed class MetricsEndpointIpAllowListMiddleware : IMiddleware
{
    // Denials are logged once per distinct peer, so a port scan costs one line each rather than
    // one per probe. The cap bounds what a scan can retain; a /24 of scanners still fits.
    private const int LoggedDenyCap = 512;

    private readonly ScrapeIpAllowList _allowed;
    private readonly int _scrapePort;
    private readonly ILogger<MetricsEndpointIpAllowListMiddleware> _logger;
    private readonly ConcurrentDictionary<IPAddress, byte> _loggedDenies = new();
    private int _loggedDenyCount;
    private int _capWarned;

    public MetricsEndpointIpAllowListMiddleware(
        IOptions<ObservabilitySettings> settings,
        ILogger<MetricsEndpointIpAllowListMiddleware> logger)
    {
        _logger     = logger;
        _scrapePort = settings.Value.Metrics.ScrapePort;
        // Parse errors are the validator's job — it fails boot on them, so they cannot reach here.
        _allowed    = ScrapeIpAllowList.Parse(settings.Value.Metrics.AllowedScrapeIps, out _);
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (_scrapePort != 0 && context.Connection.LocalPort != _scrapePort)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var ip = context.Connection.RemoteIpAddress;
        if (!_allowed.Contains(ip))
        {
            if (ip is not null)
                LogDenied(ip);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await next(context);
    }

    private void LogDenied(IPAddress ip)
    {
        if (Volatile.Read(ref _loggedDenyCount) >= LoggedDenyCap)
        {
            if (Interlocked.Exchange(ref _capWarned, 1) == 0)
            {
                _logger.LogWarning(
                    "metrics.scrape.denied.log_cap_reached distinct_ips={Cap} — further denied "
                    + "scrape sources are not logged until restart",
                    LoggedDenyCap);
            }
            return;
        }

        // Log the canonical form: a mapped or scoped peer would otherwise read as a different
        // address from the one the allow-list was compared against.
        var peer = ScrapeIpAllowList.Canonicalize(ip);
        if (!_loggedDenies.TryAdd(peer, 0))
            return;

        Interlocked.Increment(ref _loggedDenyCount);
        _logger.LogInformation(
            "metrics.scrape.denied ip={Ip} — not in Observability:Metrics:AllowedScrapeIps",
            peer);
    }
}
