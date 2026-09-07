namespace PhotoPrint.API.DTOs.Cart;

public record CartGroupDto(
    Guid ProductId,
    string ProductName,
    Guid SizeId,
    string SizeName,
    string? FinishName,
    IReadOnlyList<CartItemDto> Items,
    int TotalCopies,
    decimal UnitPrice,
    decimal Subtotal);

public record CartResponseDto(
    IReadOnlyList<CartGroupDto> Groups,
    decimal Subtotal,
    int ItemCount,
    string? CouponCode,
    string? CouponType,
    string? CouponStatus,
    string? CouponReason,
    decimal DiscountRon,
    decimal TotalRon,
    decimal NetTotalRon,
    decimal VatRon,
    decimal VatRate)
{
    public static CartResponseDto Empty { get; } =
        new([], 0m, 0, null, null, null, null, 0m, 0m, 0m, 0m, 0m);
}
