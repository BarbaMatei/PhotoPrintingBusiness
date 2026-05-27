---
unit: 002-admin-ui
intent: 007-admin-panel
phase: inception
status: ready
created: 2026-05-22T12:00:00Z
updated: 2026-05-22T12:00:00Z
default_bolt_type: simple-construction-bolt
---

# Unit Brief: 002-admin-ui

## Purpose

Build the 4 Angular admin pages: Dashboard (KPIs + charts), Order Queue (real-time, filterable), Order Detail & Workflow (status buttons, ZIP, refund, notes), and Product Management (inline price/active editing).

## Scope

### In Scope
- `/admin` — Dashboard page: KPI cards, donut chart (orders by status), bar chart (revenue 30d), horizontal bar (top products); auto-refresh 5 min
- `/admin/comenzi` — Order Queue page: table + SignalR subscription for new orders; filter tabs; bulk status action; search
- `/admin/comenzi/:id` — Order Detail page: full order, workflow buttons, ZIP download, AWB input, cancel+refund, internal notes
- `/admin/produse` — Product Management page: table with inline active toggle, edit dialog
- `AdminService` — HTTP client for all `/api/admin/*` calls
- SignalR connection management (HubConnection from `@microsoft/signalr`)
- `ng2-charts` (Chart.js) for dashboard charts
- Route registration under `/admin` with `adminGuard`

### Out of Scope
- Admin login (uses same auth system as customers)
- Sameday AWB auto-generation UI (Phase 2)

---

## Domain Concepts

### Key Entities
| Entity | Description |
|--------|-------------|
| `AdminOrderSummaryDto` | Table row: orderNumber, customerName, itemCount, status, createdAt |
| `AdminOrderDetailDto` | Full admin view including internalNotes, awbNumber |
| `AdminStatsDto` | Summary KPI metrics |
| `RevenueDataPointDto` | `{date, totalRon}` for bar chart |

### Key Operations
| Operation | Service Method |
|-----------|----------------|
| Load dashboard stats | `AdminService.getStats()`, `getRevenue()`, `getProductStats()`, `getOrdersByStatus()` |
| Load order queue | `AdminService.getAdminOrders(filters)` |
| Update order status | `AdminService.updateOrderStatus(id, status, awbNumber?)` |
| Download ZIP | Navigate to `/api/admin/orders/{id}/download-zip` (new tab) |
| Cancel order | `AdminService.cancelOrder(id)` |
| Save notes | `AdminService.saveNotes(id, notes)` |
| Listen for new orders | SignalR `on('NewOrderReceived')` |

## Story Summary

| # | Story | Priority |
|---|-------|----------|
| 001 | `001-admin-dashboard-page` | Must |
| 002 | `002-admin-order-queue-page` | Must |
| 003 | `003-admin-order-detail-page` | Must |
| 004 | `004-admin-product-management-page` | Must |
