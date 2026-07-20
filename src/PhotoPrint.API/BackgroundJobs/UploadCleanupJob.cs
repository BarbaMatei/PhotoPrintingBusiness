using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.BackgroundJobs;

public class UploadCleanupJob : BackgroundService
{
    private const int BatchSize = 500;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<UploadCleanupSettings> _settings;
    private readonly ILogger<UploadCleanupJob> _logger;
    private bool _loggedRetentionOnce;

    public UploadCleanupJob(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<UploadCleanupSettings> settings,
        ILogger<UploadCleanupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var (deleted, errors) = await CleanupAsync(stoppingToken);
                _logger.LogInformation(
                    "Upload cleanup: {Deleted} deleted, {Errors} file errors, batch_size={Batch}",
                    deleted, errors, BatchSize);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during upload cleanup");
            }
        }
    }

    internal async Task<(int deleted, int errors)> CleanupAsync(CancellationToken ct)
    {
        var settings = _settings.CurrentValue;

        if (!_loggedRetentionOnce)
        {
            _logger.LogInformation(
                "UploadCleanupJob effective retention — orphan_hours={OrphanHours}, referenced_days={ReferencedDays}",
                settings.OrphanRetentionHours, settings.ReferencedRetentionDays);
            _loggedRetentionOnce = true;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        var router = scope.ServiceProvider.GetRequiredService<IStorageRouter>();

        var now = DateTimeOffset.UtcNow;
        var orphanCutoff = now.AddHours(-settings.OrphanRetentionHours);
        var referencedCutoff = now.AddDays(-settings.ReferencedRetentionDays);

        var candidates = await db.Uploads
            .Where(u => u.DeletedAt == null)
            .Where(u =>
                (u.UploadedAt < orphanCutoff
                    && !db.CartItems.Any(ci => ci.UploadId == u.Id)
                    && !db.OrderItems.Any(oi => oi.UploadId == u.Id))
                || u.UploadedAt < referencedCutoff)
            .OrderBy(u => u.UploadedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        var fileErrors = 0;
        var unroutable = 0;

        foreach (var upload in candidates)
        {
            // A Cloud-located row with the cloud tier disabled is unroutable: For(Cloud) would
            // throw here — outside TryDeleteAsync and before SaveChanges — aborting the whole
            // deterministic batch and wedging cleanup (incl. local orphans) every hour
            // (F2, review 043-v3). Skip it (no soft-delete) so the batch proceeds; it is retried
            // when Storage:Provider is set back to S3.
            if (upload.StorageLocation == StorageLocation.Cloud && !router.CloudEnabled)
            {
                unroutable++;
                continue;
            }

            // Route deletes to the tier that owns this upload's bytes. A promoted (Cloud)
            // upload's blobs live in the object store; resolving the local default no-oped
            // on disk and orphaned the cloud objects with no row left to reclaim them
            // (F2, review 043-v1).
            var store = router.For(upload.StorageLocation);

            // Bolt 052: FilePath may have been nulled by the original-purge already. If
            // so, the cloud blob is gone; only the row needs the soft-delete bookkeeping.
            if (upload.FilePath is { } filePath)
                fileErrors += await TryDeleteAsync(store, filePath, "original", ct);

            // Bolt 042 adds a second persistent file per upload (the cached thumbnail).
            // Delete it too, otherwise a previewed-then-expired upload leaves its thumbnail
            // on disk forever — the row is soft-deleted so no path ever revisits it (BUG-2).
            if (upload.ThumbnailPath is not null)
                fileErrors += await TryDeleteAsync(store, upload.ThumbnailPath, "thumbnail", ct);

            // Bolt 043/051 adds a third persistent object for promoted uploads (the large
            // preview). It was never deleted here, so an aged Cloud upload leaked it
            // (F2, review 043-v1).
            if (upload.LargePreviewPath is not null)
                fileErrors += await TryDeleteAsync(store, upload.LargePreviewPath, "large-preview", ct);

            upload.DeletedAt = now;
        }

        if (unroutable > 0)
            _logger.LogWarning(
                "upload.cleanup.unroutable count={Count} reason=cloud-tier-off — Cloud-located uploads cannot be reclaimed while Storage:Provider is local; left for a later sweep",
                unroutable);

        if (candidates.Count > 0)
            await db.SaveChangesAsync(ct);

        return (candidates.Count - unroutable, fileErrors);
    }

    // Returns 1 on a delete failure (counted into fileErrors), 0 otherwise. The row is
    // soft-deleted regardless — a failed blob delete is logged for ops, not retried here.
    private async Task<int> TryDeleteAsync(
        IStorageService store, string key, string kind, CancellationToken ct)
    {
        try
        {
            await store.DeleteAsync(key, ct);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete upload {Kind} file {StoragePath}", kind, key);
            return 1;
        }
    }
}
