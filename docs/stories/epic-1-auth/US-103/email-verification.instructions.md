# US-103 — Email Verification (Backend)

## Story
**As a** system  
**I want to** verify user owns the email before activating the account  
**So that** only legitimate email addresses are used

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-1 | Autentificare & Conturi

## Dependencies
- US-102 (Register must create users + tokens)
- US-605 (IEmailService for resend)

## Acceptance Criteria

1. **`GET /api/auth/confirm-email?userId=&token=`** — marks `IsEmailConfirmed=true`; deletes token row
2. **Expired or invalid token** → `400` with message `Link invalid sau expirat`
3. **`POST /api/auth/resend-confirmation`** — rate-limited 3/hour/email; no-ops silently if already confirmed
4. **FE confirmation-pending page** shows countdown and resend button

## Technical Notes

### Endpoints

```
GET /api/auth/confirm-email?userId={uuid}&token={uuid}
→ 200 { "message": "Email confirmat cu succes" }
→ 400 { "message": "Link invalid sau expirat" }
```

```
POST /api/auth/resend-confirmation
Content-Type: application/json
{ "email": "string" }
→ 200 (always, to prevent email enumeration)
```

### Implementation Details
- Confirm flow: lookup token by `SHA256(token)` + `userId`; check `ExpiresAt > now`; set `IsEmailConfirmed=true`; delete token row; all in a single transaction
- Resend: check if user exists AND `IsEmailConfirmed=false`; if already confirmed, return 200 silently; otherwise generate new token (invalidate old), send email
- Rate limit resend: 3 requests/hour per email address
- Confirmation URL format: `{frontendUrl}/auth/confirm-email?userId={userId}&token={rawToken}`

### Frontend Component (included in this story)
- `src/app/features/auth/confirm-email/confirm-email.component.ts`
- Reads `userId` and `token` from query params
- Calls `GET /api/auth/confirm-email` on init
- Shows success message or error with resend option
- Confirmation-pending page: shows email sent message + resend button + countdown timer (60s between resends)

## Files to Create/Modify
- `src/PhotoPrint.API/Controllers/AuthController.cs` (ConfirmEmail, ResendConfirmation actions)
- `src/PhotoPrint.API/DTOs/Auth/ResendConfirmationRequest.cs`
- `src/PhotoPrint.API/Services/AuthService.cs` (ConfirmEmailAsync, ResendConfirmationAsync)
- `src/app/features/auth/confirm-email/confirm-email.component.ts`
- `src/app/features/auth/email-pending/email-pending.component.ts`

## Testing
- Unit test: valid token confirms email
- Unit test: expired token returns 400
- Unit test: invalid token returns 400
- Unit test: resend when already confirmed → no-op
- Unit test: resend rate limiting
- Integration test: full confirm flow
