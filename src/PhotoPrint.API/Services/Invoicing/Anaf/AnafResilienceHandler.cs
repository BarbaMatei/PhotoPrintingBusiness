using System.Net;
using Polly;
using Polly.Retry;

namespace PhotoPrint.API.Services.Invoicing.Anaf;

/// <summary>
/// Wraps <see cref="HttpMessageHandler.SendAsync"/> in a Polly v8 retry
/// pipeline for transient ANAF failures. Mirrors <c>SamedayResilienceHandler</c>'s
/// shape so the project keeps a single resilience pattern.
///
/// Retry set: 5xx, 408, 429, <see cref="HttpRequestException"/>.
/// **401 is NOT here** — that's owned by <see cref="AnafAuthHandler"/>.
///
/// 3 attempts, exponential backoff 1s / 2s / 4s.
/// </summary>
public sealed class AnafResilienceHandler : DelegatingHandler
{
    private static readonly ResiliencePipeline<HttpResponseMessage> Pipeline = Build();

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return await Pipeline.ExecuteAsync(
            async ct => await base.SendAsync(request, ct),
            cancellationToken);
    }

    private static ResiliencePipeline<HttpResponseMessage> Build()
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                BackoffType      = DelayBackoffType.Exponential,
                Delay            = TimeSpan.FromSeconds(1),
                UseJitter        = false,
                ShouldHandle     = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => IsRetryable(r.StatusCode)),
            })
            .Build();
    }

    private static bool IsRetryable(HttpStatusCode status)
    {
        if ((int)status >= 500 && (int)status < 600) return true;
        if (status == HttpStatusCode.RequestTimeout)  return true;
        if (status == HttpStatusCode.TooManyRequests) return true;
        return false;
    }
}
