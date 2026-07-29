using Microsoft.Extensions.Options;
using PhotoPrint.API.BackgroundJobs;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Services;
using PhotoPrint.API.Services.Sameday;
using PhotoPrint.API.Validators;

namespace PhotoPrint.API.Extensions;

/// <summary>
/// Registers the Sameday courier integration. Extracted from Program so the enabled-path
/// composition root (which is dormant behind <c>Sameday:Enabled=false</c> and never otherwise
/// exercised) can be resolved in a test without booting the whole host.
/// </summary>
public static class SamedayServiceCollectionExtensions
{
    public static IServiceCollection AddSamedayIntegration(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SamedaySettings>(
            configuration.GetSection(SamedaySettings.SectionName));
        services.AddSingleton<IValidateOptions<SamedaySettings>, SamedaySettingsValidator>();
        services.AddOptions<SamedaySettings>().ValidateOnStart();

        var samedayEnabled = configuration
            .GetSection(SamedaySettings.SectionName)
            .GetValue<bool>("Enabled");

        // Default: no Sameday lifecycle automation. The webhook handlers depend on
        // IAwbCreationNotifier; the Null impl keeps the integration invisible when Sameday is off
        // or only credentials are wired.
        services.AddSingleton<IAwbCreationNotifier, NullAwbCreationNotifier>();

        if (!samedayEnabled)
        {
            services.AddScoped<IShippingService, StaticShippingService>();
            return services;
        }

        services.AddSingleton<ISamedayTokenProvider, SamedayTokenProvider>();
        services.AddSingleton<ISamedayAuthenticator>(sp => sp.GetRequiredService<ISamedayClient>());
        services.AddTransient<SamedayAuthHandler>();
        services.AddTransient<SamedayResilienceHandler>();

        services
            .AddHttpClient<ISamedayClient, SamedayClient>((sp, http) =>
            {
                var s = sp.GetRequiredService<IOptions<SamedaySettings>>().Value;
                http.BaseAddress = new Uri(s.BaseUrl);
                http.Timeout = TimeSpan.FromSeconds(s.RequestTimeoutSeconds);
            })
            .AddHttpMessageHandler<SamedayAuthHandler>()
            .AddHttpMessageHandler<SamedayResilienceHandler>();

        // Concrete fallback, injected into SamedayShippingService (it delegates locker/cost reads).
        services.AddScoped<StaticShippingService>();
        services.AddScoped<IShippingService, SamedayShippingService>();

        // ── Bolt 037: AWB + tracking lifecycle jobs ───────────────────────────
        // Orthogonal flag — credentials may be wired (Sameday:Enabled=true) without yet flipping
        // the lifecycle on. See ADR-015/016.
        var samedayJobsEnabled = configuration
            .GetSection(SamedaySettings.SectionName + ":Jobs")
            .GetValue<bool>("Enabled");

        if (samedayJobsEnabled)
        {
            services.AddSingleton<IAwbJobQueue, AwbJobQueue>();
            services.AddSingleton<AwbGiveUpRegistry>();
            services.AddSingleton<TrackingStopRegistry>();
            services.AddScoped<IAwbCreator, AwbCreator>();

            // Override the default Null notifier with the real enqueuer.
            services.AddSingleton<IAwbCreationNotifier, AwbCreationNotifier>();

            services.AddHostedService<AwbDispatcher>();
            services.AddHostedService<AwbRetryJob>();
            services.AddHostedService<ShipmentTrackingJob>();
        }

        return services;
    }
}
