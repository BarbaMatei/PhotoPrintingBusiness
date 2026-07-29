using PhotoPrint.API.DTOs.Orders;

namespace PhotoPrint.API.DTOs.Admin;

public record AdminOrderSummaryDto(
    Guid Id,
    string OrderNumber,
    string Status,
    string CustomerEmail,
    string CustomerName,
    decimal TotalRon,
    DateTimeOffset CreatedAt,
    int ItemCount,
    string DeliveryType);

public record AdminOrderItemDto(
    Guid UploadId,
    string ProductName,
    string Size,
    string Finish,
    int Quantity,
    decimal UnitPriceRon,
    decimal LineTotalRon);

public record AdminOrderDetailDto(
    Guid Id,
    string OrderNumber,
    string Status,
    string CustomerEmail,
    string CustomerName,
    decimal SubtotalRon,
    decimal ShippingCostRon,
    decimal TotalRon,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    string DeliveryType,
    string? LockerName,
    string? LockerAddress,
    ShippingAddressDto? ShippingAddress,
    string PaymentProcessor,
    string? PaymentIntentId,
    string? EuPlatescTransactionId,
    string? AwbNumber,
    string? TrackingUrl,
    string? AwbLabelUrl,
    DateTimeOffset? ShippedAt,
    DateTimeOffset? DeliveredAt,
    string? InternalNotes,
    IReadOnlyList<AdminOrderItemDto> Items);
