---
unit: 001-auth-core
intent: 002-authentication
unit_type: backend
default_bolt_type: ddd-construction-bolt
phase: inception
status: ready
created: 2026-05-20T12:54:00Z
updated: 2026-05-20T12:54:00Z
---

# Unit Brief: auth-core

## Purpose

Core authentication engine for FotoTipar. Manages user identity lifecycle: registration with secure password hashing, email address verification, JWT RS256 issuance with sliding-window refresh tokens, session logout, progressive account lockout, and password reset via email link.

## Scope

### In Scope
- User entity creation and persistence
- Password hashing (PBKDF2-SHA256, 10 000 iterations via ASP.NET Identity)
- Email confirmation token generation, storage (hashed), and validation
- JWT RS256 access token issuance (15-min)
- Refresh token lifecycle: issue, rotate (sliding 30-day), revoke on logout
- Account lockout after 5 consecutive failures (15-min cooldown)
- Password reset token generation, storage (hashed, 1h), and validation
- Revocation of all refresh tokens on password reset

### Out of Scope
- Google OAuth (→ `002-social-auth`)
- Guest sessions (→ `003-guest-sessions`)
- Frontend pages (→ `004-authentication-ui`)

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-1 | Email/password registration — persist user, generate email token, fire async email | Must |
| FR-2 | Email verification — confirm token, resend with rate limit | Must |
| FR-3 | JWT login + refresh token — login, refresh, logout, account lockout | Must |
| FR-6 | Password reset — forgot-password (anti-enum), reset with token, revoke all refresh tokens | Must |

---

## Domain Concepts

### Key Entities
| Entity | Description | Key Attributes |
|--------|-------------|----------------|
| `User` | Registered account | Id (UUID), Email (unique), PasswordHash, FirstName, LastName, Phone?, Role, IsEmailConfirmed, FailedLoginCount, LockoutEnd?, CreatedAt |
| `RefreshToken` | Sliding-window session token | Id, UserId, TokenHash (SHA-256), ExpiresAt, CreatedAt, RevokedAt? |
| `EmailConfirmationToken` | One-time email verification token | Id, UserId, TokenHash, ExpiresAt |
| `PasswordResetToken` | One-time password reset token | Id, UserId, TokenHash, ExpiresAt |

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| Register | Create user, send verification | RegisterDto | 201 {userId} / 409 duplicate |
| ConfirmEmail | Verify token, activate account | userId, token | 200 / 400 invalid |
| Login | Authenticate, issue JWT + cookie | LoginDto | {accessToken, expiresIn} + cookie |
| Refresh | Rotate refresh token | HttpOnly cookie | {accessToken, expiresIn} + new cookie |
| Logout | Revoke refresh token | HttpOnly cookie | 200 |
| ForgotPassword | Send reset email (anti-enum) | {email} | 200 (always) |
| ResetPassword | Validate token, update hash, revoke sessions | {userId, token, newPassword} | 200 / 400 |

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
| 001 | user-registration | Must |
| 002 | email-verification | Must |
| 003 | jwt-login | Must |
| 004 | refresh-token | Must |
| 005 | logout | Must |
| 006 | account-lockout | Must |
| 007 | password-reset | Must |

---

## Technical Constraints

- Use `IRSAKeyProvider` (or `IOptions<JwtSettings>`) to load RS256 key pair from config — no hardcoded secrets
- `IEmailService` from bolt 003 for all email sends
- Rate limiting from bolt 002 middleware applies to all endpoints
- All tokens stored as SHA-256 hashes — raw token only ever returned once to client
- EF Core migration must include indexes on `Email` (unique), `RefreshToken.TokenHash`, `EmailConfirmationToken.TokenHash`
