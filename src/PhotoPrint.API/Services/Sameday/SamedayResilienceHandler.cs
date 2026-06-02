using Polly;

namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Wraps <see cref="HttpMessageHandler.SendAsync"/> in the Sameday retry
/// resilience pipeline. The pipeline is built once per handler instance —
/// <see cref="IHttpClientFactory"/> rebuilds the handler chain every
/// <c>HandlerLifetime</c> (default 2 min), which is acceptable since retry
/// state is per-call. 401 is explicitly outside this pipeline (ADR-014).
/// </summary>
public sealed class SamedayResilienceHandler : DelegatingHandler
{
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

    public SamedayResilienceHandler()
    {
        _pipeline = SamedayPolicies.BuildRetryPipeline();
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // ResiliencePipeline retries the lambda on transient failures only;
        // 401 short-circuits to the SamedayAuthHandler one frame outside.
        return await _pipeline.ExecuteAsync(
            async ct => await base.SendAsync(request, ct),
            cancellationToken);
    }
}
