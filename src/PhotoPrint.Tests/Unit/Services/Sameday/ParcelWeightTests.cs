using FluentAssertions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

public class ParcelWeightTests
{
    [Theory]
    [InlineData(1,  100)]   // 1 print × 50 + 50 = 100
    [InlineData(5,  300)]   // 5 prints × 50 + 50 = 300
    [InlineData(20, 1050)]
    public void FromOrder_applies_FR3_heuristic(int totalPrints, int expectedGrams)
    {
        var order = new Order { Items = MakeItems(totalPrints) };
        var weight = ParcelWeight.FromOrder(order);
        weight.Grams.Should().Be(expectedGrams);
    }

    [Fact]
    public void FromOrder_respects_minimum_grams_floor()
    {
        var order = new Order { Items = MakeItems(1) };
        var weight = ParcelWeight.FromOrder(order);
        weight.Grams.Should().BeGreaterThanOrEqualTo(ParcelWeight.MinimumGrams);
    }

    [Fact]
    public void Kilograms_rounds_to_three_decimals()
    {
        var order = new Order { Items = MakeItems(3) };
        var weight = ParcelWeight.FromOrder(order); // 3*50+50 = 200 g → 0.200 kg
        weight.Kilograms.Should().Be(0.200m);
    }

    [Fact]
    public void FromOrder_with_null_items_throws_ArgumentException()
    {
        var order = new Order { Items = null! };
        var act = () => ParcelWeight.FromOrder(order);
        act.Should().Throw<ArgumentException>().WithMessage("*no items*");
    }

    [Fact]
    public void FromOrder_with_empty_items_throws_ArgumentException()
    {
        var order = new Order { Items = new List<OrderItem>() };
        var act = () => ParcelWeight.FromOrder(order);
        act.Should().Throw<ArgumentException>().WithMessage("*no items*");
    }

    [Fact]
    public void FromOrder_with_zero_total_quantity_throws_ArgumentException()
    {
        var order = new Order { Items = new List<OrderItem> { Item(quantity: 0) } };
        var act = () => ParcelWeight.FromOrder(order);
        act.Should().Throw<ArgumentException>().WithMessage("*zero total prints*");
    }

    private static List<OrderItem> MakeItems(int totalPrints)
        => new() { Item(quantity: totalPrints) };

    private static OrderItem Item(int quantity) => new()
    {
        Id = Guid.NewGuid(),
        Quantity = quantity,
        UnitPriceRon = 1m,
        LineTotalRon = quantity,
        ProductSnapshot = new ProductSnapshot { ProductName = "x", Size = "x", Finish = "x" },
    };
}
