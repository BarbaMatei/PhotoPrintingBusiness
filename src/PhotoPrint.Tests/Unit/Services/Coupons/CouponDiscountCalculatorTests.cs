using FluentAssertions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using PhotoPrint.API.Services.Coupons;
using Xunit;

namespace PhotoPrint.Tests.Unit.Services.Coupons;

public class CouponDiscountCalculatorTests
{
    [Fact]
    public void Compute_Percent_TakesTheShareOfGoodsOnly_NotShipping()
    {
        var discount = CouponDiscountCalculator.Compute(
            CouponType.Percent, value: 25m, goodsGrossRon: 100.00m, shippingGrossRon: 20.00m);

        discount.Should().Be(25.00m);
    }

    [Fact]
    public void Compute_Fixed_TakesTheFaceValue()
    {
        var discount = CouponDiscountCalculator.Compute(
            CouponType.Fixed, value: 30m, goodsGrossRon: 100.00m, shippingGrossRon: 20.00m);

        discount.Should().Be(30.00m);
    }

    [Fact]
    public void Compute_FixedValueAboveSubtotal_CapsAtSubtotal()
    {
        var discount = CouponDiscountCalculator.Compute(
            CouponType.Fixed, value: 500m, goodsGrossRon: 40.00m, shippingGrossRon: 20.00m);

        discount.Should().Be(40.00m);
    }

    [Fact]
    public void Compute_PercentOfEverything_NeverExceedsTheGoodsSubtotal()
    {
        var discount = CouponDiscountCalculator.Compute(
            CouponType.Percent, value: 100m, goodsGrossRon: 40.00m, shippingGrossRon: 20.00m);

        discount.Should().Be(40.00m);
    }

    [Fact]
    public void Compute_FreeShipping_IsCappedAtPayableGross_NotGoods()
    {
        var discount = CouponDiscountCalculator.Compute(
            CouponType.FreeShipping, value: 0m, goodsGrossRon: 5.00m, shippingGrossRon: 20.00m);

        discount.Should().Be(20.00m);
    }

    [Fact]
    public void Compute_FreeShippingWithNoShippingCost_DiscountsNothing()
    {
        var discount = CouponDiscountCalculator.Compute(
            CouponType.FreeShipping, value: 0m, goodsGrossRon: 50.00m, shippingGrossRon: 0m);

        discount.Should().Be(0m);
    }

    [Fact]
    public void Compute_PercentWithHalfBani_RoundsAwayFromZero()
    {
        var discount = CouponDiscountCalculator.Compute(
            CouponType.Percent, value: 10m, goodsGrossRon: 10.05m, shippingGrossRon: 0m);

        discount.Should().Be(1.01m);
        discount.Should().NotBe(decimal.Round(10.05m * 0.10m, 2));
    }

    [Theory]
    [InlineData(-1.0, 0.0, 0.0)]
    [InlineData(0.0, -1.0, 0.0)]
    [InlineData(0.0, 0.0, -1.0)]
    public void Compute_NegativeInputs_Throw(double goods, double shipping, double value)
    {
        var act = () => CouponDiscountCalculator.Compute(
            CouponType.Fixed, (decimal)value, (decimal)goods, (decimal)shipping);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DiscountThenVat_DiffersFromVatThenDiscount_AndIsTheDeclaredFigure()
    {
        const decimal goods = 100.00m;
        const decimal shippingCost = 20.00m;
        const decimal rate = 0.19m;

        var discount = CouponDiscountCalculator.Compute(
            CouponType.Fixed, value: 30m, goodsGrossRon: goods, shippingGrossRon: shippingCost);

        var correct = VatCalculator.ExtractBreakdown(goods + shippingCost - discount, rate);
        var wrongOrder = VatCalculator.ExtractBreakdown(goods + shippingCost, rate);

        correct.VatRon.Should().Be(14.37m);
        wrongOrder.VatRon.Should().Be(19.16m);
        correct.NetTotalRon.Should().Be(75.63m);
        (correct.NetTotalRon + correct.VatRon).Should().Be(90.00m);
    }
}
