---
id: 034-shipping-cost-server-side
unit: 001-shipping-cost-server-side
intent: 014-payment-hardening
type: simple-construction-bolt
status: complete
stories:
  - 001-remove-client-shipping-cost
  - 002-create-order-validator
created: 2026-05-25T10:05:00Z
started: 2026-05-25T12:30:00Z
completed: 2026-05-25T13:00:00Z
current_stage: null
stages_completed:
  - name: plan
    completed: 2026-05-25T12:35:00Z
    artifact: implementation-plan.md
  - name: implement
    completed: 2026-05-25T12:45:00Z
    artifact: implementation-walkthrough.md
  - name: test
    completed: 2026-05-25T13:00:00Z
    artifact: test-walkthrough.md

requires_bolts: [015-shipping-and-order-core, 016-payment-backends]
enables_bolts: [035-payment-idempotency]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 2
---

# Bolt: 034-shipping-cost-server-side

## Overview

Cut the client's ability to set `ShippingCostRon`. The server resolves it from `DeliveryType`. Add `CreateOrderRequestValidator` to enforce conditional fields.

## Objective

After this bolt, no value the browser sends can influence `Order.TotalRon` beyond the cart's contents and the chosen delivery type. All invalid combinations 422 before any DB write or Stripe call.

## Stories Included

- **001-remove-client-shipping-cost** — drop the field from the DTO; resolve via `IShippingService` (Must).
- **002-create-order-validator** — FluentValidation rules for delivery-type-conditional fields (Must).

## Bolt Type

`simple-construction-bolt`.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Plan | DTO diff, validator structure, transitional-logging approach |
| 2 | Implement | DTO change, validator class, service wiring, controller cleanup |
| 3 | Test | Integration test: tampered shipping value ignored + 422 cases |

## Dependencies

- **Requires**: 015-shipping-and-order-core (the order pipeline), 016-payment-backends (the controller).
- **Enables**: 035-payment-idempotency (clean inputs prerequisite to idempotency lookup).
