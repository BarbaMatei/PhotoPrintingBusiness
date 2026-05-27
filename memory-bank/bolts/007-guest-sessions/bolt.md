---
id: 007-guest-sessions
unit: 003-guest-sessions
intent: 002-authentication
type: ddd-construction-bolt
status: done
started: 2026-05-20T15:30:00Z
completed: 2026-05-20T17:30:00Z
current_stage: complete
stages_completed: [domain-model, technical-design, implement, test]
stories:
  - 001-guest-session-create
  - 002-guest-session-claim
  - 003-guest-session-cleanup
created: 2026-05-20T13:00:00Z

requires_bolts: [005-auth-core]
enables_bolts: [008-authentication-ui]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 2
---

## Bolt: 007-guest-sessions

### Objective

Build the guest checkout backend: `GuestSession` entity, token issuance endpoint, dual-auth authorization handler (`X-Guest-Token` alongside Bearer JWT), claim-after-registration endpoint, and a background cleanup job for orphaned sessions.

### Stories Included

- [ ] **001-guest-session-create**: POST /api/auth/guest — validate contact info, create session, return token - Priority: Must
- [ ] **002-guest-session-claim**: POST /api/auth/guest/claim — transfer orders to real account, invalidate session - Priority: Must
- [ ] **003-guest-session-cleanup**: BackgroundService — hourly cleanup of expired orphaned sessions - Priority: Must

### Expected Outputs

- `GuestSession` entity + EF Core migration
- `POST /api/auth/guest` and `POST /api/auth/guest/claim` endpoints
- `GuestAuthenticationHandler` (custom `AuthenticationHandler`) or `AuthorizationHandler` for `X-Guest-Token`
- `GuestSessionCleanupJob` (`BackgroundService`, `PeriodicTimer`, 1h interval)
- Unit tests: session creation, claim logic, cleanup query
- Integration tests: endpoint happy paths + expired token + dual-auth precedence

### Dependencies

#### Bolt Dependencies (within intent)
- **005-auth-core** (Required): Must be complete — claim endpoint needs `User` entity and JWT auth

#### Unit Dependencies (cross-unit)
- **Bolt 001** (error-handling-logging): Required — ExceptionHandlerMiddleware
- **Bolt 002** (security-baselines): Required — CORS, security headers
