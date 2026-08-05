using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.Observability;

public static class ScrapeListenerCheck
{
    // Kestrel reports display names post-bind (http://[::]:8080) but a TestServer host reports the
    // raw wildcard forms, and Uri rejects both '+' and '*' while silently giving unix sockets port 80.
    public static string? Verdict(IReadOnlyCollection<string> boundAddresses, int scrapePort)
    {
        if (scrapePort == 0)
            return null;

        var ports = new HashSet<int>();
        foreach (var address in boundAddresses)
        {
            try
            {
                ports.Add(BindingAddress.Parse(address).Port);
            }
            catch (FormatException)
            {
                // A socket or pipe address carries no port and cannot serve the scrape listener.
            }
        }

        // A host that reports nothing bound is not serving sockets at all (TestServer), so there
        // is no listener topology to be wrong about.
        if (ports.Count == 0)
            return null;

        if (!ports.Contains(scrapePort))
        {
            return $"Observability:Metrics:ScrapePort={scrapePort} is not a port this process "
                 + $"listens on (bound: {Describe(ports)}). Every scrape would get 404. Add "
                 + $"http://+:{scrapePort} to ASPNETCORE_URLS, or set ScrapePort to one of the bound ports.";
        }

        if (ports.Count == 1)
        {
            return $"Observability:Metrics:ScrapePort={scrapePort} is the only port this process "
                 + "listens on, so /metrics is served on the same listener the reverse proxy talks "
                 + "to and the scrape-port gate protects nothing. Bind a second port for the API "
                 + "and keep ScrapePort off the proxied one, or set ScrapePort to 0 and gate at the edge.";
        }

        return null;
    }

    private static string Describe(IEnumerable<int> ports) => string.Join(", ", ports.Order());
}

internal sealed class ScrapeListenerGuard : IHostedLifecycleService
{
    private readonly IServer _server;
    private readonly ObservabilitySettings _settings;
    private readonly ILogger<ScrapeListenerGuard> _logger;

    public ScrapeListenerGuard(
        IServer server,
        IOptions<ObservabilitySettings> settings,
        ILogger<ScrapeListenerGuard> logger)
    {
        _server = server;
        _settings = settings.Value;
        _logger = logger;
    }

    // StartedAsync is the first point where Kestrel has bound; a throw from ApplicationStarted is
    // swallowed by the host, and StartAsync runs before the web host service binds anything.
    public Task StartedAsync(CancellationToken cancellationToken)
    {
        var addresses = _server.Features.Get<IServerAddressesFeature>()?.Addresses ?? [];
        var verdict = ScrapeListenerCheck.Verdict([.. addresses], _settings.Metrics.ScrapePort);

        if (verdict is null)
            return Task.CompletedTask;

        _logger.LogCritical("observability.metrics.scrape_listener_invalid {Reason}", verdict);
        throw new InvalidOperationException(verdict);
    }

    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
