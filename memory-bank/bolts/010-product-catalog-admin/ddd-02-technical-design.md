---
stage: design
bolt: 010-product-catalog-admin
created: 2026-05-21T10:10:00Z
---

# Technical Design: product-catalog-admin

## Architecture Pattern

Same layered architecture as bolt 009 and the rest of the project.  
All admin code lives in `PhotoPrint.API` — no new projects.

---

## Layer Structure

```text
┌──────────────────────────────────────────────────────┐
│  Presentation (Controllers/)                         │
│  AdminProductsController — admin CRUD routes         │
├──────────────────────────────────────────────────────┤
│  Application (Services/)                             │
│  AdminProductService — orchestrates writes + queries │
├──────────────────────────────────────────────────────┤
│  Domain (Services/)                                  │
│  PricingService.ValidateTiers — tier business rules  │
├──────────────────────────────────────────────────────┤
│  Infrastructure (Data/ + Validators/)                │
│  EF Core + FluentValidation request validators       │
└──────────────────────────────────────────────────────┘
```

---

## API Design

All routes require **`[Authorize(Roles = "Admin")]`** — JWT with `ClaimTypes.Role = "Admin"`.  
Unauthenticated → 401. Authenticated non-admin → 403.

---

### `POST /api/admin/products`
_Story 006 — Create product with initial sizes_

**Request body**:
```json
{
  "name": "Poze foto",
  "productType": "PhotoPrint",
  "imageUrl": null,
  "sortOrder": 0,
  "sizes": [
    { "label": "10×15", "widthMm": 100, "heightMm": 150 }
  ]
}
```
**Validation** (FluentValidation):
- `name`: required, max 200
- `sortOrder`: ≥ 0
- `sizes`: min 1 element
- `sizes[].label`: required, max 50
- `sizes[].widthMm / heightMm`: ≥ 1

**Response `201 Created`**: full `ProductDto` (same shape as public GET)

**Behaviour**:
- Creates product with `IsActive = true`
- Creates all sizes with `IsActive = false` (no tiers yet)
- Creates default finishes: "Lucioasă" and "Mată"

---

### `PUT /api/admin/products/{id}`
_Story 006 — Update product metadata_

**Request body**:
```json
{ "name": "Poze foto color", "productType": "PhotoPrint", "imageUrl": "https://...", "sortOrder": 1 }
```
**Validation**: same as create (without sizes)

**Response `200 OK`**: updated `ProductDto`  
**Response `404`**: product not found

---

### `PATCH /api/admin/products/{id}/status`
_Story 006 — Toggle active flag_

**Request body**: `{ "isActive": false }`

**Response `200 OK`**: `{ "id": "...", "isActive": false }`  
**Response `404`**: product not found

---

### `DELETE /api/admin/products/{id}`
_Story 006 — Soft-delete product_

**Response `204 No Content`**  
**Response `404`**: product not found  
**Note**: Sets `IsActive = false` — does NOT remove rows

---

### `POST /api/admin/products/{id}/sizes`
_Story 006 — Add a new size variant_

**Request body**: `{ "label": "A5", "widthMm": 148, "heightMm": 210 }`

**Response `201 Created`**: `ProductSizeDto` with empty `pricingTiers`  
**Response `404`**: product not found  
**Response `409 Conflict`**: label already exists for this product

---

### `PATCH /api/admin/products/{id}/sizes/{sizeId}/status`
_Story 006 — Toggle size active flag_

**Request body**: `{ "isActive": true }`

**Response `200 OK`**: `{ "id": "...", "isActive": true }`  
**Response `404`**: product or size not found  
**Response `422`**: activating a size that has no pricing tiers

---

### `PUT /api/admin/products/{id}/sizes/{sizeId}/pricing`
_Story 007 — Replace all pricing tiers (atomic)_

**Request body**:
```json
{
  "tiers": [
    { "minQuantity": 1,  "maxQuantity": 9,    "unitPrice": 1.20 },
    { "minQuantity": 10, "maxQuantity": 49,   "unitPrice": 0.90 },
    { "minQuantity": 50, "maxQuantity": null,  "unitPrice": 0.70 }
  ]
}
```
**Validation** (FluentValidation + PricingService.ValidateTiers):
- `tiers`: not empty
- Each `minQuantity` ≥ 1
- Each `unitPrice` > 0 and ≤ 2 decimal places
- No `maxQuantity` < `minQuantity`
- Business rules: contiguous, non-overlapping, monotonically non-increasing price
- Exactly one open-ended tier (null maxQuantity), must be last

**Response `200 OK`**: `ProductSizeDto` with new tiers  
**Response `404`**: size not found or not owned by product  
**Response `422`**: tier validation failure with descriptive error message

---

## File / Class Map

```text
src/PhotoPrint.API/
├── Controllers/
│   └── AdminProductsController.cs           # all 7 admin endpoints
├── DTOs/
│   └── Admin/
│       ├── CreateProductRequest.cs          # POST /api/admin/products
│       ├── UpdateProductRequest.cs          # PUT /api/admin/products/{id}
│       ├── SetStatusRequest.cs              # PATCH status endpoints (shared)
│       ├── CreateProductSizeRequest.cs      # POST /api/admin/products/{id}/sizes
│       └── ReplacePricingTiersRequest.cs    # PUT /api/admin/products/{id}/sizes/{sizeId}/pricing
├── Validators/
│   └── Admin/
│       ├── CreateProductRequestValidator.cs
│       ├── UpdateProductRequestValidator.cs
│       ├── CreateProductSizeRequestValidator.cs
│       └── ReplacePricingTiersRequestValidator.cs
└── Services/
    ├── IAdminProductService.cs
    └── AdminProductService.cs
```

**PricingService** (`Services/PricingService.cs`) — add `ValidateTiers` method (no new file)

---

## Security Design

| Concern | Approach |
|---------|----------|
| **Authentication** | `[Authorize]` on controller — JWT Bearer required |
| **Authorization** | `[Authorize(Roles = "Admin")]` — `ClaimTypes.Role = "Admin"` in JWT (set by `TokenService`) |
| **Input validation** | FluentValidation via `ValidationFilter` (bolt 001) — 422 on invalid input |
| **SQL injection** | EF Core parameterized queries only |
| **Soft-delete** | Rows never removed — preserves auditability |
| **Atomic tier replace** | EF transaction via `SaveChangesAsync` — delete + insert in same DbContext |

---

## NFR Implementation

| Requirement | Design Approach |
|-------------|----------------|
| **Atomicity** | Tier replacement: `db.PricingTiers.RemoveRange(...)` + `db.PricingTiers.AddRange(...)` in a single `SaveChangesAsync` call |
| **Consistency** | `PricingService.ValidateTiers` validates all 8 rules before any DB write |
| **Cache invalidation** | Response cache (`[ResponseCache]`) is process-local; admin writes do not explicitly bust it — 5-minute TTL is acceptable for MVP |
| **Testability** | `AdminProductService` depends on `PhotoPrintDbContext` + `PricingService` only — easy to test with InMemory DB |
