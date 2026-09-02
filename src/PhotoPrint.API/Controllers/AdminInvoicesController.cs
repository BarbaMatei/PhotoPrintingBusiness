using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Invoices;
using PhotoPrint.API.Extensions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Invoicing;

namespace PhotoPrint.API.Controllers;

/// <summary>
/// Admin invoice tooling (story 004): list, retry, raw XML download.
/// All operations are audit-logged via Information-level structured events
/// with the admin's user id (existing admin-controller convention).
/// </summary>
[ApiController]
[Route("api/admin/invoices")]
[Authorize(Roles = "Admin")]
public sealed class AdminInvoicesController : ControllerBase
{
    private readonly PhotoPrintDbContext _db;
    private readonly IInvoiceLifecycle _lifecycle;
    private readonly ILogger<AdminInvoicesController> _logger;

    public AdminInvoicesController(
        PhotoPrintDbContext db,
        IInvoiceLifecycle lifecycle,
        ILogger<AdminInvoicesController> logger)
    {
        _db = db;
        _lifecycle = lifecycle;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/admin/invoices?status=&page=&size= — paged list with optional status filter.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] AdminInvoiceListQuery query,
        CancellationToken ct)
    {
        var q = _db.Invoices.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Status)
            && Enum.TryParse<InvoiceAnafStatus>(query.Status, ignoreCase: true, out var parsed))
        {
            q = q.Where(i => i.AnafStatus == parsed);
        }

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(i => i.CreatedAt)
            .Skip((query.Page - 1) * query.Size)
            .Take(query.Size)
            .Join(_db.Orders.AsNoTracking(),
                i => i.OrderId,
                o => o.Id,
                (i, o) => new AdminInvoiceListItem(
                    i.Id,
                    o.Id,
                    o.OrderNumber,
                    i.InvoiceNumber,
                    i.IssuedAt,
                    i.AnafStatus.ToString(),
                    i.AnafUploadId,
                    i.LastError))
            .ToListAsync(ct);

        _logger.LogInformation(
            "admin.invoice.list admin_user_id={AdminUserId} status={Status} page={Page} size={Size} total={Total}",
            User.GetUserIdOrNull(), query.Status, query.Page, query.Size, total);

        return Ok(new
        {
            items,
            total,
            page = query.Page,
            size = query.Size,
        });
    }

    /// <summary>
    /// POST /api/admin/invoices/{id}/retry — flip <c>Rejected</c> or
    /// <c>Failed</c> back to <c>Pending</c> so the worker re-processes
    /// on its next tick. 409 on any other status.
    /// </summary>
    [HttpPost("{invoiceId:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RetryAsync(Guid invoiceId, CancellationToken ct)
    {
        var snapshot = await _db.Invoices.AsNoTracking()
            .Where(i => i.Id == invoiceId)
            .Select(i => new { i.Id, i.AnafStatus })
            .FirstOrDefaultAsync(ct);

        if (snapshot is null) return NotFound();

        if (snapshot.AnafStatus is not (InvoiceAnafStatus.Rejected or InvoiceAnafStatus.Failed))
        {
            return Conflict(new
            {
                error = "invoice-not-retryable",
                message = "Only invoices in Rejected or Failed state can be retried.",
                currentStatus = snapshot.AnafStatus.ToString(),
            });
        }

        var ok = await _lifecycle.RetryAsync(invoiceId, snapshot.AnafStatus, ct);

        if (!ok)
        {
            // Lost a race against the worker — re-read and report the
            // current state so the admin can decide.
            return Conflict(new
            {
                error = "invoice-cas-lost",
                message = "Invoice state changed between read and retry; refresh and try again.",
            });
        }

        _logger.LogInformation(
            "admin.invoice.retry admin_user_id={AdminUserId} invoice_id={InvoiceId} from={From}",
            User.GetUserIdOrNull(), invoiceId, snapshot.AnafStatus);

        return Ok(new
        {
            invoiceId,
            oldStatus = snapshot.AnafStatus.ToString(),
            newStatus = InvoiceAnafStatus.Pending.ToString(),
        });
    }

    /// <summary>
    /// GET /api/admin/invoices/{id}/xml — returns the raw UBL XML
    /// payload bytes (Admin role only). Used by ops to inspect what
    /// went to ANAF when reconciling rejections.
    /// </summary>
    [HttpGet("{invoiceId:guid}/xml")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetXmlAsync(Guid invoiceId, CancellationToken ct)
    {
        var invoice = await _db.Invoices.AsNoTracking()
            .Where(i => i.Id == invoiceId)
            .Select(i => new { i.InvoiceNumber, i.XmlPayload })
            .FirstOrDefaultAsync(ct);

        if (invoice is null || string.IsNullOrEmpty(invoice.XmlPayload))
            return NotFound();

        _logger.LogInformation(
            "admin.invoice.xml-download admin_user_id={AdminUserId} invoice_id={InvoiceId}",
            User.GetUserIdOrNull(), invoiceId);

        var bytes = Encoding.UTF8.GetBytes(invoice.XmlPayload);
        return File(bytes, "application/xml", fileDownloadName: $"{invoice.InvoiceNumber}.xml");
    }
}
