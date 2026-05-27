---
unit: 001-account-api
intent: 008-user-account
phase: inception
status: ready
created: 2026-05-22T12:00:00Z
updated: 2026-05-22T12:00:00Z
default_bolt_type: ddd-construction-bolt
---

# Unit Brief: 001-account-api

## Purpose

Expose backend endpoints for authenticated customers to manage their profile, change their password, save delivery addresses, and request account deletion.

## Scope

### In Scope
- `AccountController` — `/api/account` endpoints (GET, PATCH, change-password, DELETE)
- `AccountAddressesController` — `/api/account/addresses` CRUD (GET, POST, PUT, DELETE, PATCH default)
- `IAccountService` + `AccountService`
- `SavedAddress` entity + EF Core migration
- DTOs: `AccountDto`, `UpdateAccountRequest`, `ChangePasswordRequest`, `SavedAddressDto`, `CreateAddressRequest`
- FluentValidation for all write endpoints
- `AccountDeletionJob` dependency: sets `DeletionRequestedAt` on User entity (job runs in bolt 025)

### Out of Scope
- Google account linking/unlinking (complex OAuth flow — deferred)
- 2FA
- Email preference management
- Frontend (unit 002-account-ui)

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|--------|-------------|------------|
| `User` | Platform user | FirstName, LastName, Email, Phone, HasPassword (computed), DeletionRequestedAt |
| `SavedAddress` | Stored delivery address | Id, UserId, RecipientName, Street, Number, Block?, City, County, PostalCode, Phone, IsDefault, CreatedAt |

### Key Operations
| Operation | Endpoint | Business Rule |
|-----------|----------|---------------|
| Get profile | `GET /api/account` | Returns profile + linked providers |
| Update profile | `PATCH /api/account` | Name + phone only; email is immutable |
| Change password | `POST /api/account/change-password` | Verify current password; invalidate all refresh tokens |
| Delete account | `DELETE /api/account` | Set DeletionRequestedAt; do not hard-delete yet |
| List addresses | `GET /api/account/addresses` | All addresses for user |
| Add address | `POST /api/account/addresses` | Max 5 per user |
| Update address | `PUT /api/account/addresses/{id}` | Ownership check |
| Delete address | `DELETE /api/account/addresses/{id}` | Ownership check |
| Set default | `PATCH /api/account/addresses/{id}/default` | Clears other defaults |

## Technical Constraints

- `SavedAddress` requires a new EF Core migration
- `hasPassword` computed from `PasswordHash != null`
- `linkedProviders` from `ExternalLogins` table
- Max 5 addresses enforced server-side (400 if exceeded)
- Change-password: use `UserManager.ChangePasswordAsync`; then revoke all refresh tokens for user

## Story Summary

| # | Story | Priority |
|---|-------|----------|
| 001 | `001-account-profile-api` | Must |
| 002 | `002-saved-addresses-api` | Must |
