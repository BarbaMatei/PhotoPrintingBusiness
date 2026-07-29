using FluentAssertions;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Validators;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

/// <summary>
/// Tests for the IValidateOptions adapter. The validator is a no-op when
/// the integration is switched off — that is the intent goal: disabled-by-default
/// must remain byte-identical to the pre-bolt baseline.
/// </summary>
public class SamedaySettingsValidatorTests
{
    private readonly SamedaySettingsValidator _sut = new();

    private static SamedaySettings ValidEnabled() => new()
    {
        Enabled = true,
        BaseUrl = "https://api.sameday.ro",
        Username = "user",
        Password = "pass",
        PickupPointId = "1234",
        RequestTimeoutSeconds = 10,
    };

    [Fact]
    public void Disabled_with_everything_blank_is_valid()
    {
        var settings = new SamedaySettings();
        var result = _sut.Validate(name: null, settings);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Enabled_with_full_valid_settings_is_valid()
    {
        var result = _sut.Validate(name: null, ValidEnabled());
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Enabled_with_empty_username_fails()
    {
        var s = ValidEnabled();
        s.Username = string.Empty;
        var result = _sut.Validate(name: null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Sameday:Username"));
    }

    [Fact]
    public void Enabled_with_empty_password_fails()
    {
        var s = ValidEnabled();
        s.Password = string.Empty;
        var result = _sut.Validate(name: null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Sameday:Password"));
    }

    [Fact]
    public void Enabled_with_empty_pickup_point_fails()
    {
        var s = ValidEnabled();
        s.PickupPointId = string.Empty;
        var result = _sut.Validate(name: null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("PickupPointId"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("ftp://api.sameday.ro")]
    [InlineData("api.sameday.ro")]
    public void Enabled_with_invalid_baseurl_fails(string baseUrl)
    {
        var s = ValidEnabled();
        s.BaseUrl = baseUrl;
        var result = _sut.Validate(name: null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("BaseUrl"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(61)]
    [InlineData(999)]
    public void Enabled_with_request_timeout_out_of_range_fails(int seconds)
    {
        var s = ValidEnabled();
        s.RequestTimeoutSeconds = seconds;
        var result = _sut.Validate(name: null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("RequestTimeoutSeconds"));
    }

    [Fact]
    public void Enabled_with_multiple_failures_aggregates_all()
    {
        var s = ValidEnabled();
        s.Username = string.Empty;
        s.Password = string.Empty;
        s.PickupPointId = string.Empty;
        var result = _sut.Validate(name: null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCount(3);
    }

    // ── Bolt-037 Jobs validation ─────────────────────────────────────────────

    [Fact]
    public void Jobs_disabled_skips_every_Jobs_rule_even_when_settings_are_garbage()
    {
        var s = ValidEnabled();
        s.Jobs = new SamedayJobsSettings
        {
            Enabled = false,
            AwbRetryIntervalMinutes = 0,
            AwbGiveUpHours = 0,
            TrackingIntervalMinutes = 0,
            TrackingMaxAgeDays = 0,
            MaxConcurrentSamedayCalls = 0,
            DispatchBackoffSeconds = [],
        };

        var result = _sut.Validate(name: null, s);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Jobs_enabled_with_zero_retry_interval_fails()
    {
        var s = ValidEnabled();
        s.Jobs.Enabled = true;
        s.Jobs.AwbRetryIntervalMinutes = 0;

        var result = _sut.Validate(name: null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("AwbRetryIntervalMinutes"));
    }

    [Fact]
    public void Jobs_enabled_with_max_concurrent_above_50_fails()
    {
        var s = ValidEnabled();
        s.Jobs.Enabled = true;
        s.Jobs.MaxConcurrentSamedayCalls = 51;

        var result = _sut.Validate(name: null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("MaxConcurrentSamedayCalls"));
    }

    [Fact]
    public void Jobs_enabled_with_empty_backoff_array_fails()
    {
        var s = ValidEnabled();
        s.Jobs.Enabled = true;
        s.Jobs.DispatchBackoffSeconds = [];

        var result = _sut.Validate(name: null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("DispatchBackoffSeconds"));
    }

    [Fact]
    public void Jobs_enabled_with_negative_backoff_entry_fails()
    {
        var s = ValidEnabled();
        s.Jobs.Enabled = true;
        s.Jobs.DispatchBackoffSeconds = [30, -1, 300];

        var result = _sut.Validate(name: null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("DispatchBackoffSeconds"));
    }

    [Fact]
    public void Jobs_enabled_with_full_valid_settings_is_valid()
    {
        var s = ValidEnabled();
        s.Jobs = new SamedayJobsSettings
        {
            Enabled = true,
            AwbRetryIntervalMinutes = 60,
            AwbGiveUpHours = 24,
            TrackingIntervalMinutes = 15,
            TrackingMaxAgeDays = 30,
            MaxConcurrentSamedayCalls = 5,
            DispatchBackoffSeconds = [30, 120, 300, 900, 3600],
        };

        var result = _sut.Validate(name: null, s);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Disabled_with_blanks_skips_all_rules()
    {
        // Critical: validator is a no-op when Enabled=false. Otherwise the shipped
        // default appsettings.json (empty credentials) would prevent boot.
        var s = new SamedaySettings { Enabled = false };
        s.Username = string.Empty;
        s.Password = string.Empty;
        s.PickupPointId = string.Empty;
        s.BaseUrl = string.Empty;
        s.RequestTimeoutSeconds = 0;

        var result = _sut.Validate(name: null, s);
        result.Succeeded.Should().BeTrue();
    }
}
