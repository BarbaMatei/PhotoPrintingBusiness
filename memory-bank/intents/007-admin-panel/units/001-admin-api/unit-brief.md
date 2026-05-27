---
unit: 001-admin-api
intent: 007-admin-panel
phase: inception
status: ready
created: 2026-05-22T12:00:00Z
updated: 2026-05-22T12:00:00Z
default_bolt_type: ddd-construction-bolt
---

# Unit Brief: 001-admin-api

## Purpose

Expose all admin-only backend endpoints: order management (list, detail, status transitions, ZIP download, cancel+refund, notes), analytics (stats/summary, revenue chart, product chart, orders-by-status), and a SignalR hub for real-time order events.

## Scope

### In Scope
- `AdminOrdersController` — all `/api/admin/orders/*` endpoints
- `AdminStatsController` — all `/api/admin/stats/*` endpoints
- `IAdminOrderService` + `AdminOrderService` — business logic
- `IAdminStatsService` + `AdminStatsService` — analytics queries (cached 5 min)
- `AdminOrderHub` (SignalR) — `NewOrderReceived`, `OrderStatusChanged` broadcasts
- Stripe refund integration via `StripeClient.RefundCreateAsync`
- EuPlatesc refund integration via `IEuPlatescService.RefundAsync`
- ZIP download via `System.IO.Compression.ZipArchive` streaming
- DTOs: `AdminOrderSummaryDto`, `AdminOrderDetailDto`, `UpdateOrderStatusRequest`, `AdminStatsDto`, `RevenueDataPointDto`, `ProductStatsDto`

### Out of Scope
- Sameday AWB auto-generation (Phase 2 — API call placeholder only)
- Admin product management (US-506 endpoints already exist in bolt 010-product-catalog-admin)
- Frontend (unit 002-admin-ui)

---

## Domain Concepts

### Key Operations
| Operation | Endpoint | Notes |
|-----------|----------|-------|
| List all orders | `GET /api/admin/orders` | Paginated, filterable by status/search |
| Get order detail | `GET /api/admin/orders/{id}` | Includes internalNotes |
| Update status | `PATCH /api/admin/orders/{id}/status` | Validates transitions; fires SignalR + email |
| Download ZIP | `GET /api/admin/orders/{id}/download-zip` | Streams ZipArchive |
| Cancel order | `POST /api/admin/orders/{id}/cancel` | Refund via Stripe or EuPlatesc |
| Save notes | `PATCH /api/admin/orders/{id}/notes` | Internal notes only |
| Stats summary | `GET /api/admin/stats/summary` | Today + month KPIs |
| Revenue chart | `GET /api/admin/stats/revenue?days=30` | Daily totals |
| Product stats | `GET /api/admin/stats/products` | Top products by order count |
| Orders by status | `GET /api/admin/stats/orders-by-status` | Count per status |
| SignalR | Hub at `/hubs/admin-orders` | Broadcasts to admin clients |

## Technical Constraints

- All endpoints: `[Authorize(Roles = "Admin")]`
- `Order.AwbNumber` and `Order.TrackingUrl` are nullable strings — set on Shipped
- `Order.InternalNotes` is a nullable string — add as EF migration if not present
- Status transitions: Paid→Printing→Shipped→Delivered; any→Cancelled
- Stats queries must use indexed `CreatedAt` and `Status` columns; cache 5 min via `IMemoryCache`
- ZIP: stream directly to response (no temp file); use `Content-Disposition: attachment; filename="order-{number}.zip"`

## Story Summary

| # | Story | Priority |
|---|-------|----------|
| 001 | `001-admin-orders-api` | Must |
| 002 | `002-admin-stats-api` | Must |
| 003 | `003-admin-signalr-hub` | Must |
