---
unit: 004-authentication-ui
intent: 002-authentication
unit_type: frontend
default_bolt_type: simple-construction-bolt
phase: inception
status: ready
created: 2026-05-20T12:58:00Z
updated: 2026-05-20T12:58:00Z
---

# Unit Brief: authentication-ui

## Purpose

Angular 21 pages and components for the complete authentication UX: registration with GDPR consent, email verification pending with resend, login with redirect-back, Google Identity Services button, guest checkout prompt/form, forgot-password, and reset-password. Extends the existing `AuthService` and wires into the app shell routing from bolt 004.

## Scope

### In Scope
- `RegisterPage` component with reactive form, real-time password strength, GDPR checkbox
- `EmailVerificationPendingPage` with resend button and countdown
- `LoginPage` with redirect-back, show/hide password, forgot-password link
- `GoogleAuthButton` shared component (Google Identity Services SDK)
- `GuestCheckoutPromptComponent` modal (3 options: guest / login / register)
- `GuestCheckoutFormComponent` with Romanian phone validation
- `ForgotPasswordPage` form
- `ResetPasswordPage` form (reads `userId` + `token` from query params)
- Extended `AuthService` methods: `register()`, `login()`, `googleLogin()`, `refreshToken()`
- New `GuestAuthService`: `createGuestSession()`, `claimGuestSession()`
- Vitest unit tests for all services and page components

### Out of Scope
- Backend endpoints (→ units 001, 002, 003)
- Cart/checkout/order pages (→ future intents)
- Admin UI (→ future intents)

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-7 | Registration frontend — form, GDPR, email-pending redirect | Must |
| FR-8 | Login frontend — form, redirect-back, "Ține-mă minte" toggle | Must |
| FR-9 | Google OAuth frontend — Identity Services button, account-linked toast | Must |
| FR-10 | Guest checkout frontend — prompt modal, form, localStorage storage | Must |

---

## Domain Concepts

### Key Components/Services
| Item | Type | Description |
|------|------|-------------|
| `RegisterPage` | Component (page) | Registration form, GDPR consent, email-pending redirect |
| `LoginPage` | Component (page) | Login form, redirect-back |
| `EmailVerificationPendingPage` | Component (page) | Shows after registration; resend button |
| `ForgotPasswordPage` | Component (page) | Email input to request reset |
| `ResetPasswordPage` | Component (page) | New password form, reads token from URL |
| `GoogleAuthButton` | Component (shared) | Renders Google Identity Services button |
| `GuestCheckoutPromptComponent` | Component (shared) | Modal with 3 options |
| `AuthService` | Service | HTTP calls to auth API; manages accessToken in sessionStorage |
| `GuestAuthService` | Service | Guest session HTTP calls; manages guestToken in localStorage |

### Route Integration (into bolt 004 shell)
| Route | Component | Guard |
|-------|-----------|-------|
| `/auth/register` | `RegisterPage` | None (public) |
| `/auth/login` | `LoginPage` | None (public) |
| `/auth/verify-email` | `EmailVerificationPendingPage` | None (public) |
| `/auth/forgot-password` | `ForgotPasswordPage` | None (public) |
| `/auth/reset-password` | `ResetPasswordPage` | None (public) |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 7 |
| Must Have | 7 |
| Should Have | 0 |
| Could Have | 0 |

### Stories
| # | Story | Priority |
|---|-------|----------|
| 001 | register-page | Must |
| 002 | email-verification-pending | Must |
| 003 | login-page | Must |
| 004 | google-auth-button | Must |
| 005 | guest-checkout-prompt | Must |
| 006 | forgot-password-page | Must |
| 007 | reset-password-page | Must |

---

## Technical Constraints

- Angular 21 standalone components; no `NgModule`
- Reactive forms (`ReactiveFormsModule`) for all forms — no template-driven forms
- Vitest 4.x for tests; `TestBed` + `vi.spyOn()` pattern (established in bolt 004)
- SCSS: `@use 'styles/variables' as *; @use 'styles/mixins' as *` in all component stylesheets
- Google Identity Services: loaded via `<script>` tag in `index.html` (not npm package); typed via `@types/google.accounts` or manual `declare const google`
- `jwtInterceptor` and `guestInterceptor` from bolt 004 handle header attachment automatically
- `errorInterceptor` from bolt 004 handles 401 → logout + redirect automatically
