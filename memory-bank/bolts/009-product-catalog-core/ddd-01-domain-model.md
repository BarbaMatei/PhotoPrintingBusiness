---
stage: domain-model
bolt: 009-product-catalog-core
created: 2026-05-21T09:15:00Z
---

# Static Domain Model: product-catalog-core

## Stories Covered
- 001-product-entity-schema
- 002-quantity-tiered-pricing
- 003-public-catalog-endpoint
- 004-product-detail-endpoint
- 005-price-calculation-endpoint

---

## Entities

### Product
**Purpose**: Root of the catalog — represents a type of printable product offered by FotoTipar.

| Property | Type | Rules |
|----------|------|-------|
| Id | Guid | Required, immutable |
| Name | string | Required, max 200 chars |
| ProductType | string | Discriminator, default "PhotoPrint"; allows future canvas/book types |
| ImageUrl | string? | Optional CDN URL |
| SortOrder | int | ≥ 0, default 0; controls display order |
| IsActive | bool | Default true; false = hidden from public catalog |
| CreatedAt | DateTimeOffset | Set on creation, immutable |

**Business Rules**:
- A `Product` must have at least one active `ProductSize` to appear in the public catalog
- Deactivated products (`IsActive = false`) are invisible to customers but preserved for order history integrity
- `SortOrder` controls display sequence ascending

---

### ProductSize
**Purpose**: A specific physical size variant of a product (e.g. "10×15 cm").

| Property | Type | Rules |
|----------|------|-------|
| Id | Guid | Required, immutable |
| ProductId | Guid | FK → Product |
| Label | string | Required, max 50 chars (e.g. "10×15", "A4") |
| WidthMm | int | > 0 |
| HeightMm | int | > 0 |
| IsActive | bool | Default true |

**Business Rules**:
- `(ProductId, Label)` must be unique — no duplicate size labels within same product
- Deactivated sizes are excluded from public API responses
- A `ProductSize` must have at least one `PricingTier` before being activated

**Initial Seed Data** (6 standard photo print sizes):
| Label | WidthMm | HeightMm |
|-------|---------|----------|
| 10×15 | 100 | 150 |
| 13×18 | 130 | 180 |
| 15×21 | 150 | 210 |
| 20×30 | 200 | 300 |
| A4 | 210 | 297 |
| A3 | 297 | 420 |

---

### ProductFinish
**Purpose**: A finish/paper-type option available for a product (e.g. glossy, matte).

| Property | Type | Rules |
|----------|------|-------|
| Id | Guid | Required, immutable |
| ProductId | Guid | FK → Product |
| Name | string | Required, max 50 chars (e.g. "Glossy", "Matte") |

**Business Rules**:
- Finishes are product-level (all sizes of a product share the same finish options)
- No price modifier in this intent — finishes are cosmetic choices
- `(ProductId, Name)` should be unique

---

### PricingTier
**Purpose**: Defines the unit price for a specific quantity range of a size. Implements bulk-discount pricing.

| Property | Type | Rules |
|----------|------|-------|
| Id | Guid | Required, immutable |
| ProductSizeId | Guid | FK → ProductSize |
| MinQuantity | int | ≥ 1 |
| MaxQuantity | int? | nullable = open-ended (50+) |
| UnitPrice | decimal | > 0, precision (10, 2), in RON |

**Business Rules**:
- Each `ProductSize` must have a complete, non-overlapping, contiguous tier set
- `MinQuantity` of the first tier must be 1
- Where `MaxQuantity` is not null: `MaxQuantity ≥ MinQuantity`
- Tiers must be contiguous: `tier[n].MaxQuantity + 1 == tier[n+1].MinQuantity`
- Tiers must be monotonically non-increasing in price: `tier[n].UnitPrice ≥ tier[n+1].UnitPrice`
- A single open-ended tier (MinQuantity=1, MaxQuantity=null) is valid (flat pricing)

**Default Tier Structure** (seeded for all 6 sizes, prices are example defaults in RON):
| Tier | MinQty | MaxQty | Notes |
|------|--------|--------|-------|
| 1 | 1 | 9 | Highest unit price |
| 2 | 10 | 49 | Mid-range discount |
| 3 | 50 | null | Best unit price (50+) |

---

## Value Objects

### Money
- **Properties**: Amount (decimal, precision 10,2), Currency (string, "RON")
- **Constraints**: Amount > 0; Currency must be "RON" at MVP
- **Note**: `UnitPrice` and `TotalPrice` are represented as `decimal` in C# but conceptually this is a Money value object

### QuantityRange
- **Properties**: Min (int ≥ 1), Max (int? nullable)
- **Constraints**: If Max is not null, Max ≥ Min
- **Equality**: Two ranges are equal if Min and Max are identical
- **Used by**: `PricingTier`

### TierResult
- **Properties**: UnitPrice (decimal), TotalPrice (decimal), TierLabel (string, e.g. "10-49")
- **Purpose**: Output of price calculation — not persisted, computed on demand

---

## Aggregates

### ProductAggregate
**Root**: `Product`
**Members**: `ProductSize[]`, `ProductFinish[]`, `PricingTier[]` (via ProductSize)

**Invariants**:
1. At most one active size with a given label per product
2. Pricing tiers per size are contiguous, non-overlapping, non-increasing in price
3. Cascade rules: deleting a `Product` deletes its sizes, finishes, and tiers

**Access Rule**: `PricingTier` is only accessed through its parent `ProductSize`, which is accessed through `Product`. No direct tier queries bypassing the aggregate.

---

## Domain Events

### ProductCreated
- **Trigger**: A new product is created by admin
- **Payload**: `{ ProductId, Name, ProductType, CreatedAt }`
- **Note**: Not used for external side effects in this bolt; captured for future catalog cache invalidation

### ProductDeactivated
- **Trigger**: Admin sets `IsActive = false`
- **Payload**: `{ ProductId, DeactivatedAt }`
- **Note**: Future use — could trigger cache bust of `GET /api/products`

### PricingTiersUpdated
- **Trigger**: Admin replaces pricing tiers for a size
- **Payload**: `{ ProductSizeId, NewTierCount, UpdatedAt }`

---

## Domain Services

### PricingService
**Purpose**: Encapsulates tier lookup and price calculation logic — separated from entities to keep them anemic and testable.

**Operations**:
- `GetApplicableTier(IEnumerable<PricingTier> tiers, int quantity) → PricingTier`
  - Finds the tier where `MinQuantity ≤ quantity ≤ MaxQuantity` (or `MaxQuantity = null` for open-ended)
  - Throws `InvalidOperationException` if no tier covers the quantity (data integrity error)
- `Calculate(PricingTier tier, int quantity) → TierResult`
  - Returns `{ UnitPrice, TotalPrice = UnitPrice × quantity, TierLabel }`
- `ValidateTiers(IEnumerable<CreatePricingTierRequest> tiers) → ValidationResult`
  - Checks contiguity, non-overlap, monotonic pricing — used by admin endpoints

### ProductCatalogService
**Purpose**: Orchestrates product queries with active-status filtering.

**Operations**:
- `GetActiveCatalog() → IEnumerable<ProductDto>`
- `GetActiveProductById(Guid id) → ProductDto` (throws `NotFoundException` if not found or inactive)

---

## Repository Interfaces

### IProductRepository
- `GetAllActiveAsync() → Task<IEnumerable<Product>>`
  - Returns products with IsActive=true, including active sizes, finishes, and pricing tiers
- `GetByIdAsync(Guid id) → Task<Product?>`
  - Returns product regardless of active status (admin needs inactive too)
- `GetActiveByIdAsync(Guid id) → Task<Product?>`
  - Returns only if IsActive=true (public endpoints)
- `AddAsync(Product product) → Task`
- `UpdateAsync(Product product) → Task`
- `DeleteAsync(Guid id) → Task`

---

## Ubiquitous Language

| Term | Definition |
|------|------------|
| **Product** | A type of printable item offered (e.g. "Photo Print"). Not an order line. |
| **Size** | A physical dimension variant of a product (e.g. "10×15 cm"). |
| **Finish** | A paper/surface treatment option (Glossy, Matte). No price impact. |
| **Pricing Tier** | A quantity bracket with a unit price (e.g. 10–49 units @ 0.80 RON each). |
| **Catalog** | The publicly visible set of all active products with their sizes and prices. |
| **Active** | `IsActive = true` — visible to customers. |
| **Inactive** | `IsActive = false` — hidden from customers but preserved in the system. |
| **Unit Price** | Price per single item at a given quantity tier (in RON). |
| **Total Price** | `UnitPrice × quantity`. |
| **Tier Label** | Human-readable range string for the applied tier (e.g. "10-49", "50+"). |
| **Open-ended tier** | A tier with `MaxQuantity = null` — applies to any quantity ≥ MinQuantity. |
| **Contiguous tiers** | Tiers covering a continuous range with no gaps between ranges. |
