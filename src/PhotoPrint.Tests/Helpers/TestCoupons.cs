using Microsoft.Extensions.Logging.Abstractions;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Coupons;

namespace PhotoPrint.Tests.Helpers;

internal static class TestCoupons
{
    public static CouponService ServiceFor(PhotoPrintDbContext db)
        => new(db, NullLogger<CouponService>.Instance);

    public static Coupon Make(
        string code = "VARA25",
        CouponType type = CouponType.Fixed,
        decimal value = 10.00m,
        decimal minSubtotalRon = 0m,
        int? maxRedemptions = null,
        int redemptionsCount = 0,
        bool isActive = true,
        DateTimeOffset? validFrom = null,
        DateTimeOffset? validUntil = null)
        => new()
        {
            Code = code,
            Type = type,
            Value = value,
            MinSubtotalRon = minSubtotalRon,
            MaxRedemptions = maxRedemptions,
            RedemptionsCount = redemptionsCount,
            IsActive = isActive,
            ValidFrom = validFrom ?? DateTimeOffset.UtcNow.AddDays(-1),
            ValidUntil = validUntil ?? DateTimeOffset.UtcNow.AddDays(30),
        };
}
