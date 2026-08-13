namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// In-process channel for AWB creation work (bolt 037; the family — same
/// "in-process now, durable later" stance as the photo-promotion queue).
/// Singleton — the channel must outlive every scoped DbContext.
/// </summary>
public interface IAwbJobQueue
{
    ValueTask EnqueueAsync(AwbJob job, CancellationToken ct = default);
    IAsyncEnumerable<AwbJob> DequeueAllAsync(CancellationToken ct);
}
