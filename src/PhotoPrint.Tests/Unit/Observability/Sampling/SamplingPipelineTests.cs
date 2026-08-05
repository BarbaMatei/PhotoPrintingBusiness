using System.Diagnostics;
using FluentAssertions;
using OpenTelemetry;
using OpenTelemetry.Trace;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Extensions;
using PhotoPrint.API.Observability;
using PhotoPrint.API.Observability.Sampling;

namespace PhotoPrint.Tests.Unit.Observability.Sampling;

// What a sampling decision costs and exports is an SDK contract, so these run the real
// SDK. Each pipeline needs a unique ActivitySource: every listener in the process samples
// a source, so a shared one lets another test's provider decide these spans.
public class SamplingPipelineTests : IDisposable
{
    private readonly List<TracerProvider> _providers = [];

    private (ActivitySource Source, CollectingSpanExporter Exporter) Pipeline(double rate)
    {
        var name     = $"SamplingPipelineTests.{Guid.NewGuid():N}";
        var exporter = new CollectingSpanExporter();

        _providers.Add(Sdk.CreateTracerProviderBuilder()
            .AddSource(name)
            .SetSampler(ObservabilityExtensions.BuildSampler(
                new ObservabilitySamplingSettings { Default = rate }))
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
    public void An_errored_background_root_span_is_dropped_rather_than_held()
    {
        // Every EF command a background job issues is a root span of its own. Holding those
        // would record their SQL text for spans no rate will ever export.
        var (source, exporter) = Pipeline(rate: 0.0);

        using (var span = source.StartActivity("db.query", ActivityKind.Client))
        {
            // The SDK still creates a root activity to carry trace context, but nothing is
            // collected on it — which is what keeps SQL text out of memory.
            span!.IsAllDataRequested.Should().BeFalse();
            span.SetStatus(ActivityStatusCode.Error);
        }

        exporter.Spans.Should().BeEmpty();
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

    // ── A caller's traceparent must not decide our sampling ───────────────────

    private static ActivityContext RemoteParent(ActivityTraceFlags flags) =>
        new(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), flags, isRemote: true);

    [Fact]
    public void An_inbound_unsampled_traceparent_cannot_suppress_a_span_we_would_keep()
    {
        var (source, exporter) = Pipeline(rate: 1.0);

        using (source.StartActivity(
            "GET /api/orders", ActivityKind.Server, RemoteParent(ActivityTraceFlags.None))) { }

        exporter.Spans.Should().ContainSingle("our configured rate decides, not the caller's flag");
    }

    [Fact]
    public void An_inbound_sampled_traceparent_cannot_force_a_span_we_would_drop()
    {
        var (source, exporter) = Pipeline(rate: 0.0);

        using (source.StartActivity(
            "GET /api/orders", ActivityKind.Server, RemoteParent(ActivityTraceFlags.Recorded))) { }

        exporter.Spans.Should().BeEmpty("a caller must not be able to buy full tracing past our rate");
    }

    [Fact]
    public void An_errored_span_under_an_unsampled_traceparent_is_still_promoted()
    {
        // The finding's payload: with the caller's flag honoured, a 500 on this request was
        // invisible at every rate because the span was dropped before OnEnd could run.
        var (source, exporter) = Pipeline(rate: 0.0);

        using (var span = source.StartActivity(
            "GET /api/orders", ActivityKind.Server, RemoteParent(ActivityTraceFlags.None)))
        {
            span.Should().NotBeNull();
            span!.SetStatus(ActivityStatusCode.Error);
        }

        exporter.Spans.Should().ContainSingle();
        exporter.Spans[0].Promoted.Should().BeTrue();
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
