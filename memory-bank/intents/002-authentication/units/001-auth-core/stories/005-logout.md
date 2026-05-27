---
id: 005-logout
unit: 001-auth-core
intent: 002-authentication
status: draft
priority: must
created: 2026-05-20T12:55:00Z
assigned_bolt: 005-auth-core
implemented: false
---

# Story: 005-logout

## User Story

**As a** logged-in user
**I want** to log out
**So that** my session is invalidated and no one else can use my refresh token

## Acceptance Criteria

- [ ] **Given** `POST /api/auth/logout` with a valid HttpOnly cookie, **When** the token hash is found in DB, **Then** the `RefreshToken` row has `RevokedAt = UtcNow` set and the cookie is cleared; returns 200
- [ ] **Given** a logout request, **When** the cookie is absent or the token is not found, **Then** returns 200 (idempotent — already logged out)
- [ ] **Given** a logout, **When** the cookie is cleared, **Then** `Set-Cookie: refreshToken=; Max-Age=0; Path=/api/auth` is set in the response

## Technical Notes

- Logout is always 200 — never expose whether a session existed
- Setting `RevokedAt` (soft delete) is preferred over deleting the row, to enable audit logging
- A `POST` (not `GET`) is required so the CSRF `SameSite=Strict` cookie protection applies

## Dependencies

### Requires
- Story 003-jwt-login (RefreshToken table)

### Enables
- Nothing (terminal operation)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Logout with expired cookie | 200 (idempotent) |
| Logout when access token is also expired | 200 — logout endpoint requires only the refresh cookie, not a valid JWT |

## Out of Scope

- "Log out all devices" (revoke all refresh tokens) — not in scope for this story; password reset already covers this
