using System.Net;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.RateLimiting;
using Polly.Retry;

namespace PhotoPrint.API.Services.Sameday;

// Resilience pipeline for the Sameday transport (Polly v8). Pipeline order,
// outer→inner: rate limiter (optional) → retry (3×, 1s/4s/16s; 5xx/408/429/
// HttpRequestException). 401 is deliberately outside this pipeline — owned by
// SamedayAuthHandler (the caller must not retry it).
public static class SamedayPolicies
{
    // One limiter is created per handler instance and shared across every call it
    // makes (a per-call limiter never throttles and leaks its replenishment timer).
    // The int.MaxValue sentinel opts out entirely (bolt-036 retry-only callers).
    public static RateLimiter? CreateRateLimiter(int permitsPerSecond)
    {
        if (permitsPerSecond == int.MaxValue) return null;
        return new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            PermitLimit          = Math.Max(1, permitsPerSecond),
            Window               = TimeSpan.FromSeconds(1),
            SegmentsPerWindow    = 4,
            QueueLimit           = int.MaxValue,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        });
    }

    public static ResiliencePipeline<HttpResponseMessage> BuildRetryPipeline()
        => BuildPipeline(rateLimiter: null);

    public static ResiliencePipeline<HttpResponseMessage> BuildPipeline(
        RateLimiter? rateLimiter, ILogger? logger = null)
    {
        var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();

        if (rateLimiter is not null)
        {
            builder.AddRateLimiter(new RateLimiterStrategyOptions
            {
                RateLimiter = args => rateLimiter.AcquireAsync(1, args.Context.CancellationToken),
            });
        }

        builder.AddRetry(BuildRetryOptions(logger));

        return builder.Build();
    }

    /// <summary>The transport retry strategy the pipeline is built from. Public so the backoff
    /// schedule is assertable without waiting out real delays.</summary>
    public static RetryStrategyOptions<HttpResponseMessage> BuildRetryOptions(ILogger? logger = null)
        => new()
        {
            MaxRetryAttempts = 3,
            UseJitter = false,
            // Backoff schedule is 1 s / 4 s / 16 s (base-4). Polly's Exponential backoff is base-2
            // (1/2/4), so the delays are produced explicitly.
            DelayGenerator = args =>
                ValueTask.FromResult<TimeSpan?>(TimeSpan.FromSeconds(Math.Pow(4, args.AttemptNumber))),
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .HandleResult(r => IsRetryableStatus(r.StatusCode)),
            OnRetry = args =>
            {
                logger?.LogWarning(
                    "sameday.transport.retry attempt={Attempt} delay={Delay}s outcome={Outcome}",
                    args.AttemptNumber + 1,
                    (int)args.RetryDelay.TotalSeconds,
                    args.Outcome.Exception?.GetType().Name
                        ?? ((int?)args.Outcome.Result?.StatusCode)?.ToString()
                        ?? "unknown");
                return default;
            },
        };

    internal static bool IsRetryableStatus(HttpStatusCode status)
    {
        if ((int)status >= 500 && (int)status < 600) return true;
        if (status == HttpStatusCode.RequestTimeout) return true;
        if (status == HttpStatusCode.TooManyRequests) return true;
        return false;
    }
}
