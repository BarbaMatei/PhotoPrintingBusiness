---
id: 047-coupon-domain-and-api
unit: 001-coupon-domain-and-api
intent: 022-coupon-promo-codes
type: ddd-construction-bolt
status: in-progress
stories:
  - 001-coupon-schema
  - 002-cart-coupon-endpoints
  - 003-redemption-on-order-create
  - 004-admin-coupon-crud
created: 2026-05-25T10:45:00Z
started: 2026-09-03T20:42:00Z
completed: null
current_stage: test
stages_completed:
  - name: domain-model
    completed: 2026-09-03T20:50:00Z
    artifact: ddd-01-domain-model.md
  - name: technical-design
    completed: 2026-09-04T00:30:00Z
    artifact: ddd-02-technical-design.md
  - name: adr-analysis
    completed: 2026-09-04T00:55:00Z
    artifact: adr-025-coupon-redemption-cas-not-rowversion.md, adr-026-discount-before-vat-extraction.md
  - name: implement
    completed: 2026-09-04T01:40:00Z
    artifact: src/PhotoPrint.API (coupon domain, endpoints, migration)

requires_bolts: [038-vat-calculation, 015-shipping-and-order-core]
enables_bolts: [048-coupon-frontend]
requires_units: []
blocks: false

complexity:
  avg_complexity: 3
  avg_uncertainty: 2
  max_dependencies: 2
  testing_scope: 4
---

# Bolt: 047-coupon-domain-and-api

## Overview

Schema, customer endpoints, atomic redemption on order creation, admin CRUD.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Domain Model | `ddd-01-domain-model.md` — coupon types, lifecycle, redemption invariants, VAT order |
| 2 | Technical Design | `ddd-02-technical-design.md` — endpoints, validators, concurrency strategy |
| 3 | Implement | Migrations + services + controllers |
| 4 | Test | `ddd-03-test-report.md` — concurrent-redemption integration test, validation matrix |

## Dependencies

- **Requires**: 038-vat-calculation (must subtract pre-VAT), 015-shipping-and-order-core.
- **Enables**: 048-coupon-frontend.

## Key Technical Notes

- Concurrent redemption test is the single most important guarantee — gate the bolt on it.
- Discount-then-VAT math must be documented in `decision-index.md` because it's irreversible once invoices are issued.
