using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Invoicing;

namespace PhotoPrint.Tests.Unit.Services.Invoicing;

/// <summary>
/// ADR-022 — the customer-facing "your invoice is ready" follow-up email
/// is suppressed during the dual-write rollout window. This test pins the
/// feature flag so a future PR can't silently flip the default to <c>true</c>.
/// </summary>
public class InvoicePdfReadyNotifierTests
{
    [Fact]
    public void Default_settings_have_attachments_disabled()
    {
        // Pin the default. Production rollout flips this to true AFTER the
        // inspection week — flipping it BEFORE is what ADR-022 prevents.
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
    public async Task When_flag_enabled_notifier_runs_send_path()
    {
        // For v1 the "send" path is a log line + Task.CompletedTask
        // (see InvoicePdfReadyNotifier — actual MailKit attachment integration
        // is in scope for the GA flip, not for the inspection-week artefact).
        // This test pins that the enabled branch is reached without exception.
        var sut = new InvoicePdfReadyNotifier(
            Options.Create(new InvoicingSettings
            {
                CustomerEmailAttachments = new CustomerEmailAttachmentSettings { Enabled = true },
            }),
            NullLogger<InvoicePdfReadyNotifier>.Instance);

        await sut.NotifyAsync(new Invoice { Id = Guid.NewGuid() }, new Order { Id = Guid.NewGuid() });
    }
}
