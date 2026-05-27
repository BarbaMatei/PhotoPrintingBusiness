---
intent: 001-foundation-infrastructure
phase: inception
status: complete
created: 2026-05-05T15:16:00Z
updated: 2026-05-05T15:35:00Z
---

# Requirements: Foundation & Infrastructure

## Intent Overview

Establish the foundational infrastructure that ALL other features depend on: global error handling & logging (backend), security baselines (backend), Angular app shell & routing (frontend), and email infrastructure (backend). These cross-cutting concerns must be implemented first to ensure consistent behavior across every future endpoint and component.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Consistent error responses across all API endpoints | 100% of errors return ProblemDetails (RFC 7807) format | Must |
| Production-grade security posture | Pass OWASP baseline checks (CORS, headers, rate limiting, HTTPS) | Must |
| Reusable Angular app shell for all features | All feature modules lazy-load into the shell with proper guards | Must |
| Switchable email delivery (dev/prod) | Emails send via MailHog (dev) and SendGrid (prod) with zero code changes | Must |

---

## Functional Requirements

### FR-1: Exception Handler Middleware
- **Description**: Catch all unhandled exceptions and return RFC 7807 ProblemDetails JSON with `type`, `title`, `status`, `detail`, `correlationId`
- **Acceptance Criteria**: NotFoundException→404, ConflictException→409, ForbiddenException→403, UnauthorizedException→401, ValidationException→422, unknown→500 (no internal details in production)
- **Priority**: Must

### FR-2: Correlation ID Middleware
- **Description**: Read `X-Correlation-Id` from request header or generate UUID; include in all responses and Serilog log entries
- **Acceptance Criteria**: Every response includes `X-Correlation-Id` header; all log entries contain the correlation ID
- **Priority**: Must

### FR-3: Structured Logging (Serilog)
- **Description**: Configure Serilog with console sink (dev, human-readable) and file sink (prod, JSON, daily rolling, 30-day retention)
- **Acceptance Criteria**: Logs enriched with CorrelationId, MachineName, ThreadId, RequestPath; sensitive data never logged
- **Priority**: Must

### FR-4: Health Check Endpoint
- **Description**: `GET /health` public endpoint checking DB connectivity and disk free space
- **Acceptance Criteria**: Returns `{ "status": "Healthy|Unhealthy", "db": "OK|Error", "diskFreeGb": number }`
- **Priority**: Must

### FR-5: FluentValidation Integration
- **Description**: Auto-validate request DTOs via FluentValidation; return 422 with `[{field, message}]` array
- **Acceptance Criteria**: Validation errors return 422 with field-level Romanian error messages
- **Priority**: Must

### FR-6: HTTPS & HSTS
- **Description**: Enforce HTTPS redirect; HSTS header with `max-age=31536000`, `includeSubDomains` in production
- **Acceptance Criteria**: All HTTP requests redirect to HTTPS; HSTS header present on all responses in production
- **Priority**: Must

### FR-7: CORS Policy
- **Description**: Restrict CORS to exact frontend origin(s); no wildcard; allow credentials for cookies
- **Acceptance Criteria**: Requests from unauthorized origins are rejected; credentials (refresh token cookie) allowed from configured origin
- **Priority**: Must

### FR-8: Rate Limiting
- **Description**: ASP.NET Core Rate Limiting middleware — 100 req/min per IP (public), 10 req/min per IP (auth endpoints)
- **Acceptance Criteria**: Exceeding limits returns 429; rate limits configurable via appsettings
- **Priority**: Must

### FR-9: Security Headers
- **Description**: Add `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, and `Content-Security-Policy` to all responses (not just admin)
- **Acceptance Criteria**: All four headers present on every API response; CSP restricts inline scripts, frame-ancestors, and object-src
- **Priority**: Must

### FR-10: Angular App Shell
- **Description**: Header (logo, nav, cart badge, login/avatar), main `router-outlet`, responsive footer with legal links. All components use modern Angular 17+ standalone component pattern (no NgModules).
- **Acceptance Criteria**: Shell renders on all routes; hamburger menu on mobile; cart badge shows item count; all components are standalone
- **Priority**: Must

### FR-11: Lazy-Loaded Route Groups
- **Description**: Feature routes using modern Angular 17+ standalone components with `loadComponent`/`loadChildren` (no NgModules); lazy-loaded at `/auth/*`, `/cos/*`, `/checkout/*`, `/comenzile-mele/*`, `/contul-meu/*`, `/admin/*`, plus legal pages
- **Acceptance Criteria**: Each feature loads on demand via standalone component routing; initial bundle excludes feature code; no NgModules used
- **Priority**: Must

### FR-12: Route Guards
- **Description**: AuthGuard (redirect to login + store returnUrl), AdminGuard (check role=Admin), GuestOrAuthGuard (allow JWT or guest token)
- **Acceptance Criteria**: Unauthenticated users redirected to `/auth/login`; non-admins redirected from `/admin/*`; checkout accessible with either auth method
- **Priority**: Must

### FR-13: HTTP Interceptors
- **Description**: JwtInterceptor (attach Bearer header, handle 401 refresh), GuestInterceptor (attach X-Guest-Token), ErrorInterceptor (toast for 403/5xx)
- **Acceptance Criteria**: All API calls automatically include correct auth header; 401 triggers silent refresh; 5xx shows Romanian error toast
- **Priority**: Must

### FR-14: Environment Configuration
- **Description**: `environment.ts` (dev) and `environment.prod.ts` with apiUrl, stripePublishableKey, googleClientId
- **Acceptance Criteria**: Angular build uses correct environment file per build configuration
- **Priority**: Must

### FR-15: IEmailService Abstraction
- **Description**: `IEmailService` with `SendAsync` and `SendTemplatedAsync<T>` methods; SmtpEmailService (MailKit, dev) and SendGridEmailService (prod) implementations switched via config
- **Acceptance Criteria**: Switching `Email:Provider` config changes delivery method without code changes
- **Priority**: Must

### FR-16: Email Razor Templates
- **Description**: Razor template rendering via RazorLight; shared `_Layout.cshtml` with FotoTipar logo and footer
- **Acceptance Criteria**: Templates render to HTML with dynamic model data; consistent layout across all email types
- **Priority**: Must

### FR-17: Email Retry Queue
- **Description**: `EmailRetryJob` (IHostedService) — database-backed queue of failed emails; retry with exponential backoff (1s, 4s, 16s); max 3 attempts. Failed sends persisted to `EmailQueue` table so retries survive app restarts.
- **Acceptance Criteria**: Failed emails persisted to DB and retried up to 3 times; final failure logged with Serilog; operator BCC on all emails; queue survives app restart
- **Priority**: Must

---

## Non-Functional Requirements

### Performance
| Requirement | Metric | Target |
|-------------|--------|--------|
| Middleware overhead | Added latency per request | < 1ms |
| Health check response | p95 latency | < 100ms |

### Security
| Requirement | Standard | Notes |
|-------------|----------|-------|
| HTTPS enforcement | TLS 1.2+ | HSTS with includeSubDomains |
| Rate limiting | OWASP | 100 req/min public, 10 req/min auth |
| Security headers | OWASP | nosniff, DENY, strict-origin |
| Secret management | OWASP | .NET Secret Manager (dev), env vars (prod) |
| CORS | OWASP | Exact origin whitelist, no wildcard |

### Reliability
| Requirement | Metric | Target |
|-------------|--------|--------|
| Email delivery | Retry success rate | > 95% within 3 retries |
| Health check | Availability | Always responsive even if DB down |

---

## Constraints

### Technical Constraints
- All middleware must be registered in correct pipeline order in `Program.cs`
- Email retry queue is database-backed (persists across app restarts)
- FluentValidation messages must be in Romanian

### Business Constraints
- No additional infrastructure costs — MailHog for dev (free), SendGrid free tier for MVP
- Must be completed before any feature epic begins
