using System.Linq.Expressions;
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
        var cloudEnabled = router.CloudEnabled;

        var now = DateTimeOffset.UtcNow;
        var orphanCutoff = now.AddHours(-settings.OrphanRetentionHours);
        var referencedCutoff = now.AddDays(-settings.ReferencedRetentionDays);

        Expression<Func<Upload, bool>> retentionExpired = u =>
            (u.UploadedAt < orphanCutoff
                && !db.CartItems.Any(ci => ci.UploadId == u.Id)
                && !db.OrderItems.Any(oi => oi.UploadId == u.Id))
            || u.UploadedAt < referencedCutoff;

        var candidates = await db.Uploads
            .Where(u => u.DeletedAt == null)
            // Exclude unroutable Cloud rows in the QUERY, not after: For(Cloud) would throw with
            // the cloud tier off, but skipping post-fetch let >=BatchSize aged Cloud rows re-fill
            // the OrderBy/Take window every sweep and starve local-orphan cleanup indefinitely
            // (D38, review 043-v5; the wedge the F2/043-v3 post-fetch skip missed at scale).
            .Where(u => cloudEnabled || u.StorageLocation != StorageLocation.Cloud)
            .Where(retentionExpired)
            .OrderBy(u => u.UploadedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        var fileErrors = 0;

        foreach (var upload in candidates)
        {
            // Route deletes to the tier that owns this upload's bytes. A promoted (Cloud)
            // upload's blobs live in the object store; resolving the local default no-oped
            // on disk and orphaned the cloud objects with no row left to reclaim them
            // (F2, review 043-v1). Cloud rows only reach here when the cloud tier is enabled
            // (excluded above otherwise), so For(Cloud) never throws.
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

        if (candidates.Count > 0)
            await db.SaveChangesAsync(ct);

        // Observability: the Cloud rows excluded above are silently out of the batch, so surface how
        // many aged Cloud uploads can't be reclaimed while the cloud tier is off (D38 keeps the F2
        // ops signal that the query filter would otherwise drop). Only runs when cloud is disabled.
        if (!cloudEnabled)
        {
            var unroutable = await db.Uploads
                .Where(u => u.DeletedAt == null && u.StorageLocation == StorageLocation.Cloud)
                .Where(retentionExpired)
                .CountAsync(ct);
            if (unroutable > 0)
                _logger.LogWarning(
                    "upload.cleanup.unroutable count={Count} reason=cloud-tier-off — Cloud-located uploads cannot be reclaimed while Storage:Provider is local; excluded from the batch so local cleanup proceeds",
                    unroutable);
        }

        return (candidates.Count, fileErrors);
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
