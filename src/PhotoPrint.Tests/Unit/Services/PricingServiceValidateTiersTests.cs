using FluentAssertions;
using PhotoPrint.API.DTOs.Admin;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

public class PricingServiceValidateTiersTests
{
    private readonly PricingService _sut = new();

    // ── Valid cases ───────────────────────────────────────────────────────────

    [Fact]
    public void ValidateTiers_ValidThreeTiers_ReturnsSuccess()
    {
        var tiers = ThreeTiers(1.20m, 0.90m, 0.70m);

        var (isValid, error) = _sut.ValidateTiers(tiers);

        isValid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ValidateTiers_SingleOpenEndedTier_ReturnsSuccess()
    {
        var tiers = new List<CreatePricingTierRequest>
        {
            Tier(1, null, 1.00m),
        };

        var (isValid, _) = _sut.ValidateTiers(tiers);

        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateTiers_SamePriceAllTiers_ReturnsSuccess()
    {
        var tiers = ThreeTiers(1.00m, 1.00m, 1.00m);

        var (isValid, _) = _sut.ValidateTiers(tiers);

        isValid.Should().BeTrue();
    }

    // ── Empty / first tier rules ──────────────────────────────────────────────

    [Fact]
    public void ValidateTiers_EmptyList_ReturnsError()
    {
        var (isValid, error) = _sut.ValidateTiers([]);

        isValid.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateTiers_FirstTierMinQuantityNotOne_ReturnsError()
    {
        var tiers = new List<CreatePricingTierRequest>
        {
            Tier(2, 9, 1.20m),
            Tier(10, null, 0.90m),
        };

        var (isValid, error) = _sut.ValidateTiers(tiers);

        isValid.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    // ── Price rules ───────────────────────────────────────────────────────────

    [Fact]
    public void ValidateTiers_UnitPriceZero_ReturnsError()
    {
        var tiers = new List<CreatePricingTierRequest>
        {
            Tier(1, 9, 0m),
            Tier(10, null, 0.90m),
        };

        var (isValid, error) = _sut.ValidateTiers(tiers);

        isValid.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateTiers_LaterTierHigherPrice_ReturnsError()
    {
        var tiers = ThreeTiers(0.70m, 0.90m, 1.20m); // ascending — invalid

        var (isValid, error) = _sut.ValidateTiers(tiers);

        isValid.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    // ── Range rules ───────────────────────────────────────────────────────────

    [Fact]
    public void ValidateTiers_MaxQuantityLessThanMin_ReturnsError()
    {
        var tiers = new List<CreatePricingTierRequest>
        {
            Tier(1, 0, 1.20m), // max < min
        };

        var (isValid, error) = _sut.ValidateTiers(tiers);

        isValid.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateTiers_GapBetweenTiers_ReturnsError()
    {
        var tiers = new List<CreatePricingTierRequest>
        {
            Tier(1, 9, 1.20m),
            Tier(11, null, 0.90m), // gap: 10 is missing
        };

        var (isValid, error) = _sut.ValidateTiers(tiers);

        isValid.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateTiers_OpenEndedTierNotLast_ReturnsError()
    {
        var tiers = new List<CreatePricingTierRequest>
        {
            Tier(1, null, 1.20m),  // open-ended but not last
            Tier(10, null, 0.90m),
        };

        var (isValid, error) = _sut.ValidateTiers(tiers);

        isValid.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static List<CreatePricingTierRequest> ThreeTiers(decimal p1, decimal p2, decimal p3) =>
    [
        Tier(1,  9,    p1),
        Tier(10, 49,   p2),
        Tier(50, null, p3),
    ];

    private static CreatePricingTierRequest Tier(int min, int? max, decimal price) =>
        new() { MinQuantity = min, MaxQuantity = max, UnitPrice = price };
}
