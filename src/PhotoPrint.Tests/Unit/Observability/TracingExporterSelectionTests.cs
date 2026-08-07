using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using PhotoPrint.API.Extensions;
using PhotoPrint.API.Observability.Sampling;

namespace PhotoPrint.Tests.Unit.Observability;

// An empty Otlp:Endpoint is the only thing that selects the console span exporter, which
// prints EF SQL text to stdout on the request thread — so pin which environments reach it.
public class TracingExporterSelectionTests
{
    private static ServiceProvider Build(
        string environmentName, string otlpEndpoint, string? staleRouteKey = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Observability:Enabled"]                    = "true",
            ["Observability:ServiceName"]                = "PhotoPrint.API",
            ["Observability:Otlp:Endpoint"]              = otlpEndpoint,
            ["Observability:Metrics:PrometheusEndpoint"] = "/metrics",
            ["Observability:Metrics:AllowedScrapeIps:0"] = "127.0.0.1",
        };
        if (staleRouteKey is not null)
            values[$"Observability:Sampling:Routes:{staleRouteKey}"] = "0.05";

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

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

    [Fact]
    public void The_production_pipeline_installs_our_sampler_without_a_parent_based_wrapper()
    {
        using var provider = Build("Production", "http://collector:4317");

        var sampler = InstalledSampler(provider.GetRequiredService<TracerProvider>());

        sampler.Should().BeOfType<DeterministicTraceIdSampler>(
            "re-wrapping the call site as new ParentBasedSampler(BuildSampler(...)) hands an "
                + "inbound traceparent the sampling decision again, and the pipeline tests build "
                + "through the seam so they cannot see it");
    }

    private static object? InstalledSampler(TracerProvider tracerProvider)
    {
        var type = tracerProvider.GetType();
        const BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        var member = (MemberInfo?)type.GetProperty("Sampler", Flags)
                     ?? type.GetField("Sampler", Flags);

        member.Should().NotBeNull(
            $"this pin reads the installed sampler off {type.Name}; if the SDK renames it the "
                + "test has to be rewritten rather than quietly stop checking");

        return member is PropertyInfo property
            ? property.GetValue(tracerProvider)
            : ((FieldInfo)member!).GetValue(tracerProvider);
    }

    [Fact]
    public void A_leftover_per_route_rate_aborts_boot_instead_of_being_ignored()
    {
        var act = () => Build("Production", "http://collector:4317", staleRouteKey: "GET /api/products");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Sampling:Routes*no longer supported*");
    }
}
