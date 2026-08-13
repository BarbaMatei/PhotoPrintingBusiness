using System.Diagnostics;
using OpenTelemetry;

namespace PhotoPrint.API.Observability;

public sealed class ErrorOverrideProcessor : BaseProcessor<Activity>
{
    public const string PromotedTag = "fototipar.sampling.error_override";

    // Reached only for spans the sampler held: the SDK skips OnEnd for dropped ones.
    public override void OnEnd(Activity data)
    {
        if (data.Status != ActivityStatusCode.Error || data.Recorded)
            return;

        // The tag says why a promoted trace has no children: they were dropped at start.
        data.SetTag(PromotedTag, true);
        data.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
    }
}
