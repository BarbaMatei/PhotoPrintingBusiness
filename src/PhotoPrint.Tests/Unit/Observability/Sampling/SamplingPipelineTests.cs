using System.Diagnostics;
using FluentAssertions;
using OpenTelemetry;
using OpenTelemetry.Trace;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Observability;
using PhotoPrint.API.Observability.Sampling;

namespace PhotoPrint.Tests.Unit.Observability.Sampling;

/// <summary>
/// Runs the real OTel SDK — real sampler, real processors, real export processor —
/// because the defect these cover lives in the SDK's contract, not in our arithmetic:
/// a span the sampler drops never reaches <c>OnEnd</c>, so a processor cannot rescue
/// it. Calling the sampler directly cannot see that.
///
/// Each pipeline listens to a unique <see cref="ActivitySource"/>: an
/// <c>ActivitySource</c> is sampled by every listener in the process at once, so a
/// shared source would let another test's provider decide this one's spans.
/// </summary>
public class SamplingPipelineTests : IDisposable
{
    private readonly List<TracerProvider> _providers = [];

    private (ActivitySource Source, CollectingSpanExporter Exporter) Pipeline(double rate)
    {
        var name     = $"SamplingPipelineTests.{Guid.NewGuid():N}";
        var exporter = new CollectingSpanExporter();

        _providers.Add(Sdk.CreateTracerProviderBuilder()
            .AddSource(name)
            .SetSampler(new ParentBasedSampler(
                new DeterministicTraceIdSampler(new ObservabilitySamplingSettings { Default = rate })))
            .AddProcessor(new ErrorOverrideProcessor())
            .AddProcessor(new SimpleActivityExportProcessor(exporter))
            .Build()!);

        return (new ActivitySource(name), exporter);
    }

    [Fact]
    public void An_errored_span_is_exported_at_a_rate_that_keeps_nothing()
    {
        var (source, exporter) = Pipeline(rate: 0.0);

        using (var span = source.StartActivity("GET /api/orders", ActivityKind.Server))
        {
            span.Should().NotBeNull("an out-of-rate span must stay alive until its outcome is known");
            span!.SetStatus(ActivityStatusCode.Error);
        }

        exporter.Spans.Should().ContainSingle();
        exporter.Spans[0].Status.Should().Be(ActivityStatusCode.Error);
        exporter.Spans[0].Promoted.Should().BeTrue();
    }

    [Fact]
    public void A_healthy_span_is_not_exported_at_a_rate_that_keeps_nothing()
    {
        var (source, exporter) = Pipeline(rate: 0.0);

        using (source.StartActivity("GET /api/orders", ActivityKind.Server)) { }

        exporter.Spans.Should().BeEmpty();
    }

    [Fact]
    public void A_sampled_in_span_is_exported_and_is_not_marked_promoted()
    {
        var (source, exporter) = Pipeline(rate: 1.0);

        using (source.StartActivity("GET /api/orders", ActivityKind.Server)) { }

        exporter.Spans.Should().ContainSingle();
        exporter.Spans[0].Promoted.Should().BeFalse();
    }

    [Fact]
    public void Children_of_an_out_of_rate_span_are_never_created()
    {
        var (source, exporter) = Pipeline(rate: 0.0);

        using (var parent = source.StartActivity("GET /api/orders", ActivityKind.Server))
        {
            using var child = source.StartActivity("db.query", ActivityKind.Client);
            child.Should().BeNull("this is why a promoted error span arrives with no children");
            parent!.SetStatus(ActivityStatusCode.Error);
        }

        exporter.Spans.Should().ContainSingle();
    }

    public void Dispose()
    {
        foreach (var provider in _providers) provider.Dispose();
        GC.SuppressFinalize(this);
    }
}

internal sealed record CapturedSpan(string DisplayName, ActivityStatusCode Status, bool Promoted);

internal sealed class CollectingSpanExporter : BaseExporter<Activity>
{
    private readonly List<CapturedSpan> _spans = [];

    public IReadOnlyList<CapturedSpan> Spans => _spans;

    public override ExportResult Export(in Batch<Activity> batch)
    {
        foreach (var activity in batch)
            _spans.Add(new CapturedSpan(
                activity.DisplayName,
                activity.Status,
                activity.GetTagItem(ErrorOverrideProcessor.PromotedTag) is true));

        return ExportResult.Success;
    }
}
