using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

/// <summary>
/// Default <see cref="IOrderPhotoPromoter"/> — the orchestrator described in
/// <c>memory-bank/bolts/051-order-photo-promotion/ddd-02-technical-design.md</c>.
/// Per-upload atomic, Confirmed-Write-Then-Delete (ADR-011).
/// </summary>
public class OrderPhotoPromoter : IOrderPhotoPromoter
{
    private readonly IPromotionQueue _queue;
    private readonly IStorageRouter _router;
    private readonly IImageProcessor _imageProcessor;
    private readonly PhotoPrintDbContext _db;
    private readonly OrderPhotoArchiveSettings _settings;
    private readonly ILogger<OrderPhotoPromoter> _logger;

    public OrderPhotoPromoter(
        IPromotionQueue queue,
        IStorageRouter router,
        IImageProcessor imageProcessor,
        PhotoPrintDbContext db,
        IOptions<OrderPhotoArchiveSettings> settings,
        ILogger<OrderPhotoPromoter> logger)
    {
        _queue = queue;
        _router = router;
        _imageProcessor = imageProcessor;
        _db = db;
        _settings = settings.Value;
        _logger = logger;
    }

    public async ValueTask EnqueueAsync(Guid orderId, CancellationToken ct = default)
    {
        // ADR-008 §"fail loudly": refuse to enqueue when the cloud tier is off. A paid
        // order whose photos can't be archived is the silent-data-loss case we want to
        // catch in the log dashboard before a customer asks for their photos in 6 months.
        if (!_settings.Enabled)
        {
            _logger.LogError(
                "promotion.enqueue.refused order_id={OrderId} reason=archive-disabled", orderId);
            return;
        }
        if (!_router.CloudEnabled)
        {
            _logger.LogError(
                "promotion.enqueue.refused order_id={OrderId} reason=cloud-tier-off", orderId);
            return;
        }

        await _queue.EnqueueAsync(new PromotionJob(orderId), ct);
        _logger.LogInformation("promotion.enqueued order_id={OrderId}", orderId);
    }

    public async Task<PromotionOutcome> PromoteOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogError("promotion.refused order_id={OrderId} reason=archive-disabled", orderId);
            return PromotionOutcome.Empty;
        }
        if (!_router.CloudEnabled)
        {
            _logger.LogError("promotion.refused order_id={OrderId} reason=cloud-tier-off", orderId);
            return PromotionOutcome.Empty;
        }

        // Load order + items + uploads in one round trip. AsSplitQuery would also work;
        // the default join-includes are fine here because the collection cardinality is
        // bounded by an order's upload count (typically < 20).
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Upload)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            _logger.LogWarning("promotion.skipped order_id={OrderId} reason=order-not-found", orderId);
            return PromotionOutcome.Empty;
        }
        if (order.Status < OrderStatus.Paid || order.Status == OrderStatus.PaymentFailed ||
            order.Status == OrderStatus.Cancelled)
        {
            _logger.LogWarning(
                "promotion.skipped order_id={OrderId} status={Status} reason=not-paid",
                orderId, order.Status);
            return PromotionOutcome.Empty;
        }

        // Distinct() in case a future order shape duplicates the same upload across line items.
        var uploads = order.Items.Select(i => i.Upload).Distinct().ToList();
        var outcome = PromotionOutcome.Empty;

        foreach (var upload in uploads)
        {
            ct.ThrowIfCancellationRequested();
            outcome = outcome.Add(await PromoteUploadAsync(upload, ct));
        }

        _logger.LogInformation(
            "promotion.summary order_id={OrderId} promoted={Promoted} skipped={Skipped} failed={Failed} bytes={Bytes}",
            orderId, outcome.Promoted, outcome.Skipped, outcome.Failed, outcome.TotalBytes);

        return outcome;
    }

    // ─── Per-upload work ──────────────────────────────────────────────────────

    private async Task<PromotionOutcome> PromoteUploadAsync(Upload upload, CancellationToken ct)
    {
        // ADR-011 §"Idempotent per-upload": short-circuit on already-promoted rows.
        if (upload.StorageLocation == StorageLocation.Cloud)
        {
            _logger.LogDebug(
                "promotion.upload.skipped upload_id={UploadId} reason=already-cloud", upload.Id);
            return new PromotionOutcome(0, 1, 0, 0);
        }

        // Step 1: read source bytes from local. Failure = missing local file → mark Failed.
        byte[] sourceBytes;
        try
        {
            await using var srcStream = await _router.Local.GetStreamAsync(upload.FilePath, ct);
            using var buf = new MemoryStream();
            await srcStream.CopyToAsync(buf, ct);
            sourceBytes = buf.ToArray();
        }
        catch (FileNotFoundException)
        {
            _logger.LogWarning(
                "promotion.upload.failed upload_id={UploadId} reason=local-original-missing path={Path}",
                upload.Id, upload.FilePath);
            return new PromotionOutcome(0, 0, 1, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "promotion.upload.failed upload_id={UploadId} reason=local-read-error path={Path}",
                upload.Id, upload.FilePath);
            return new PromotionOutcome(0, 0, 1, 0);
        }

        // Capture old keys for the post-update local cleanup — the row about to change.
        var oldFilePath = upload.FilePath;
        var oldThumbPath = upload.ThumbnailPath;

        // Step 2: write the three cloud objects. Any exception escaping here aborts the
        // upload (no row update happens → next pass redoes the cloud writes; S3 PUT at the
        // same key is idempotent so re-uploading the same bytes has no effect).
        var thumbKey = StorageKeys.Thumbnail(upload.Id);
        var previewKey = StorageKeys.Preview(upload.Id);

        try
        {
            // a) Original — same key it already has locally; cloud is the new home.
            await _router.Cloud.SaveAsync(new MemoryStream(sourceBytes), oldFilePath, ct);

            // b) Thumbnail — prefer the existing local thumb to avoid re-decoding the source.
            //    Regenerate inline if the upload was paid before its first preview hit (story Q4).
            await using (var thumbStream = await GetOrGenerateThumbnailAsync(upload, sourceBytes, ct))
                await _router.Cloud.SaveAsync(thumbStream, thumbKey, ct);

            // c) Large preview — always generated (no local source exists for this key).
            await using (var previewStream = await _imageProcessor.GenerateLargePreviewAsync(
                new MemoryStream(sourceBytes), ct))
                await _router.Cloud.SaveAsync(previewStream, previewKey, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "promotion.upload.failed upload_id={UploadId} reason=cloud-write-error",
                upload.Id);
            return new PromotionOutcome(0, 0, 1, 0);
        }

        // Step 3: the durability boundary — flip the row. After this returns, the cloud
        // bytes are canonical and the local files become "litter that may still exist."
        upload.ThumbnailPath = thumbKey;
        upload.LargePreviewPath = previewKey;
        upload.StorageLocation = StorageLocation.Cloud;
        // upload.FilePath is unchanged — same key, new tier.
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Cloud writes succeeded but the row update failed. Next pass will re-upload
            // (PUT same key, no harm) and try the row update again. We do not Promoted-count
            // because the durability boundary wasn't crossed.
            _logger.LogError(ex,
                "promotion.upload.failed upload_id={UploadId} reason=row-update-failed",
                upload.Id);
            return new PromotionOutcome(0, 0, 1, 0);
        }

        // Step 4: best-effort local deletes (litter cleanup). Logged on failure but does
        // not affect the upload's Promoted status. Local files at the same key still exist
        // on disk even though the row now points cloud-ward — they are litter to clean up.
        await BestEffortDeleteLocalAsync(oldFilePath, upload.Id, "original", ct);
        if (!string.IsNullOrEmpty(oldThumbPath))
            await BestEffortDeleteLocalAsync(oldThumbPath, upload.Id, "thumbnail", ct);

        _logger.LogInformation(
            "promotion.upload.promoted upload_id={UploadId} bytes={Bytes}",
            upload.Id, upload.FileSizeBytes);
        return new PromotionOutcome(1, 0, 0, upload.FileSizeBytes);
    }

    private async Task<Stream> GetOrGenerateThumbnailAsync(
        Upload upload, byte[] sourceBytes, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(upload.ThumbnailPath) &&
            await _router.Local.ExistsAsync(upload.ThumbnailPath, ct))
        {
            return await _router.Local.GetStreamAsync(upload.ThumbnailPath, ct);
        }

        // Inline regeneration — covers the "paid without previewing" corner case.
        return await _imageProcessor.GenerateThumbnailAsync(new MemoryStream(sourceBytes), ct);
    }

    private async Task BestEffortDeleteLocalAsync(
        string key, Guid uploadId, string kind, CancellationToken ct)
    {
        try
        {
            await _router.Local.DeleteAsync(key, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "promotion.upload.local-delete-failed upload_id={UploadId} kind={Kind} key={Key}",
                uploadId, kind, key);
        }
    }
}
