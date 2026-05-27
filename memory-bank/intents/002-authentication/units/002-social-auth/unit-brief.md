---
unit: 002-social-auth
intent: 002-authentication
unit_type: backend
default_bolt_type: ddd-construction-bolt
phase: inception
status: ready
created: 2026-05-20T12:56:00Z
updated: 2026-05-20T12:56:00Z
---

# Unit Brief: social-auth

## Purpose

Handle Google OAuth authentication server-side. Accepts a Google `id_token` from the Angular frontend, validates it with Google's tokeninfo endpoint, upserts the user record (create if new, link if existing email account), and issues the same JWT + refresh cookie as password login.

## Scope

### In Scope
- Server-side validation of Google `id_token` (tokeninfo endpoint + `aud` check)
- User upsert: create new user with `IsEmailConfirmed=true`, no password
- Account auto-linking when an email+password account with the same email already exists
- `ExternalLogin` entity for storing the Google subject identifier
- Issuing platform JWT + refresh token (reuses auth-core issuance logic)

### Out of Scope
- Password-based login (→ `001-auth-core`)
- Other OAuth providers (not in scope for FotoTipar v1)
- Frontend Google button (→ `004-authentication-ui`)

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-4 | Google OAuth backend — validate id_token, upsert user, issue JWT | Must |

---

## Domain Concepts

### Key Entities
| Entity | Description | Key Attributes |
|--------|-------------|----------------|
| `ExternalLogin` | Maps a provider identity to a FotoTipar user | Id, UserId (FK), Provider (`"Google"`), ProviderKey (googleSub) |

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| GoogleSignIn | Validate token, upsert user, issue JWT | `{idToken}` | `{accessToken, expiresIn, accountLinked?}` + cookie |
| LinkAccount | Add ExternalLogin row to existing user | UserId, Provider, ProviderKey | ExternalLogin row |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 2 |
| Must Have | 2 |
| Should Have | 0 |
| Could Have | 0 |

### Stories
| # | Story | Priority |
|---|-------|----------|
| 001 | google-token-validation | Must |
| 002 | account-upsert-linking | Must |

---

## Technical Constraints

- Google tokeninfo URL: `https://oauth2.googleapis.com/tokeninfo?id_token={token}`
- Must verify `aud` claim equals `GoogleClientId` from configuration
- Never forward or store the raw Google `id_token` after validation
- JWT + refresh token issuance must reuse the same service method as `001-auth-core` (no duplication)
- `HttpClient` to Google tokeninfo should use `IHttpClientFactory` (registered as typed/named client)
