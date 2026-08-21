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

    public async Task<bool> RecordErrorAsync(
        Guid invoiceId, string errorMessage, InvoiceAnafStatus expected, CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow();
        var affected = await _db.Invoices
            .Where(i => i.Id == invoiceId && i.AnafStatus == expected)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.LastError, (string?)errorMessage)
                .SetProperty(i => i.UpdatedAt, (DateTimeOffset?)now),
                ct);
        return LogAndReturn(invoiceId, expected, expected, affected);
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

    public async Task<UnknownUploadOutcome> RecordUnknownUploadOutcomeAsync(
        Guid invoiceId, string errorMessage, string budgetSpentMessage, int maxOutcomes,
        CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow();
        var counted = await _db.Invoices
            .Where(i => i.Id == invoiceId && i.AnafStatus == InvoiceAnafStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.UnknownUploadOutcomes, i => i.UnknownUploadOutcomes + 1)
                .SetProperty(i => i.LastError, (string?)errorMessage)
                .SetProperty(i => i.UpdatedAt, (DateTimeOffset?)now),
                ct);

        if (counted == 0)
        {
            LogAndReturn(invoiceId, InvoiceAnafStatus.Pending, InvoiceAnafStatus.Pending, counted);
            return new UnknownUploadOutcome(0, false);
        }

        var outcomes = await _db.Invoices
            .Where(i => i.Id == invoiceId)
            .Select(i => i.UnknownUploadOutcomes)
            .FirstOrDefaultAsync(ct);

        if (outcomes < maxOutcomes) return new UnknownUploadOutcome(outcomes, false);

        var parked = await _db.Invoices
            .Where(i => i.Id == invoiceId && i.AnafStatus == InvoiceAnafStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.AnafStatus, InvoiceAnafStatus.Failed)
                .SetProperty(i => i.LastError,  (string?)budgetSpentMessage)
                .SetProperty(i => i.UpdatedAt,  (DateTimeOffset?)now),
                ct);

        return new UnknownUploadOutcome(
            outcomes,
            LogAndReturn(invoiceId, InvoiceAnafStatus.Pending, InvoiceAnafStatus.Failed, parked));
    }

    public async Task<bool> RetryAsync(
        Guid invoiceId, InvoiceAnafStatus expected, CancellationToken ct = default)
    {
        // PdfStoragePath stays untouched — clearing it would re-trigger the customer "ready" notification once that integration ships, and it plays no role in the ANAF path.
        var before = await _db.Invoices
            .Where(i => i.Id == invoiceId)
            .Select(i => new { i.XmlPayload, i.LastError })
            .FirstOrDefaultAsync(ct);
        if (before is not null)
        {
            _logger.LogInformation(
                "invoice.lifecycle.retry-clearing invoice_id={InvoiceId} last_error={LastError} xml_payload_length={XmlPayloadLength}",
                invoiceId, before.LastError, before.XmlPayload?.Length ?? 0);
        }

        var now = _clock.GetUtcNow();
        var affected = await _db.Invoices
            .Where(i => i.Id == invoiceId && i.AnafStatus == expected)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.AnafStatus,   InvoiceAnafStatus.Pending)
                .SetProperty(i => i.AnafUploadId, (string?)null)
                .SetProperty(i => i.LastError,    (string?)null)
                .SetProperty(i => i.XmlPayload,   (string?)null)
                .SetProperty(i => i.UnknownUploadOutcomes, 0)
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
