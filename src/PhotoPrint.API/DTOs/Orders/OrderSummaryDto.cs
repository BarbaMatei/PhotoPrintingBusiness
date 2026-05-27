namespace PhotoPrint.API.DTOs.Orders;

public record OrderSummaryDto(
    Guid Id,
    string OrderNumber,
    string Status,
    decimal TotalRon,
    DateTimeOffset CreatedAt,
    string DeliveryType,
    int ItemCount);
