---
intent: 002-authentication
phase: inception
status: inception-complete
created: 2026-05-20T12:30:00Z
updated: 2026-05-20T13:05:00Z
---

# Requirements: User Authentication

## Intent Overview

Implement full authentication for FotoTipar: email+password register/login with JWT RS256 + rotating refresh tokens, Google OAuth, email verification, password reset, and guest checkout. Covers both backend (ASP.NET Core 8) and frontend (Angular 21) layers. This intent is the prerequisite for all user-facing features.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Users can register and log in securely | 0 auth-related security incidents; JWT tokens expire and rotate correctly | Must |
| Verified accounts only | ≥ 95% of registered users complete email verification within 24h | Must |
| Google OAuth increases conversion | Users can sign in with Google in < 3 clicks | Must |
| Guest checkout reduces friction | Guests can place an order without registering | Must |
| Password reset self-service | Users can reset password via email link within 60 min; no support ticket needed | Must |
| Account linking works transparently | Registering with Google after email signup auto-links with toast confirmation | Must |

---

## Decisions (from Checkpoint 1)

| # | Question | Decision | Rationale |
|---|----------|----------|-----------|
| D-1 | Refresh token storage | HttpOnly `SameSite=Strict` cookie (30-day) | Provides persistence the user wants while being XSS-immune; localStorage would expose token to script injection |
| D-2 | Refresh token rotation | Sliding-window (30-day, reset on each use) | Balances UX (active users stay logged in) with security (old tokens revoked immediately on rotation) |
| D-3 | Guest session TTL | 7 days | Enough time to complete a delayed order; background job cleans up orphaned sessions |
| D-4 | Google OAuth scope | `email`, `profile` (name + picture) only | No need for additional Google APIs; minimal-permission principle |
| D-5 | Account linking | Auto-link on matching email with toast notification | Removes friction; user notified so they're aware the link happened |
| D-6 | Admin account creation | Database seed only (no UI flow) | Admin accounts are not self-service; seeded via EF Core migration |

---

## Functional Requirements

### FR-1: Email/Password Registration — Backend
- **Description**: Accept registration request; hash password; create user record; generate email verification token; send confirmation email asynchronously
- **Acceptance Criteria**:
  - `POST /api/auth/register` validates all fields via FluentValidation; returns 400 with error map on failure
  - Password hashed with ASP.NET Identity PBKDF2-SHA256, 10 000 iterations
  - User stored with: `Id` (UUID), `Email` (unique), `PasswordHash`, `FirstName`, `LastName`, `Phone?`, `Role=Customer`, `IsEmailConfirmed=false`, `CreatedAt`
  - Email verification token: UUID, SHA-256 hashed in `EmailConfirmationTokens` table, 24h expiry
  - Confirmation email sent via `IEmailService.SendConfirmationEmailAsync` — non-blocking; failure logged, request still returns 201
  - Duplicate email → 409 with message `"Adresa de email este deja folosită"`
  - Rate-limited: 5 requests per IP per hour
- **Priority**: Must
- **Related Stories**: US-102

### FR-2: Email Verification — Backend
- **Description**: Activate account when user clicks the confirmation link; support resend
- **Acceptance Criteria**:
  - `GET /api/auth/confirm-email?userId=&token=` marks `IsEmailConfirmed=true`, deletes token row; returns 200
  - Expired or invalid token → 400 `"Link invalid sau expirat"`
  - `POST /api/auth/resend-confirmation` — rate-limited 3/hour/email; silently no-ops if already confirmed
- **Priority**: Must
- **Related Stories**: US-103

### FR-3: JWT Login + Refresh Token — Backend
- **Description**: Authenticate user; issue short-lived JWT access token and long-lived rotating refresh token
- **Acceptance Criteria**:
  - `POST /api/auth/login` → returns `{accessToken, expiresIn}`; sets `refreshToken` in HttpOnly Secure `SameSite=Strict` cookie
  - Access token: JWT RS256, 15-min expiry, claims: `sub` (userId), `email`, `role`, `firstName`
  - Refresh token: opaque UUID, SHA-256 hashed in DB, 30-day sliding-window; old token revoked immediately on rotation
  - `POST /api/auth/refresh` — reads cookie, validates, issues new access + refresh pair
  - `POST /api/auth/logout` — revokes refresh token in DB, clears cookie; returns 200
  - 5 consecutive failed login attempts → account locked 15 min; 423 response; lockout email sent
  - Unverified email → login returns 403 `"Confirmați adresa de email pentru a continua"`
- **Priority**: Must
- **Related Stories**: US-105

### FR-4: Google OAuth — Backend
- **Description**: Validate Google `id_token` server-side; upsert user; issue platform JWT
- **Acceptance Criteria**:
  - `POST /api/auth/google {idToken}` — validates token with Google tokeninfo endpoint; verifies `aud == CLIENT_ID`
  - New user: created with `Role=Customer`, `IsEmailConfirmed=true`, no password
  - Existing user with same email and no Google link: accounts auto-linked; `ExternalLogins(UserId, Provider='Google', ProviderKey=googleSub)` row inserted; toast message `"Contul tău Google a fost conectat"` (returned as response flag)
  - Returns same `{accessToken, expiresIn}` + refresh cookie as password login
  - Google `id_token` never forwarded to client after validation
  - OAuth scope restricted to `email`, `profile` only
- **Priority**: Must
- **Related Stories**: US-107

### FR-5: Guest Session — Backend
- **Description**: Allow anonymous order placement via a guest token; support claiming orders after registration
- **Acceptance Criteria**:
  - `POST /api/auth/guest {firstName, lastName, email, phone}` — validates input; creates `GuestSession(Id=UUID, email, firstName, lastName, phone, CreatedAt, ExpiresAt=+7days)`; returns `{guestToken: UUID}`
  - All order/cart/upload endpoints accept either `Authorization: Bearer` or `X-Guest-Token` header
  - `POST /api/auth/guest/claim` — after guest registers/logs in, transfers guest orders to real account; invalidates guest token; returns 200
  - Background job cleans up `GuestSessions` with no linked orders after 7 days
- **Priority**: Must
- **Related Stories**: US-109

### FR-6: Password Reset — Backend
- **Description**: Allow users to reset forgotten passwords via a time-limited email link
- **Acceptance Criteria**:
  - `POST /api/auth/forgot-password {email}` — always returns 200 (prevents email enumeration); sends reset email if account exists
  - Reset token: UUID, SHA-256 hashed in DB, 1-hour expiry
  - `POST /api/auth/reset-password {userId, token, newPassword}` — validates token; updates password hash; revokes ALL active refresh tokens for that user; returns 200
  - Invalid/expired token → 400 `"Link invalid sau expirat"`
  - Same password strength rules as registration enforced by FluentValidation
- **Priority**: Must
- **Related Stories**: US-110

### FR-7: Registration — Frontend
- **Description**: Registration form with real-time validation and GDPR consent
- **Acceptance Criteria**:
  - Fields: First Name, Last Name, Email, Password, Confirm Password, Phone (optional)
  - Real-time password validation: min 8 chars, 1 uppercase, 1 digit, 1 special character; inline strength indicator
  - GDPR consent checkbox (mandatory) with link to Privacy Policy; submit disabled until checked
  - On success: navigate to email-verification-pending page (no auto-login)
  - Duplicate email: inline field error `"Adresa de email este deja folosită"` (mapped from 409 response)
  - All labels and error messages in Romanian
  - Spinner + disabled submit during in-flight request
- **Priority**: Must
- **Related Stories**: US-101

### FR-8: Login — Frontend
- **Description**: Login form with redirect-back support and forgot-password link
- **Acceptance Criteria**:
  - Fields: Email, Password; `"Ține-mă minte"` toggle (has no effect on cookie — always 30-day sliding); show/hide password icon
  - On success: redirect to originally requested URL or `/tipareste` (homepage)
  - On failure: `"Email sau parolă incorectă"` — no field-level leakage of which credential is wrong
  - `"Am uitat parola"` link → `/forgot-password`; `"Nu ai cont? Înregistrează-te"` link → `/register`
  - Spinner + disabled submit during in-flight request
- **Priority**: Must
- **Related Stories**: US-104

### FR-9: Google Social Login — Frontend
- **Description**: "Continuă cu Google" button on both login and register pages
- **Acceptance Criteria**:
  - Google Identity Services SDK renders button on login and register pages
  - On success callback: sends `idToken` to `POST /api/auth/google`; stores returned access token; navigates to homepage
  - If auto-link occurred: toast `"Contul tău Google a fost conectat"` shown
  - On Google error: toast `"Autentificarea Google a eșuat. Încearcă din nou"`
- **Priority**: Must
- **Related Stories**: US-106

### FR-10: Guest Checkout — Frontend
- **Description**: Prompt on checkout entry; guest info form; store guest session locally
- **Acceptance Criteria**:
  - On checkout start (unauthenticated): modal with three options — `"Continuă ca oaspete"` | `"Conectează-te"` | `"Creează cont"`
  - Guest form: First Name (required), Last Name (required), Email (required, valid format), Phone (required, Romanian format `07xxxxxxxx`)
  - After `POST /api/auth/guest` succeeds: `{guestToken, firstName, lastName, email, phone}` stored in `localStorage`; `X-Guest-Token` header added by `guestInterceptor` (already implemented in bolt 004)
  - After order placed: subtle nudge — `"La final îți vei putea crea un cont pentru a urmări comenzile"`
- **Priority**: Must
- **Related Stories**: US-108

---

## Non-Functional Requirements

### Security
| Requirement | Metric | Target |
|-------------|--------|--------|
| No email enumeration | `forgot-password` always returns 200 regardless of email existence | Pass/Fail |
| XSS-safe token storage | Refresh token in HttpOnly cookie, never in script-accessible storage | Pass/Fail |
| Account lockout | ≥ 5 consecutive failures lock account for 15 min | Pass/Fail |
| Token revocation on password reset | All refresh tokens for user invalidated after password reset | Pass/Fail |
| Google token never forwarded | `id_token` validated server-side only; never sent to client | Pass/Fail |
| OWASP A07 (Identification failures) | No credential stuffing via rate limiting (5 req/IP/hour on register; lockout on login) | Pass/Fail |

### Performance
| Requirement | Metric | Target |
|-------------|--------|--------|
| Login response time | p95 latency for `POST /api/auth/login` | < 300 ms |
| Token refresh | p95 latency for `POST /api/auth/refresh` | < 100 ms |
| Registration | p95 latency for `POST /api/auth/register` (excluding email send) | < 300 ms |

### Reliability
| Requirement | Metric | Target |
|-------------|--------|--------|
| Email send failure tolerance | Confirmation/reset emails fail silently; request still succeeds | Pass/Fail |
| Token cleanup | Expired tokens and orphaned guest sessions removed within 1h of expiry | Pass/Fail |

### Compliance
| Requirement | Metric | Target |
|-------------|--------|--------|
| GDPR consent | Registration cannot complete without explicit consent checkbox tick | Pass/Fail |
| Password strength | Minimum: 8 chars, 1 uppercase, 1 digit, 1 special character — enforced FE + BE | Pass/Fail |
| Password hashing | PBKDF2-SHA256, 10 000 iterations — no plain-text or MD5/SHA1 | Pass/Fail |
