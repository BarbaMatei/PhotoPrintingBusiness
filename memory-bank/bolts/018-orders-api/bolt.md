---
id: 018-orders-api
unit: 001-orders-api
intent: 005-order-management
type: ddd-construction-bolt
status: completed
stories:
  - 001-orders-list-endpoint
  - 002-order-detail-endpoint
created: 2026-05-22T07:10:00Z
started: 2026-05-22T07:25:00Z
completed: 2026-05-22T08:00:00Z
current_stage: done
stages_completed: [domain-model, technical-design, implement, test]

requires_bolts: [016-payment-backends]
enables_bolts: [019-orders-ui]
requires_units: []
blocks: false

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 2
---

# Bolt: 018-orders-api

## Overview

Implement the Orders API backend — two read-only endpoints (`GET /api/orders` and `GET /api/orders/{id}`) on top of the existing Order entity from bolt 015.

## Objective

By the end of this bolt the Angular frontend can fetch a paginated order list and full order detail for any authenticated user, with ownership enforcement and proper pagination headers.

## Stories Included

- **001-orders-list-endpoint**: Paginated `GET /api/orders` with `X-Total-Count` (Must)
- **002-order-detail-endpoint**: Ownership-checked `GET /api/orders/{id}` with full DTOs (Must)

## Bolt Type

`ddd-construction-bolt` — backend domain work with EF Core queries, DTOs, and ownership logic.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Domain Model | `ddd-01-domain-model.md` — DTO shapes, query design, ownership rules |
| 2 | Technical Design | `ddd-02-technical-design.md` — controller, service, EF Core queries, pagination |
| 3 | Implement | Code changes in `OrdersController`, `IOrderService`, `OrderService`, DTOs |
| 4 | Test | `ddd-03-test-report.md` — integration tests for both endpoints |

## Dependencies

- **Requires**: bolt `016-payment-backends` (Order entity, OrderItem entity — ✅ complete)
- **Enables**: bolt `019-orders-ui`

## Acceptance Definition

- All AC in stories 001 and 002 pass
- Integration tests cover: list pagination, empty list, detail 200/403/404
- No new EF migrations needed (reads only existing schema)
