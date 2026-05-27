---
unit: 002-product-catalog-ui
intent: 003-product-catalog
unit_type: frontend
default_bolt_type: simple-construction-bolt
phase: inception
status: draft
created: 2026-05-20T20:35:00Z
updated: 2026-05-20T20:35:00Z
---

# Unit Brief: product-catalog-ui

## Purpose

Angular 21 frontend for the FotoTipar product catalog: a customer-facing catalog page with format selection and live price calculator, plus an admin dashboard for product and pricing management.

## Scope

### In Scope
- `/tipareste` route — catalog grid page showing all active products
- Product card component (thumbnail, name, available sizes, "de la X lei")
- Format selection page/component (size selector, finish toggle, quantity input, live price display)
- Client-side tier-based price calculation (no additional HTTP call)
- "Adaugă în coș" button — disabled until size + quantity ≥ 1 selected
- `/admin/products` route — admin product management (list, create, edit, delete)
- Admin pricing tier editor (per size)
- Angular service consuming `GET /api/products`, `GET /api/products/{id}`, `POST /api/admin/products/**`
- Reactive forms with inline validation
- Guard protecting `/admin/products` (admin role required)

### Out of Scope
- Shopping cart (future intent)
- Checkout flow (future intent)
- Customer reviews
- Image upload (admin links to CDN URL)

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-8 | Angular product catalog page | Must |
| FR-9 | Angular format selection & price calculator UI | Must |
| FR-10 | Admin Angular product catalog management | Must |

---

## Domain Concepts

### Key Components
| Component | Route / Location | Purpose |
|-----------|-----------------|---------|
| `CatalogPage` | `/tipareste` | Grid of all active products |
| `ProductCardComponent` | shared | Displays product thumbnail + starting price |
| `FormatSelectorPage` | `/tipareste/{id}` | Size/finish/quantity + live price |
| `PriceCalculatorComponent` | feature | Client-side tier calculation, reactive |
| `AdminProductsPage` | `/admin/products` | List + CRUD actions |
| `ProductFormComponent` | admin feature | Create/edit product form |
| `PricingTierEditorComponent` | admin feature | Edit pricing tiers per size |

### Key Service
`ProductService` — wraps all product catalog API calls; exposes typed Observables; caches catalog list in memory.

---

## Technical Notes

- Pattern: standalone components, `ChangeDetectionStrategy.OnPush`, `inject()` pattern (consistent with bolt-008)
- SCSS uses `@use 'styles/variables' as *` (project convention)
- Client-side price calculation: given tiers array + quantity → find matching tier → compute `unitPrice × quantity`
- Admin route protected by existing `authGuard` + admin check (reuse from bolt-004 routing infrastructure)
- Lazy-loaded under `features/catalog/` and `features/admin/` feature folders

---

## Success Criteria

- Catalog page displays all active products on navigation to `/tipareste`
- Price label updates < 5ms on quantity/size change (no network call)
- Admin can create a product with sizes, finishes, and pricing tiers end-to-end
- All components have Vitest unit tests
