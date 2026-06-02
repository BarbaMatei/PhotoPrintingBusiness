using FluentAssertions;
using PhotoPrint.API.Observability;

namespace PhotoPrint.Tests.Unit.Observability;

/// <summary>
/// Cardinality budget: each labelled instrument is capped at ≤ 100 distinct
/// series (per the technical design's NFR section). The budget exists to
/// catch free-form label leaks — a single counter with a user-id label would
/// blow this immediately.
///
/// The series count is the product of the cardinalities of each label's
/// enumerated value set, listed in <see cref="MetricNames"/>.
/// </summary>
public class MetricsCardinalityTests
{
    [Fact]
    public void Orders_created_total_cardinality_is_bounded()
    {
        var series = MetricNames.ProcessorValues.All.Length
                   * MetricNames.OrderStatusValues.All.Length;

        series.Should().Be(6);
        series.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public void Payment_webhook_total_cardinality_is_bounded()
    {
        var series = MetricNames.ProcessorValues.All.Length
                   * MetricNames.WebhookResultValues.All.Length;

        series.Should().Be(12);
        series.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public void Awb_creation_total_cardinality_is_bounded()
    {
        var series = MetricNames.AwbResultValues.All.Length;

        series.Should().Be(4);
        series.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public void Invoice_anaf_status_total_cardinality_is_bounded()
    {
        var series = MetricNames.AnafStatusValues.All.Length;

        series.Should().Be(4);
        series.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public void Label_value_enumerations_have_no_duplicates()
    {
        // A duplicate constant value across two label-value enumerations is
        // confusing in dashboards and may indicate a copy-paste error.
        MetricNames.ProcessorValues.All.Should().OnlyHaveUniqueItems();
        MetricNames.OrderStatusValues.All.Should().OnlyHaveUniqueItems();
        MetricNames.WebhookResultValues.All.Should().OnlyHaveUniqueItems();
        MetricNames.AwbResultValues.All.Should().OnlyHaveUniqueItems();
        MetricNames.AnafStatusValues.All.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Label_value_constants_are_snake_case_or_simple_words()
    {
        // Prometheus best practice: label values are lowercase, snake_case if
        // multi-word. Catches accidental "OrderNotFound" PascalCase leaks.
        var all = MetricNames.ProcessorValues.All
            .Concat(MetricNames.OrderStatusValues.All)
            .Concat(MetricNames.WebhookResultValues.All)
            .Concat(MetricNames.AwbResultValues.All)
            .Concat(MetricNames.AnafStatusValues.All);

        all.Should().AllSatisfy(v =>
        {
            v.Should().NotBeNullOrWhiteSpace();
            v.Should().MatchRegex("^[a-z][a-z0-9_]*$",
                because: $"label value '{v}' must be lowercase snake_case");
        });
    }
}
