---
id: 001-guest-session-create
unit: 003-guest-sessions
intent: 002-authentication
status: complete
priority: must
created: 2026-05-20T12:57:00Z
assigned_bolt: 007-guest-sessions
implemented: true
---

# Story: 001-guest-session-create

## User Story

**As a** visitor who wants to order without registering
**I want** to provide my contact details and receive a guest token
**So that** I can proceed to checkout anonymously

## Acceptance Criteria

- [ ] **Given** `POST /api/auth/guest {firstName, lastName, email, phone}`, **When** all fields are valid, **Then** a `GuestSession` row is inserted (`Id=UUID`, `ExpiresAt=now+7days`) and returns 200 `{guestToken: UUID}`
- [ ] **Given** a guest request, **When** any field is invalid (missing name, invalid email, wrong phone format), **Then** returns 400 with a field-keyed error map
- [ ] **Given** a valid `X-Guest-Token` header on a protected endpoint, **When** the token exists in DB and is not expired, **Then** the request is authorized (equivalent to authenticated user for order/cart/upload operations)
- [ ] **Given** an `X-Guest-Token` header, **When** the session is expired, **Then** returns 401 and the guest must create a new session
- [ ] **Given** a request with both `Authorization: Bearer` and `X-Guest-Token`, **When** processed, **Then** the Bearer JWT takes precedence

## Technical Notes

- Phone validation regex: `^07[0-9]{8}$` (Romanian mobile)
- Guest token = raw `GuestSession.Id` (UUID) — returned once, not re-derivable
- Authorization: implement `IAuthorizationHandler` or a custom `AuthenticationHandler` for the `X-Guest-Token` scheme

## Dependencies

### Requires
- Bolt 001 (ExceptionHandlerMiddleware)
- Bolt 002 (security middleware, CORS)

### Enables
- Story 002-guest-session-claim
- All upload/cart/order endpoints (future intents — X-Guest-Token dual auth)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Same email used for multiple guest sessions | Allowed — no uniqueness constraint on email in GuestSession |
| Guest creates session but never places order | Cleaned up by story 003 after 7 days |

## Out of Scope

- Frontend guest form and localStorage storage (→ `004-authentication-ui`)
