using System.Net.Sockets;
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

        var parsed = ScrapeIpAllowList.Parse(trusted, out var entryErrors);
        foreach (var error in entryErrors)
            failures.Add($"ForwardedHeaders:TrustedProxies: {error}");

        foreach (var network in parsed.Networks)
        {
            var pairPrefix = network.BaseAddress.AddressFamily == AddressFamily.InterNetwork ? 31 : 127;
            if (network.PrefixLength < pairPrefix)
            {
                failures.Add(
                    $"ForwardedHeaders:TrustedProxies: '{network}' is wider than a single address "
                    + $"pair (/{pairPrefix}). Every host in it can reach the API's exposed ports "
                    + "directly and would be believed about who the client is — name the proxy's "
                    + "own address instead.");
                continue;
            }

            if (network.BaseAddress.IsIPv6LinkLocal)
                failures.Add(LinkLocalFailure(network.ToString()));
        }

        foreach (var address in parsed.Addresses)
        {
            if (address.IsIPv6LinkLocal)
                failures.Add(LinkLocalFailure(address.ToString()));
        }

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

    private static string LinkLocalFailure(string entry) =>
        $"ForwardedHeaders:TrustedProxies: '{entry}' is IPv6 link-local, which names no single host: "
        + "the zone is dropped when a peer is compared, so this would trust the same address on "
        + "every interface — name the proxy's routable address instead.";
}
