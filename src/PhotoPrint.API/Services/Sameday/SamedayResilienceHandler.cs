using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using Polly;

namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Wraps <see cref="HttpMessageHandler.SendAsync"/> in the Sameday resilience
/// pipeline (rate limit + retry). The pipeline is built once per handler
/// instance — <see cref="IHttpClientFactory"/> rebuilds the handler chain
/// every <c>HandlerLifetime</c> (default 2 min), which is acceptable since
/// retry / rate-limit state is per-call. 401 is explicitly outside this
/// pipeline.
///
/// <para>The rate-limit ceiling is taken from
/// <c>Sameday:Jobs:MaxConcurrentSamedayCalls</c> when the bolt-037 jobs
/// are enabled. When the jobs are off, the handler is still installed but
/// the configured limit (default 5 req/s) still applies — it's safe to
/// over-throttle a low-volume bolt-036-only flow.</para>
/// </summary>
public sealed class SamedayResilienceHandler : DelegatingHandler
{
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;
    private readonly System.Threading.RateLimiting.RateLimiter? _rateLimiter;

    public SamedayResilienceHandler(IOptions<SamedaySettings> settings, ILogger<SamedayResilienceHandler> logger)
    {
        var jobs = settings.Value.Jobs;
        // Concurrency gate and request rate are distinct concerns; MaxRequestsPerSecond decouples
        // them (falling back to the concurrency ceiling when unset, preserving prior behaviour).
        var rate = jobs.MaxRequestsPerSecond ?? jobs.MaxConcurrentSamedayCalls;
        _rateLimiter = SamedayPolicies.CreateRateLimiter(rate > 0 ? rate : 5);
        _pipeline = SamedayPolicies.BuildPipeline(_rateLimiter, logger);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return await _pipeline.ExecuteAsync(
            async ct => await base.SendAsync(request, ct),
            cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _rateLimiter?.Dispose();
        base.Dispose(disposing);
    }
}
