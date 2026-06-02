using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.Validators;

/// <summary>
/// Fails fast at startup when Sentry is misconfigured. Every rule is guarded by
/// <c>options.Enabled</c> so the validator is a no-op when the integration is
/// switched off — the disabled-by-default path stays byte-identical to the
/// pre-bolt baseline.
/// </summary>
public sealed class SentrySettingsValidator : IValidateOptions<SentrySettings>
{
    public ValidateOptionsResult Validate(string? name, SentrySettings options)
    {
        if (!options.Enabled)
            return ValidateOptionsResult.Success;

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Dsn))
        {
            failures.Add("Sentry:Dsn is required when Sentry:Enabled = true.");
        }
        else if (!Uri.TryCreate(options.Dsn, UriKind.Absolute, out var u)
                 || (u.Scheme != Uri.UriSchemeHttp && u.Scheme != Uri.UriSchemeHttps))
        {
            failures.Add("Sentry:Dsn must be an absolute http(s) URL.");
        }

        if (options.SampleRate is < 0.0 or > 1.0)
            failures.Add("Sentry:SampleRate must be between 0.0 and 1.0.");

        if (options.TracesSampleRate is < 0.0 or > 1.0)
            failures.Add("Sentry:TracesSampleRate must be between 0.0 and 1.0.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
