---
id: 013-cart-api
unit: 001-upload-and-cart-backend
intent: 004-checkout-payment
type: ddd-construction-bolt
status: complete
started: 2026-05-21T18:30:00Z
completed: 2026-05-21T20:00:00Z
current_stage: done
stages_completed: [domain-model, technical-design, implement, test]
stories:
  - 004-cart-item-entity
  - 005-cart-crud-endpoints
  - 006-cart-merge-endpoint
created: 2026-05-21T18:00:00Z

requires_bolts: ["012-photo-upload-backend", "009-product-catalog-core", "005-auth-core", "007-guest-sessions"]
enables_bolts: ["014-upload-format-cart-ui", "015-shipping-and-order-core"]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 2
  max_dependencies: 4
  testing_scope: 2
---

## Bolt: 013-cart-api

### Summary
Implements the CartItem entity, cart CRUD endpoints (GET/POST/DELETE /api/cart), and the
cart merge endpoint (POST /api/cart/merge). Depends on bolt 012 (Uploads table) and bolt 009
(Products table for price lookups). Supports both authenticated users and guest sessions.

### Stories
- **004-cart-item-entity**: CartItem entity + EF Core migration + unique composite indexes
- **005-cart-crud-endpoints**: GET, POST (replace), DELETE /api/cart
- **006-cart-merge-endpoint**: POST /api/cart/merge — guest → user cart merge with conflict resolution

### Key Design Decisions
- Replace strategy on SetCart: delete-all + insert-new in a transaction
- Composite unique indexes on (UserId, UploadId) and (GuestSessionId, UploadId)
- Price computed at request time from PricingTiers (NOT snapshotted in CartItem — that is Order's job)
- GuestSessionId stored as plain column (no FK, same pattern as Upload entity)
- Merge: if UploadId conflict, user's existing item wins; guest uploads transfer ownership to user
