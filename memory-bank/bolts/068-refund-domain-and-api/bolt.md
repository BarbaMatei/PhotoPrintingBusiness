---
id: 068-refund-domain-and-api
unit: 001-refund-domain-and-api
intent: 031-refund-return-flow
type: ddd-construction-bolt
status: planned
stories:
  - 001-refund-schema-and-status
  - 002-refund-service-stripe-euplatesc
  - 003-anaf-credit-note
  - 004-admin-refund-endpoint
created: 2026-06-05T09:30:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [059-layering-foundation, 063-access-hardening]
enables_bolts: [069-refund-return-flow-ui]
requires_units: [001-layering-foundation, 001-access-hardening]
blocks: false

complexity:
  avg_complexity: 3
  avg_uncertainty: 2
  max_dependencies: 3
  testing_scope: 3
---

# Bolt: 068-refund-domain-and-api

## Overview

Full server-side refund flow (P09): schema + Refunded status, refund service across gateways, ANAF credit-note (type 381), and the admin endpoint. New regulated domain → DDD bolt.

## Objective

Honour the EU 14-day right of withdrawal with fiscal correctness, keeping DB / gateway / ANAF consistent.

## Stories Included

- **001-refund-schema-and-status**: Schema + Refunded terminal state (Must)
- **002-refund-service-stripe-euplatesc**: Refund service, full/partial (Must)
- **003-anaf-credit-note**: Credit-note UBL 381 + submission (Must)
- **004-admin-refund-endpoint**: Admin refund endpoint (Must)

## Bolt Type

**Type**: ddd-construction-bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/ddd-construction-bolt.md`

## Stages

- [ ] **1. model**: Pending → ddd-01-domain-model.md (refund + credit-note domain)
- [ ] **2. design**: Pending → ddd-02-technical-design.md (gateways, UBL, transaction)
- [ ] **3. implement**: Pending → Application/Refunds + Infrastructure/Payments + migration
- [ ] **4. test**: Pending → ddd-03-test-report.md (state machine, idempotency, credit-note, endpoint)

## Dependencies

### Requires
- 059-layering-foundation (placement)
- 063-access-hardening (Policies.Admin)

### Enables
- 069-refund-return-flow-ui

## Success Criteria

- [ ] Full/partial refund consistent across DB/gateway/ANAF; idempotent
- [ ] Credit-note (381) validates + submitted by InvoiceUploadJob
- [ ] Admin-only endpoint; invalid states → 409/422

## Notes

Pre-launch must-have IF EU launch (requirements Q1). Intersects bolt 039 (ANAF) + bolt 052 (archive purge) — dedicated review. Reuse bolt 038 VatCalculator.
