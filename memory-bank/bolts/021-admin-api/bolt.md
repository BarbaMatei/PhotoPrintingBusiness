---
id: 021-admin-api
unit: 001-admin-api
intent: 007-admin-panel
type: ddd-construction-bolt
status: completed
stories:
  - 001-admin-orders-api
  - 002-admin-stats-api
  - 003-admin-signalr-hub
created: 2026-05-22T12:00:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [020-transactional-emails, 018-orders-api]
enables_bolts: [022-admin-ui]
requires_units: []
blocks: false

complexity:
  avg_complexity: 3
  avg_uncertainty: 2
  max_dependencies: 2
  testing_scope: 3
---

# Bolt: 021-admin-api

## Overview

Build all admin-only backend endpoints: order management (list, detail, status transitions with email triggers, ZIP download, cancel+refund), analytics (4 stat endpoints with caching), and the SignalR `AdminOrderHub` for real-time broadcasting.

## Objective

By the end of this bolt the admin frontend can manage every order through its lifecycle, view business KPIs, download photo ZIPs, cancel orders with automatic refunds, and receive real-time notifications.

## Stories Included

- **001-admin-orders-api**: Full order management CRUD + status transitions + ZIP + cancel+refund + notes (Must)
- **002-admin-stats-api**: KPI summary + revenue chart + product stats + orders-by-status, cached 5 min (Must)
- **003-admin-signalr-hub**: `AdminOrderHub` broadcasting `NewOrderReceived` + `OrderStatusChanged` (Must)

## Bolt Type

`ddd-construction-bolt` — complex backend with domain transitions, external API calls (Stripe/EuPlatesc refunds), SignalR, and streaming ZIP responses.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Domain Model | `ddd-01-domain-model.md` — status transition rules, refund flow, stats query design |
| 2 | Technical Design | `ddd-02-technical-design.md` — controllers, services, SignalR hub, DTO shapes |
| 3 | ADR | `ddd-03-adr.md` — streaming ZIP vs temp file; InMemoryCache vs Redis at MVP |
| 4 | Implement | Code: AdminOrdersController, AdminStatsController, AdminOrderService, AdminStatsService, AdminOrderHub |
| 5 | Test | `ddd-04-test-report.md` — integration tests for status transitions, auth enforcement, stats endpoints |

## Dependencies

- **Requires**: bolt `020-transactional-emails` (email calls on Shipped/Delivered)
- **Requires**: bolt `018-orders-api` (Order aggregate, existing OrderService)
- **Enables**: bolt `022-admin-ui`

## Key Technical Notes

- `Order.InternalNotes` — add nullable string column (EF migration if not present)
- `Order.AwbNumber` / `Order.TrackingUrl` — set on status → Shipped
- Stripe refund: `StripeClient.RefundService.CreateAsync(new RefundCreateOptions { PaymentIntent = order.PaymentIntentId })`
- EuPlatesc refund: `IEuPlatescService.RefundAsync(order.EuPlatescTransactionId, order.TotalRon)`
- SignalR hub: requires `services.AddSignalR()` and `app.MapHub<AdminOrderHub>("/hubs/admin-orders")`
