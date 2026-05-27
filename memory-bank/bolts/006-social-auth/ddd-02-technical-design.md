---
stage: design
bolt: 006-social-auth
created: 2026-05-20T14:05:00Z
---

## Technical Design: social-auth

### Architecture Pattern
Layered architecture (same as bolt-005): Controller → Service → EF Core DbContext. No CQRS — single write path with no complex read models.

### Layer Structure
```
┌────────────────────────────────────────┐
│  Presentation (AuthController)         │  POST /api/auth/google
├────────────────────────────────────────┤
│  Application (SocialAuthService)       │  Upsert + token issuance
├────────────────────────────────────────┤
│  Domain (IGoogleTokenValidator)        │  Token validation, GooglePayload
├────────────────────────────────────────┤
│  Infrastructure (HttpClientFactory)    │  Google tokeninfo HTTP call
└────────────────────────────────────────┘
```

### API Design

**POST /api/auth/google**
- Rate limit: `[EnableRateLimiting("auth")]` (existing 10/60s)
- Request: `{ "idToken": "eyJhb..." }`
- Response 200: `{ "accessToken": "...", "expiresIn": 900, "accountLinked": false }` + Set-Cookie refresh_token
- Response 401: `{ "title": "Autentificarea Google a eșuat", "status": 401 }`
- Response 502: `{ "title": "Serviciu extern indisponibil", "status": 502 }`
- No `[Authorize]` — public endpoint

### New Files

| File | Description |
|------|-------------|
| `Models/ExternalLogin.cs` | EF Core entity |
| `Configuration/GoogleAuthSettings.cs` | Bound from "GoogleAuth" section |
| `DTOs/Auth/GoogleLoginRequest.cs` | `{ IdToken }` |
| `DTOs/Auth/GoogleLoginResponse.cs` | `{ AccessToken, ExpiresIn, AccountLinked }` |
| `Validators/Auth/GoogleLoginRequestValidator.cs` | NotEmpty on IdToken |
| `Services/IGoogleTokenValidator.cs` + `GoogleTokenValidator.cs` | HTTP call to tokeninfo |
| `Services/ISocialAuthService.cs` + `SocialAuthService.cs` | Upsert + JWT issuance |
| `Extensions/SocialAuthExtensions.cs` | `AddSocialAuth(IServiceCollection, IConfiguration)` |
| `Exceptions/BadGatewayException.cs` | 502 exception type |

### Data Model

Table: `external_logins`
- `id` uuid PK
- `user_id` uuid FK→users(id) ON DELETE CASCADE
- `provider` varchar(50) NOT NULL
- `provider_key` varchar(256) NOT NULL
- `created_at` timestamptz NOT NULL
- UNIQUE index on (provider, provider_key)
- UNIQUE index on (user_id, provider)

EF Migration: `AddExternalLoginTable`

### Security Design

- id_token never logged or returned — only sub/email/name extracted
- aud verification in GoogleTokenValidator prevents cross-app token reuse
- Rate limiting via existing "auth" policy

### NFR Implementation

- 5s timeout on named HttpClient("Google")
- Single retry via `AddStandardResilienceHandler()` (Microsoft.Extensions.Http.Resilience)
- No caching — Google tokens are short-lived

### Extension Method

`AddSocialAuth(IServiceCollection, IConfiguration)` in `SocialAuthExtensions.cs`:
- Binds GoogleAuthSettings
- Registers IGoogleTokenValidator (Scoped), ISocialAuthService (Scoped)
- Configures named HttpClient("Google") with base address + standard resilience
