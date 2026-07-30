using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.Middleware;

/// <summary>
/// Gates <c>GET /metrics</c> via a configured IP allow-list.
/// The endpoint deliberately does NOT participate in JWT bearer auth; the
/// scrape consumer is a Prometheus scraper, not a user, and network
/// identity is the right primitive (see the ADR for the reasoning and
/// alternatives considered).
///
/// Configuration: <c>Observability:Metrics:AllowedScrapeIps</c>.
///
/// Logging: a single Info entry per distinct denied IP per process. We
/// deliberately don't log every 403 — a port-scanner would otherwise fill
/// the logs.
/// </summary>
public sealed class MetricsEndpointIpAllowListMiddleware : IMiddleware
{
    private readonly HashSet<IPAddress> _allowed;
    private readonly ILogger<MetricsEndpointIpAllowListMiddleware> _logger;
    private readonly ConcurrentDictionary<IPAddress, byte> _loggedDenies = new();

    public MetricsEndpointIpAllowListMiddleware(
        IOptions<ObservabilitySettings> settings,
        ILogger<MetricsEndpointIpAllowListMiddleware> logger)
    {
        _logger  = logger;
        _allowed = (settings.Value.Metrics.AllowedScrapeIps ?? [])
            .Select(s => IPAddress.TryParse(s, out var ip) ? ip : null)
            .Where(ip => ip is not null)
            .Select(ip => ip!)
            .ToHashSet();
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
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
