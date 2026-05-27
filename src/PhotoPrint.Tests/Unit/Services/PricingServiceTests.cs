using FluentAssertions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

public class PricingServiceTests
{
    private readonly PricingService _sut = new();

    // ── GetApplicableTier ────────────────────────────────────────────────────

    [Fact]
    public void GetApplicableTier_QuantityInFirstTier_ReturnsCorrectTier()
    {
        var tiers = MakeThreeTiers();

        var result = _sut.GetApplicableTier(tiers, 5);

        result.MinQuantity.Should().Be(1);
        result.MaxQuantity.Should().Be(9);
        result.UnitPrice.Should().Be(1.20m);
    }

    [Fact]
    public void GetApplicableTier_QuantityInMiddleTier_ReturnsCorrectTier()
    {
        var tiers = MakeThreeTiers();

        var result = _sut.GetApplicableTier(tiers, 10);

        result.MinQuantity.Should().Be(10);
        result.MaxQuantity.Should().Be(49);
        result.UnitPrice.Should().Be(0.90m);
    }

    [Fact]
    public void GetApplicableTier_QuantityInOpenEndedTier_ReturnsCorrectTier()
    {
        var tiers = MakeThreeTiers();

        var result = _sut.GetApplicableTier(tiers, 999);

        result.MinQuantity.Should().Be(50);
        result.MaxQuantity.Should().BeNull();
        result.UnitPrice.Should().Be(0.70m);
    }

    [Fact]
    public void GetApplicableTier_QuantityAtExactTierBoundary_ReturnsCorrectTier()
    {
        var tiers = MakeThreeTiers();

        var result = _sut.GetApplicableTier(tiers, 49);

        result.MinQuantity.Should().Be(10);
        result.MaxQuantity.Should().Be(49);
    }

    [Fact]
    public void GetApplicableTier_SingleOpenEndedTier_AlwaysMatches()
    {
        var tiers = new List<PricingTier>
        {
            Tier(1, null, 1.00m),
        };

        _sut.GetApplicableTier(tiers, 1).UnitPrice.Should().Be(1.00m);
        _sut.GetApplicableTier(tiers, 500).UnitPrice.Should().Be(1.00m);
    }

    [Fact]
    public void GetApplicableTier_NoMatchingTier_ThrowsInvalidOperationException()
    {
        // Tiers only cover 1-9; quantity 10 has no tier
        var tiers = new List<PricingTier>
        {
            Tier(1, 9, 1.20m),
        };

        var act = () => _sut.GetApplicableTier(tiers, 10);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*10*");
    }

    // ── Calculate ────────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_MultipliesUnitPriceByQuantity()
    {
        var tier = Tier(10, 49, 0.90m);

        var (unitPrice, totalPrice, _) = _sut.Calculate(tier, 15);

        unitPrice.Should().Be(0.90m);
        totalPrice.Should().Be(13.50m);
    }

    [Fact]
    public void Calculate_BoundedTier_ProducesRangeTierLabel()
    {
        var tier = Tier(10, 49, 0.90m);

        var (_, _, label) = _sut.Calculate(tier, 15);

        label.Should().Be("10-49");
    }

    [Fact]
    public void Calculate_OpenEndedTier_ProducesPlusTierLabel()
    {
        var tier = Tier(50, null, 0.70m);

        var (_, _, label) = _sut.Calculate(tier, 100);

        label.Should().Be("50+");
    }

    [Fact]
    public void Calculate_SingleQuantity_TotalEqualUnitPrice()
    {
        var tier = Tier(1, 9, 1.20m);

        var (unitPrice, totalPrice, _) = _sut.Calculate(tier, 1);

        totalPrice.Should().Be(unitPrice);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static List<PricingTier> MakeThreeTiers() =>
    [
        Tier(1,  9,    1.20m),
        Tier(10, 49,   0.90m),
        Tier(50, null, 0.70m),
    ];

    private static PricingTier Tier(int min, int? max, decimal price) => new()
    {
        Id            = Guid.NewGuid(),
        ProductSizeId = Guid.NewGuid(),
        MinQuantity   = min,
        MaxQuantity   = max,
        UnitPrice     = price,
    };
}
