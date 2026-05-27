---
id: 006-social-auth
unit: 002-social-auth
intent: 002-authentication
type: ddd-construction-bolt
status: complete
started: 2026-05-20T14:00:00Z
completed: 2026-05-20T15:00:00Z
current_stage: done
stages_completed: [domain-model, technical-design, implement, test]
stories:
  - 001-google-token-validation
  - 002-account-upsert-linking
created: 2026-05-20T13:00:00Z

requires_bolts: [005-auth-core]
enables_bolts: [008-authentication-ui]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 2
  max_dependencies: 3
  testing_scope: 2
---

## Bolt: 006-social-auth

### Objective

Implement Google OAuth server-side: validate `id_token` against Google's tokeninfo endpoint, upsert user (create new or link to existing email+password account), store `ExternalLogin` record, and issue the same platform JWT + refresh cookie as password login.

### Stories Included

- [ ] **001-google-token-validation**: POST /api/auth/google — validate id_token with Google, verify aud - Priority: Must
- [ ] **002-account-upsert-linking**: Create/link user, store ExternalLogin, issue JWT + cookie - Priority: Must

### Expected Outputs

- `ExternalLogin` entity + EF Core migration
- `POST /api/auth/google` endpoint
- `IGoogleTokenValidator` service (wraps IHttpClientFactory call to Google tokeninfo)
- Account linking logic (auto-link on matching email)
- Unit tests: Google token validation, upsert paths (new user, existing no-link, existing with-link)
- Integration tests: full endpoint happy path + invalid token + unreachable Google

### Dependencies

#### Bolt Dependencies (within intent)
- **005-auth-core** (Required): Must be complete — needs `User` entity, `ITokenService` for JWT issuance, `RefreshToken` table

#### Unit Dependencies (cross-unit)
- **Bolt 001** (error-handling-logging): Required — ExceptionHandlerMiddleware
- **Bolt 002** (security-baselines): Required — rate limiter, CORS
