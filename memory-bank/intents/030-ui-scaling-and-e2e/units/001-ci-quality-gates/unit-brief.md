---
unit: 001-ci-quality-gates
intent: 030-ui-scaling-and-e2e
phase: inception
status: draft
unit_type: frontend
default_bolt_type: simple-construction-bolt
created: 2026-06-05T09:30:00Z
updated: 2026-06-05T09:30:00Z
---

# Unit Brief: CI Quality Gates

## Purpose

Add the two pre-launch quality gates the frontend lacks: a CI bundle-size budget and three Playwright e2e smoke tests covering the real-money paths.

## Scope

### In Scope
- `angular.json` budgets; `@playwright/test` dev dep; `guest-checkout`, `admin-login`, `realtime-order` specs; `playwright-e2e.yml` CI workflow.

### Out of Scope
- The page breakups + BaseApiService (unit 002).

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-1 (P18) | Bundle-size CI budget + 3 e2e smoke tests | Must (e2e) |

---

## Domain Concepts

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| Budget check | Fail build over threshold | bundle size | pass/fail |
| E2e run | Drive real-money paths | docker-compose API+UI | pass/fail |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 2 |
| Must Have | 1 |
| Should Have | 1 |
| Could Have | 0 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-bundle-size-budget | CI bundle-size budget | Should | Planned |
| 002-playwright-e2e-smoke-tests | 3 Playwright e2e | Must | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| None | — |

### Depended By
| Unit | Reason |
|------|--------|
| 002-ui-scaling-and-e2e-ui | E2e verifies the page breakups don't regress |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| Stripe test mode | Guest-checkout e2e | Low |
| Playwright/CI | Browser runtime | Medium (~200MB cache) |

---

## Technical Context

### Suggested Technology
Angular `budgets`, `@playwright/test`, GitHub Actions, docker-compose.

---

## Constraints

- Measure current bundle size before setting the budget.
- ~3 min per e2e run; use the official Playwright action.

---

## Success Criteria

### Functional
- [ ] Build fails over the error budget; `anyComponentStyle` error at 4kB.
- [ ] 3 e2e (guest checkout, admin login, real-time SignalR) pass in CI.

### Non-Functional
- [ ] E2e run within ~3 min.

### Quality
- [ ] Workflow stable (no flakes).

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 066-ci-quality-gates | simple | 001, 002 | Budget + e2e |

---

## Notes

Pre-launch must-have (e2e). Ship first within this intent.
