---
id: 003-jwt-login
unit: 001-auth-core
intent: 002-authentication
status: draft
priority: must
created: 2026-05-20T12:55:00Z
assigned_bolt: 005-auth-core
implemented: false
---

# Story: 003-jwt-login

## User Story

**As a** registered and verified user
**I want** to log in with my email and password
**So that** I receive a JWT access token to call protected APIs

## Acceptance Criteria

- [ ] **Given** `POST /api/auth/login {email, password}`, **When** credentials are valid and email is confirmed, **Then** returns 200 `{accessToken, expiresIn: 900}` and sets HttpOnly Secure `SameSite=Strict` cookie named `refreshToken`
- [ ] **Given** a login request, **When** the email does not exist or the password does not match, **Then** returns 401 `"Email sau parolă incorectă"` (identical message — no field-level leakage)
- [ ] **Given** a login request for a valid user whose email is NOT confirmed, **Then** returns 403 `"Confirmați adresa de email pentru a continua"`
- [ ] **Given** a login request for an account that is locked out, **Then** returns 423 with remaining lockout seconds
- [ ] **Given** successful login, **When** the JWT is decoded, **Then** claims include: `sub` (userId), `email`, `role`, `firstName`, `exp` (15 min from now), `iss`, `aud`
- [ ] **Given** successful login, **When** a `RefreshToken` row is created, **Then** it stores `SHA256(rawToken)`, `ExpiresAt = now + 30 days`, `CreatedAt`, `UserId`

## Technical Notes

- JWT signed with RS256 private key loaded from `appsettings.json` (`JwtSettings:PrivateKeyPem` or file path)
- Access token: 15-min expiry (`JwtSettings:AccessTokenMinutes`)
- Refresh token cookie: `HttpOnly = true`, `Secure = true`, `SameSite = Strict`, `Path = /api/auth`, `MaxAge = 30 days`
- Failed login increments `User.FailedLoginCount`; lock is applied when count reaches 5

## Dependencies

### Requires
- Story 001-user-registration (User entity)
- Story 002-email-verification (IsEmailConfirmed flag must be set)

### Enables
- Story 004-refresh-token (uses same RefreshToken table)
- Story 005-logout (uses same cookie)
- Story 006-account-lockout (uses FailedLoginCount)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Email with mixed case | Normalize before lookup |
| Account locked for exactly 0 remaining seconds | Treat as unlocked, allow login |
| Login after password reset (old refresh tokens revoked) | New login succeeds, new refresh token issued |

## Out of Scope

- "Remember me" toggle behaviour change (cookie always 30-day — decision D-1)
- Frontend login form (→ `004-authentication-ui`)
