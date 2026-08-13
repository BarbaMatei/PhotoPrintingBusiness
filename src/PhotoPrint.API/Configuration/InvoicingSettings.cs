namespace PhotoPrint.API.Configuration;

/// <summary>
/// Cross-cutting invoicing toggles (intent 016, bolt 039).
/// Distinct from <see cref="VatSettings"/> (math) and <see cref="AnafSettings"/>
/// (external integration) — this block holds the rollout-phase flags.
/// </summary>
public sealed class InvoicingSettings
{
    public const string SectionName = "Invoicing";

    public CustomerEmailAttachmentSettings CustomerEmailAttachments { get; set; } = new();
}

/// <summary>
/// Dual-write rollout flag for customer-facing PDF attachments (ADR-022).
/// Default <c>false</c>: the full pipeline runs (XML build, ANAF upload,
/// PDF render, storage write) but the PDF is NOT attached to the
/// order-confirmation email and no follow-up "Invoice ready" email is sent.
/// Flipping to <c>true</c> does not yet send anything — no email attachment integration exists.
/// </summary>
public sealed class CustomerEmailAttachmentSettings
{
    public bool Enabled { get; set; } = false;
}
