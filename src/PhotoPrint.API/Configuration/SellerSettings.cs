namespace PhotoPrint.API.Configuration;

/// <summary>
/// Fiscal identity of the seller — embedded in every UBL invoice and PDF
/// (intent 016, bolt 039). Read once at startup; immutable for the lifetime
/// of the process. The seller is config-driven, not per-request — single-tenant.
/// Validated by <c>SellerSettingsValidator</c> at boot.
/// </summary>
public sealed class SellerSettings
{
    public const string SectionName = "Seller";

    public string Name               { get; set; } = string.Empty;
    public string Cui                { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string IbanRon            { get; set; } = string.Empty;
    public SellerAddress Address     { get; set; } = new();
}

public sealed class SellerAddress
{
    public string Line1       { get; set; } = string.Empty;
    public string City        { get; set; } = string.Empty;
    public string PostalCode  { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "RO";
}
