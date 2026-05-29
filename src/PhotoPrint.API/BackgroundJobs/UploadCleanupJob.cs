using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
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
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();

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

        foreach (var upload in candidates)
        {
            // Bolt 052: FilePath may have been nulled by the original-purge already. If
            // so, the cloud blob is gone; only the row needs the soft-delete bookkeeping.
            if (upload.FilePath is { } filePath)
            {
                try
                {
                    await storage.DeleteAsync(filePath, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete upload file {StoragePath}", filePath);
                    fileErrors++;
                }
            }

            upload.DeletedAt = now;
        }

        if (candidates.Count > 0)
            await db.SaveChangesAsync(ct);

        return (candidates.Count, fileErrors);
    }
}
