using System.Diagnostics.Metrics;
using FluentAssertions;
using PhotoPrint.API.Observability;

namespace PhotoPrint.Tests.Unit.Observability;

/// <summary>
/// Pins the metric registry contract: names, types, and units match what the
/// Grafana dashboard and the SLO doc expect. Any rename in <see cref="FotoMetrics"/>
/// without a matching dashboard update will fail one of these tests.
/// </summary>
public class FotoMetricsTests
{
    [Fact]
    public void Meter_name_matches_constants()
    {
        FotoMetrics.Meter.Name.Should().Be(MetricNames.Meter);
        FotoMetrics.Meter.Name.Should().Be("PhotoPrint.API");
    }

    [Theory]
    [InlineData(nameof(FotoMetrics.OrdersCreated),           "orders_created_total",              "1")]
    [InlineData(nameof(FotoMetrics.PaymentWebhook),          "payment_webhook_total",             "1")]
    [InlineData(nameof(FotoMetrics.UploadSize),              "upload_size_bytes",                 "By")]
    [InlineData(nameof(FotoMetrics.OrderProcessingDuration), "order_processing_duration_seconds", "s")]
    [InlineData(nameof(FotoMetrics.AwbCreation),             "awb_creation_total",                "1")]
    [InlineData(nameof(FotoMetrics.InvoiceAnafStatus),       "invoice_anaf_status_total",         "1")]
    public void Instrument_has_expected_name_and_unit(string memberName, string expectedName, string expectedUnit)
    {
        var member  = typeof(FotoMetrics).GetField(memberName);
        member.Should().NotBeNull();
        var instrument = (Instrument)member!.GetValue(null)!;

        instrument.Name.Should().Be(expectedName);
        instrument.Unit.Should().Be(expectedUnit);
    }

    [Fact]
    public void Counters_are_counters_histograms_are_histograms()
    {
        FotoMetrics.OrdersCreated.Should().BeOfType<Counter<long>>();
        FotoMetrics.PaymentWebhook.Should().BeOfType<Counter<long>>();
        FotoMetrics.AwbCreation.Should().BeOfType<Counter<long>>();
        FotoMetrics.InvoiceAnafStatus.Should().BeOfType<Counter<long>>();

        FotoMetrics.UploadSize.Should().BeOfType<Histogram<long>>();
        FotoMetrics.OrderProcessingDuration.Should().BeOfType<Histogram<double>>();
    }

    [Fact]
    public void Every_instrument_has_a_description()
    {
        // Sanity — the description is what surfaces in /metrics HELP lines and in
        // tracing backends. An empty description is a red flag in code review.
        var instruments = new Instrument[]
        {
            FotoMetrics.OrdersCreated,
            FotoMetrics.PaymentWebhook,
            FotoMetrics.UploadSize,
            FotoMetrics.OrderProcessingDuration,
            FotoMetrics.AwbCreation,
            FotoMetrics.InvoiceAnafStatus,
        };
        instruments.Should().AllSatisfy(i => i.Description.Should().NotBeNullOrWhiteSpace());
    }
}
