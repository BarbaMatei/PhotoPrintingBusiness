namespace PhotoPrint.API.DTOs.Cart;

public record CartItemDto(
    Guid UploadId,
    int Quantity,
    string PreviewUrl,
    decimal UnitPrice,
    decimal LineTotal,
    int WidthPx,
    int HeightPx);
