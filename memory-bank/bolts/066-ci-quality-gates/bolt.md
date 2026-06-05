---
id: 066-ci-quality-gates
unit: 001-ci-quality-gates
intent: 030-ui-scaling-and-e2e
type: simple-construction-bolt
status: planned
stories:
  - 001-bundle-size-budget
  - 002-playwright-e2e-smoke-tests
created: 2026-06-05T09:30:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: []
enables_bolts: [067-ui-scaling-and-e2e-ui]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 3
---

# Bolt: 066-ci-quality-gates

## Overview

CI bundle-size budget + 3 Playwright e2e smoke tests on the real-money paths (P18).

## Objective

Add the pre-launch frontend quality gates: catch bundle bloat and automate guest checkout / admin login / real-time SignalR.

## Stories Included

- **001-bundle-size-budget**: angular.json budgets (Should)
- **002-playwright-e2e-smoke-tests**: 3 e2e + CI workflow (Must)

## Bolt Type

**Type**: simple-construction-bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Pending → implementation-plan.md
- [ ] **2. implement**: Pending → angular.json budgets; e2e specs; playwright-e2e.yml
- [ ] **3. test**: Pending → CI runs budget + e2e green

## Dependencies

### Requires
- None

### Enables
- 067-ui-scaling-and-e2e-ui

## Success Criteria

- [ ] Build fails over budget
- [ ] 3 e2e pass in CI within ~3 min

## Notes

Independent of backend. Pre-launch must-have (e2e). Parallelisable on a second developer.
