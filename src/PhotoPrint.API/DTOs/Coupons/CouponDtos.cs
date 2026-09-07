namespace PhotoPrint.API.DTOs.Coupons;

public interface ICouponWriteRequest
{
    string Code { get; }
    string Type { get; }
    decimal Value { get; }
    decimal MinSubtotalRon { get; }
    DateTimeOffset ValidFrom { get; }
    DateTimeOffset ValidUntil { get; }
    int? MaxRedemptions { get; }
}

public record CouponDto(
    Guid Id,
    string Code,
    string Type,
    decimal Value,
    decimal MinSubtotalRon,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidUntil,
    int? MaxRedemptions,
    int RedemptionsCount,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record CouponCreateRequest(
    string Code,
    string Type,
    decimal Value,
    decimal MinSubtotalRon,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidUntil,
    int? MaxRedemptions) : ICouponWriteRequest;

public record CouponUpdateRequest(
    string Code,
    string Type,
    decimal Value,
    decimal MinSubtotalRon,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidUntil,
    int? MaxRedemptions,
    bool IsActive) : ICouponWriteRequest;

public record CouponRedemptionDto(
    Guid Id,
    Guid OrderId,
    string OrderNumber,
    Guid? UserId,
    decimal DiscountRon,
    DateTimeOffset RedeemedAt);
