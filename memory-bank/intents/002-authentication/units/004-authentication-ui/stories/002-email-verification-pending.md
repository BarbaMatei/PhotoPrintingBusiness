---
id: 002-email-verification-pending
unit: 004-authentication-ui
intent: 002-authentication
status: draft
priority: must
created: 2026-05-20T12:58:00Z
assigned_bolt: 008-authentication-ui
implemented: false
---

# Story: 002-email-verification-pending

## User Story

**As a** user who just registered
**I want** to see a confirmation page that explains I need to verify my email
**So that** I understand why I can't log in yet and how to resend the email

## Acceptance Criteria

- [ ] **Given** navigation to `/auth/verify-email`, **When** rendered, **Then** shows a message explaining that a verification email has been sent and the user must click the link
- [ ] **Given** the verification pending page, **When** rendered, **Then** shows a "Retrimite email" (resend) button
- [ ] **Given** the resend button, **When** clicked, **Then** calls `POST /api/auth/resend-confirmation {email}` and shows a toast `"Email de confirmare retrimis"` on success
- [ ] **Given** a resend, **When** the API returns 429 (rate limited), **Then** shows a toast `"Prea multe încercări. Așteptați câteva minute"` and disables the button for 60 seconds with a countdown
- [ ] **Given** the page, **When** the user clicks a link in the email and the backend confirms, **Then** a success banner at `/auth/verify-email?confirmed=true` shows `"Email confirmat! Vă puteți autentifica"` with a login link

## Technical Notes

- The email address to resend to is passed via Angular router state from the register page (not stored in a cookie)
- Countdown timer for resend cooldown: use `setInterval` and `OnDestroy` to clean up
- `?confirmed=true` query param: set by the backend redirect after `GET /api/auth/confirm-email` succeeds

## Dependencies

### Requires
- Story 001-register-page (navigates here after success)
- Bolt 005 (Unit 001-auth-core: `POST /api/auth/resend-confirmation`)

### Enables
- Story 003-login-page (user navigates there after confirming)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| User visits `/auth/verify-email` directly without registering | Page renders without email address; resend button disabled |
| Verification email link clicked in a different browser tab | Page still shows pending state (no WebSocket); user manually navigates to login |

## Out of Scope

- Automatic redirect after email click (user manually navigates to login)
