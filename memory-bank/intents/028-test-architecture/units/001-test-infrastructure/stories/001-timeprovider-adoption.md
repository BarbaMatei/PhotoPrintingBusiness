---
id: 001-timeprovider-adoption
unit: 001-test-infrastructure
intent: 028-test-architecture
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 062-test-infrastructure
implemented: false
---

# Story: 001-timeprovider-adoption

## User Story

**As a** developer testing token expiry and lockout logic
**I want** `TimeProvider` injected everywhere instead of raw `DateTimeOffset.UtcNow`
**So that** time-dependent behaviour is deterministically testable with `FakeTimeProvider`

## Acceptance Criteria

- [ ] **Given** the 35 older files (63 calls), **When** refactored, **Then** `DateTimeOffset.UtcNow` is replaced with `_clock.GetUtcNow()` — priority order: `AuthService` (13), `AccountService` (4), `AdminOrderService` (3), `OrderService` (1), `EuPlatescService` (3), all `BackgroundJobs/*` (6)
- [ ] **Given** a banned-API rule, **When** added, **Then** raw `DateTimeOffset.UtcNow` is forbidden in Application/Infrastructure
- [ ] **Given** each refactored service, **When** tested, **Then** ≥1 time-sensitive scenario uses `FakeTimeProvider` (no `Thread.Sleep`, no "within 5 seconds")
- [ ] **Given** `Models/*.cs` default-clock assignments, **When** kept as write-time fallback, **Then** Builders set the clock explicitly in tests

## Technical Notes

- Constructor-signature changes are absorbed by P27 Builders (story 003), not scattered across tests.

## Dependencies

### Requires
- None (ship first within this unit)

### Enables
- 002-shared-test-application-factory, 003-test-builders

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Model default clock in a test | Builder overrides with FakeTimeProvider |

## Out of Scope

- Replacing model default-clock assignments (kept as fallback).
