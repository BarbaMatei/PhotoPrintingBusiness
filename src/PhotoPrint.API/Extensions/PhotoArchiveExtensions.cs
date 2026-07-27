using Microsoft.Extensions.Options;
using PhotoPrint.API.BackgroundJobs;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Extensions;

/// <summary>
/// Wires the intent-024 lifecycle:
/// <list type="bullet">
///   <item><b>Bolt 051 (promote-on-paid):</b> <see cref="IPromotionQueue"/> singleton,
///         <see cref="IOrderPhotoPromoter"/> scoped, <see cref="PromotionRecoveryScanner"/>
///         + <see cref="OrderPhotoPromotionWorker"/> hosted (ADR-010).</item>
///   <item><b>Bolt 052 (retention):</b> <see cref="IOriginalPurger"/> scoped,
///         <see cref="OriginalPurgeRecoveryScanner"/> + <see cref="ArchiveRetentionJob"/>
///         hosted. Retention anchor is <c>Order.PaidAt</c> (ADR-012).</item>
/// </list>
/// All settings fail fast at startup via <c>.ValidateOnStart()</c>.
/// </summary>
public static class PhotoArchiveExtensions
{
    public static IServiceCollection AddPhotoArchive(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Bolt 051: promote-on-paid ─────────────────────────────────────────────
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

        // Both are BackgroundServices: StartAsync returns before the boot sweep runs, so
        // registration order no longer sequences them — the channel decouples them (the worker
        // blocks on ReadAllAsync until the scanner enqueues, whenever that lands).
        services.AddHostedService<PromotionRecoveryScanner>();
        services.AddHostedService<OrderPhotoPromotionWorker>();

        // ── Bolt 052: retention (ADR-012 anchor = Order.PaidAt) ───────────────────
        services.Configure<ArchiveSettings>(
            configuration.GetSection(ArchiveSettings.SectionName));
        services.AddSingleton<IValidateOptions<ArchiveSettings>, ArchiveSettingsValidator>();
        services.AddOptions<ArchiveSettings>().ValidateOnStart();

        // Purger holds a DbContext → scoped. Called synchronously from AdminOrderService
        // (request-scoped) and inline from the recovery scanner (its own scope).
        services.AddScoped<IOriginalPurger, OriginalPurger>();

        // Periodic backstop for the synchronous admin-transition purge: one sweep at boot,
        // then every ArchiveSettings.PurgeSweepIntervalHours. Catches promotions that complete
        // after the production-complete transition (F4, review 043-v1) plus crash-stuck purges.
        services.AddHostedService<OriginalPurgeRecoveryScanner>();
        services.AddHostedService<ArchiveRetentionJob>();

        return services;
    }
}
