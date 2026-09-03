---
unit: 001-e2e-data-strategy
intent: 032-regression-and-e2e-stabilization
phase: inception
status: stories-defined
unit_type: frontend
default_bolt_type: simple-construction-bolt
created: 2026-06-05T11:20:00Z
updated: 2026-06-05T11:30:00Z
---

# Unit Brief: E2E Data Strategy

## Purpose

Establish the single, deterministic data foundation every e2e journey runs against — reusing bolt 062's fluent Builders and the existing `--seed`/`--seed-dev` seed modes — so journey specs (unit 002) are stable, isolated, and free of ad-hoc data setup.

## Scope

### In Scope
- A documented **e2e data contract** (seeded products/slugs, admin credentials, lockers, test users).
- Playwright **fixtures** (`e2e/fixtures/`): authenticated-user fixture, admin fixture, seeded-catalog handle, payment test-mode config.
- Deterministic, idempotent setup/teardown reusing bolt 062 Builders + `DevDataSeed`/`ProductCatalogSeed`.
- A docker-compose boot that runs the API against **real PostgreSQL** for the e2e suite.

### Out of Scope
- The journey specs themselves (unit 002).
- The Playwright runner + base `playwright-e2e.yml` workflow (already shipped by bolt 066 — reused, not rebuilt).
- The Builders + shared factory (already shipped by bolt 062 — reused, not rebuilt).

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-1 | Deterministic, seeded e2e test-data strategy | Must |

---

## Domain Concepts

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| Seed e2e DB | Apply ProductCatalogSeed + DevDataSeed deterministically | empty Postgres | known catalog, admin, lockers |
| Acquire fixture | Provide a logged-in user / admin / guest context | seed + Builder | authenticated Playwright context |
| Provision per-spec data | Builder-generated unique entities to avoid contention | Builders (bolt 062) | isolated test entities |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 4 |
| Must Have | 2 |
| Should Have | 2 |
| Could Have | 0 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-e2e-data-contract | Documented e2e data contract | Must | Planned |
| 002-builder-backed-fixtures | Builder-backed Playwright fixtures | Must | Planned |
| 003-payment-testmode-fixtures | Stripe/EuPlatesc test-mode fixtures | Should | Planned |
| 004-real-postgres-e2e-boot | Real-Postgres docker-compose e2e boot | Should | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| bolt 066 (intent 030) | Playwright runner + `playwright-e2e.yml` harness this builds on |
| bolt 062 (intent 028) | Fluent Builders + shared factory reused for fixtures |

### Depended By
| Unit | Reason |
|------|--------|
| 002-e2e-journey-coverage | Every journey spec consumes these fixtures |
| 003-regression-methodology | Indirectly, via unit 002 coverage |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| PostgreSQL 16 (compose) | Real-DB e2e boot | Medium (surfaces the InMemory-vs-Postgres parity gap) |
| Stripe / EuPlatesc test mode | Payment fixtures | Low |

---

## Technical Context

### Suggested Technology
`@playwright/test` fixtures, docker-compose (Postgres), the existing `dotnet PhotoPrint.API.dll --seed`/`--seed-dev` modes, bolt 062 Builders.

### Integration Points
| Integration | Type | Protocol |
|-------------|------|----------|
| Seed modes | CLI | `--seed` / `--seed-dev` |
| Builders | in-process / API | bolt 062 |

---

## Constraints

- Idempotent setup; per-spec uniqueness via Builder-generated identifiers (no shared mutable contention).
- Test mode only for payments; no live keys.
- Reuse, never fork, bolt 066 and bolt 062 assets.

---

## Success Criteria

### Functional
- [ ] E2e data contract documents every seeded entity specs rely on.
- [ ] Fixtures provide guest/user/admin contexts deterministically.
- [ ] Re-running the suite twice yields identical results.

### Non-Functional
- [ ] Suite boots against real Postgres via docker-compose.

### Quality
- [ ] No spec hand-creates undocumented data outside the journey under test.

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 070-e2e-data-strategy | simple | 001–004 | Deterministic seeded e2e foundation |

---

## Notes

This unit is the contract between bolt 062's Builders and unit 002's journeys. If bolt 066/062 slip, this unit blocks — it deliberately does not re-implement either.
