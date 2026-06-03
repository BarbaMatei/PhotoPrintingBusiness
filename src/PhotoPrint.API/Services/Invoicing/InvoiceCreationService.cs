using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services.Invoicing;

/// <summary>
/// Inserts the per-order <see cref="Invoice"/> row at the Paid transition.
/// Snapshots the VAT breakdown bolt 038 staged onto <see cref="Order"/>
/// (NetTotalRon, VatRon, TotalRon, VatRate); the invoice is then a frozen
/// legal artefact independent of later order mutations.
/// </summary>
public sealed class InvoiceCreationService : IInvoiceCreationService
{
    private readonly PhotoPrintDbContext _db;
    private readonly IInvoiceNumberingService _numbering;
    private readonly VatSettings _vatSettings;
    private readonly TimeProvider _clock;
    private readonly ILogger<InvoiceCreationService> _logger;

    public InvoiceCreationService(
        PhotoPrintDbContext db,
        IInvoiceNumberingService numbering,
        IOptions<VatSettings> vatSettings,
        TimeProvider clock,
        ILogger<InvoiceCreationService> logger)
    {
        _db = db;
        _numbering = numbering;
        _vatSettings = vatSettings.Value;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Invoice?> CreateForOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        // Idempotency: replay path (Stripe webhook re-delivered, bolt 035).
        var existing = await _db.Invoices.FirstOrDefaultAsync(i => i.OrderId == orderId, ct);
        if (existing is not null)
        {
            _logger.LogInformation(
                "invoice.creation.idempotent-replay order_id={OrderId} invoice_id={InvoiceId}",
                orderId, existing.Id);
            return existing;
        }

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
        {
            _logger.LogWarning(
                "invoice.creation.order-missing order_id={OrderId}", orderId);
            return null;
        }

        var issuedAt = order.PaidAt ?? _clock.GetUtcNow();
        var number = await _numbering.NextNumberAsync(
            _vatSettings.InvoiceSeries, issuedAt.Year, ct);

        var invoice = new Invoice
        {
            OrderId       = order.Id,
            InvoiceNumber = number.ToString(),
            Series        = number.Series,
            Number        = number.Number,
            IssuedAt      = issuedAt,
            NetTotalRon   = order.NetTotalRon,
            VatRon        = order.VatRon,
            TotalRon      = order.TotalRon,
            AnafStatus    = InvoiceAnafStatus.Pending,
            CreatedAt     = _clock.GetUtcNow(),
        };

        _db.Invoices.Add(invoice);

        _logger.LogInformation(
            "invoice.creation.allocated order_id={OrderId} invoice_number={InvoiceNumber}",
            orderId, invoice.InvoiceNumber);

        return invoice;
    }
}
