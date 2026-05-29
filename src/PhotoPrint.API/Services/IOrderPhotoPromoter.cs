namespace PhotoPrint.API.Services;

/// <summary>
/// Orchestrates the promote-on-paid lifecycle (intent 024, bolt 051): given an order id,
/// upload each <c>StorageLocation = Local</c> upload's bytes (original, thumbnail, large
/// preview) to the cloud tier, then flip the row to <c>StorageLocation = Cloud</c>, then
/// delete the local files (Confirmed-Write-Then-Delete — ADR-011).
/// <para>Implementations are per-upload idempotent — re-running on a fully-promoted order
/// returns an outcome with <see cref="PromotionOutcome.Skipped"/> equal to the upload count
/// and zero new work.</para>
/// </summary>
public interface IOrderPhotoPromoter
{
    /// <summary>
    /// Fire-and-forget enqueue from a webhook hot path. Returns immediately after the
    /// channel write (sub-microsecond). Logs Error and returns without enqueueing if the
    /// cloud tier is disabled — see ADR-008 §"fail loudly".
    /// </summary>
    ValueTask EnqueueAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>
    /// Synchronously promote one order's photos. Used directly by the backfill CLI and
    /// indirectly by <see cref="OrderPhotoPromotionWorker"/>. Idempotent.
    /// </summary>
    Task<PromotionOutcome> PromoteOrderAsync(Guid orderId, CancellationToken ct = default);
}
