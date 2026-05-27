# US-204 — Product Catalogue API (Backend)

## Story
**As a** system  
**I want to** expose print products (format+finish combinations) with prices and resolution requirements

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-2 | Upload Fotografii & Selecție Format

## Dependencies
- US-801 (Error handling)
- Database schema: `Products` table

## Acceptance Criteria

1. **`GET /api/products`** — public, no auth; returns `[{id, name, widthCm, heightCm, finish, priceRon, minWidthPx, minHeightPx, optimalWidthPx, optimalHeightPx, isActive, sortOrder}]`
2. **Only `isActive=true`** products returned to public; admin sees all
3. **`POST /api/products/calculate`** `[{productId, quantity}]` → returns `[{lineTotal}, grandTotal]` in RON
4. **Seeded**: 6 products (3 formats × Lucios + Mat); prices editable from admin panel
5. **Response cached 60 seconds** (IMemoryCache) — invalidated on admin product update

## Technical Notes

### Endpoints
```
GET /api/products
→ 200 [
  {
    "id": "uuid",
    "name": "10×15 Lucios",
    "widthCm": 10,
    "heightCm": 15,
    "finish": "Lucios",
    "priceRon": 0.50,
    "minWidthPx": 1200,
    "minHeightPx": 1800,
    "optimalWidthPx": 2400,
    "optimalHeightPx": 3600,
    "isActive": true,
    "sortOrder": 1
  }
]
```

```
POST /api/products/calculate
[{ "productId": "uuid", "quantity": 5 }]
→ 200 { "lines": [{ "productId": "uuid", "quantity": 5, "unitPrice": 0.50, "lineTotal": 2.50 }], "grandTotal": 2.50 }
```

### Implementation Details
- `Products` entity (see Appendix A)
- Seed data in EF Core migration or `DbContext.OnModelCreating`:
  - 10×15 Lucios, 10×15 Mat
  - 13×18 Lucios, 13×18 Mat
  - 15×21 Lucios, 15×21 Mat
- Public endpoint returns only `isActive=true`, ordered by `sortOrder`
- Calculate endpoint: validate all productIds exist and are active; multiply `quantity × priceRon`; return line totals + grand total
- Caching: `IMemoryCache` with 60s expiry on GET /api/products; cache key invalidated when admin updates any product
- Prices in RON (Romanian Lei) with 2 decimal places

### Database
- `Products` table with unique constraint on (WidthCm, HeightCm, Finish)

## Files to Create/Modify
- `src/PhotoPrint.API/Controllers/ProductsController.cs`
- `src/PhotoPrint.API/DTOs/ProductDto.cs`
- `src/PhotoPrint.API/DTOs/CalculateRequest.cs`
- `src/PhotoPrint.API/DTOs/CalculateResponse.cs`
- `src/PhotoPrint.API/Models/Product.cs`
- `src/PhotoPrint.API/Services/IProductService.cs` + `ProductService.cs`
- EF Core migration with seed data for Products

## Testing
- Unit test: only active products returned publicly
- Unit test: calculate with valid products
- Unit test: calculate with invalid productId → error
- Unit test: cache invalidation on product update
- Integration test: GET /api/products returns seeded data
