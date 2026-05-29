namespace PhotoPrint.API.Services;

/// <summary>
/// Aggregated counts returned from <see cref="IOrderPhotoPromoter.PromoteOrderAsync"/>.
/// <para><see cref="Promoted"/> + <see cref="Skipped"/> + <see cref="Failed"/> = total
/// uploads considered. <see cref="TotalBytes"/> sums only successfully <see cref="Promoted"/>
/// uploads (the relevant figure for "how much went to cloud this run"). Backfill summarises
/// across many orders; the live worker logs per-order.</para>
/// </summary>
public sealed record PromotionOutcome(int Promoted, int Skipped, int Failed, long TotalBytes)
{
    /// <summary>Empty outcome — no uploads considered (no-op order, cloud tier off, etc.).</summary>
    public static readonly PromotionOutcome Empty = new(0, 0, 0, 0);

    /// <summary>Returns a new outcome with <paramref name="other"/>'s counts added in.</summary>
    public PromotionOutcome Add(PromotionOutcome other) => new(
        Promoted + other.Promoted,
        Skipped + other.Skipped,
        Failed + other.Failed,
        TotalBytes + other.TotalBytes);
}
