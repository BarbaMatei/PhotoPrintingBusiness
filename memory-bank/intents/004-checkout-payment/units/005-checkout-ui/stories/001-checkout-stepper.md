---
id: 001-checkout-stepper
unit: 005-checkout-ui
intent: 004-checkout-payment
status: complete
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 017-checkout-ui
implemented: true
---

# Story: 001-checkout-stepper

## User Story

**As a** customer
**I want** a guided checkout flow with clear steps and the ability to go back
**So that** I can complete my order without losing my progress if I need to correct a previous step

## Acceptance Criteria

- [ ] **Given** the `/checkout` route is loaded, **When** the page renders, **Then** a stepper with 3 steps is shown: `1. Livrare`, `2. Revizuire`, `3. Plată`
- [ ] **Given** the user is on Step 2 or 3, **When** they click a previous step, **Then** they can navigate back and the data they entered is preserved
- [ ] **Given** the browser is refreshed at Step 2, **When** the page reloads, **Then** either the state is restored from `sessionStorage` or the user is redirected to `/cos` (cart page)
- [ ] **Given** the cart is empty, **When** `/checkout` is navigated to directly, **Then** a route guard redirects to `/cos`
- [ ] **Given** `CheckoutStateService` holds the current state, **When** any step component reads from it, **Then** delivery selection, terms acceptance, and payment intent data are all available reactively

## Technical Notes

- Route: `/checkout` — lazy-loaded `CheckoutModule` with child routes `/checkout/livrare`, `/checkout/revizuire`, `/checkout/plata`
- Stepper: custom Angular standalone component (no Angular Material dependency — use custom CSS stepper)
- `CheckoutStateService`: `Injectable({ providedIn: 'root' })` with `Signal<CheckoutState>`:
  - `deliveryMethod: 'Easybox' | 'Courier' | null`
  - `lockerId: string | null`
  - `deliveryAddress: DeliveryAddressDto | null`
  - `termsAccepted: boolean`
  - `pendingOrderId: string | null`
  - `stripeClientSecret: string | null`
- `sessionStorage` key: `ft_checkout_state` — serialized JSON; restore on init; clear on confirmation
- Route guard: `CheckoutGuard` — `canActivate` checks `CartService.itemCount() > 0`

## Dependencies

### Requires
- Story 005-cart-service (CartService.itemCount for guard)
- Bolt 004 (angular-app-shell — route registration)

### Enables
- Stories 002-005 (all checkout steps use CheckoutStateService from this story)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| User navigates to `/checkout/revizuire` directly (no Step 1) | Step guard redirects to `/checkout/livrare` |
| sessionStorage unavailable | State not persisted; refresh → redirect to `/cos` |
| User completes checkout and presses back | Confirmation page guard prevents return to checkout |

## Out of Scope

- Progress bar persistence across browser close (sessionStorage is session-scoped)
- Multi-step undo / history
