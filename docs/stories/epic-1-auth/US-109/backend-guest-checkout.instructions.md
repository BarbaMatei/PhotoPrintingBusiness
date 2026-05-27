# US-109 — Guest Checkout — Backend

## Story
**As a** system  
**I want to** allow anonymous order placement linked by a guest token, with option to later claim the order

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-1 | Autentificare & Conturi

## Dependencies
- US-801 (Global error handling)
- US-803 (Background jobs for cleanup)

## Acceptance Criteria

1. **`POST /api/auth/guest`** — validates `{firstName, lastName, email, phone}`; creates `GuestSession(Id=UUID, email, firstName, lastName, phone, CreatedAt, ExpiresAt=+7days)`
2. **Returns** `{guestToken: UUID}` — client includes as `X-Guest-Token` header
3. **All order/cart/upload endpoints** accept EITHER `Bearer JWT` OR `X-Guest-Token` header
4. **Guest orders** linked to `GuestSessionId` (not `UserId`); email stored for notifications
5. **`POST /api/auth/guest/claim`** — after guest registers/logs in, transfers guest orders to real account; invalidates guest token
6. **GuestSessions** with no orders cleaned up after 7 days (background job)

## Technical Notes

### Endpoints
```
POST /api/auth/guest
{ "firstName": "string", "lastName": "string", "email": "string", "phone": "string" }
→ 201 { "guestToken": "uuid" }
→ 422 validation errors
```

```
POST /api/auth/guest/claim
Authorization: Bearer {jwt}
{ "guestToken": "uuid" }
→ 200 { "claimedOrders": 2 }
→ 404 { "message": "Sesiune oaspete invalidă" }
```

### Implementation Details
- `GuestSession` entity: `Id` (UUID), `Email`, `FirstName`, `LastName`, `Phone`, `CreatedAt`, `ExpiresAt`
- Guest token is the `GuestSession.Id` itself — no hashing needed (short-lived, limited scope)
- Auth middleware: custom `GuestAuthenticationHandler` that reads `X-Guest-Token`, validates against `GuestSessions` table (not expired), sets claims principal with `GuestSessionId`
- Claim flow: transfer all `Orders`, `CartItems`, `Uploads` from `GuestSessionId` to `UserId`; set `GuestSession.ExpiresAt = now` (invalidate)
- Background cleanup: `GuestSessionCleanupJob` — delete sessions with no linked orders where `ExpiresAt < now`

### Database
- `GuestSessions` table (see Appendix A)
- All related entities have nullable `GuestSessionId` FK

## Files to Create/Modify
- `src/PhotoPrint.API/Controllers/AuthController.cs` (Guest, GuestClaim)
- `src/PhotoPrint.API/DTOs/Auth/GuestRequest.cs`
- `src/PhotoPrint.API/DTOs/Auth/GuestResponse.cs`
- `src/PhotoPrint.API/Models/GuestSession.cs`
- `src/PhotoPrint.API/Services/AuthService.cs` (CreateGuestAsync, ClaimGuestAsync)
- `src/PhotoPrint.API/Auth/GuestAuthenticationHandler.cs`
- `src/PhotoPrint.API/BackgroundJobs/GuestSessionCleanupJob.cs`
- EF Core migration for GuestSessions

## Testing
- Unit test: guest session creation
- Unit test: guest claim transfers orders
- Unit test: expired session rejected
- Unit test: cleanup job removes orphan sessions
- Integration test: full guest → register → claim flow
