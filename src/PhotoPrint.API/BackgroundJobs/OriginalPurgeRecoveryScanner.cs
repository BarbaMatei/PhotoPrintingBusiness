using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.BackgroundJobs;

/// <summary>
/// Startup self-heal for story 001's original-purge. Runs once on host start; finds
/// orders at-or-past the configured production-complete status whose uploads still have
/// a non-null cloud <c>FilePath</c>, and fires the purger inline for each.
/// <para>Closes the crash window between <c>AdminOrderService.UpdateStatusAsync</c>
/// flushing the Shipped row and <c>OriginalPurger</c> finishing. Mirrors bolt-051's
/// <see cref="PromotionRecoveryScanner"/>.</para>
/// </summary>
public class OriginalPurgeRecoveryScanner : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IStorageRouter _router;
    private readonly ArchiveSettings _settings;
    private readonly ILogger<OriginalPurgeRecoveryScanner> _logger;

    public OriginalPurgeRecoveryScanner(
        IServiceScopeFactory scopeFactory,
        IStorageRouter router,
        IOptions<ArchiveSettings> settings,
        ILogger<OriginalPurgeRecoveryScanner> logger)
    {
        _scopeFactory = scopeFactory;
        _router = router;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("purge.recovery.skipped reason=archive-disabled");
            return;
        }
        if (!_router.CloudEnabled)
        {
            _logger.LogInformation("purge.recovery.skipped reason=cloud-tier-off");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        var purger = scope.ServiceProvider.GetRequiredService<IOriginalPurger>();

        var statuses = _settings.ProductionCompleteFloor();
        var orderIds = await db.Orders
            .Where(o => statuses.Contains(o.Status))
            .Where(o => o.Items.Any(i =>
                i.Upload.StorageLocation == StorageLocation.Cloud &&
                i.Upload.FilePath != null))
            .Select(o => o.Id)
            .ToListAsync(ct);

        foreach (var id in orderIds)
        {
            ct.ThrowIfCancellationRequested();
            await purger.PurgeOrderOriginalsAsync(id, ct);
        }

        _logger.LogInformation(
            "purge.recovery.processed count={Count}", orderIds.Count);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
