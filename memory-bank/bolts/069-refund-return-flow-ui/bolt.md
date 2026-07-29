---
id: 069-refund-return-flow-ui
unit: 002-refund-return-flow-ui
intent: 031-refund-return-flow
type: simple-construction-bolt
status: planned
stories:
  - 001-admin-refund-action
created: 2026-06-05T09:30:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [068-refund-domain-and-api]
enables_bolts: []
requires_units: [001-refund-domain-and-api]
blocks: false

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 2
---

# Bolt: 069-refund-return-flow-ui

## Overview

Admin refund action + modal on the order-detail view (P09 FR4 UI).

## Objective

Let admins issue full/partial refunds with a reason from the order detail page.

## Stories Included

- **001-admin-refund-action**: Admin refund action + modal (Must)

## Bolt Type

**Type**: simple-construction-bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Pending → implementation-plan.md
- [ ] **2. implement**: Pending → admin order-detail refund action + modal
- [ ] **3. test**: Pending → Vitest spec; error-code → Romanian copy

## Dependencies

### Requires
- 068-refund-domain-and-api (refund endpoint)

### Enables
- None

## Success Criteria

- [ ] Refund action (full/partial + reason) on order detail; refunded state shown
- [ ] Admin-only; irreversible-action confirmation

## Notes

After the endpoint exists. Reuse BaseApiService if intent 030 P26 has landed.
