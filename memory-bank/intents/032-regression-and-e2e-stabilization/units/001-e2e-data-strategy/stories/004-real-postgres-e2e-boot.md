---
id: 004-real-postgres-e2e-boot
unit: 001-e2e-data-strategy
intent: 032-regression-and-e2e-stabilization
status: draft
priority: should
created: 2026-06-05T11:30:00Z
assigned_bolt: 070-e2e-data-strategy
implemented: false
---

# Story: 004-real-postgres-e2e-boot

## User Story

**As a** maintainer who knows the EF InMemory test gap exists
**I want** the e2e suite to boot the app against real PostgreSQL via docker-compose
**So that** the journeys exercise the production-shaped database, not a different provider

## Acceptance Criteria

- [ ] **Given** the e2e harness, **When** it boots, **Then** the API runs against the compose Postgres (reusing `docker-compose.yml`'s db service), not EF InMemory
- [ ] **Given** a fresh compose DB, **When** the API boots, **Then** EF Core migrations apply (`Database.Migrate()`), and the seed runs, leaving the contracted data in place
- [ ] **Given** a migration that is broken on Postgres, **When** it is applied against the compose Postgres, **Then** the e2e boot fails loudly (surfacing what the EF InMemory suites cannot catch) rather than silently passing
- [ ] **Given** the boot pattern, **When** documented, **Then** it is the one the unit-002 specs and the CI workflow (story 008) both reuse

## Technical Notes

- Reuse bolt 066's docker-compose boot but pin the API to the Postgres provider; do not introduce a new compose file.
- This story deliberately does NOT close the InMemory-vs-Postgres parity gap (the `db-parity` review lens exists for it) — it makes the e2e suite the place the gap surfaces.

## Dependencies

### Requires
- bolt 066 (compose boot pattern)

### Enables
- 008-e2e-ci-tiers-and-stability (unit 002)
- All journey specs (run against real PG)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Compose Postgres slow to become healthy | Boot waits on the existing `pg_isready` healthcheck before seeding |
| Provider-inconsistent migration | Boot fails with a clear migration error → triaged in unit 003 |

## Out of Scope

- Fixing the broken migration (migration-guard / a future bolt).
- A real-Postgres *integration*-test profile (ai-workflow-review §2.3 — folds into bolt 062).
