export interface PricingTier {
  minQuantity: number;
  maxQuantity: number | null;
  unitPrice: number;
}

export interface ProductSize {
  id: string;
  label: string;
  widthMm: number;
  heightMm: number;
  isActive?: boolean;
  pricingTiers: PricingTier[];
}

export interface Product {
  id: string;
  name: string;
  productType: string;
  imageUrl: string | null;
  sortOrder: number;
  isActive?: boolean;
  sizes: ProductSize[];
  finishes: string[];
}

export interface PriceResult {
  unitPrice: number;
  totalPrice: number;
  tierLabel: string;
}
