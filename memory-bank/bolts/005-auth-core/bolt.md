---
id: 005-auth-core
unit: 001-auth-core
intent: 002-authentication
type: ddd-construction-bolt
status: complete
started: 2026-05-20T13:10:00Z
completed: 2026-05-21T00:00:00Z
current_stage: done
stages_completed: [domain-model, technical-design, implement, test]
stories:
  - 001-user-registration
  - 002-email-verification
  - 003-jwt-login
  - 004-refresh-token
  - 005-logout
  - 006-account-lockout
  - 007-password-reset
created: 2026-05-20T13:00:00Z

requires_bolts: []
enables_bolts: [006-social-auth, 007-guest-sessions, 008-authentication-ui]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 2
---

## Bolt: 005-auth-core

### Objective

Build the core authentication engine: User entity with EF Core migration, secure password hashing, email confirmation tokens, JWT RS256 issuance, 30-day sliding refresh tokens, account lockout, and password reset. This bolt is the foundation all other authentication bolts depend on.

### Stories Included

- [ ] **001-user-registration**: POST /api/auth/register — persist user, hash password, send verification email - Priority: Must
- [ ] **002-email-verification**: GET /api/auth/confirm-email + POST /api/auth/resend-confirmation - Priority: Must
- [ ] **003-jwt-login**: POST /api/auth/login — JWT RS256 + HttpOnly refresh cookie - Priority: Must
- [ ] **004-refresh-token**: POST /api/auth/refresh — sliding-window rotation, token family compromise detection - Priority: Must
- [ ] **005-logout**: POST /api/auth/logout — revoke token, clear cookie - Priority: Must
- [ ] **006-account-lockout**: 5 failures → 15-min lockout + lockout email - Priority: Must
- [ ] **007-password-reset**: POST /api/auth/forgot-password + POST /api/auth/reset-password - Priority: Must

### Expected Outputs

- EF Core migration: `Users`, `RefreshTokens`, `EmailConfirmationTokens`, `PasswordResetTokens` tables
- `ITokenService` / `IAuthService` with JWT issuance logic (reused by bolt 006)
- 7 API endpoints: `/register`, `/confirm-email`, `/resend-confirmation`, `/login`, `/refresh`, `/logout`, `/forgot-password`, `/reset-password`
- Unit tests (Moq/xUnit): service layer, token generation, lockout logic
- Integration tests (WebApplicationFactory): all 7 endpoints happy + error paths

### Dependencies

#### Bolt Dependencies (within intent)
- None — first bolt in this intent

#### Unit Dependencies (cross-unit)
- **Bolt 001** (error-handling-logging): Required — ExceptionHandlerMiddleware, ProblemDetails, Serilog
- **Bolt 002** (security-baselines): Required — rate limiter, security headers, CORS
- **Bolt 003** (email-infrastructure): Required — IEmailService for confirmation/reset/lockout emails
