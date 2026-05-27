---
id: 019-orders-ui
unit: 002-orders-ui
intent: 005-order-management
type: simple-construction-bolt
status: completed
stories:
  - 003-order-status-pipe
  - 001-order-history-page
  - 002-order-detail-page
created: 2026-05-22T07:10:00Z
started: 2026-05-22T11:00:00Z
completed: 2026-05-22T11:45:00Z
current_stage: null
stages_completed: [plan, implement, test]

requires_bolts: [018-orders-api]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 2
---

# Bolt: 019-orders-ui

## Overview

Implement the Angular Order Management UI: shared `OrderStatusPipe` + constants, the `/comenzi` Order History page, and the `/comenzi/:id` Order Detail page.

## Objective

By the end of this bolt authenticated users can view their order history and drill into individual order details, with consistent Romanian status labels across all pages.

## Stories Included

- **003-order-status-pipe**: `OrderStatusPipe` + `STATUS_ORDER` constants (Must) — implement first
- **001-order-history-page**: Paginated order list at `/comenzi` (Must)
- **002-order-detail-page**: Full order detail at `/comenzi/:id` (Must)

## Bolt Type

`simple-construction-bolt` — Angular feature with models, a service, two pages, and a pipe.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Design | `implementation-plan.md` — component tree, service API, route plan |
| 2 | Implement | Code: `OrderStatusPipe`, `OrderService`, `OrderHistoryPage`, `OrderDetailPage`, routes |
| 3 | Test | Spec files for pipe, service, and both pages; verify all AC |

## Implementation Order (within bolt)

1. `order-status.constants.ts` + `order-status.pipe.ts` — no dependencies
2. `order.service.ts` — depends on constants
3. `order-history-page.ts` — depends on service + pipe
4. `order-detail-page.ts` — depends on service + pipe
5. Update `app.routes.ts` with `/comenzi` and `/comenzi/:id` routes
6. Refactor `ConfirmationPage` to import `STATUS_ORDER` / `isAtLeast` from constants

## Dependencies

- **Requires**: bolt `018-orders-api` (endpoint contracts)
- **Enables**: nothing (Phase 4 complete)

## Acceptance Definition

- All AC in stories 001, 002, and 003 pass
- `OrderStatusPipe` has unit tests for all 6 statuses + unknown fallback
- History page: list renders, pagination works, guest redirected
- Detail page: items render, 403/404 → navigate to `/comenzi`
- `ConfirmationPage` still passes its existing 6 tests after refactor
