---
id: 022-admin-ui
unit: 002-admin-ui
intent: 007-admin-panel
type: simple-construction-bolt
status: complete
stories:
  - 001-admin-dashboard-page
  - 002-admin-order-queue-page
  - 003-admin-order-detail-page
  - 004-admin-product-management-page
created: 2026-05-22T12:00:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [021-admin-api]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 3
  avg_uncertainty: 2
  max_dependencies: 1
  testing_scope: 2
---

# Bolt: 022-admin-ui

## Overview

Build the 4 Angular admin pages using `AdminService` for HTTP calls and `@microsoft/signalr` for real-time updates. Charts use `ng2-charts` (Chart.js wrapper).

## Objective

By the end of this bolt the operator can manage the full print workflow from a browser: view KPIs, see new orders in real time, move orders through statuses, download photo ZIPs, cancel/refund, and edit product prices.

## Stories Included

- **001-admin-dashboard-page**: `/admin` — KPI cards + 3 charts + auto-refresh (Must)
- **002-admin-order-queue-page**: `/admin/comenzi` — real-time table + filters + bulk action (Must)
- **003-admin-order-detail-page**: `/admin/comenzi/:id` — detail + workflow + ZIP + cancel + notes (Must)
- **004-admin-product-management-page**: `/admin/produse` — table + inline toggle + edit dialog (Must)

## Bolt Type

`simple-construction-bolt` — Angular feature with 4 pages, a service, and SignalR integration.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Plan | `implementation-plan.md` — component tree, service API, SignalR connection strategy |
| 2 | Implement | Source code: AdminService, 4 page components, route registration |
| 3 | Test | Spec files for AdminService and all 4 pages |

## Dependencies

- **Requires**: bolt `021-admin-api` (all `/api/admin/*` endpoints must exist)
- **Enables**: nothing (final phase 6 deliverable)

## Key Technical Notes

- `@microsoft/signalr` package — `HubConnectionBuilder` in `AdminService` or dedicated `AdminHubService`
- `ng2-charts` — `NgChartsModule` for `<canvas baseChart ...>`; install if not present
- `input.required<string>()` for `orderId` on detail page
- All Angular 21 conventions: `@if`/`@for`, `signal()`, `OnPush`, `vi.fn()` in tests
