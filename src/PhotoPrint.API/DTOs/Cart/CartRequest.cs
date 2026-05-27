namespace PhotoPrint.API.DTOs.Cart;

public record CartRequest(Guid ProductId, Guid SizeId, string? FinishName, IReadOnlyList<CartItemRequest> Items);
