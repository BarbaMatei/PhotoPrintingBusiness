---
id: 004-cart-page
unit: 002-upload-format-cart-ui
intent: 004-checkout-payment
status: draft
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 014-upload-format-cart-ui
implemented: false
---

# Story: 004-cart-page

## User Story

**As a** customer
**I want** to review and edit my cart before proceeding to checkout
**So that** I can confirm my photo selections, quantities, and totals before paying

## Acceptance Criteria

- [ ] **Given** the `/cos` route is loaded, **When** the cart has items, **Then** each item shows: thumbnail (from `previewUrl`), format, finish, quantity stepper, unit price, line total, and a remove button
- [ ] **Given** a quantity is changed on the cart page, **When** the stepper value settles, **Then** `POST /api/cart` is called with the updated quantities and the totals recalculate
- [ ] **Given** the remove button is clicked for an item, **When** confirmed, **Then** that item is removed and the cart is updated via `POST /api/cart`
- [ ] **Given** the cart is empty, **When** the `/cos` route loads, **Then** an empty state is shown: `"Coșul tău este gol"` with a CTA to `/upload`
- [ ] **Given** an authenticated user visits `/cos`, **When** the page loads, **Then** the cart is fetched from `GET /api/cart` and the badge count is synced
- [ ] **Given** the nav cart icon is visible, **When** the cart has items, **Then** the badge shows the total number of cart items reactively
- [ ] **Given** the `Continuă spre plată` button is clicked, **When** the cart has items, **Then** the user is routed to `/checkout`

## Technical Notes

- Cart page route: `/cos` (lazy-loaded module `CartModule`)
- Shipping cost row shows `Calculat la pasul următor` (not a number)
- Debounce quantity stepper changes by 500ms before calling `POST /api/cart` to avoid rapid API calls
- For guests: cart items loaded from `CartService` which reads localStorage; server sync on change
- Cart badge: `CartService.itemCount$` observable subscribed in `AppShellComponent`

## Dependencies

### Requires
- Story 005-cart-service (CartService observable state)
- Bolt 013 (cart-api — GET/POST /api/cart)
- Bolt 004 (angular-app-shell — nav badge, route registration)

### Enables
- Bolt 017 (checkout-ui — user proceeds from cart to checkout)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Cart API returns 401 (token expired) | AuthInterceptor redirects to login; cart preserved in localStorage |
| Upload preview 404 (upload deleted) | Show placeholder image; allow removal |
| Cart update fails | Toast error; revert quantity to previous value |
| Guest with localStorage cart opens cart page | CartService loads from localStorage; shows items without API call |

## Out of Scope

- Saved carts / wishlists
- Cart sharing by link
- Coupon / discount codes
