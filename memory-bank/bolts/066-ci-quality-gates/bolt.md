---
id: 066-ci-quality-gates
unit: 001-ci-quality-gates
intent: 030-ui-scaling-and-e2e
type: simple-construction-bolt
status: review-pending
stories:
  - 001-bundle-size-budget
  - 002-playwright-e2e-smoke-tests
created: 2026-06-05T09:30:00Z
started: 2026-09-03T20:45:00Z
completed: null
current_stage: null
stages_completed:
  - name: plan
    completed: 2026-09-03T23:40:00Z
    artifact: implementation-plan.md
  - name: implement
    completed: 2026-09-04T00:20:00Z
    artifact: implementation-walkthrough.md
  - name: test
    completed: 2026-09-04T11:30:00Z
    artifact: test-walkthrough.md

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

- [x] **1. plan**: ✅ Complete → implementation-plan.md
- [x] **2. implement**: ✅ Complete → angular.json budgets; e2e specs; playwright-e2e.yml
- [x] **3. test**: ✅ Complete → CI runs budget + e2e green

## Dependencies

### Requires
- None

### Enables
- 067-ui-scaling-and-e2e-ui

## Success Criteria

- [x] Build fails over budget
- [x] 3 e2e pass in CI within ~3 min (16.7 s, three consecutive green runs)

## Notes

Independent of backend. Pre-launch must-have (e2e). Parallelisable on a second developer.
