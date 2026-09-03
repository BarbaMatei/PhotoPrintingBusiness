---
id: 061-handler-pattern
unit: 003-handler-pattern
intent: 027-architectural-layering
type: simple-construction-bolt
status: planned
stories:
  - 001-command-handler-abstractions
  - 002-create-order-handler
  - 003-order-paid-event-dispatcher
  - 004-retry-and-promote-handlers
created: 2026-06-05T09:30:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [059-layering-foundation, 060-conventions-and-policy]
enables_bolts: []
requires_units: [001-layering-foundation, 002-conventions-and-policy]
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 2
---

# Bolt: 061-handler-pattern

## Overview

Handler-per-use-case (P25): abstractions + four target handlers, folding in the OrderPaidEventDispatcher (P11).

## Objective

Give multi-step use cases a discoverable, testable home and dedupe the webhook post-Paid fan-out.

## Stories Included

- **001-command-handler-abstractions**: ICommandHandler/IEventDispatcher (Should)
- **002-create-order-handler**: CreateOrderHandler (Should)
- **003-order-paid-event-dispatcher**: OrderPaidEventDispatcher / P11 (Should)
- **004-retry-and-promote-handlers**: Retry-invoice + promote-photos handlers (Should)

## Bolt Type

**Type**: simple-construction-bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Pending → implementation-plan.md
- [ ] **2. implement**: Pending → Application/Shared/Abstractions + 4 handlers; controllers/services delegate
- [ ] **3. test**: Pending → per-handler tests; dispatcher ordering test; payment/webhook suite green

## Dependencies

### Requires
- 059-layering-foundation, 060-conventions-and-policy

### Enables
- None

## Success Criteria

- [ ] Four use cases are handlers with own tests; service methods one-line delegate
- [ ] Both webhook paths call the dispatcher; ADR-020 ordering asserted
- [ ] No behaviour change; OrderServiceTests.cs shrinks

## Notes

P25 folds P11. Coordinates with intent 029 P14 (residual decomposition).
