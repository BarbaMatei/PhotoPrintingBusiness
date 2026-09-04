using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services.Coupons;

public sealed record CouponResolution(
    Guid CouponId,
    string Code,
    CouponType Type,
    decimal DiscountRon);

public interface ICouponService
{
    Task<CouponResolution> ApplyToCartAsync(
        Guid? userId, Guid? guestSessionId, string rawCode, decimal goodsGrossRon,
        CancellationToken ct = default);

    Task ClearCartCouponAsync(Guid? userId, Guid? guestSessionId, CancellationToken ct = default);

    Task<CouponResolution?> ResolveForCartAsync(
        Guid? userId, Guid? guestSessionId, decimal goodsGrossRon, bool deleteWhenUnusable,
        CancellationToken ct = default);

    Task<CouponResolution?> ResolveForOrderAsync(
        Guid? userId, Guid? guestSessionId, decimal goodsGrossRon, decimal shippingGrossRon,
        CancellationToken ct = default);

    Task ConsumeOrThrowAsync(Guid couponId, CancellationToken ct = default);

    Task ReleaseForOrderAsync(Guid orderId, CancellationToken ct = default);

    Task TransferGuestCouponAsync(Guid userId, Guid guestSessionId, CancellationToken ct = default);
}
