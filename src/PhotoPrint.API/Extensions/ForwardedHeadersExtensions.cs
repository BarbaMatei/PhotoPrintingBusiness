using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Middleware;
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

        services.AddSingleton<TrustedProxyList>();
        services.AddSingleton<UntrustedForwardedPeerMiddleware>();

        services.AddOptions<ForwardedHeadersOptions>().Configure<TrustedProxyList>((options, trusted) =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();

            foreach (var address in trusted.Addresses)
                options.KnownProxies.Add(address);
            foreach (var network in trusted.Networks)
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
            if (!app.Environment.IsDevelopment())
            {
                app.Logger.LogWarning(
                    "forwarded_headers.disabled — ForwardedHeaders:TrustedProxies is empty, so every "
                    + "request's client identity is its TCP peer. Behind a reverse proxy that is the "
                    + "proxy's own address for all traffic: one rate-limit partition for the whole "
                    + "internet, an audit trail that names the proxy, and a refresh cookie without "
                    + "Secure. See DEPLOYMENT.md §16.");
            }

            return app;
        }

        var rateLimit = app.Configuration.GetSection("RateLimit").Get<RateLimitSettings>()
            ?? new RateLimitSettings();

        app.Logger.LogInformation(
            "forwarded_headers.enabled trusted_proxies={TrustedProxies} public_permit_limit={PermitLimit} "
            + "— the client identity now comes from X-Forwarded-For, so the public rate-limit budget "
            + "applies per client for the first time",
            string.Join(", ", trustedProxies), rateLimit.Public.PermitLimit);

        var scrapePort  = ScrapeListenerPort(app);
        var metricsPath = MetricsPath(app);

        app.UseWhen(
            context => !IsMetricsScrape(context, scrapePort, metricsPath),
            branch =>
            {
                branch.UseMiddleware<UntrustedForwardedPeerMiddleware>();
                branch.UseForwardedHeaders();
            });

        return app;
    }

    private static bool IsMetricsScrape(HttpContext context, int scrapePort, PathString metricsPath) =>
        scrapePort != 0
        && context.Connection.LocalPort == scrapePort
        && context.Request.Path.StartsWithSegments(metricsPath, StringComparison.OrdinalIgnoreCase);

    private static int ScrapeListenerPort(WebApplication app)
    {
        var observability = app.Services
            .GetRequiredService<IOptions<ObservabilitySettings>>().Value;

        return observability.Enabled ? observability.Metrics.ScrapePort : 0;
    }

    private static PathString MetricsPath(WebApplication app)
    {
        var configured = app.Services
            .GetRequiredService<IOptions<ObservabilitySettings>>()
            .Value.Metrics.PrometheusEndpoint;

        return string.IsNullOrWhiteSpace(configured) || !configured.StartsWith('/')
            ? "/metrics"
            : configured;
    }
}
