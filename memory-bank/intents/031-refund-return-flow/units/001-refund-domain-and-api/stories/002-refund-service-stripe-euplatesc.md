---
id: 002-refund-service-stripe-euplatesc
unit: 001-refund-domain-and-api
intent: 031-refund-return-flow
status: draft
priority: must
created: 2026-06-05T09:30:00Z
assigned_bolt: 068-refund-domain-and-api
implemented: false
---

# Story: 002-refund-service-stripe-euplatesc

## User Story

**As an** admin
**I want** a refund service that executes full or partial refunds across gateways
**So that** the money is actually returned and the order reflects it

## Acceptance Criteria

- [ ] **Given** `IRefundService.RefundOrderAsync(orderId, amount?, reason, ct)`, **When** a full refund runs, **Then** the gateway is refunded, `Order.Status=Refunded`, and `RefundedAt`/`RefundAmountRon`/`RefundReason` are stamped
- [ ] **Given** a partial refund, **When** it runs, **Then** the partial amount is recorded (status policy per Open Question Q2) and spread proportionally across line items
- [ ] **Given** a duplicate refund request, **When** submitted, **Then** the gateway is not double-refunded (idempotent)
- [ ] **Given** Stripe, **When** refunding, **Then** it calls refund-create against the PaymentIntent; **Given** EuPlatesc, **When** no API exists, **Then** a flagged manual Z-report path is used
- [ ] **Given** the refund, **When** it commits, **Then** it is transactional with the credit-note creation (story 003)

## Technical Notes

- Reuse bolt 038 `VatCalculator` for the refunded-portion VAT.

## Dependencies

### Requires
- 001-refund-schema-and-status

### Enables
- 003-anaf-credit-note, 004-admin-refund-endpoint

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Gateway refund succeeds, DB commit fails | Reconcilable; retry-safe (idempotent) |
| Partial > refundable balance | Rejected (422) |

## Out of Scope

- ANAF credit-note submission (next story).
