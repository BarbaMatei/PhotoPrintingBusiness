namespace PhotoPrint.API.Services.Invoicing.Anaf;

/// <summary>
/// HTTP transport to ANAF SPV (Spațiu Privat Virtual). Owns the network
/// chokepoint and performs the wire-to-domain mapping at the
/// anti-corruption boundary. Body content is NEVER logged — buyer PII
/// would leak otherwise.
/// </summary>
public interface IAnafSpvClient
{
    /// <summary>
    /// Uploads a UBL invoice XML payload to ANAF.
    /// Returns the <c>AnafUploadId</c> on success.
    /// Throws <see cref="AnafAuthException"/> on twice-401,
    /// <see cref="AnafUploadException"/> on body-encoded errors,
    /// <see cref="AnafUnreachableException"/> on transport failure.
    /// </summary>
    Task<AnafUploadResult> UploadAsync(byte[] invoiceXml, CancellationToken ct = default);

    /// <summary>
    /// Polls ANAF for the status of a previously-uploaded invoice.
    /// </summary>
    Task<AnafStatusResult> GetStatusAsync(string uploadId, CancellationToken ct = default);
}

public sealed record AnafUploadResult(string UploadId, DateTimeOffset SubmittedAt);

public sealed record AnafStatusResult(
    AnafExternalStatus Status,
    string? ErrorMessage = null,
    DateTimeOffset? ProcessedAt = null);

/// <summary>
/// ANAF's wire status vocabulary. Mapped to our internal
/// <see cref="PhotoPrint.API.Models.InvoiceAnafStatus"/> by the worker.
/// </summary>
public enum AnafExternalStatus
{
    /// <summary>ANAF wire <c>"ok"</c> — invoice validated and registered.
    /// Maps to internal <c>Accepted</c>.</summary>
    Validated,

    /// <summary>ANAF wire <c>"nok"</c> — invoice rejected with errors.
    /// Maps to internal <c>Rejected</c> (or <c>Failed</c> when budget exhausted).</summary>
    Rejected,

    /// <summary>ANAF wire <c>"in prelucrare"</c> — still being processed.
    /// The worker leaves the invoice in <c>Submitted</c> and re-polls next tick.</summary>
    InProgress,

    /// <summary>Unrecognised wire status. Treated as <c>InProgress</c> by
    /// the worker — never trips a state transition.</summary>
    Unknown,
}
