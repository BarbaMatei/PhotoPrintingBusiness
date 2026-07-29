---
id: 005-price-calculation-endpoint
unit: 001-product-catalog-core
intent: 003-product-catalog
status: complete
priority: must
created: 2026-05-20T20:35:00Z
assigned_bolt: 009-product-catalog-core
implemented: true
---

# Story: 005-price-calculation-endpoint

## User Story

**As a** client application
**I want** a server-side price calculation endpoint
**So that** I can get an authoritative price for any product size and quantity combination

## Acceptance Criteria

- [ ] **Given** a valid `sizeId` and `quantity = 5`, **When** `POST /api/products/{id}/calculate-price`, **Then** returns `{ unitPrice, totalPrice, tierApplied: "1-9" }`
- [ ] **Given** `quantity = 15`, **When** the request hits, **Then** the 10–49 tier is applied
- [ ] **Given** `quantity = 100`, **When** the request hits, **Then** the 50+ tier is applied
- [ ] **Given** `quantity = 0` or negative, **When** the request hits, **Then** returns 422 validation error
- [ ] **Given** a `sizeId` that belongs to a different product than the `{id}` in the URL, **When** the request hits, **Then** returns 404
- [ ] **Given** an inactive product or size, **When** the request hits, **Then** returns 404

## Technical Notes

- `[AllowAnonymous]` — used by both authenticated and guest customers
- Request: `{ sizeId: Guid, quantity: int }`
- Response: `{ unitPrice: decimal, totalPrice: decimal, tierApplied: string }`
- `tierApplied` is a human-readable string e.g. `"1-9"`, `"10-49"`, `"50+"`
- Reuses `PricingTierService.GetUnitPrice()` from story 002

## Dependencies

### Requires
- 002-quantity-tiered-pricing (tier lookup logic)
- 004-product-detail-endpoint (validates product/size ownership)

### Enables
- Nothing directly — used by checkout in future intent

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| `sizeId` not found | 404 |
| Quantity exactly at tier boundary (e.g. 10) | Lower price tier applies (≥10 = tier 2) |
| `quantity = 1` | Most expensive tier applies |
