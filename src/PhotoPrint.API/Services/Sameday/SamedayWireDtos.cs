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
}
