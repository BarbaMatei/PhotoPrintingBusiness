---
id: 002-background-job-liveness-check
unit: 002-system-manifest-and-liveness
intent: 026-observability-boot-manifest
status: draft
priority: must
created: 2026-06-05T09:30:00Z
assigned_bolt: 056-system-manifest-and-liveness
implemented: false
---

# Story: 002-background-job-liveness-check

## User Story

**As an** operator
**I want** a health check that detects a silently-dead background job
**So that** a swallowed exception in `InvoiceUploadJob` doesn't go unnoticed for hours

## Acceptance Criteria

- [ ] **Given** `IHeartbeat`, **When** each `BackgroundService` ticks, **Then** it calls `Beat(jobName)` and `Snapshot()` exposes last-beat timestamps
- [ ] **Given** `BackgroundJobLivenessCheck`, **When** a heartbeat is older than 3× the job's scheduled interval, **Then** the check reports Degraded
- [ ] **Given** a test that stops a job, **When** the check runs, **Then** it degrades for that job only
- [ ] **Given** all jobs healthy, **When** the check runs, **Then** it reports Healthy

## Technical Notes

- Register a heartbeat per hosted service; staleness multiplier default 3× (allow per-job override).

## Dependencies

### Requires
- None (independent of the manifest)

### Enables
- None

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Job legitimately idle (no work) | Beat still fires per tick even with no items |
| Host shutting down | Check tolerates graceful drain |

## Out of Scope

- ANAF metrics (next story).
