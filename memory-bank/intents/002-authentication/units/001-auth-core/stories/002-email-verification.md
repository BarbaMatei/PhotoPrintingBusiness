---
id: 002-email-verification
unit: 001-auth-core
intent: 002-authentication
status: complete
priority: must
created: 2026-05-20T12:55:00Z
assigned_bolt: 005-auth-core
implemented: true
---

# Story: 002-email-verification

## User Story

**As a** newly registered user
**I want** to verify my email address by clicking a link
**So that** my account is activated and I can log in

## Acceptance Criteria

- [ ] **Given** `GET /api/auth/confirm-email?userId={id}&token={raw}`, **When** the hashed token matches the DB record and is not expired, **Then** `User.IsEmailConfirmed` is set to `true`, the token row is deleted, and returns 200
- [ ] **Given** a confirmation request, **When** the token is expired (> 24h), **Then** returns 400 `"Link invalid sau expirat"`
- [ ] **Given** a confirmation request, **When** the token does not match (tampered or already used), **Then** returns 400 `"Link invalid sau expirat"`
- [ ] **Given** a confirmation request, **When** the account is already confirmed, **Then** returns 200 (idempotent, no error)
- [ ] **Given** `POST /api/auth/resend-confirmation {email}`, **When** the account is not yet confirmed, **Then** a new token row is created (old one deleted) and a new email is sent
- [ ] **Given** a resend request, **When** the same email resends more than 3 times in an hour, **Then** returns 429
- [ ] **Given** a resend request, **When** the account is already confirmed, **Then** returns 200 silently (no email sent, no error)

## Technical Notes

- Token in URL is the raw UUID; comparison is `SHA256(rawToken) == storedHash`
- Resend: delete old token row before inserting new one (prevent accumulation)
- Email link format: `{frontendUrl}/auth/verify-email?userId={id}&token={rawToken}`

## Dependencies

### Requires
- Story 001-user-registration (EmailConfirmationTokens table)
- Bolt 003 (IEmailService)

### Enables
- Story 003-jwt-login (login blocked until IsEmailConfirmed=true)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Token used twice (replay) | Second call: token row deleted on first use → 400 on second |
| Non-existent userId | 400 (same message — no userId enumeration) |
| Resend for non-existent email | 200 silently (no email enumeration) |

## Out of Scope

- Frontend verification pending page (→ `004-authentication-ui`)
