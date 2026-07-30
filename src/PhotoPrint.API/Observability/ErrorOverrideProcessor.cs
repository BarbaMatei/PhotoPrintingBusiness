using System.Diagnostics;
using OpenTelemetry;

namespace PhotoPrint.API.Observability;

/// <summary>
/// Span processor that promotes errored spans to "recorded" regardless of
/// the sampler's earlier decision. Implements the "errors are always
/// sampled" invariant and the per-route sampling story.
///
/// The OTel sampler runs at span start, before the request outcome is
/// known. A 5xx response or an unhandled exception only surfaces at span
/// end. This processor's <c>OnEnd</c> hook detects that case and forces
/// the recorded flag on so the span is exported.
/// </summary>
public sealed class ErrorOverrideProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity data)
    {
        if (data.Status == ActivityStatusCode.Error)
            data.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
    }
}
