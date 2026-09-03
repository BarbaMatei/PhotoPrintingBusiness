---
id: 001-global-rate-limit
unit: 001-access-hardening
intent: 029-decomposition-and-hardening
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 063-access-hardening
implemented: false
---

# Story: 001-global-rate-limit

## User Story

**As an** operator
**I want** a global per-IP rate limit on the non-auth API surface
**So that** an unauthenticated visitor can't hammer `/api/products` 1000×/sec

## Acceptance Criteria

- [ ] **Given** `AddRateLimiter`, **When** a global sliding-window limiter (~200 req/min/IP) is registered, **Then** it applies to `/api/*` as a fallback
- [ ] **Given** auth-specific policies, **When** the global limiter is added, **Then** they still override with their stricter limits
- [ ] **Given** the limiter partitions on the real client IP, **When** behind Caddy, **Then** it uses the forwarded IP (depends on intent 025 P05)
- [ ] **Given** an over-limit client, **When** it exceeds the window, **Then** it receives 429

## Technical Notes

- Tune the limit in a pre-launch load test; admin uploading 30 photos in 10s must not be throttled.

## Dependencies

### Requires
- 025/001/004-forwarded-headers-metrics (real client IP)

### Enables
- None

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Legitimate admin burst | Not throttled (tuned limit / per-route override) |
| Unknown IP | Partitioned under "unknown" bucket |

## Out of Scope

- Admin policy constant (next story).
