---
unit: 002-orders-ui
intent: 005-order-management
phase: inception
status: ready
created: 2026-05-22T07:10:00Z
updated: 2026-05-22T07:10:00Z
default_bolt_type: simple-construction-bolt
---

# Unit Brief: 002-orders-ui

## Purpose

Deliver the Angular Order History List and Order Detail pages — standalone components that allow authenticated customers to view and inspect their past orders.

## Scope

### In Scope
- `/comenzi` — Order History List page (auth-guarded, paginated)
- `/comenzi/:id` — Order Detail page (auth-guarded, full breakdown)
- `OrderStatusPipe` — Romanian status label pipe (Pending → Livrat) shared across pages
- Status badge component / styling integrated into history list and detail
- Routes registered in `app.routes.ts`

### Out of Scope
- Backend Orders API (handled by unit 001-orders-api)
- Admin order views (Phase 6)
- Order cancellation UI
- Invoice PDF download

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-3 | `/comenzi` — order history list, auth-guarded, paginated | Must |
| FR-4 | `/comenzi/:id` — full detail: items, delivery, status stepper | Must |
| FR-5 | Shared Romanian status labels/pipe across pages | Must |

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|--------|-------------|------------|
| `OrderSummaryDto` | List item shape | id, orderNumber, status, totalRon, createdAt, deliveryType, itemCount |
| `OrderDetailDto` | Detail shape | all of above + items[], shippingAddress/lockerId, paymentProcessor, paidAt |
| `OrderItemDto` | Line item in detail | uploadId, previewUrl, quantity, unitPriceRon, lineTotal |

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| Load history | GET /api/orders?page&pageSize | page signal | OrderSummaryDto[] + total |
| Load detail | GET /api/orders/{id} | route param orderId | OrderDetailDto or 404→redirect |
| Format status | Transform status string | status string | Romanian label string |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 3 |
| Must Have | 3 |
| Should Have | 0 |
| Could Have | 0 |

### Stories
| # | Story | Priority |
|---|-------|----------|
| 001 | `001-order-history-page` | Must |
| 002 | `002-order-detail-page` | Must |
| 003 | `003-order-status-pipe` | Must |
