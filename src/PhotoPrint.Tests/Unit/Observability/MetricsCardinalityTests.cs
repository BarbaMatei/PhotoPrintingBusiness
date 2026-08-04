using System.Diagnostics.Metrics;
using System.Reflection;
using FluentAssertions;
using PhotoPrint.API.Observability;

namespace PhotoPrint.Tests.Unit.Observability;

/// <summary>
/// Cardinality budget: each labelled instrument is capped at ≤ 100 distinct series (per the
/// technical design's NFR section). Multiplying the value arrays proves only that the arrays
/// are short — it cannot see a call site attaching an unbounded label. So the budget here is
/// derived from <see cref="MetricNames.LabelContract"/>, and the emission tests beside each
/// call site assert their observed tags against that same contract
/// (<c>MetricCapture.ContractViolations</c>). Both halves are needed: this file bounds what is
/// declared, those tests bound what is emitted.
/// </summary>
public class MetricsCardinalityTests
{
    private const int Budget = 100;

    // The exact count per instrument, not just the ceiling: silent growth from 6 series to 9
    // stays under any sane budget, so a bare "<= 100" would let cardinality creep unnoticed.
    // Changing a number here is the acknowledgement that a label set grew.
    public static TheoryData<string, int> DeclaredInstruments() => new()
    {
        { MetricNames.Instruments.OrdersCreatedTotal, 6 },
        { MetricNames.Instruments.PaymentWebhookTotal, 12 },
        { MetricNames.Instruments.AwbCreationTotal, 5 },
        { MetricNames.Instruments.InvoiceAnafStatusTotal, 4 },
        { MetricNames.Instruments.UploadSizeBytes, 1 },
        { MetricNames.Instruments.OrderProcessingDurationSeconds, 1 },
    };

    [Theory]
    [MemberData(nameof(DeclaredInstruments))]
    public void Every_declared_instrument_is_within_the_cardinality_budget(
        string instrument, int expectedSeries)
    {
        var labels = MetricNames.LabelContract[instrument];

        var series = labels.Values.Aggregate(1, (acc, values) => acc * values.Length);

        series.Should().Be(expectedSeries,
            because: $"{instrument} has labels {string.Join(", ", labels.Keys)}");
        series.Should().BeLessThanOrEqualTo(Budget);
    }

    [Fact]
    public void Every_declared_instrument_has_an_expected_series_count()
    {
        // Otherwise a new instrument could be added to the contract and skip the count above.
        DeclaredInstruments().Select(row => (string)row[0])
            .Should().BeEquivalentTo(MetricNames.LabelContract.Keys);
    }

    [Fact]
    public void Every_instrument_FotoMetrics_defines_is_declared_in_the_label_contract()
    {
        // A new instrument that skips the contract would also skip the budget and the emission
        // tests' tag assertions, which is exactly how an unbounded label ships unnoticed.
        var defined = typeof(FotoMetrics)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => typeof(Instrument).IsAssignableFrom(f.FieldType))
            .Select(f => ((Instrument)f.GetValue(null)!).Name);

        defined.Should().BeSubsetOf(MetricNames.LabelContract.Keys);
    }

    [Fact]
    public void The_label_contract_declares_no_instrument_that_does_not_exist()
    {
        var defined = typeof(FotoMetrics)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => typeof(Instrument).IsAssignableFrom(f.FieldType))
            .Select(f => ((Instrument)f.GetValue(null)!).Name);

        MetricNames.LabelContract.Keys.Should().BeSubsetOf(defined);
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
        var all = MetricNames.LabelContract.Values
            .SelectMany(labels => labels.Values)
            .SelectMany(values => values);

        all.Should().AllSatisfy(v =>
        {
            v.Should().NotBeNullOrWhiteSpace();
            v.Should().MatchRegex("^[a-z][a-z0-9_]*$",
                because: $"label value '{v}' must be lowercase snake_case");
        });
    }

    [Fact]
    public void Label_names_are_snake_case()
    {
        MetricNames.LabelContract.Values
            .SelectMany(labels => labels.Keys)
            .Should().AllSatisfy(k => k.Should().MatchRegex("^[a-z][a-z0-9_]*$"));
    }
}
