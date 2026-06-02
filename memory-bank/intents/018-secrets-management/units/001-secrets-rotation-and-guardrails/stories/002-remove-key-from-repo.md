---
id: 002-remove-key-from-repo
unit: 001-secrets-rotation-and-guardrails
intent: 018-secrets-management
status: complete
priority: must
created: 2026-05-25T10:25:00Z
assigned_bolt: 041-secrets-management
implemented: true
implemented_at: 2026-05-25T15:10:00Z
---

# Story: 002-remove-key-from-repo

## User Story

**As** a contributor
**I want** the dev RSA key removed from source control and replaced with a documented user-secrets workflow
**So that** the repo no longer leaks a JWT signing key on every clone

## Acceptance Criteria

- [ ] `appsettings.Development.json` `Jwt:PrivateKeyPem` is now `""` (empty string).
- [ ] `README.md` "First-time setup" instructs:
  ```
  openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out dev-private.pem
  openssl rsa -in dev-private.pem -pubout -out dev-public.pem
  dotnet user-secrets init --project src/PhotoPrint.API
  dotnet user-secrets set "Jwt:PrivateKeyPem" "$(cat dev-private.pem)" --project src/PhotoPrint.API
  dotnet user-secrets set "Jwt:PublicKeyPem"  "$(cat dev-public.pem)"  --project src/PhotoPrint.API
  ```
- [ ] Boot fails fast with a clear message when neither user-secrets nor env var supply a value.
- [ ] No file in `src/` matches `BEGIN .* PRIVATE KEY` after the change.

## Technical Notes

- `JwtSettings` already exists; add `[Required]` to `PrivateKeyPem` and `PublicKeyPem` (or `Validate(s => !string.IsNullOrEmpty(s.PrivateKeyPem))`).

## Dependencies

### Requires
- 001-rotate-jwt-keypair

### Enables
- 003-gitignore-and-secrets-dir

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Developer skips user-secrets | Boot fails with `OptionsValidationException` naming `Jwt:PrivateKeyPem` |
| Multi-line PEM in env var | Newlines escaped; document `\n` substitution or use file path indirection |

## Out of Scope

- Replacing user-secrets with a vault (later).
