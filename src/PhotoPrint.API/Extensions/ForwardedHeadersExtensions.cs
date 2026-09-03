using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Observability;
using PhotoPrint.API.Validators;

namespace PhotoPrint.API.Extensions;

public static class ForwardedHeadersExtensions
{
    public static IServiceCollection AddTrustedProxyForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<ForwardedHeadersSettings>>(
            new ForwardedHeadersSettingsValidator(configuration));
        services.AddOptions<ForwardedHeadersSettings>()
            .Bind(configuration.GetSection(ForwardedHeadersSettings.SectionName))
            .ValidateOnStart();

        var trustedProxies = configuration
            .GetSection($"{ForwardedHeadersSettings.SectionName}:{nameof(ForwardedHeadersSettings.TrustedProxies)}")
            .Get<string[]>() ?? [];

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();

            var parsed = ScrapeIpAllowList.Parse(trustedProxies, out _);
            foreach (var address in parsed.Addresses)
                options.KnownProxies.Add(address);
            foreach (var network in parsed.Networks)
                options.KnownNetworks.Add(new IPNetwork(network.BaseAddress, network.PrefixLength));
        });

        return services;
    }

    public static WebApplication UseTrustedProxyForwardedHeaders(this WebApplication app)
    {
        var trustedProxies = app.Services
            .GetRequiredService<IOptions<ForwardedHeadersSettings>>().Value.TrustedProxies;

        if (trustedProxies.Length == 0)
        {
            if (app.Environment.IsProduction())
            {
                app.Logger.LogWarning(
                    "forwarded_headers.disabled — ForwardedHeaders:TrustedProxies is empty, so every "
                    + "request's client identity is its TCP peer. Behind a reverse proxy that is the "
                    + "proxy's own address for all traffic: one rate-limit partition for the whole "
                    + "internet, and an audit trail that names the proxy. See DEPLOYMENT.md §16.");
            }

            return app;
        }

        var permitLimit = app.Configuration
            .GetSection("RateLimit").Get<RateLimitSettings>()?.Public.PermitLimit ?? 0;

        app.Logger.LogInformation(
            "forwarded_headers.enabled trusted_proxies={TrustedProxies} public_permit_limit={PermitLimit} "
            + "— the client identity now comes from X-Forwarded-For, so the public rate-limit budget "
            + "applies per client for the first time",
            string.Join(", ", trustedProxies), permitLimit);

        var scrapePort = ScrapeListenerPort(app);

        app.UseWhen(
            context => scrapePort == 0 || context.Connection.LocalPort != scrapePort,
            branch => branch.UseForwardedHeaders());

        return app;
    }

    private static int ScrapeListenerPort(WebApplication app)
    {
        var observability = app.Services
            .GetRequiredService<IOptions<ObservabilitySettings>>().Value;

        return observability.Enabled ? observability.Metrics.ScrapePort : 0;
    }
}
