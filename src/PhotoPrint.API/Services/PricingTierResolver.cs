using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

/// <summary>
/// Shared unit-price tier resolution. <see cref="CartService"/> and
/// <see cref="OrderService"/> pick the applicable pricing tier by the same rule — the
/// highest-<c>MinQuantity</c> tier that brackets the quantity, falling back to the
/// highest-<c>MinQuantity</c> tier overall, or <c>0</c> when no tiers exist.
///
/// They differ ONLY in WHICH tiers and WHICH quantity they feed in (CartService: a single
/// size's tiers + the per-group total copies; OrderService: an item's product tiers + that
/// item's quantity), so that selection stays at each call site. Centralizing the shared
/// bracket-matching here means the two can never silently diverge on the tier rule — the
/// trap the old "mirrors CartService.ResolveUnitPrice" comment hid.
/// </summary>
internal static class PricingTierResolver
{
    public static decimal Resolve(IEnumerable<PricingTier> tiers, int quantity)
    {
        var ordered = tiers.OrderByDescending(t => t.MinQuantity).ToList();
        if (ordered.Count == 0)
            return 0m;

        var matched = ordered.FirstOrDefault(t =>
            t.MinQuantity <= quantity &&
            (t.MaxQuantity == null || quantity <= t.MaxQuantity));

        return (matched ?? ordered[0]).UnitPrice;
    }
}
