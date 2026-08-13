using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sentry;

namespace PhotoPrint.Tests.Integration;

// A test host that switches observability or Sentry on process-wide decides what every other
// host in the run boots, so which tests exercise the real SDKs depends on xUnit's scheduling
// and the suite's green is not reproducible.
[Collection(ObservabilityHostCollection.Name)]
public class TestHostConfigurationIsolationTests
{
    private static readonly string[] LeakableVariables =
    [
        "Sentry__Enabled",
        "Sentry__Dsn",
        "Observability__Enabled",
        "Observability__Metrics__AllowedScrapeIps__0",
    ];

    [Fact]
    public async Task Booting_the_observability_hosts_leaves_no_process_wide_configuration()
    {
        using (var metrics = new ObservabilityEnabledLoopbackFactory())
        {
            using var enabledClient = metrics.CreateClient();
            (await enabledClient.GetAsync("/metrics")).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var sentry = new SentryIntegrationFactory())
        {
            using var sentryClient = sentry.CreateClient();
            sentry.Services.GetService<IHub>().Should().NotBeNull();
        }

        foreach (var variable in LeakableVariables)
            Environment.GetEnvironmentVariable(variable).Should()
                .BeNull($"{variable} would configure every other test host in the process");
    }

    [Fact]
    public async Task A_default_host_booted_after_them_still_has_observability_and_sentry_off()
    {
        using (var metrics = new ObservabilityEnabledLoopbackFactory())
        using (var _ = metrics.CreateClient()) { }

        using (var sentry = new SentryIntegrationFactory())
        using (var _ = sentry.CreateClient()) { }

        using var defaults = new ObservabilityDefaultFactory();
        using var client = defaults.CreateClient();

        (await client.GetAsync("/metrics")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        defaults.Services.GetService<IHub>().Should().BeNull();
    }
}
