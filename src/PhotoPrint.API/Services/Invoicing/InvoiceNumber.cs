namespace PhotoPrint.API.Services.Invoicing;

/// <summary>
/// Structured representation of an invoice number, e.g.
/// <c>InvoiceNumber("FT", 2026, 1) → "FT-2026-00001"</c>. The DB stores
/// the formatted string; this struct exists so call sites that need to
/// read the components back semantically don't parse strings.
/// </summary>
public readonly record struct InvoiceNumber(string Series, int Year, int Number)
{
    public override string ToString() => $"{Series}-{Year:D4}-{Number:D5}";
}
