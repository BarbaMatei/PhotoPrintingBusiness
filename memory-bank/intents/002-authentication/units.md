---
intent: 002-authentication
phase: inception
status: units-decomposed
updated: 2026-05-20T12:52:00Z
---

# Authentication — Unit Decomposition

## Units Overview

This intent decomposes into **4 units** of work (3 backend + 1 frontend):

---

### Unit 1: `001-auth-core`

**Description**: Core authentication engine — user registration, email verification, JWT login with rotating refresh tokens, logout, account lockout, and password reset. The foundational unit all others depend on.

**Assigned Requirements**: FR-1, FR-2, FR-3, FR-6

**Stories**:
- 001-user-registration
- 002-email-verification
- 003-jwt-login
- 004-refresh-token
- 005-logout
- 006-account-lockout
- 007-password-reset

**Deliverables**:
- `User` entity + EF Core migration
- `RefreshToken` entity (hashed, sliding-window)
- `EmailConfirmationToken` entity
- `PasswordResetToken` entity
- `POST /api/auth/register`, `GET /api/auth/confirm-email`, `POST /api/auth/resend-confirmation`
- `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/logout`
- `POST /api/auth/forgot-password`, `POST /api/auth/reset-password`
- Unit + integration tests

**Dependencies**:
- Depends on: bolt 001 (error handling), bolt 002 (security middleware), bolt 003 (IEmailService)
- Depended by: Unit 2 (social-auth), Unit 3 (guest-sessions), Unit 4 (auth-ui)

**Estimated Complexity**: XL

---

### Unit 2: `002-social-auth`

**Description**: Google OAuth integration — validates Google `id_token` server-side, upserts users, handles account linking when a matching email already exists.

**Assigned Requirements**: FR-4

**Stories**:
- 001-google-token-validation
- 002-account-upsert-linking

**Deliverables**:
- `ExternalLogin` entity (UserId, Provider, ProviderKey)
- `POST /api/auth/google` endpoint
- Account auto-link logic
- Unit + integration tests

**Dependencies**:
- Depends on: Unit 1 (`001-auth-core`) — needs `User` entity and JWT issuance logic
- Depended by: Unit 4 (auth-ui)

**Estimated Complexity**: M

---

### Unit 3: `003-guest-sessions`

**Description**: Anonymous guest checkout — creates short-lived guest sessions identified by an opaque token, supports claiming guest orders after registration, and cleans up expired sessions via background job.

**Assigned Requirements**: FR-5

**Stories**:
- 001-guest-session-create
- 002-guest-session-claim
- 003-guest-session-cleanup

**Deliverables**:
- `GuestSession` entity (UUID, contact info, 7-day TTL)
- `POST /api/auth/guest`, `POST /api/auth/guest/claim`
- `GuestSessionCleanupJob` background service
- `AuthorizationHandler` that accepts X-Guest-Token alongside Bearer JWT
- Unit + integration tests

**Dependencies**:
- Depends on: Unit 1 (`001-auth-core`) — claim flow needs authenticated user context
- Depended by: Unit 4 (auth-ui); all upload/cart/order endpoints

**Estimated Complexity**: M

---

### Unit 4: `004-authentication-ui`

**Description**: Angular 21 pages and components for all authentication flows — registration with GDPR consent, email verification pending, login with redirect-back, Google OAuth button, guest checkout prompt, forgot/reset password.

**Assigned Requirements**: FR-7, FR-8, FR-9, FR-10

**Unit Type**: frontend
**Default Bolt Type**: simple-construction-bolt

**Stories**:
- 001-register-page
- 002-email-verification-pending
- 003-login-page
- 004-google-auth-button
- 005-guest-checkout-prompt
- 006-forgot-password-page
- 007-reset-password-page

**Deliverables**:
- `RegisterPage` component (`/auth/register`)
- `EmailVerificationPendingPage` component (`/auth/verify-email`)
- `LoginPage` component (`/auth/login`)
- `GoogleAuthButton` shared component
- `GuestCheckoutPrompt` modal component
- `ForgotPasswordPage` component (`/auth/forgot-password`)
- `ResetPasswordPage` component (`/auth/reset-password`)
- `AuthService` methods: `register()`, `login()`, `googleLogin()`, `logout()`, `refreshToken()`
- `GuestAuthService`: `createGuestSession()`, `claimGuestSession()`
- Vitest unit tests for all services and components

**Dependencies**:
- Depends on: Units 1, 2, 3 (all backend units must have their APIs available)
- Depends on: bolt 004 (Angular app shell + routing, `AuthGuard`, `GuestOrAuthGuard`)
- Depended by: None within this intent

**Estimated Complexity**: XL

---

## Requirement-to-Unit Mapping

| FR | Requirement | Unit |
|----|-------------|------|
| FR-1 | Email/password registration backend | `001-auth-core` |
| FR-2 | Email verification backend | `001-auth-core` |
| FR-3 | JWT login + refresh token backend | `001-auth-core` |
| FR-4 | Google OAuth backend | `002-social-auth` |
| FR-5 | Guest session backend | `003-guest-sessions` |
| FR-6 | Password reset backend | `001-auth-core` |
| FR-7 | Registration frontend | `004-authentication-ui` |
| FR-8 | Login frontend | `004-authentication-ui` |
| FR-9 | Google OAuth frontend | `004-authentication-ui` |
| FR-10 | Guest checkout frontend | `004-authentication-ui` |

---

## Unit Dependency Graph

```text
[bolt 001-004 Foundation]
         │
         ▼
[001-auth-core] ──────────────────────────────────┐
         │                                         │
         ├──► [002-social-auth]                   │
         │                                         │
         └──► [003-guest-sessions]                 │
                    │                              │
                    ▼                              ▼
             [004-authentication-ui] ◄────────────┘
```

## Execution Order

1. **Bolt 005**: `001-auth-core` (foundation — must complete first)
2. **Bolt 006**: `002-social-auth` (depends on auth-core User entity)
3. **Bolt 007**: `003-guest-sessions` (depends on auth-core for claim flow)
4. **Bolt 008**: `004-authentication-ui` (depends on all 3 backend bolts)
