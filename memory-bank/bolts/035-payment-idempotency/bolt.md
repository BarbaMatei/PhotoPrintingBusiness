---
id: 035-payment-idempotency
unit: 002-payment-idempotency
intent: 014-payment-hardening
type: ddd-construction-bolt
status: complete
stories:
  - 001-idempotency-key-migration
  - 002-stripe-intent-idempotency
  - 003-legacy-processor-initiate-idempotency
created: 2026-05-25T10:05:00Z
started: 2026-05-25T13:10:00Z
completed: 2026-05-25T14:15:00Z
current_stage: null
stages_completed:
  - name: model
    completed: 2026-05-25T13:20:00Z
    artifact: ddd-01-domain-model.md
  - name: design
    completed: 2026-05-25T13:30:00Z
    artifact: ddd-02-technical-design.md
  - name: adr-analysis
    completed: 2026-05-25T13:36:00Z
    artifacts: [adr-004-state-conflict-409.md, adr-005-logical-request-excludes-shipping-address.md]
  - name: implement
    completed: 2026-05-25T13:55:00Z
    artifact: implementation-walkthrough.md
  - name: test
    completed: 2026-05-25T14:10:00Z
    artifact: ddd-03-test-report.md

requires_bolts: [034-shipping-cost-server-side, 016-payment-backends]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 3
  avg_uncertainty: 2
  max_dependencies: 2
  testing_scope: 3
---

# Bolt: 035-payment-idempotency

## Overview

Make payment-intent creation idempotent end-to-end: at the DB (filtered unique index), at the application layer (lookup-then-create), and at Stripe (`RequestOptions.IdempotencyKey`).

## Objective

By the end of this bolt, a double-clicked "Pay" produces exactly one order, exactly one Stripe charge, and exactly one the legacy processor redirect URL — verified by integration tests.

## Stories Included

- **001-idempotency-key-migration** — schema change + filtered unique index (Must).
- **002-stripe-intent-idempotency** — controller + service flow + Stripe SDK options (Must).
- **003-legacy-processor-initiate-idempotency** — reuse the persisted redirect URL/order (Must).

## Bolt Type

`ddd-construction-bolt` — touches domain entity (`Order`), service layer, and external SDK boundary; warrants the design pass.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Domain Model | `ddd-01-domain-model.md` — `Order.IdempotencyKey` invariant; lookup vs. compare-then-conflict policy |
| 2 | Technical Design | `ddd-02-technical-design.md` — migration, controller diff, `IOrderService` extension, Stripe options wiring |
| 3 | Implement | Code per design |
| 4 | Test | `ddd-03-test-report.md` — duplicate-click integration tests for both processors |

## Dependencies

- **Requires**: 034-shipping-cost-server-side (clean inputs), 016-payment-backends (the controller).
- **Enables**: intent 015 (lower probability of duplicate AWB calls).

## Key Technical Notes

- Idempotency window: 24 h from `Order.CreatedAt`. Past that, treat as a new request.
- `Idempotency-Key` format: free-form up to 80 chars; UUID v4 recommended (document in OpenAPI).
- Do **not** introduce Redis here. Distributed idempotency is intent 021.
- Surface idempotency conflicts as 409, not 422 (semantically different from validation).
