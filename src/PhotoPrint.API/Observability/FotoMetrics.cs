using System.Diagnostics.Metrics;

namespace PhotoPrint.API.Observability;

/// <summary>
/// Static registry of every Meter and Instrument the API emits. Centralised
/// because:
///
/// - Adding a metric is a single-file change reviewable in one diff.
/// - Two call sites can never accidentally create instruments with the same
///   name (the static property is the only construction site).
/// - The Grafana dashboard in bolt 045 + the SLO doc reference these names;
///   `MetricNames` ties them to the same string constants.
///
/// Increment call sites in <c>OrderService</c>, <c>WebhooksController</c>,
/// <c>UploadService</c>, <c>AdminOrderService</c>, <c>AwbCreator</c> use
/// <see cref="TagList"/> (a stack-allocated struct) — no heap allocation per
/// observation.
///
/// The Meter is constructed eagerly. Even when <c>Observability:Enabled=false</c>,
/// the static instruments exist but no exporter is wired, so observations are
/// effectively a no-op (the OTel SDK's `Listener` pattern means an instrument
/// with no listener does the minimal amount of work).
/// </summary>
public static class FotoMetrics
{
    public static readonly Meter Meter = new(
        name: MetricNames.Meter,
        version: typeof(FotoMetrics).Assembly.GetName().Version?.ToString() ?? "0.0.0");

    public static readonly Counter<long> OrdersCreated =
        Meter.CreateCounter<long>(
            name: MetricNames.Instruments.OrdersCreatedTotal,
            unit: "1",
            description: "Orders created, by payment processor and resulting status.");

    public static readonly Counter<long> PaymentWebhook =
        Meter.CreateCounter<long>(
            name: MetricNames.Instruments.PaymentWebhookTotal,
            unit: "1",
            description: "Payment-provider webhook receipts, by processor and result class.");

    public static readonly Histogram<long> UploadSize =
        Meter.CreateHistogram<long>(
            name: MetricNames.Instruments.UploadSizeBytes,
            unit: "By",
            description: "Size of successfully accepted uploads in bytes.");

    public static readonly Histogram<double> OrderProcessingDuration =
        Meter.CreateHistogram<double>(
            name: MetricNames.Instruments.OrderProcessingDurationSeconds,
            unit: "s",
            description: "Wall-clock seconds from Order.PaidAt to Order.ShippedAt.");

    public static readonly Counter<long> AwbCreation =
        Meter.CreateCounter<long>(
            name: MetricNames.Instruments.AwbCreationTotal,
            unit: "1",
            description: "AWB-creation outcomes, by terminal result branch.");

    public static readonly Counter<long> InvoiceAnafStatus =
        Meter.CreateCounter<long>(
            name: MetricNames.Instruments.InvoiceAnafStatusTotal,
            unit: "1",
            description: "ANAF e-Factura submission outcomes (defined here, increments ship with intent 016).");
}
