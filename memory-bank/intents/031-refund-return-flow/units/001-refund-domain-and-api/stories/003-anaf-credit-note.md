---
id: 003-anaf-credit-note
unit: 001-refund-domain-and-api
intent: 031-refund-return-flow
status: draft
priority: must
created: 2026-06-05T09:30:00Z
assigned_bolt: 068-refund-domain-and-api
implemented: false
---

# Story: 003-anaf-credit-note

## User Story

**As a** business meeting Romanian fiscal law
**I want** a credit-note generated and submitted to ANAF on refund
**So that** the refund is fiscally correct and ANAF stays in sync

## Acceptance Criteria

- [ ] **Given** a refund, **When** processed, **Then** a credit-note `Invoice` (InvoiceType=CreditNote) is created referencing the original via `OriginalInvoiceId` with negative amounts and correct VAT reversal
- [ ] **Given** the credit-note UBL, **When** generated, **Then** `cbc:InvoiceTypeCode` is 381 and it validates against the e-Factura schema
- [ ] **Given** `InvoiceUploadJob` (filters Pending+Submitted regardless of type), **When** it runs, **Then** it submits the credit-note without type-specific changes
- [ ] **Given** the submission, **When** observed, **Then** its lifecycle shows in the invoice metrics (intent 026 P17)

## Technical Notes

- VAT reversal reuses bolt 038 `VatCalculator` semantics (ADR-019 rounding).

## Dependencies

### Requires
- 002-refund-service-stripe-euplatesc

### Enables
- 004-admin-refund-endpoint

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Partial refund | Credit-note for the refunded portion only |
| ANAF rejects credit-note | Surfaces via metrics + admin retry (existing tooling) |

## Out of Scope

- Admin endpoint/UI (later).
