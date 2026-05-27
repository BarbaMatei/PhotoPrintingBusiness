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
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Unexpected server failure |

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

- Endpoints accept EITHER Bearer JWT OR X-Guest-Token — never both
- `JwtInterceptor` (Angular) attaches Bearer header automatically
- `GuestInterceptor` (Angular) attaches X-Guest-Token when no JWT present
- 401 response triggers JWT refresh flow (silent refresh via HttpOnly cookie)

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
- Max files: 30 per request
- Max file size: 50MB each
- Accepted MIME types: JPEG, PNG, HEIC (validated by magic bytes, not extension)

## Decision Relationships
- ProblemDetails (RFC 7807) provides a standard error format recognized by HTTP tooling
- Offset-based pagination is simple and sufficient for the expected data volumes
- Field-level validation errors enable direct mapping to Angular reactive form controls
- Romanian error messages avoid the need for frontend translation logic
