---
id: 003-uploads-cart-and-merge
unit: 002-e2e-journey-coverage
intent: 032-regression-and-e2e-stabilization
status: draft
priority: must
created: 2026-06-05T11:35:00Z
assigned_bolt: 071-e2e-journey-coverage
implemented: false
---

# Story: 003-uploads-cart-and-merge

## User Story

**As a** maintainer
**I want** end-to-end specs for uploads, cart editing, and the guest→user cart merge
**So that** the pre-checkout flow that feeds every order is proven

## Acceptance Criteria

- [ ] **Given** the upload page, **When** a user uploads multiple files, **Then** progress bars complete and thumbnails render for each accepted file
- [ ] **Given** an unsupported/oversized file, **When** uploaded, **Then** it is rejected with a clear message and accepted files are unaffected
- [ ] **Given** items in the cart, **When** the user edits quantity / changes format / removes an item, **Then** the cart totals recompute correctly
- [ ] **Given** a guest with a cart, **When** they log in, **Then** the **guest cart merges** into the user cart transactionally (no duplication, correct totals) — `POST /api/cart/merge` path
- [ ] **Given** these specs, **When** run in CI, **Then** they pass against real Postgres via the unit-001 fixtures

## Technical Notes

- Use small fixture image files committed to the e2e assets folder; assert thumbnails via `data-testid`.
- Merge journey chains the guest fixture → registered-user fixture (shares the unit-002 auth transition).

## Dependencies

### Requires
- 002-builder-backed-fixtures (unit 001)

### Enables
- 001-guest-and-registered-checkout (uploads feed checkout)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| HEIC upload | Browser-side preview conversion; accepted |
| Merge when user already has items | Items combined, not overwritten; totals correct |

## Out of Scope

- Photo-archive lifecycle (covered by intent-024 integration tests).
