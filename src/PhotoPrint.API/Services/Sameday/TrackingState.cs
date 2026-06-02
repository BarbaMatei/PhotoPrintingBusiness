namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Normalised view of a Sameday parcel's lifecycle state. The mapping from
/// Sameday's raw vendor-specific status codes to this set lives at the
/// anti-corruption boundary inside <c>SamedayClient.GetTrackingAsync</c>
/// (bolt 037).
/// </summary>
public enum TrackingState
{
    Unknown = 0,
    Pending,
    InTransit,
    OutForDelivery,
    Delivered,
    Failed,
    Cancelled,
}
