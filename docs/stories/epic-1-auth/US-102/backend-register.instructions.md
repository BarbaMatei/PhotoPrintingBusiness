# US-102 — Register — Backend

## Story
**As a** system  
**I want to** persist new accounts securely and trigger email confirmation  
**So that** users can create verified accounts

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-1 | Autentificare & Conturi

## Dependencies
- US-801 (Global Error Handling must be in place)
- US-605 (IEmailService abstraction for sending confirmation emails)
- Database schema: `Users`, `EmailConfirmationTokens` tables

## Acceptance Criteria

1. **`POST /api/auth/register`** — FluentValidation on all fields
2. **Password hashed** with ASP.NET Identity (PBKDF2-SHA256, 10,000 iterations)
3. **User record created**: `Id` (UUID), `Email` (unique index), `PasswordHash`, `FirstName`, `LastName`, `Phone`, `Role=Customer`, `IsEmailConfirmed=false`, `CreatedAt`
4. **Email token**: generates UUID, stores SHA-256 hash with 24h expiry in `EmailConfirmationTokens` table
5. **Fires** `IEmailService.SendConfirmationEmailAsync` (async, non-blocking); logs failure but does NOT fail the request
6. **Returns** `201 {userId}`; `409` on duplicate email; rate-limited **5 req/IP/hour**

## Technical Notes

### Endpoint
```
POST /api/auth/register
Content-Type: application/json

{
  "firstName": "string",
  "lastName": "string",
  "email": "string",
  "password": "string",
  "confirmPassword": "string",
  "phone": "string?" 
}
```

### Response
- `201 Created` → `{ "userId": "uuid" }`
- `409 Conflict` → duplicate email
- `422 Unprocessable Entity` → validation errors `[{ "field": "...", "message": "..." }]`
- `429 Too Many Requests` → rate limit exceeded

### Implementation Details
- `RegisterValidator` (FluentValidation): email format, password rules (8+ chars, 1 upper, 1 digit, 1 special), confirmPassword match, phone format if provided
- Use `IPasswordHasher<User>` from ASP.NET Identity for hashing
- Email uniqueness enforced at DB level (unique index) — catch `DbUpdateException` and return 409
- Token generation: `Guid.NewGuid()` → store `SHA256(token)` in DB; send raw token in confirmation URL
- Rate limiting: use ASP.NET Core Rate Limiting middleware, partition by IP, 5 requests/hour on this endpoint
- Fire-and-forget email via `IEmailService` — wrap in try/catch, log errors with Serilog

### Database Entities
- `Users` table (see Appendix A)
- `EmailConfirmationTokens`: `Id`, `UserId→Users`, `TokenHash`, `ExpiresAt`, `UsedAt?`

## Files to Create/Modify
- `src/PhotoPrint.API/Controllers/AuthController.cs` (Register action)
- `src/PhotoPrint.API/DTOs/Auth/RegisterRequest.cs`
- `src/PhotoPrint.API/DTOs/Auth/RegisterResponse.cs`
- `src/PhotoPrint.API/Validators/RegisterValidator.cs`
- `src/PhotoPrint.API/Models/User.cs`
- `src/PhotoPrint.API/Models/EmailConfirmationToken.cs`
- `src/PhotoPrint.API/Services/IAuthService.cs` + `AuthService.cs`
- EF Core migration for Users + EmailConfirmationTokens

## Testing
- Unit test: RegisterValidator — all validation rules
- Unit test: AuthService.RegisterAsync — happy path, duplicate email, email send failure tolerance
- Integration test: POST /api/auth/register — full flow with DB
- Integration test: rate limiting enforcement
