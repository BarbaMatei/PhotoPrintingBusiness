namespace PhotoPrint.API.DTOs.Cart;

public record CartItemRequest(Guid UploadId, int Quantity);
