// ── Stats DTOs ─────────────────────────────────────────────────────────────

export interface AdminStatsDto {
  todayOrders: number;
  todayRevenue: number;
  monthOrders: number;
  monthRevenue: number;
}

export interface RevenueDataPointDto {
  date: string;
  revenue: number;
}

export interface ProductStatsDto {
  productName: string;
  totalQuantity: number;
  orderCount: number;
}

export interface OrdersByStatusDto {
  status: string;
  count: number;
}

// ── Order DTOs ─────────────────────────────────────────────────────────────

export interface AdminOrderSummaryDto {
  id: string;
  orderNumber: string;
  status: string;
  customerEmail: string;
  customerName: string;
  totalRon: number;
  createdAt: string;
  itemCount: number;
  deliveryType: string;
}

export interface AdminOrderItemDto {
  uploadId: string;
  productName: string;
  size: string;
  finish: string;
  quantity: number;
  unitPriceRon: number;
  lineTotalRon: number;
}

export interface AdminShippingAddressDto {
  recipientName: string;
  street: string;
  number: string;
  block: string | null;
  city: string;
  county: string;
  postalCode: string;
  phone: string;
}

export interface AdminOrderDetailDto {
  id: string;
  orderNumber: string;
  status: string;
  customerEmail: string;
  customerName: string;
  subtotalRon: number;
  shippingCostRon: number;
  totalRon: number;
  createdAt: string;
  paidAt: string | null;
  deliveryType: string;
  lockerName: string | null;
  lockerAddress: string | null;
  shippingAddress: AdminShippingAddressDto | null;
  paymentIntentId: string | null;
  awbNumber: string | null;
  trackingUrl: string | null;
  internalNotes: string | null;
  items: AdminOrderItemDto[];
}

// ── Orders page response ───────────────────────────────────────────────────

export interface AdminOrdersPage {
  items: AdminOrderSummaryDto[];
  total: number;
}

// ── Request DTOs ───────────────────────────────────────────────────────────

export interface UpdateOrderStatusRequest {
  status: string;
  awbNumber?: string | null;
  trackingUrl?: string | null;
}

export interface UpdateOrderNotesRequest {
  notes: string | null;
}

// ── SignalR events ─────────────────────────────────────────────────────────

export interface NewOrderEvent {
  id: string;
  orderNumber: string;
  customerEmail: string;
  customerName: string;
  totalRon: number;
  createdAt: string;
  status: string;
}
