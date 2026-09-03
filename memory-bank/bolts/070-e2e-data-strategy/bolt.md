---
id: 070-e2e-data-strategy
unit: 001-e2e-data-strategy
intent: 032-regression-and-e2e-stabilization
type: simple-construction-bolt
status: planned
stories:
  - 001-e2e-data-contract
  - 002-builder-backed-fixtures
  - 003-payment-testmode-fixtures
  - 004-real-postgres-e2e-boot
created: 2026-06-05T11:45:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [066-ci-quality-gates, 062-test-infrastructure]
enables_bolts: [071-e2e-journey-coverage]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 2
  max_dependencies: 3
  testing_scope: 3
---

# Bolt: 070-e2e-data-strategy

## Overview

The deterministic, seeded e2e data foundation: a documented data contract, Builder-backed Playwright fixtures (guest/user/admin), payment test-mode fixtures, and a real-Postgres docker-compose boot. Reuses bolt 066's Playwright module and bolt 062's fluent Builders — it does not rebuild either.

## Objective

Give the journey specs (bolt 071) a single, stable, isolated data layer so they are deterministic and free of ad-hoc setup, and make the suite the first place the InMemory-vs-Postgres parity gap surfaces (the `db-parity` review lens exists for it).

## Stories Included

- **001-e2e-data-contract**: Documented data contract (Must)
- **002-builder-backed-fixtures**: Guest/user/admin fixtures via bolt 062 Builders (Must)
- **003-payment-testmode-fixtures**: Stripe + EuPlatesc test-mode fixtures (Should)
- **004-real-postgres-e2e-boot**: Real-Postgres compose boot (Should)

## Bolt Type

**Type**: simple-construction-bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Pending → implementation-plan.md
- [ ] **2. implement**: Pending → `e2e/fixtures/` (data-contract, seed-data.ts, auth + payment fixtures); compose-PG boot
- [ ] **3. test**: Pending → fixtures yield deterministic contexts; suite re-run is idempotent

## Dependencies

### Requires
- **066-ci-quality-gates** (Required): Playwright runner + `playwright-e2e.yml` harness this builds on
- **062-test-infrastructure** (Required): fluent Builders + shared factory reused for fixtures

### Enables
- 071-e2e-journey-coverage

## Success Criteria

- [ ] Data contract documents every seeded entity specs rely on
- [ ] Guest/user/admin fixtures deterministic; per-spec uniqueness via Builders
- [ ] Stripe + EuPlatesc test-mode fixtures (no live keys)
- [ ] Suite boots against real Postgres; re-run yields identical results

## Notes

Hard dependency on bolts 066 + 062 — if either slips, this bolt blocks rather than duplicating their assets. Surfaces (does not fix) the DEPLOYMENT.md §7 migration gap.
