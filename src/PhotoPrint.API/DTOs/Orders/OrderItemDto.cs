namespace PhotoPrint.API.DTOs.Orders;

public record OrderItemDto(
    Guid UploadId,
    string PreviewUrl,
    string ProductName,
    string Size,
    string Finish,
    int Quantity,
    decimal UnitPriceRon,
    decimal LineTotalRon);
