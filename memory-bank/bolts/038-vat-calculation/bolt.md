---
id: 038-vat-calculation
unit: 001-vat-calculation
intent: 016-romanian-vat-efactura
type: ddd-construction-bolt
status: complete
stories:
  - 001-vat-fields-and-computation
  - 002-invoice-entity-and-numbering
created: 2026-05-25T10:15:00.000Z
started: 2026-06-03T04:00:00.000Z
completed: "2026-06-03T10:32:07Z"
current_stage: null
stages_completed:
  - name: domain-model
    completed: 2026-06-03T04:30:00.000Z
    artifact: ddd-01-domain-model.md
  - name: technical-design
    completed: 2026-06-03T05:00:00.000Z
    artifact: ddd-02-technical-design.md
  - name: adr-analysis
    completed: 2026-06-03T05:00:00.000Z
    artifacts:
      - adr-019-decimal-rounding-away-from-zero-for-regulatory-math.md
      - adr-020-postgres-sequence-for-invoice-numbering-accept-gap-on-rollback.md
  - name: implement
    completed: 2026-06-03T05:30:00.000Z
    artifact: src code + migration
requires_bolts:
  - 015-shipping-and-order-core
  - 035-payment-idempotency
enables_bolts:
  - 039-efactura-anaf
requires_units: []
blocks: false
complexity:
  avg_complexity: 3
  avg_uncertainty: 2
  max_dependencies: 2
  testing_scope: 3
---

# Bolt: 038-vat-calculation

## Overview

Schema, math, and numbering. Establishes the legal-grade foundation that the next bolt consumes.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Domain Model | VAT formula invariants, sequence-per-series-per-year design, gap-free guarantee |
| 2 | Technical Design | Migration scripts, `IInvoiceNumberingService` contract, rounding rules |
| 3 | Implement | Migrations + service + tests |
| 4 | Test | `ddd-03-test-report.md` — formula property tests, sequence concurrency test |

## Dependencies

- **Requires**: 015-shipping-and-order-core, 035-payment-idempotency.
- **Enables**: 039-efactura-anaf.
