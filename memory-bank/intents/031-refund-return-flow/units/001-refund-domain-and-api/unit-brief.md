---
unit: 001-refund-domain-and-api
intent: 031-refund-return-flow
phase: inception
status: draft
unit_type: backend
default_bolt_type: ddd-construction-bolt
created: 2026-06-05T09:30:00Z
updated: 2026-06-05T09:30:00Z
---

# Unit Brief: Refund Domain & API

## Purpose

Wire the full server-side refund flow: schema + terminal `Refunded` status, the gateway refund service, the ANAF credit-note, and the admin endpoint — keeping the DB, payment gateway, and ANAF consistent. Genuine new domain → `ddd-construction-bolt`.

## Scope

### In Scope
- `Orders.RefundedAt/RefundAmountRon/RefundReason`; `OrderStatus.Refunded`; `Invoice.InvoiceType`(Final|CreditNote) + `OriginalInvoiceId`.
- `IRefundService` (full + partial); Stripe + EuPlatesc paths; credit-note UBL (type 381); `POST /api/admin/orders/{id}/refund`.

### Out of Scope
- The admin UI (unit 002).
- Re-implementing VAT (reuse bolt 038 `VatCalculator`).

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-1 (P09) | Order/Invoice refund schema + state machine | Must |
| FR-2 (P09) | Refund service (full/partial, Stripe + EuPlatesc) | Must |
| FR-3 (P09) | ANAF credit-note generation + submission | Must |
| FR-4 (P09) | Admin refund endpoint | Must |

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|--------|-------------|------------|
| Order (refund fields) | Refund state | RefundedAt, RefundAmountRon, RefundReason, Status=Refunded |
| Invoice (credit-note) | Negative-amount invoice | InvoiceType=CreditNote, OriginalInvoiceId |

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| RefundOrderAsync | Full/partial refund | orderId, amount?, reason | RefundResult |
| Generate credit-note | UBL type 381 | original invoice, refunded amount | credit-note row |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 4 |
| Must Have | 4 |
| Should Have | 0 |
| Could Have | 0 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-refund-schema-and-status | Refund schema + Refunded status | Must | Planned |
| 002-refund-service-stripe-euplatesc | Refund service across gateways | Must | Planned |
| 003-anaf-credit-note | ANAF credit-note (type 381) | Must | Planned |
| 004-admin-refund-endpoint | Admin refund endpoint | Must | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 027 (all) | Lands in Application/Refunds + Infrastructure/Payments |
| 029/001 | Reuses `Policies.Admin` |

### Depended By
| Unit | Reason |
|------|--------|
| 002-refund-return-flow-ui | Consumes the refund endpoint |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| Stripe | Refund-create | Medium |
| EuPlatesc | Refund / manual Z-report | High (API may be absent) |
| ANAF SPV | Credit-note submission | High (regulated) |

---

## Technical Context

### Suggested Technology
EF Core migration; `OrderStatusMachine` update; QuestPDF/UBL credit-note; reuse `InvoiceUploadJob`.

### Data Storage
| Data | Type | Volume | Retention |
|------|------|--------|-----------|
| Refund fields + credit-notes | PostgreSQL | low | per fiscal retention |

---

## Constraints

- Partial refunds spread proportionally across line items (documented).
- A refunded order must NOT auto-purge originals on the Shipped trigger (bolt 052 interaction).
- Idempotent — no double-refund at the gateway.

---

## Success Criteria

### Functional
- [ ] Full/partial refund updates order, refunds gateway, and issues a credit-note in one transaction.
- [ ] Credit-note (type 381) validates and is submitted by `InvoiceUploadJob`.
- [ ] `POST /api/admin/orders/{id}/refund` admin-only; invalid states → 409/422.

### Non-Functional
- [ ] DB/gateway/ANAF reconcilable; idempotent.

### Quality
- [ ] State-machine + refund-service unit tests; integration test for the endpoint.

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 068-refund-domain-and-api | DDD | 001–004 | Full refund flow incl. ANAF credit-note |

---

## Notes

Pre-launch must-have IF EU launch (Open Question Q1 in requirements). Intersects regulated paths — plan dedicated review.
