namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Domain shape of an AWB-creation request. Bolt 037's workflow translates
/// an <c>Order</c> into one of these before calling
/// <see cref="ISamedayClient.CreateAwbAsync"/>. Declared in bolt 036 so the
/// interface is stable.
/// </summary>
public sealed record AwbCreationRequest(
    string PickupPointId,
    string OrderNumber,
    int ServiceId,
    string? LockerSamedayId,
    string RecipientName,
    string RecipientPhone,
    string RecipientAddress,
    string RecipientCity,
    string RecipientCounty,
    string RecipientPostalCode,
    decimal ParcelWeightKg,
    int ParcelCount,
    decimal CodAmountRon,
    string? Observations);
