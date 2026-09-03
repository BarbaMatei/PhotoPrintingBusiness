using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Observability;

namespace PhotoPrint.API.Validators;

public sealed class ForwardedHeadersSettingsValidator : IValidateOptions<ForwardedHeadersSettings>
{
    private readonly IConfiguration _configuration;

    public ForwardedHeadersSettingsValidator(IConfiguration configuration) =>
        _configuration = configuration;

    public ValidateOptionsResult Validate(string? name, ForwardedHeadersSettings options)
    {
        var trusted = options.TrustedProxies ?? [];
        if (trusted.Length == 0)
            return ValidateOptionsResult.Success;

        var failures = new List<string>();

        ScrapeIpAllowList.Parse(trusted, out var entryErrors);
        foreach (var error in entryErrors)
            failures.Add($"ForwardedHeaders:TrustedProxies: {error}");

        var observability = _configuration
            .GetSection(ObservabilitySettings.SectionName)
            .Get<ObservabilitySettings>();

        if (observability is { Enabled: true } && observability.Metrics.ScrapePort == 0)
        {
            failures.Add(
                "ForwardedHeaders:TrustedProxies is set while Observability:Metrics:ScrapePort is 0. "
                + "A trusted proxy in front of the API means the scrape path must be served only on a "
                + "listener that proxy does not route, so the metrics allow-list keeps judging the real "
                + "peer — set Observability:Metrics:ScrapePort to that listener's port.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
