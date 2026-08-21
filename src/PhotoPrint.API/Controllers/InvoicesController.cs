using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.Extensions;
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
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(
        PhotoPrintDbContext db, IStorageRouter storageRouter, ILogger<InvoicesController> logger)
    {
        _db = db;
        _storageRouter = storageRouter;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/orders/{orderId}/invoice → PDF stream.
    /// 404 when the Invoice row or PDF is not yet present (carries
    /// <c>Retry-After: 30</c> per story 003 acceptance criteria).
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
        if (!owns) return Forbid();

        var invoice = await _db.Invoices
            .AsNoTracking()
            .Where(i => i.OrderId == orderId)
            .Select(i => new { i.InvoiceNumber, i.PdfStoragePath })
            .FirstOrDefaultAsync(ct);

        if (invoice is null || string.IsNullOrEmpty(invoice.PdfStoragePath))
        {
            Response.Headers["Retry-After"] = "30";
            return NotFound();
        }

        var store = _storageRouter.CloudEnabled ? _storageRouter.Cloud : _storageRouter.Local;

        Stream stream;
        try
        {
            stream = await store.GetStreamAsync(invoice.PdfStoragePath, ct);
        }
        catch (FileNotFoundException ex)
        {
            // Distinct from the not-yet-rendered 404 above: the key no longer resolves, so retrying cannot help.
            _logger.LogError(ex,
                "invoice.pdf.blob-missing order_id={OrderId} invoice_number={InvoiceNumber} key={Key} cloud_enabled={CloudEnabled}",
                orderId, invoice.InvoiceNumber, invoice.PdfStoragePath, _storageRouter.CloudEnabled);
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
