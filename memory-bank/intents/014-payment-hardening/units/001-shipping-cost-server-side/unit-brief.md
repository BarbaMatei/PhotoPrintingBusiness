---
unit: 001-shipping-cost-server-side
intent: 014-payment-hardening
phase: inception
status: draft
created: 2026-05-25T10:05:00Z
updated: 2026-05-25T10:05:00Z
---

# Unit Brief: Shipping Cost Server-Side

## Purpose

Remove the client's authority over the order total. The server alone decides shipping cost from the chosen `DeliveryType` (and shipping address county where relevant); the DTO no longer carries `ShippingCostRon`. Add a FluentValidation validator enforcing the conditional-field rules the controller currently relies on by accident.

## Scope

### In Scope
- `DTOs/Payments/CreateOrderRequest.cs` — drop `ShippingCostRon` from the record
- `Services/OrderService.CreateFromCartAsync` — fetch shipping cost from `IShippingService`
- `Validators/Payments/CreateOrderRequestValidator.cs` — new
- `Controllers/PaymentsController` — register validator + remove any field echoes
- Test: validator unit tests + integration test asserting tampered field is ignored

### Out of Scope
- Idempotency wiring (002-payment-idempotency)
- Real per-zone shipping rates (deferred to intent 015 Sameday)

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-1 | Server-side shipping cost resolution | Must |
| FR-2 | CreateOrderRequest validator | Must |

---

## Story Summary

| Story ID | Title | Priority |
|----------|-------|----------|
| 001-remove-client-shipping-cost | Drop `ShippingCostRon` from DTO; resolve server-side | Must |
| 002-create-order-validator | FluentValidation rules for delivery-type conditional fields | Must |

---

## Dependencies

### Depends On
- bolt 015-shipping-and-order-core (IShippingService already exists)

### Depended By
- 002-payment-idempotency (the validator runs before the idempotency path)
