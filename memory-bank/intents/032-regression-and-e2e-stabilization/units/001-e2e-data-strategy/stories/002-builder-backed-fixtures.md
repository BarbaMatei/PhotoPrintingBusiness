---
id: 002-builder-backed-fixtures
unit: 001-e2e-data-strategy
intent: 032-regression-and-e2e-stabilization
status: draft
priority: must
created: 2026-06-05T11:30:00Z
assigned_bolt: 070-e2e-data-strategy
implemented: false
---

# Story: 002-builder-backed-fixtures

## User Story

**As a** developer authoring e2e journey specs
**I want** Playwright fixtures that provide guest, registered-user, and admin contexts deterministically
**So that** each journey starts from a known auth state without repeating login boilerplate

## Acceptance Criteria

- [ ] **Given** `@playwright/test`, **When** fixtures are defined, **Then** there are `guest`, `authenticatedUser`, and `admin` fixtures yielding the appropriate browser context (cookies/tokens/guest header set)
- [ ] **Given** a spec needs an isolated entity (e.g. a fresh user or order), **When** it requests one, **Then** the fixture provisions it via bolt 062's fluent Builders with a Builder-generated unique identifier so specs do not contend
- [ ] **Given** two specs run in the same suite run, **When** both use the `authenticatedUser` fixture, **Then** they receive distinct, non-colliding users (no shared mutable state)
- [ ] **Given** the suite is run twice on the same seeded DB, **When** results are compared, **Then** they are identical (idempotent setup; teardown leaves no residue that breaks a re-run)

## Technical Notes

- Reuse bolt 062 Builders for per-spec data; reuse the documented seed (story 001) for shared read-only data (catalog, lockers).
- Guest fixture sets the `X-Guest-Token`; user/admin fixtures perform the real login flow once and reuse storage state.

## Dependencies

### Requires
- 001-e2e-data-contract
- bolt 062 (Builders)

### Enables
- All unit-002 journey specs

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Builder identifier collision | Builder generates unique values (e.g. GUID-suffixed email) — no collision |
| Fixture teardown fails mid-run | Setup is idempotent; next run self-heals without manual DB reset |

## Out of Scope

- Payment test-mode fixtures (story 003).
- The journeys themselves (unit 002).
