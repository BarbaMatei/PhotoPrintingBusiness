using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.Validators;

// Every rule is guarded by options.Enabled, so a disabled deploy boots with unvalidated ANAF config — the worker that would read it is not registered either.
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

        if (options.ClaimTtlMinutes is < 2 or > 1440)
            failures.Add("Anaf:ClaimTtlMinutes must be between 2 and 1440 — below 2 a second worker can reclaim an invoice while the first is still mid-pass.");

        if (options.MaxUnknownUploadOutcomes is < 1 or > 10)
            failures.Add("Anaf:MaxUnknownUploadOutcomes must be between 1 and 10 — below 1 a single network blip strands an invoice, and every attempt above risks another copy of the same invoice number at ANAF.");

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
