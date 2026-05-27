---
stage: technical-design
bolt: 005-auth-core
created: 2026-05-20T13:20:00Z
---

# Technical Design: auth-core

## Architecture Pattern

**Pattern**: Layered Architecture — Controller → Service → Repository → EF Core + PostgreSQL

Consistent with the existing `PhotoPrint.API` structure. The auth domain fits naturally into thin controllers delegating to stateless services. No CQRS or event sourcing needed — auth operations are synchronous request/response with no complex read-model requirements.

```text
┌──────────────────────────────────────────────┐
│  Presentation Layer (AuthController)          │  ← HTTP request/response, DTO mapping
├──────────────────────────────────────────────┤
│  Application Layer (AuthService, TokenService)│  ← Business logic, orchestration
├──────────────────────────────────────────────┤
│  Domain Layer (Entities, Interfaces)         │  ← Business rules, invariants
├──────────────────────────────────────────────┤
│  Infrastructure Layer (Repos, DbContext)     │  ← EF Core, PostgreSQL
└──────────────────────────────────────────────┘
         ↑ IEmailService (bolt 003) called from Application layer
         ↑ Rate limiter + exception middleware (bolts 002, 001) wrap Presentation
```

---

## File Structure

All new files follow the existing `PhotoPrint.API/` type-based organization:

```text
src/PhotoPrint.API/
├── Controllers/
│   └── AuthController.cs                      ← 8 endpoints
├── Services/
│   ├── IAuthService.cs + AuthService.cs       ← register, login, verify, reset
│   ├── ITokenService.cs + TokenService.cs     ← JWT + refresh token lifecycle
│   └── IEmailTokenService.cs + EmailTokenService.cs  ← one-time email tokens
├── Models/
│   ├── User.cs
│   ├── RefreshToken.cs
│   ├── EmailConfirmationToken.cs
│   └── PasswordResetToken.cs
├── DTOs/
│   └── Auth/
│       ├── RegisterRequest.cs
│       ├── LoginRequest.cs
│       ├── LoginResponse.cs
│       ├── ForgotPasswordRequest.cs
│       ├── ResetPasswordRequest.cs
│       └── ResendConfirmationRequest.cs
├── Validators/
│   └── Auth/
│       ├── RegisterRequestValidator.cs
│       ├── LoginRequestValidator.cs
│       ├── ResetPasswordRequestValidator.cs
│       └── ResendConfirmationRequestValidator.cs
├── Configuration/
│   └── JwtSettings.cs
├── Extensions/
│   └── AuthServiceExtensions.cs               ← AddAuthCore() extension method
└── Migrations/
    └── {timestamp}_AddAuthTables.cs           ← EF Core migration (auto-generated)
```

Test files:
```text
src/PhotoPrint.Tests/
├── Unit/
│   └── Services/
│       ├── AuthServiceTests.cs
│       └── TokenServiceTests.cs
└── Integration/
    └── Auth/
        └── AuthEndpointsTests.cs
```

---

## API Contracts

All endpoints under `[Route("api/auth")]` on `AuthController : ControllerBase`.

---

### POST `/api/auth/register`

**Purpose**: Create new user account, trigger email verification.
**Rate limit policy**: `"register"` — 5 requests/IP/hour (bolt 002)

**Request**:
```json
{
  "firstName": "Ion",
  "lastName": "Popescu",
  "email": "ion@example.com",
  "password": "Parola1!",
  "confirmPassword": "Parola1!",
  "phone": "0712345678",
  "gdprConsentAccepted": true
}
```

**Responses**:
| Status | Body | Condition |
|--------|------|-----------|
| 201 | `{ "userId": "uuid" }` | Success |
| 400 | ProblemDetails (field errors) | FluentValidation failure |
| 409 | ProblemDetails `"Adresa de email este deja folosită"` | Duplicate email |
| 429 | ProblemDetails | Rate limit exceeded |

**FluentValidation rules** (`RegisterRequestValidator`):
- `firstName`, `lastName`: Required, max 100 chars
- `email`: Required, valid email format
- `password`: Required, min 8 chars, `[A-Z]`, `[0-9]`, `[!@#$%^&*]`
- `confirmPassword`: Must equal `password`
- `phone`: Optional; `^07[0-9]{8}$` if provided
- `gdprConsentAccepted`: Must be `true`

---

### GET `/api/auth/confirm-email`

**Purpose**: Verify email address via one-time token link.

**Query params**: `userId` (Guid), `token` (string)

**Responses**:
| Status | Body | Condition |
|--------|------|-----------|
| 200 | (empty) | Success — account activated |
| 400 | ProblemDetails `"Link invalid sau expirat"` | Token invalid, expired, or already used |

> Anti-enumeration: non-existent `userId` returns same 400 as bad token.

---

### POST `/api/auth/resend-confirmation`

**Purpose**: Resend email verification link.
**Rate limit policy**: `"resend-confirmation"` — 3 requests/email/hour

**Request**:
```json
{ "email": "ion@example.com" }
```

**Responses**:
| Status | Body | Condition |
|--------|------|-----------|
| 200 | (empty) | Sent (or already confirmed — silent no-op) |
| 400 | ProblemDetails (field error) | Invalid email format |
| 429 | ProblemDetails | Rate limit exceeded |

---

### POST `/api/auth/login`

**Purpose**: Authenticate user; issue JWT access token + HttpOnly refresh cookie.
**Rate limit policy**: `"login"` — 10 requests/IP/minute

**Request**:
```json
{
  "email": "ion@example.com",
  "password": "Parola1!"
}
```

**Responses**:
| Status | Body | Condition |
|--------|------|-----------|
| 200 | `{ "accessToken": "jwt...", "expiresIn": 900 }` + `Set-Cookie: refreshToken=...` | Success |
| 401 | ProblemDetails `"Email sau parolă incorectă"` | Bad credentials |
| 403 | ProblemDetails `"Confirmați adresa de email pentru a continua"` | Email not confirmed |
| 423 | ProblemDetails `{ retryAfterSeconds: N }` | Account locked |
| 429 | ProblemDetails | Rate limit |

**Cookie spec** (`Set-Cookie`):
```
refreshToken={rawToken}; HttpOnly; Secure; SameSite=Strict; Path=/api/auth; Max-Age=2592000
```

---

### POST `/api/auth/refresh`

**Purpose**: Rotate refresh token; issue new access token.

**Input**: HttpOnly cookie `refreshToken` (no request body)

**Responses**:
| Status | Body | Condition |
|--------|------|-----------|
| 200 | `{ "accessToken": "jwt...", "expiresIn": 900 }` + new `Set-Cookie` | Success |
| 401 | ProblemDetails + `Set-Cookie: refreshToken=; Max-Age=0` | Missing, expired, or revoked token |

> On token family compromise (hash not found), ALL refresh tokens for that user are revoked before returning 401.

---

### POST `/api/auth/logout`

**Purpose**: Revoke current refresh token and clear cookie.
**Auth**: Refresh cookie only (no JWT required — supports expired access tokens)

**Input**: HttpOnly cookie `refreshToken` (no request body)

**Responses**:
| Status | Body | Condition |
|--------|------|-----------|
| 200 | (empty) + `Set-Cookie: refreshToken=; Max-Age=0` | Always (idempotent) |

---

### POST `/api/auth/forgot-password`

**Purpose**: Trigger password reset email.
**Rate limit policy**: `"forgot-password"` — 3 requests/IP/hour

**Request**:
```json
{ "email": "ion@example.com" }
```

**Responses**:
| Status | Body | Condition |
|--------|------|-----------|
| 200 | (empty) | Always (anti-enumeration) |

---

### POST `/api/auth/reset-password`

**Purpose**: Set new password using one-time reset token.

**Request**:
```json
{
  "userId": "uuid",
  "token": "raw-token-string",
  "newPassword": "NewaParola1!"
}
```

**Responses**:
| Status | Body | Condition |
|--------|------|-----------|
| 200 | (empty) | Success |
| 400 | ProblemDetails `"Link invalid sau expirat"` | Token invalid/expired |
| 400 | ProblemDetails (field errors) | Password validation failure |

---

## Data Persistence

### EF Core Configuration

- **DbContext**: `AppDbContext` (existing, to be extended with new DbSets)
- **Provider**: `Npgsql.EntityFrameworkCore.PostgreSQL`
- **Migration name**: `AddAuthTables`

### Table: `Users`

```sql
CREATE TABLE "Users" (
    "Id"                    UUID            NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "Email"                 VARCHAR(256)    NOT NULL,
    "PasswordHash"          TEXT            NULL,
    "FirstName"             VARCHAR(100)    NOT NULL,
    "LastName"              VARCHAR(100)    NOT NULL,
    "Phone"                 VARCHAR(20)     NULL,
    "Role"                  VARCHAR(20)     NOT NULL DEFAULT 'Customer',
    "IsEmailConfirmed"      BOOLEAN         NOT NULL DEFAULT FALSE,
    "GdprConsentAccepted"   BOOLEAN         NOT NULL DEFAULT FALSE,
    "FailedLoginCount"      INTEGER         NOT NULL DEFAULT 0,
    "LockoutEnd"            TIMESTAMPTZ     NULL,
    "CreatedAt"             TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" (LOWER("Email"));
```

### Table: `RefreshTokens`

```sql
CREATE TABLE "RefreshTokens" (
    "Id"          UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "UserId"      UUID        NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "TokenHash"   VARCHAR(64) NOT NULL,
    "ExpiresAt"   TIMESTAMPTZ NOT NULL,
    "CreatedAt"   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "RevokedAt"   TIMESTAMPTZ NULL
);

CREATE INDEX "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");
CREATE UNIQUE INDEX "IX_RefreshTokens_TokenHash" ON "RefreshTokens" ("TokenHash");
```

### Table: `EmailConfirmationTokens`

```sql
CREATE TABLE "EmailConfirmationTokens" (
    "Id"         UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "UserId"     UUID        NOT NULL UNIQUE REFERENCES "Users"("Id") ON DELETE CASCADE,
    "TokenHash"  VARCHAR(64) NOT NULL,
    "ExpiresAt"  TIMESTAMPTZ NOT NULL
);

CREATE UNIQUE INDEX "IX_EmailConfirmationTokens_UserId" ON "EmailConfirmationTokens" ("UserId");
```

### Table: `PasswordResetTokens`

```sql
CREATE TABLE "PasswordResetTokens" (
    "Id"         UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "UserId"     UUID        NOT NULL UNIQUE REFERENCES "Users"("Id") ON DELETE CASCADE,
    "TokenHash"  VARCHAR(64) NOT NULL,
    "ExpiresAt"  TIMESTAMPTZ NOT NULL
);

CREATE UNIQUE INDEX "IX_PasswordResetTokens_UserId" ON "PasswordResetTokens" ("UserId");
```

### EF Core Entity Configuration (Fluent API)

```csharp
// User
builder.HasIndex(u => u.Email).IsUnique();
builder.Property(u => u.Email).HasMaxLength(256);
builder.Property(u => u.Role).HasConversion<string>();

// RefreshToken
builder.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
builder.HasIndex(t => t.TokenHash).IsUnique();
builder.HasIndex(t => t.UserId);

// EmailConfirmationToken / PasswordResetToken
builder.HasOne<User>().WithOne().HasForeignKey<EmailConfirmationToken>(t => t.UserId);
```

---

## Security Design

### JWT RS256 Key Management
- **Key storage**: RSA private key PEM loaded from `appsettings.json` key `JwtSettings:PrivateKeyPem`; **never committed to source control** — set via environment variable or secrets management in production
- **Public key**: Derived from private key at startup; used for token validation
- **Key rotation**: Not in scope for v1 — single key pair; future: JWKS endpoint

### Configuration (`JwtSettings.cs`)
```csharp
public class JwtSettings
{
    public string PrivateKeyPem { get; init; } = "";
    public string Issuer { get; init; } = "fototipar";
    public string Audience { get; init; } = "fototipar-spa";
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 30;
}
```

### Refresh Token Cookie
```csharp
new CookieOptions
{
    HttpOnly = true,
    Secure = true,
    SameSite = SameSiteMode.Strict,
    Path = "/api/auth",
    MaxAge = TimeSpan.FromDays(30),
    Expires = refreshToken.ExpiresAt
}
```

### Anti-Enumeration Patterns
| Endpoint | Behaviour |
|----------|-----------|
| `forgot-password` | Always returns 200; sends email only if account exists |
| `confirm-email` | Invalid `userId` returns same 400 as bad token |
| `resend-confirmation` | Returns 200 silently if email not found or already confirmed |
| `login` | Identical 401 response for wrong email and wrong password |

### Token Hashing
```csharp
// Generate: raw UUID → store hash
var raw = Guid.NewGuid().ToString("N");
var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

// Validate: re-hash incoming raw token → compare with stored hash
var incomingHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawFromRequest)));
// constant-time comparison via CryptographicOperations.FixedTimeEquals
```

---

## Service Layer Design

### `AuthService` — main orchestrator
```csharp
public interface IAuthService
{
    Task<Guid> RegisterAsync(RegisterRequest dto);
    Task ConfirmEmailAsync(Guid userId, string rawToken);
    Task ResendConfirmationAsync(string email);
    Task<LoginResponse> LoginAsync(LoginRequest dto, HttpResponse httpResponse);
    Task<LoginResponse> RefreshAsync(string rawRefreshToken, HttpResponse httpResponse);
    Task LogoutAsync(string rawRefreshToken, HttpResponse httpResponse);
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(ResetPasswordRequest dto);
}
```

### `TokenService` — JWT + cookie
```csharp
public interface ITokenService
{
    (string jwt, int expiresIn) GenerateAccessToken(User user);
    (string rawToken, string tokenHash, DateTimeOffset expiresAt) GenerateRefreshToken();
    void SetRefreshCookie(HttpResponse response, string rawToken, DateTimeOffset expiresAt);
    void ClearRefreshCookie(HttpResponse response);
}
```

### `EmailTokenService` — one-time tokens
```csharp
public interface IEmailTokenService
{
    (string rawToken, EmailConfirmationToken entity) CreateEmailConfirmationToken(Guid userId);
    (string rawToken, PasswordResetToken entity) CreatePasswordResetToken(Guid userId);
    bool ValidateToken(string rawToken, string storedHash, DateTimeOffset expiresAt);
}
```

### Error handling conventions (via bolt 001 `ExceptionHandlerMiddleware`)
- `NotFoundException` → 404
- `ConflictException` → 409 (duplicate email)
- `UnauthorizedException` → 401
- `ForbiddenException` → 403 (unverified email, insufficient role)
- `ValidationException` → 422 (FluentValidation via filter)
- HTTP 423 (locked): returned directly from controller action with `StatusCode(423, problemDetails)`

---

## NFR Implementation

### Performance
| Concern | Approach |
|---------|----------|
| Email sending non-blocking | `IEmailService` called without `await` (fire-and-forget wrapped in `Task.Run` with error logging) |
| Token hash lookup | Unique index on `RefreshTokens.TokenHash` — O(log n) lookup |
| Email uniqueness | Unique index on `Users.Email` — DB-enforced, no pre-check needed |
| Lockout check | Checked BEFORE password hash comparison — avoids expensive PBKDF2 on locked accounts |

### Reliability
| Concern | Approach |
|---------|----------|
| Email failure non-fatal | All email calls wrapped in `try/catch`; exception logged at `Warning`; operation proceeds |
| Atomic token rotation | EF Core transaction: revoke old + insert new in single `SaveChangesAsync` |
| Idempotent logout | Always returns 200; token not found → no-op |

---

## Integration Points

### IEmailService (bolt 003)
```csharp
// Used in:
await _emailService.SendConfirmationEmailAsync(user.Email, user.FirstName, confirmationLink);
await _emailService.SendPasswordResetEmailAsync(user.Email, user.FirstName, resetLink);
await _emailService.SendAccountLockedEmailAsync(user.Email, user.FirstName, lockoutMinutes);
```
All calls: non-blocking. Failure logged but not thrown.

### Rate Limiter (bolt 002)
New named policies registered in `AddAuthCore()`:

```csharp
options.AddFixedWindowLimiter("register",    o => { o.PermitLimit = 5;  o.Window = TimeSpan.FromHours(1); });
options.AddFixedWindowLimiter("login",       o => { o.PermitLimit = 10; o.Window = TimeSpan.FromMinutes(1); });
options.AddFixedWindowLimiter("resend-confirmation", o => { o.PermitLimit = 3; o.Window = TimeSpan.FromHours(1); });
options.AddFixedWindowLimiter("forgot-password",     o => { o.PermitLimit = 3; o.Window = TimeSpan.FromHours(1); });
```
Applied via `[EnableRateLimiting("policy-name")]` attribute on each action.

### ExceptionHandlerMiddleware (bolt 001)
No changes required. Existing handler already maps all custom exception types to ProblemDetails. HTTP 423 (Locked) is the only non-standard status code; it will be returned directly from the controller.

### JWT Authentication Middleware
```csharp
// In AddAuthCore() / Program.cs
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new RsaSecurityKey(rsaPublicKey),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero  // strict 15-min expiry
        };
    });

builder.Services.AddAuthorization();
```

---

## Service Registration (`AddAuthCore`)

New extension method to keep `Program.cs` clean:

```csharp
// Extensions/AuthServiceExtensions.cs
public static IServiceCollection AddAuthCore(this IServiceCollection services, IConfiguration configuration)
{
    services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<ITokenService, TokenService>();
    services.AddScoped<IEmailTokenService, EmailTokenService>();
    // Repository registrations
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    services.AddScoped<IEmailConfirmationTokenRepository, EmailConfirmationTokenRepository>();
    services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
    // ASP.NET Identity password hasher (no full Identity stack — just the hasher)
    services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
    // JWT Bearer auth
    services.AddJwtAuthentication(configuration);
    return services;
}
```

---

## Test Strategy

### Unit Tests (`PhotoPrint.Tests/Unit/Services/`)

| Test Class | What's Tested |
|------------|--------------|
| `AuthServiceTests` | Registration flow, email confirmation, lockout state machine, password reset |
| `TokenServiceTests` | JWT claims, cookie options, token hash generation, rotation logic |

**Mocks**: `IUserRepository`, `IRefreshTokenRepository`, `IEmailTokenService`, `IEmailService` — all via Moq.

### Integration Tests (`PhotoPrint.Tests/Integration/Auth/`)

Using `WebApplicationFactory<Program>` with in-memory test DB or real PostgreSQL (test container).

| Test | Coverage |
|------|----------|
| `Register_ValidData_Returns201` | Happy path |
| `Register_DuplicateEmail_Returns409` | Conflict |
| `Register_WeakPassword_Returns400` | Validation |
| `ConfirmEmail_ValidToken_Returns200` | Happy path |
| `ConfirmEmail_ExpiredToken_Returns400` | Expiry |
| `Login_ValidCredentials_Returns200WithCookie` | Happy path + cookie |
| `Login_UnconfirmedEmail_Returns403` | Guard |
| `Login_LockedAccount_Returns423` | Lockout |
| `Refresh_ValidCookie_RotatesToken` | Rotation |
| `Refresh_ReplayedToken_RevokesAllTokens` | Family compromise |
| `Logout_ValidCookie_Returns200` | Happy path |
| `ForgotPassword_AnyEmail_Returns200` | Anti-enumeration |
| `ResetPassword_ValidToken_Returns200` | Happy path |
| `ResetPassword_ExpiredToken_Returns400` | Expiry |
