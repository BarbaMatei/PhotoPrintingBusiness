---
unit: 001-orders-api
intent: 005-order-management
phase: inception
status: ready
created: 2026-05-22T07:10:00Z
updated: 2026-05-22T07:10:00Z
default_bolt_type: ddd-construction-bolt
---

# Unit Brief: 001-orders-api

## Purpose

Expose a secure, paginated Orders API on top of the existing `Order` entity so authenticated customers can retrieve their order history and full order details.

## Scope

### In Scope
- `GET /api/orders` — paginated list of orders owned by current user
- `GET /api/orders/{id}` — full order detail with 403 ownership enforcement
- `OrderSummaryDto` and `OrderDetailDto` response shapes
- Pagination via `page` / `pageSize` query params; `X-Total-Count` header
- EF Core queries against existing `Orders`, `OrderItems`, `Uploads` tables

### Out of Scope
- Creating or updating orders (handled by payment flow — bolts 015–017)
- Admin order management (Phase 6 — US-504)
- Order cancellation
- Order search or filtering beyond pagination

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-1 | `GET /api/orders` — paginated list, newest first | Must |
| FR-2 | `GET /api/orders/{id}` — full detail, 403 ownership | Must |
| FR-5 | Shared `OrderStatus` constants used in DTOs | Must |

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|--------|-------------|------------|
| `Order` | A completed checkout (exists) | Id, UserId, OrderNumber, Status, TotalRon, SubtotalRon, ShippingCostRon, DeliveryType, PaymentProcessor, CreatedAt, PaidAt |
| `OrderItem` | A line item within an order (exists) | Id, OrderId, UploadId, ProductId, Quantity, UnitPriceRon, LineTotal |
| `Upload` | Photo upload referenced by item (exists) | Id, PreviewUrl, FileName |

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| ListOrders | Paginated query filtered by UserId | JWT identity, page, pageSize | OrderSummaryDto[], X-Total-Count |
| GetOrderDetail | Single order with items | orderId, JWT identity | OrderDetailDto or 403/404 |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 2 |
| Must Have | 2 |
| Should Have | 0 |
| Could Have | 0 |

### Stories
| # | Story | Priority |
|---|-------|----------|
| 001 | `001-orders-list-endpoint` | Must |
| 002 | `002-order-detail-endpoint` | Must |
