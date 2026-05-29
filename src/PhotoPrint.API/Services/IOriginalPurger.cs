namespace PhotoPrint.API.Services;

/// <summary>
/// Deletes the cloud <b>original</b> for every upload on a given order (intent 024,
/// bolt 052, story 001). The thumbnail + large preview are retained — they fall under
/// the periodic retention sweep (story 002).
/// <para>Fired synchronously from <c>AdminOrderService.UpdateStatusAsync</c> when the
/// order enters the configured production-complete status (default <c>Shipped</c>).
/// Also fired from <c>OriginalPurgeRecoveryScanner</c> at boot to close the crash
/// window between the status transition and the purger finishing.</para>
/// <para>Per-upload idempotent: an upload whose <c>FilePath</c> is already null is
/// counted as <see cref="PurgeOutcome.Skipped"/> and not re-attempted.</para>
/// </summary>
public interface IOriginalPurger
{
    Task<PurgeOutcome> PurgeOrderOriginalsAsync(Guid orderId, CancellationToken ct = default);
}
