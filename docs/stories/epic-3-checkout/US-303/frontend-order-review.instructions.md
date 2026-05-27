# US-303 — Checkout — Order Review (Frontend)

## Story
**As a** customer  
**I want to** see a complete summary of my order before paying

## Type
FRONTEND — Angular

## Epic
EPIC-3 | Checkout & Plată

## Dependencies
- US-301 (Delivery method selected)
- US-205 (Cart data)

## Acceptance Criteria

1. **Step 2**: read-only summary — photo count, format/finish, subtotal, delivery method, address/locker, shipping cost, **GRAND TOTAL in RON**
2. **Estimated delivery**: `2–4 zile lucrătoare`
3. **`Modifică coșul`** and **`Modifică adresa`** edit links
4. **Acceptance of Terms** checkbox (required); link opens `/termeni-si-conditii` in new tab
5. **`Plătește acum`** button enabled only when Terms accepted

## Technical Notes

### Component Location
`src/app/features/checkout/review-step/review-step.component.ts`

### Implementation Details
- Read checkout state from service (cart items, selected delivery, address/locker)
- Display all items: thumbnail, format name, finish, quantity, unit price, line total
- Subtotal: sum of all line totals
- Shipping cost: from delivery step selection
- Grand total: subtotal + shipping
- Edit links: navigate back to respective steps without losing data
- Terms checkbox: links to `/termeni-si-conditii` (opens new tab via `target="_blank"`)
- `Plătește acum` button: disabled until terms checked; on click → navigate to Step 3 (Payment)

### UI/UX
- Clean, organized summary layout
- Prices right-aligned, RON currency format
- Estimated delivery badge: `Livrare estimată: 2–4 zile lucrătoare`
- All text in Romanian

## Files to Create/Modify
- `src/app/features/checkout/review-step/review-step.component.ts`
- `src/app/features/checkout/review-step/review-step.component.html`
- `src/app/features/checkout/review-step/review-step.component.scss`

## Testing
- Unit test: all order data displayed correctly
- Unit test: terms checkbox enables/disables button
- Unit test: edit links navigate correctly
- E2E: review step displays correct totals
