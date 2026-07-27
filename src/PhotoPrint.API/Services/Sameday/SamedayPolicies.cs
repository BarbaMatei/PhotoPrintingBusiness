using System.Net;
using System.Threading.RateLimiting;
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

    public static ResiliencePipeline<HttpResponseMessage> BuildPipeline(RateLimiter? rateLimiter)
    {
        var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();

        if (rateLimiter is not null)
        {
            builder.AddRateLimiter(new RateLimiterStrategyOptions
            {
                RateLimiter = args => rateLimiter.AcquireAsync(1, args.Context.CancellationToken),
            });
        }

        builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromSeconds(1), // 1 s, 4 s, 16 s
            UseJitter = false,
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .HandleResult(r => IsRetryableStatus(r.StatusCode)),
        });

        return builder.Build();
    }

    private static bool IsRetryableStatus(HttpStatusCode status)
    {
        if ((int)status >= 500 && (int)status < 600) return true;
        if (status == HttpStatusCode.RequestTimeout) return true;
        if (status == HttpStatusCode.TooManyRequests) return true;
        return false;
    }
}
