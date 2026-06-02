namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Result of one tracking-poll tick for a single order.
/// Discriminated union — every case is a sealed subtype.
///
/// <list type="bullet">
///   <item><see cref="NoChange"/> — Sameday still reports a non-terminal state; <c>LastTrackingSyncAt</c> updated, status untouched.</item>
///   <item><see cref="Delivered"/> — Sameday reports <c>delivered</c>; CAS transition + delivery email applied.</item>
///   <item><see cref="RaceLost"/> — CAS UPDATE affected 0 rows; another writer already moved the row.</item>
///   <item><see cref="PollingStopped"/> — order older than 30 days from <c>ShippedAt</c>; one-shot warning emitted, no future polls.</item>
///   <item><see cref="Failed"/> — Sameday call failed; <c>IsTransient</c> tells the caller whether the next tick should retry.</item>
/// </list>
/// </summary>
public abstract record TrackingPollOutcome
{
    public sealed record NoChange                                : TrackingPollOutcome;
    public sealed record Delivered(DateTimeOffset DeliveredAt)   : TrackingPollOutcome;
    public sealed record RaceLost                                : TrackingPollOutcome;
    public sealed record PollingStopped(string Reason)           : TrackingPollOutcome;
    public sealed record Failed(string Reason, bool IsTransient) : TrackingPollOutcome;
}
