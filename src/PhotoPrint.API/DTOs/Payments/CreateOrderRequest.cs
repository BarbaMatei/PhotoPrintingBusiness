using PhotoPrint.API.Models;

namespace PhotoPrint.API.DTOs.Payments;

/// <summary>
/// Carries delivery/shipping details when creating an order from the current cart.
/// </summary>
public record CreateOrderRequest(
    DeliveryType DeliveryType,
    Guid? EasyboxLockerId,
    ShippingAddressSnapshot? ShippingAddress
);
