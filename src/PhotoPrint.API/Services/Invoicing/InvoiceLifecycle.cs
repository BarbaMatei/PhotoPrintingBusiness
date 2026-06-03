using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services.Invoicing;

/// <summary>
/// Compare-and-swap status transitions for <see cref="Invoice"/> per ADR-016.
/// Each method is a single <c>ExecuteUpdateAsync</c> with a literal
/// <c>SetProperty</c> list — EF Core 8 translates it cleanly without
/// closure-capture surprises.
/// </summary>
public sealed class InvoiceLifecycle : IInvoiceLifecycle
{
    private readonly PhotoPrintDbContext _db;
    private readonly TimeProvider _clock;
    private readonly ILogger<InvoiceLifecycle> _logger;

    public InvoiceLifecycle(
        PhotoPrintDbContext db,
        TimeProvider clock,
        ILogger<InvoiceLifecycle> logger)
    {
        _db = db;
        _clock = clock;
        _logger = logger;
    }

    public async Task<bool> MarkSubmittedAsync(
        Guid invoiceId, string anafUploadId, CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow();
        var affected = await _db.Invoices
            .Where(i => i.Id == invoiceId && i.AnafStatus == InvoiceAnafStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.AnafStatus,   InvoiceAnafStatus.Submitted)
                .SetProperty(i => i.AnafUploadId, anafUploadId)
                .SetProperty(i => i.LastError,    (string?)null)
                .SetProperty(i => i.UpdatedAt,    (DateTimeOffset?)now),
                ct);
        return LogAndReturn(invoiceId, InvoiceAnafStatus.Pending, InvoiceAnafStatus.Submitted, affected);
    }

    public async Task<bool> RecordPendingErrorAsync(
        Guid invoiceId, string errorMessage, CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow();
        var affected = await _db.Invoices
            .Where(i => i.Id == invoiceId && i.AnafStatus == InvoiceAnafStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.LastError, (string?)errorMessage)
                .SetProperty(i => i.UpdatedAt, (DateTimeOffset?)now),
                ct);
        return LogAndReturn(invoiceId, InvoiceAnafStatus.Pending, InvoiceAnafStatus.Pending, affected);
    }

    public async Task<bool> MarkAcceptedAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow();
        var affected = await _db.Invoices
            .Where(i => i.Id == invoiceId && i.AnafStatus == InvoiceAnafStatus.Submitted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.AnafStatus, InvoiceAnafStatus.Accepted)
                .SetProperty(i => i.LastError,  (string?)null)
                .SetProperty(i => i.UpdatedAt,  (DateTimeOffset?)now),
                ct);
        return LogAndReturn(invoiceId, InvoiceAnafStatus.Submitted, InvoiceAnafStatus.Accepted, affected);
    }

    public async Task<bool> MarkRejectedAsync(
        Guid invoiceId, string errorMessage, CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow();
        var affected = await _db.Invoices
            .Where(i => i.Id == invoiceId && i.AnafStatus == InvoiceAnafStatus.Submitted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.AnafStatus, InvoiceAnafStatus.Rejected)
                .SetProperty(i => i.LastError,  (string?)errorMessage)
                .SetProperty(i => i.UpdatedAt,  (DateTimeOffset?)now),
                ct);
        return LogAndReturn(invoiceId, InvoiceAnafStatus.Submitted, InvoiceAnafStatus.Rejected, affected);
    }

    public async Task<bool> MarkFailedAsync(
        Guid invoiceId, string errorMessage, CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow();
        var affected = await _db.Invoices
            .Where(i => i.Id == invoiceId && i.AnafStatus == InvoiceAnafStatus.Submitted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.AnafStatus, InvoiceAnafStatus.Failed)
                .SetProperty(i => i.LastError,  (string?)errorMessage)
                .SetProperty(i => i.UpdatedAt,  (DateTimeOffset?)now),
                ct);
        return LogAndReturn(invoiceId, InvoiceAnafStatus.Submitted, InvoiceAnafStatus.Failed, affected);
    }

    public async Task<bool> RetryAsync(
        Guid invoiceId, InvoiceAnafStatus expected, CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow();
        var affected = await _db.Invoices
            .Where(i => i.Id == invoiceId && i.AnafStatus == expected)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.AnafStatus,   InvoiceAnafStatus.Pending)
                .SetProperty(i => i.AnafUploadId, (string?)null)
                .SetProperty(i => i.LastError,    (string?)null)
                .SetProperty(i => i.UpdatedAt,    (DateTimeOffset?)now),
                ct);
        return LogAndReturn(invoiceId, expected, InvoiceAnafStatus.Pending, affected);
    }

    private bool LogAndReturn(
        Guid invoiceId, InvoiceAnafStatus expected, InvoiceAnafStatus target, int affected)
    {
        if (affected == 0)
        {
            _logger.LogInformation(
                "invoice.lifecycle.cas-lost invoice_id={InvoiceId} expected={Expected} target={Target}",
                invoiceId, expected, target);
            return false;
        }
        _logger.LogInformation(
            "invoice.lifecycle.transition invoice_id={InvoiceId} {Expected} -> {Target}",
            invoiceId, expected, target);
        return true;
    }
}
