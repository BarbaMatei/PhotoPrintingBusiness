using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Invoicing;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Unit.Services.Invoicing;

/// <summary>
/// The customer-facing "your invoice is ready" follow-up email
/// is suppressed during the dual-write rollout window. This test pins the
/// feature flag so a future PR can't silently flip the default to <c>true</c>.
/// </summary>
public class InvoicePdfReadyNotifierTests
{
    [Fact]
    public void Default_settings_have_attachments_disabled()
    {
        // The rollout may only flip this to true after the inspection week.
        var defaults = new InvoicingSettings();
        defaults.CustomerEmailAttachments.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task When_flag_disabled_notifier_no_ops()
    {
        var sut = new InvoicePdfReadyNotifier(
            Options.Create(new InvoicingSettings
            {
                CustomerEmailAttachments = new CustomerEmailAttachmentSettings { Enabled = false },
            }),
            NullLogger<InvoicePdfReadyNotifier>.Instance);

        // The notifier method must complete cleanly without throwing or
        // calling any side-effect collaborator (none injected here).
        await sut.NotifyAsync(new Invoice { Id = Guid.NewGuid() }, new Order { Id = Guid.NewGuid() });
    }

    [Fact]
    public async Task When_flag_enabled_notifier_does_not_claim_it_sent_anything()
    {
        var logs = new LogCapture();
        var sut = new InvoicePdfReadyNotifier(
            Options.Create(new InvoicingSettings
            {
                CustomerEmailAttachments = new CustomerEmailAttachmentSettings { Enabled = true },
            }),
            logs.LoggerFor<InvoicePdfReadyNotifier>());

        await sut.NotifyAsync(new Invoice { Id = Guid.NewGuid() }, new Order { Id = Guid.NewGuid() });

        logs.Records.Should().ContainSingle(r => r.Message.StartsWith("invoice.pdf-ready.no-email-integration", StringComparison.Ordinal));
        logs.Records.Should().NotContain(r => r.Message.Contains("invoice.pdf-ready.sent", StringComparison.Ordinal));
    }
}
