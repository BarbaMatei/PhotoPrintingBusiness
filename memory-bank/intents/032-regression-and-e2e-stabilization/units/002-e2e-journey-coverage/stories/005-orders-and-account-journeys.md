---
id: 005-orders-and-account-journeys
unit: 002-e2e-journey-coverage
intent: 032-regression-and-e2e-stabilization
status: draft
priority: must
created: 2026-06-05T11:35:00Z
assigned_bolt: 071-e2e-journey-coverage
implemented: false
---

# Story: 005-orders-and-account-journeys

## User Story

**As a** maintainer
**I want** end-to-end specs for order history/detail and account management
**So that** the post-purchase and self-service surfaces are proven

## Acceptance Criteria

- [ ] **Given** a user with several orders, **When** they open `/comenzi`, **Then** the list paginates correctly and each row shows the right status
- [ ] **Given** an order they own, **When** they open its detail, **Then** the full detail renders including the order-photo grid
- [ ] **Given** an order owned by **another** user, **When** they attempt to open it by ID, **Then** access is denied (ownership enforced) — an explicit authz check
- [ ] **Given** the account area, **When** the user edits their profile, changes their password, and adds/edits/deletes a saved address, **Then** each change persists and is reflected on reload
- [ ] **Given** these specs, **When** run in CI, **Then** they pass deterministically

## Technical Notes

- Use the Builder to create the "other user's order" for the ownership-denial branch.
- Password-change journey should assert re-login with the new password works.

## Dependencies

### Requires
- 002-builder-backed-fixtures (unit 001)

### Enables
- 001-regression-checklist (unit 003) marks these as automated-by-e2e

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Page beyond last | Empty state, no error |
| Delete the only address | Allowed; address book empty state shown |

## Out of Scope

- Admin-side order management (story 006).
