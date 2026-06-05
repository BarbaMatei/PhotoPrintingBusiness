---
id: 064-service-decomposition
unit: 002-service-decomposition
intent: 029-decomposition-and-hardening
type: simple-construction-bolt
status: planned
stories:
  - 001-decompose-auth-service
  - 002-thin-webhooks-and-order-photo-query
created: 2026-06-05T09:30:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [059-layering-foundation, 061-handler-pattern]
enables_bolts: []
requires_units: [001-layering-foundation, 003-handler-pattern]
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 2
---

# Bolt: 064-service-decomposition

## Overview

Split AuthService into 3 (P13) and finish thinning WebhooksController + extract OrderPhotoQueryService (P14 residual).

## Objective

Decompose the remaining god-classes into the layered shape without behaviour change.

## Stories Included

- **001-decompose-auth-service**: AuthService → 3 services (Should)
- **002-thin-webhooks-and-order-photo-query**: OrderPhotoQueryService + thin webhooks (Should)

## Bolt Type

**Type**: simple-construction-bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Pending → implementation-plan.md
- [ ] **2. implement**: Pending → Application/Auth/Services split; OrderPhotoQueryService; thin webhooks
- [ ] **3. test**: Pending → auth + payment/webhook integration suites green

## Dependencies

### Requires
- 059-layering-foundation (layered shape)
- 061-handler-pattern (CreateOrderHandler + dispatcher already extracted)

### Enables
- None

## Success Criteria

- [ ] 3 auth services with own tests
- [ ] GetOrderPhotosAsync in OrderPhotoQueryService; webhooks free of data-access orchestration
- [ ] No behaviour change

## Notes

Scope P14 to residuals (avoid re-extracting CreateFromCartAsync / fan-out).
