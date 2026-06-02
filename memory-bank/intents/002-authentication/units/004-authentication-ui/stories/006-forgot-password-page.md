---
id: 006-forgot-password-page
unit: 004-authentication-ui
intent: 002-authentication
status: complete
priority: must
created: 2026-05-20T12:58:00Z
assigned_bolt: 008-authentication-ui
implemented: true
---

# Story: 006-forgot-password-page

## User Story

**As a** user who has forgotten their password
**I want** to enter my email and receive a reset link
**So that** I can regain access to my account

## Acceptance Criteria

- [ ] **Given** the `/auth/forgot-password` page, **When** rendered, **Then** shows a single Email field and a "Trimite link de resetare" button
- [ ] **Given** a valid email submission, **When** `POST /api/auth/forgot-password` returns 200, **Then** the form is replaced with a confirmation message `"Dacă adresa există, vei primi un email cu instrucțiuni"` (anti-enumeration — same message regardless)
- [ ] **Given** a submission, **When** the API returns 400 (invalid email format), **Then** shows inline field error
- [ ] **Given** an in-flight request, **When** active, **Then** spinner shown, button disabled
- [ ] **Given** a "Înapoi la autentificare" link, **When** clicked, **Then** navigates to `/auth/login`

## Technical Notes

- Always shows success message after submit (regardless of whether the email exists) to prevent enumeration
- `AuthService.forgotPassword(email)` → `POST /api/auth/forgot-password`

## Dependencies

### Requires
- Bolt 005 (Unit 001-auth-core: `POST /api/auth/forgot-password`)
- Story 003-login-page ("Am uitat parola" link targets this page)

### Enables
- Story 007-reset-password-page (user clicks link in email → lands there)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Non-existent email submitted | Success message shown (anti-enumeration) |
| Email submitted twice quickly | Second submission shows success message again; backend handles idempotently |

## Out of Scope

- Countdown before allowing re-submit (not required)
