export interface OrderSummaryDto {
  id: string;
  orderNumber: string;
  status: string;
  totalRon: number;
  createdAt: string;
  deliveryType: string;
  itemCount: number;
}

export interface ShippingAddressDto {
  recipientName: string;
  street: string;
  number: string;
  block: string | null;
  city: string;
  county: string;
  postalCode: string;
  phone: string;
}

export interface OrderItemDto {
  uploadId: string;
  previewUrl: string;
  productName: string;
  size: string;
  finish: string;
  quantity: number;
  unitPriceRon: number;
  lineTotalRon: number;
}

export interface OrderDetailDto extends OrderSummaryDto {
  subtotalRon: number;
  shippingCostRon: number;
  paidAt: string | null;
  paymentProcessor: string;
  lockerId: string | null;
  lockerName: string | null;
  lockerAddress: string | null;
  shippingAddress: ShippingAddressDto | null;
  items: OrderItemDto[];
}
