namespace PhotoPrint.API.Configuration;

public sealed class AppSettings
{
    /// <summary>Base URL of the Angular frontend (used in email links).</summary>
    public string BaseUrl { get; init; } = "http://localhost:4200";
}
