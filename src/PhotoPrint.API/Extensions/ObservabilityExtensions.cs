using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Middleware;
using PhotoPrint.API.Observability;
using PhotoPrint.API.Observability.Sampling;
using PhotoPrint.API.Validators;

namespace PhotoPrint.API.Extensions;

// Observability:Enabled=false registers nothing at all — boot stays identical to a build
// without the stack, which is what makes the flag safe to ship off.
public static class ObservabilityExtensions
{
    // Without an endpoint the only exporter left prints spans — EF SQL included — to stdout.
    public static bool TracingWired(ObservabilitySettings settings, IHostEnvironment environment) =>
        !string.IsNullOrWhiteSpace(settings.Otlp.Endpoint) || environment.IsDevelopment();

    // ParentBasedSampler's remote arms are AlwaysOn/AlwaysOff, so a caller's traceparent would decide.
    public static Sampler BuildSampler(ObservabilitySamplingSettings settings) =>
        new DeterministicTraceIdSampler(settings);

    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<ObservabilitySettings>(
            configuration.GetSection(ObservabilitySettings.SectionName));
        services.AddSingleton<IValidateOptions<ObservabilitySettings>, ObservabilitySettingsValidator>();
        services.AddOptions<ObservabilitySettings>().ValidateOnStart();

        var enabled = configuration
            .GetSection(ObservabilitySettings.SectionName)
            .GetValue<bool>("Enabled");

        if (!enabled)
            return services;

        // Binding ignores keys with no property, so a deployment still carrying the removed
        // per-route table would get the default rate everywhere and never be told.
        if (configuration.GetSection($"{ObservabilitySettings.SectionName}:Sampling:Routes").Exists())
        {
            throw new InvalidOperationException(
                "Observability:Sampling:Routes is no longer supported — the sampler runs before "
                + "routing resolves an endpoint, so a per-route rate could never match. Remove the "
                + "key and set Observability:Sampling:Default, or move the per-route rate to the "
                + "collector's tail sampling.");
        }

        var settings = configuration
            .GetSection(ObservabilitySettings.SectionName)
            .Get<ObservabilitySettings>()!;

        services.AddSingleton<MetricsEndpointIpAllowListMiddleware>();
        services.AddSingleton<IHostedService, ScrapeListenerGuard>();

        var builder = services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(
                serviceName:    settings.ServiceName,
                serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0"));

        if (TracingWired(settings, environment))
        {
            builder.WithTracing(t =>
            {
                t.SetSampler(BuildSampler(settings.Sampling));
                t.AddProcessor(new ErrorOverrideProcessor());
                t.AddAspNetCoreInstrumentation(o => o.RecordException = true);
                t.AddHttpClientInstrumentation();
                t.AddEntityFrameworkCoreInstrumentation();

                if (!string.IsNullOrWhiteSpace(settings.Otlp.Endpoint))
                {
                    t.AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri(settings.Otlp.Endpoint);
                        o.Protocol = settings.Otlp.Protocol == "HttpProtobuf"
                            ? OtlpExportProtocol.HttpProtobuf
                            : OtlpExportProtocol.Grpc;
                    });
                }
                else
                {
                    t.AddConsoleExporter();
                }
            });
        }

        builder
            .WithMetrics(m =>
            {
                m.AddMeter(MetricNames.Meter);
                m.AddAspNetCoreInstrumentation();
                m.AddHttpClientInstrumentation();
                m.AddRuntimeInstrumentation();
                m.AddPrometheusExporter();
            });

        return services;
    }
}
