using FluentAssertions;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Validators;

namespace PhotoPrint.Tests.Unit.Configuration;

/// <summary>
/// Pins the IValidateOptions contract for the observability stack. The
/// validator is a no-op when the integration is switched off — that's a
/// load-bearing property because the shipped default appsettings.json
/// has <c>Enabled=false</c> with empty values that would otherwise
/// fail validation and prevent boot.
/// </summary>
public class ObservabilitySettingsValidatorTests
{
    private readonly ObservabilitySettingsValidator _sut = new();

    private static ObservabilitySettings ValidEnabled() => new()
    {
        Enabled     = true,
        ServiceName = "PhotoPrint.API",
        Otlp        = new ObservabilityOtlpSettings { Endpoint = "http://collector:4317", Protocol = "Grpc" },
        Metrics     = new ObservabilityMetricsSettings
        {
            PrometheusEndpoint = "/metrics",
            AllowedScrapeIps   = ["127.0.0.1"],
        },
        Sampling    = new ObservabilitySamplingSettings { Default = 1.0 },
    };

    [Fact]
    public void Disabled_with_blank_settings_is_valid()
    {
        var result = _sut.Validate(null, new ObservabilitySettings());
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Enabled_with_full_valid_settings_is_valid()
    {
        var result = _sut.Validate(null, ValidEnabled());
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Enabled_with_empty_otlp_endpoint_is_valid_console_exporter_fallback()
    {
        var s = ValidEnabled();
        s.Otlp.Endpoint = string.Empty;
        var result = _sut.Validate(null, s);
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("collector:4317")]
    public void Enabled_with_invalid_otlp_endpoint_fails(string endpoint)
    {
        var s = ValidEnabled();
        s.Otlp.Endpoint = endpoint;
        var result = _sut.Validate(null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Observability:Otlp:Endpoint"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Tcp")]
    [InlineData("grpc")]
    public void Enabled_with_invalid_otlp_protocol_fails(string protocol)
    {
        var s = ValidEnabled();
        s.Otlp.Protocol = protocol;
        var result = _sut.Validate(null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Observability:Otlp:Protocol"));
    }

    [Fact]
    public void Enabled_with_empty_service_name_fails()
    {
        var s = ValidEnabled();
        s.ServiceName = string.Empty;
        var result = _sut.Validate(null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Observability:ServiceName"));
    }

    [Theory]
    [InlineData("metrics")]
    [InlineData("")]
    public void Enabled_with_invalid_prometheus_endpoint_fails(string endpoint)
    {
        var s = ValidEnabled();
        s.Metrics.PrometheusEndpoint = endpoint;
        var result = _sut.Validate(null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("PrometheusEndpoint"));
    }

    [Fact]
    public void Enabled_with_empty_allowed_scrape_ips_fails()
    {
        var s = ValidEnabled();
        s.Metrics.AllowedScrapeIps = [];
        var result = _sut.Validate(null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("AllowedScrapeIps"));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    public void Enabled_with_default_sample_rate_out_of_range_fails(double rate)
    {
        var s = ValidEnabled();
        s.Sampling.Default = rate;
        var result = _sut.Validate(null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Sampling:Default"));
    }

    [Fact]
    public void Enabled_with_route_rate_out_of_range_fails()
    {
        var s = ValidEnabled();
        s.Sampling.Routes = new Dictionary<string, double>
        {
            { "GET /api/products", 1.5 },
        };
        var result = _sut.Validate(null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Routes['GET /api/products']"));
    }

    [Fact]
    public void Disabled_skips_all_rules_even_with_garbage()
    {
        var s = new ObservabilitySettings
        {
            Enabled     = false,
            ServiceName = "",
            Otlp        = new ObservabilityOtlpSettings { Endpoint = "not-a-url", Protocol = "" },
            Metrics     = new ObservabilityMetricsSettings { PrometheusEndpoint = "", AllowedScrapeIps = [] },
            Sampling    = new ObservabilitySamplingSettings { Default = -5 },
        };
        var result = _sut.Validate(null, s);
        result.Succeeded.Should().BeTrue();
    }
}
