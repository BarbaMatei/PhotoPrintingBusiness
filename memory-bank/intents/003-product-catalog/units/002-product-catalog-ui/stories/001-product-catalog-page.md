---
id: 001-product-catalog-page
unit: 002-product-catalog-ui
intent: 003-product-catalog
status: complete
priority: must
created: 2026-05-20T20:35:00Z
assigned_bolt: 011-product-catalog-ui
implemented: true
---

# Story: 001-product-catalog-page

## User Story

**As a** customer
**I want** to see all available print products when I visit `/tipareste`
**So that** I can choose what to order

## Acceptance Criteria

- [ ] **Given** I navigate to `/tipareste`, **When** the page loads, **Then** all active products are displayed in a responsive grid
- [ ] **Given** a product card, **When** rendered, **Then** it shows product name, thumbnail image, available size labels, and "de la X lei" (lowest tier unit price)
- [ ] **Given** a loading state, **When** catalog is fetching, **Then** skeleton cards are displayed
- [ ] **Given** an API error, **When** the catalog fails to load, **Then** an error message with retry option is shown
- [ ] **Given** I click a product card, **When** navigated, **Then** I land on `/tipareste/{id}` (format selection page)

## Technical Notes

- Route: `{ path: 'tipareste', loadComponent: () => import('./pages/catalog/catalog-page') }`
- `ProductService.getCatalog()` → `GET /api/products`
- Cache catalog in service memory for session duration (avoid repeat calls)
- `ChangeDetectionStrategy.OnPush`; `toSignal()` for async data
- `ProductCardComponent` — standalone, reusable
- `de la X lei` = minimum `unitPrice` across all sizes and tiers

## Dependencies

### Requires
- `009-product-catalog-core` bolt complete (needs `GET /api/products`)

### Enables
- 002-format-selection-price-calculator (navigated from this page)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Empty catalog (no active products) | Shows "No products available" message |
| Product with no active sizes | Show product card but no size labels |
| Slow API (>3s) | Loading skeleton remains visible |
