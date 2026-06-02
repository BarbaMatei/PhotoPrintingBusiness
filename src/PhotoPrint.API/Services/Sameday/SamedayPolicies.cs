using System.Net;
using Polly;
using Polly.Retry;

namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Resilience pipelines for the Sameday transport. Aligned with the project's
/// Polly v8 pattern (see <c>S3StorageService</c>).
///
/// Scope note: this bolt (036) only retries — the 5 req/s rate-limit ceiling
/// originally specified in the technical design is moved to bolt 037, where
/// high-frequency callers (the tracking-poll job) actually appear. Bolt 036's
/// sole outbound call (<c>AuthenticateAsync</c>) is already serialized by
/// <see cref="SamedayTokenProvider"/>'s <see cref="SemaphoreSlim"/>, so a
/// separate rate-limit would add no value here.
///
/// 401 is NOT in the retryable set (ADR-014): session-expiry retries live in
/// <c>SamedayAuthHandler</c>, outside this pipeline.
/// </summary>
public static class SamedayPolicies
{
    /// <summary>3 attempts, exponential backoff 1 s / 4 s / 16 s. Retries on
    /// 5xx, 408 RequestTimeout, 429 TooManyRequests, and on transport-level
    /// <see cref="HttpRequestException"/> (DNS / TCP / TLS errors). Does NOT
    /// retry on 401.</summary>
    public static ResiliencePipeline<HttpResponseMessage> BuildRetryPipeline()
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1), // 1 s, 4 s, 16 s
                UseJitter = false,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    // NOTE: 401 deliberately NOT here — owned by SamedayAuthHandler (ADR-014).
                    .HandleResult(r => IsRetryableStatus(r.StatusCode)),
            })
            .Build();
    }

    private static bool IsRetryableStatus(HttpStatusCode status)
    {
        if ((int)status >= 500 && (int)status < 600) return true;
        if (status == HttpStatusCode.RequestTimeout) return true;
        if (status == HttpStatusCode.TooManyRequests) return true;
        return false;
    }
}
