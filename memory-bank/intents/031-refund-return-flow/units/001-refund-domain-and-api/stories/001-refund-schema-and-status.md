---
id: 001-refund-schema-and-status
unit: 001-refund-domain-and-api
intent: 031-refund-return-flow
status: draft
priority: must
created: 2026-06-05T09:30:00Z
assigned_bolt: 068-refund-domain-and-api
implemented: false
---

# Story: 001-refund-schema-and-status

## User Story

**As a** developer building refunds
**I want** the order/invoice refund schema and a `Refunded` terminal status
**So that** refunds and credit-notes have a place to live and a valid state transition

## Acceptance Criteria

- [ ] **Given** a migration, **When** applied, **Then** `Orders` gains `RefundedAt`, `RefundAmountRon`, `RefundReason`; `Invoices` gains `InvoiceType` (Final|CreditNote, default Final) + `OriginalInvoiceId` FK + partial index
- [ ] **Given** `OrderStatus`, **When** extended, **Then** `Refunded` is a terminal state and `OrderStatusMachine` allows `Paid`/`Delivered` → `Refunded` and rejects illegal transitions
- [ ] **Given** existing invoices, **When** the migration runs, **Then** they default to `InvoiceType = 'Final'`
- [ ] **Given** the state-machine change, **When** unit-tested, **Then** legal/illegal transitions are covered

## Technical Notes

- Lands under `Infrastructure/Data` + `Domain/Orders` (post-027). Review the `Add-Migration` diff carefully.

## Dependencies

### Requires
- 027 (placement)

### Enables
- 002-refund-service-stripe-euplatesc, 003-anaf-credit-note

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Refund attempted on unpaid order | State machine rejects |

## Out of Scope

- The refund service + endpoint (later stories).
