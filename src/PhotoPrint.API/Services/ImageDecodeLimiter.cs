namespace PhotoPrint.API.Services;

/// <summary>
/// Process-wide bound on how many image decodes may run concurrently. A single 100 MP image
/// decodes to ~400 MB, and the per-image caps (pixel-area + allocator backstop) are per
/// decode — so a burst of concurrent first-preview requests can still exhaust memory and OOM
/// the process even though every individual image is within limits (M3, review 042-v4). This
/// caps total in-flight decode memory regardless of request rate or source IP.
/// </summary>
public sealed class ImageDecodeLimiter : IDisposable
{
    private readonly SemaphoreSlim _gate;

    public ImageDecodeLimiter(int maxConcurrentDecodes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrentDecodes, 1);
        _gate = new SemaphoreSlim(maxConcurrentDecodes, maxConcurrentDecodes);
    }

    /// <summary>Slots currently available — exposed for diagnostics/tests.</summary>
    public int AvailableSlots => _gate.CurrentCount;

    /// <summary>
    /// Waits for a decode slot and returns a handle that releases it when disposed. Honours
    /// <paramref name="ct"/> while waiting, so a cancelled/aborted request stops queueing.
    /// </summary>
    public async Task<IDisposable> AcquireAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        return new Slot(_gate);
    }

    public void Dispose() => _gate.Dispose();

    private sealed class Slot : IDisposable
    {
        private SemaphoreSlim? _gate;
        public Slot(SemaphoreSlim gate) => _gate = gate;
        public void Dispose() => Interlocked.Exchange(ref _gate, null)?.Release();
    }
}
