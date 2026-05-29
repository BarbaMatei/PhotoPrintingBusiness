using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Extensions;

/// <summary>
/// Wires the two-tier storage layer (bolt 043, ADR-008):
/// <list type="bullet">
///   <item>Local adapter — always registered, keyed <c>"local"</c>.</item>
///   <item>S3 adapter + <see cref="IAmazonS3"/> + <see cref="S3BucketVerifier"/> — only when <c>Storage:Provider=S3</c>, keyed <c>"cloud"</c>.</item>
///   <item><see cref="IStorageRouter"/> — resolves per-upload via <see cref="Models.StorageLocation"/>.</item>
/// </list>
/// </summary>
public static class StorageExtensions
{
    public static IServiceCollection AddPhotoStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<StorageSettings>(configuration.GetSection(StorageSettings.SectionName));
        services.AddSingleton<IValidateOptions<StorageSettings>, StorageSettingsValidator>();
        services.AddOptions<StorageSettings>().ValidateOnStart();

        // Local adapter — always available (pre-payment bytes live here).
        services.AddKeyedSingleton<IStorageService, LocalStorageService>("local");

        // Cloud adapter — only when the cloud tier is enabled.
        var settings = configuration.GetSection(StorageSettings.SectionName).Get<StorageSettings>()
            ?? new StorageSettings();

        if (settings.IsCloudEnabled)
        {
            services.AddSingleton<IAmazonS3>(sp =>
            {
                var s = sp.GetRequiredService<IOptions<StorageSettings>>().Value;
                var cfg = new AmazonS3Config
                {
                    // Cloudflare R2 wants the region as the literal string "auto"; the SDK
                    // accepts that via AuthenticationRegion. AWS-native paths set
                    // RegionEndpoint instead (a real region name).
                    AuthenticationRegion = s.Region,
                };

                if (!string.IsNullOrEmpty(s.EndpointUrl))
                {
                    // R2 / MinIO path — custom endpoint + path-style addressing.
                    cfg.ServiceURL = s.EndpointUrl;
                    cfg.ForcePathStyle = s.ForcePathStyle;
                }
                else
                {
                    // AWS native — discover RegionEndpoint from the configured region name.
                    cfg.RegionEndpoint = RegionEndpoint.GetBySystemName(s.Region);
                }

                var creds = new BasicAWSCredentials(s.AccessKey, s.SecretKey);
                return new AmazonS3Client(creds, cfg);
            });

            services.AddKeyedSingleton<IStorageService, S3StorageService>("cloud");
            services.AddHostedService<S3BucketVerifier>();
        }

        // Per-upload routing front-door (ADR-008).
        services.AddSingleton<IStorageRouter, StorageRouter>();

        // Legacy default registration — resolves to the local adapter. Kept so any caller
        // that still injects IStorageService directly (e.g. ImageProcessor's interim wiring,
        // tests' FakeStorageService swap-in) gets a working store. Two-tier callers should
        // use IStorageRouter instead.
        services.AddSingleton<IStorageService>(sp =>
            sp.GetRequiredKeyedService<IStorageService>("local"));

        return services;
    }
}
