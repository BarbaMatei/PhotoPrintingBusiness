import { DeliveryType, ShippingAddressForm } from './shipping.model';

export interface CreateOrderRequest {
  paymentProcessor: 'Stripe' | 'EuPlatesc';
  deliveryType: DeliveryType;
  easyboxLockerId: string | null;
  shippingAddress: ShippingAddressForm | null;
  shippingCostRon: number;
}

export interface StripeIntentResponse {
  clientSecret: string;
  orderId: string;
}

export interface EuPlatescInitiateResponse {
  redirectUrl: string;
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

export interface OrderDto {
  id: string;
  orderNumber: string;
  status: OrderStatus;
  totalRon: number;
  subtotalRon: number;
  shippingCostRon: number;
  deliveryType: DeliveryType;
  paymentProcessor: 'Stripe' | 'EuPlatesc';
  createdAt: string;
  paidAt: string | null;
}
