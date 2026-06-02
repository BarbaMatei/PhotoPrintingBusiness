---
id: 004-cart-item-entity
unit: 001-upload-and-cart-backend
intent: 004-checkout-payment
status: complete
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 013-cart-api
implemented: true
---

# Story: 004-cart-item-entity

## User Story

**As a** developer
**I want** a `CartItem` entity with its EF Core migration
**So that** the cart API has a persistent, relational backing store that ties uploads to a product selection

## Acceptance Criteria

- [ ] **Given** the migration runs, **When** the database is updated, **Then** a `CartItems` table exists with columns: `Id (UUID)`, `UserId? (nullable FK → Users)`, `GuestSessionId? (nullable)`, `UploadId (FK → Uploads)`, `ProductId (FK → Products)`, `Quantity (int)`, `CreatedAt (DateTimeOffset)`, `UpdatedAt (DateTimeOffset)`
- [ ] **Given** a `CartItem` is created, **When** both `UserId` and `GuestSessionId` are null, **Then** a DB constraint rejects the insert
- [ ] **Given** `CartItems` are queried by user, **When** the table is large, **Then** indexes on `(UserId)` and `(GuestSessionId)` make the query efficient
- [ ] **Given** the associated `Upload` is soft-deleted, **When** a cart is retrieved, **Then** the `CartItem` is excluded from the response (filter on `Upload.DeletedAt IS NULL`)

## Technical Notes

- `CartItem` has a composite uniqueness constraint on `(UserId, UploadId)` and `(GuestSessionId, UploadId)` — one cart item per upload per user/guest
- Soft-delete on `Upload` should NOT cascade-delete `CartItems` — instead filter at query time
- Include `CartItem.Product` navigation property with `Include(ci => ci.Product)` in cart queries for price computation
- `Quantity` minimum = 1; enforced by DB check constraint and FluentValidation

## Dependencies

### Requires
- Story 001-upload-entity-schema (Upload entity + FK)
- Bolt 009 (product-catalog-core — Products table for FK)
- Bolt 005 (auth-core — Users table for FK)

### Enables
- Story 005-cart-crud-endpoints (CartItem entity needed to build endpoints)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Same upload added twice to same user cart | Uniqueness constraint → 409 or update quantity |
| Product FK points to deleted product | Product soft-delete not in scope yet; FK enforced by DB |
| `Quantity = 0` insert | DB check constraint rejects; service validates before insert |

## Out of Scope

- Cart expiry / TTL (handled by UploadCleanupJob removing the backing upload)
- Wishlist or saved-for-later
