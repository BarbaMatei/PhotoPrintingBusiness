# API Conventions

## Overview
REST API conventions for FotoTipar's ASP.NET Core 8 backend, consumed by the Angular frontend. All endpoints follow consistent patterns for URLs, authentication, pagination, and error handling.

## API Style

**Style**: REST with resource-oriented URLs
**Base path**: `/api`
**Content type**: `application/json` (requests and responses)
**File uploads**: `multipart/form-data`
**Controller routing**: `[Route("api/[controller]")]` with `[ApiController]` attribute

### URL Patterns

| Pattern | Example | Description |
|---------|---------|-------------|
| `GET /api/{resource}` | `GET /api/orders` | List resources (paginated) |
| `GET /api/{resource}/{id}` | `GET /api/orders/{id}` | Get single resource |
| `POST /api/{resource}` | `POST /api/orders` | Create resource |
| `PUT /api/{resource}/{id}` | `PUT /api/products/{id}` | Full update |
| `PATCH /api/{resource}/{id}` | `PATCH /api/orders/{id}/status` | Partial update / action |
| `DELETE /api/{resource}/{id}` | `DELETE /api/cart/{id}` | Delete resource |

### Naming Rules
- Resource names: plural nouns, lowercase (`orders`, `products`, `cart`)
- Actions on resources: verb suffix on PATCH (`/status`, `/cancel`)
- Nested resources: `GET /api/orders/{id}/items`

## Versioning

**Strategy**: No versioning at MVP — unversioned `/api/` prefix
**Future**: URL prefix versioning (`/api/v2/`) when breaking changes are introduced
**Rationale**: Single client (Angular SPA) controlled by the same team; versioning adds unnecessary complexity until needed

## Response Format

### Success Responses

**Single resource:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "orderNumber": "FT-20260001",
  "status": "Paid",
  "total": 45.50,
  "createdAt": "2026-05-05T10:30:00Z"
}
```

**Collection (paginated):**
```json
{
  "items": [...],
  "total": 42,
  "page": 1,
  "size": 20
}
```

**Re-validated state on a read (bolt 047):** where a stored selection can go bad on its own
(a coupon expiring, running out, or being deactivated while it sits in a cart), the read model
carries its state instead of the read silently repairing or dropping it. `GET /api/cart` and
`POST /api/cart` both return `couponStatus` (`"valid"` | `"stale"`) and, when stale,
`couponReason` — the **same** code the write path would return — with `discountRon` recomputed
to `0`. Reads never write: the stored selection survives so the customer can see it and remove
it themselves. The write path (checkout) stays the authority and still refuses with `409` and
the same code.

### Status Code Usage

| Code | Meaning | When |
|------|---------|------|
| 200 | OK | Successful GET, PUT, PATCH |
| 201 | Created | Successful POST (with `Location` header) |
| 204 | No Content | Successful DELETE |
| 400 | Bad Request | Invalid request / business rule violation |
| 401 | Unauthorized | Missing or invalid auth token |
| 403 | Forbidden | Authenticated but insufficient permissions |
| 404 | Not Found | Resource doesn't exist |
| 409 | Conflict | Duplicate resource (e.g., email already registered) |
| 422 | Unprocessable Entity | Validation errors |
| 413 | Payload Too Large | Upload exceeds size limits |
| 415 | Unsupported Media Type | Wrong file type (magic-byte check) |
| 429 | Too Many Requests | Rate limit exceeded (`Retry-After` header set) |
| 500 | Internal Server Error | Unexpected server failure |
| 502 | Bad Gateway | Upstream dependency unreachable (e.g. Google token verification) |

409 vs 422: a structurally-valid request conflicting with persisted state is **409**
(ADR-004 — e.g. idempotency divergence, carrying `divergentFields`); a malformed/invalid
request is **422** (ADR-002). Full exception→status map: `Middleware/ExceptionHandlerMiddleware.cs`.

### Idempotency (payments — bolt 035)

`Idempotency-Key` request header (trimmed, ≤80 chars, enforced by `IdempotencyKeyFilter`),
persisted on `Orders.IdempotencyKey` (globally-unique index), 24-hour replay window. Same key +
same logical request (ADR-005: processor, delivery type, locker, total — shipping address
excluded) → replays the cached client secret / redirect URL. Same key + divergent request →
409 with `divergentFields`.

## Error Response Format

### Standard Errors (RFC 7807 ProblemDetails)

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Not Found",
  "status": 404,
  "detail": "Comanda nu a fost găsită",
  "correlationId": "abc-123-def-456"
}
```

### Validation Errors (422)

```json
{
  "errors": [
    { "field": "email", "message": "Adresa de email nu este validă" },
    { "field": "password", "message": "Parola trebuie să aibă minim 8 caractere" }
  ]
}
```

### Machine-readable Error Codes (`code`)

Two different 422 shapes exist and clients must handle both:

- **DTO/model validation** → the field-level `errors[]` array above.
- **A well-formed request the domain refuses** → ProblemDetails plus a `code` extension.

Any exception implementing `IErrorCoded` (`CouponRejectedException` → 422,
`CouponConflictException` → 409) surfaces its code as a `code` extension in both the
production ProblemDetails and the Development diagnostic shape:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Unprocessable Entity",
  "status": 422,
  "detail": "Codul introdus nu este valid.",
  "correlationId": "abc-123-def-456",
  "code": "INVALID_COUPON"
}
```

Clients branch on `code`, never on `detail` — `detail` is Romanian user-facing copy and may be
reworded at any time. Codes are `SCREAMING_SNAKE_CASE` and live in one constants class per
domain (`CouponErrorCodes`). Deliberately indistinguishable causes share a code: an unknown,
inactive, expired or not-yet-started coupon all return `INVALID_COUPON`, so a code cannot be
used to probe which codes exist.

### Error Conventions
- All error `detail` messages in **Romanian** (user-facing)
- Always include `correlationId` for traceability
- Never expose stack traces, internal paths, or SQL in error responses
- Validation errors return field-level messages as an array

## Pagination Strategy

**Style**: Offset-based with query parameters

### Parameters

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `page` | int | 1 | Page number (1-indexed) |
| `size` | int | 20 | Items per page (max: 100) |

### Response Envelope

```json
{
  "items": [...],
  "total": 142,
  "page": 1,
  "size": 20
}
```

Frontend can calculate total pages: `Math.ceil(total / size)`

## Authentication Headers

| Scenario | Header |
|----------|--------|
| Logged-in user | `Authorization: Bearer <jwt>` |
| Guest user | `X-Guest-Token: <token>` |
| Public endpoint | No auth header |

- Endpoints accept EITHER Bearer JWT OR X-Guest-Token — never both; the backend policy is
  `DualAuth` (Bearer or GuestToken scheme)
- `jwtInterceptor` (Angular) attaches the Bearer header; `guestInterceptor` attaches
  X-Guest-Token when unauthenticated — both only for `environment.apiUrl` requests
- The refresh token lives in an HttpOnly SameSite=Strict cookie scoped to `Path=/api/auth` —
  but **the SPA has no silent-refresh flow**: on 401 the error interceptor logs out
  (authenticated) or clears the guest token (guest/anon). Don't design against a refresh flow
  that doesn't exist.
- **Ownership convention: 403 for non-owner** (`ForbiddenException`), not 404 — codebase-wide
  precedent (reviews 043-F10); resource IDs are unguessable GUIDs, enumeration risk accepted.

## Date/Time Convention

- All dates: **ISO 8601 UTC** strings in JSON (`2026-05-05T10:30:00Z`)
- Frontend displays in Romanian locale format (`dd.MM.yyyy`)
- Backend stores as `DateTimeOffset` (UTC)

## Request Conventions

### DTOs
- Separate Request and Response DTOs — never expose EF entities
- Use C# records for immutable DTOs: `public record RegisterRequest(string Email, ...)`
- Validate with FluentValidation — automatic via `ValidationFilter`
- Validation messages in Romanian

### File Uploads
- Endpoint: `POST /api/uploads`
- Content type: `multipart/form-data`
- Max file size: 50MB each; 500MB per batch; 100 uploads per guest session
- Accepted types: **JPG, PNG** (validated by magic bytes, not extension). HEIC was removed in
  bolt 042 — no decoder exists and the frontend rejects it.

## Decision Relationships
- ProblemDetails (RFC 7807) provides a standard error format recognized by HTTP tooling
- Offset-based pagination is simple and sufficient for the expected data volumes
- Field-level validation errors enable direct mapping to Angular reactive form controls
- Romanian error messages avoid the need for frontend translation logic
