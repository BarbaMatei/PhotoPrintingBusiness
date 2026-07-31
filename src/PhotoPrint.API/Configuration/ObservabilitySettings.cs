namespace PhotoPrint.API.Configuration;

/// <summary>
/// Configuration for the OpenTelemetry observability stack (intent 020, bolt 044).
/// Mirrors the two-stage rollout posture used elsewhere (as for Sameday and Sentry): <see cref="Enabled"/> is false by default, the SDK is
/// never wired when off, and boot is byte-identical to the pre-bolt baseline.
///
/// OTLP endpoint and any production allow-list values live in
/// <c>dotnet user-secrets</c> (dev) or environment variables (staging/prod) —
/// never in <c>appsettings.json</c>.
/// </summary>
public sealed class ObservabilitySettings
{
    public const string SectionName = "Observability";

    public bool                          Enabled     { get; set; }
    public string                        ServiceName { get; set; } = "PhotoPrint.API";
    public ObservabilityOtlpSettings     Otlp        { get; set; } = new();
    public ObservabilityMetricsSettings  Metrics     { get; set; } = new();
    public ObservabilitySamplingSettings Sampling    { get; set; } = new();
}

public sealed class ObservabilityOtlpSettings
{
    /// <summary>OTLP exporter target. Empty → console exporter used for traces (dev).</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>OTLP protocol: <c>Grpc</c> or <c>HttpProtobuf</c>.</summary>
    public string Protocol { get; set; } = "Grpc";
}

public sealed class ObservabilityMetricsSettings
{
    /// <summary>Path the Prometheus exporter binds to.</summary>
    public string PrometheusEndpoint { get; set; } = "/metrics";

    // 0 = served on every Kestrel listener (dev). Production binds a second port that the
    // TLS edge does not proxy and sets it here, so the scrape path is unreachable from outside.
    public int ScrapePort { get; set; }

    /// <summary>
    /// Addresses allowed to scrape <c>/metrics</c>: plain IPv4/IPv6 addresses or CIDR ranges.
    /// Every entry is validated at boot. Production deployments override this with the
    /// Prometheus scraper's address or its subnet.
    /// </summary>
    public string[] AllowedScrapeIps { get; set; } = ["127.0.0.1", "::1"];
}

public sealed class ObservabilitySamplingSettings
{
    /// <summary>Default sample rate for routes not listed in <see cref="Routes"/>.</summary>
    public double Default { get; set; } = 1.0;

    /// <summary>
    /// Per-route sample-rate overrides. Key shape: <c>"{METHOD} {route-template}"</c>
    /// (e.g. <c>"GET /api/products"</c>). Values in [0.0, 1.0].
    /// </summary>
    public Dictionary<string, double> Routes { get; set; } = new();
}
