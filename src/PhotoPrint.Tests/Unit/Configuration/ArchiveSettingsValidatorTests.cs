using FluentAssertions;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Models;
using Xunit;

namespace PhotoPrint.Tests.Unit.Configuration;

/// <summary>
/// Unit tests for <see cref="ArchiveSettingsValidator"/>. Wired with
/// <c>.ValidateOnStart</c> — a failure surface here would crash the API at boot.
/// </summary>
public class ArchiveSettingsValidatorTests
{
    private static ArchiveSettings Defaults() => new();
    private readonly ArchiveSettingsValidator _sut = new();

    [Fact]
    public void Validate_Defaults_Succeeds()
    {
        var result = _sut.Validate(name: null, Defaults());

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("Shipped")]
    [InlineData("Delivered")]
    [InlineData("shipped")]    // case-insensitive
    [InlineData("DELIVERED")]
    public void Validate_AllowedStatuses_Succeed(string status)
    {
        var s = Defaults();
        s.PurgeOriginalAtStatus = status;

        var result = _sut.Validate(null, s);

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("Paid")]
    [InlineData("Printing")]
    [InlineData("Cancelled")]
    [InlineData("PaymentFailed")]
    [InlineData("")]
    [InlineData("BogusStatus")]
    public void Validate_DisallowedStatuses_Fail(string status)
    {
        var s = Defaults();
        s.PurgeOriginalAtStatus = status;

        var result = _sut.Validate(null, s);

        result.Failed.Should().BeTrue();
        result.Failures!.Should().ContainMatch("*PurgeOriginalAtStatus*");
    }

    [Fact]
    public void Validate_NonPositiveRetentionMonths_Fails()
    {
        var s = Defaults();
        s.RetentionMonths = 0;

        var result = _sut.Validate(null, s);

        result.Failed.Should().BeTrue();
        result.Failures!.Should().ContainMatch("*RetentionMonths*");
    }

    [Fact]
    public void Validate_NonPositiveJobInterval_Fails()
    {
        var s = Defaults();
        s.JobIntervalHours = -1;

        var result = _sut.Validate(null, s);

        result.Failed.Should().BeTrue();
        result.Failures!.Should().ContainMatch("*JobIntervalHours*");
    }

    [Fact]
    public void Validate_NonPositiveBatchSize_Fails()
    {
        var s = Defaults();
        s.BatchSize = 0;

        var result = _sut.Validate(null, s);

        result.Failed.Should().BeTrue();
        result.Failures!.Should().ContainMatch("*BatchSize*");
    }

    [Fact]
    public void Validate_NonPositivePurgeSweepInterval_Fails()
    {
        // The PurgeSweepIntervalHours <= 0 rule shipped without a test, so
        // dropping it would boot fine and then crash at `new PeriodicTimer(TimeSpan.Zero)` at runtime.
        var s = Defaults();
        s.PurgeSweepIntervalHours = 0;

        var result = _sut.Validate(null, s);

        result.Failed.Should().BeTrue();
        result.Failures!.Should().ContainMatch("*PurgeSweepIntervalHours*");
    }

    // ── IsProductionCompleteStatus + ProductionCompleteFloor helpers ─────────

    [Fact]
    public void IsProductionCompleteStatus_DefaultsToShipped()
    {
        var s = Defaults();

        s.IsProductionCompleteStatus(OrderStatus.Shipped).Should().BeTrue();
        s.IsProductionCompleteStatus(OrderStatus.Delivered).Should().BeFalse();
        s.IsProductionCompleteStatus(OrderStatus.Paid).Should().BeFalse();
    }

    [Fact]
    public void IsProductionCompleteStatus_ConfiguredDelivered_OnlyDeliveredMatches()
    {
        var s = Defaults();
        s.PurgeOriginalAtStatus = "Delivered";

        s.IsProductionCompleteStatus(OrderStatus.Delivered).Should().BeTrue();
        s.IsProductionCompleteStatus(OrderStatus.Shipped).Should().BeFalse();
    }

    [Fact]
    public void ProductionCompleteFloor_DefaultIncludesShippedAndDelivered()
    {
        var s = Defaults();

        s.ProductionCompleteFloor().Should().BeEquivalentTo(
            new[] { OrderStatus.Shipped, OrderStatus.Delivered });
    }

    [Fact]
    public void ProductionCompleteFloor_DeliveredOnlyConfig()
    {
        var s = Defaults();
        s.PurgeOriginalAtStatus = "Delivered";

        s.ProductionCompleteFloor().Should().BeEquivalentTo(new[] { OrderStatus.Delivered });
    }
}
