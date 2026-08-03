using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using PhotoPrint.API.Observability;

namespace PhotoPrint.Tests.Helpers;

public sealed record MetricMeasurement(
    string Instrument,
    double Value,
    IReadOnlyDictionary<string, string?> Tags);

public sealed class MetricCapture : IDisposable
{
    private readonly MeterListener _listener = new();
    private readonly ConcurrentQueue<MetricMeasurement> _measurements = new();

    public MetricCapture(params string[] instrumentNames)
    {
        var wanted = new HashSet<string>(instrumentNames, StringComparer.Ordinal);
        var meter = FotoMetrics.Meter;

        // FotoMetrics.Meter is a process-wide static and xUnit runs test classes in parallel,
        // so match the meter instance too — a same-named instrument from elsewhere is not ours.
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (ReferenceEquals(instrument.Meter, meter) && wanted.Contains(instrument.Name))
                listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<long>((i, v, tags, _) => Capture(i, v, tags));
        _listener.SetMeasurementEventCallback<double>((i, v, tags, _) => Capture(i, v, tags));
        _listener.Start();
    }

    public IReadOnlyList<MetricMeasurement> Measurements => [.. _measurements];

    public IReadOnlyList<MetricMeasurement> For(
        string instrument, params (string Key, string Value)[] tags) =>
        [.. _measurements.Where(m =>
            m.Instrument == instrument &&
            tags.All(t => m.Tags.TryGetValue(t.Key, out var v) && v == t.Value))];

    public void Dispose() => _listener.Dispose();

    private void Capture(
        Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var map = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var tag in tags)
            map[tag.Key] = tag.Value?.ToString();

        _measurements.Enqueue(new MetricMeasurement(instrument.Name, value, map));
    }
}
