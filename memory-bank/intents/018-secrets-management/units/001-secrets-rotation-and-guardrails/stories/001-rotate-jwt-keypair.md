---
id: 001-rotate-jwt-keypair
unit: 001-secrets-rotation-and-guardrails
intent: 018-secrets-management
status: draft
priority: must
created: 2026-05-25T10:25:00Z
assigned_bolt: 041-secrets-management
implemented: false
---

# Story: 001-rotate-jwt-keypair

## User Story

**As** an operator
**I want** the JWT keypair rotated across every environment
**So that** any actor in possession of the leaked key cannot forge tokens against us

## Acceptance Criteria

- [ ] New RSA 2048-bit keypair generated using `openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048`.
- [ ] Production env vars `Jwt__PrivateKeyPem` / `Jwt__PublicKeyPem` updated with the new keypair on deploy host.
- [ ] Staging key rotated independently of production.
- [ ] Dev key is generated locally per developer; no shared dev key going forward.
- [ ] Post-rotation smoke test: existing access token rejected with 401; refresh-token flow issues a new valid one signed with the new key.

## Technical Notes

- Coordinate with intent 017 deploy workflow — env var rotation triggers a routine deploy.
- Refresh tokens are SHA-256 hashed in DB — unaffected by signing key rotation.
- Plan a 5-minute window where users may need to refresh once. Communicate via banner if user-visible.

## Dependencies

### Requires
- intent 017 (env-var matrix wired)

### Enables
- 002-remove-key-from-repo

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Active JWTs at rotation | All become invalid; clients silently refresh; UX impact: at most one extra request |
| Misconfigured public key | Boot fails fast via `ValidateOnStart` |

## Out of Scope

- HSM / KMS storage.
