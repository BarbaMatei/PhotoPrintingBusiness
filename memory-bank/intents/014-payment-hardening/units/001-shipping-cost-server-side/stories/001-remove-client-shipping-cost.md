---
id: 001-remove-client-shipping-cost
unit: 001-shipping-cost-server-side
intent: 014-payment-hardening
status: implemented
priority: must
created: 2026-05-25T10:05:00Z
assigned_bolt: 034-shipping-cost-server-side
implemented: true
implemented_at: 2026-05-25T13:00:00Z
---

# Story: 001-remove-client-shipping-cost

## User Story

**As** the platform owner
**I want** the server, not the browser, to set the shipping cost on every order
**So that** a tampered request cannot reduce the customer's charge

## Acceptance Criteria

- [ ] **Given** a request `POST /api/payments/stripe/intent` with body `{ DeliveryType: "Easybox", EasyboxLockerId: "...", ShippingCostRon: -100 }`, **When** the order is created, **Then** `order.TotalRon` equals the cart subtotal **plus the server-resolved Easybox cost** (currently 20.00 RON from configuration), regardless of the `ShippingCostRon` value sent.
- [ ] **Given** the same request with `DeliveryType: "Courier"` and a valid `ShippingAddress`, **Then** the server-resolved courier cost (25.00 RON) is added.
- [ ] `CreateOrderRequest` no longer compiles with a `ShippingCostRon` member. Existing callers fail at build, not at runtime.
- [ ] The frontend may still serialise `ShippingCostRon` in its request body — server **silently ignores** unknown fields (`JsonSerializerOptions.UnknownTypeHandling` default) and logs a warning at Information level so we can detect the transition.

## Technical Notes

```csharp
// DTOs/Payments/CreateOrderRequest.cs (after)
public record CreateOrderRequest(
    PaymentProcessor PaymentProcessor,
    DeliveryType     DeliveryType,
    Guid?            EasyboxLockerId,
    ShippingAddressSnapshot? ShippingAddress);

// Services/OrderService.cs — in CreateFromCartAsync(...)
var shippingRon = await _shipping.GetShippingCostAsync(
    request.DeliveryType,
    request.ShippingAddress?.CountyCode,
    ct);

order.ShippingCostRon = shippingRon;
order.TotalRon        = subtotal + shippingRon;
```

- Detection of "unexpected `ShippingCostRon` in body" can be done via a tiny middleware reading the raw JSON keys for one release, then removed.

## Dependencies

### Requires
- bolt 015-shipping-and-order-core (`IShippingService` is already in use)

### Enables
- 002-create-order-validator (validator covers shape after this DTO change)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Cart subtotal is 0 (free promo) | Server still adds shipping; total = shipping |
| Shipping config missing | `IShippingService` throws; controller returns 500 ProblemDetails |
| FE sends negative `ShippingCostRon` | Logged at Warning; ignored |

## Out of Scope

- Multi-courier rate resolution (Sameday returns per-locker rates in intent 015).
