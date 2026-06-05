---
intent: 032-regression-and-e2e-stabilization
phase: inception
status: units-decomposed
updated: 2026-06-05T11:20:00Z
---

# Regression & Comprehensive E2E - Unit Decomposition

## Units Overview

Decomposes into **3 units**. All are test/CI/methodology work (no domain model), so all use `simple-construction-bolt`. The natural order is: foundation (data strategy) → journey coverage (the bulk) → regression methodology (which consumes the e2e coverage). Bolts: 070 → 071 → 072.

### Unit 1: 001-e2e-data-strategy
**Description**: The deterministic, seeded e2e data contract — reusing bolt 062's fluent Builders and the existing `--seed`/`--seed-dev` modes — plus the shared spec fixtures (test users, admin, seeded catalog, lockers, payment test-mode config) every journey spec builds on.
**Stories**: 001-e2e-data-contract, 002-builder-backed-fixtures, 003-payment-testmode-fixtures, 004-real-postgres-e2e-boot
**Deliverables**: documented e2e data contract; `e2e/fixtures/` (auth, seed, payment); deterministic idempotent setup; docker-compose-with-Postgres boot used by the suite.
**Dependencies**: Depends on bolt 066 (Playwright foundation) + bolt 062 (Builders) · Depended by Units 2 and 3.
**Estimated Complexity**: M
**Unit Type**: frontend
**Default Bolt Type**: simple-construction-bolt

### Unit 2: 002-e2e-journey-coverage
**Description**: The comprehensive journey specs themselves plus their CI integration (fast PR tier + scheduled full suite, trace/video artifacts, flake controls). Extends bolt 066's module from 3 smoke specs to full coverage. Coupon/refund journeys authored here but gated.
**Stories**: 001-guest-and-registered-checkout, 002-authentication-journeys, 003-uploads-cart-and-merge, 004-payments-journeys, 005-orders-and-account-journeys, 006-admin-journeys, 007-gated-coupon-refund-journeys, 008-e2e-ci-tiers-and-stability
**Deliverables**: `e2e/journeys/*.spec.ts` across all domains; extended `playwright-e2e.yml` (fast/full tiers); Playwright config with bounded retries + artifacts; gated coupon/refund specs (`test.fixme`).
**Dependencies**: Depends on Unit 1 (fixtures) + bolt 066 (harness); gated specs reference bolts 047/048 + 068/069 · Depended by Unit 3.
**Estimated Complexity**: L
**Unit Type**: frontend
**Default Bolt Type**: simple-construction-bolt

### Unit 3: 003-regression-methodology
**Description**: The documented regression checklist mapped to every shipped intent, one executed + recorded baseline pass, and the triage of findings into the backlog (new bolt / existing bolt / KNOWN_FAILURES). Cross-references which checks the FR-2 e2e specs now automate.
**Stories**: 001-regression-checklist, 002-execute-regression-baseline, 003-triage-findings-to-backlog
**Deliverables**: `docs/testing/regression-checklist.md`; a dated baseline result; backlog/KNOWN_FAILURES entries for findings.
**Dependencies**: Depends on Unit 2 (so the checklist can mark automated-by-e2e coverage) · Depended by None.
**Estimated Complexity**: M
**Unit Type**: frontend
**Default Bolt Type**: simple-construction-bolt

## Requirement-to-Unit Mapping

- **FR-1** (deterministic seeded e2e data) → `001-e2e-data-strategy`
- **FR-2** (comprehensive journey coverage) → `002-e2e-journey-coverage`
- **FR-3** (CI integration, stability, reporting) → `002-e2e-journey-coverage`
- **FR-4** (regression methodology + baseline) → `003-regression-methodology`

## Unit Dependency Graph

```text
[bolt 066: Playwright foundation] ─┐
[bolt 062: Builders + factory]  ───┼─> [001-e2e-data-strategy] ─> [002-e2e-journey-coverage] ─> [003-regression-methodology]
                                                                          │
                                            (gated) [bolt 047/048 coupons]┤
                                            (gated) [bolt 068/069 refunds]┘
```

## Execution Order

1. Unit 1 — e2e data strategy (depends on 066 + 062 landing first)
2. Unit 2 — journey coverage + CI tiers (the bulk of the work)
3. Unit 3 — regression checklist + executed baseline (consumes Unit 2 coverage)
