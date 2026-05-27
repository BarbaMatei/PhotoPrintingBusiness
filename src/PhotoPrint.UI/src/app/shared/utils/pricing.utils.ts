import { PricingTier, PriceResult } from '../../core/models/product.model';

/**
 * Pure client-side price calculation. Mirrors PricingService.GetApplicableTier + Calculate
 * from the backend (PhotoPrint.API.Services.PricingService).
 *
 * Returns null if no tier covers the given quantity (data integrity issue).
 */
export function calcPrice(tiers: PricingTier[], quantity: number): PriceResult | null {
  const tier = tiers.find(
    t => t.minQuantity <= quantity && (t.maxQuantity === null || t.maxQuantity >= quantity),
  );
  if (!tier) return null;

  const label = tier.maxQuantity !== null
    ? `${tier.minQuantity}–${tier.maxQuantity}`
    : `${tier.minQuantity}+`;

  return {
    unitPrice: tier.unitPrice,
    totalPrice: +(tier.unitPrice * quantity).toFixed(2),
    tierLabel: label,
  };
}

/**
 * Returns the lowest unit price across all sizes and tiers (for "de la X lei" display).
 * Returns null if no tiers exist.
 */
export function lowestPrice(sizes: { pricingTiers: PricingTier[] }[]): number | null {
  const prices = sizes.flatMap(s => s.pricingTiers.map(t => t.unitPrice));
  return prices.length > 0 ? Math.min(...prices) : null;
}
