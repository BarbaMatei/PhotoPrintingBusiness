---
id: 003-redemption-on-order-create
unit: 001-coupon-domain-and-api
intent: 022-coupon-promo-codes
status: draft
priority: must
created: 2026-05-25T10:45:00Z
assigned_bolt: 047-coupon-domain-and-api
implemented: false
---

# Story: 003-redemption-on-order-create

## User Story

**As** the platform
**I want** coupon redemption to be atomic with order creation
**So that** we never over-redeem and the invoice always reflects the discount correctly

## Acceptance Criteria

- [ ] `OrderService.CreateFromCartAsync` looks up the applied coupon, re-validates (active, in-window, min-subtotal met, `RedemptionsCount < MaxRedemptions`), computes `DiscountRon`, then:
  - Computes `NetTotalRon = (subtotal - discount) / (1 + VatRate)` and `VatRon = (subtotal - discount) - NetTotalRon` — VAT on the post-discount net.
  - Persists `Order.CouponCode`, `Order.DiscountRon`.
  - Inserts `CouponRedemption` row + increments `Coupon.RedemptionsCount` with `RowVersion` concurrency check, all in the same transaction.
- [ ] Concurrent test (100 parallel orders for a coupon with `MaxRedemptions=5`): exactly 5 redemptions persist; the other 95 receive 409 with `code: "COUPON_EXHAUSTED"` and **no `Order` row** is created for them.
- [ ] If `RowVersion` conflict occurs, retry once; second conflict → 409.

## Technical Notes

```csharp
// Pseudo-code inside transaction
var coupon = await _db.Coupons
    .Where(c => c.Id == cart.CouponId && c.IsActive)
    .FirstOrDefaultAsync(ct)
    ?? throw new InvalidCouponException();

if (coupon.MaxRedemptions is { } cap && coupon.RedemptionsCount >= cap)
    throw new CouponExhaustedException();

coupon.RedemptionsCount += 1; // RowVersion concurrency check on SaveChanges
// compute discount, net, vat as above
```

## Dependencies

### Requires
- 002-cart-coupon-endpoints, intent 016 (VAT)

### Enables
- 004-admin-coupon-crud (consumption stats)
- 001-cart-coupon-ux (FE)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Discount > subtotal | Cap discount at subtotal; never negative net |
| Discount = subtotal | Order proceeds with `NetTotal=0, Vat=0` |
| Coupon deactivated mid-checkout | 409 with `code: "INVALID_COUPON"`; cart code cleared |

## Out of Scope

- Refund-flow handling of discount reversal (separate intent).
