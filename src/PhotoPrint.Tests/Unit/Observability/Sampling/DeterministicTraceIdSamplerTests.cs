using System.Diagnostics;
using FluentAssertions;
using OpenTelemetry.Trace;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Observability.Sampling;

namespace PhotoPrint.Tests.Unit.Observability.Sampling;

// Hand-built parameters can only pin the decision function; what the sampler is actually
// handed at span start is pinned in SamplingPipelineTests against the real SDK.
public class DeterministicTraceIdSamplerTests
{
    private static SamplingParameters BuildParams(
        ActivityTraceId traceId, ActivityKind kind = ActivityKind.Server) =>
        new(parentContext: default,
            traceId:       traceId,
            name:          "Microsoft.AspNetCore.Hosting.HttpRequestIn",
            kind:          kind,
            tags:          null);

    [Fact]
    public void Rate_one_always_samples()
    {
        var sut = new DeterministicTraceIdSampler(new ObservabilitySamplingSettings { Default = 1.0 });

        for (var i = 0; i < 100; i++)
        {
            var result = sut.ShouldSample(BuildParams(ActivityTraceId.CreateRandom()));
            result.Decision.Should().Be(SamplingDecision.RecordAndSample);
        }
    }

    [Fact]
    public void Rate_zero_records_without_exporting_so_errors_stay_rescuable()
    {
        var sut = new DeterministicTraceIdSampler(new ObservabilitySamplingSettings { Default = 0.0 });

        for (var i = 0; i < 100; i++)
        {
            var result = sut.ShouldSample(BuildParams(ActivityTraceId.CreateRandom()));
            result.Decision.Should().Be(SamplingDecision.RecordOnly);
        }
    }

    [Theory]
    [InlineData(ActivityKind.Client)]
    [InlineData(ActivityKind.Internal)]
    [InlineData(ActivityKind.Consumer)]
    public void Out_of_rate_roots_that_are_not_inbound_requests_are_dropped(ActivityKind kind)
    {
        var sut = new DeterministicTraceIdSampler(new ObservabilitySamplingSettings { Default = 0.0 });

        sut.ShouldSample(BuildParams(ActivityTraceId.CreateRandom(), kind))
            .Decision.Should().Be(SamplingDecision.Drop);
    }

    [Fact]
    public void Same_trace_id_same_rate_always_yields_same_decision()
    {
        // This is the invariant. If a future PR replaces the trace_id
        // hash with Random.NextDouble, this test fails on the second iteration.
        var sut     = new DeterministicTraceIdSampler(new ObservabilitySamplingSettings { Default = 0.5 });
        var traceId = ActivityTraceId.CreateRandom();

        var first = sut.ShouldSample(BuildParams(traceId)).Decision;
        for (var i = 0; i < 1_000; i++)
        {
            sut.ShouldSample(BuildParams(traceId)).Decision.Should().Be(first);
        }
    }

    [Fact]
    public void Different_trace_ids_with_intermediate_rate_produce_a_mix()
    {
        // Sanity that the deterministic hash isn't degenerate: across many
        // different trace_ids at rate 0.5, we should see both decisions.
        var sut = new DeterministicTraceIdSampler(new ObservabilitySamplingSettings { Default = 0.5 });

        var sampled = 0;
        var held    = 0;
        for (var i = 0; i < 10_000; i++)
        {
            var d = sut.ShouldSample(BuildParams(ActivityTraceId.CreateRandom())).Decision;
            if (d == SamplingDecision.RecordAndSample) sampled++;
            else if (d == SamplingDecision.RecordOnly) held++;
        }

        // Expected ~50/50 with binomial noise; budget ±10% for headroom.
        sampled.Should().BeInRange(4_000, 6_000);
        held.Should().BeInRange(4_000, 6_000);
    }

    [Fact]
    public void Description_includes_the_rate_for_the_startup_log()
    {
        var sut = new DeterministicTraceIdSampler(new ObservabilitySamplingSettings { Default = 0.05 });

        sut.Description.Should().Contain("0.050");
    }

    [Fact]
    public void Nothing_about_the_span_identity_can_steer_the_rate()
    {
        var sut = new DeterministicTraceIdSampler(new ObservabilitySamplingSettings { Default = 0.0 });

        var withRouteTag = new SamplingParameters(
            parentContext: default,
            traceId:       ActivityTraceId.CreateRandom(),
            name:          "GET /api/products",
            kind:          ActivityKind.Server,
            tags:          [new KeyValuePair<string, object?>("http.route", "api/products")]);

        sut.ShouldSample(withRouteTag).Decision.Should().Be(SamplingDecision.RecordOnly);
    }
}
