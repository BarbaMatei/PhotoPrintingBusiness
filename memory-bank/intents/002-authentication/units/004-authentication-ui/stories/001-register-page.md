---
id: 001-register-page
unit: 004-authentication-ui
intent: 002-authentication
status: draft
priority: must
created: 2026-05-20T12:58:00Z
assigned_bolt: 008-authentication-ui
implemented: false
---

# Story: 001-register-page

## User Story

**As a** new visitor
**I want** to fill in a registration form with real-time validation
**So that** I can create an account and start ordering

## Acceptance Criteria

- [ ] **Given** the `/auth/register` page, **When** rendered, **Then** shows fields: First Name, Last Name, Email, Password, Confirm Password, Phone (optional), GDPR consent checkbox
- [ ] **Given** the password field, **When** the user types, **Then** a real-time strength indicator shows which rules are met (min 8 chars, 1 uppercase, 1 digit, 1 special character)
- [ ] **Given** the GDPR checkbox is unchecked, **When** the form is otherwise valid, **Then** the submit button remains disabled
- [ ] **Given** a valid form submission, **When** `POST /api/auth/register` returns 201, **Then** the user is navigated to `/auth/verify-email` (no auto-login)
- [ ] **Given** a submission, **When** the API returns 409, **Then** the email field shows inline error `"Adresa de email este deja folosită"`
- [ ] **Given** a submission, **When** the API returns 400, **Then** field-level errors are mapped to the corresponding form controls
- [ ] **Given** an in-flight request, **When** the user tries to submit, **Then** the submit button shows a spinner and is disabled
- [ ] **Given** all labels and messages, **When** displayed, **Then** all text is in Romanian

## Technical Notes

- Form group: `firstName` (required), `lastName` (required), `email` (required, email validator), `password` (required, custom strength validator), `confirmPassword` (required, must match password), `phone` (optional, pattern `07[0-9]{8}`), `gdprConsent` (required, must be `true`)
- Password strength validator: custom `ValidatorFn` — checks each rule independently, returns null only if all pass
- `AuthService.register(dto)` → `POST /api/auth/register` → returns `Observable<{userId}>`

## Dependencies

### Requires
- Bolt 005 (Unit 001-auth-core: `POST /api/auth/register` available)
- Bolt 004 (routing shell: `/auth/register` route + `auth.routes.ts`)

### Enables
- Story 002-email-verification-pending (navigated to after success)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Paste into password field | Strength validator triggers on value change, not keystroke |
| Phone left empty (optional) | Submitted as null; no validation error |
| User navigates back from verify-email page | Can re-fill form (no state preserved) |

## Out of Scope

- Email verification logic (→ story 002)
- Auto-login after registration
