---
id: 001-order-service
unit: 004-payment-backends
intent: 004-checkout-payment
status: draft
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 016-payment-backends
implemented: false
---

# Story: 001-order-service

## User Story

**As a** developer
**I want** an `IOrderService` that creates an order from the current cart
**So that** both Stripe and EuPlatesc payment initiation have a single, consistent way to create a pending order

## Acceptance Criteria

- [ ] **Given** a valid cart and delivery details, **When** `IOrderService.CreateFromCartAsync(userId/guestToken, deliveryDto)` is called, **Then** an `Order` in `AwaitingPayment` status is created with `OrderItems` snapshotting the current cart quantities and product prices
- [ ] **Given** an order is created, **When** the `PricingSnapshot` JSON column is inspected, **Then** it contains `[{ productId, formatSize, finish, unitPriceRon, quantity }]` for every item
- [ ] **Given** the cart is empty at order creation time, **When** `CreateFromCartAsync` is called, **Then** an exception is thrown and no order is persisted
- [ ] **Given** an order is created, **When** inspecting `Order.TotalRon`, **Then** it equals the sum of `(unitPriceRon × quantity)` for all items plus `ShippingCostRon`
- [ ] **Given** `IOrderService.GetByIdAsync(orderId, userId)` is called, **When** the order belongs to a different user, **Then** null is returned (not 403 — no order enumeration)

## Technical Notes

- `IOrderService` interface: `CreateFromCartAsync(CreateOrderDto dto) → Task<Order>`, `GetByIdAsync(Guid orderId, Guid? userId, string? guestSessionId) → Task<Order?>`
- `CreateOrderDto`: `{ UserId?, GuestSessionId?, EasyboxLockerId?, DeliveryAddress?, ShippingType }`
- Order creation is transactional: generate order number + insert Order + insert OrderItems in one transaction
- `UnitPriceRon` on `OrderItem` is snapshotted at creation time from `Product.PricingTiers` (not recalculated later)
- Call `IOrderNumberService.NextAsync(year)` within the transaction to get the next order number

## Dependencies

### Requires
- Bolt 015 (shipping-and-order-core — Order + OrderItem entities + IOrderNumberService)
- Bolt 013 (cart-api — CartItem query for order creation)

### Enables
- Story 002-stripe-payment-intent (needs IOrderService.CreateFromCartAsync)
- Story 004-euplatesc-initiate (same)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Product price changes between cart-add and order creation | Snapshot at order creation time; cart price shown to user may differ — acceptable |
| Cart has item with soft-deleted upload | Rejected: `OrderItem` cannot reference a soft-deleted upload |
| Two payment initiations for same cart (race condition) | Second call creates a second `AwaitingPayment` order — orphan cleanup is handled by future intent |

## Out of Scope

- Order cancellation / refund
- Admin order creation
