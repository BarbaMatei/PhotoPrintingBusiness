---
stage: model
bolt: 007-guest-sessions
created: 2026-05-20T15:30:00Z
---

## Static Model: guest-sessions

### Entities

- **GuestSession**: `Id` (Guid — IS the token, returned as-is to client), `Email` (string), `FirstName` (string), `LastName` (string), `Phone` (string), `CreatedAt` (DateTimeOffset), `ExpiresAt` (DateTimeOffset — now+7days), `ClaimedByUserId` (Guid? nullable FK→User) — Business Rules: token possession is the only authorization check; `ClaimedByUserId` acts as soft-invalidation; no uniqueness constraint on email; claimed sessions are never deleted

### Value Objects

- **GuestContactInfo**: `Email`, `FirstName`, `LastName`, `Phone` — immutable; phone validated against `^07[0-9]{8}$` (Romanian mobile)

### Aggregates

- **GuestSession** (root): No child entities — `IsExpired => ExpiresAt < DateTimeOffset.UtcNow`; `IsClaimed => ClaimedByUserId is not null`; `IsValid => !IsExpired && !IsClaimed`

### Domain Events

- **GuestSessionCreated**: Trigger: new GuestSession row inserted — Payload: SessionId, Email
- **GuestSessionClaimed**: Trigger: ClaimedByUserId set — Payload: SessionId, UserId

### Domain Services

- **IGuestSessionService**: Operations: `CreateAsync(request, ct) → CreateGuestSessionResponse`, `ClaimAsync(guestToken, userId, ct) → void`, `CleanupExpiredAsync(ct) → int`

### Authorization Concept

- **GuestAuthenticationHandler**: Custom `AuthenticationHandler<AuthenticationSchemeOptions>` — reads `X-Guest-Token` header, validates UUID, queries DB for valid (non-expired, non-claimed) session, builds ClaimsPrincipal
- **DualAuthPolicy**: Accepts EITHER `JwtBearerDefaults.AuthenticationScheme` OR `"GuestToken"` scheme. Bearer JWT takes precedence.

### Repository Interfaces

- **GuestSession** (via EF DbSet): Methods: `FindActiveByIdAsync(id)`, batch delete expired unclaimed

### Ubiquitous Language

- **Guest Token**: Raw GuestSession.Id UUID — passed in `X-Guest-Token` header
- **Claiming**: Linking a guest session to an authenticated user after registration
- **Dual-auth**: Endpoints accept EITHER Bearer JWT OR X-Guest-Token
- **Orphaned Session**: Expired, unclaimed, no linked orders — eligible for cleanup
- **TTL**: 7 days from creation
