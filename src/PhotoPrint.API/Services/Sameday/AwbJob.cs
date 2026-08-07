namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// In-process channel payload for AWB creation work. Carries an attempt counter
/// so a failed item can be re-enqueued with a back-off (mirror of bolt 051's
/// <c>PromotionJob</c>). The order id is the only durable handle — every other
/// piece of state is re-read from <c>Orders</c> at the moment of dispatch
/// (the load-bearing re-check).
/// </summary>
public sealed record AwbJob(Guid OrderId, int Attempt, DateTimeOffset EnqueuedAt);
