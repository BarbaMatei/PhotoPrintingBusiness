---
id: 003-order-summary-panel
unit: 002-upload-format-cart-ui
intent: 004-checkout-payment
status: complete
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 014-upload-format-cart-ui
implemented: true
---

# Story: 003-order-summary-panel

## User Story

**As a** customer
**I want** to see a live-updating order summary with per-photo quantity controls and a running total
**So that** I know exactly what I'm about to add to my cart before confirming

## Acceptance Criteria

- [ ] **Given** photos are uploaded and format/finish is selected, **When** the summary panel is visible, **Then** it shows: format label, finish label, total photo count, per-line `quantity × unit price = line total (RON)`, and grand total (excl. shipping) in `XX,XX RON` format
- [ ] **Given** a quantity stepper is adjusted, **When** the value changes, **Then** the affected line total and grand total update immediately (synchronous, no API call)
- [ ] **Given** the quantity stepper is decremented below 1, **When** at minimum, **Then** the decrement button is disabled and `1` is enforced
- [ ] **Given** the quantity stepper is incremented above 100, **When** at maximum, **Then** the increment button is disabled and `100` is enforced
- [ ] **Given** there are no photos, **When** the `Adaugă în coș` button is shown, **Then** it is disabled and has `aria-disabled="true"`
- [ ] **Given** `Adaugă în coș` is clicked with valid photos, **When** the cart API responds with success, **Then** the nav cart badge increments and the user is routed to `/cos`

## Technical Notes

- Summary panel is a sticky sidebar on desktop, collapsible bottom sheet on mobile
- Price format: use Angular `DecimalPipe` with locale `ro` — `{{ price | number:'1.2-2' }} RON` with comma as decimal separator
- Quantities per upload stored in `UploadService` as `Signal<Map<uploadId, quantity>>`
- `Adaugă în coș` calls `CartService.replaceCart([...items])` → `POST /api/cart`
- After successful cart update, `CartService` emits new badge count

## Dependencies

### Requires
- Story 001-upload-page (upload items)
- Story 002-format-finish-selector (selected product + unit price)
- Story 005-cart-service (CartService for add-to-cart action)

### Enables
- Story 004-cart-page (user lands here after add-to-cart)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| `POST /api/cart` fails | Error toast: `"Nu s-a putut adăuga în coș. Încearcă din nou."` |
| All quantities set to 0 (edge) | Not possible — stepper min is 1 |
| 30 uploads × 100 quantity = 3000 prints | Accepted; total shown correctly |

## Out of Scope

- Shipping cost in this panel (shown as `Calculat la pasul următor`)
- Saving cart as draft / sharing cart link
