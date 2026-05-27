---
id: 004-product-detail-endpoint
unit: 001-product-catalog-core
intent: 003-product-catalog
status: draft
priority: must
created: 2026-05-20T20:35:00Z
assigned_bolt: 009-product-catalog-core
implemented: false
---

# Story: 004-product-detail-endpoint

## User Story

**As a** customer
**I want** to load a single product's full details
**So that** the Angular format selection page has all sizes, finishes, and pricing tiers

## Acceptance Criteria

- [ ] **Given** a valid active product `id`, **When** `GET /api/products/{id}`, **Then** returns 200 with full product DTO including all sizes, finishes, and pricing tiers
- [ ] **Given** an inactive product `id`, **When** `GET /api/products/{id}`, **Then** returns 404
- [ ] **Given** a non-existent `id`, **When** `GET /api/products/{id}`, **Then** returns 404
- [ ] **Given** an invalid GUID `id`, **When** `GET /api/products/{id}`, **Then** returns 400

## Technical Notes

- `[AllowAnonymous]`
- Same response DTO shape as catalog list item (no separate DTO needed)
- Returns 404 via existing `NotFoundException` → `ExceptionHandlerMiddleware` pipeline
- Inactive sizes within an active product are excluded from the response

## Dependencies

### Requires
- 003-public-catalog-endpoint (shares DTO definitions)

### Enables
- FR-9 Angular format selector (consumes this endpoint for single product view)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Product is active, zero active sizes | Returns product with `sizes: []` |
| Request with malformed GUID | Returns 400 Bad Request |
