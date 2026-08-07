using System.Diagnostics;
using FluentAssertions;
using PhotoPrint.API.Observability;

namespace PhotoPrint.Tests.Unit.Observability;

public class ErrorOverrideProcessorTests
{
    private readonly ErrorOverrideProcessor _sut = new();

    private static Activity Span(ActivityStatusCode status, bool recorded)
    {
        var activity = new Activity("span").SetStatus(status);
        if (recorded) activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
        return activity;
    }

    [Fact]
    public void An_errored_unrecorded_span_is_promoted_and_marked()
    {
        var span = Span(ActivityStatusCode.Error, recorded: false);

        _sut.OnEnd(span);

        span.Recorded.Should().BeTrue();
        span.GetTagItem(ErrorOverrideProcessor.PromotedTag).Should().Be(true);
    }

    [Theory]
    [InlineData(ActivityStatusCode.Unset)]
    [InlineData(ActivityStatusCode.Ok)]
    public void A_healthy_unrecorded_span_stays_unrecorded(ActivityStatusCode status)
    {
        var span = Span(status, recorded: false);

        _sut.OnEnd(span);

        span.Recorded.Should().BeFalse();
        span.GetTagItem(ErrorOverrideProcessor.PromotedTag).Should().BeNull();
    }

    [Fact]
    public void An_errored_span_the_sampler_already_kept_is_not_marked_as_promoted()
    {
        var span = Span(ActivityStatusCode.Error, recorded: true);

        _sut.OnEnd(span);

        span.Recorded.Should().BeTrue();
        span.GetTagItem(ErrorOverrideProcessor.PromotedTag).Should().BeNull(
            "the tag exists to explain a missing child trace, which only a promotion causes");
    }
}
