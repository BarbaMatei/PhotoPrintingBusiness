---
unit: 001-product-catalog-core
intent: 003-product-catalog
unit_type: backend
default_bolt_type: ddd-construction-bolt
phase: inception
status: draft
created: 2026-05-20T20:35:00Z
updated: 2026-05-20T20:35:00Z
---

# Unit Brief: product-catalog-core

## Purpose

Backend domain model, EF Core schema, and REST API for the FotoTipar product catalog. Covers product/size/finish entities, quantity-tiered pricing storage and lookup, public read endpoints, and admin-protected CRUD endpoints.

## Scope

### In Scope
- `Product` aggregate: entity, size variants, finish options, active/inactive status
- `PricingTier` entity: per-size, quantity range (min/max), unit price
- EF Core migrations for all new tables
- `GET /api/products` — public catalog list
- `GET /api/products/{id}` — public product detail with all tier data
- `POST /api/products/{id}/calculate-price` — server-side price calculation
- `POST/PUT/DELETE /api/admin/products/**` — admin-protected CRUD
- `PUT /api/admin/products/{id}/sizes/{sizeId}/pricing` — admin pricing tier update
- FluentValidation for all request bodies
- Unit + integration tests

### Out of Scope
- Angular UI (unit-002)
- Image storage/CDN hosting (link stored, not served)
- Inventory tracking
- Promotional codes

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-1 | Product entity with sizes and finishes | Must |
| FR-2 | Quantity-tiered pricing per product size | Must |
| FR-3 | Public product catalog API endpoint | Must |
| FR-4 | Product detail endpoint | Must |
| FR-5 | Price calculation endpoint | Must |
| FR-6 | Admin product management API | Must |
| FR-7 | Admin pricing management API | Must |

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|--------|-------------|------------|
| `Product` | A printable product type | Id, Name, ProductType (discriminator), IsActive, ImageUrl, SortOrder |
| `ProductSize` | A size variant of a product | Id, ProductId, Label (e.g. "10×15"), WidthMm, HeightMm, IsActive |
| `ProductFinish` | A finish option for a product | Id, ProductId, Name ("Glossy"/"Matte") |
| `PricingTier` | A quantity-range price for a size | Id, ProductSizeId, MinQuantity, MaxQuantity (nullable=50+), UnitPrice |

### Key Invariants
- A `ProductSize` must have at least one `PricingTier`
- Tier ranges must be contiguous and non-overlapping within a size
- Higher-quantity tiers must have lower or equal unit price
- Only active products/sizes appear in public API responses

### Bounded Context
Self-contained catalog bounded context. Depends on `ApplicationUser` (for admin identity check) but owns no auth logic.

---

## Technical Notes

- Use `productType` discriminator column (string) — value `"PhotoPrint"` for all initial products; allows future types without schema change
- `PricingTier.MaxQuantity` is nullable: `null` means "50 and above" (open-ended tier)
- Admin endpoints reuse existing `[Authorize]` + `isAdmin` claim pattern from bolt-005
- Response DTOs include all tier data so Angular can do client-side price calculation
- Cache `GET /api/products` with `[ResponseCache]` for 60 seconds

---

## Success Criteria

- `GET /api/products` returns seeded catalog in < 200ms p95
- Price calculation returns correct tier for any quantity input
- Admin cannot create overlapping or ascending-price tiers (422 validation)
- All endpoints covered by integration tests using `WebApplicationFactory`
