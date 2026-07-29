---
id: 006-admin-journeys
unit: 002-e2e-journey-coverage
intent: 032-regression-and-e2e-stabilization
status: draft
priority: must
created: 2026-06-05T11:35:00Z
assigned_bolt: 071-e2e-journey-coverage
implemented: false
---

# Story: 006-admin-journeys

## User Story

**As a** maintainer
**I want** end-to-end specs for the admin management surfaces
**So that** order, product, and invoice administration are proven

## Acceptance Criteria

- [ ] **Given** the admin fixture, **When** an admin logs in, **Then** the admin area is reachable and a non-admin is denied (authz gate)
- [ ] **Given** the admin order list, **When** the admin opens an order and transitions its status, **Then** the transition succeeds only along valid `OrderStatusMachine` edges and is reflected in the list
- [ ] **Given** the admin product surface, **When** the admin creates and then edits a product (incl. pricing tiers), **Then** the changes persist and appear in the public catalog
- [ ] **Given** the admin invoice surface, **When** the admin views the invoice list and (where bolt 039 has shipped) retrieves a PDF/XML, **Then** the list renders and retrieval works; where 039 has not shipped, that assertion is gated/skipped
- [ ] **Given** these specs, **When** run in CI, **Then** they pass deterministically

## Technical Notes

- Admin order/product surfaces shipped in intent 007 (bolts 021/022); invoice surface depends on intent 016 bolt 039 shipping.
- Real-time SignalR admin notification is already a bolt-066 smoke spec — reference it, do not duplicate.

## Dependencies

### Requires
- 002-builder-backed-fixtures (unit 001)
- bolt 066 (real-time admin smoke spec referenced, not duplicated)

### Enables
- 001-regression-checklist (unit 003)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Invalid status transition | Rejected by the state machine |
| Non-admin hits admin URL | Redirected/denied |
| Invoice surface absent (039 not shipped) | Invoice assertions skip cleanly |

## Out of Scope

- Refund admin action (gated story 007); coupon admin CRUD (gated story 007).
