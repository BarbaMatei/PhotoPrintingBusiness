---
id: 067-ui-scaling-and-e2e-ui
unit: 002-ui-scaling-and-e2e-ui
intent: 030-ui-scaling-and-e2e
type: simple-construction-bolt
status: review-pending
stories:
  - 001-base-api-service
  - 002-home-page-breakup
  - 003-account-pages-breakup
  - 004-delivery-step-locker-selector
created: 2026-06-05T09:30:00Z
started: 2026-09-04T00:55:00Z
completed: 2026-09-04T14:00:00Z
current_stage: null
stages_completed:
  - name: plan
    completed: 2026-09-04T11:20:00Z
    artifact: implementation-plan.md
  - name: implement
    completed: 2026-09-04T13:10:00Z
    artifact: implementation-walkthrough.md
  - name: test
    completed: 2026-09-04T14:00:00Z
    artifact: test-walkthrough.md

requires_bolts: [066-ci-quality-gates]
enables_bolts: []
requires_units: [001-ci-quality-gates]
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 2
---

# Bolt: 067-ui-scaling-and-e2e-ui

## Overview

Break up the four largest Angular pages into smart/dumb components and introduce a shared `BaseApiService` (P26).

## Objective

Make the UI component layer maintainable and DRY up HTTP plumbing — verified by the e2e + budget gates from bolt 066.

## Stories Included

- **001-base-api-service**: Shared BaseApiService (Should)
- **002-home-page-breakup**: home-page.ts 951 LOC → container + 5 children (Should)
- **003-account-pages-breakup**: saved-addresses + profile (Should)
- **004-delivery-step-locker-selector**: extract locker-selector (Should)

## Bolt Type

**Type**: simple-construction-bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [x] **1. plan**: ✅ Complete → implementation-plan.md
- [x] **2. implement**: ✅ Complete → implementation-walkthrough.md (BaseApiService + four page breakups)
- [x] **3. test**: ✅ Complete → test-walkthrough.md (126/126 Vitest green, build 330.13 kB; e2e not runnable locally)

## Dependencies

### Requires
- 066-ci-quality-gates (e2e + budget guard the refactor)

### Enables
- None

## Success Criteria

- [ ] No page > ~200 LOC — home 951→66 and profile 473→217, but saved-addresses is 334 and delivery-step 574 (see test-walkthrough.md)
- [x] All services route through BaseApiService (the six data services; auth/guest-auth deliberately excluded)
- [x] Within bundle budget; no home visual regression (no screenshot baseline exists — checked by eye)

## Notes

One PR per page (home → saved-addresses → profile → delivery-step). Parallelisable with backend intents.
