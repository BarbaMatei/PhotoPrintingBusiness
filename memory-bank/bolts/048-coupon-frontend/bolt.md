---
id: 048-coupon-frontend
unit: 002-coupon-frontend
intent: 022-coupon-promo-codes
type: simple-construction-bolt
status: planned
stories:
  - 001-cart-coupon-ux
created: 2026-05-25T10:45:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [047-coupon-domain-and-api, 014-upload-format-cart-ui, 039-efactura-anaf]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 2
  max_dependencies: 3
  testing_scope: 2
---

# Bolt: 048-coupon-frontend

## Overview

Single-story FE bolt — cart input, Romanian copy mapping, summary updates, invoice PDF line.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Plan | `implementation-plan.md` — component additions, copy table, PDF template diff |
| 2 | Implement | Cart page + summary + review/confirmation + invoice template |
| 3 | Test | Spec files for cart coupon flow + visual check on PDF |

## Dependencies

- **Requires**: 047-coupon-domain-and-api, 014-upload-format-cart-ui, 039-efactura-anaf.
- **Enables**: customer-visible launch.
