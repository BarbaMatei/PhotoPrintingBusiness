using PhotoPrint.API.DTOs.Coupons;

namespace PhotoPrint.API.Services.Coupons;

public interface IAdminCouponService
{
    Task<(IReadOnlyList<CouponDto> Items, int Total)> ListAsync(
        string? status, int page, int size, CancellationToken ct = default);

    Task<CouponDto> CreateAsync(
        CouponCreateRequest request, Guid adminUserId, CancellationToken ct = default);

    Task<CouponDto> UpdateAsync(
        Guid id, CouponUpdateRequest request, Guid adminUserId, CancellationToken ct = default);

    Task DeactivateAsync(Guid id, Guid adminUserId, CancellationToken ct = default);

    Task<(IReadOnlyList<CouponRedemptionDto> Items, int Total)> ListRedemptionsAsync(
        Guid couponId, int page, int size, CancellationToken ct = default);
}
