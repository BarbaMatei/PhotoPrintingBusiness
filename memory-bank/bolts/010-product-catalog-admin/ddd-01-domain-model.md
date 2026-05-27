---
stage: domain-model
bolt: 010-product-catalog-admin
created: 2026-05-21T10:05:00Z
---

# Static Domain Model: product-catalog-admin

## Stories Covered
- 006-admin-product-management
- 007-admin-pricing-management

## Context
The core domain entities (`Product`, `ProductSize`, `ProductFinish`, `PricingTier`) and the `PricingService` were defined and implemented in bolt 009. This model extends those with the **write-side** admin operations: commands, validators, and domain rules for mutations.

---

## Command Operations

### CreateProduct
**Trigger**: Admin `POST /api/admin/products`
**Input**:
| Field | Type | Rules |
|-------|------|-------|
| Name | string | Required, max 200 chars |
| ProductType | string | Optional, default "PhotoPrint" |
| ImageUrl | string? | Optional, max 500 chars |
| SortOrder | int | ≥ 0 |
| Sizes | CreateProductSizeRequest[] | Min 1 element |

**`CreateProductSizeRequest`**:
| Field | Type | Rules |
|-------|------|-------|
| Label | string | Required, max 50 chars |
| WidthMm | int | ≥ 1 |
| HeightMm | int | ≥ 1 |

**Business Rules**:
- Must include at least one size
- No two sizes in the same request may share the same label
- Finishes are NOT set on create — they are product-level and managed separately (or defaulted to Glossy/Matte on create)
- New product is created with `IsActive = true`

---

### UpdateProduct
**Trigger**: Admin `PUT /api/admin/products/{id}`
**Input**: same shape as CreateProduct (full replacement of Name, ImageUrl, SortOrder, ProductType)
**Business Rules**:
- Size list is NOT part of update — sizes are managed via separate endpoints
- `IsActive` is NOT toggled here — use SetProductStatus command
- 404 if product not found

---

### SetProductStatus
**Trigger**: Admin `PATCH /api/admin/products/{id}/status`
**Input**: `{ isActive: bool }`
**Business Rules**:
- Flips `IsActive` to the requested value
- Idempotent: setting IsActive=true on already-active product is valid (200 OK)
- Does NOT cascade to sizes (sizes retain their own IsActive state)
- 404 if product not found

---

### DeleteProduct
**Trigger**: Admin `DELETE /api/admin/products/{id}`
**Business Rules**:
- **Soft-delete** (set `IsActive = false`) — preserves data integrity for future order history
- Does NOT physically remove rows
- 404 if product not found
- Returns 204 No Content on success

---

### AddProductSize
**Trigger**: Admin `POST /api/admin/products/{id}/sizes`
**Input**:
| Field | Type | Rules |
|-------|------|-------|
| Label | string | Required, max 50 chars |
| WidthMm | int | ≥ 1 |
| HeightMm | int | ≥ 1 |

**Business Rules**:
- Label must be unique within the product (no existing active or inactive size with same label)
- New size created with `IsActive = false` by default — must have pricing tiers before activation
- 404 if product not found
- 409 Conflict if label already exists for the product

---

### SetProductSizeStatus
**Trigger**: Admin `PATCH /api/admin/products/{id}/sizes/{sizeId}/status`
**Input**: `{ isActive: bool }`
**Business Rules**:
- Cannot activate a size that has no pricing tiers — returns 422 with message
- 404 if product or size not found / size does not belong to product

---

### ReplacePricingTiers
**Trigger**: Admin `PUT /api/admin/products/{id}/sizes/{sizeId}/pricing`
**Input**:
```
{
  tiers: [
    { minQuantity: int, maxQuantity?: int, unitPrice: decimal }
  ]
}
```
**Business Rules** (all validated via `PricingService.ValidateTiers`):
1. `tiers` array must not be empty (at least 1 tier required)
2. First tier's `minQuantity` must be 1
3. `unitPrice` > 0 for every tier
4. `minQuantity` ≥ 1 for every tier
5. Where `maxQuantity` is not null: `maxQuantity ≥ minQuantity`
6. Tiers must be contiguous: `tier[n].maxQuantity + 1 == tier[n+1].minQuantity` (no gaps)
7. No overlapping ranges
8. Monotonically non-increasing price: `tier[n].unitPrice ≥ tier[n+1].unitPrice`
9. Exactly one tier may have `maxQuantity = null` and it must be the last tier
**Operation**: Atomic — delete all existing tiers for the size, insert new ones in a single transaction
**Returns**: Updated size with new tiers (200 OK)
**Errors**: 404 if product/size not found or size not owned by product; 422 if any validation rule fails

---

## Validation Rules Summary

### `PricingService.ValidateTiers` (extends bolt 009 domain service)
New method added to existing `PricingService`:

```text
ValidateTiers(IEnumerable<CreatePricingTierRequest> tiers) → (bool IsValid, string? Error)
```

Checks in order:
1. Not empty
2. First MinQuantity = 1
3. All UnitPrice > 0
4. All MinQuantity ≥ 1
5. No null MaxQuantity except on last tier
6. MaxQuantity ≥ MinQuantity (where not null)
7. Contiguous: tier[n].MaxQuantity + 1 == tier[n+1].MinQuantity
8. Non-increasing price

Returns the first validation error encountered (fail-fast).

---

## Authorization Model

| Operation | Required Role |
|-----------|---------------|
| All `POST /api/admin/*` | Admin JWT |
| All `PUT /api/admin/*` | Admin JWT |
| All `PATCH /api/admin/*` | Admin JWT |
| All `DELETE /api/admin/*` | Admin JWT |
| Unauthenticated request | 401 |
| Authenticated non-admin | 403 |

Uses existing `[Authorize(Roles = "Admin")]` claim from bolt 005/006 auth infrastructure.

---

## Aggregate Invariants (extensions from bolt 009)

### ProductAggregate — write-side additions
1. **On CreateProduct**: persist product + sizes atomically; default finishes (Glossy/Matte) may be added
2. **On ReplacePricingTiers**: all-or-nothing replacement; validation must pass before any DB write
3. **On DeleteProduct**: soft-delete only; child sizes/tiers remain in DB for auditability

---

## Repository Interface Extensions

### IProductRepository (additions to bolt 009)
```text
AddAsync(Product product) → Task                          ← already defined, now used
UpdateAsync(Product product) → Task                       ← already defined
GetByIdAsync(Guid id) → Task<Product?>                   ← already defined (admin needs inactive)
GetSizeByIdAsync(Guid productId, Guid sizeId) → Task<ProductSize?>
AddSizeAsync(ProductSize size) → Task
DeleteTiersForSizeAsync(Guid sizeId) → Task
AddTiersAsync(IEnumerable<PricingTier> tiers) → Task
```

---

## Ubiquitous Language (additions)

| Term | Definition |
|------|------------|
| **Soft-delete** | Setting `IsActive = false` to hide without removing data |
| **Atomic tier replacement** | Delete + insert tiers for a size in a single DB transaction |
| **Tier validation** | Set of rules ensuring tiers are contiguous, non-overlapping, and price-descending |
| **Admin command** | A write operation requiring admin role; mutates catalog state |
