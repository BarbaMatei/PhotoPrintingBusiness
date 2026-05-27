---
id: 004-order-review-step
unit: 005-checkout-ui
intent: 004-checkout-payment
status: draft
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 017-checkout-ui
implemented: false
---

# Story: 004-order-review-step

## User Story

**As a** customer
**I want** to review my complete order summary before paying
**So that** I can verify everything is correct and confirm I agree to the terms before entering payment details

## Acceptance Criteria

- [ ] **Given** Step 2 (`/checkout/revizuire`) is loaded, **When** rendered, **Then** the page shows: all cart items (thumbnail, format, finish, quantity, unit price, line total), subtotal, shipping cost (from CheckoutStateService), grand total — all in `XX,XX RON` format
- [ ] **Given** the delivery details section, **When** shown, **Then** it displays either the selected Easybox locker name + address or the home delivery address, with a `Modifică adresa` link back to Step 1
- [ ] **Given** the Terms & Conditions checkbox, **When** unchecked, **Then** the `Plătește acum` button is disabled with `aria-disabled="true"`
- [ ] **Given** the checkbox is checked, **When** `Plătește acum` is clicked, **Then** `CheckoutStateService.termsAccepted = true` is set and the user advances to Step 3
- [ ] **Given** the `Modifică coșul` link, **When** clicked, **Then** the user is routed to `/cos` and `CheckoutStateService` is cleared

## Technical Notes

- Cart data: read from `CartService.items$` (already loaded from server for auth users or localStorage for guests)
- Delivery summary: read from `CheckoutStateService`
- `grandTotal = cartSubtotal + shippingCost` — both available in state
- T&C checkbox: `[formControl]` bound to `termsAccepted` field; `Plătește acum` `[disabled]="!termsAccepted"`
- Price format: Romanian locale `{{ price | number:'1.2-2':'ro' }} RON`
- T&C link: opens in new tab `<a href="/termeni-si-conditii" target="_blank">`

## Dependencies

### Requires
- Story 001-checkout-stepper (CheckoutStateService)
- Story 005-cart-service (CartService.items$)
- Story 002-delivery-step (delivery details stored in state)

### Enables
- Story 005-payment-step (user proceeds from review to payment)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Cart items changed in another tab | Review shows stale cart data; user must go back to cart to refresh |
| Shipping cost not in state (navigated directly) | Step guard redirects to `/checkout/livrare` |
| T&C page not yet created | Link shows 404 — acceptable for Phase 1 |

## Out of Scope

- Coupon / discount code entry
- Editing quantities on the review page (use `Modifică coșul`)
- VAT breakdown
