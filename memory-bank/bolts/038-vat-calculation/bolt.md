---
id: 038-vat-calculation
unit: 001-vat-calculation
intent: 016-romanian-vat-efactura
type: ddd-construction-bolt
status: planned
stories:
  - 001-vat-fields-and-computation
  - 002-invoice-entity-and-numbering
created: 2026-05-25T10:15:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [015-shipping-and-order-core, 035-payment-idempotency]
enables_bolts: [039-efactura-anaf]
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
