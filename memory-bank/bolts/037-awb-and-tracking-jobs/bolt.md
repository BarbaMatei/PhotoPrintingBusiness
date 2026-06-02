---
id: 037-awb-and-tracking-jobs
unit: 002-awb-and-tracking-jobs
intent: 015-sameday-shipping-integration
type: ddd-construction-bolt
status: complete
stories:
  - 001-awb-creation-on-paid
  - 002-awb-retry-job
  - 003-shipment-tracking-job
created: 2026-05-25T10:10:00.000Z
started: 2026-06-02T16:00:00.000Z
completed: "2026-06-02T19:28:01Z"
current_stage: null
stages_completed:
  - name: domain-model
    completed: 2026-06-02T16:30:00.000Z
    artifact: ddd-01-domain-model.md
  - name: technical-design
    completed: 2026-06-02T17:00:00.000Z
    artifact: ddd-02-technical-design.md
  - name: adr-analysis
    completed: 2026-06-02T17:20:00.000Z
    artifact: adr-015-accept-duplicate-awb-create-on-multi-replica.md, adr-016-cas-execute-update-for-multi-replica-status-transitions.md
  - name: implement
    completed: 2026-06-02T19:00:00.000Z
    artifact: implementation-walkthrough.md
  - name: test
    completed: 2026-06-02T20:00:00.000Z
    artifact: ddd-03-test-report.md
requires_bolts:
  - 036-sameday-api-client
enables_bolts: []
requires_units: []
blocks: false
complexity:
  avg_complexity: 3
  avg_uncertainty: 3
  max_dependencies: 1
  testing_scope: 4
---

# Bolt: 037-awb-and-tracking-jobs

## Overview

Wire AWB creation into the `Paid` transition, add a 1-hourly retry job for failed AWBs, and a 15-minute tracking poll that auto-progresses `Shipped → Delivered`.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Domain Model | `ddd-01-domain-model.md` — `OrderStatusMachine` hook contract, retry semantics |
| 2 | Technical Design | `ddd-02-technical-design.md` — channel-based async work queue, job schedules, concurrency controls |
| 3 | Implement | Code, hooks, two `BackgroundService` jobs |
| 4 | Test | `ddd-03-test-report.md` — paid-order-creates-awb, retry-succeeds-on-second-attempt, tracking-delivers-and-mails |

## Dependencies

- **Requires**: 036-sameday-api-client.
- **Enables**: intent 020 (observability metrics on the new jobs).

## Key Technical Notes

- All three jobs MUST be safe under multi-instance until intent 021 lands a leader-election or Redis lock. Sameday's external reference makes duplicate AWB creates safe; tracking transitions are idempotent on status check.
- Channel-based async dispatch keeps the controller's response time unaffected.
