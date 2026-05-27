---
id: 002-guest-session-claim
unit: 003-guest-sessions
intent: 002-authentication
status: draft
priority: must
created: 2026-05-20T12:57:00Z
assigned_bolt: 007-guest-sessions
implemented: false
---

# Story: 002-guest-session-claim

## User Story

**As a** guest who placed an order and now wants to create an account
**I want** my guest orders to transfer to my new account
**So that** I can track them under my profile

## Acceptance Criteria

- [ ] **Given** `POST /api/auth/guest/claim` with a valid Bearer JWT (authenticated user) and `{guestToken}` in body, **When** the guest session exists and is not yet claimed, **Then** all orders linked to `GuestSessionId` are updated with `UserId = current user` and `GuestSession.ClaimedByUserId = current user`, returns 200
- [ ] **Given** a claim request, **When** the guest token does not exist or is already claimed, **Then** returns 400 `"Sesiunea de oaspete este invalidă sau a fost deja revendicată"`
- [ ] **Given** a claim request, **When** the guest session is expired, **Then** returns 400 (expired sessions cannot be claimed)
- [ ] **Given** a claim request, **When** there are no orders linked to the guest session, **Then** returns 200 (no-op is acceptable — guest may have abandoned without ordering)

## Technical Notes

- Requires `[Authorize]` — Bearer JWT (authenticated user) + guest token in request body
- Order transfer is done in a single DB transaction
- After claiming, the guest session row is NOT deleted — `ClaimedByUserId` acts as soft-invalidation

## Dependencies

### Requires
- Story 001-guest-session-create (GuestSession table)
- Bolt 005 (Unit 001-auth-core: User entity, JWT authorization)

### Enables
- Nothing directly — enables order tracking in future intents

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Claim with a guest token that belongs to a different user's email | Allowed — token possession is the only check (not email ownership) |
| Concurrent claim requests for same token | Transaction ensures only one succeeds; second gets 400 |

## Out of Scope

- UI prompt after order to claim (→ `004-authentication-ui`)
- Order history page (→ future intents)
