---
id: 002-account-upsert-linking
unit: 002-social-auth
intent: 002-authentication
status: draft
priority: must
created: 2026-05-20T12:56:00Z
assigned_bolt: 006-social-auth
implemented: false
---

# Story: 002-account-upsert-linking

## User Story

**As a** user signing in with Google
**I want** my FotoTipar account to be created or linked automatically
**So that** I don't need to manage a separate password

## Acceptance Criteria

- [ ] **Given** a validated Google payload for an email not in the DB, **When** processed, **Then** a new `User` row is created (`IsEmailConfirmed=true`, `Role=Customer`, no password hash, `FirstName/LastName` from Google payload) and an `ExternalLogin` row is inserted (`Provider="Google"`, `ProviderKey=googleSub`)
- [ ] **Given** a validated Google payload for an email that already has an `ExternalLogin` row for Google, **When** processed, **Then** the existing user is retrieved (no insert) and JWT + cookie are issued
- [ ] **Given** a validated Google payload for an email that exists as an email+password account with NO Google `ExternalLogin`, **When** processed, **Then** the `ExternalLogin` row is inserted linking the accounts; response includes `accountLinked: true` flag
- [ ] **Given** any successful Google sign-in, **When** the response is built, **Then** returns `{accessToken, expiresIn, accountLinked: bool}` + HttpOnly Secure refresh cookie (same as password login)
- [ ] **Given** the Google `id_token` is NOT forwarded, **When** the response is sent, **Then** the raw `idToken` value is absent from the response body and logs

## Technical Notes

- JWT + refresh token issuance: call the same `ITokenService.IssueTokensAsync(user)` method used by `001-auth-core` (DRY)
- `accountLinked` flag: `true` only when an existing email+password account was linked for the first time
- Transaction: user upsert + ExternalLogin insert must be atomic

## Dependencies

### Requires
- Story 001-google-token-validation (validated Google payload)
- Bolt 005 (Unit 001-auth-core: User entity + token issuance service)

### Enables
- Story 003-login-page in unit 004 (account-linked toast display)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Same Google account used from two concurrent requests | One succeeds; second finds ExternalLogin row already exists → normal login path |
| Google returns different email after account rename | New user created (sub is stable, but email changed) |
| User previously deleted their account but ExternalLogin row persists | Re-create user (treat as new) |

## Out of Scope

- Unlinking Google account
- Adding Google to an account that already has a different Google sub (not supported in v1)
