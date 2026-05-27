---
id: 007-admin-pricing-management
unit: 001-product-catalog-core
intent: 003-product-catalog
status: draft
priority: must
created: 2026-05-20T20:35:00Z
assigned_bolt: 010-product-catalog-admin
implemented: false
---

# Story: 007-admin-pricing-management

## User Story

**As an** admin
**I want** to update the pricing tiers for any product size
**So that** I can adjust prices without code deployments

## Acceptance Criteria

- [ ] **Given** a valid admin JWT, **When** `PUT /api/admin/products/{id}/sizes/{sizeId}/pricing` with valid tiers array, **Then** returns 200 and atomically replaces all tiers for that size
- [ ] **Given** tiers where a higher quantity range has a higher unit price, **When** the request is validated, **Then** returns 422 with a descriptive error
- [ ] **Given** overlapping tier ranges (e.g. 1–10 and 5–20), **When** the request is validated, **Then** returns 422
- [ ] **Given** missing tier (gap in range, e.g. 1–9 then 11–49), **When** the request is validated, **Then** returns 422
- [ ] **Given** a `sizeId` not belonging to the specified product, **When** the request is processed, **Then** returns 404
- [ ] **Given** an update with a single open-ended tier (MinQuantity=1, MaxQuantity=null), **When** saved, **Then** returns 200 — single-tier pricing is valid

## Technical Notes

- Full replace semantics: DELETE existing tiers for size, INSERT new ones in a transaction
- Request: `{ tiers: [{ minQuantity, maxQuantity?, unitPrice }] }`
- Validation rules: contiguous ranges, monotonically non-increasing price, minQuantity=1 for first tier
- Decimal precision: `unitPrice` to 2 decimal places, currency in RON
- Return updated size with tiers in response

## Dependencies

### Requires
- 002-quantity-tiered-pricing (PricingTier entity + validation logic)
- 006-admin-product-management (admin auth pattern established)

### Enables
- FR-10 Admin UI pricing editor

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| `unitPrice = 0` | 422 — price must be > 0 |
| `minQuantity > maxQuantity` | 422 — invalid range |
| Same price for all tiers | Valid — allowed but unusual |
