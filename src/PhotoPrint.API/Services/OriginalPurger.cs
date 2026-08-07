using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

/// <summary>
/// Default <see cref="IOriginalPurger"/>. Confirmed-Delete-Then-Update per upload —
/// mirror 's write rule, applied to deletes. <c>Cloud.DeleteAsync</c> succeeds,
/// then the row is updated atomically (per upload, not batched).
/// </summary>
public class OriginalPurger : IOriginalPurger
{
    private readonly IStorageRouter _router;
    private readonly PhotoPrintDbContext _db;
    private readonly ArchiveSettings _settings;
    private readonly ILogger<OriginalPurger> _logger;

    public OriginalPurger(
        IStorageRouter router,
        PhotoPrintDbContext db,
        IOptions<ArchiveSettings> settings,
        ILogger<OriginalPurger> logger)
    {
        _router = router;
        _db = db;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<PurgeOutcome> PurgeOrderOriginalsAsync(Guid orderId, CancellationToken ct = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogError(
                "purge.refused order_id={OrderId} reason=archive-disabled", orderId);
            return PurgeOutcome.Empty;
        }
        if (!_router.CloudEnabled)
        {
            _logger.LogError(
                "purge.refused order_id={OrderId} reason=cloud-tier-off", orderId);
            return PurgeOutcome.Empty;
        }

        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Upload)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            _logger.LogWarning(
                "purge.skipped order_id={OrderId} reason=order-not-found", orderId);
            return PurgeOutcome.Empty;
        }

        var uploads = order.Items.Select(i => i.Upload).Distinct().ToList();
        var outcome = PurgeOutcome.Empty;

        foreach (var upload in uploads)
        {
            ct.ThrowIfCancellationRequested();
            outcome = outcome.Add(await PurgeUploadAsync(upload, orderId, ct));
        }

        _logger.LogInformation(
            "purge.summary order_id={OrderId} purged={Purged} skipped={Skipped} failed={Failed} bytes={Bytes}",
            orderId, outcome.Purged, outcome.Skipped, outcome.Failed, outcome.BytesFreed);

        return outcome;
    }

    private async Task<PurgeOutcome> PurgeUploadAsync(Upload upload, Guid orderId, CancellationToken ct)
    {
        // Per-upload idempotency: FilePath is the only signal "original blob still exists."
        if (upload.FilePath is null)
        {
            _logger.LogDebug(
                "purge.upload.skipped upload_id={UploadId} reason=already-purged", upload.Id);
            return new PurgeOutcome(0, 1, 0, 0);
        }

        // Defence in depth: a Local upload reaching this code path means something's
        // wrong upstream (an order that hasn't been promoted shouldn't be Shipped). Don't
        // delete from the wrong tier; surface it.
        if (upload.StorageLocation != StorageLocation.Cloud)
        {
            _logger.LogWarning(
                "purge.upload.skipped upload_id={UploadId} reason=not-cloud location={Location}",
                upload.Id, upload.StorageLocation);
            return new PurgeOutcome(0, 1, 0, 0);
        }

        // A cart-reused upload can be referenced by MULTIPLE orders (checkout does not clear
        // the cart, so a payment-fail retry / double-checkout shares the Upload row). Deleting
        // the original because THIS order completed would truncate the other order's fulfilment
        // ZIP. Skip while any other order still needs the bytes
        // ({Paid, Printing} = pre-fulfilment); liveness holds because the recovery sweep keys
        // on FilePath != null and re-attempts once the blocking order resolves.
        var blockingOrderId = await _db.OrderItems
            .Where(oi => oi.UploadId == upload.Id && oi.OrderId != orderId)
            .Where(oi => oi.Order.Status == OrderStatus.Paid || oi.Order.Status == OrderStatus.Printing)
            .Select(oi => oi.OrderId)
            .FirstOrDefaultAsync(ct);
        if (blockingOrderId != Guid.Empty)
        {
            _logger.LogInformation(
                "purge.upload.skipped upload_id={UploadId} reason=shared-with-live-order blocking_order_id={BlockingOrderId}",
                upload.Id, blockingOrderId);
            return new PurgeOutcome(0, 1, 0, 0);
        }

        var oldPath = upload.FilePath;
        var sizeBytes = upload.FileSizeBytes;

        // Confirmed-Delete-Then-Update — cloud delete first.
        try
        {
            await _router.Cloud.DeleteAsync(oldPath, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "purge.upload.failed upload_id={UploadId} reason=cloud-delete-error path={Path}",
                upload.Id, oldPath);
            return new PurgeOutcome(0, 0, 1, 0);
        }

        // Atomic row update — single SaveChanges per upload.
        upload.FilePath = null;
        upload.OriginalPurgedAt = DateTimeOffset.UtcNow;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Cloud delete succeeded but the row update failed. Next sweep will see
            // FilePath != null (we didn't persist the null) and re-attempt the delete —
            // S3 DeleteObject on a missing key is a successful no-op, so the second pass
            // converges cleanly.
            _logger.LogError(ex,
                "purge.upload.failed upload_id={UploadId} reason=row-update-failed",
                upload.Id);
            return new PurgeOutcome(0, 0, 1, 0);
        }

        _logger.LogInformation(
            "OriginalPurged upload_id={UploadId} order_id={OrderId} bytes={Bytes}",
            upload.Id, orderId, sizeBytes);
        return new PurgeOutcome(1, 0, 0, sizeBytes);
    }
}
