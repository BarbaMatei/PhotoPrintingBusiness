using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services.Coupons;

public static class CouponDiscountCalculator
{
    public static decimal Compute(
        CouponType type,
        decimal value,
        decimal goodsGrossRon,
        decimal shippingGrossRon)
    {
        if (goodsGrossRon < 0m)
            throw new ArgumentOutOfRangeException(nameof(goodsGrossRon),
                "Goods gross must be non-negative.");
        if (shippingGrossRon < 0m)
            throw new ArgumentOutOfRangeException(nameof(shippingGrossRon),
                "Shipping gross must be non-negative.");
        if (value < 0m)
            throw new ArgumentOutOfRangeException(nameof(value),
                "Coupon value must be non-negative.");

        var raw = type switch
        {
            CouponType.Percent => Math.Min(
                decimal.Round(goodsGrossRon * value / 100m, 2, MidpointRounding.AwayFromZero),
                goodsGrossRon),
            CouponType.Fixed => Math.Min(
                decimal.Round(value, 2, MidpointRounding.AwayFromZero),
                goodsGrossRon),
            CouponType.FreeShipping => shippingGrossRon,
            _ => 0m,
        };

        var payableGross = goodsGrossRon + shippingGrossRon;
        return Math.Clamp(decimal.Round(raw, 2, MidpointRounding.AwayFromZero), 0m, payableGross);
    }
}
