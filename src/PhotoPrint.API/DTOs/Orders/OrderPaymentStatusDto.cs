namespace PhotoPrint.API.DTOs.Orders;

// Narrow on purpose: a guest may read this, so it carries nothing beyond settlement state.
public record OrderPaymentStatusDto(
    Guid Id,
    string OrderNumber,
    string Status,
    decimal TotalRon,
    decimal VatRon,
    decimal VatRate,
    string DeliveryType,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt);
