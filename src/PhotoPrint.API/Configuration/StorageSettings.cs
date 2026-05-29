using Microsoft.Extensions.Options;

namespace PhotoPrint.API.Configuration;

/// <summary>
/// Storage layer configuration (bolt 043).
/// <para><b>Provider</b> selects the cloud tier (<c>Local</c> = disabled, <c>S3</c> = enabled).
/// The local tier is always available. See ADR-008 for the two-tier model.</para>
/// <para>For Cloudflare R2 (recommended — ADR-009): <c>Provider=S3</c>, <c>Region="auto"</c>,
/// <c>ForcePathStyle=true</c>, <c>EndpointUrl=https://&lt;account-id&gt;.r2.cloudflarestorage.com</c>.</para>
/// </summary>
public class StorageSettings
{
    public const string SectionName = "Storage";

    /// <summary>Selects the cloud tier. <c>Local</c> (default — cloud disabled) | <c>S3</c>.</summary>
    public string Provider { get; set; } = "Local";

    /// <summary>Absolute path to the root upload directory (local tier).</summary>
    public string BasePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PhotoPrint", "uploads");

    // ─── S3 / R2 / MinIO settings (only used when Provider == "S3") ────────────

    /// <summary>S3 bucket name. Required when <see cref="IsCloudEnabled"/>.</summary>
    public string? Bucket { get; set; }

    /// <summary>S3 region. Use <c>auto</c> for Cloudflare R2; a real region for AWS.</summary>
    public string Region { get; set; } = "auto";

    /// <summary>Custom endpoint URL (R2 / MinIO). Null = AWS native.</summary>
    public string? EndpointUrl { get; set; }

    /// <summary>Required true for R2 / MinIO; leave false for AWS native.</summary>
    public bool ForcePathStyle { get; set; }

    /// <summary>S3 access key (R2 API token id). Loaded from secret store — never committed (ADR-006).</summary>
    public string? AccessKey { get; set; }

    /// <summary>S3 secret key (R2 API token secret). Loaded from secret store — never committed (ADR-006).</summary>
    public string? SecretKey { get; set; }

    /// <summary>Presigned-URL lifetime for previews (minutes).</summary>
    public int PresignTtlMinutes { get; set; } = 60;

    /// <summary>True when the cloud tier is enabled (case-insensitive comparison on Provider).</summary>
    public bool IsCloudEnabled => string.Equals(Provider, "S3", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Fails fast at startup (via <c>.ValidateOnStart()</c>) when the cloud tier is on but
/// required S3 settings are missing — keeps misconfiguration off the request hot path.
/// </summary>
public class StorageSettingsValidator : IValidateOptions<StorageSettings>
{
    public ValidateOptionsResult Validate(string? name, StorageSettings options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BasePath))
            failures.Add("Storage:BasePath is required.");

        if (options.IsCloudEnabled)
        {
            if (string.IsNullOrWhiteSpace(options.Bucket))
                failures.Add("Storage:Bucket is required when Storage:Provider=S3.");
            if (string.IsNullOrWhiteSpace(options.AccessKey))
                failures.Add("Storage:AccessKey is required when Storage:Provider=S3.");
            if (string.IsNullOrWhiteSpace(options.SecretKey))
                failures.Add("Storage:SecretKey is required when Storage:Provider=S3.");
            if (string.IsNullOrWhiteSpace(options.Region))
                failures.Add("Storage:Region is required when Storage:Provider=S3 (use 'auto' for R2).");
            if (options.PresignTtlMinutes <= 0)
                failures.Add("Storage:PresignTtlMinutes must be > 0.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
