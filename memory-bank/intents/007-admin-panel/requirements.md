---
intent: 007-admin-panel
phase: inception
status: complete
created: 2026-05-22T12:00:00Z
updated: 2026-05-22T12:00:00Z
---

# Requirements: Admin Panel

## Intent Overview

Build the operator-facing administration interface for FotoTipar. Includes a stats API, a full order management API with SignalR real-time updates, and four Angular admin pages: Dashboard (KPI + charts), Order Queue (real-time FIFO list), Order Detail & Workflow (status transitions, ZIP download, refund), and Product Management (price/active toggle).

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Operator sees all paid orders in real time | New orders appear via SignalR within 2 s | Must |
| Operator can move orders through the print workflow | Status transitions Paid→Printing→Shipped→Delivered work | Must |
| Operator can view KPI metrics at a glance | Dashboard loads with charts in < 2 s | Must |
| Operator can adjust product prices without code deploy | Price change reflected on storefront within 60 s | Must |
| Operator can cancel and refund orders | Stripe/EuPlatesc refund initiated; status = Cancelled | Must |

---

## Functional Requirements

### FR-1: Admin Stats API
- **Description**: Aggregated KPI and chart data endpoints, all scoped to admin role.
- **Acceptance Criteria**: `GET /api/admin/stats/summary` → `{ordersToday, revenueToday, ordersThisMonth, revenueThisMonth}`; `GET /api/admin/stats/revenue?days=30` → `[{date, totalRon}]`; `GET /api/admin/stats/products` → top products; `GET /api/admin/stats/orders-by-status`; all cached 5 min.
- **Priority**: Must
- **Related Stories**: US-505

### FR-2: Admin Orders API
- **Description**: Full CRUD for order management: list all orders, view detail, patch status, download ZIP of photos, cancel with refund, save internal notes. SignalR hub broadcasts new orders and status changes.
- **Acceptance Criteria**: All `/api/admin/*` require `[Authorize(Roles="Admin")]`; status transitions validated (Paid→Printing→Shipped→Delivered); ZIP streams all order photo files; cancel triggers Stripe or EuPlatesc refund based on `PaymentProcessor` field; `AdminOrderHub` broadcasts `NewOrderReceived` and `OrderStatusChanged`.
- **Priority**: Must
- **Related Stories**: US-504

### FR-3: Admin Dashboard (Frontend)
- **Description**: Angular page at `/admin` with KPI cards and 3 charts (donut, bar, horizontal bar). Auth-guarded to Admin role. Auto-refreshes every 5 min.
- **Priority**: Must
- **Related Stories**: US-501

### FR-4: Admin Order Queue (Frontend)
- **Description**: Angular page at `/admin/comenzi` with real-time order table (SignalR), filter tabs, search, bulk "Marchează ca În imprimare" action.
- **Priority**: Must
- **Related Stories**: US-502

### FR-5: Admin Order Detail & Workflow (Frontend)
- **Description**: Side panel or full page at `/admin/comenzi/:id`. Shows all items, customer info, workflow buttons, ZIP download, AWB input, cancel/refund, internal notes.
- **Priority**: Must
- **Related Stories**: US-503

### FR-6: Admin Product Management (Frontend)
- **Description**: Angular page at `/admin/produse`. Data table with inline active toggle and edit dialog for all product fields.
- **Priority**: Must
- **Related Stories**: US-506

---

## Non-Functional Requirements

| Category | Requirement |
|----------|-------------|
| Security | All admin endpoints require Admin role JWT; no guest access |
| Real-time | SignalR hub for new orders and status changes |
| Performance | Stats queries cached 5 min; revenue query uses indexed CreatedAt, Status columns |
| File handling | ZIP download streams (no temp file); UUID-named paths only |
| Refund | Stripe `RefundAsync` via Stripe SDK; EuPlatesc refund via API call |

---

## Out of Scope

- Multi-operator / permissions system
- Email sending from admin panel
- Sameday AWB auto-generation (Phase 2 — FR noted but not implemented)
- Operator authentication (uses same Auth system)
