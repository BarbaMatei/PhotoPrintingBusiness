---
id: 003-guest-session-cleanup
unit: 003-guest-sessions
intent: 002-authentication
status: complete
priority: must
created: 2026-05-20T12:57:00Z
assigned_bolt: 007-guest-sessions
implemented: true
---

# Story: 003-guest-session-cleanup

## User Story

**As a** platform operator
**I want** abandoned guest sessions to be automatically removed
**So that** the database doesn't accumulate orphaned rows indefinitely

## Acceptance Criteria

- [ ] **Given** a `GuestSessionCleanupJob` background service, **When** it runs (every hour), **Then** all `GuestSession` rows where `ExpiresAt < UtcNow` AND `ClaimedByUserId IS NULL` AND no linked orders exist are deleted
- [ ] **Given** a guest session has at least one linked order, **When** the cleanup runs, **Then** the session row is NOT deleted (order reference must be preserved)
- [ ] **Given** the cleanup job, **When** it starts, **Then** it logs the number of rows deleted at `Information` level
- [ ] **Given** the cleanup job, **When** an unhandled exception occurs, **Then** it is caught, logged at `Error` level with correlation context, and the job continues on the next cycle (does not crash the host)

## Technical Notes

- Implement as `BackgroundService` (inherits `IHostedService`) using `PeriodicTimer` with 1-hour interval
- EF Core query: `WHERE ExpiresAt < UtcNow AND ClaimedByUserId IS NULL AND NOT EXISTS (SELECT 1 FROM Orders WHERE GuestSessionId = Id)`
- Batch delete to avoid long transactions on large tables
- Registration: `services.AddHostedService<GuestSessionCleanupJob>()`

## Dependencies

### Requires
- Story 001-guest-session-create (GuestSession table)
- Bolt 001 (Serilog structured logging)

### Enables
- Nothing (maintenance operation)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Cleanup runs while a guest session is being claimed | Transaction on claim completes first; cleanup skips the now-claimed session |
| No expired sessions | Logs "0 guest sessions cleaned up" — no error |

## Out of Scope

- Claimed session cleanup (claimed sessions are retained for order history)
- Email notification on session expiry
