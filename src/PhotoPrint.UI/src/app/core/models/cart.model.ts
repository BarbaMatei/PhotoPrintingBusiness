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

export interface CartResponseDto {
  groups: CartGroupDto[];
  subtotal: number;
  itemCount: number;
}

export const EMPTY_CART: CartResponseDto = {
  groups: [],
  subtotal: 0,
  itemCount: 0,
};

/** Key used to persist cart state in localStorage for guest sessions. */
export const CART_STORAGE_KEY = 'fotoTipar_cart';
