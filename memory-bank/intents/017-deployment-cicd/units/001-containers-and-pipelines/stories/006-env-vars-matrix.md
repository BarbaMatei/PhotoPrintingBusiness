---
id: 006-env-vars-matrix
unit: 001-containers-and-pipelines
intent: 017-deployment-cicd
status: draft
priority: must
created: 2026-05-25T10:20:00Z
assigned_bolt: 040-containers-and-pipelines
implemented: false
---

# Story: 006-env-vars-matrix

## User Story

**As** an operator
**I want** every secret and environment-specific config documented as an env var
**So that** moving between dev / staging / prod is a configuration swap, not a code change

## Acceptance Criteria

- [ ] `.env.example` at repo root lists every required variable with a placeholder and a comment.
- [ ] `README.md` has an "Environment matrix" table grouping vars by feature (DB, JWT, Stripe, EuPlatesc, Sameday, ANAF, SendGrid).
- [ ] Each `IOptions<T>` settings class binds via `AddOptions<T>().Bind(...).ValidateDataAnnotations().ValidateOnStart()`.
- [ ] Missing required env var → boot fails with clear `OptionsValidationException` naming the field.
- [ ] No secret values committed; CI verifies via `git diff --check` + `gitleaks` step (basic ruleset).

## Technical Notes

```dotenv
# .env.example
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Default=Host=db;Database=fototipar;Username=fototipar;Password=changeme
Jwt__PrivateKeyPem=
Jwt__PublicKeyPem=
Stripe__SecretKey=
Stripe__WebhookSecret=
EuPlatesc__MerchantId=
EuPlatesc__SecretKey=
Sameday__Enabled=false
Sameday__Username=
Sameday__Password=
Anaf__ClientId=
Anaf__ClientSecret=
SendGrid__ApiKey=
```

## Dependencies

### Requires
- None (documentation + light wiring)

### Enables
- intent 018 (clean baseline for rotation work)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Optional service (Sameday disabled) | Flagged via `Enabled=false`; validation skips downstream fields |
| Multiple `appsettings.*.json` overrides | Documented precedence: env > user-secrets > appsettings.{env}.json > appsettings.json |

## Out of Scope

- Vault / KMS integration (later intent).
