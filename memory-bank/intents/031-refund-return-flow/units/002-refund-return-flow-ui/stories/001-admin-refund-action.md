---
id: 001-admin-refund-action
unit: 002-refund-return-flow-ui
intent: 031-refund-return-flow
status: draft
priority: must
created: 2026-06-05T09:30:00Z
assigned_bolt: 069-refund-return-flow-ui
implemented: false
---

# Story: 001-admin-refund-action

## User Story

**As an** admin
**I want** a refund action on the order-detail view
**So that** I can issue full or partial refunds with a reason, without leaving the app

## Acceptance Criteria

- [ ] **Given** the admin order-detail page, **When** an order is refundable, **Then** a Refund action opens a modal (optional amount, required reason)
- [ ] **Given** the modal submitted, **When** the endpoint succeeds, **Then** the order shows the refunded state + amount
- [ ] **Given** an error code from the API, **When** returned, **Then** it maps to Romanian copy
- [ ] **Given** a non-admin, **When** they reach the route, **Then** access is denied

## Technical Notes

- Reuse `BaseApiService` (intent 030 P26) if available; confirm the irreversible action.

## Dependencies

### Requires
- 031/001/004-admin-refund-endpoint

### Enables
- None

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Already-refunded order | Action disabled / shows refunded state |

## Out of Scope

- Backend refund logic (unit 001).
