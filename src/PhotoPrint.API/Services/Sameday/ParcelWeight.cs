using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Parcel-weight heuristic from FR-3: <c>grams = totalPrintCount * 50 + 50</c>.
/// Encapsulated as a value object so the *formula* is a single chokepoint;
/// intent 016+ is expected to replace this with per-<c>ProductSize</c> weights
/// and a single source-file change to <see cref="FromOrder"/> covers it.
/// </summary>
public readonly record struct ParcelWeight(int Grams)
{
    /// <summary>Minimum grams the formula will ever produce (the +50 g floor).</summary>
    public const int MinimumGrams = 50;

    public decimal Kilograms => Math.Round(Grams / 1000m, 3);

    /// <summary>
    /// Computes the heuristic weight from an order's items. Throws
    /// <see cref="ArgumentException"/> if the order has no items (caller — the
    /// AWB mapper — surfaces this as <c>AwbCreationOutcome.GiveUp("invalid request")</c>
    /// rather than letting a zero-weight call reach the wire).
    /// </summary>
    public static ParcelWeight FromOrder(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (order.Items is null || order.Items.Count == 0)
            throw new ArgumentException("Cannot compute parcel weight: order has no items.", nameof(order));

        var totalPrints = order.Items.Sum(i => i.Quantity);
        if (totalPrints <= 0)
            throw new ArgumentException("Cannot compute parcel weight: order has zero total prints.", nameof(order));

        return new ParcelWeight(totalPrints * 50 + MinimumGrams);
    }
}
