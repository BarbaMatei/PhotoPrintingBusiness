---
id: 008-authentication-ui
unit: 004-authentication-ui
intent: 002-authentication
type: simple-construction-bolt
status: complete
started: 2026-05-20T18:00:00Z
completed: 2026-05-20T20:00:00Z
current_stage: complete
stages_completed: [plan, implement, test]
stories:
  - 001-register-page
  - 002-email-verification-pending
  - 003-login-page
  - 004-google-auth-button
  - 005-guest-checkout-prompt
  - 006-forgot-password-page
  - 007-reset-password-page
created: 2026-05-20T13:00:00Z

requires_bolts: [005-auth-core, 006-social-auth, 007-guest-sessions]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 1
---

## Bolt: 008-authentication-ui

### Objective

Implement all Angular 21 authentication pages and shared components: registration with real-time password strength and GDPR consent, email verification pending with resend, login with redirect-back, Google Identity Services button, guest checkout prompt/form modal, forgot-password page, and reset-password page.

### Stories Included

- [ ] **001-register-page**: RegisterPage component — reactive form, password strength indicator, GDPR checkbox - Priority: Must
- [ ] **002-email-verification-pending**: EmailVerificationPendingPage — resend button, countdown, confirmed banner - Priority: Must
- [ ] **003-login-page**: LoginPage — email/password form, redirect-back, error mapping - Priority: Must
- [ ] **004-google-auth-button**: GoogleAuthButton shared component — Identity Services SDK integration - Priority: Must
- [ ] **005-guest-checkout-prompt**: GuestCheckoutPromptComponent modal — 3-option prompt + guest form - Priority: Must
- [ ] **006-forgot-password-page**: ForgotPasswordPage — single email field, always-success message - Priority: Must
- [ ] **007-reset-password-page**: ResetPasswordPage — new password form, reads token from query params - Priority: Must

### Expected Outputs

- 5 page components under `src/app/features/auth/pages/`
- 2 shared components: `GoogleAuthButton`, `GuestCheckoutPromptComponent`
- Extended `AuthService` (new methods: `register`, `login`, `googleLogin`, `forgotPassword`, `resetPassword`, `resendConfirmation`)
- New `GuestAuthService` (`createGuestSession`, `claimGuestSession`)
- Updated `auth.routes.ts` with all 5 routes
- SCSS for each component using `@use 'styles/variables' as *`
- Vitest tests: all services (mocked HTTP) + all page components (TestBed)

### Dependencies

#### Bolt Dependencies (within intent)
- **005-auth-core** (Required): `/register`, `/login`, `/confirm-email`, `/forgot-password`, `/reset-password` endpoints
- **006-social-auth** (Required): `/google` endpoint
- **007-guest-sessions** (Required): `/guest` endpoint

#### Unit Dependencies (cross-unit)
- **Bolt 004** (angular-app-shell): Required — routing shell, AuthService skeleton, guards, interceptors, ToastService
