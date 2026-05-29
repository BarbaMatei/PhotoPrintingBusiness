namespace PhotoPrint.API.Services;

/// <summary>
/// Aggregated counts from <see cref="IOriginalPurger.PurgeOrderOriginalsAsync"/>.
/// <para><see cref="Purged"/> + <see cref="Skipped"/> + <see cref="Failed"/> = total
/// uploads considered. <see cref="BytesFreed"/> sums only the originals successfully
/// purged.</para>
/// </summary>
public sealed record PurgeOutcome(int Purged, int Skipped, int Failed, long BytesFreed)
{
    public static readonly PurgeOutcome Empty = new(0, 0, 0, 0);

    public PurgeOutcome Add(PurgeOutcome other) => new(
        Purged + other.Purged,
        Skipped + other.Skipped,
        Failed + other.Failed,
        BytesFreed + other.BytesFreed);
}
