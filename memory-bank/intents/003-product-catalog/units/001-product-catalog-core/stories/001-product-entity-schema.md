---
id: 001-product-entity-schema
unit: 001-product-catalog-core
intent: 003-product-catalog
status: draft
priority: must
created: 2026-05-20T20:35:00Z
assigned_bolt: 009-product-catalog-core
implemented: false
---

# Story: 001-product-entity-schema

## User Story

**As an** admin
**I want** products, sizes, and finishes to be stored in the database with proper schema
**So that** the catalog can be queried and managed reliably

## Acceptance Criteria

- [ ] **Given** the API starts, **When** migrations run, **Then** `Products`, `ProductSizes`, `ProductFinishes`, and `PricingTiers` tables exist with correct columns and indexes
- [ ] **Given** a product entity, **When** saved, **Then** it persists `Id`, `Name`, `ProductType`, `IsActive`, `ImageUrl`, `SortOrder`
- [ ] **Given** a product size, **When** saved, **Then** it persists `Id`, `ProductId`, `Label`, `WidthMm`, `HeightMm`, `IsActive`
- [ ] **Given** a product finish, **When** saved, **Then** it persists `Id`, `ProductId`, `Name`
- [ ] **Given** a `ProductType` discriminator column, **When** new product types are added later, **Then** no schema migration is required

## Technical Notes

- EF Core entity config in `Data/Configurations/`
- `ProductType` is a `string` column with default `"PhotoPrint"`
- Seed 6 standard photo print sizes: 10×15, 13×18, 15×21, 20×30, A4, A3
- Add migration `AddProductCatalogTables`
- Follow existing `PhotoPrintDbContext` conventions

## Dependencies

### Requires
- None

### Enables
- 002-quantity-tiered-pricing (needs ProductSize entity)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Product deleted with active sizes | Cascade delete sizes and finishes |
| ProductSize label collision on same product | DB unique index prevents it |
