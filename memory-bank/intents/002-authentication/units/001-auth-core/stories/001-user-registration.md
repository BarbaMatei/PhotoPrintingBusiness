---
id: 001-user-registration
unit: 001-auth-core
intent: 002-authentication
status: draft
priority: must
created: 2026-05-20T12:55:00Z
assigned_bolt: 005-auth-core
implemented: false
---

# Story: 001-user-registration

## User Story

**As a** new visitor
**I want** to create an account with my email and password
**So that** I can track my orders and use personalised features

## Acceptance Criteria

- [ ] **Given** a POST to `/api/auth/register` with valid fields, **When** the email is not already registered, **Then** a `User` row is inserted with `IsEmailConfirmed=false`, `Role=Customer`, and a hashed password; returns 201 `{userId}`
- [ ] **Given** a registration request, **When** the email already exists, **Then** returns 409 with `"Adresa de email este deja folosită"`
- [ ] **Given** a registration request, **When** FluentValidation fails (missing field, weak password, invalid email), **Then** returns 400 with a field-keyed error map
- [ ] **Given** a successful registration, **When** the user row is saved, **Then** an `EmailConfirmationToken` row is inserted (hashed UUID, 24h expiry) and `IEmailService.SendConfirmationEmailAsync` is called non-blocking
- [ ] **Given** a failing email send, **When** `IEmailService` throws, **Then** the exception is logged and the 201 response is still returned (email failure is non-fatal)
- [ ] **Given** more than 5 registration requests from the same IP in one hour, **When** the 6th request arrives, **Then** returns 429 (rate limiter from bolt 002)

## Technical Notes

- Password hashed via `IPasswordHasher<User>` (ASP.NET Identity PBKDF2-SHA256, 10 000 iterations)
- Email confirmation token: `Guid.NewGuid().ToString()` → stored as `SHA256(token)` in DB; raw token included in email link
- `GdprConsentAccepted` field on `User` must be `true` — validated by FluentValidator
- Registration DTO: `{ firstName, lastName, email, password, confirmPassword, phone?, gdprConsent }`

## Dependencies

### Requires
- Bolt 001 (ExceptionHandlerMiddleware for 400/409 responses)
- Bolt 002 (rate limiter policy)
- Bolt 003 (IEmailService)

### Enables
- Story 002-email-verification (needs EmailConfirmationToken table)
- Story 003-jwt-login (needs User entity)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Email with uppercase letters | Normalized to lowercase before uniqueness check |
| `confirmPassword` mismatch | 400 with field error on `confirmPassword` |
| `gdprConsent = false` | 400 — consent is mandatory |
| Concurrent duplicate registration | One succeeds with 201; other gets 409 |

## Out of Scope

- Auto-login after registration (user must verify email first)
- Admin account creation
