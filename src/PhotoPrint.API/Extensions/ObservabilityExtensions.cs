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

/// <summary>
/// Wires the OpenTelemetry tracing + metrics pipeline behind the
/// <c>Observability:Enabled</c> master flag. With the flag off, this method
/// returns without registering anything — boot is byte-identical to the
/// pre-bolt baseline.
///
/// Layering (per <c>ddd-02-technical-design.md</c>):
///   Tracing:  ASP.NET / HttpClient / EF Core auto-instrumentation
///             → ParentBased(RouteAwareSampler) → ErrorOverrideProcessor
///             → OTLP exporter (if endpoint set) or console exporter (dev)
///   Metrics:  AddMeter(FotoMetrics.Meter) + runtime + ASP.NET / HttpClient
///             → Prometheus exporter (always when enabled)
/// </summary>
public static class ObservabilityExtensions
{
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration)
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

        var settings = configuration
            .GetSection(ObservabilitySettings.SectionName)
            .Get<ObservabilitySettings>()!;

        services.AddSingleton<MetricsEndpointIpAllowListMiddleware>();

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(
                serviceName:    settings.ServiceName,
                serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0"))
            .WithTracing(t =>
            {
                t.SetSampler(new ParentBasedSampler(new RouteAwareSampler(settings.Sampling)));
                t.AddProcessor(new ErrorOverrideProcessor());
                t.AddAspNetCoreInstrumentation(o => o.RecordException = true);
                t.AddHttpClientInstrumentation();
                t.AddEntityFrameworkCoreInstrumentation(o => o.SetDbStatementForText = true);

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
                    // Dev fallback — visible spans in stdout when no collector
                    // is wired. Production deployments always set Otlp:Endpoint.
                    t.AddConsoleExporter();
                }
            })
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
