# US-105 — Login — JWT + Refresh Token (Backend)

## Story
**As a** system  
**I want to** authenticate users and issue short-lived JWT with rotating refresh tokens  
**So that** sessions are secure with automatic rotation

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-1 | Autentificare & Conturi

## Dependencies
- US-102 (Users must exist in DB)
- US-801 (Global error handling)

## Acceptance Criteria

1. **`POST /api/auth/login`** → `{accessToken, expiresIn}`; sets `refreshToken` in HttpOnly Secure SameSite=Strict cookie
2. **Access token**: JWT RS256, 15-min expiry; claims: `sub` (userId), `email`, `role`, `firstName`
3. **Refresh token**: opaque UUID, SHA-256 hashed in DB, 30-day expiry; rotated on every use (old token revoked immediately)
4. **`POST /api/auth/refresh`** — reads cookie, validates, issues new pair
5. **`POST /api/auth/logout`** — revokes refresh token in DB, clears cookie
6. **5 consecutive failures** → account locked 15 min; `423` response + lockout email sent

## Technical Notes

### Endpoints

```
POST /api/auth/login
{ "email": "string", "password": "string" }
→ 200 { "accessToken": "jwt", "expiresIn": 900 }
  + Set-Cookie: refreshToken=uuid; HttpOnly; Secure; SameSite=Strict; Path=/api/auth; Max-Age=2592000

→ 401 { "message": "Email sau parolă incorectă" }
→ 423 { "message": "Cont blocat temporar", "lockedUntil": "ISO8601" }
```

```
POST /api/auth/refresh
(reads refreshToken cookie)
→ 200 { "accessToken": "jwt", "expiresIn": 900 } + new cookie
→ 401 { "message": "Sesiune expirată" }
```

```
POST /api/auth/logout
Authorization: Bearer {jwt}
→ 204 No Content + cookie cleared
```

### Implementation Details
- JWT signing: RS256 with RSA key pair (private key in config/env, public key available for validation)
- Claims: `sub`, `email`, `role`, `firstName`; issuer and audience configured in `appsettings.json`
- Refresh token rotation: on each `/refresh` call, old token marked `RevokedAt=now`, new token created
- Reuse detection: if a revoked token is used, revoke ALL tokens for that user (potential theft)
- Failed login counter: `FailedLoginAttempts` column on Users; reset on successful login
- Account lockout: `LockoutEnd` column; 5 failures → locked 15 min; send lockout notification email

### Database
- `RefreshTokens` table: `Id`, `UserId→Users`, `TokenHash`, `ExpiresAt`, `RevokedAt?`, `CreatedAt`
- Add `FailedLoginAttempts` (int), `LockoutEnd` (DateTime?) to `Users`

## Files to Create/Modify
- `src/PhotoPrint.API/Controllers/AuthController.cs` (Login, Refresh, Logout)
- `src/PhotoPrint.API/DTOs/Auth/LoginRequest.cs`
- `src/PhotoPrint.API/DTOs/Auth/LoginResponse.cs`
- `src/PhotoPrint.API/Services/ITokenService.cs` + `TokenService.cs`
- `src/PhotoPrint.API/Services/AuthService.cs` (LoginAsync, RefreshAsync, LogoutAsync)
- `src/PhotoPrint.API/Models/RefreshToken.cs`
- EF Core migration for RefreshTokens + Users lockout fields

## Testing
- Unit test: valid credentials → JWT + cookie
- Unit test: invalid password → 401
- Unit test: unconfirmed email → appropriate error
- Unit test: refresh token rotation
- Unit test: reuse detection revokes all tokens
- Unit test: lockout after 5 failures
- Integration test: full login → refresh → logout flow
