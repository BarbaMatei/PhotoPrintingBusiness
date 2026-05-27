---
unit: 002-awb-and-tracking-jobs
intent: 015-sameday-shipping-integration
phase: inception
status: draft
created: 2026-05-25T10:10:00Z
updated: 2026-05-25T10:10:00Z
---

# Unit Brief: AWB Generation & Tracking Jobs

## Purpose

Wire the API client into the order lifecycle: AWB created when an order goes `Paid` (with retry on failure), and `Shipped → Delivered` auto-transition driven by a tracking poll.

## Scope

### In Scope
- AWB creation hook in `OrderStatusMachine` / `OrderService` for `Paid` transitions
- `AwbRetryJob : BackgroundService` — hourly retry of failed AWB creations
- `ShipmentTrackingJob : BackgroundService` — 15-min poll of `Shipped` orders
- Email trigger on `Delivered` transition (reuses existing `EmailQueue`)

### Out of Scope
- Outbound webhooks (we poll, not subscribe)
- AWB cancellation on refund flow (defer; admin manual today)

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-3 | AWB generation on Paid → Processing | Must |
| FR-4 | AWB retry job | Must |
| FR-5 | ShipmentTrackingJob | Should |

---

## Story Summary

| Story ID | Title | Priority |
|----------|-------|----------|
| 001-awb-creation-on-paid | Auto-create AWB when order transitions to Paid | Must |
| 002-awb-retry-job | BackgroundService retries failed AWB creations | Must |
| 003-shipment-tracking-job | Background polling drives Shipped → Delivered | Should |

---

## Dependencies

### Depends On
- 001-sameday-api-client

### Depended By
- intent 020 (observability) — adds metrics on this job
