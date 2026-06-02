using System.Text.Json.Serialization;

namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Internal vendor JSON shapes. These are wire DTOs — they exist only to
/// be deserialized at the anti-corruption boundary inside
/// <c>SamedayClient</c> and never escape this namespace. Domain code talks
/// in <see cref="SamedayToken"/>, <see cref="AwbCreationResult"/>, etc.
/// </summary>
internal static class SamedayWireDtos
{
    /// <summary>
    /// Successful response from <c>POST /api/authenticate</c>.
    /// The Sameday docs document <c>token</c> + <c>expire_at_utc</c>
    /// (ISO-8601 string). Names use the snake_case the vendor emits.
    /// </summary>
    public sealed class AuthenticateResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("expire_at_utc")]
        public DateTimeOffset? ExpireAtUtc { get; set; }
    }

    // ── AWB creation (bolt 037 story 001) ────────────────────────────────────

    /// <summary>Request body for <c>POST /api/awb</c>.</summary>
    public sealed class AwbCreateRequest
    {
        [JsonPropertyName("pickupPoint")]    public string? PickupPoint    { get; set; }
        [JsonPropertyName("awbPayment")]     public int     AwbPayment     { get; set; } = 1;   // paid
        [JsonPropertyName("thirdPartyPickup")] public int   ThirdPartyPickup { get; set; }
        [JsonPropertyName("service")]        public int     Service        { get; set; } = 7;   // Easybox / locker — vendor-specific code
        [JsonPropertyName("packageType")]    public int     PackageType    { get; set; } = 1;   // parcel
        [JsonPropertyName("packageNumber")]  public int     PackageNumber  { get; set; } = 1;
        [JsonPropertyName("packageWeight")]  public decimal PackageWeight  { get; set; }
        [JsonPropertyName("cashOnDelivery")] public decimal CashOnDelivery { get; set; }
        [JsonPropertyName("insuredValue")]   public decimal InsuredValue   { get; set; }
        [JsonPropertyName("observation")]    public string? Observation    { get; set; }
        [JsonPropertyName("lockerLastMile")] public string? LockerLastMile { get; set; }
        [JsonPropertyName("clientInternalReference")] public string? ClientInternalReference { get; set; }
        [JsonPropertyName("awbRecipient")]   public AwbRecipient? AwbRecipient { get; set; }
        [JsonPropertyName("parcels")]        public IReadOnlyList<AwbParcel>? Parcels { get; set; }
    }

    public sealed class AwbRecipient
    {
        [JsonPropertyName("name")]        public string? Name        { get; set; }
        [JsonPropertyName("phoneNumber")] public string? PhoneNumber { get; set; }
        [JsonPropertyName("address")]     public string? Address     { get; set; }
        [JsonPropertyName("city")]        public string? City        { get; set; }
        [JsonPropertyName("county")]      public string? County      { get; set; }
        [JsonPropertyName("postalCode")]  public string? PostalCode  { get; set; }
    }

    public sealed class AwbParcel
    {
        [JsonPropertyName("weight")] public decimal Weight { get; set; }
        [JsonPropertyName("length")] public int     Length { get; set; } = 20;
        [JsonPropertyName("width")]  public int     Width  { get; set; } = 15;
        [JsonPropertyName("height")] public int     Height { get; set; } = 2;
        [JsonPropertyName("type")]   public int     Type   { get; set; }
    }

    /// <summary>Successful response from <c>POST /api/awb</c>.</summary>
    public sealed class AwbCreateResponse
    {
        [JsonPropertyName("awbNumber")]     public string? AwbNumber     { get; set; }
        [JsonPropertyName("awbCost")]       public decimal AwbCost       { get; set; }
        [JsonPropertyName("pdfLink")]       public string? PdfLink       { get; set; }
    }

    // ── Tracking (bolt 037 story 003) ────────────────────────────────────────

    /// <summary>Response shape for <c>GET /api/awb/{number}/tracking</c>.</summary>
    public sealed class TrackingResponse
    {
        [JsonPropertyName("awbNumber")]   public string? AwbNumber  { get; set; }
        [JsonPropertyName("status")]      public string? Status     { get; set; }   // vendor status code
        [JsonPropertyName("deliveredAt")] public DateTimeOffset? DeliveredAt { get; set; }
        [JsonPropertyName("observedAt")]  public DateTimeOffset? ObservedAt  { get; set; }
        [JsonPropertyName("history")]     public IReadOnlyList<TrackingHistoryEntry>? History { get; set; }
    }

    public sealed class TrackingHistoryEntry
    {
        [JsonPropertyName("status")]      public string? Status      { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("occurredAt")]  public DateTimeOffset? OccurredAt { get; set; }
    }
}
