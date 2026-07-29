using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.Validators;

/// <summary>
/// Fails fast at boot when <see cref="AnafSettings"/> is misconfigured.
/// Every rule is guarded by <c>options.Enabled</c> so the disabled path
/// stays a no-op (intent goal: production-identical when the integration is
/// switched off). When enabled, the PKCS#12 cert file existence is checked
/// — boot rather than first-request is the right place to surface a missing
/// cert.
/// </summary>
public sealed class AnafSettingsValidator : IValidateOptions<AnafSettings>
{
    public ValidateOptionsResult Validate(string? name, AnafSettings options)
    {
        if (!options.Enabled)
            return ValidateOptionsResult.Success;

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BaseUrl) || !BeAbsoluteHttpUri(options.BaseUrl))
            failures.Add("Anaf:BaseUrl must be an absolute http(s) URL when Anaf:Enabled = true.");

        if (string.IsNullOrWhiteSpace(options.ClientId))
            failures.Add("Anaf:ClientId is required when Anaf:Enabled = true.");

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
            failures.Add("Anaf:ClientSecret is required when Anaf:Enabled = true.");

        if (string.IsNullOrWhiteSpace(options.CertPath))
        {
            failures.Add("Anaf:CertPath is required when Anaf:Enabled = true.");
        }
        else if (!File.Exists(options.CertPath))
        {
            // Don't include the path in the message — boot logs are widely
            // accessible and a host-path is mildly disclosive on its own.
            failures.Add("Anaf:CertPath points to a file that does not exist.");
        }

        if (string.IsNullOrWhiteSpace(options.CertPassword))
            failures.Add("Anaf:CertPassword is required when Anaf:Enabled = true.");

        if (options.PollIntervalMinutes is < 1 or > 1440)
            failures.Add("Anaf:PollIntervalMinutes must be between 1 and 1440 (a day).");

        if (options.MaxBatchSize is < 1 or > 500)
            failures.Add("Anaf:MaxBatchSize must be between 1 and 500.");

        if (options.BackoffHours is null || options.BackoffHours.Length == 0)
            failures.Add("Anaf:BackoffHours must contain at least one entry.");
        else if (options.BackoffHours.Any(h => h is < 1 or > 168))
            failures.Add("Anaf:BackoffHours entries must each be between 1 and 168 (one week).");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool BeAbsoluteHttpUri(string raw)
        => Uri.TryCreate(raw, UriKind.Absolute, out var u)
            && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
}
