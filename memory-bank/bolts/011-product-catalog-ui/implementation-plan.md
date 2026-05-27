---
stage: plan
bolt: 011-product-catalog-ui
created: 2026-05-21T11:10:00Z
---

## Implementation Plan: product-catalog-ui

### Objective
Build the Angular product catalog UI — a customer-facing catalog grid, a format selection + price calculator page, and an admin product management dashboard. All backed by the APIs from bolts 009 and 010.

---

### Deliverables

**Services**
- `ProductService` — `GET /api/products` (cached), `GET /api/products/{id}`
- `ProductAdminService` — full admin CRUD (`POST/PUT/PATCH/DELETE /api/admin/products/**`)
- `calcPrice(tiers, quantity)` — pure client-side tier lookup + price calculation (replicates PricingService logic)

**Story 001 — `/tipareste` Catalog Page**
- `CatalogPage` — standalone page, `OnPush`, `toSignal()`, skeleton loaders, error state with retry
- `ProductCardComponent` — standalone, reusable; shows name, thumbnail, size labels, "de la X lei"
- Update `upload.routes.ts`: `path: ''` → `CatalogPage`; `path: ':id'` → `FormatSelectorPage`

**Story 002 — `/tipareste/:id` Format Selector Page**
- `FormatSelectorPage` — standalone, `OnPush`, reactive form (size + quantity), computed price signal
- Client-side pricing: `calcPrice(tiers, qty)` → `{ unitPrice, total, tierLabel }`
- Finish toggle (Lucioasă/Mată), "Adaugă în coș" button (disabled until size + qty valid)
- `addToCart` output event emitted on submit (cart integration is future intent)

**Story 003 — `/admin/products` Admin Dashboard**
- `AdminProductsPage` — table of all products, `OnPush`
- Inline create/edit form with `FormArray` for sizes and pricing tiers
- Pricing tier editor per size (add/remove/edit rows)
- API 422 errors mapped to form controls
- Soft-delete with confirmation
- Update `admin.routes.ts`: `path: 'products'` → `AdminProductsPage` (replace placeholder)

---

### Dependencies

- `GET /api/products` → bolt 009 ✅
- `GET /api/products/{id}` → bolt 009 ✅
- `POST/PUT/PATCH/DELETE /api/admin/products/**` → bolt 010 ✅
- `adminGuard` → existing in `core/guards/admin.guard.ts`
- `AuthService.isAdmin$` → existing
- `HttpClient` with JWT interceptor → existing

---

### Technical Approach

1 - **Angular patterns**: standalone components, `ChangeDetectionStrategy.OnPush`, `inject()`, `toSignal()`, `computed()`
2 - **Product model interfaces**: `Product`, `ProductSize`, `PricingTier`, `ProductFinish` in `core/models/product.model.ts`
3 - **ProductService**: `providedIn: 'root'`, caches catalog in a `BehaviorSubject` (reset on null), exposes `catalog$` signal
4 - **Price calculation**: pure function `calcPrice(tiers: PricingTier[], qty: number)` in `shared/utils/pricing.utils.ts` — zero dependency, directly testable
5 - **Admin reactive forms**: `FormBuilder` with typed `FormArray`, server validation errors applied with `setErrors()`
6 - **Routing**: `upload.routes.ts` gets two routes (catalog + format selector); `admin.routes.ts` gets `products` sub-route

---

### File Structure

```text
src/app/
├── core/
│   ├── models/
│   │   └── product.model.ts          # Product, ProductSize, PricingTier, ProductFinish interfaces
│   └── services/
│       ├── product.service.ts        # getCatalog$, getProduct(id)
│       └── product-admin.service.ts  # full admin CRUD
├── shared/
│   └── utils/
│       └── pricing.utils.ts          # pure calcPrice() function
├── features/
│   ├── upload/
│   │   ├── upload.routes.ts          # updated: '' + ':id'
│   │   └── pages/
│   │       ├── upload-page.ts        # replaced by CatalogPage
│   │       ├── catalog/
│   │       │   ├── catalog-page.ts
│   │       │   └── catalog-page.scss
│   │       └── format-selector/
│   │           ├── format-selector-page.ts
│   │           └── format-selector-page.scss
│   └── admin/
│       ├── admin.routes.ts           # updated: 'products' sub-route
│       └── pages/
│           ├── admin-page.ts         # kept (dashboard shell placeholder)
│           └── products/
│               ├── admin-products-page.ts
│               └── admin-products-page.scss
└── shared/
    └── components/
        └── product-card/
            ├── product-card.ts
            └── product-card.scss
```

---

### Acceptance Criteria

- [ ] `GET /api/products` called once per session; results cached in service
- [ ] Catalog grid shows skeleton loaders while fetching
- [ ] Catalog grid shows error state with retry on API failure
- [ ] Product card shows name, size labels, "de la X lei" (minimum unit price)
- [ ] Clicking product card navigates to `/tipareste/{id}`
- [ ] Format selector shows all active sizes + finishes for product
- [ ] Price updates instantly on size/quantity change (no HTTP call)
- [ ] Tier label badge shown (e.g. "10–49 buc.")
- [ ] "Adaugă în coș" disabled without size or with qty ≤ 0
- [ ] Admin `/admin/products` shows all products (including inactive)
- [ ] Admin can create/update/soft-delete products
- [ ] Admin can set pricing tiers per size; tier validation errors shown inline
- [ ] Admin route guarded — non-admin redirected to `/auth/login`
- [ ] All Vitest tests pass (0 errors)
