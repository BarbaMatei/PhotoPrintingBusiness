namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// A point-in-time observation of an AWB's tracking state. Used by bolt 037's
/// <c>ShipmentTrackingJob</c>. Declared in bolt 036 so the
/// <see cref="ISamedayClient"/> interface is settled.
/// </summary>
public sealed record TrackingSnapshot(
    string AwbNumber,
    TrackingState State,
    DateTimeOffset ObservedAt,
    IReadOnlyList<TrackingEvent> History);

public sealed record TrackingEvent(
    TrackingState State,
    string Description,
    DateTimeOffset OccurredAt);
