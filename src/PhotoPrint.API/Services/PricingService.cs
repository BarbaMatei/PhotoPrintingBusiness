using PhotoPrint.API.DTOs.Admin;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

public class PricingService
{
    /// <summary>
    /// Finds the pricing tier that covers <paramref name="quantity"/>.
    /// An open-ended tier (MaxQuantity = null) covers any quantity ≥ MinQuantity.
    /// </summary>
    public PricingTier GetApplicableTier(IEnumerable<PricingTier> tiers, int quantity)
    {
        var tier = tiers.FirstOrDefault(t =>
            t.MinQuantity <= quantity &&
            (t.MaxQuantity == null || t.MaxQuantity >= quantity));

        if (tier is null)
            throw new InvalidOperationException(
                $"No pricing tier covers quantity {quantity}. Ensure tiers are contiguous and cover all quantities.");

        return tier;
    }

    /// <summary>
    /// Calculates unit price, total price, and a human-readable tier label for a given tier and quantity.
    /// </summary>
    public (decimal UnitPrice, decimal TotalPrice, string TierLabel) Calculate(PricingTier tier, int quantity)
    {
        var total = tier.UnitPrice * quantity;
        var label = tier.MaxQuantity.HasValue
            ? $"{tier.MinQuantity}-{tier.MaxQuantity}"
            : $"{tier.MinQuantity}+";
        return (tier.UnitPrice, total, label);
    }

    /// <summary>
    /// Validates a proposed set of pricing tiers against all 8 business rules.
    /// Returns (true, null) on success or (false, errorMessage) on first failure.
    /// </summary>
    public (bool IsValid, string? Error) ValidateTiers(IEnumerable<CreatePricingTierRequest> tiers)
    {
        var list = tiers.OrderBy(t => t.MinQuantity).ToList();

        if (list.Count == 0)
            return (false, "Lista de niveluri de prețuri nu poate fi goală.");

        if (list[0].MinQuantity != 1)
            return (false, "Primul nivel trebuie să înceapă de la cantitatea minimă 1.");

        for (var i = 0; i < list.Count; i++)
        {
            var t = list[i];

            if (t.UnitPrice <= 0)
                return (false, $"Prețul unitar trebuie să fie > 0 (nivel {i + 1}).");

            if (t.MaxQuantity.HasValue && t.MaxQuantity < t.MinQuantity)
                return (false, $"Cantitatea maximă trebuie să fie ≥ cantitatea minimă (nivel {i + 1}).");

            // Only the last tier may be open-ended
            if (!t.MaxQuantity.HasValue && i < list.Count - 1)
                return (false, "Doar ultimul nivel poate fi nelimitat (maxQuantity = null).");

            if (i < list.Count - 1)
            {
                var next = list[i + 1];

                // Contiguous: no gaps
                if (t.MaxQuantity + 1 != next.MinQuantity)
                    return (false, $"Intervalul dintre nivelul {i + 1} și nivelul {i + 2} nu este contiguu.");

                // Monotonically non-increasing price
                if (next.UnitPrice > t.UnitPrice)
                    return (false, $"Prețul nivelului {i + 2} nu poate fi mai mare decât prețul nivelului {i + 1}.");
            }
        }

        return (true, null);
    }
}
