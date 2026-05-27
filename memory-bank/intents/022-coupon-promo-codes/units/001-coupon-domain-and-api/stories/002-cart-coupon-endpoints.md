---
id: 002-cart-coupon-endpoints
unit: 001-coupon-domain-and-api
intent: 022-coupon-promo-codes
status: draft
priority: must
created: 2026-05-25T10:45:00Z
assigned_bolt: 047-coupon-domain-and-api
implemented: false
---

# Story: 002-cart-coupon-endpoints

## User Story

**As** a customer
**I want** to apply or remove a promo code on my cart
**So that** I can see the discount before going to checkout

## Acceptance Criteria

- [ ] `POST /api/cart/coupon { code }`:
  - Code lookup is case-insensitive (stored uppercase).
  - Returns 200 with updated cart preview (subtotal, discount, net, vat estimate, gross) when valid.
  - Returns 422 with `code: "INVALID_COUPON"` on unknown / inactive / expired.
  - Returns 422 with `code: "MIN_SUBTOTAL_NOT_MET"` when cart subtotal below `MinSubtotalRon`.
  - Stores the applied code on the cart row (for both registered and guest carts).
- [ ] `DELETE /api/cart/coupon` clears the applied code.
- [ ] Dual-auth (JWT or `X-Guest-Token`) — same policy as existing cart endpoints.
- [ ] Re-applying replaces the previously applied code without error.

## Technical Notes

- `ICouponService.PreviewAsync(code, cartSubtotal)` returns either a `CouponPreview` record or a `CouponError`.
- Note: preview at apply time != atomic redemption at order time — `MaxRedemptions` may be exhausted between apply and checkout. Document the race; order endpoint owns final enforcement.

## Dependencies

### Requires
- 001-coupon-schema

### Enables
- 003-redemption-on-order-create

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Cart subtotal changes after apply | Re-preview on every cart read |
| Coupon deactivated after apply | Order-time check catches; show clear error then |

## Out of Scope

- Stacking codes.
