---
id: 002-format-selection-price-calculator
unit: 002-product-catalog-ui
intent: 003-product-catalog
status: draft
priority: must
created: 2026-05-20T20:35:00Z
assigned_bolt: 011-product-catalog-ui
implemented: false
---

# Story: 002-format-selection-price-calculator

## User Story

**As a** customer
**I want** to choose a size, finish, and quantity for a product and see the price update instantly
**So that** I know exactly what I'll pay before adding to my cart

## Acceptance Criteria

- [ ] **Given** I navigate to `/tipareste/{id}`, **When** the page loads, **Then** all active sizes and finishes for the product are displayed
- [ ] **Given** I select a size and enter a quantity, **When** either input changes, **Then** the unit price and total price update instantly (no network call)
- [ ] **Given** quantity = 15, **When** displayed, **Then** the 10–49 tier price is shown with a "10–49 buc." badge
- [ ] **Given** I have not selected a size, **When** the "Adaugă în coș" button is rendered, **Then** it is disabled
- [ ] **Given** quantity = 0 or empty, **When** rendered, **Then** "Adaugă în coș" button is disabled
- [ ] **Given** a valid selection, **When** "Adaugă în coș" is clicked, **Then** emits an `addToCart` event (wired in future cart intent)

## Technical Notes

- Route: `{ path: 'tipareste/:id', loadComponent: () => import('./pages/format-selector/format-selector-page') }`
- `ProductService.getProduct(id)` → `GET /api/products/{id}`
- Client-side price calculation: `calcPrice(tiers, quantity) → { unitPrice, total, tierLabel }`
- Reactive form for size selector (radio/select) + quantity (number input, min=1)
- `toSignal()` on product observable; computed signals for price
- Finish toggle: glossy/matte — stored in selection state but no price impact in this intent

## Dependencies

### Requires
- 001-product-catalog-page (user arrives from catalog)
- `009-product-catalog-core` bolt complete (needs `GET /api/products/{id}`)

### Enables
- Shopping cart (future intent — receives `{ productId, sizeId, finishId, quantity }`)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Product not found (404) | Shows error page with back button |
| Quantity entered as text | Input validation prevents non-numeric input |
| Only one size available | Pre-select it automatically |
| Quantity at exact tier boundary (10) | Display lower unit price tier |
