---
id: 004-health-check-endpoint
unit: 001-error-handling-logging
intent: 001-foundation-infrastructure
status: draft
priority: must
created: 2026-05-05T15:25:00Z
assigned_bolt: null
implemented: false
---

# Story: 004-health-check-endpoint

## User Story

**As an** operations engineer
**I want** a health check endpoint that reports database and disk status
**So that** monitoring tools can detect infrastructure issues before they impact users

## Acceptance Criteria

- [ ] **Given** the database is reachable, **When** `GET /health` is called, **Then** response is `{ "status": "Healthy", "db": "OK", "diskFreeGb": 45.2 }` with 200 status
- [ ] **Given** the database is unreachable, **When** `GET /health` is called, **Then** response is `{ "status": "Unhealthy", "db": "Error", "diskFreeGb": 45.2 }` with 503 status
- [ ] **Given** any auth state, **When** `GET /health` is called, **Then** it succeeds (public endpoint, no auth required)
- [ ] **Given** the health check is called, **When** response is generated, **Then** it completes within 100ms (no long timeouts on DB check)

## Technical Notes

- Register ASP.NET Core health checks in `Program.cs`
- Create custom `DbHealthCheck` using `PhotoPrintDbContext.Database.CanConnectAsync()` with a 5-second timeout
- Disk check: use `DriveInfo` to get free space on the uploads volume
- Map to `/health` route with `app.MapHealthChecks()`
- No authentication on this endpoint

## Dependencies

### Requires
- None (but benefits from Serilog for logging health check failures)

### Enables
- External monitoring tools and uptime services

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| DB check hangs | 5-second timeout; report "Error" and return 503 |
| Disk path not configured | Report diskFreeGb as -1 or skip disk check |
| Multiple drives | Report free space for the configured uploads path |

## Out of Scope

- Detailed dependency health checks (Stripe, SendGrid availability)
- Health check UI dashboard
