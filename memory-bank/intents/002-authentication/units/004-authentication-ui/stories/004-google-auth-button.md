---
id: 004-google-auth-button
unit: 004-authentication-ui
intent: 002-authentication
status: complete
priority: must
created: 2026-05-20T12:58:00Z
assigned_bolt: 008-authentication-ui
implemented: true
---

# Story: 004-google-auth-button

## User Story

**As a** user on the login or register page
**I want** to sign in with my Google account
**So that** I don't need to type a password

## Acceptance Criteria

- [ ] **Given** the `GoogleAuthButton` component, **When** rendered on login or register page, **Then** the Google Identity Services button is visible with the label "Continuă cu Google"
- [ ] **Given** the user clicks the Google button and authorizes, **When** the `id_token` callback fires, **Then** `POST /api/auth/google {idToken}` is called
- [ ] **Given** a successful Google sign-in, **When** the API returns `{accessToken, accountLinked: false}`, **Then** `accessToken` stored in `sessionStorage`, `AuthService` state updated, navigated to return URL or `/tipareste`
- [ ] **Given** a successful Google sign-in, **When** `accountLinked: true`, **Then** same as above plus toast `"Contul tău Google a fost conectat"`
- [ ] **Given** the Google SDK fails to load or the user cancels, **When** the error callback fires, **Then** toast `"Autentificarea Google a eșuat. Încearcă din nou"` is shown
- [ ] **Given** the component, **When** Google SDK is not yet loaded (script async), **Then** renders a placeholder/disabled button until `window.google` is available

## Technical Notes

- Google Identity Services loaded via `<script src="https://accounts.google.com/gsi/client" async defer>` in `index.html`
- Component uses `afterNextRender()` or `ngAfterViewInit` to call `google.accounts.id.initialize()` and `google.accounts.id.renderButton()`
- `GoogleClientId` read from `environment.googleClientId`
- `AuthService.googleLogin(idToken)` → `POST /api/auth/google` → same storage/state update as password login

## Dependencies

### Requires
- Bolt 006 (Unit 002-social-auth: `POST /api/auth/google` available)
- Story 003-login-page (mounted on login page)
- Story 001-register-page (mounted on register page)

### Enables
- Nothing (terminal UI action)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| User already has an account with same email | `accountLinked: true` in response → toast |
| Google popup blocked by browser | Google SDK fires error callback → toast |
| `environment.googleClientId` is empty/placeholder | Button still renders; clicking shows Google error |

## Out of Scope

- Google One Tap prompt (not required for v1)
- Other social providers
