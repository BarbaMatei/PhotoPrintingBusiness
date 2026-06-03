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
[Authorize]
public sealed class InvoicesController : ControllerBase
{
    private readonly PhotoPrintDbContext _db;
    private readonly IStorageService _storage;

    public InvoicesController(PhotoPrintDbContext db, IStorageService storage)
    {
        _db = db;
        _storage = storage;
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
        if (userId is null) return Unauthorized();

        var order = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new { o.Id, o.UserId })
            .FirstOrDefaultAsync(ct);

        if (order is null) return NotFound();
        if (order.UserId != userId.Value) return Forbid();

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

        var stream = await _storage.GetStreamAsync(invoice.PdfStoragePath, ct);

        Response.Headers["Cache-Control"] = "private, max-age=31536000, immutable";
        return File(
            stream,
            "application/pdf",
            fileDownloadName: $"{invoice.InvoiceNumber}.pdf");
    }
}
