---
id: 006-account-lockout
unit: 001-auth-core
intent: 002-authentication
status: complete
priority: must
created: 2026-05-20T12:55:00Z
assigned_bolt: 005-auth-core
implemented: true
---

# Story: 006-account-lockout

## User Story

**As a** platform operator
**I want** accounts to lock after repeated failed logins
**So that** brute-force and credential-stuffing attacks are slowed down

## Acceptance Criteria

- [ ] **Given** a login attempt, **When** credentials are wrong, **Then** `User.FailedLoginCount` increments by 1
- [ ] **Given** `User.FailedLoginCount` reaches 5, **When** any further login attempt occurs, **Then** `User.LockoutEnd = UtcNow + 15 min` is set and a lockout notification email is sent via `IEmailService`
- [ ] **Given** a locked account, **When** a login attempt is made, **Then** returns 423 with `{message: "Contul este blocat. Încercați din nou în {N} minute.", retryAfterSeconds: N}`
- [ ] **Given** a locked account, **When** the lockout period expires, **Then** the next login attempt is allowed and `FailedLoginCount` resets to 0 on success
- [ ] **Given** a successful login, **When** `FailedLoginCount > 0`, **Then** `FailedLoginCount` is reset to 0

## Technical Notes

- Check `LockoutEnd > UtcNow` before password validation to avoid unnecessary hash computation
- Lockout email uses the same `IEmailService` abstraction; failure is non-fatal (log and continue)
- `LockoutEnd` and `FailedLoginCount` are columns on the `User` entity

## Dependencies

### Requires
- Story 003-jwt-login (User entity with FailedLoginCount)
- Bolt 003 (IEmailService for lockout notification)

### Enables
- Nothing (defensive behavior integrated into login flow)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Password reset while locked | Lock is cleared (all refresh tokens revoked, FailedLoginCount reset) |
| Clock skew on lockout expiry | Use `DateTime.UtcNow` consistently — no local time |

## Out of Scope

- IP-based lockout (handled by rate limiter in bolt 002)
- Unlock via admin UI (not in scope — lock expires automatically)
