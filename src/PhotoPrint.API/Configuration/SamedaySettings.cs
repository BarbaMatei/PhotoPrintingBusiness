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

    // Sameday service ids are per-merchant vendor config, not universal constants —
    // set both to the real ids from your Sameday contract before enabling.
    public int    LockerServiceId       { get; set; } = 7;
    public int    CourierServiceId      { get; set; } = 7;

    /// <summary>Lifecycle jobs (bolt 037). Orthogonal to <see cref="Enabled"/>: a
    /// deployment can wire credentials and validate them through the typed client
    /// without yet flipping the AWB / tracking workflows on.</summary>
    public SamedayJobsSettings Jobs { get; set; } = new();
}

/// <summary>
/// Settings for the AWB creation + tracking background jobs (bolt 037).
/// Gated separately from <see cref="SamedaySettings.Enabled"/> so a deployment
/// can run "credentials wired but no lifecycle automation yet" as a deliberate
/// rollout step.
/// </summary>
public sealed class SamedayJobsSettings
{
    public bool   Enabled                       { get; set; } = false;
    public int    AwbRetryIntervalMinutes       { get; set; } = 60;
    public int    AwbGiveUpHours                { get; set; } = 24;
    public int    TrackingIntervalMinutes       { get; set; } = 15;
    public int    TrackingMaxAgeDays            { get; set; } = 30;
    public int    MaxConcurrentSamedayCalls     { get; set; } = 5;

    // Transport rate limit (req/s), distinct from the concurrency gate above. Null = fall back to
    // MaxConcurrentSamedayCalls (the historical coupled behaviour).
    public int?   MaxRequestsPerSecond          { get; set; }

    public int[]  DispatchBackoffSeconds        { get; set; } = [30, 120, 300, 900, 3600];

    // How long one AWB-creation attempt owns an order before it's reclaimable. Must exceed
    // one vendor round-trip (RequestTimeoutSeconds) with margin; NOT tied to the retry cadence.
    public int    AwbClaimTtlMinutes            { get; set; } = 5;
}
