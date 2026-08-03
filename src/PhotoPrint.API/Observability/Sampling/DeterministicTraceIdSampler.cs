using System.Buffers.Binary;
using OpenTelemetry.Trace;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.Observability.Sampling;

public sealed class DeterministicTraceIdSampler : Sampler
{
    private static readonly SamplingResult RecordAndSample = new(SamplingDecision.RecordAndSample);

    // Out-of-rate spans are recorded but left unexported rather than dropped: a dropped
    // span never reaches OnEnd, so an error discovered at span end could not be rescued.
    private static readonly SamplingResult ErrorsOnly = new(SamplingDecision.RecordOnly);

    private readonly double _rate;

    public DeterministicTraceIdSampler(ObservabilitySamplingSettings settings)
    {
        _rate       = settings.Default;
        Description = $"DeterministicTraceIdSampler{{rate={_rate:F3}}}";
    }

    public override SamplingResult ShouldSample(in SamplingParameters parameters)
    {
        // Nothing route-shaped is available here: the sampler runs while the server span is
        // being created, before routing has matched an endpoint, and ASP.NET Core supplies
        // no tags at that point. Per-route rates belong to the collector.
        if (_rate >= 1.0) return RecordAndSample;
        if (_rate <= 0.0) return ErrorsOnly;

        // Lower 63 bits of the trace_id, normalised to [0, 1). Deterministic by
        // trace_id — random sampling forbidden in this path.
        Span<byte> bytes = stackalloc byte[16];
        parameters.TraceId.CopyTo(bytes);
        var lower = BinaryPrimitives.ReadUInt64BigEndian(bytes[8..]);
        var ratio = (lower & 0x7FFFFFFFFFFFFFFFul) / (double)long.MaxValue;

        return ratio < _rate ? RecordAndSample : ErrorsOnly;
    }
}
