using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PhotoPrint.Tests.Helpers;
using Sentry;
using Sentry.AspNetCore;

namespace PhotoPrint.Tests.Integration;

// The scrubber runs inside the real Sentry client, which a mocked IHub never reaches. These
// take the options the booted host actually configured and push an event through a real
// client, so deleting the wiring in Program.cs cannot stay green.
[Collection(ObservabilityHostCollection.Name)]
public class SentryOptionsWiringTests
{
    private const string GuestToken = "5f0c-live-guest-guid";
    private const string CustomerEmail = "ion.popescu@gmail.com";

    private static SentryAspNetCoreOptions BootedOptions(SentryIntegrationFactory factory)
    {
        using var _ = factory.CreateClient();
        return factory.Services.GetRequiredService<IOptions<SentryAspNetCoreOptions>>().Value;
    }

    [Fact]
    public async Task The_booted_host_scrubs_pii_before_the_sdk_sends_an_event()
    {
        using var factory = new SentryIntegrationFactory();
        var options = BootedOptions(factory);

        var transport = new CapturingSentryTransport();
        options.Transport = transport;
        options.AutoSessionTracking = false;

        using (var client = new SentryClient(options))
        {
            var captured = new SentryEvent(new InvalidOperationException("boom"))
            {
                Request = new SentryRequest { QueryString = $"?search={CustomerEmail}" },
            };
            captured.Request.Headers["x-guest-token"] = GuestToken;

            client.CaptureEvent(captured);
            await client.FlushAsync(TimeSpan.FromSeconds(10));
        }

        transport.Payloads.Should().ContainSingle();
        transport.Payloads[0].Should().NotContain(GuestToken).And.NotContain(CustomerEmail);
    }

    [Fact]
    public async Task The_booted_host_scrubs_pii_before_the_sdk_sends_a_transaction()
    {
        using var factory = new SentryIntegrationFactory();
        var options = BootedOptions(factory);

        var transport = new CapturingSentryTransport();
        options.Transport = transport;
        options.AutoSessionTracking = false;

        using (var client = new SentryClient(options))
        {
            client.CaptureTransaction(PiiTransaction());
            await client.FlushAsync(TimeSpan.FromSeconds(10));
        }

        transport.Payloads.Should().ContainSingle();
        transport.Payloads[0].Should().NotContain(GuestToken).And.NotContain(CustomerEmail);
    }

    [Fact]
    public async Task The_booted_host_scrubs_pii_before_the_sdk_sends_a_breadcrumb()
    {
        // Sentry's HttpClient integration copies the outbound URL into a breadcrumb verbatim, and
        // GoogleTokenValidator puts a live id_token in that query string.
        using var factory = new SentryIntegrationFactory();
        var options = BootedOptions(factory);

        var transport = new CapturingSentryTransport();
        options.Transport = transport;
        options.AutoSessionTracking = false;

        using (var client = new SentryClient(options))
        {
            var scope = new Scope(options);
            scope.AddBreadcrumb(new Breadcrumb(
                message: "GET https://oauth2.googleapis.com/tokeninfo",
                type: "http",
                data: new Dictionary<string, string>
                {
                    ["url"] = $"https://oauth2.googleapis.com/tokeninfo?id_token={GuestToken}",
                },
                category: "http"));

            client.CaptureEvent(new SentryEvent(new InvalidOperationException("boom")), scope);
            await client.FlushAsync(TimeSpan.FromSeconds(10));
        }

        transport.Payloads.Should().ContainSingle();
        transport.Payloads[0].Should().NotContain(GuestToken);
    }

    [Fact]
    public void The_booted_host_keeps_send_default_pii_off_even_when_configuration_asks_for_it()
    {
        using var factory = new SentryPiiRequestedFactory();

        BootedOptions(factory).SendDefaultPii.Should().BeFalse();
    }

    [Fact]
    public void The_booted_host_reports_sdk_failures_even_with_sentry_debug_off()
    {
        // SentryOptions.DiagnosticLogger's getter returns null whenever Debug is false, so a
        // reachable logger here is the only thing standing between a 429 quota exhaustion and
        // silence. SentryIntegrationFactory never sets Sentry:Debug, so this is the shipped default.
        using var factory = new SentryIntegrationFactory();

        var options = BootedOptions(factory);

        options.DiagnosticLogger.Should().NotBeNull(
            "a dropped-event report that nothing can log is the failure mode this guards");
        options.DiagnosticLevel.Should().Be(SentryLevel.Warning);
        options.DiagnosticLogger!.IsEnabled(SentryLevel.Warning).Should().BeTrue();
        options.DiagnosticLogger.IsEnabled(SentryLevel.Error).Should().BeTrue();
    }

    [Fact]
    public void Sentry_debug_on_lowers_the_diagnostic_level_rather_than_switching_logging_on()
    {
        using var factory = new SentryDebugRequestedFactory();

        var options = BootedOptions(factory);

        options.DiagnosticLevel.Should().Be(SentryLevel.Debug);
        options.DiagnosticLogger.Should().NotBeNull();
    }

    private static SentryTransaction PiiTransaction()
    {
        const string TraceId = "75302ac48a024bde9a3b3734a82e36c8";
        var json = $$"""
        {
          "type": "transaction",
          "event_id": "{{TraceId}}",
          "transaction": "GET /api/admin/orders",
          "start_timestamp": "2026-07-31T10:00:00Z",
          "timestamp": "2026-07-31T10:00:01Z",
          "contexts": { "trace": { "op": "http.server", "span_id": "1000000000000000", "trace_id": "{{TraceId}}" } },
          "request": {
            "url": "https://fototipar.ro/api/admin/orders",
            "query_string": "?search={{CustomerEmail}}",
            "headers": { "x-guest-token": "{{GuestToken}}" }
          }
        }
        """;

        using var document = JsonDocument.Parse(json);
        return SentryTransaction.FromJson(document.RootElement);
    }
}

internal sealed class SentryPiiRequestedFactory : SentryIntegrationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(
            new Dictionary<string, string?> { ["Sentry:SendDefaultPii"] = "true" }));
    }
}

internal sealed class SentryDebugRequestedFactory : SentryIntegrationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Sentry:Debug", "true");
    }
}
