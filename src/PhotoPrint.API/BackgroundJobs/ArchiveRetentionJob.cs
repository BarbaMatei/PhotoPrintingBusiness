using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.BackgroundJobs;

/// <summary>
/// Periodic retention sweep — deletes large preview + thumbnail blobs from cloud for
/// uploads whose parent order paid more than <see cref="ArchiveSettings.RetentionMonths"/>
/// months ago. The original is purged separately (bolt 052, story 001 / <see cref="OriginalPurger"/>);
/// this job is story 002. Retention anchor is <c>Order.PaidAt</c> — see ADR-012.
/// <para>Per-upload idempotent: a row with both preview keys already null is filtered out
/// by the query. Failed deletes are retried implicitly on the next tick.</para>
/// </summary>
public class ArchiveRetentionJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ArchiveSettings _settings;
    private readonly ILogger<ArchiveRetentionJob> _logger;

    public ArchiveRetentionJob(
        IServiceScopeFactory scopeFactory,
        IOptions<ArchiveSettings> settings,
        ILogger<ArchiveRetentionJob> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("retention.disabled (Archive:Enabled=false)");
            return;
        }

        _logger.LogInformation(
            "retention.started interval_hours={Hours} retention_months={Months} batch={Batch}",
            _settings.JobIntervalHours, _settings.RetentionMonths, _settings.BatchSize);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(_settings.JobIntervalHours));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var (cleaned, blobsDeleted, failed) = await SweepAsync(stoppingToken);
                _logger.LogInformation(
                    "retention.tick uploads_cleaned={Cleaned} blobs_deleted={Blobs} failed={Failed}",
                    cleaned, blobsDeleted, failed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "retention.tick.error");
            }
        }

        _logger.LogInformation("retention.stopped");
    }

    internal async Task<(int uploads, int blobs, int failed)> SweepAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        var router = scope.ServiceProvider.GetRequiredService<IStorageRouter>();

        if (!router.CloudEnabled)
        {
            _logger.LogInformation("retention.tick.skipped reason=cloud-tier-off");
            return (0, 0, 0);
        }

        var cutoff = DateTimeOffset.UtcNow.AddMonths(-_settings.RetentionMonths);

        // Anchor on Order.PaidAt — see ADR-012. Cancelled orders included naturally because
        // they had a non-null PaidAt before status changed; we don't filter by status here.
        var rows = await db.Uploads
            .Where(u => u.StorageLocation == StorageLocation.Cloud)
            .Where(u => u.LargePreviewPath != null || u.ThumbnailPath != null)
            .Where(u => db.OrderItems
                .Where(oi => oi.UploadId == u.Id)
                .Any(oi => oi.Order.PaidAt != null && oi.Order.PaidAt < cutoff))
            .OrderBy(u => u.UploadedAt)
            .Take(_settings.BatchSize)
            .ToListAsync(ct);

        var blobsDeleted = 0;
        var failed = 0;

        foreach (var u in rows)
        {
            ct.ThrowIfCancellationRequested();

            // Confirmed-Delete-Then-Update — per blob delete; we update both keys at the
            // end of the loop in a single batched SaveChanges (story 002, not story 001:
            // here the bytes being gone IS the durability boundary; row update is for
            // queryability of subsequent sweeps).
            try
            {
                if (u.LargePreviewPath is { } previewKey)
                {
                    await router.Cloud.DeleteAsync(previewKey, ct);
                    u.LargePreviewPath = null;
                    blobsDeleted++;
                }
                if (u.ThumbnailPath is { } thumbKey)
                {
                    await router.Cloud.DeleteAsync(thumbKey, ct);
                    u.ThumbnailPath = null;
                    blobsDeleted++;
                }

                _logger.LogInformation(
                    "ArchiveExpired upload_id={UploadId} retention_months={Months}",
                    u.Id, _settings.RetentionMonths);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "retention.delete-failed upload_id={UploadId}", u.Id);
                failed++;
            }
        }

        if (rows.Count > 0)
            await db.SaveChangesAsync(ct);

        return (rows.Count, blobsDeleted, failed);
    }
}
