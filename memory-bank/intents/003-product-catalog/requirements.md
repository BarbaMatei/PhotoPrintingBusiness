---
intent: 003-product-catalog
phase: inception
status: inception-complete
created: 2026-05-20T20:30:00Z
updated: 2026-05-21T09:00:00Z
---

# Requirements: Product Catalog & Pricing

## Intent Overview

Enable customers to browse available print products, select paper sizes and finishes, and see accurate per-unit prices that reflect quantity-based tiers. Includes the backend API, database schema, admin CRUD, and the Angular customer-facing product browsing and format-selection UI.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Customers can discover and select print formats | Product page loads in < 300ms | Must |
| Prices update instantly when quantity changes | Price tier calculation is client-side, zero extra HTTP calls | Must |
| Admin can manage the product catalog without code deploys | Full CRUD via admin UI | Must |
| Pricing is consistent across web and any future API clients | Single pricing source of truth in DB | Must |
| Catalog can grow to include new product types later | Extensible schema (product type discriminator) | Should |

---

## Functional Requirements

### FR-1: Product entity with sizes and finishes
- **Description**: The system must store print products with their available sizes (10×15, 13×18, 15×21, 20×30, A4, A3), finish options (glossy/matte), and active/inactive status.
- **Acceptance Criteria**: Products can be created, updated, activated/deactivated, and deleted. At least one size variant must exist per product.
- **Priority**: Must

### FR-2: Quantity-tiered pricing per product size
- **Description**: Each product size must have price tiers: 1–9 units, 10–49 units, 50+ units. The unit price decreases at higher tiers.
- **Acceptance Criteria**: Given a quantity, the API returns the correct per-unit price and total. Tier boundaries are configurable in the DB.
- **Priority**: Must

### FR-3: Public product catalog API endpoint
- **Description**: A public (unauthenticated) REST endpoint returns all active products with their sizes, finishes, and pricing tiers.
- **Acceptance Criteria**: `GET /api/products` returns all active products. Response is cacheable. Inactive products are excluded.
- **Priority**: Must

### FR-4: Product detail endpoint
- **Description**: A public endpoint returns a single product with full pricing detail.
- **Acceptance Criteria**: `GET /api/products/{id}` returns product with all sizes and tier prices. Returns 404 for unknown or inactive products.
- **Priority**: Must

### FR-5: Price calculation endpoint
- **Description**: An endpoint computes the price for a given product size and quantity, applying the correct tier.
- **Acceptance Criteria**: `POST /api/products/{id}/calculate-price` accepts `{ sizeId, quantity }` and returns `{ unitPrice, totalPrice, tierApplied }`.
- **Priority**: Must

### FR-6: Admin product management API
- **Description**: Authenticated admin endpoints to create, update, activate/deactivate, and delete products.
- **Acceptance Criteria**: `POST /api/admin/products`, `PUT /api/admin/products/{id}`, `DELETE /api/admin/products/{id}`. Non-admin requests return 403.
- **Priority**: Must

### FR-7: Admin pricing management API
- **Description**: Admin can update pricing tiers for any product size.
- **Acceptance Criteria**: `PUT /api/admin/products/{id}/sizes/{sizeId}/pricing` updates all tiers atomically.
- **Priority**: Must

### FR-8: Angular product catalog page
- **Description**: The Angular frontend displays all active products in a grid/list with size thumbnails, finish options, and starting-from prices.
- **Acceptance Criteria**: Navigating to `/tipareste` shows the full catalog. Products display lowest-tier unit price as "de la X lei".
- **Priority**: Must

### FR-9: Angular format selection & price calculator UI
- **Description**: When a customer selects a product, they can choose size, finish, and quantity; the displayed price updates instantly to reflect the correct tier.
- **Acceptance Criteria**: Price label updates client-side on any input change. "Add to cart" is disabled until size + quantity are valid.
- **Priority**: Must

### FR-10: Admin Angular product catalog management
- **Description**: Admin users see a management interface to add/edit/delete products and update pricing tiers.
- **Acceptance Criteria**: Accessible via `/admin/products`. Requires admin role. Full CRUD with inline validation.
- **Priority**: Must

---

## Non-Functional Requirements

### Performance
| Requirement | Metric | Target |
|-------------|--------|--------|
| Catalog endpoint response time | p95 latency | < 200ms |
| Price calculation | Client-side, no HTTP call | < 5ms |
| Admin CRUD operations | p95 latency | < 300ms |

### Security
| Requirement | Standard | Notes |
|-------------|----------|-------|
| Admin endpoints protected | JWT + `isAdmin` claim | Existing auth infrastructure |
| No PII in product responses | N/A | Products are public catalog data |
| Input validation | FluentValidation (422) | Consistent with existing conventions |

### Reliability
| Requirement | Metric | Target |
|-------------|--------|--------|
| Catalog availability | Uptime | 99.9% |
| Stale product data | Cache TTL | ≤ 60 seconds |

### Maintainability
- Product types use a discriminator column for future extensibility (canvas, photo books, etc.)
- Pricing tiers stored as rows (not JSON columns) for queryability
- Schema follows existing EF Core conventions in the codebase

---

## Out of Scope

- Canvas prints, mugs, photo books, calendars — future intent
- Inventory / stock management
- Promotional codes / discounts
- Customer reviews and ratings
- Product images hosted in this service (link to CDN URL)
