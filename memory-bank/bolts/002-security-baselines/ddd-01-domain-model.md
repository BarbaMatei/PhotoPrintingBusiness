---
unit: 002-security-baselines
bolt: 002-security-baselines
stage: model
status: complete
updated: 2026-05-19T00:00:00Z
---

# Static Model - Security Baselines

## Bounded Context

The **Security** bounded context owns the request-level security posture of the API. It is pure infrastructure — no entities are persisted, no business logic lives here. It provides four capabilities:

1. **HTTPS Enforcement** — redirect HTTP → HTTPS and set HSTS on all production responses
2. **CORS Policy** — allow only whitelisted frontend origins with credentials
3. **Rate Limiting** — protect endpoints from abuse via per-IP fixed-window limits
4. **Security Headers** — add OWASP-baseline response headers (including CSP) on every response

This context has no external domain dependencies. It is consumed by every request before reaching any controller or business logic.

---

## Entities

*None.* Security baselines are stateless infrastructure configuration — no entities are persisted or tracked.

---

## Value Objects

| Value Object | Properties | Constraints |
|--------------|------------|-------------|
| **RateLimitPolicy** | `Name` (string), `PermitLimit` (int), `Window` (TimeSpan) | `PermitLimit` > 0; `Window` > zero; `Name` must be unique within the app |
| **CorsSettings** | `AllowedOrigins` (string[]), `AllowCredentials` (bool) | `AllowedOrigins` must be non-empty; no wildcard (`"*"`) entry permitted; each entry must be an absolute URI with scheme |
| **HstsSettings** | `MaxAge` (TimeSpan), `IncludeSubDomains` (bool) | `MaxAge` ≥ 1 year (31536000s) per OWASP recommendation; only applied in Production environment — never in Development |
| **SecurityHeadersOptions** | `ContentSecurityPolicy` (string), `FrameOptions` (string), `ContentTypeOptions` (string), `ReferrerPolicy` (string) | All fields non-empty; `ContentTypeOptions` fixed to `"nosniff"`; `FrameOptions` fixed to `"DENY"`; CSP must be configurable via `appsettings.json` |

---

## Aggregates

*None.* All configuration is loaded at startup from `IConfiguration` and held in options objects. There is no runtime mutation of security configuration.

---

## Domain Events

*None persisted or published.* Security events are emitted as structured log entries only:

| Log Event | Trigger | Severity |
|-----------|---------|----------|
| `CORS request rejected` | Origin not in whitelist | Warning |
| `Rate limit exceeded` | Client IP exceeds permit limit | Warning |
| `Security headers applied` | Each response (sampled) | Debug |

---

## Domain Services

| Service | Responsibility | Inputs | Outputs |
|---------|---------------|--------|---------|
| **SecurityHeadersMiddleware** | Adds all 4 security headers to every HTTP response | `HttpContext`, `SecurityHeadersOptions` (from `IOptions<>`) | Response headers mutated before write |
| **HTTPS/HSTS (built-in)** | Redirects HTTP → HTTPS; sets Strict-Transport-Security header | `HttpContext`, `HstsOptions` | 301 redirect or HSTS header |
| **CORS (built-in)** | Validates Origin header; returns `Access-Control-*` headers or rejects | `HttpContext`, named CORS policy | CORS headers or rejection |
| **Rate Limiter (built-in)** | Tracks per-IP request count in a fixed window; returns 429 on exhaustion | `HttpContext`, named rate limit policy | Permit or 429 + `Retry-After` |

---

## Middleware Execution Order

Security middleware **must** execute in this order within the ASP.NET Core pipeline:

```text
1. CorrelationId          (from bolt 001)
2. ExceptionHandler       (from bolt 001)
3. SerilogRequestLogging  (from bolt 001)
4. HttpsRedirection       (this bolt — redirects before any security headers)
5. HSTS                   (this bolt — production only)
6. SecurityHeaders        (this bolt — before routing so headers are on all responses)
7. CORS                   (this bolt — before routing, after security headers)
8. RateLimiting           (this bolt — after CORS, before routing)
9. Routing / Auth / Controllers
```

*Order is an invariant: HSTS before headers; headers before CORS; rate limiting after CORS to avoid counting preflight rejections.*

---

## Repository Interfaces

*None.* All configuration is loaded from `IConfiguration` at startup via `IOptions<T>` — no repository pattern required.

---

## Configuration Contracts

These are the `appsettings.json` keys that drive security behaviour at runtime:

| Key | Type | Required | Description |
|-----|------|----------|-------------|
| `Cors:AllowedOrigins` | comma-separated string | Yes | Exact frontend URLs (e.g., `https://fotipar.ro`) |
| `RateLimit:Public:PermitLimit` | int | No (default 100) | Requests/min per IP for public endpoints |
| `RateLimit:Auth:PermitLimit` | int | No (default 10) | Requests/min per IP for auth endpoints |
| `SecurityHeaders:ContentSecurityPolicy` | string | No (default provided) | Full CSP header value |

---

## Ubiquitous Language

| Term | Definition |
|------|-----------|
| **HSTS** | HTTP Strict Transport Security — a response header that tells browsers to only use HTTPS for the domain for the specified duration |
| **CORS** | Cross-Origin Resource Sharing — a browser mechanism that restricts which origins can call an API; the API controls this via response headers |
| **CSP** | Content Security Policy — a header that restricts what resources the browser can load, mitigating XSS and injection attacks |
| **Preflight** | An OPTIONS request browsers send before a cross-origin request; CORS middleware must return correct headers to permit the actual request |
| **Fixed Window** | A rate limiting strategy where the request count resets at a fixed interval (e.g., every 60 seconds) |
| **Permit Limit** | The maximum number of requests allowed within one rate limit window |
| **Allowed Origin** | An exact URL (with scheme, host, and optional port) that is whitelisted for cross-origin access |
| **Production Guard** | A check that restricts certain middleware (HSTS) to non-Development environments to prevent developer certificate issues |
