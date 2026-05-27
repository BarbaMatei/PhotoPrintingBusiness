# US-702 — Account API (Backend)

## Story
**As a** system  
**I want to** allow users to manage profile data and account lifecycle

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-7 | Cont Utilizator & Legal

## Dependencies
- US-105 (Auth — JWT)
- US-803 (AccountDeletionJob for deferred deletion)

## Acceptance Criteria

1. **`GET /api/account`** — `{firstName, lastName, email, phone, hasPassword, linkedProviders[]}`
2. **`PATCH /api/account`** — updates name/phone; FluentValidation
3. **`POST /api/account/change-password`** — requires `currentPassword`; invalidates all refresh tokens on success
4. **`DELETE /api/account`** — sets `DeletionRequestedAt`; background job hard-deletes after 30 days if not cancelled

## Technical Notes

### Endpoints
```
GET /api/account
Authorization: Bearer {jwt}
→ 200 {
  "firstName": "Ion",
  "lastName": "Popescu",
  "email": "ion@email.com",
  "phone": "0712345678",
  "hasPassword": true,
  "linkedProviders": ["Google"]
}
```

```
PATCH /api/account
{ "firstName": "Ion", "lastName": "Ionescu", "phone": "0712345679" }
→ 200

POST /api/account/change-password
{ "currentPassword": "old", "newPassword": "new" }
→ 200
→ 400 { "message": "Parola curentă este incorectă" }

DELETE /api/account
→ 200 { "message": "Contul va fi șters în 30 de zile. Conectează-te pentru a anula." }
```

### Implementation Details
- GET: map User entity to AccountDto; check ExternalLogins for linked providers; hasPassword = PasswordHash is not null
- PATCH: FluentValidation on name/phone; update only provided fields
- Change password: verify current password with `IPasswordHasher`; update hash; revoke ALL refresh tokens (force re-login)
- Delete: set `DeletionRequestedAt = DateTime.UtcNow`; do NOT delete immediately; `AccountDeletionJob` checks for accounts where `DeletionRequestedAt < 30 days ago`
- Cancel deletion: any login within 30 days clears `DeletionRequestedAt` (handled in login flow)

### Address Sub-endpoints
```
GET /api/account/addresses → list saved addresses
POST /api/account/addresses → add (max 5)
PUT /api/account/addresses/{id} → update
DELETE /api/account/addresses/{id} → delete
```

## Files to Create/Modify
- `src/PhotoPrint.API/Controllers/AccountController.cs`
- `src/PhotoPrint.API/DTOs/Account/AccountDto.cs`
- `src/PhotoPrint.API/DTOs/Account/UpdateAccountRequest.cs`
- `src/PhotoPrint.API/DTOs/Account/ChangePasswordRequest.cs`
- `src/PhotoPrint.API/Validators/UpdateAccountValidator.cs`
- `src/PhotoPrint.API/Validators/ChangePasswordValidator.cs`
- `src/PhotoPrint.API/Services/IAccountService.cs` + `AccountService.cs`
- `src/PhotoPrint.API/Models/Address.cs`

## Testing
- Unit test: get account returns correct DTO
- Unit test: update validates fields
- Unit test: change password verifies current password
- Unit test: change password revokes refresh tokens
- Unit test: delete sets DeletionRequestedAt
- Unit test: address CRUD operations
- Integration test: full account management flow
