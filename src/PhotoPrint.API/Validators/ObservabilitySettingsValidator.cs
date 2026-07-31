using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.Validators;

/// <summary>
/// Fails fast at startup when the observability stack is misconfigured.
/// Every rule is guarded by <c>options.Enabled</c> so the validator is a
/// no-op when the integration is switched off — boot stays byte-identical
/// to the pre-bolt baseline.
/// </summary>
public sealed class ObservabilitySettingsValidator : IValidateOptions<ObservabilitySettings>
{
    public ValidateOptionsResult Validate(string? name, ObservabilitySettings options)
    {
        if (!options.Enabled)
            return ValidateOptionsResult.Success;

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ServiceName))
            failures.Add("Observability:ServiceName is required when Observability:Enabled = true.");

        if (options.Otlp.Protocol is not ("Grpc" or "HttpProtobuf"))
            failures.Add("Observability:Otlp:Protocol must be 'Grpc' or 'HttpProtobuf'.");

        if (!string.IsNullOrWhiteSpace(options.Otlp.Endpoint))
        {
            if (!Uri.TryCreate(options.Otlp.Endpoint, UriKind.Absolute, out var u)
                || u.Scheme is not ("http" or "https"))
            {
                failures.Add("Observability:Otlp:Endpoint must be an absolute http(s) URL when provided.");
            }
        }

        if (string.IsNullOrWhiteSpace(options.Metrics.PrometheusEndpoint)
            || !options.Metrics.PrometheusEndpoint.StartsWith('/'))
        {
            failures.Add("Observability:Metrics:PrometheusEndpoint must start with '/'.");
        }

        if (options.Metrics.ScrapePort is < 0 or > 65535)
            failures.Add("Observability:Metrics:ScrapePort must be between 0 and 65535 (0 = every listener).");

        if (options.Metrics.AllowedScrapeIps is null || options.Metrics.AllowedScrapeIps.Length == 0)
            failures.Add("Observability:Metrics:AllowedScrapeIps must contain at least one entry " +
                         "(per ADR-018, network identity is the only access control on /metrics).");

        if (options.Sampling.Default is < 0.0 or > 1.0)
            failures.Add("Observability:Sampling:Default must be between 0.0 and 1.0.");

        if (options.Sampling.Routes is not null)
        {
            foreach (var (route, rate) in options.Sampling.Routes)
            {
                if (rate is < 0.0 or > 1.0)
                    failures.Add($"Observability:Sampling:Routes['{route}'] must be between 0.0 and 1.0.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
