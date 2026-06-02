namespace PhotoPrint.API.Configuration;

/// <summary>
/// Configuration for the Sameday courier integration (intent 015, bolt 036).
/// When <see cref="Enabled"/> is false the rest of the system is bit-for-bit
/// identical to the pre-integration baseline — <c>StaticShippingService</c>
/// remains the registered <c>IShippingService</c>. Validation is conditional
/// on <see cref="Enabled"/>; see <c>SamedaySettingsValidator</c>.
///
/// Credentials live in <c>dotnet user-secrets</c> (dev) or environment
/// variables (staging/prod) — never in <c>appsettings.json</c> (ADR-006).
/// </summary>
public sealed class SamedaySettings
{
    public const string SectionName = "Sameday";

    public bool   Enabled               { get; set; }
    public string BaseUrl               { get; set; } = "https://api.sameday.ro";
    public string Username              { get; set; } = string.Empty;
    public string Password              { get; set; } = string.Empty;
    public string PickupPointId         { get; set; } = string.Empty;
    public int    RequestTimeoutSeconds { get; set; } = 10;
}
