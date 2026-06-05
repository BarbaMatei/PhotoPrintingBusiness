---
id: 004-admin-refund-endpoint
unit: 001-refund-domain-and-api
intent: 031-refund-return-flow
status: draft
priority: must
created: 2026-06-05T09:30:00Z
assigned_bolt: 068-refund-domain-and-api
implemented: false
---

# Story: 004-admin-refund-endpoint

## User Story

**As an** admin
**I want** an endpoint to refund an order with an optional amount and reason
**So that** I can honour the 14-day right of withdrawal from within the app

## Acceptance Criteria

- [ ] **Given** `POST /api/admin/orders/{id}/refund { amount?, reason }`, **When** called by an admin, **Then** it invokes `IRefundService` and returns the updated order + refund result
- [ ] **Given** the endpoint, **When** secured, **Then** it uses `Policies.Admin` (intent 029 P08); non-admin → 401/403
- [ ] **Given** an invalid state (already refunded, unpaid) or amount > refundable, **When** requested, **Then** it returns 409/422 with a `code:`
- [ ] **Given** no customer-facing endpoint, **When** the API surface is reviewed, **Then** refunds are admin-initiated only

## Technical Notes

- Lands in `Web/Controllers/AdminRefundsController` (post-027).

## Dependencies

### Requires
- 003-anaf-credit-note (full flow wired)

### Enables
- 031/002 admin refund UI

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Concurrent double refund | Idempotent; second returns conflict |

## Out of Scope

- The UI (unit 002).
