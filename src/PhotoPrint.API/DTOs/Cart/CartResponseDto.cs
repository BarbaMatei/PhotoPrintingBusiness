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
    int ItemCount)
{
    public static CartResponseDto Empty { get; } =
        new([], 0m, 0);
}
