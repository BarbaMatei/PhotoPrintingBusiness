---
id: 005-cart-service
unit: 002-upload-format-cart-ui
intent: 004-checkout-payment
status: draft
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 014-upload-format-cart-ui
implemented: false
---

# Story: 005-cart-service

## User Story

**As a** developer
**I want** a `CartService` that is the single source of truth for cart state across all components
**So that** the badge count, cart page, and upload page all stay in sync without duplicated API calls

## Acceptance Criteria

- [ ] **Given** an authenticated user, **When** `CartService` is initialized, **Then** it fetches `GET /api/cart` and populates the in-memory cart state
- [ ] **Given** a guest user, **When** `CartService` is initialized, **Then** it loads cart items from `localStorage` key `ft_cart_guest` and does not call the API
- [ ] **Given** any cart mutation (add, update, remove), **When** the user is a guest, **Then** `CartService` updates both the in-memory state and `localStorage` — then syncs to server via `POST /api/cart`
- [ ] **Given** a guest logs in, **When** `AuthService.login()` resolves, **Then** `CartService.mergeGuestCart()` is called which calls `POST /api/cart/merge` and reloads cart state from server
- [ ] **Given** any component subscribes to `CartService.items$`, **When** the cart changes, **Then** the component receives the updated items without re-fetching the API
- [ ] **Given** `CartService.itemCount$` is subscribed, **When** cart items change, **Then** the count emits the total number of items (not quantities) reactively

## Technical Notes

- Implement as Angular `Injectable({ providedIn: 'root' })` service
- Internal state: `BehaviorSubject<CartItem[]>` (or Angular `signal`)
- `itemCount$`: derived `computed(() => items().length)` or `map(items => items.length)`
- localStorage key: `ft_cart_guest` — JSON array of `{ uploadId, productId, quantity }`
- `mergeGuestCart()`: calls `POST /api/cart/merge { guestToken }`, then calls `loadFromServer()`
- Expose: `items$`, `itemCount$`, `replaceCart(items)`, `removeItem(uploadId)`, `mergeGuestCart()`, `clearCart()`

## Dependencies

### Requires
- Bolt 013 (cart-api — GET/POST/DELETE /api/cart + POST /api/cart/merge)
- Bolt 005 / 007 (auth — `AuthService` emits login events; guest token available)

### Enables
- Story 003-order-summary-panel (add-to-cart action)
- Story 004-cart-page (cart display)
- Bolt 017 (checkout-ui — reads cart for order review)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| localStorage is unavailable (private mode) | Fall back to in-memory only; no error thrown |
| Server sync fails for guest | In-memory and localStorage remain updated; retry on next mutation |
| Cart loaded while offline | In-memory state from last fetch is used; error banner shown |
| `mergeGuestCart` called with empty guest cart | No-op API call returns 200; cart unchanged |

## Out of Scope

- Server-side session resumption from localStorage on page refresh for authenticated users (auth session covers this via re-fetch)
- Cart analytics / event tracking
