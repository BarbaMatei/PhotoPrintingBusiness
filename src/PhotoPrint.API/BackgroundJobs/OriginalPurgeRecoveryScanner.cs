using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.BackgroundJobs;

/// <summary>
/// Backstop for story 001's original-purge. Runs one sweep at boot, then repeats every
/// <see cref="ArchiveSettings.PurgeSweepIntervalHours"/>. Each sweep finds orders at-or-past
/// the configured production-complete status whose uploads still have a non-null cloud
/// <c>FilePath</c>, and fires the purger inline for each.
/// <para>The synchronous purge on the admin status transition is the fast path; this periodic
/// sweep closes the windows it misses (F4, review 043-v1): a promotion that completes <em>after</em>
/// the Shipped transition (so the upload was still Local when the synchronous purge ran and got
/// skipped), and any purge stuck by a crash. Boot-only was insufficient on an always-on server —
/// a late-completing promotion's original would linger past its retention/GDPR window until the
/// next reboot.</para>
/// </summary>
public class OriginalPurgeRecoveryScanner : BackgroundService
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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

        _logger.LogInformation(
            "purge.recovery.started interval_hours={Hours} batch={Batch}",
            _settings.PurgeSweepIntervalHours, _settings.BatchSize);

        // Immediate boot sweep — catch up any purge stuck by a crash between the admin status
        // flush and the synchronous purge finishing.
        await SafeSweepAsync("boot", stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(_settings.PurgeSweepIntervalHours));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await SafeSweepAsync("periodic", stoppingToken);

        _logger.LogInformation("purge.recovery.stopped");
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
            _logger.LogError(ex, "purge.recovery.sweep.error phase={Phase}", phase);
        }
    }

    // Fires the purger for every order still holding an un-purged cloud original at a
    // production-complete status. Returns the number of orders processed this sweep.
    internal async Task<int> RunSweepAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        var purger = scope.ServiceProvider.GetRequiredService<IOriginalPurger>();

        var statuses = _settings.ProductionCompleteFloor();
        var orderIds = await db.Orders
            .Where(o => statuses.Contains(o.Status))
            .Where(o => o.Items.Any(i =>
                i.Upload.StorageLocation == StorageLocation.Cloud &&
                i.Upload.FilePath != null))
            .OrderBy(o => o.CreatedAt).ThenBy(o => o.Id)
            .Take(_settings.BatchSize)
            .Select(o => o.Id)
            .ToListAsync(ct);

        foreach (var id in orderIds)
        {
            ct.ThrowIfCancellationRequested();
            await purger.PurgeOrderOriginalsAsync(id, ct);
        }

        _logger.LogInformation("purge.recovery.processed count={Count}", orderIds.Count);
        return orderIds.Count;
    }
}
