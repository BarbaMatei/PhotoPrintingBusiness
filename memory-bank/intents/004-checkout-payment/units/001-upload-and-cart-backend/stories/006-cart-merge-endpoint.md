---
id: 006-cart-merge-endpoint
unit: 001-upload-and-cart-backend
intent: 004-checkout-payment
status: draft
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 013-cart-api
implemented: false
---

# Story: 006-cart-merge-endpoint

## User Story

**As a** guest who has just logged in
**I want** my guest cart to be merged into my user account cart
**So that** I don't lose the photos I selected before registering

## Acceptance Criteria

- [ ] **Given** a guest cart with items, **When** `POST /api/cart/merge` is called with a valid Bearer JWT and `{ guestToken }` body, **Then** guest cart items are transferred to the authenticated user's cart within a single database transaction
- [ ] **Given** both the guest cart and user cart contain items for the same upload, **When** merge runs, **Then** the server-side (user) cart item takes precedence — guest version is discarded
- [ ] **Given** the guest has uploads linked to guest session, **When** merge runs, **Then** those `Upload` rows have their `GuestSessionId` cleared and `UserId` set to the authenticated user's ID
- [ ] **Given** the database transaction fails mid-merge, **When** the error is caught, **Then** the cart is left in its original state (no partial merge)
- [ ] **Given** the `guestToken` does not resolve to a session, **When** merge is called, **Then** 400 is returned and the user's cart is unchanged

## Technical Notes

- Execute merge in a single `IDbContextTransaction` (EF Core `BeginTransactionAsync`)
- Merge algorithm: for each guest `CartItem`, if `(UserId, UploadId)` uniqueness constraint would be violated, skip (user item wins); otherwise reassign `UserId`, set `GuestSessionId = null`
- Upload reassignment: `UPDATE Uploads SET UserId = @userId, GuestSessionId = NULL WHERE GuestSessionId = @guestSessionId`
- Guest `GuestSession` record: mark `IsConsumed = true` or delete after merge
- Called automatically by Angular `AuthService` immediately after successful JWT login response

## Dependencies

### Requires
- Story 004-cart-item-entity (CartItem entity)
- Story 005-cart-crud-endpoints (GET /api/cart used for post-merge response)
- Bolt 007 (guest-sessions — GuestSession resolution from token)

### Enables
- Bolt 014 (upload-format-cart-ui — post-login cart state shows merged items)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Guest cart is empty | Merge is a no-op; returns user's existing cart |
| User cart is empty | All guest items transferred to user |
| Guest token expired | 400 with `"Sesiunea de oaspete a expirat"` |
| Merge called twice with same guest token | Second call is no-op (session already consumed) |

## Out of Scope

- Merging from multiple guest sessions (only one guest token per merge call)
- Admin-triggered cart operations
