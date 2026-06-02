using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.Validators;

/// <summary>
/// Fails fast at startup (via <c>.ValidateOnStart()</c>) when
/// <see cref="SamedaySettings"/> is misconfigured. Every rule is guarded by
/// <c>options.Enabled</c> so the validator is a no-op when the integration
/// is switched off — keeping the disabled-by-default path identical to the
/// pre-integration baseline (intent goal: zero-risk rollback).
///
/// Follows the project's <c>IValidateOptions&lt;T&gt;</c> pattern for
/// configuration validation; <c>FluentValidation</c> remains the path for
/// controller DTOs (ADR-002).
/// </summary>
public sealed class SamedaySettingsValidator : IValidateOptions<SamedaySettings>
{
    public ValidateOptionsResult Validate(string? name, SamedaySettings options)
    {
        if (!options.Enabled)
            return ValidateOptionsResult.Success;

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BaseUrl) || !BeAbsoluteHttpUri(options.BaseUrl))
            failures.Add("Sameday:BaseUrl must be an absolute http(s) URL.");

        if (string.IsNullOrWhiteSpace(options.Username))
            failures.Add("Sameday:Username is required when Sameday:Enabled = true.");

        if (string.IsNullOrWhiteSpace(options.Password))
            failures.Add("Sameday:Password is required when Sameday:Enabled = true.");

        if (string.IsNullOrWhiteSpace(options.PickupPointId))
            failures.Add("Sameday:PickupPointId is required when Sameday:Enabled = true.");

        if (options.RequestTimeoutSeconds is < 1 or > 60)
            failures.Add("Sameday:RequestTimeoutSeconds must be between 1 and 60.");

        if (options.Jobs.Enabled)
        {
            if (options.Jobs.AwbRetryIntervalMinutes < 1)
                failures.Add("Sameday:Jobs:AwbRetryIntervalMinutes must be >= 1.");
            if (options.Jobs.AwbGiveUpHours < 1)
                failures.Add("Sameday:Jobs:AwbGiveUpHours must be >= 1.");
            if (options.Jobs.TrackingIntervalMinutes < 1)
                failures.Add("Sameday:Jobs:TrackingIntervalMinutes must be >= 1.");
            if (options.Jobs.TrackingMaxAgeDays < 1)
                failures.Add("Sameday:Jobs:TrackingMaxAgeDays must be >= 1.");
            if (options.Jobs.MaxConcurrentSamedayCalls is < 1 or > 50)
                failures.Add("Sameday:Jobs:MaxConcurrentSamedayCalls must be between 1 and 50.");
            if (options.Jobs.DispatchBackoffSeconds is null || options.Jobs.DispatchBackoffSeconds.Length == 0)
                failures.Add("Sameday:Jobs:DispatchBackoffSeconds must contain at least one value.");
            else if (options.Jobs.DispatchBackoffSeconds.Any(s => s < 1))
                failures.Add("Sameday:Jobs:DispatchBackoffSeconds entries must each be >= 1.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool BeAbsoluteHttpUri(string raw)
        => Uri.TryCreate(raw, UriKind.Absolute, out var u)
            && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
}
