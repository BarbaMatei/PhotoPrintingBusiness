using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services.Invoicing;

// Placeholder for the "your invoice is ready" email: no send path exists yet, so both flag states only log.
public sealed class InvoicePdfReadyNotifier
{
    private readonly InvoicingSettings _settings;
    private readonly ILogger<InvoicePdfReadyNotifier> _logger;

    public InvoicePdfReadyNotifier(
        IOptions<InvoicingSettings> settings,
        ILogger<InvoicePdfReadyNotifier> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public Task NotifyAsync(Invoice invoice, Order order, CancellationToken ct = default)
    {
        if (!_settings.CustomerEmailAttachments.Enabled)
        {
            _logger.LogInformation(
                "invoice.pdf-ready.suppressed invoice_id={InvoiceId} reason=feature-flag-off",
                invoice.Id);
            return Task.CompletedTask;
        }

        // No email integration exists yet, so nothing is actually sent here.
        _logger.LogWarning(
            "invoice.pdf-ready.no-email-integration invoice_id={InvoiceId} order_id={OrderId} — flag enabled but no send is implemented",
            invoice.Id, order.Id);

        return Task.CompletedTask;
    }
}
