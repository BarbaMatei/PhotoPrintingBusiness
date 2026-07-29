---
id: 001-google-token-validation
unit: 002-social-auth
intent: 002-authentication
status: complete
priority: must
created: 2026-05-20T12:56:00Z
assigned_bolt: 006-social-auth
implemented: true
---

# Story: 001-google-token-validation

## User Story

**As a** user who clicks "Continuă cu Google"
**I want** the backend to verify my Google identity token
**So that** only legitimate Google accounts can sign in

## Acceptance Criteria

- [ ] **Given** `POST /api/auth/google {idToken}`, **When** the token is valid and `aud` matches `GoogleClientId`, **Then** the Google payload (sub, email, name, picture) is extracted and returned to the service layer
- [ ] **Given** a google sign-in request, **When** the `id_token` is malformed or Google tokeninfo returns an error, **Then** returns 401 `"Autentificarea Google a eșuat"`
- [ ] **Given** a google sign-in request, **When** the `aud` claim does not match the configured `GoogleClientId`, **Then** returns 401 (prevents token reuse from another app)
- [ ] **Given** a google sign-in request, **When** Google tokeninfo endpoint is unreachable, **Then** returns 502 `"Serviciu extern indisponibil"` (logged as warning)

## Technical Notes

- Google tokeninfo endpoint: `GET https://oauth2.googleapis.com/tokeninfo?id_token={token}`
- Use `IHttpClientFactory` with named client `"Google"` (set timeout 5s, retry 1x)
- Extract from payload: `sub` (ProviderKey), `email`, `given_name`, `family_name`, `picture`
- `aud` must equal `appsettings["GoogleAuth:ClientId"]`

## Dependencies

### Requires
- Bolt 001 (ExceptionHandlerMiddleware for 401/502 responses)

### Enables
- Story 002-account-upsert-linking (needs validated Google payload)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Expired Google token | tokeninfo returns error → 401 |
| `picture` field absent | Proceed without picture (nullable) |
| Google tokeninfo returns HTTP 400 | 401 to client |

## Out of Scope

- Other OAuth providers
- Frontend button rendering (→ `004-authentication-ui`)
