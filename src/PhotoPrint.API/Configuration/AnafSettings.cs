namespace PhotoPrint.API.Configuration;

/// <summary>
/// ANAF SPV (e-Factura submission) integration — intent 016, bolt 039.
///
/// When <see cref="Enabled"/> is false the OAuth, HTTP client, and
/// <c>InvoiceUploadJob</c> are never wired; the system is byte-identical to
/// the pre-integration baseline. Same two-stage rollout posture as Sameday
/// (intent 015) and Sentry (intent 020).
///
/// Secrets (<see cref="ClientSecret"/>, <see cref="CertPath"/>,
/// <see cref="CertPassword"/>) live in environment variables in production
/// (<c>Anaf__ClientSecret</c>, <c>Anaf__CertPath</c>, <c>Anaf__CertPassword</c>) —
/// never in <c>appsettings.json</c> (ADR-006).
/// </summary>
public sealed class AnafSettings
{
    public const string SectionName = "Anaf";

    public bool Enabled { get; set; }

    public string BaseUrl      { get; set; } = string.Empty;
    public string ClientId     { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string CertPath     { get; set; } = string.Empty;
    public string CertPassword { get; set; } = string.Empty;

    /// <summary>Worker poll cadence. Default 30 min matches the
    /// 5-business-day ANAF SLA with ~240× headroom (ADR-023).</summary>
    public int PollIntervalMinutes { get; set; } = 30;

    /// <summary>Max invoices the worker fetches per tick. Caps DB read
    /// pressure and bounds per-tick HTTP fan-out.</summary>
    public int MaxBatchSize { get; set; } = 50;

    /// <summary>Retry budget for ANAF rejections in hours.
    /// Default <c>1h, 4h, 16h, 64h</c> then escalate to <c>Failed</c>
    /// (ADR-024). Attempt count is derived from
    /// <c>(now - Invoice.CreatedAt)</c> against the cumulative sum;
    /// no persisted counter.</summary>
    public int[] BackoffHours { get; set; } = [1, 4, 16, 64];
}
