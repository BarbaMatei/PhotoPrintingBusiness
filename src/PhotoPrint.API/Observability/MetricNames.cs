namespace PhotoPrint.API.Observability;

/// <summary>
/// Single source of truth for metric names and label values used across the
/// API. Constants here are referenced by both the instrument definitions in
/// <see cref="FotoMetrics"/> and the Grafana dashboard at
/// <c>ops/dashboards/fototipar-overview.json</c>; renames need both edits.
///
/// Label values are listed here as constants (not enum values) so they
/// serialise as the literal string the metric backend sees, and so the
/// cardinality test can enumerate the set programmatically.
/// </summary>
public static class MetricNames
{
    public const string Meter = "PhotoPrint.API";

    public static class Instruments
    {
        public const string OrdersCreatedTotal           = "orders_created_total";
        public const string PaymentWebhookTotal          = "payment_webhook_total";
        public const string UploadSizeBytes              = "upload_size_bytes";
        public const string OrderProcessingDurationSeconds = "order_processing_duration_seconds";
        public const string AwbCreationTotal             = "awb_creation_total";
        public const string InvoiceAnafStatusTotal       = "invoice_anaf_status_total";
    }

    public static class Labels
    {
        public const string Processor = "processor";
        public const string Result    = "result";
        public const string Status    = "status";
    }

    public static class ProcessorValues
    {
        public const string Stripe    = "stripe";
        public const string EuPlatesc = "euplatesc";

        public static readonly string[] All = [Stripe, EuPlatesc];
    }

    public static class OrderStatusValues
    {
        public const string Created   = "created";
        public const string Paid      = "paid";
        public const string Cancelled = "cancelled";

        public static readonly string[] All = [Created, Paid, Cancelled];
    }

    public static class WebhookResultValues
    {
        public const string Ok                = "ok";
        public const string SignatureInvalid  = "signature_invalid";
        public const string OrderNotFound     = "order_not_found";
        public const string AmountMismatch    = "amount_mismatch";
        public const string Duplicate         = "duplicate";
        public const string Failed            = "failed";

        public static readonly string[] All =
            [Ok, SignatureInvalid, OrderNotFound, AmountMismatch, Duplicate, Failed];
    }

    public static class AwbResultValues
    {
        public const string Ok          = "ok";
        public const string Skipped     = "skipped";
        public const string RetryLater  = "retry_later";
        public const string GiveUp      = "give_up";
        public const string Error       = "error";

        public static readonly string[] All = [Ok, Skipped, RetryLater, GiveUp, Error];
    }

    public static class AnafStatusValues
    {
        public const string Accepted = "accepted";
        public const string Rejected = "rejected";
        public const string Pending  = "pending";
        public const string Failed   = "failed";

        public static readonly string[] All = [Accepted, Rejected, Pending, Failed];
    }
}
