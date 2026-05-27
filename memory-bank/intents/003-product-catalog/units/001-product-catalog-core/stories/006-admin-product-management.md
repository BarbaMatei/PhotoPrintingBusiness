---
id: 006-admin-product-management
unit: 001-product-catalog-core
intent: 003-product-catalog
status: draft
priority: must
created: 2026-05-20T20:35:00Z
assigned_bolt: 010-product-catalog-admin
implemented: false
---

# Story: 006-admin-product-management

## User Story

**As an** admin
**I want** to create, update, activate/deactivate, and delete products and their size variants
**So that** I can manage the catalog without code deployments

## Acceptance Criteria

- [ ] **Given** a valid admin JWT, **When** `POST /api/admin/products` with valid body, **Then** returns 201 with the created product
- [ ] **Given** a valid admin JWT, **When** `PUT /api/admin/products/{id}` with valid body, **Then** returns 200 with updated product
- [ ] **Given** a valid admin JWT, **When** `PATCH /api/admin/products/{id}/status` with `{ isActive: bool }`, **Then** returns 200 and flips the product's active flag
- [ ] **Given** a valid admin JWT, **When** `DELETE /api/admin/products/{id}`, **Then** returns 204 and soft-deletes (or hard-deletes) the product
- [ ] **Given** a non-admin JWT, **When** any admin endpoint is called, **Then** returns 403
- [ ] **Given** an unauthenticated request, **When** any admin endpoint is called, **Then** returns 401
- [ ] **Given** invalid request body (missing Name), **When** `POST /api/admin/products`, **Then** returns 422 with FluentValidation errors

## Technical Notes

- Admin controller at `/api/admin/products`
- Reuse `[Authorize(Policy = "AdminOnly")]` or check `isAdmin` claim (consistent with bolt-005)
- Create request: `{ name, productType, imageUrl?, sortOrder, sizes: [{ label, widthMm, heightMm, finishes: [...] }] }`
- Soft-delete via `IsActive = false` preferred over hard-delete (preserves order history in future)
- FluentValidation: `Name` required, `sortOrder` ≥ 0

## Dependencies

### Requires
- 001-product-entity-schema (Product + ProductSize entities)

### Enables
- 007-admin-pricing-management (needs sizes to attach tiers to)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Delete product with no order references (future) | Soft-delete (IsActive = false) |
| Duplicate product name | Allow — no uniqueness constraint on name |
| Product with zero sizes in create request | 422 — at least one size required |
