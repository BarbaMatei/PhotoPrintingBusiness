---
stage: design
bolt: 007-guest-sessions
created: 2026-05-20T15:35:00Z
---

## Technical Design: guest-sessions

### Architecture Pattern
Layered architecture: Controller → Service → EF Core DbContext. Plus a secondary `AuthenticationHandler` plugged into ASP.NET Core's auth pipeline and a `BackgroundService` for cleanup.

### Layer Structure
```
┌────────────────────────────────────────────────────┐
│  Presentation (AuthController)                     │  POST /api/auth/guest, /guest/claim
├────────────────────────────────────────────────────┤
│  Application (GuestSessionService)                 │  Create, Claim
├────────────────────────────────────────────────────┤
│  Authentication (GuestAuthenticationHandler)       │  X-Guest-Token → ClaimsPrincipal
├────────────────────────────────────────────────────┤
│  Background (GuestSessionCleanupJob)               │  PeriodicTimer, 1h, ExecuteDeleteAsync
├────────────────────────────────────────────────────┤
│  Infrastructure (PhotoPrintDbContext)              │  GuestSessions DbSet
└────────────────────────────────────────────────────┘
```

### API Design

**POST /api/auth/guest** — no auth
- Request: `{ firstName, lastName, email, phone }`
- Response 200: `{ guestToken: "uuid" }`
- Response 422: validation error

**POST /api/auth/guest/claim** — `[Authorize]` (Bearer JWT only)
- Request: `{ guestToken: "uuid" }`
- Response 200: no body
- Response 400: "Sesiunea de oaspete este invalidă sau a fost deja revendicată"
- Response 401: no valid Bearer JWT

### New Files

| File | Description |
|------|-------------|
| `Models/GuestSession.cs` | EF Core entity, Id = token |
| `Exceptions/BadRequestException.cs` | 400 exception |
| `DTOs/Auth/CreateGuestSessionRequest.cs` | firstName/lastName/email/phone |
| `DTOs/Auth/CreateGuestSessionResponse.cs` | { GuestToken: Guid } |
| `DTOs/Auth/ClaimGuestSessionRequest.cs` | { GuestToken: Guid } |
| `Validators/Auth/CreateGuestSessionRequestValidator.cs` | Romanian phone regex |
| `Validators/Auth/ClaimGuestSessionRequestValidator.cs` | NotEqual(Guid.Empty) |
| `Services/IGuestSessionService.cs` + `GuestSessionService.cs` | Create + Claim |
| `Authentication/GuestAuthenticationHandler.cs` | X-Guest-Token scheme |
| `Extensions/GuestSessionExtensions.cs` | AddGuestSessions() |
| `BackgroundJobs/GuestSessionCleanupJob.cs` | PeriodicTimer + ExecuteDeleteAsync |

### Data Model

Table: `guest_sessions`
- `id` uuid PK (IS the token, set by application)
- `email` varchar(256) NOT NULL
- `first_name` varchar(100) NOT NULL
- `last_name` varchar(100) NOT NULL
- `phone` varchar(20) NOT NULL
- `created_at` timestamptz NOT NULL
- `expires_at` timestamptz NOT NULL
- `claimed_by_user_id` uuid NULL FK→users(id) ON DELETE SET NULL
- INDEX on `expires_at` (cleanup queries)
- INDEX on `claimed_by_user_id` (claim lookup)

EF Migration: `AddGuestSessionTable`

### Authorization

- `GuestAuthenticationHandler` scheme name: `"GuestToken"`
- `DualAuth` policy: AddAuthenticationSchemes(Bearer, GuestToken).RequireAuthenticatedUser()
- Bearer JWT takes precedence (registered as default scheme first)

### Security Design

- Guest token = UUID (non-guessable 128-bit, 7-day TTL, low privilege)
- Handler short-circuits on invalid UUID before any DB call
- Claim endpoint requires real Bearer JWT — guests cannot claim their own sessions

### NFR Implementation

- `ExecuteDeleteAsync()` → single SQL DELETE, no entity loading
- `NoResult()` returned immediately if no X-Guest-Token header present (no DB cost)
