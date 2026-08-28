using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Extensions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Controllers;

/// <summary>
/// Customer-facing invoice access (story 003). Returns the rendered PDF,
/// not the UBL XML — customers never see the wire payload.
/// </summary>
[ApiController]
[Route("api/orders/{orderId:guid}/invoice")]
[Authorize(Policy = GuestSessionExtensions.DualAuthPolicy)]
public sealed class InvoicesController : ControllerBase
{
    private readonly PhotoPrintDbContext _db;
    private readonly IStorageRouter _storageRouter;
    private readonly AnafSettings _anafSettings;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(
        PhotoPrintDbContext db, IStorageRouter storageRouter,
        IOptions<AnafSettings> anafSettings, ILogger<InvoicesController> logger)
    {
        _db = db;
        _storageRouter = storageRouter;
        _anafSettings = anafSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/orders/{orderId}/invoice → PDF stream.
    /// 404 when the Invoice row or PDF is not yet present, carrying a
    /// <c>Retry-After</c> that matches the background poll interval — the only producer of the PDF.
    /// 403 when the caller is not the order owner.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvoiceAsync(Guid orderId, CancellationToken ct)
    {
        var userId = User.GetUserIdOrNull();
        var guestSessionId = User.GetGuestSessionIdOrNull();

        var order = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new { o.Id, o.UserId, o.GuestSessionId })
            .FirstOrDefaultAsync(ct);

        if (order is null) return NotFound();

        var owns = (userId is not null && order.UserId == userId.Value) ||
                   (guestSessionId is not null && order.GuestSessionId == guestSessionId.Value);

        // Ops must fetch customer invoices during the dual-write inspection week; the read is
        // logged because it is one person reading another person's fiscal document.
        var isAdmin = User.IsInRole("Admin");
        if (!owns && !isAdmin) return Forbid();

        if (!owns && isAdmin)
            _logger.LogInformation(
                "invoice.pdf.admin-read order_id={OrderId} admin_id={AdminId}",
                orderId, User.GetBearerUserIdOrNull());

        var invoice = await _db.Invoices
            .AsNoTracking()
            .Where(i => i.OrderId == orderId)
            .Select(i => new { i.InvoiceNumber, i.PdfStoragePath, i.StorageLocation })
            .FirstOrDefaultAsync(ct);

        if (invoice is null || string.IsNullOrEmpty(invoice.PdfStoragePath))
        {
            // The background poll is the only producer of the PDF, so a shorter hint just burns requests.
            var retryAfterSeconds = Math.Max(30, _anafSettings.PollIntervalMinutes * 60);
            Response.Headers["Retry-After"] = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
            return NotFound();
        }

        // The stamped tier is a preference, not a guarantee: Cloud is unreachable on a local-only provider.
        var stampedIsReachable =
            invoice.StorageLocation == StorageLocation.Local || _storageRouter.CloudEnabled;
        var store = stampedIsReachable
            ? _storageRouter.For(invoice.StorageLocation)
            : _storageRouter.Local;

        Stream? stream = null;
        FileNotFoundException? miss = null;
        var tiersTried = 0;

        void RecordMiss(FileNotFoundException ex)
        {
            // Keep whichever miss names a bucket-level fault: S3 maps a missing BUCKET to the same 404 as a missing key.
            if (miss?.InnerException is null) miss = ex;
        }

        try
        {
            tiersTried++;
            stream = await store.GetStreamAsync(invoice.PdfStoragePath, ct);
        }
        catch (FileNotFoundException ex)
        {
            RecordMiss(ex);
        }

        if (stream is null && stampedIsReachable && _storageRouter.CloudEnabled)
        {
            // Written before the tier was recorded, or moved since: try the other tier before giving up.
            var fallback = invoice.StorageLocation == StorageLocation.Cloud
                ? _storageRouter.Local
                : _storageRouter.Cloud;
            try
            {
                tiersTried++;
                stream = await fallback.GetStreamAsync(invoice.PdfStoragePath, ct);
                _logger.LogWarning(
                    "invoice.pdf.tier-mismatch order_id={OrderId} invoice_number={InvoiceNumber} key={Key} stamped_tier={StampedTier}",
                    orderId, invoice.InvoiceNumber, invoice.PdfStoragePath, invoice.StorageLocation);
            }
            catch (FileNotFoundException ex)
            {
                RecordMiss(ex);
            }
        }

        if (stream is null)
        {
            // Distinct from the not-yet-rendered 404 above: the key no longer resolves, so retrying cannot help.
            _logger.LogError(miss,
                "invoice.pdf.blob-missing order_id={OrderId} invoice_number={InvoiceNumber} key={Key} cloud_enabled={CloudEnabled} tiers_tried={TiersTried} miss_cause={MissCause}",
                orderId, invoice.InvoiceNumber, invoice.PdfStoragePath, _storageRouter.CloudEnabled,
                tiersTried, (miss?.InnerException ?? miss)?.Message);
            return Problem(
                title: "Invoice PDF unavailable",
                detail: "The invoice record points at a file that is no longer in storage.",
                statusCode: StatusCodes.Status404NotFound);
        }

        Response.Headers["Cache-Control"] = "private, max-age=31536000, immutable";
        return File(
            stream,
            "application/pdf",
            fileDownloadName: $"{invoice.InvoiceNumber}.pdf");
    }
}
