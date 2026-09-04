export interface CartItemRequest {
  uploadId: string;
  quantity: number;
}

export interface CartRequest {
  productId: string;
  sizeId: string;
  finishName: string | null;
  items: CartItemRequest[];
}

export interface CartMergeRequest {
  guestSessionId: string;
}

export interface CartItemDto {
  uploadId: string;
  quantity: number;
  previewUrl: string;
  unitPrice: number;
  lineTotal: number;
  widthPx: number;
  heightPx: number;
}

export interface CartGroupDto {
  productId: string;
  productName: string;
  sizeId: string;
  sizeName: string;
  finishName: string | null;
  items: CartItemDto[];
  totalCopies: number;
  unitPrice: number;
  subtotal: number;
}

export type CouponStatus = 'valid' | 'stale';

export type CouponKind = 'Percent' | 'Fixed' | 'FreeShipping';

export interface CartResponseDto {
  groups: CartGroupDto[];
  subtotal: number;
  itemCount: number;
  /** Coupon and total fields are absent on a guest cart snapshot written by an older build. */
  couponCode?: string | null;
  couponType?: CouponKind | string | null;
  couponStatus?: CouponStatus | string | null;
  couponReason?: string | null;
  discountRon?: number;
  totalRon?: number;
  netTotalRon?: number;
  vatRon?: number;
  vatRate?: number;
}

export interface ApplyCouponRequest {
  code: string;
}

export const EMPTY_CART: CartResponseDto = {
  groups: [],
  subtotal: 0,
  itemCount: 0,
  couponCode: null,
  couponType: null,
  couponStatus: null,
  couponReason: null,
  discountRon: 0,
  totalRon: 0,
  netTotalRon: 0,
  vatRon: 0,
  vatRate: 0,
};

/** Cart total after any discount, falling back to the subtotal for a legacy snapshot. */
export function cartTotal(cart: CartResponseDto | null | undefined): number {
  if (!cart) return 0;
  return cart.totalRon ?? cart.subtotal;
}

/** Discount applied to the cart; a stale coupon discounts nothing. */
export function cartDiscount(cart: CartResponseDto | null | undefined): number {
  return cart?.discountRon ?? 0;
}

/** True for a valid free-shipping coupon, whose value the cart itself cannot show. */
export function hasFreeShippingCoupon(cart: CartResponseDto | null | undefined): boolean {
  return cart?.couponStatus === 'valid' && cart?.couponType === 'FreeShipping';
}

/** True when the coupon on the cart no longer applies and must be removed by the customer. */
export function isCouponStale(cart: CartResponseDto | null | undefined): boolean {
  return !!cart?.couponCode && cart?.couponStatus === 'stale';
}

/** Key used to persist cart state in localStorage for guest sessions. */
export const CART_STORAGE_KEY = 'fotoTipar_cart';
