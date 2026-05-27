---
stage: design
bolt: 009-product-catalog-core
created: 2026-05-21T09:20:00Z
---

# Technical Design: product-catalog-core

## Architecture Pattern

**Pattern**: Layered Architecture (consistent with existing codebase)

The existing project uses a flat-layered structure: `Controllers → Services → Models/Data`. No CQRS, no separate Application project — all logic lives in `PhotoPrint.API`. This bolt follows the same pattern to stay consistent with bolts 001–008.

---

## Layer Structure

```text
┌──────────────────────────────────────────────────────┐
│  Presentation (Controllers/)                         │
│  ProductsController — routes, auth, HTTP responses   │
├──────────────────────────────────────────────────────┤
│  Application (Services/)                             │
│  ProductService — orchestrates query + pricing logic │
├──────────────────────────────────────────────────────┤
│  Domain (Services/)                                  │
│  PricingService — tier lookup, price calculation     │
├──────────────────────────────────────────────────────┤
│  Infrastructure (Data/ + Models/)                    │
│  EF Core entities, PhotoPrintDbContext, migrations   │
└──────────────────────────────────────────────────────┘
```

**Responsibility breakdown**:
| Layer | Class(es) | Responsibility |
|-------|-----------|----------------|
| Controller | `ProductsController` | Route → call service → return DTO |
| Application | `ProductService` | EF Core queries (with `.Include()`), map to DTOs |
| Domain | `PricingService` | Tier lookup, price calculation, tier validation |
| Data | `PhotoPrintDbContext` + EF models | Persistence |

---

## API Design

### Stories covered: 003, 004, 005

All endpoints are **public** (no auth required) — the catalog must be accessible to guests.

---

### `GET /api/products`
_Story 003 — Public Catalog Endpoint_

Returns all active products with their active sizes, finishes, and pricing tiers. Embeds full tier data so the Angular client can calculate prices locally without additional HTTP calls.

**Request**: no body, no query params (no pagination — catalog is small, max ~5 products at MVP)

**Response `200 OK`**:
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Poze foto",
    "productType": "PhotoPrint",
    "imageUrl": "https://cdn.example.com/photo-print.jpg",
    "sortOrder": 0,
    "sizes": [
      {
        "id": "661f9511-fa3c-52e5-b827-557766551111",
        "label": "10×15",
        "widthMm": 100,
        "heightMm": 150,
        "pricingTiers": [
          { "minQuantity": 1,  "maxQuantity": 9,    "unitPrice": 1.20 },
          { "minQuantity": 10, "maxQuantity": 49,   "unitPrice": 0.90 },
          { "minQuantity": 50, "maxQuantity": null,  "unitPrice": 0.70 }
        ]
      }
    ],
    "finishes": ["Glossy", "Matte"]
  }
]
```

**Notes**:
- Returns an array (not wrapped in `{ items, total }`) — catalog is not paginated at MVP
- Only active products + active sizes are included
- Finishes returned as plain string array (no finish ID needed by client at this stage)
- Response cached in-memory for 5 minutes (see NFR section)

---

### `GET /api/products/{id}`
_Story 004 — Product Detail Endpoint_

Returns a single active product by ID with full detail (same shape as catalog item).

**Path param**: `id` (Guid)

**Response `200 OK`**: same shape as single item in catalog array

**Response `404 Not Found`**:
```json
{ "type": "...", "title": "Not Found", "status": 404, "traceId": "..." }
```

**Notes**:
- Returns 404 if product does not exist OR `IsActive = false`
- Includes all active sizes + tiers + finishes

---

### `GET /api/products/{id}/sizes/{sizeId}/price?quantity={n}`
_Story 005 — Price Calculation Endpoint_

Server-authoritative price calculation for a given product size and quantity. Used for order validation — Angular uses embedded tier data for display, but calls this endpoint before checkout to confirm price.

**Query param**: `quantity` (int, required, 1–9999)

**Response `200 OK`**:
```json
{
  "sizeId": "661f9511-fa3c-52e5-b827-557766551111",
  "sizeLabel": "10×15",
  "quantity": 15,
  "unitPrice": 0.90,
  "totalPrice": 13.50,
  "tierLabel": "10-49",
  "currency": "RON"
}
```

**Response `400 Bad Request`** (invalid quantity):
```json
{ "type": "...", "title": "Validation Failed", "status": 400, "errors": { "quantity": ["Quantity must be between 1 and 9999."] } }
```

**Response `404 Not Found`**: product or size not found / inactive

---

## Data Model

### New Tables

#### `Products`
| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | UUID | PK |
| `Name` | VARCHAR(200) | NOT NULL |
| `ProductType` | VARCHAR(50) | NOT NULL, DEFAULT 'PhotoPrint' |
| `ImageUrl` | VARCHAR(500) | NULL |
| `SortOrder` | INT | NOT NULL, DEFAULT 0 |
| `IsActive` | BOOL | NOT NULL, DEFAULT true |
| `CreatedAt` | TIMESTAMPTZ | NOT NULL |

#### `ProductSizes`
| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | UUID | PK |
| `ProductId` | UUID | FK → Products(Id) CASCADE |
| `Label` | VARCHAR(50) | NOT NULL |
| `WidthMm` | INT | NOT NULL |
| `HeightMm` | INT | NOT NULL |
| `IsActive` | BOOL | NOT NULL, DEFAULT true |

#### `ProductFinishes`
| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | UUID | PK |
| `ProductId` | UUID | FK → Products(Id) CASCADE |
| `Name` | VARCHAR(50) | NOT NULL |

#### `PricingTiers`
| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | UUID | PK |
| `ProductSizeId` | UUID | FK → ProductSizes(Id) CASCADE |
| `MinQuantity` | INT | NOT NULL |
| `MaxQuantity` | INT | NULL (open-ended) |
| `UnitPrice` | DECIMAL(10,2) | NOT NULL |

### Indexes
| Index | Table | Columns | Purpose |
|-------|-------|---------|---------|
| `ix_products_is_active_sort_order` | Products | (IsActive, SortOrder) | Catalog query filter+sort |
| `ix_product_sizes_product_id` | ProductSizes | (ProductId) | EF navigation |
| `ix_product_sizes_product_id_label` | ProductSizes | (ProductId, Label) UNIQUE | Prevent duplicate labels |
| `ix_product_finishes_product_id` | ProductFinishes | (ProductId) | EF navigation |
| `ix_pricing_tiers_product_size_id` | PricingTiers | (ProductSizeId) | Tier lookup |

### Entity Relationships
```text
Product ──< ProductSize ──< PricingTier
        ──< ProductFinish
```

### Seed Data (via EF Core `HasData`)
- 1 `Product`: "Poze foto", ProductType="PhotoPrint", SortOrder=0, IsActive=true
- 6 `ProductSize` rows: 10×15, 13×18, 15×21, 20×30, A4, A3
- 2 `ProductFinish` rows: Glossy, Matte
- 18 `PricingTier` rows: 3 tiers × 6 sizes (prices TBD — placeholder values in initial migration)

---

## File / Class Map

```text
src/PhotoPrint.API/
├── Controllers/
│   └── ProductsController.cs            # GET /api/products, /{id}, /{id}/sizes/{sizeId}/price
├── Models/
│   ├── Product.cs                        # EF entity
│   ├── ProductSize.cs                    # EF entity
│   ├── ProductFinish.cs                  # EF entity
│   └── PricingTier.cs                    # EF entity
├── Services/
│   ├── IProductService.cs               # interface
│   ├── ProductService.cs                # application service
│   └── PricingService.cs               # domain service (tier lookup + calculation)
├── Data/
│   └── PhotoPrintDbContext.cs           # add 4 new DbSets + Fluent config
└── Migrations/
    └── {timestamp}_AddProductCatalog.cs # new migration
```

**DTOs** (inline in `Services/` or dedicated `DTOs/` subfolder):
```text
├── Services/
│   ├── ProductDto.cs         # ProductSizeDto, PricingTierDto, ProductFinish[]
│   └── PriceCalculationDto.cs
```

---

## Security Design

| Concern | Approach |
|---------|----------|
| **Authorization** | All 3 endpoints are `[AllowAnonymous]` — catalog is public |
| **Input validation** | FluentValidation on `quantity` query param (1–9999); ID format validated by model binding (Guid) |
| **SQL injection** | EF Core parameterized queries; no raw SQL |
| **Enumeration** | Returning 404 (not 403) for inactive products is intentional — not a user account endpoint, no privacy concern |
| **Rate limiting** | Inherited from global rate limiter (bolt 002 security baselines) |
| **CORS** | Handled globally (bolt 002) |

---

## NFR Implementation

| Requirement | Design Approach |
|-------------|----------------|
| **Performance** | Eager loading with `.Include()` chains to avoid N+1 queries; single DB round-trip per request |
| **Caching** | `GET /api/products` uses `[ResponseCache(Duration = 300)]` (5 min) — catalog changes rarely; admin operations clear cache |
| **Low query count** | `ProductService.GetCatalogAsync()` executes a single query with `.Include(p => p.ProductSizes).ThenInclude(s => s.PricingTiers)` + separate finishes include |
| **Correctness** | Price calculation uses `PricingService` — same logic as domain model; no floating-point arithmetic (decimal throughout) |
| **Testability** | `PricingService` has no EF Core dependency — pure C# class, fully unit-testable |
