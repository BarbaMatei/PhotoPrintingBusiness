---
unit: 003-guest-sessions
intent: 002-authentication
unit_type: backend
default_bolt_type: ddd-construction-bolt
phase: inception
status: ready
created: 2026-05-20T12:57:00Z
updated: 2026-05-20T12:57:00Z
---

# Unit Brief: guest-sessions

## Purpose

Enable anonymous users to place orders without registering. Issues an opaque guest token tied to contact details, allows all order/cart/upload endpoints to accept it as an alternative to Bearer JWT, supports claiming guest orders after registration, and cleans up orphaned sessions automatically.

## Scope

### In Scope
- `GuestSession` entity creation with 7-day TTL
- `POST /api/auth/guest` endpoint (issue guest token)
- `POST /api/auth/guest/claim` endpoint (transfer orders to real account)
- Authorization policy / handler that accepts `X-Guest-Token` header alongside Bearer JWT
- `GuestSessionCleanupJob` background service (removes sessions with no linked orders after 7 days)

### Out of Scope
- Frontend guest checkout form (→ `004-authentication-ui`)
- Order/cart/upload endpoints themselves (→ future intents)
- Registered user login (→ `001-auth-core`)

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-5 | Guest session backend — create, claim, cleanup | Must |

---

## Domain Concepts

### Key Entities
| Entity | Description | Key Attributes |
|--------|-------------|----------------|
| `GuestSession` | Anonymous session for guest checkout | Id (UUID), Email, FirstName, LastName, Phone, CreatedAt, ExpiresAt, ClaimedByUserId? (FK, nullable) |

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| CreateGuestSession | Validate contact info, create session, return token | GuestDto | `{guestToken: UUID}` |
| ClaimGuestSession | Transfer guest orders to real account, invalidate session | guestToken, authenticated userId | 200 |
| CleanupExpiredSessions | Remove sessions with no orders older than 7 days | (scheduled) | rows deleted |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 3 |
| Must Have | 3 |
| Should Have | 0 |
| Could Have | 0 |

### Stories
| # | Story | Priority |
|---|-------|----------|
| 001 | guest-session-create | Must |
| 002 | guest-session-claim | Must |
| 003 | guest-session-cleanup | Must |

---

## Technical Constraints

- The `X-Guest-Token` header handler must integrate with ASP.NET Core authorization without breaking existing Bearer JWT auth
- Guest token is the raw UUID returned to the client; stored as-is in `GuestSession.Id` (not hashed — low-value token, 7-day TTL, no account privilege)
- Phone validation: Romanian mobile format `07[0-9]{8}` (FluentValidation regex)
- `GuestSessionCleanupJob` runs as `IHostedService` with `PeriodicTimer` (1-hour interval)
