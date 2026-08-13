using FluentAssertions;
using PhotoPrint.API.Configuration;
using Xunit;

namespace PhotoPrint.Tests.Unit.Configuration;

/// <summary>
/// Unit tests for <see cref="OrderPhotoArchiveSettingsValidator"/>. The validator is
/// wired with <c>.ValidateOnStart</c>, so a failure here would crash the API at boot —
/// these tests guarantee the failure surface is correct rather than silent.
/// </summary>
public class OrderPhotoArchiveSettingsValidatorTests
{
    private static OrderPhotoArchiveSettings Defaults() => new();

    private readonly OrderPhotoArchiveSettingsValidator _sut = new();

    [Fact]
    public void Validate_Defaults_Succeeds()
    {
        var result = _sut.Validate(name: null, Defaults());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_NonPositiveConcurrency_Fails()
    {
        var s = Defaults();
        s.MaxConcurrentOrders = 0;

        var result = _sut.Validate(null, s);

        result.Failed.Should().BeTrue();
        result.Failures!.Should().ContainMatch("*MaxConcurrentOrders*");
    }

    [Fact]
    public void Validate_NonPositiveMaxAttempts_Fails()
    {
        var s = Defaults();
        s.MaxAttempts = -1;

        var result = _sut.Validate(null, s);

        result.Failed.Should().BeTrue();
        result.Failures!.Should().ContainMatch("*MaxAttempts*");
    }

    [Fact]
    public void Validate_EmptyBackoffArray_Fails()
    {
        var s = Defaults();
        s.BackoffSeconds = [];

        var result = _sut.Validate(null, s);

        result.Failed.Should().BeTrue();
        result.Failures!.Should().ContainMatch("*BackoffSeconds*at least one*");
    }

    [Fact]
    public void Validate_NegativeBackoffValue_Fails()
    {
        var s = Defaults();
        s.BackoffSeconds = [30, -1, 60];

        var result = _sut.Validate(null, s);

        result.Failed.Should().BeTrue();
        result.Failures!.Should().ContainMatch("*BackoffSeconds*≥ 0*");
    }

    [Fact]
    public void Validate_NonPositivePromotionRecoveryInterval_Fails()
    {
        // Added the periodic promotion-recovery sweep; its interval feeds a
        // PeriodicTimer, so <= 0 must fail fast at boot rather than crash at runtime.
        var s = Defaults();
        s.PromotionRecoverySweepIntervalHours = 0;

        var result = _sut.Validate(null, s);

        result.Failed.Should().BeTrue();
        result.Failures!.Should().ContainMatch("*PromotionRecoverySweepIntervalHours*");
    }
}
