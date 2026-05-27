---
id: 003-rate-limiting
unit: 002-security-baselines
intent: 001-foundation-infrastructure
status: draft
priority: must
created: 2026-05-05T15:26:00Z
assigned_bolt: null
implemented: false
---

# Story: 003-rate-limiting

## User Story

**As a** system
**I want** rate limiting on API endpoints to prevent abuse
**So that** brute-force attacks and DDoS are mitigated

## Acceptance Criteria

- [ ] **Given** a client IP making public API requests, **When** 100 requests are made within 1 minute, **Then** subsequent requests receive 429 Too Many Requests
- [ ] **Given** a client IP making auth endpoint requests (login, register, password-reset), **When** 10 requests are made within 1 minute, **Then** subsequent requests receive 429
- [ ] **Given** a 429 response, **When** examined, **Then** it includes `Retry-After` header
- [ ] **Given** rate limit configuration, **When** changed in appsettings, **Then** limits update without code changes

## Technical Notes

- Use ASP.NET Core Rate Limiting middleware (`Microsoft.AspNetCore.RateLimiting`)
- Define two named policies: `"public"` (100/min) and `"auth"` (10/min)
- Use `AddFixedWindowLimiter` with `Window = TimeSpan.FromMinutes(1)`
- Apply `"auth"` policy to auth controllers via `[EnableRateLimiting("auth")]`
- Default policy: `"public"` for all other endpoints

## Dependencies

### Requires
- None

### Enables
- Secure auth endpoints (login, register, password reset)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Behind load balancer (shared IP) | Use `X-Forwarded-For` for real client IP |
| Health check endpoint | Exempt from rate limiting or use generous limit |
| Distributed deployment | Rate limiting is per-instance (acceptable for MVP single-instance) |

## Out of Scope

- Distributed rate limiting (Redis-backed) — future if needed
- Per-user rate limiting (requires auth context)
