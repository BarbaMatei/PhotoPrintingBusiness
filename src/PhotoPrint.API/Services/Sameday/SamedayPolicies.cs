using System.Net;
using System.Threading.RateLimiting;
using Polly;
using Polly.RateLimiting;
using Polly.Retry;

namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Resilience pipeline for the Sameday transport. Aligned with the project's
/// Polly v8 pattern (see <c>S3StorageService</c>).
///
/// <para>Pipeline order (outer → inner):</para>
/// <list type="number">
///   <item><b>Rate limiter</b> — bolt 037 caps outbound traffic at
///   <c>maxRequestsPerSecond</c> (default 5) using a sliding window. Pending
///   callers queue rather than fail; <c>SamedayResilienceHandler</c> is
///   responsible only for transient retry, not for shedding load.</item>
///   <item><b>Retry</b> — 3 attempts, exponential backoff 1 s / 4 s / 16 s.
///   Retries on 5xx, 408 RequestTimeout, 429 TooManyRequests, and
///   <see cref="HttpRequestException"/>. 401 deliberately NOT in the
///   retryable set (ADR-014).</item>
/// </list>
/// </summary>
public static class SamedayPolicies
{
    /// <summary>Build the retry-only pipeline used by bolt 036. Kept for tests
    /// that exercise the retry semantics without rate-limit interference.</summary>
    public static ResiliencePipeline<HttpResponseMessage> BuildRetryPipeline()
        => BuildPipeline(rateLimitPermitsPerSecond: int.MaxValue);

    /// <summary>Full pipeline (rate limit + retry). Bolt 037's
    /// high-frequency callers (`ShipmentTrackingJob` polling many orders per
    /// tick, `AwbDispatcher` draining a backlog) require the cap.</summary>
    public static ResiliencePipeline<HttpResponseMessage> BuildPipeline(int rateLimitPermitsPerSecond)
    {
        var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();

        // Skip the rate limiter when the caller explicitly opts out (legacy bolt-036 callers).
        if (rateLimitPermitsPerSecond != int.MaxValue)
        {
            builder.AddRateLimiter(new RateLimiterStrategyOptions
            {
                RateLimiter = args =>
                {
                    var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit          = Math.Max(1, rateLimitPermitsPerSecond),
                        Window               = TimeSpan.FromSeconds(1),
                        SegmentsPerWindow    = 4,
                        QueueLimit           = int.MaxValue,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    });
                    return limiter.AcquireAsync(1, args.Context.CancellationToken);
                },
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
                // NOTE: 401 deliberately NOT here — owned by SamedayAuthHandler (ADR-014).
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
