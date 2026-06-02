using FluentAssertions;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Validators;

namespace PhotoPrint.Tests.Unit.Configuration;

/// <summary>
/// Verifies the IValidateOptions adapter. The validator is a no-op when
/// the integration is switched off — required so the disabled-by-default path
/// remains byte-identical to the pre-bolt baseline.
/// </summary>
public class SentrySettingsValidatorTests
{
    private readonly SentrySettingsValidator _sut = new();

    private static SentrySettings ValidEnabled() => new()
    {
        Enabled = true,
        Dsn = "https://abc@sentry.io/12345",
        SampleRate = 1.0,
        TracesSampleRate = 0.1,
    };

    [Fact]
    public void Disabled_with_blank_dsn_is_valid()
    {
        var result = _sut.Validate(null, new SentrySettings());
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Enabled_with_full_valid_settings_is_valid()
    {
        var result = _sut.Validate(null, ValidEnabled());
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Enabled_with_blank_dsn_fails()
    {
        var s = ValidEnabled();
        s.Dsn = string.Empty;
        var result = _sut.Validate(null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Sentry:Dsn"));
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://sentry.example/0")]
    [InlineData("sentry.example/0")]
    public void Enabled_with_invalid_dsn_fails(string dsn)
    {
        var s = ValidEnabled();
        s.Dsn = dsn;
        var result = _sut.Validate(null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Sentry:Dsn"));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    public void Enabled_with_sample_rate_out_of_range_fails(double rate)
    {
        var s = ValidEnabled();
        s.SampleRate = rate;
        var result = _sut.Validate(null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("SampleRate"));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    public void Enabled_with_traces_sample_rate_out_of_range_fails(double rate)
    {
        var s = ValidEnabled();
        s.TracesSampleRate = rate;
        var result = _sut.Validate(null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("TracesSampleRate"));
    }

    [Fact]
    public void Enabled_with_multiple_failures_aggregates_all()
    {
        var s = ValidEnabled();
        s.Dsn = string.Empty;
        s.SampleRate = 2.0;
        s.TracesSampleRate = -1.0;
        var result = _sut.Validate(null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCount(3);
    }

    [Fact]
    public void Disabled_skips_all_rules_even_with_garbage()
    {
        // Critical: validator is a no-op when Enabled=false. Otherwise the shipped
        // default appsettings.json (empty Dsn) would prevent boot.
        var s = new SentrySettings
        {
            Enabled = false,
            Dsn = "not-a-url",
            SampleRate = -10,
            TracesSampleRate = 99,
        };
        var result = _sut.Validate(null, s);
        result.Succeeded.Should().BeTrue();
    }
}
