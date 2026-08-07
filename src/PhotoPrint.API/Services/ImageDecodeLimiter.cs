namespace PhotoPrint.API.Services;

/// <summary>
/// Process-wide bound on how many image decodes may run concurrently. A single 100 MP image
/// decodes to ~400 MB, and the per-image caps (pixel-area + allocator backstop) are per
/// decode — so a burst of concurrent first-preview requests can still exhaust memory and OOM
/// the process even though every individual image is within limits. This
/// caps total in-flight decode memory regardless of request rate or source IP.
/// </summary>
public sealed class ImageDecodeLimiter : IDisposable
{
    /// <summary>
    /// Worst-case memory a single decode may hold: the ImageSharp allocation backstop
    /// (Program.cs, 512 MB). A ~100 MP RGBA decode is ~400 MB and sits under this. Sizing the
    /// default slot count off this value keeps <c>slots × 512 MB ≤ available RAM</c>, so the
    /// summed in-flight decode memory can't exceed what the host has.
    /// </summary>
    public const long PerDecodeMemoryBudgetBytes = 512L * 1024 * 1024;

    private readonly SemaphoreSlim _gate;

    public ImageDecodeLimiter(int maxConcurrentDecodes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrentDecodes, 1);
        _gate = new SemaphoreSlim(maxConcurrentDecodes, maxConcurrentDecodes);
    }

    /// <summary>
    /// Default concurrent-decode ceiling when no explicit config is supplied. Bounds by *both*
    /// CPU (decode is CPU-bound) *and* memory (<see cref="PerDecodeMemoryBudgetBytes"/> per slot),
    /// taking the smaller — so a high-core / low-RAM host (e.g. 8 cores, 2 GB) no longer defaults
    /// to ProcessorCount slots whose summed decode memory OOM-kills the process. Always at least 1.
    /// </summary>
    public static int RecommendedMaxConcurrentDecodes(
        long availableMemoryBytes,
        int processorCount,
        long perDecodeMemoryBudgetBytes = PerDecodeMemoryBudgetBytes)
    {
        var byMemory = availableMemoryBytes / perDecodeMemoryBudgetBytes;
        var slots = Math.Min(processorCount, byMemory);
        return (int)Math.Clamp(slots, 1, processorCount);
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
