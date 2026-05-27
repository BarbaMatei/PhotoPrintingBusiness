namespace PhotoPrint.API.Configuration;

public sealed class SecurityHeadersOptions
{
    /// <summary>
    /// Full Content-Security-Policy header value. Configurable via appsettings to allow
    /// fine-tuning for third-party integrations (Stripe, Google OAuth) without recompiling.
    /// </summary>
    public string ContentSecurityPolicy { get; init; } =
        "default-src 'self'; frame-ancestors 'none'; object-src 'none'";
}
