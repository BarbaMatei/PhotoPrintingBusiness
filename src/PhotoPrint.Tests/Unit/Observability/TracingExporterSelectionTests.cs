using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using PhotoPrint.API.Extensions;

namespace PhotoPrint.Tests.Unit.Observability;

/// <summary>
/// The console span exporter prints whole spans — EF SQL text and request paths — to
/// stdout, synchronously on the request thread. It is a Development convenience, and
/// an empty <c>Otlp:Endpoint</c> is the only thing that selects it, so these pin which
/// environments may reach it.
/// </summary>
public class TracingExporterSelectionTests
{
    private static ServiceProvider Build(string environmentName, string otlpEndpoint)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Observability:Enabled"]                    = "true",
                ["Observability:ServiceName"]                = "PhotoPrint.API",
                ["Observability:Otlp:Endpoint"]              = otlpEndpoint,
                ["Observability:Metrics:PrometheusEndpoint"] = "/metrics",
                ["Observability:Metrics:AllowedScrapeIps:0"] = "127.0.0.1",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(lb => lb.ClearProviders());
        services.AddObservability(
            configuration,
            Mock.Of<IHostEnvironment>(e => e.EnvironmentName == environmentName));
        return services.BuildServiceProvider();
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Testing")]
    public void Without_an_otlp_endpoint_no_trace_pipeline_is_built_outside_development(string environmentName)
    {
        using var provider = Build(environmentName, otlpEndpoint: "");

        provider.GetService<TracerProvider>().Should().BeNull(
            "spans would have nowhere to go but stdout");
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Testing")]
    public void Metrics_still_run_when_tracing_is_skipped(string environmentName)
    {
        using var provider = Build(environmentName, otlpEndpoint: "");

        provider.GetService<MeterProvider>().Should().NotBeNull(
            "the metrics-first rollout stage has no collector yet");
    }

    [Fact]
    public void Development_keeps_the_console_fallback()
    {
        using var provider = Build("Development", otlpEndpoint: "");

        provider.GetService<TracerProvider>().Should().NotBeNull();
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    public void An_otlp_endpoint_wires_tracing_in_any_environment(string environmentName)
    {
        using var provider = Build(environmentName, otlpEndpoint: "http://collector:4317");

        provider.GetService<TracerProvider>().Should().NotBeNull();
    }
}
