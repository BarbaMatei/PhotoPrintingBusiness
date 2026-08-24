namespace PhotoPrint.API.DTOs.Orders;

public record OrderDetailDto(
    Guid Id,
    string OrderNumber,
    string Status,
    decimal SubtotalRon,
    decimal NetTotalRon,
    decimal VatRon,
    decimal VatRate,
    decimal ShippingCostRon,
    decimal TotalRon,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    string DeliveryType,
    Guid? LockerId,
    string? LockerName,
    string? LockerAddress,
    ShippingAddressDto? ShippingAddress,
    IReadOnlyList<OrderItemDto> Items);
