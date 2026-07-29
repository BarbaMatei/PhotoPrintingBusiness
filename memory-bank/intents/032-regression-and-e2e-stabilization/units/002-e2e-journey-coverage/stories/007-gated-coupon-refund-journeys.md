---
id: 007-gated-coupon-refund-journeys
unit: 002-e2e-journey-coverage
intent: 032-regression-and-e2e-stabilization
status: draft
priority: should
created: 2026-06-05T11:35:00Z
assigned_bolt: 071-e2e-journey-coverage
implemented: false
---

# Story: 007-gated-coupon-refund-journeys

## User Story

**As a** maintainer
**I want** coupon and refund journeys authored now but gated until their features ship
**So that** coverage is ready the moment bolts 047/048 and 068/069 land — without this intent re-implementing them

## Acceptance Criteria

- [ ] **Given** the coupon feature is not yet shipped, **When** the coupon specs are added, **Then** they are authored as `should` and marked `test.fixme` (or `test.skip` with a `// requires bolt 047/048` note) so CI stays green
- [ ] **Given** the refund feature is not yet shipped, **When** the refund specs are added, **Then** they are likewise gated with a `// requires bolt 068/069` note
- [ ] **Given** the coupon feature ships, **When** the gate is removed, **Then** the spec asserts: apply valid coupon at cart → discount line shown → order total reflects discount; invalid/expired coupon rejected
- [ ] **Given** the refund feature ships, **When** the gate is removed, **Then** the spec asserts: admin issues full/partial refund → order status `Refunded` → (bolt 068) credit-note path available
- [ ] **Given** this intent closes with the features unshipped, **When** the suite runs, **Then** the gated specs skip cleanly and are reported as pending (not failing)

## Technical Notes

- This story authors the specs only; it MUST NOT implement coupons or refunds (those are bolts 047/048 and 068/069).
- Un-gating is a deliberately trivial follow-up: remove `fixme`, confirm green.

## Dependencies

### Requires
- 002-builder-backed-fixtures (unit 001)

### Enables
- (on un-gating) full coverage of the coupon + refund money paths

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Feature partially shipped (API but no UI) | Keep UI assertions gated; enable API-level assertions if safe |

## Out of Scope

- Implementing coupons (bolts 047/048) or refunds (bolts 068/069).
