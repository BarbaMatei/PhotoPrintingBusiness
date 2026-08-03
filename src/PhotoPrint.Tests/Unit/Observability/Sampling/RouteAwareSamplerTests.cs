using System.Diagnostics;
using FluentAssertions;
using OpenTelemetry.Trace;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Observability.Sampling;

namespace PhotoPrint.Tests.Unit.Observability.Sampling;

/// <summary>
/// Tests for <see cref="RouteAwareSampler"/>. The most important property is
/// that sampling is deterministic by trace_id — same trace_id +
/// same rate always yields the same decision. Random sampling is explicitly
/// forbidden in this path; if a future "simplification" introduces it, these
/// tests fail.
/// </summary>
public class RouteAwareSamplerTests
{
    private static SamplingParameters BuildParams(
        ActivityTraceId traceId, string route, ActivityKind kind = ActivityKind.Server)
    {
        return new SamplingParameters(
            parentContext: default,
            traceId:       traceId,
            name:          route,
            kind:          kind,
            tags:          new[] { new KeyValuePair<string, object?>("http.route", route) });
    }

    [Fact]
    public void Rate_one_always_samples()
    {
        var sut = new RouteAwareSampler(new ObservabilitySamplingSettings { Default = 1.0 });

        for (var i = 0; i < 100; i++)
        {
            var result = sut.ShouldSample(BuildParams(ActivityTraceId.CreateRandom(), "GET /api/x"));
            result.Decision.Should().Be(SamplingDecision.RecordAndSample);
        }
    }

    [Fact]
    public void Rate_zero_records_without_exporting_so_errors_stay_rescuable()
    {
        var sut = new RouteAwareSampler(new ObservabilitySamplingSettings { Default = 0.0 });

        for (var i = 0; i < 100; i++)
        {
            var result = sut.ShouldSample(BuildParams(ActivityTraceId.CreateRandom(), "GET /api/x"));
            result.Decision.Should().Be(SamplingDecision.RecordOnly);
        }
    }

    [Fact]
    public void Route_override_takes_precedence_over_default()
    {
        var sut = new RouteAwareSampler(new ObservabilitySamplingSettings
        {
            Default = 1.0,
            Routes  = new() { { "GET /api/hot", 0.0 } },
        });

        sut.ShouldSample(BuildParams(ActivityTraceId.CreateRandom(), "GET /api/hot"))
            .Decision.Should().Be(SamplingDecision.RecordOnly);
        sut.ShouldSample(BuildParams(ActivityTraceId.CreateRandom(), "GET /api/normal"))
            .Decision.Should().Be(SamplingDecision.RecordAndSample);
    }

    [Fact]
    public void Unknown_route_falls_back_to_default()
    {
        var sut = new RouteAwareSampler(new ObservabilitySamplingSettings
        {
            Default = 0.0,
            Routes  = new() { { "GET /api/known", 1.0 } },
        });

        sut.ShouldSample(BuildParams(ActivityTraceId.CreateRandom(), "GET /api/unknown"))
            .Decision.Should().Be(SamplingDecision.RecordOnly);
    }

    [Fact]
    public void Same_trace_id_same_rate_always_yields_same_decision()
    {
        // This is the invariant. If a future PR replaces the trace_id
        // hash with Random.NextDouble, this test fails on the second iteration.
        var sut = new RouteAwareSampler(new ObservabilitySamplingSettings { Default = 0.5 });
        var traceId = ActivityTraceId.CreateRandom();
        var route   = "GET /api/x";

        var first = sut.ShouldSample(BuildParams(traceId, route)).Decision;
        for (var i = 0; i < 1_000; i++)
        {
            sut.ShouldSample(BuildParams(traceId, route)).Decision.Should().Be(first);
        }
    }

    [Fact]
    public void Different_trace_ids_with_intermediate_rate_produce_a_mix()
    {
        // Sanity that the deterministic hash isn't degenerate: across many
        // different trace_ids at rate 0.5, we should see both decisions.
        var sut = new RouteAwareSampler(new ObservabilitySamplingSettings { Default = 0.5 });
        var route = "GET /api/x";

        var sampled = 0;
        var dropped = 0;
        for (var i = 0; i < 10_000; i++)
        {
            var d = sut.ShouldSample(BuildParams(ActivityTraceId.CreateRandom(), route)).Decision;
            if (d == SamplingDecision.RecordAndSample)   sampled++;
            else if (d == SamplingDecision.RecordOnly)   dropped++;
        }

        // Expected ~50/50 with binomial noise; budget ±10% for headroom.
        sampled.Should().BeInRange(4_000, 6_000);
        dropped.Should().BeInRange(4_000, 6_000);
    }

    [Fact]
    public void Description_includes_default_rate_and_route_count_for_startup_log()
    {
        var sut = new RouteAwareSampler(new ObservabilitySamplingSettings
        {
            Default = 0.05,
            Routes  = new() { { "GET /a", 1.0 }, { "GET /b", 0.5 } },
        });

        sut.Description.Should().Contain("0.050");
        sut.Description.Should().Contain("routes=2");
    }

    [Fact]
    public void Sampler_uses_route_template_not_resolved_url()
    {
        // The sampler reads `http.route` tag (template), not parameters.Name.
        // This prevents cardinality blowup from /api/orders/{guid} variations.
        var sut = new RouteAwareSampler(new ObservabilitySamplingSettings
        {
            Default = 0.0,
            Routes  = new() { { "GET /api/orders/{id}", 1.0 } },
        });

        var traceId = ActivityTraceId.CreateRandom();
        var p = new SamplingParameters(
            parentContext: default,
            traceId:       traceId,
            name:          "GET /api/orders/abc-123",                       // resolved URL
            kind:          ActivityKind.Server,
            tags:          new[] { new KeyValuePair<string, object?>("http.route", "GET /api/orders/{id}") });

        sut.ShouldSample(p).Decision.Should().Be(SamplingDecision.RecordAndSample);
    }
}
