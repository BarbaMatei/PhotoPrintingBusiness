namespace PhotoPrint.API.Configuration;

public sealed class CorsSettings
{
    public string AllowedOrigins { get; init; } = string.Empty;

    /// <summary>Splits the comma-separated origins string into a trimmed, non-empty array.</summary>
    public string[] GetOrigins() =>
        AllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
