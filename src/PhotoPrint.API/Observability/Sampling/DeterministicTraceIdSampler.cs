using System.Buffers.Binary;
using System.Diagnostics;
using OpenTelemetry.Trace;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.Observability.Sampling;

public sealed class DeterministicTraceIdSampler : Sampler
{
    private static readonly SamplingResult RecordAndSample = new(SamplingDecision.RecordAndSample);
    private static readonly SamplingResult ErrorsOnly      = new(SamplingDecision.RecordOnly);
    private static readonly SamplingResult Drop            = new(SamplingDecision.Drop);

    private readonly double _rate;

    public DeterministicTraceIdSampler(ObservabilitySamplingSettings settings)
    {
        _rate       = settings.Default;
        Description = $"DeterministicTraceIdSampler{{rate={_rate:F3}}}";
    }

    // No route is available here — the sampler runs while the server span is created, before
    // routing matches an endpoint, and ASP.NET Core passes no tags at all.
    public override SamplingResult ShouldSample(in SamplingParameters parameters)
    {
        if (_rate >= 1.0) return RecordAndSample;

        if (_rate > 0.0)
        {
            // Lower 63 bits of the trace_id, normalised to [0, 1). Deterministic by
            // trace_id — random sampling forbidden in this path.
            Span<byte> bytes = stackalloc byte[16];
            parameters.TraceId.CopyTo(bytes);
            var lower = BinaryPrimitives.ReadUInt64BigEndian(bytes[8..]);
            var ratio = (lower & 0x7FFFFFFFFFFFFFFFul) / (double)long.MaxValue;

            if (ratio < _rate) return RecordAndSample;
        }

        // Held, not dropped, so an error found at span end can still export — a dropped span
        // never reaches OnEnd. Background roots stay dropped: their EF spans carry SQL text.
        return parameters.Kind == ActivityKind.Server ? ErrorsOnly : Drop;
    }
}
