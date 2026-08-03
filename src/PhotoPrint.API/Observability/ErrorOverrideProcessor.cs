using System.Diagnostics;
using OpenTelemetry;

namespace PhotoPrint.API.Observability;

public sealed class ErrorOverrideProcessor : BaseProcessor<Activity>
{
    public const string PromotedTag = "fototipar.sampling.error_override";

    public override void OnEnd(Activity data)
    {
        // Only reachable because the sampler returns RecordOnly rather than Drop —
        // the SDK never calls OnEnd for a span it dropped at start.
        if (data.Status != ActivityStatusCode.Error || data.Recorded)
            return;

        // Children of a non-recorded parent are dropped at start, so a promoted span
        // arrives alone; the tag says why the trace has no children.
        data.SetTag(PromotedTag, true);
        data.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
    }
}
