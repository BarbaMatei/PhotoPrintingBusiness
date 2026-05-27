---
id: 007-reset-password-page
unit: 004-authentication-ui
intent: 002-authentication
status: draft
priority: must
created: 2026-05-20T12:58:00Z
assigned_bolt: 008-authentication-ui
implemented: false
---

# Story: 007-reset-password-page

## User Story

**As a** user who clicked a password reset link in their email
**I want** to enter a new password
**So that** I can regain access to my account

## Acceptance Criteria

- [ ] **Given** `/auth/reset-password?userId={id}&token={token}`, **When** rendered, **Then** shows "Parolă nouă" and "Confirmă parola" fields with the same strength rules as registration
- [ ] **Given** a valid form submission, **When** `POST /api/auth/reset-password` returns 200, **Then** shows success message `"Parola a fost schimbată. Vă puteți autentifica"` with a link to `/auth/login`
- [ ] **Given** a submission, **When** the API returns 400 `"Link invalid sau expirat"`, **Then** shows the error prominently with a link back to `/auth/forgot-password`
- [ ] **Given** the form, **When** `confirmPassword` does not match `password`, **Then** shows inline error `"Parolele nu se potrivesc"`
- [ ] **Given** an in-flight request, **When** active, **Then** spinner shown, button disabled
- [ ] **Given** the page, **When** `userId` or `token` query params are missing, **Then** shows error `"Link invalid"` without rendering the form

## Technical Notes

- `userId` and `token` read from `ActivatedRoute.queryParamMap` (Angular router)
- `AuthService.resetPassword({userId, token, newPassword})` → `POST /api/auth/reset-password`
- Same password strength validator as `RegisterPage` (extracted as shared `passwordStrengthValidator`)

## Dependencies

### Requires
- Bolt 005 (Unit 001-auth-core: `POST /api/auth/reset-password`)
- Story 006-forgot-password-page (user arrives from reset email)
- Story 001-register-page (shared `passwordStrengthValidator`)

### Enables
- Story 003-login-page (user redirected there after success)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Token already used (one-time) | API returns 400 → error message + link to forgot-password |
| Page visited without query params | Shows "Link invalid" — no form rendered |
| Token expired (1h) | API returns 400 → same error handling |

## Out of Scope

- Automatic login after reset (user must log in manually)
