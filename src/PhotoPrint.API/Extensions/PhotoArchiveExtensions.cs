using Microsoft.Extensions.Options;
using PhotoPrint.API.BackgroundJobs;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Extensions;

/// <summary>
/// Wires the intent-024 promote-on-paid lifecycle (bolt 051):
/// <list type="bullet">
///   <item><see cref="IPromotionQueue"/> — in-memory channel singleton (ADR-010).</item>
///   <item><see cref="IOrderPhotoPromoter"/> — scoped (uses <see cref="Data.PhotoPrintDbContext"/>).</item>
///   <item><see cref="OrderPhotoPromotionWorker"/> — single hosted consumer.</item>
///   <item><see cref="PromotionRecoveryScanner"/> — startup self-heal (ADR-010).</item>
/// </list>
/// Settings validation fails fast at startup (<c>.ValidateOnStart()</c>) via
/// <see cref="OrderPhotoArchiveSettingsValidator"/>.
/// </summary>
public static class PhotoArchiveExtensions
{
    public static IServiceCollection AddPhotoArchive(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OrderPhotoArchiveSettings>(
            configuration.GetSection(OrderPhotoArchiveSettings.SectionName));
        services.AddSingleton<
            IValidateOptions<OrderPhotoArchiveSettings>, OrderPhotoArchiveSettingsValidator>();
        services.AddOptions<OrderPhotoArchiveSettings>().ValidateOnStart();

        // The queue is a singleton — the channel must outlive every scoped DbContext.
        services.AddSingleton<IPromotionQueue, PromotionQueue>();

        // The promoter holds a DbContext, so it must be scoped. The worker resolves it
        // via IServiceScopeFactory.CreateScope() per job (matches UploadCleanupJob pattern).
        services.AddScoped<IOrderPhotoPromoter, OrderPhotoPromoter>();

        // The recovery scanner must run before the worker. IHostedServices start in the
        // order they're registered; AddHostedService<Scanner> first guarantees the channel
        // is primed by the time the worker begins reading.
        services.AddHostedService<PromotionRecoveryScanner>();
        services.AddHostedService<OrderPhotoPromotionWorker>();

        return services;
    }
}
