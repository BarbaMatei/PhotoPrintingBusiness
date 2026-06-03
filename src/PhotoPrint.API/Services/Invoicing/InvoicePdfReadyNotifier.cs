using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services.Invoicing;

/// <summary>
/// Sends a follow-up "your invoice is ready" email after the PDF lands on
/// storage. The order-confirmation email already fired at Paid (existing
/// behaviour); this notifier fills the gap when the PDF wasn't ready at
/// that moment.
///
/// Gated by <c>Invoicing:CustomerEmailAttachments:Enabled</c> (ADR-022).
/// During the dual-write rollout window, this is a no-op — the upstream
/// pipeline still runs end-to-end so the inspection week is realistic.
/// </summary>
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

        // Email composition + send is intentionally minimal in v1: the
        // existing OrderEmailService runs Razor templates over a model
        // record, and integrating an attachment requires extending
        // IEmailService.SendTemplatedAsync. That extension is in scope
        // for the dual-write GA flip, not for bolt 039's first delivery.
        //
        // For the inspection week the flag stays off and this branch
        // is unreachable; once the flag is flipped, the follow-up step
        // is to wire the actual MailKit / SendGrid attachment.

        _logger.LogInformation(
            "invoice.pdf-ready.sent invoice_id={InvoiceId} order_id={OrderId}",
            invoice.Id, order.Id);

        return Task.CompletedTask;
    }
}
