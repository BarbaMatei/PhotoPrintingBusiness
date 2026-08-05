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
    // FotoMetrics.Meter is one process-wide static, so a MeterListener sees every test's
    // measurements. The execution context is what separates them: it flows into the code this
    // test calls and into nothing another test calls.
    private static readonly AsyncLocal<Guid?> Active = new();

    private readonly MeterListener _listener = new();
    private readonly ConcurrentQueue<MetricMeasurement> _measurements = new();
    private readonly Guid _token = Guid.NewGuid();
    private readonly Guid? _outer;

    public MetricCapture(params string[] instrumentNames)
    {
        var wanted = new HashSet<string>(instrumentNames, StringComparer.Ordinal);
        var meter = FotoMetrics.Meter;

        _outer = Active.Value;
        Active.Value = _token;

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

    /// <summary>
    /// Every way the captured observations breach <see cref="MetricNames.LabelContract"/>:
    /// an undeclared instrument, a tag the instrument does not declare, a declared tag that
    /// is missing, or a value outside the enumerated set. Empty means the emissions are
    /// within the cardinality budget the contract describes.
    /// </summary>
    public IReadOnlyList<string> ContractViolations()
    {
        var violations = new List<string>();

        foreach (var m in Measurements)
        {
            if (!MetricNames.LabelContract.TryGetValue(m.Instrument, out var declared))
            {
                violations.Add($"{m.Instrument}: not declared in MetricNames.LabelContract");
                continue;
            }

            foreach (var (key, value) in m.Tags)
            {
                if (!declared.TryGetValue(key, out var allowed))
                {
                    violations.Add($"{m.Instrument}: undeclared label '{key}'");
                    continue;
                }
                if (!allowed.Contains(value, StringComparer.Ordinal))
                    violations.Add(
                        $"{m.Instrument}.{key}: value '{value}' is outside the enumerated set");
            }

            foreach (var key in declared.Keys.Where(k => !m.Tags.ContainsKey(k)))
                violations.Add($"{m.Instrument}: declared label '{key}' was not emitted");
        }

        return violations;
    }

    public void Dispose()
    {
        Active.Value = _outer;
        _listener.Dispose();
    }

    private void Capture(
        Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (Active.Value != _token)
            return;

        var map = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var tag in tags)
            map[tag.Key] = tag.Value?.ToString();

        _measurements.Enqueue(new MetricMeasurement(instrument.Name, value, map));
    }
}
