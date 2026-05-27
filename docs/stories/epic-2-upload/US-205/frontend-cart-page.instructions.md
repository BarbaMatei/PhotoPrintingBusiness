# US-205 — Cart Page (Frontend)

## Story
**As a** customer  
**I want to** review my selections before proceeding to checkout, and make adjustments

## Type
FRONTEND — Angular

## Epic
EPIC-2 | Upload Fotografii & Selecție Format

## Dependencies
- US-206 (Cart API backend)
- US-201/US-203 (Photos must be uploaded and format selected)

## Acceptance Criteria

1. **Cart page `/cos`**: list of items — thumbnail, format, finish, quantity stepper, unit price, line total, remove button
2. **Global format/finish** displayed as read-only summary banner at top: `Format ales: 10×15 cm, Lucios`
3. **Subtotal, shipping placeholder** (`Calculat la pasul următor`), grand total
4. **Persistent**: localStorage for guests; server-side cart for logged-in users, merged on login
5. **Cart icon in nav** shows item count badge; badge updates reactively
6. **Empty cart state** with CTA `Adaugă fotografii`
7. **`Continuă cumpărăturile`** + **`Finalizează comanda`** buttons

## Technical Notes

### Component Location
`src/app/features/upload/cart/cart.component.ts`

### Implementation Details
- Cart state management: use a `CartService` that syncs between localStorage (guest) and server API (logged in)
- On login: call `POST /api/cart/merge` to merge any localStorage cart into server-side cart
- Quantity stepper: reuse same component from format-selector; min 1, max 100
- Remove item: call service to remove; update totals reactively
- Cart icon badge: `CartService` exposes `itemCount$` observable; header component subscribes
- Empty cart: show illustration + `Adaugă fotografii` button linking to upload page
- `Continuă cumpărăturile` → navigate to upload page
- `Finalizează comanda` → navigate to checkout (triggers auth gate from US-108)

### Persistence Strategy
- **Guest**: full cart stored in localStorage as JSON; synced to server on each change if online
- **Logged-in**: cart stored server-side via Cart API; localStorage cleared after merge
- Cart items: `{ uploadId, productId, quantity, previewUrl, widthPx, heightPx }`

## Files to Create/Modify
- `src/app/features/upload/cart/cart.component.ts`
- `src/app/features/upload/cart/cart.component.html`
- `src/app/features/upload/cart/cart.component.scss`
- `src/app/core/services/cart.service.ts`
- `src/app/core/models/cart.model.ts`
- `src/app/shared/components/header/header.component.ts` (add cart badge)

## Testing
- Unit test: cart item add/remove/update quantity
- Unit test: localStorage persistence for guests
- Unit test: cart merge on login
- Unit test: cart badge count reactive updates
- Unit test: empty cart state display
- E2E: add items, modify quantities, proceed to checkout
