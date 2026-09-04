import { DeliveryType, ShippingAddressForm } from './shipping.model';

export interface CreateOrderRequest {
  deliveryType: DeliveryType;
  easyboxLockerId: string | null;
  shippingAddress: ShippingAddressForm | null;
}

export interface StripeIntentResponse {
  clientSecret: string;
  orderId: string;
}

export type OrderStatus =
  | 'AwaitingPayment'
  | 'Paid'
  | 'Printing'
  | 'Shipped'
  | 'Delivered'
  | 'PaymentFailed'
  | 'Cancelled';

export interface OrderPaymentStatusDto {
  id: string;
  orderNumber: string;
  status: OrderStatus;
  totalRon: number;
  vatRon: number;
  vatRate: number;
  couponCode: string | null;
  discountRon: number;
  deliveryType: DeliveryType;
  createdAt: string;
  paidAt: string | null;
}
