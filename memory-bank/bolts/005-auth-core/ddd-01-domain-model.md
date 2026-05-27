---
stage: domain-model
bolt: 005-auth-core
created: 2026-05-20T13:12:00Z
---

# Static Domain Model: auth-core

## Entities

### User *(Aggregate Root)*
| Property | Type | Constraints / Business Rules |
|----------|------|------------------------------|
| `Id` | `Guid` | Generated on creation; never changes |
| `Email` | `string` | Normalized to lowercase; unique across system |
| `PasswordHash` | `string?` | Nullable — null for Google-only accounts; PBKDF2-SHA256 |
| `FirstName` | `string` | Required; max 100 chars |
| `LastName` | `string` | Required; max 100 chars |
| `Phone` | `string?` | Optional; Romanian mobile format `07[0-9]{8}` |
| `Role` | `UserRole` | Enum: `Customer` (default), `Admin` (seed only) |
| `IsEmailConfirmed` | `bool` | `false` on registration; `true` after email verification |
| `GdprConsentAccepted` | `bool` | Must be `true` — enforced at registration |
| `FailedLoginCount` | `int` | 0–5; incremented on wrong password; reset on success or password reset |
| `LockoutEnd` | `DateTimeOffset?` | Null unless locked; set to `UtcNow + 15 min` when `FailedLoginCount` reaches 5 |
| `CreatedAt` | `DateTimeOffset` | Set on creation; UTC |

**Invariants**:
- Email must be unique across all `User` records
- `PasswordHash` must be set OR an `ExternalLogin` record for the user must exist (cannot have neither)
- Login is rejected when `LockoutEnd > UtcNow`
- Login is rejected when `IsEmailConfirmed == false`
- `GdprConsentAccepted` must be `true` to create a user

---

### RefreshToken
| Property | Type | Constraints |
|----------|------|-------------|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → User; indexed |
| `TokenHash` | `string` | SHA-256 of raw UUID token; indexed (unique lookups) |
| `ExpiresAt` | `DateTimeOffset` | `UtcNow + 30 days`; sliding — reset on each rotation |
| `CreatedAt` | `DateTimeOffset` | UTC |
| `RevokedAt` | `DateTimeOffset?` | Null = active; set on logout, rotation, or password reset |

**Invariants**:
- `RevokedAt == null && ExpiresAt > UtcNow` → token is valid
- Raw token is only ever returned to the client once; only the hash is stored
- On rotation: old token `RevokedAt` set; new token inserted atomically

---

### EmailConfirmationToken
| Property | Type | Constraints |
|----------|------|-------------|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → User; unique (one active token per user) |
| `TokenHash` | `string` | SHA-256 of raw UUID token |
| `ExpiresAt` | `DateTimeOffset` | `UtcNow + 24 hours` |

**Invariants**:
- Only one active `EmailConfirmationToken` per user (old row replaced on resend)
- Token is deleted upon successful use (one-time)

---

### PasswordResetToken
| Property | Type | Constraints |
|----------|------|-------------|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → User; unique (one active token per user) |
| `TokenHash` | `string` | SHA-256 of raw UUID token |
| `ExpiresAt` | `DateTimeOffset` | `UtcNow + 1 hour` |

**Invariants**:
- Only one active `PasswordResetToken` per user (old row replaced on new forgot-password request)
- Token is deleted upon successful use (one-time)
- Successful reset also revokes ALL `RefreshToken` rows for the user

---

## Value Objects

### `NormalizedEmail`
- **Properties**: `string Value`
- **Constraints**: Valid RFC 5322 format; stored and compared as lowercase
- **Equality**: By `Value` (case-insensitive)

### `PasswordStrength`
- **Properties**: (not stored — validated at input boundary)
- **Constraints**: min 8 characters, ≥1 uppercase letter, ≥1 digit, ≥1 special character (`!@#$%^&*`)
- **Used in**: Registration and password reset FluentValidation rules

### `PhoneNumber`
- **Properties**: `string Value`
- **Constraints**: Optional; Romanian mobile format `07[0-9]{8}`
- **Used in**: User.Phone

### `TokenHash`
- **Properties**: `string Value` (SHA-256 hex string, 64 chars)
- **Behaviour**: Computed via `SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))` → hex string
- **Used in**: RefreshToken.TokenHash, EmailConfirmationToken.TokenHash, PasswordResetToken.TokenHash

---

## Aggregates

### User Aggregate
- **Root**: `User`
- **Members**: `RefreshToken[]`, `EmailConfirmationToken?`, `PasswordResetToken?`
- **Boundary**: The `User` root controls all token lifecycle operations

**Invariants enforced by aggregate**:
1. Cannot register with a duplicate email
2. Cannot login when `LockoutEnd > UtcNow`
3. Cannot login when `IsEmailConfirmed == false`
4. `FailedLoginCount` never exceeds 5 (lockout applied at threshold)
5. On password reset: all `RefreshToken.RevokedAt` set, `FailedLoginCount` reset, `LockoutEnd` cleared
6. `GdprConsentAccepted` must be `true` — immutable after creation

---

## Domain Events

| Event | Trigger | Payload |
|-------|---------|---------|
| `UserRegistered` | New `User` saved | `UserId`, `Email`, `rawConfirmationToken` |
| `EmailConfirmed` | `IsEmailConfirmed` set to `true` | `UserId` |
| `LoginSucceeded` | Password verified, not locked, email confirmed | `UserId` |
| `LoginFailed` | Wrong password | `UserId`, `FailedLoginCount` |
| `AccountLocked` | `FailedLoginCount` reaches 5 | `UserId`, `LockoutEnd` |
| `RefreshTokenRotated` | Token refreshed successfully | `UserId`, `OldTokenId`, `NewTokenId` |
| `TokenFamilyCompromise` | Unknown token hash presented at refresh | `UserId` |
| `PasswordResetRequested` | forgot-password for existing email | `UserId`, `rawResetToken` |
| `PasswordReset` | reset-password succeeds | `UserId` |
| `UserLoggedOut` | logout endpoint called | `UserId`, `TokenId` |

> **Note**: Events are raised in the service layer and consumed by `IEmailService` for email dispatch. No event bus is needed in this bolt — all consumers are in-process.

---

## Domain Services

### `ITokenService`
Responsible for JWT access token generation and refresh token lifecycle.

| Method | Inputs | Output | Notes |
|--------|--------|--------|-------|
| `GenerateAccessToken(user)` | `User` | `(string jwt, int expiresIn)` | RS256, 15-min, claims: sub/email/role/firstName |
| `GenerateRefreshToken()` | — | `(string rawToken, string hash)` | UUID; hash is SHA-256 hex |
| `GetRefreshTokenFromCookieAsync(httpContext)` | `HttpContext` | `string? rawToken` | Reads `refreshToken` cookie |
| `SetRefreshCookie(response, rawToken, expiresAt)` | `HttpResponse`, `string`, `DateTimeOffset` | void | HttpOnly Secure SameSite=Strict |
| `ClearRefreshCookie(response)` | `HttpResponse` | void | MaxAge=0 |

### `IPasswordHasher` *(from ASP.NET Identity)*
Delegates to `IPasswordHasher<User>` — PBKDF2-SHA256, 10 000 iterations.

| Method | Inputs | Output |
|--------|--------|--------|
| `HashPassword(user, password)` | `User`, `string` | `string hash` |
| `VerifyHashedPassword(user, hash, password)` | `User`, `string`, `string` | `PasswordVerificationResult` |

### `IAccountLockoutService`
Encapsulates the lockout state machine.

| Method | Inputs | Output |
|--------|--------|--------|
| `IsLockedAsync(user)` | `User` | `bool` |
| `RecordFailedLoginAsync(user)` | `User` | `bool wasJustLocked` |
| `ClearLockoutAsync(user)` | `User` | `void` |

### `IEmailTokenService`
Generates one-time tokens for email confirmation and password reset.

| Method | Inputs | Output |
|--------|--------|--------|
| `GenerateEmailConfirmationToken(userId)` | `Guid` | `(string raw, EmailConfirmationToken entity)` |
| `ValidateEmailConfirmationToken(userId, rawToken)` | `Guid`, `string` | `EmailConfirmationToken?` |
| `GeneratePasswordResetToken(userId)` | `Guid` | `(string raw, PasswordResetToken entity)` |
| `ValidatePasswordResetToken(userId, rawToken)` | `Guid`, `string` | `PasswordResetToken?` |

---

## Repository Interfaces

### `IUserRepository`
| Method | Returns |
|--------|---------|
| `GetByIdAsync(Guid id)` | `User?` |
| `GetByEmailAsync(string email)` | `User?` |
| `AddAsync(User user)` | `Task` |
| `SaveChangesAsync()` | `Task` |

### `IRefreshTokenRepository`
| Method | Returns |
|--------|---------|
| `GetByHashAsync(string hash)` | `RefreshToken?` |
| `AddAsync(RefreshToken token)` | `Task` |
| `RevokeAsync(RefreshToken token)` | `Task` (sets RevokedAt) |
| `RevokeAllForUserAsync(Guid userId)` | `Task` (bulk revoke for password reset) |
| `SaveChangesAsync()` | `Task` |

### `IEmailConfirmationTokenRepository`
| Method | Returns |
|--------|---------|
| `GetByUserIdAsync(Guid userId)` | `EmailConfirmationToken?` |
| `GetByHashAsync(Guid userId, string hash)` | `EmailConfirmationToken?` |
| `AddAsync(EmailConfirmationToken token)` | `Task` |
| `DeleteAsync(EmailConfirmationToken token)` | `Task` |
| `SaveChangesAsync()` | `Task` |

### `IPasswordResetTokenRepository`
| Method | Returns |
|--------|---------|
| `GetByUserIdAsync(Guid userId)` | `PasswordResetToken?` |
| `GetByHashAsync(Guid userId, string hash)` | `PasswordResetToken?` |
| `AddAsync(PasswordResetToken token)` | `Task` |
| `DeleteAsync(PasswordResetToken token)` | `Task` |
| `SaveChangesAsync()` | `Task` |

---

## Story Coverage

| Story | Domain Concept Covered |
|-------|------------------------|
| 001-user-registration | User entity, EmailConfirmationToken, UserRegistered event, IEmailTokenService |
| 002-email-verification | EmailConfirmationToken validation, EmailConfirmed event, resend with rate-limit |
| 003-jwt-login | ITokenService.GenerateAccessToken, RefreshToken, LoginSucceeded/LoginFailed events |
| 004-refresh-token | RefreshToken rotation, TokenFamilyCompromise event, ITokenService.SetRefreshCookie |
| 005-logout | RefreshToken.RevokedAt, UserLoggedOut event, ITokenService.ClearRefreshCookie |
| 006-account-lockout | User.LockoutEnd, IAccountLockoutService, AccountLocked event |
| 007-password-reset | PasswordResetToken, IRefreshTokenRepository.RevokeAllForUserAsync, PasswordReset event |

---

## Ubiquitous Language

| Term | Definition |
|------|-----------|
| **Access Token** | Short-lived JWT RS256 (15 min) sent as `Authorization: Bearer` header; contains user identity claims |
| **Refresh Token** | Long-lived opaque UUID (30-day sliding window); stored only as SHA-256 hash in DB; travels only in HttpOnly cookie |
| **Email Confirmation Token** | One-time UUID (24h); sent in email link to verify account ownership |
| **Password Reset Token** | One-time UUID (1h); sent in email link to authorize a password change |
| **Token Rotation** | On each refresh: old RefreshToken revoked, new one issued — sliding window resets |
| **Token Family Compromise** | If an already-revoked (or unknown) refresh token hash is presented, ALL tokens for that user are revoked (prevents stolen-token replay) |
| **Account Lockout** | 15-minute block applied when `FailedLoginCount` reaches 5; automatically expires |
| **Normalized Email** | Email address stored and compared as lowercase; prevents duplicate-by-case accounts |
| **GDPR Consent** | Explicit opt-in checkbox at registration; stored as `GdprConsentAccepted=true` on User |
| **Guest User** | Not in scope for this unit — handled by unit `003-guest-sessions` |
