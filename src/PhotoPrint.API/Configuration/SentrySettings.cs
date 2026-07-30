namespace PhotoPrint.API.Configuration;

/// <summary>
/// Configuration for Sentry error tracking (intent 020, bolt 045).
/// Two-stage rollout posture matching <c>SamedaySettings</c>: <see cref="Enabled"/>
/// is false by default and validation is conditional on it. When off, the SDK is
/// never constructed — boot is byte-identical to the pre-bolt baseline.
///
/// The DSN lives in <c>dotnet user-secrets</c> (dev) or environment variables
/// (staging/prod) — never in <c>appsettings.json</c>.
/// </summary>
public sealed class SentrySettings
{
    public const string SectionName = "Sentry";

    public bool    Enabled          { get; set; }
    public string  Dsn              { get; set; } = string.Empty;
    public string? Release          { get; set; }
    public string? Environment      { get; set; }
    public double  SampleRate       { get; set; } = 1.0;
    public double  TracesSampleRate { get; set; } = 0.1;
    public bool    Debug            { get; set; }
}
