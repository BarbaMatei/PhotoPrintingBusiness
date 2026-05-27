# US-110 — Password Reset (Backend + Frontend)

## Story
**As a** system  
**I want to** allow registered users to securely reset a forgotten password

## Type
BACKEND — ASP.NET Core + FRONTEND — Angular

## Epic
EPIC-1 | Autentificare & Conturi

## Dependencies
- US-102 (Users must exist)
- US-105 (Refresh tokens — must revoke all on password change)
- US-605 (IEmailService for reset email)

## Acceptance Criteria

1. **`POST /api/auth/forgot-password {email}`** — always returns `200` (no email enumeration); fires reset email if account exists
2. **Reset token**: UUID, hashed in DB, 1h expiry
3. **`POST /api/auth/reset-password {userId, token, newPassword}`** — validates token, updates hash, revokes ALL refresh tokens for user
4. **FE**: `/forgot-password` page and `/reset-password?userId=&token=` page with same password validation rules as registration

## Technical Notes

### Backend Endpoints
```
POST /api/auth/forgot-password
{ "email": "string" }
→ 200 { "message": "Dacă adresa este înregistrată, vei primi un email" }
```

```
POST /api/auth/reset-password
{ "userId": "uuid", "token": "uuid", "newPassword": "string" }
→ 200 { "message": "Parola a fost schimbată cu succes" }
→ 400 { "message": "Link invalid sau expirat" }
→ 422 validation errors
```

### Implementation Details (Backend)
- Forgot password: look up user by email; if found, generate UUID token, store SHA-256 hash in `PasswordResetTokens` with 1h expiry; send email with link `{frontendUrl}/auth/reset-password?userId={id}&token={token}`
- Always return 200 regardless of whether email exists (prevent enumeration)
- Reset password: validate token hash + expiry; update password hash; delete token; revoke ALL refresh tokens for user
- `PasswordResetTokens`: `Id`, `UserId→Users`, `TokenHash`, `ExpiresAt`, `UsedAt?`

### Implementation Details (Frontend)
- `/auth/forgot-password` page: email input + submit → show success message regardless
- `/auth/reset-password` page: reads `userId` and `token` from query params; new password + confirm password form (same validation as register); on success redirect to login with toast
- Both pages accessible without authentication

## Files to Create/Modify
- `src/PhotoPrint.API/Controllers/AuthController.cs` (ForgotPassword, ResetPassword)
- `src/PhotoPrint.API/DTOs/Auth/ForgotPasswordRequest.cs`
- `src/PhotoPrint.API/DTOs/Auth/ResetPasswordRequest.cs`
- `src/PhotoPrint.API/Models/PasswordResetToken.cs`
- `src/PhotoPrint.API/Services/AuthService.cs` (ForgotPasswordAsync, ResetPasswordAsync)
- `src/app/features/auth/forgot-password/forgot-password.component.ts`
- `src/app/features/auth/reset-password/reset-password.component.ts`
- EF Core migration for PasswordResetTokens

## Testing
- Unit test: forgot password with existing email → token created + email sent
- Unit test: forgot password with non-existing email → 200, no email sent
- Unit test: valid reset token → password updated, refresh tokens revoked
- Unit test: expired token → 400
- E2E: forgot → reset flow
