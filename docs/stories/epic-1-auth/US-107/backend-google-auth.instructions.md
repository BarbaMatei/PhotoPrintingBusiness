# US-107 — Google Social Login — Backend

## Story
**As a** system  
**I want to** validate Google id_token server-side and issue platform JWT  
**So that** Google users get the same session management as password users

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-1 | Autentificare & Conturi

## Dependencies
- US-105 (JWT + Refresh Token infrastructure)
- US-102 (User entity)

## Acceptance Criteria

1. **`POST /api/auth/google {idToken}`** — validates with Google tokeninfo; verifies `aud=CLIENT_ID`
2. **Upserts user**: creates if new (no password), or links GoogleId to existing account by email
3. **Stores** `ExternalLogin(UserId, Provider='Google', ProviderKey=googleSub)`
4. **Returns** same JWT + refresh cookie as password login
5. **Never forwards** Google token to client after this call

## Technical Notes

### Endpoint
```
POST /api/auth/google
{ "idToken": "string" }
→ 200 { "accessToken": "jwt", "expiresIn": 900 } + refresh cookie
→ 401 { "message": "Token Google invalid" }
```

### Implementation Details
- Validate `idToken` using Google.Apis.Auth (`GoogleJsonWebSignature.ValidateAsync()`)
- Verify `Audience` matches configured `GoogleClientId`
- Extract: email, name, Google sub (unique ID)
- User lookup by email:
  - **Not found**: create new User (no password, `IsEmailConfirmed=true` since Google verifies email), create ExternalLogin
  - **Found without Google link**: add ExternalLogin to existing user; if `IsEmailConfirmed=false`, set to true
  - **Found with Google link**: proceed to login
- Issue JWT + refresh token using same `TokenService` as password login
- Never store or return the raw Google token

### Database
- `ExternalLogins` table: `Id`, `UserId→Users`, `Provider` (string), `ProviderKey` (string)
- Unique index on (Provider, ProviderKey)

## Files to Create/Modify
- `src/PhotoPrint.API/Controllers/AuthController.cs` (GoogleLogin action)
- `src/PhotoPrint.API/DTOs/Auth/GoogleLoginRequest.cs`
- `src/PhotoPrint.API/Services/AuthService.cs` (GoogleLoginAsync)
- `src/PhotoPrint.API/Services/IGoogleAuthService.cs` + `GoogleAuthService.cs`
- `src/PhotoPrint.API/Models/ExternalLogin.cs`
- EF Core migration for ExternalLogins

## Testing
- Unit test: valid Google token → new user created + JWT issued
- Unit test: existing user → account linked
- Unit test: invalid token → 401
- Unit test: wrong audience → 401
- Integration test: full Google login flow with mocked Google validation
