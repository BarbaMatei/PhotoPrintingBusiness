---
id: 003-public-catalog-endpoint
unit: 001-product-catalog-core
intent: 003-product-catalog
status: draft
priority: must
created: 2026-05-20T20:35:00Z
assigned_bolt: 009-product-catalog-core
implemented: false
---

# Story: 003-public-catalog-endpoint

## User Story

**As a** customer
**I want** to load the full product catalog via a public API
**So that** the Angular app can display all available print formats and their prices

## Acceptance Criteria

- [ ] **Given** an unauthenticated request, **When** `GET /api/products`, **Then** returns 200 with all active products
- [ ] **Given** a product with `IsActive = false`, **When** `GET /api/products`, **Then** that product is excluded from the response
- [ ] **Given** the response, **When** parsed, **Then** each product includes: `id`, `name`, `productType`, `imageUrl`, `sortOrder`, `sizes[]` (each with label, dimensions, finishes[], pricingTiers[])
- [ ] **Given** a response cache header, **When** consumed, **Then** `Cache-Control: public, max-age=60`
- [ ] **Given** no active products, **When** `GET /api/products`, **Then** returns 200 with empty array

## Technical Notes

- `[AllowAnonymous]` — no auth required
- `[ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]`
- Project `ProductSize.IsActive = false` sizes out of response
- Sort by `Product.SortOrder ASC`
- Use projection DTOs — never return EF entities directly

## Dependencies

### Requires
- 001-product-entity-schema
- 002-quantity-tiered-pricing (pricing tiers included in response)

### Enables
- 004-product-detail-endpoint (same DTOs)
- FR-8 Angular catalog page (consumes this endpoint)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Product active but all its sizes inactive | Product appears but `sizes: []` |
| Very large catalog (1000+ products) | Response within 200ms p95 (index on IsActive) |
