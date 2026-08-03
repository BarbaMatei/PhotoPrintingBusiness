using System.Buffers.Binary;
using System.Diagnostics;
using OpenTelemetry.Trace;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.Observability.Sampling;

public sealed class RouteAwareSampler : Sampler
{
    private static readonly SamplingResult RecordAndSample = new(SamplingDecision.RecordAndSample);

    // Out-of-rate spans are recorded but left unexported rather than dropped: a dropped
    // span never reaches OnEnd, so an error discovered at span end could not be rescued.
    private static readonly SamplingResult ErrorsOnly = new(SamplingDecision.RecordOnly);

    private readonly double _defaultRate;
    private readonly IReadOnlyDictionary<string, double> _routeRates;

    public RouteAwareSampler(ObservabilitySamplingSettings settings)
    {
        _defaultRate = settings.Default;
        _routeRates  = settings.Routes ?? new Dictionary<string, double>();
        Description  = $"RouteAwareSampler{{default={_defaultRate:F3}, routes={_routeRates.Count}}}";
    }

    public override SamplingResult ShouldSample(in SamplingParameters parameters)
    {
        var route = ResolveRoute(parameters);
        var rate  = route is not null && _routeRates.TryGetValue(route, out var r)
            ? r
            : _defaultRate;

        if (rate >= 1.0) return RecordAndSample;
        if (rate <= 0.0) return ErrorsOnly;

        // Lower 63 bits of the trace_id, normalised to [0, 1). Deterministic by
        // trace_id — random sampling forbidden in this path.
        Span<byte> bytes = stackalloc byte[16];
        parameters.TraceId.CopyTo(bytes);
        var lower = BinaryPrimitives.ReadUInt64BigEndian(bytes[8..]);
        var ratio = (lower & 0x7FFFFFFFFFFFFFFFul) / (double)long.MaxValue;

        return ratio < rate ? RecordAndSample : ErrorsOnly;
    }

    private static string? ResolveRoute(in SamplingParameters parameters)
    {
        // The route template lands on the activity as the `http.route` tag,
        // typically populated by the ASP.NET Core instrumentation when the
        // endpoint resolves. For server-kind spans we can also fall back to
        // the activity name when the route hasn't been resolved yet.
        if (parameters.Tags is not null)
        {
            foreach (var tag in parameters.Tags)
            {
                if (tag.Key == "http.route" && tag.Value is string route && !string.IsNullOrEmpty(route))
                    return route;
            }
        }
        return parameters.Name;
    }
}
