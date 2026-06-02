---
id: 002-quantity-tiered-pricing
unit: 001-product-catalog-core
intent: 003-product-catalog
status: complete
priority: must
created: 2026-05-20T20:35:00Z
assigned_bolt: 009-product-catalog-core
implemented: true
---

# Story: 002-quantity-tiered-pricing

## User Story

**As an** admin
**I want** each product size to have quantity-based pricing tiers (1–9, 10–49, 50+)
**So that** customers get lower unit prices for larger orders

## Acceptance Criteria

- [ ] **Given** a product size with 3 tiers, **When** I query pricing for quantity 5, **Then** the tier with MinQuantity ≤ 5 ≤ MaxQuantity is returned
- [ ] **Given** a product size with 3 tiers, **When** I query pricing for quantity 50, **Then** the open-ended tier (MaxQuantity = null) is returned
- [ ] **Given** an attempt to save overlapping tiers, **When** validation runs, **Then** a 422 error is returned
- [ ] **Given** an attempt to save tiers where a higher quantity tier has a higher price, **When** validation runs, **Then** a 422 error is returned
- [ ] **Given** a `PricingTier`, **When** saved, **Then** it persists `Id`, `ProductSizeId`, `MinQuantity`, `MaxQuantity` (nullable), `UnitPrice`

## Technical Notes

- `MaxQuantity = null` represents open-ended upper tier (50+)
- `PricingTierService.GetUnitPrice(sizeId, quantity)` — picks correct tier
- Validation: tiers must be contiguous, non-overlapping, monotonically non-increasing price
- Seed default tiers for all 6 sizes during migration

## Dependencies

### Requires
- 001-product-entity-schema (needs ProductSize entity)

### Enables
- 003-public-catalog-endpoint (needs tier data in responses)
- 005-price-calculation-endpoint (needs tier lookup logic)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Quantity = 0 or negative | Validation error — minimum quantity is 1 |
| No tier covers given quantity | Return cheapest applicable tier or 422 |
| Single tier covering all quantities | Valid — MinQuantity=1, MaxQuantity=null |
