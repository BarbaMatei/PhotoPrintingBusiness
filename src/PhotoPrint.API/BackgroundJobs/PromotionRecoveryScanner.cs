using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.BackgroundJobs;

/// <summary>
/// Self-heal for the promote-on-paid lifecycle (ADR-010). Runs one sweep at boot, then repeats
/// every <see cref="OrderPhotoArchiveSettings.PromotionRecoverySweepIntervalHours"/>. Each sweep
/// finds orders that are paid (or beyond) but still have <c>StorageLocation = Local</c> uploads and
/// re-enqueues each onto <see cref="IPromotionQueue"/>.
/// <para>Boot-only was insufficient on an always-on server (F1, review 043-v3): a promotion that
/// exhausts <see cref="OrderPhotoArchiveSettings.MaxAttempts"/> at runtime stays Local, and its
/// original never reached the durable cloud tier until the next reboot. This mirrors the periodic
/// treatment F4 gave the purge sibling (<see cref="OriginalPurgeRecoveryScanner"/>).</para>
/// </summary>
public class PromotionRecoveryScanner : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPromotionQueue _queue;
    private readonly IStorageRouter _router;
    private readonly OrderPhotoArchiveSettings _settings;
    private readonly ILogger<PromotionRecoveryScanner> _logger;

    public PromotionRecoveryScanner(
        IServiceScopeFactory scopeFactory,
        IPromotionQueue queue,
        IStorageRouter router,
        IOptions<OrderPhotoArchiveSettings> settings,
        ILogger<PromotionRecoveryScanner> logger)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
        _router = router;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("promotion.recovery.skipped reason=archive-disabled");
            return;
        }
        if (!_router.CloudEnabled)
        {
            // Informational, not Error — the host knows its config at boot. The Error level
            // is reserved for runtime payments-fire-with-cloud-off (see OrderPhotoPromoter).
            _logger.LogInformation("promotion.recovery.skipped reason=cloud-tier-off");
            return;
        }

        _logger.LogInformation(
            "promotion.recovery.started interval_hours={Hours}",
            _settings.PromotionRecoverySweepIntervalHours);

        // Immediate boot sweep — close the crash window between webhook receipt and successful
        // promotion, then re-scan periodically for orders whose promotion later went terminal.
        await SafeSweepAsync("boot", stoppingToken);

        using var timer = new PeriodicTimer(
            TimeSpan.FromHours(_settings.PromotionRecoverySweepIntervalHours));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await SafeSweepAsync("periodic", stoppingToken);

        _logger.LogInformation("promotion.recovery.stopped");
    }

    private async Task SafeSweepAsync(string phase, CancellationToken ct)
    {
        try
        {
            await RunSweepAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutdown — stop cleanly.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "promotion.recovery.sweep.error phase={Phase}", phase);
        }
    }

    // Re-enqueues every paid-or-beyond order still holding a Local upload. Returns the count.
    // Enqueue is cheap and the worker's MaxConcurrentOrders bounds real work, so the sweep
    // enqueues the whole stuck set rather than a batch — a cap would only delay recovery.
    internal async Task<int> RunSweepAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();

        // Status filter: any post-Paid status except the terminal-failure ones. The
        // resilience contract is "if it ever reached Paid, its photos belong in the cloud."
        var stuckIds = await db.Orders
            .Where(o => o.Status == OrderStatus.Paid ||
                        o.Status == OrderStatus.Printing ||
                        o.Status == OrderStatus.Shipped ||
                        o.Status == OrderStatus.Delivered)
            .Where(o => o.Items.Any(i => i.Upload.StorageLocation == StorageLocation.Local))
            .Select(o => o.Id)
            .ToListAsync(ct);

        foreach (var id in stuckIds)
        {
            ct.ThrowIfCancellationRequested();
            await _queue.EnqueueAsync(new PromotionJob(id), ct);
        }

        _logger.LogInformation("promotion.recovery.enqueued count={Count}", stuckIds.Count);
        return stuckIds.Count;
    }
}
