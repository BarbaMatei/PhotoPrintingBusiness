---
intent: 030-ui-scaling-and-e2e
phase: inception
status: units-decomposed
updated: 2026-06-05T09:30:00Z
---

# UI Scaling & E2E - Unit Decomposition

## Units Overview

Decomposes into **2 units** — CI quality gates (ship first) and the UI scaling refactor (the `{intent}-ui` frontend unit). Both `simple-construction-bolt`.

### Unit 1: 001-ci-quality-gates
**Description**: CI bundle-size budget + 3 Playwright e2e smoke tests (P18).
**Stories**: 001-bundle-size-budget, 002-playwright-e2e-smoke-tests
**Deliverables**: `angular.json` budgets; `@playwright/test`; `e2e/` specs; `playwright-e2e.yml`.
**Dependencies**: Depends on None · Depended by Unit 2 (e2e foundation aids verification)
**Estimated Complexity**: M

### Unit 2: 002-ui-scaling-and-e2e-ui
**Description**: Break up the four largest pages into smart/dumb components and introduce `BaseApiService` (P26).
**Unit Type**: frontend
**Stories**: 001-base-api-service, 002-home-page-breakup, 003-account-pages-breakup, 004-delivery-step-locker-selector
**Deliverables**: `core/services/api/base-api.service.ts`; decomposed home/saved-addresses/profile/delivery-step components.
**Dependencies**: Depends on Unit 1 (verify via e2e + budget) · Depended by None
**Estimated Complexity**: L

## Requirement-to-Unit Mapping

- **FR-1 (P18)** → `001-ci-quality-gates`
- **FR-2 (P26)** → `002-ui-scaling-and-e2e-ui`

## Unit Dependency Graph

```text
[001-ci-quality-gates] ──> [002-ui-scaling-and-e2e-ui]
```

## Execution Order

1. Unit 1 (budget + e2e foundation)
2. Unit 2 (page breakups + BaseApiService) — one PR per page
