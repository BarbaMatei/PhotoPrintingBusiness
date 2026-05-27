---
id: 004-refresh-token
unit: 001-auth-core
intent: 002-authentication
status: draft
priority: must
created: 2026-05-20T12:55:00Z
assigned_bolt: 005-auth-core
implemented: false
---

# Story: 004-refresh-token

## User Story

**As a** logged-in user whose access token has expired
**I want** the app to silently obtain a new access token
**So that** I stay logged in without re-entering my credentials

## Acceptance Criteria

- [ ] **Given** `POST /api/auth/refresh` with a valid HttpOnly cookie, **When** the stored hash matches and token is not expired, **Then** returns 200 `{accessToken, expiresIn: 900}` and sets a new refresh token cookie (old token row revoked, new row inserted)
- [ ] **Given** a refresh request, **When** the cookie is absent, **Then** returns 401
- [ ] **Given** a refresh request, **When** the token is expired, **Then** returns 401 and clears the cookie
- [ ] **Given** a refresh request, **When** the token has already been rotated (replay attack — hash not found), **Then** returns 401, revokes ALL refresh tokens for that user (token family compromise), and clears cookie
- [ ] **Given** a successful refresh, **When** the new refresh token is set, **Then** its `ExpiresAt` is `now + 30 days` (sliding window resets on each use)

## Technical Notes

- Sliding window: `ExpiresAt` is always `DateTime.UtcNow.AddDays(30)` on each successful rotation
- Token family compromise: if hash not found but UserId can be inferred (e.g., from an old JWT `sub`), revoke all tokens for that user
- Cookie path restricted to `/api/auth` to minimise cookie transmission surface

## Dependencies

### Requires
- Story 003-jwt-login (RefreshToken table and cookie setup)

### Enables
- Story 005-logout (same token table)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Two simultaneous refresh requests (race) | One succeeds; second gets 401 (old token already rotated) |
| Refresh token used after logout | 401 (token revoked at logout) |

## Out of Scope

- Frontend silent refresh logic (→ `004-authentication-ui`, handled by `jwtInterceptor`)
