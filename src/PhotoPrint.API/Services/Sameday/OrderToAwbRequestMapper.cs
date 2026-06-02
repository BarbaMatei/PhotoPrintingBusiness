using PhotoPrint.API.Configuration;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Translates an <see cref="Order"/> aggregate into an
/// <see cref="AwbCreationRequest"/>. Single chokepoint for the
/// recipient-source rules (Easybox locker vs. courier shipping address)
/// and the parcel-weight heuristic. Throws <see cref="ArgumentException"/>
/// on invariant failure; <c>IAwbCreator</c> surfaces those as
/// <see cref="AwbCreationOutcome.GiveUp"/>.
/// </summary>
public static class OrderToAwbRequestMapper
{
    public static AwbCreationRequest ToRequest(Order order, SamedaySettings settings)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(settings.PickupPointId))
            throw new ArgumentException("Sameday:PickupPointId is not configured.", nameof(settings));

        var weight = ParcelWeight.FromOrder(order);
        var parcelCount = 1; // single-parcel model today; intent 016+ may extend

        var (recipientName, recipientPhone, recipientAddress,
             recipientCity, recipientCounty, recipientPostalCode)
            = order.DeliveryType switch
            {
                DeliveryType.Easybox => EasyboxRecipient(order),
                DeliveryType.Courier => CourierRecipient(order),
                _ => throw new ArgumentException(
                    $"Unsupported DeliveryType '{order.DeliveryType}' for AWB creation.",
                    nameof(order)),
            };

        return new AwbCreationRequest(
            PickupPointId:       settings.PickupPointId,
            RecipientName:       recipientName,
            RecipientPhone:      recipientPhone,
            RecipientAddress:    recipientAddress,
            RecipientCity:       recipientCity,
            RecipientCounty:     recipientCounty,
            RecipientPostalCode: recipientPostalCode,
            ParcelWeightKg:      weight.Kilograms,
            ParcelCount:         parcelCount,
            CodAmountRon:        0m,
            Observations:        $"Order #{order.OrderNumber}");
    }

    private static (string, string, string, string, string, string) EasyboxRecipient(Order order)
    {
        if (order.EasyboxLocker is null)
            throw new ArgumentException(
                "Order delivery is Easybox but no EasyboxLocker nav property is loaded.", nameof(order));

        // Recipient name/phone come from the shipping-address snapshot (the
        // human who ordered); the *address* part is the locker.
        var addr = order.ShippingAddress
            ?? throw new ArgumentException("Order is missing ShippingAddress.", nameof(order));

        return (
            addr.RecipientName,
            addr.Phone,
            order.EasyboxLocker.Address,
            order.EasyboxLocker.City,
            order.EasyboxLocker.County,
            "000000"  /* Sameday locker drop-offs don't require a postal code; use a stable sentinel */);
    }

    private static (string, string, string, string, string, string) CourierRecipient(Order order)
    {
        var addr = order.ShippingAddress
            ?? throw new ArgumentException("Order is missing ShippingAddress.", nameof(order));

        var street = string.IsNullOrWhiteSpace(addr.Block)
            ? $"{addr.Street} {addr.Number}"
            : $"{addr.Street} {addr.Number}, {addr.Block}";

        return (
            addr.RecipientName,
            addr.Phone,
            street.Trim(),
            addr.City,
            addr.County,
            addr.PostalCode);
    }
}
