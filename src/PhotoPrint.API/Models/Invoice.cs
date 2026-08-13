namespace PhotoPrint.API.Models;

/// <summary>
/// Romanian fiscal invoice (intent 016, bolt 038). One-per-order (or zero,
/// for orders that never reached <c>Paid</c>). Frozen legal artefact —
/// 10-year retention; the fields below are sampled at issue time and not
/// re-derived from <see cref="Order"/> later.
///
/// Bolt 038 ships the schema + the entity; bolt 039 owns insertion at the
/// Paid transition and the ANAF lifecycle columns.
/// </summary>
public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }

    /// <summary>Full formatted invoice number, e.g. <c>"FT-2026-00001"</c>.
    /// Immutable once written. The unique constraint on this column is the
    /// last-line-of-defence against numbering races (ADR-020).</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>Series code, e.g. <c>"FT"</c>. Denormalised from
    /// <see cref="InvoiceNumber"/> for indexing.</summary>
    public string Series { get; set; } = string.Empty;

    /// <summary>The numeric portion of <see cref="InvoiceNumber"/>, e.g.
    /// <c>1</c> for <c>"FT-2026-00001"</c>. Denormalised so SQLite's
    /// <c>MAX + 1</c> path stays trivial (ADR-020).</summary>
    public int Number { get; set; }

    /// <summary>Legal issue date — derived from <see cref="Order.PaidAt"/>,
    /// not from "now". Determines the fiscal year for numbering.</summary>
    public DateTimeOffset IssuedAt { get; set; }

    public decimal NetTotalRon { get; set; }
    public decimal VatRon      { get; set; }
    public decimal TotalRon    { get; set; }

    /// <summary>UBL 2.1 XML body. Populated by bolt 039 at issue time.</summary>
    public string? XmlPayload { get; set; }

    /// <summary>Storage path of the rendered PDF. Populated by bolt 039.</summary>
    public string? PdfStoragePath { get; set; }

    /// <summary>ANAF SPV upload identifier returned on submission. Populated
    /// by bolt 039's <c>InvoiceUploadJob</c>.</summary>
    public string? AnafUploadId { get; set; }

    /// <summary>ANAF submission lifecycle state. Defaults to
    /// <see cref="InvoiceAnafStatus.Pending"/>; transitions are owned by
    /// bolt 039's worker.</summary>
    public InvoiceAnafStatus AnafStatus { get; set; } = InvoiceAnafStatus.Pending;

    /// <summary>Last ANAF rejection / network error. Populated by bolt 039.</summary>
    public string? LastError { get; set; }

    public DateTimeOffset CreatedAt  { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Reclaimable per Anaf:ClaimTtlMinutes; guards against two workers processing one row at once.
    public DateTimeOffset? ClaimedAt { get; set; }

    public Order? Order { get; set; }
}

/// <summary>
/// ANAF e-Factura submission lifecycle. <c>Pending</c> on creation;
/// transitions to <c>Submitted</c>/<c>Accepted</c>/<c>Rejected</c>/<c>Failed</c>
/// are bolt 039's concern.
/// </summary>
public enum InvoiceAnafStatus
{
    Pending,
    Submitted,
    Accepted,
    Rejected,
    Failed,
}
