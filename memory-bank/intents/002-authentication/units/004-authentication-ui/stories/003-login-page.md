---
id: 003-login-page
unit: 004-authentication-ui
intent: 002-authentication
status: draft
priority: must
created: 2026-05-20T12:58:00Z
assigned_bolt: 008-authentication-ui
implemented: false
---

# Story: 003-login-page

## User Story

**As a** registered user
**I want** to log in with my email and password
**So that** I can access my account and order history

## Acceptance Criteria

- [ ] **Given** the `/auth/login` page, **When** rendered, **Then** shows Email, Password fields; "Ține-mă minte" toggle; show/hide password icon; "Am uitat parola" link; "Nu ai cont? Înregistrează-te" link
- [ ] **Given** a valid form submission, **When** `POST /api/auth/login` returns 200, **Then** `accessToken` is stored in `sessionStorage`, `AuthService` state is updated, and the user is redirected to the originally requested URL (from `authGuard`) or `/tipareste`
- [ ] **Given** a submission, **When** the API returns 401, **Then** shows inline form error `"Email sau parolă incorectă"` (no field-level leakage)
- [ ] **Given** a submission, **When** the API returns 403 (unverified email), **Then** shows error `"Confirmați adresa de email pentru a continua"` with a resend link
- [ ] **Given** a submission, **When** the API returns 423 (locked), **Then** shows error with remaining minutes: `"Contul este blocat. Încercați din nou în {N} minute"`
- [ ] **Given** a submission with an `accountLinked: true` response (Google auto-link), **When** displayed, **Then** shows toast `"Contul tău Google a fost conectat"` — NOTE: this is actually set by story 004 (Google button), but login page must handle redirect-back for Google flow too
- [ ] **Given** an in-flight request, **When** active, **Then** spinner shown, submit disabled

## Technical Notes

- Redirect-back: `AuthService.getReturnUrl()` (already implemented in bolt 004)
- `AuthService.login(email, password)` → `POST /api/auth/login` → stores `accessToken` in `sessionStorage`, calls `setAuthenticated(true)`
- "Ține-mă minte" toggle: cosmetic only (cookie always 30-day sliding per D-1); label can say "Sesiune persistentă" alternatively

## Dependencies

### Requires
- Bolt 005 (Unit 001-auth-core: `POST /api/auth/login`)
- Bolt 004 (AuthService.getReturnUrl(), authGuard, routing)

### Enables
- Story 004-google-auth-button (Google button appears on this page)
- All guarded routes (user can now pass `authGuard`)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| User already authenticated navigates to `/auth/login` | Redirect to `/tipareste` (no re-login needed) |
| Return URL is an external URL (open-redirect) | Ignored; redirect to `/tipareste` (authGuard already validates relative URLs) |

## Out of Scope

- Social login logic (→ story 004)
- Guest prompt (→ story 005, triggered from `/checkout` not from login page)
