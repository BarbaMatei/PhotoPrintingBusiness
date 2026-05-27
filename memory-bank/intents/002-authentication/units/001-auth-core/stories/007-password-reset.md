---
id: 007-password-reset
unit: 001-auth-core
intent: 002-authentication
status: draft
priority: must
created: 2026-05-20T12:55:00Z
assigned_bolt: 005-auth-core
implemented: false
---

# Story: 007-password-reset

## User Story

**As a** user who has forgotten their password
**I want** to reset it via a link sent to my email
**So that** I can regain access to my account

## Acceptance Criteria

- [ ] **Given** `POST /api/auth/forgot-password {email}`, **When** called with any email, **Then** always returns 200 (no email enumeration); if account exists, a `PasswordResetToken` row is inserted and a reset email is sent via `IEmailService`
- [ ] **Given** a forgot-password request, **When** an unexpired reset token already exists for the user, **Then** the old token is deleted before inserting the new one
- [ ] **Given** `POST /api/auth/reset-password {userId, token, newPassword}`, **When** the hashed token matches and is within 1-hour expiry, **Then** the password hash is updated, the token row is deleted, and ALL `RefreshToken` rows for the user are revoked (`RevokedAt = UtcNow`)
- [ ] **Given** a reset request, **When** the token is expired or invalid, **Then** returns 400 `"Link invalid sau expirat"`
- [ ] **Given** a reset request, **When** `newPassword` fails strength validation, **Then** returns 400 with field errors
- [ ] **Given** a successful password reset, **When** the user attempts to use a previously valid refresh token cookie, **Then** the cookie is rejected with 401 (all tokens revoked)

## Technical Notes

- Reset token: `Guid.NewGuid().ToString()` → stored as `SHA256(token)` in `PasswordResetTokens` table, 1h expiry
- Reset email link: `{frontendUrl}/auth/reset-password?userId={id}&token={rawToken}`
- Password strength rules must match registration (same FluentValidation rule set, reusable)
- Lockout is also cleared on successful password reset (`FailedLoginCount = 0`, `LockoutEnd = null`)

## Dependencies

### Requires
- Story 001-user-registration (User entity)
- Story 003-jwt-login (RefreshToken table for revoking all sessions)
- Bolt 003 (IEmailService)

### Enables
- Nothing (recovery operation)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Email not in DB | 200 silently (anti-enumeration) |
| Token used twice | Second use: 400 (token deleted on first successful use) |
| Reset while locked out | Password reset also clears lockout |

## Out of Scope

- Frontend forgot-password and reset-password pages (→ `004-authentication-ui`)
