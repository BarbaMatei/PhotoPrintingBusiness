# US-506 — Admin — Product Management (Frontend)

## Story
**As an** operator  
**I want to** manage print products and prices without a code deployment

## Type
FRONTEND — Angular

## Epic
EPIC-5 | Panou Admin

## Dependencies
- US-504 (Admin API — product CRUD endpoints)
- US-501 (Admin layout)

## Acceptance Criteria

1. **`/admin/produse`** — data table: all products with name, dimensions, finish, price, active toggle
2. **Inline toggle** for `IsActive`
3. **Edit dialog**: all fields editable (name, widthCm, heightCm, finish, priceRon, min/optimal resolutions, sortOrder)
4. **`Adaugă produs`** button for new entries
5. **Price change reflects on storefront** within 60 seconds (cache TTL)

## Technical Notes

### Component Location
`src/app/features/admin/products/products.component.ts`

### Implementation Details
- Load products: `GET /api/admin/products` (includes inactive products)
- Data table with columns: Name, Dimensions (WxH cm), Finish, Price (RON), Active, Actions
- Active toggle: on change, call `PUT /api/admin/products/{id}` with updated `isActive`
- Edit: open dialog/modal with form pre-populated with product data; on save call `PUT /api/admin/products/{id}`
- Add: open same dialog with empty form; on save call `POST /api/admin/products`
- Validation: price > 0, dimensions > 0, resolutions > 0
- Cache note: inform operator that changes take up to 60 seconds to appear on storefront

### Admin Product Endpoints (in AdminController)
```
GET /api/admin/products → all products including inactive
POST /api/admin/products → create new product
PUT /api/admin/products/{id} → update product
DELETE /api/admin/products/{id} → soft-delete (set isActive=false)
```

## Files to Create/Modify
- `src/app/features/admin/products/products.component.ts`
- `src/app/features/admin/products/products.component.html`
- `src/app/features/admin/products/products.component.scss`
- `src/app/features/admin/product-dialog/product-dialog.component.ts`
- `src/app/core/services/admin-products.service.ts`

## Testing
- Unit test: products table renders all products
- Unit test: active toggle calls API
- Unit test: edit dialog populates and saves
- Unit test: add new product flow
- Unit test: validation rules
