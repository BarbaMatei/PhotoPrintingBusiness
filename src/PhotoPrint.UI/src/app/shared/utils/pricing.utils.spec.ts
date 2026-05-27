import { calcPrice, lowestPrice } from './pricing.utils';
import { PricingTier } from '../../core/models/product.model';

const THREE_TIERS: PricingTier[] = [
  { minQuantity: 1, maxQuantity: 9, unitPrice: 2.50 },
  { minQuantity: 10, maxQuantity: 49, unitPrice: 1.80 },
  { minQuantity: 50, maxQuantity: null, unitPrice: 1.20 },
];

describe('calcPrice', () => {
  it('returns null for empty tiers', () => {
    expect(calcPrice([], 5)).toBeNull();
  });

  it('matches first tier for quantity 1', () => {
    const result = calcPrice(THREE_TIERS, 1);
    expect(result).not.toBeNull();
    expect(result!.unitPrice).toBe(2.50);
    expect(result!.totalPrice).toBe(2.50);
    expect(result!.tierLabel).toBe('1–9');
  });

  it('matches first tier boundary at max (qty=9)', () => {
    const result = calcPrice(THREE_TIERS, 9);
    expect(result!.unitPrice).toBe(2.50);
    expect(result!.totalPrice).toBe(22.50);
    expect(result!.tierLabel).toBe('1–9');
  });

  it('matches second tier at boundary min (qty=10)', () => {
    const result = calcPrice(THREE_TIERS, 10);
    expect(result!.unitPrice).toBe(1.80);
    expect(result!.tierLabel).toBe('10–49');
  });

  it('matches second tier in the middle (qty=25)', () => {
    const result = calcPrice(THREE_TIERS, 25);
    expect(result!.unitPrice).toBe(1.80);
    expect(result!.totalPrice).toBe(45.00);
  });

  it('matches open-ended tier at boundary min (qty=50)', () => {
    const result = calcPrice(THREE_TIERS, 50);
    expect(result!.unitPrice).toBe(1.20);
    expect(result!.tierLabel).toBe('50+');
  });

  it('matches open-ended tier for large quantity (qty=10000)', () => {
    const result = calcPrice(THREE_TIERS, 10000);
    expect(result!.unitPrice).toBe(1.20);
    expect(result!.totalPrice).toBe(12000.00);
    expect(result!.tierLabel).toBe('50+');
  });

  it('returns null when quantity falls below all tiers (qty=0)', () => {
    const tiers: PricingTier[] = [{ minQuantity: 1, maxQuantity: null, unitPrice: 1.00 }];
    expect(calcPrice(tiers, 0)).toBeNull();
  });

  it('rounds totalPrice to 2 decimal places', () => {
    const tiers: PricingTier[] = [{ minQuantity: 1, maxQuantity: null, unitPrice: 1.33 }];
    const result = calcPrice(tiers, 3);
    expect(result!.totalPrice).toBe(3.99);
  });

  it('single flat tier has open-ended label', () => {
    const tiers: PricingTier[] = [{ minQuantity: 1, maxQuantity: null, unitPrice: 0.99 }];
    const result = calcPrice(tiers, 1);
    expect(result!.tierLabel).toBe('1+');
  });

  it('returns null for quantity between non-contiguous tiers', () => {
    const gapped: PricingTier[] = [
      { minQuantity: 1, maxQuantity: 5, unitPrice: 3.00 },
      { minQuantity: 10, maxQuantity: null, unitPrice: 2.00 },
    ];
    expect(calcPrice(gapped, 7)).toBeNull();
  });
});

describe('lowestPrice', () => {
  it('returns null for empty sizes array', () => {
    expect(lowestPrice([])).toBeNull();
  });

  it('returns null when all sizes have no tiers', () => {
    expect(lowestPrice([{ pricingTiers: [] }, { pricingTiers: [] }])).toBeNull();
  });

  it('returns the single price when one tier exists', () => {
    expect(lowestPrice([{ pricingTiers: [{ minQuantity: 1, maxQuantity: null, unitPrice: 2.00 }] }])).toBe(2.00);
  });

  it('returns lowest price across multiple tiers in one size', () => {
    const sizes = [{ pricingTiers: THREE_TIERS }];
    expect(lowestPrice(sizes)).toBe(1.20);
  });

  it('returns lowest price across multiple sizes', () => {
    const sizes = [
      { pricingTiers: [{ minQuantity: 1, maxQuantity: null, unitPrice: 3.50 }] },
      { pricingTiers: [{ minQuantity: 1, maxQuantity: null, unitPrice: 1.80 }] },
    ];
    expect(lowestPrice(sizes)).toBe(1.80);
  });
});
