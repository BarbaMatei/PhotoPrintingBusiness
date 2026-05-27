---
id: 005-cart-crud-endpoints
unit: 001-upload-and-cart-backend
intent: 004-checkout-payment
status: draft
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 013-cart-api
implemented: false
---

# Story: 005-cart-crud-endpoints

## User Story

**As a** customer (authenticated or guest)
**I want** to view, update, and clear my cart via API
**So that** my photo selections and quantities are persisted on the server and can be retrieved from any device

## Acceptance Criteria

- [ ] **Given** a valid cart payload, **When** `POST /api/cart` is called with JWT or `X-Guest-Token`, **Then** the existing cart for that user/guest is replaced atomically and a 200 response returns the updated cart with computed totals
- [ ] **Given** a cart exists, **When** `GET /api/cart` is called, **Then** a 200 response returns all non-deleted cart items with `{ uploadId, previewUrl, productId, formatSize, finish, unitPriceRon, quantity, lineTotalRon }` and a `grandTotalRon` (excl. shipping)
- [ ] **Given** a cart exists, **When** `DELETE /api/cart` is called, **Then** all `CartItems` for the user/guest are deleted and 204 is returned
- [ ] **Given** a product ID in the cart payload does not exist, **When** `POST /api/cart` is called, **Then** 400 is returned
- [ ] **Given** an upload ID in the cart payload belongs to a different user, **When** `POST /api/cart` is called, **Then** 403 is returned

## Technical Notes

- `POST /api/cart` replace strategy: delete all existing `CartItems` for user/guest, insert new ones — single transaction
- Price lookup: `Product.PricingTiers` — find tier matching quantity range, return `PricePerUnit`
- `grandTotalRon` = sum of `(unitPrice × quantity)` for all items
- Endpoint accepts both `Authorization: Bearer {jwt}` and `X-Guest-Token: {token}` via shared auth middleware
- FluentValidation: each item requires `uploadId` (UUID), `productId` (UUID), `quantity` (1–100)

## Dependencies

### Requires
- Story 004-cart-item-entity (CartItem entity)
- Bolt 009 (product-catalog-core — pricing tiers)
- Bolt 005 / 007 (auth — JWT + guest token for user resolution)

### Enables
- Story 006-cart-merge-endpoint (needs GET /api/cart for post-merge response)
- Bolt 014 (upload-format-cart-ui — Angular CartService calls these endpoints)
- Bolt 016 (payment-backends — creates Order from cart contents)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Empty cart POST `[]` | Deletes all items; GET /api/cart returns empty `{ items: [], grandTotalRon: 0 }` |
| Quantity > 100 | 400 validation error |
| Cart with 30 items (upload limit) | Accepted; no separate cart size limit |
| Concurrent POST from two tabs | Last writer wins (replace strategy); no conflict error |

## Out of Scope

- Per-item quantity patch endpoint (POST replaces all)
- Shipping cost calculation (handled by bolt 015 + FR-11)
