# System Architecture

## Overview
FotoTipar follows a classic client-server architecture with an Angular SPA frontend communicating with an ASP.NET Core 8 REST API backend, backed by PostgreSQL 16. Real-time features use SignalR.

## Architecture Style

**Pattern**: Monolithic REST API + SPA frontend

The backend is a single ASP.NET Core 8 Web API serving all domains (auth, orders, uploads, payments, admin). This is appropriate for the project's scale — a single-product e-commerce site with a small team. The frontend is a standalone Angular SPA that communicates exclusively via REST API calls.

### Rationale
- Single deployment unit simplifies ops for a small team
- Clear separation between frontend and backend via API
- Can extract microservices later if specific domains need independent scaling

## API Design

**Style**: REST with resource-oriented URLs
**Format**: JSON request/response bodies
**Auth**: Bearer JWT or X-Guest-Token header per request
**Errors**: ProblemDetails (RFC 7807)
**Versioning**: URL prefix (`/api/v1/`) when breaking changes needed — currently unversioned (`/api/`)

## State Management

### Frontend
- **Simple state**: `BehaviorSubject` in Angular services (`CartService`, `AuthService`)
- **No NgRx** — complexity not warranted at this scale
- **Cart**: localStorage for guests, server-synced for logged-in users
- **Auth state**: JWT decoded claims in `AuthService`, refresh token in HttpOnly cookie

### Backend
- **Stateless API**: no server-side sessions; all state in JWT claims or database
- **Database as source of truth**: cart, orders, user data all persisted in PostgreSQL

## Caching Strategy

- **Locker list**: 24-hour server-side cache (lockers don't change frequently)
- **Product catalog**: in-memory cache with short TTL (products rarely change)
- **No CDN caching** of API responses — all data is user-specific
- **Frontend**: Angular `HttpClient` does not cache by default; no additional cache layer needed at MVP

## Security Patterns

### Authentication
- JWT RS256 with short-lived access tokens (15 min)
- Refresh tokens: rotated on use, SHA-256 hashed in database, 30-day expiry
- Refresh token in HttpOnly, Secure, SameSite=Strict cookie
- Google OAuth: verify Google `id_token` server-side, issue own JWT

### Authorization
- Role-based: `Customer` (default), `Admin`, `Guest`
- Controllers use `[Authorize]` and `[Authorize(Roles="Admin")]`
- Guest endpoints validate `X-Guest-Token` via custom `GuestAuthenticationHandler`

### Data Protection
- All passwords: bcrypt hashed
- Guest tokens: SHA-256 hashed in database
- CORS: restricted to frontend origin
- Security headers: HSTS, X-Content-Type-Options, X-Frame-Options via middleware
- Rate limiting on auth endpoints (login, register, password reset)
- File uploads: validate MIME magic bytes, enforce size limits (50MB per file, 30 files max)
- GDPR consent required at registration

### Payment Security
- Stripe: card data never touches server (Stripe Elements); webhook signature verification
- EuPlatesc: HMAC-MD5 signature verification on IPN callbacks
- Idempotent webhook handling (prevent duplicate processing)

## Integration Architecture

```text
┌─────────────┐     REST/JSON     ┌──────────────────┐
│  Angular    │ ←───────────────→ │  ASP.NET Core 8  │
│  SPA        │                   │  Web API          │
│  (CDN)      │     SignalR       │                   │
│             │ ←─────────────── →│  SignalR Hub      │
└─────────────┘                   └──────┬───────────┘
                                         │
                          ┌──────────────┼──────────────┐
                          │              │              │
                    ┌─────▼─────┐ ┌──────▼─────┐ ┌─────▼──────┐
                    │PostgreSQL │ │  Stripe    │ │  Sameday   │
                    │  16       │ │  API       │ │  API       │
                    └───────────┘ └────────────┘ └────────────┘
                                  ┌────────────┐ ┌────────────┐
                                  │ EuPlatesc  │ │ SendGrid   │
                                  │ (redirect) │ │ / MailKit  │
                                  └────────────┘ └────────────┘
```

### External Service Patterns
- All external API keys: server-side only (environment variables)
- `IHttpClientFactory` for external HTTP calls (Sameday, Google token verification)
- Retry with exponential backoff on transient errors
- Abstraction interfaces for all integrations (`IEmailService`, `IStorageService`, `IShippingService`)

## Order Lifecycle State Machine

```text
AwaitingPayment → Paid → Printing → Shipped → Delivered
       │                    │
       ▼                    ▼
  PaymentFailed        Cancelled (+ refund)
```

Valid transitions enforced by `OrderStatusMachine` — invalid transitions return 400.

## Decision Relationships
- Monolithic architecture keeps deployment simple; SignalR for real-time avoids a separate WebSocket service
- Stateless API with JWT allows horizontal scaling when needed
- All external integrations behind interfaces enables easy testing and future provider swaps
