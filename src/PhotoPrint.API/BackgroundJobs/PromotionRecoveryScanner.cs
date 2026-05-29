using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.BackgroundJobs;

/// <summary>
/// Startup self-heal (ADR-010). Runs once during host start; queries orders that are paid
/// (or beyond) but still have <c>StorageLocation = Local</c> uploads, and re-enqueues each
/// onto <see cref="IPromotionQueue"/>. Closes the crash window between webhook receipt and
/// successful promotion.
/// </summary>
public class PromotionRecoveryScanner : IHostedService
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

    public async Task StartAsync(CancellationToken ct)
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
            await _queue.EnqueueAsync(new PromotionJob(id), ct);

        _logger.LogInformation(
            "promotion.recovery.enqueued count={Count}", stuckIds.Count);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
