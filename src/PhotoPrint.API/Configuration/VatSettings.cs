namespace PhotoPrint.API.Configuration;

/// <summary>
/// Configuration for VAT calculation (intent 016, bolt 038).
/// Romanian standard rate is 19% on photo prints (no reduced rate today).
/// Unlike Sameday / Sentry / Observability, there is NO master Enabled flag —
/// VAT computation is unconditional: every order must carry the breakdown for
/// legal compliance.
/// </summary>
public sealed class VatSettings
{
    public const string SectionName = "Vat";

    /// <summary>The VAT rate as a fraction (0.19 = 19%). Snapshot at order
    /// creation time onto <c>Order.VatRate</c>; changing this value does NOT
    /// mutate existing orders.</summary>
    public decimal Rate { get; init; } = 0.19m;

    /// <summary>The invoice series code. <c>FT</c> = factură (invoice).
    /// Future series (<c>FP</c> proforma, <c>FS</c> storno) can be added here.</summary>
    public string InvoiceSeries { get; init; } = "FT";
}
