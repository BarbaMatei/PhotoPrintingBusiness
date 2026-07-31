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
    private readonly ScrapeIpAllowList _allowed;
    private readonly int _scrapePort;
    private readonly ILogger<MetricsEndpointIpAllowListMiddleware> _logger;
    private readonly ConcurrentDictionary<IPAddress, byte> _loggedDenies = new();

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
        if (ip is null || !_allowed.Contains(ip))
        {
            if (ip is not null && _loggedDenies.TryAdd(ip, 0))
            {
                _logger.LogInformation(
                    "metrics.scrape.denied ip={Ip} — not in Observability:Metrics:AllowedScrapeIps",
                    ip);
            }
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await next(context);
    }
}
