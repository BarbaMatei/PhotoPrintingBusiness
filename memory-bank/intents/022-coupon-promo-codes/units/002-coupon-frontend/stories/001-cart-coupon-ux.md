---
id: 001-cart-coupon-ux
unit: 002-coupon-frontend
intent: 022-coupon-promo-codes
status: draft
priority: should
created: 2026-05-25T10:45:00Z
assigned_bolt: 048-coupon-frontend
implemented: false
---

# Story: 001-cart-coupon-ux

## User Story

**As** a customer on the cart page
**I want** to enter a promo code and see the discount immediately
**So that** I'm confident before going to checkout

## Acceptance Criteria

- [ ] Cart page has a "Cod promo" input + "Aplică" button below the items list.
- [ ] On apply success: discount line `Reducere: -X.XX RON` appears in the summary; subtotal / VAT / total update.
- [ ] On error: Romanian copy mapped to `code:`:
  - `INVALID_COUPON` → "Codul introdus nu este valid sau a expirat."
  - `MIN_SUBTOTAL_NOT_MET` → "Codul se aplică doar la comenzi de cel puțin X RON."
  - `COUPON_EXHAUSTED` → "Codul a atins limita de utilizări."
- [ ] Applied coupon is reflected on review + confirmation pages.
- [ ] PDF invoice template (intent 016) renders the discount line above VAT total when present.

## Technical Notes

- Reuse existing `CartService` to call the new endpoints; emit signal for the cart summary component.
- Forms reactive; inline error rendering matches existing cart UX.

## Dependencies

### Requires
- Unit 001 (backend endpoints), intent 016 (invoice template)

### Enables
- Marketing campaigns

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Customer clears cart while coupon applied | Coupon cleared automatically by backend (no orphan) |
| Customer logs in after applying coupon as guest | Coupon transfers with the cart-merge flow (existing) |

## Out of Scope

- Admin coupon management UI.
