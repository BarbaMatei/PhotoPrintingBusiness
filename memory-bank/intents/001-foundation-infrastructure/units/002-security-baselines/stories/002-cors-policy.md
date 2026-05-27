---
id: 002-cors-policy
unit: 002-security-baselines
intent: 001-foundation-infrastructure
status: draft
priority: must
created: 2026-05-05T15:26:00Z
assigned_bolt: null
implemented: false
---

# Story: 002-cors-policy

## User Story

**As a** system
**I want** CORS restricted to the exact frontend origin
**So that** only our Angular SPA can make cross-origin API requests

## Acceptance Criteria

- [ ] **Given** a request from the configured frontend origin, **When** CORS is checked, **Then** the request is allowed with credentials
- [ ] **Given** a request from an unauthorized origin, **When** CORS is checked, **Then** the request is rejected
- [ ] **Given** a preflight OPTIONS request from the allowed origin, **When** processed, **Then** correct CORS headers are returned (AllowAnyHeader, AllowAnyMethod, AllowCredentials)
- [ ] **Given** production config, **When** CORS is configured, **Then** NO wildcard (*) is used for origins

## Technical Notes

- Read `AllowedOrigins` from configuration (comma-separated for multiple origins)
- `AllowCredentials()` required for HttpOnly cookie (refresh token)
- Register with `builder.Services.AddCors()` and apply with `app.UseCors()`

## Dependencies

### Requires
- None

### Enables
- JWT refresh token flow (requires AllowCredentials for HttpOnly cookie)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Multiple frontend origins (dev + prod) | Support comma-separated list in config |
| null origin (file:// or redirects) | Reject — do not allow null origin |

## Out of Scope

- Per-endpoint CORS overrides
