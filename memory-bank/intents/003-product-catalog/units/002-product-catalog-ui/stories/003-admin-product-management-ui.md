---
id: 003-admin-product-management-ui
unit: 002-product-catalog-ui
intent: 003-product-catalog
status: complete
priority: must
created: 2026-05-20T20:35:00Z
assigned_bolt: 011-product-catalog-ui
implemented: true
---

# Story: 003-admin-product-management-ui

## User Story

**As an** admin
**I want** a web interface to manage products and pricing tiers
**So that** I can update the catalog without touching the database directly

## Acceptance Criteria

- [ ] **Given** I am logged in as admin and navigate to `/admin/products`, **When** the page loads, **Then** a table of all products (including inactive) is displayed
- [ ] **Given** the products table, **When** rendered, **Then** each row shows: name, type, sizes count, active status, and Edit/Delete actions
- [ ] **Given** I click "Add Product", **When** the form opens, **Then** I can enter name, image URL, sort order, and add size variants (label, dimensions, finishes)
- [ ] **Given** I click "Edit" on a product, **When** the form opens, **Then** existing values are pre-populated and I can modify any field
- [ ] **Given** a size row in the product form, **When** I click "Edit Pricing", **Then** a pricing tier editor opens showing current tiers with min quantity, max quantity, and unit price
- [ ] **Given** I save pricing tiers with a validation error (price ascending), **When** saved, **Then** an inline error is shown next to the offending tier
- [ ] **Given** I click "Delete" on a product, **When** confirmed, **Then** the product is soft-deleted and removed from the list
- [ ] **Given** a non-admin user navigates to `/admin/products`, **When** the guard runs, **Then** they are redirected to `/auth/login`

## Technical Notes

- Route: `{ path: 'admin/products', loadComponent: ..., canActivate: [adminGuard] }`
- `ProductAdminService` wraps `POST/PUT/PATCH/DELETE /api/admin/products/**`
- Reactive forms with `FormArray` for sizes and pricing tiers
- Inline validation errors from API 422 responses mapped to form controls
- `ChangeDetectionStrategy.OnPush`
- Reuse existing `adminGuard` from bolt-004 routing infrastructure

## Dependencies

### Requires
- `010-product-catalog-admin` bolt complete (needs admin API)
- Existing `adminGuard` (bolt-004)

### Enables
- Nothing — final UI story for this intent

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Save product with no sizes | Inline error: "At least one size required" |
| Delete product currently in a user's cart (future) | Show warning, proceed with soft-delete |
| Session expires while editing | Auth interceptor redirects to login |
