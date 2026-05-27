---
intent: 003-product-catalog
phase: inception
status: draft
created: 2026-05-20T20:35:00Z
updated: 2026-05-20T20:35:00Z
---

# Units: 003-product-catalog

## Decomposition Summary

| # | Unit | Type | Bolt Type | FRs | Bolts |
|---|------|------|-----------|-----|-------|
| 001 | product-catalog-core | backend | ddd-construction-bolt | FR-1 to FR-7 | 2 (009, 010) |
| 002 | product-catalog-ui | frontend | simple-construction-bolt | FR-8 to FR-10 | 1 (011) |

## Requirement-to-Unit Mapping

- **FR-1** Product entity with sizes and finishes → `001-product-catalog-core`
- **FR-2** Quantity-tiered pricing per product size → `001-product-catalog-core`
- **FR-3** Public product catalog API endpoint → `001-product-catalog-core`
- **FR-4** Product detail endpoint → `001-product-catalog-core`
- **FR-5** Price calculation endpoint → `001-product-catalog-core`
- **FR-6** Admin product management API → `001-product-catalog-core`
- **FR-7** Admin pricing management API → `001-product-catalog-core`
- **FR-8** Angular product catalog page → `002-product-catalog-ui`
- **FR-9** Angular format selection & price calculator UI → `002-product-catalog-ui`
- **FR-10** Admin Angular product catalog management → `002-product-catalog-ui`

## Unit 001: product-catalog-core

**Purpose**: Backend API, domain model, EF Core schema, and all REST endpoints for products, sizes, finishes, and pricing tiers. Both public and admin-protected routes.

**Bolt Plan**:
- `009-product-catalog-core` — Domain model + public API (FR-1 to FR-5)
- `010-product-catalog-admin` — Admin CRUD API (FR-6 to FR-7), requires 009

## Unit 002: product-catalog-ui

**Purpose**: Angular pages and components for customer-facing catalog browsing/format selection and admin product management dashboard.

**Bolt Plan**:
- `011-product-catalog-ui` — All Angular UI work (FR-8 to FR-10), requires 009 + 010
