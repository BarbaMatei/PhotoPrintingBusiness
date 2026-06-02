---
id: 003-shipment-tracking-job
unit: 002-awb-and-tracking-jobs
intent: 015-sameday-shipping-integration
status: complete
priority: should
created: 2026-05-25T10:10:00.000Z
assigned_bolt: 037-awb-and-tracking-jobs
implemented: true
---

# Story: 003-shipment-tracking-job

## User Story

**As** the customer
**I want** my order to be marked Delivered automatically when the parcel arrives
**So that** I see accurate status without waiting for the admin to update it

## Acceptance Criteria

- [ ] `ShipmentTrackingJob : BackgroundService` runs every 15 min (configurable).
- [ ] Selects `Orders` where `Status == Shipped AND (LastTrackingSyncAt IS NULL OR LastTrackingSyncAt < now - 15min) AND ShippedAt > now - 30d`.
- [ ] For each, calls `SamedayClient.GetTrackingAsync(order.AwbNumber)`; on Sameday status `delivered`, transitions `Order.Status → Delivered`, sets `DeliveredAt`, fires existing `IOrderEmailService.FireOrderDeliveredEmail`.
- [ ] Transition is idempotent — re-running tick after a transition is a no-op.
- [ ] After 30 days from `ShippedAt`, polling stops; order remains `Shipped` for manual admin closure (Warning log emitted once).

## Technical Notes

- Tracking response varies; map only what we need: `status`, `deliveredAt` (when present), `events[]`.
- Persist `LastTrackingSyncAt` on every tick whether or not status changed.
- Polly `RateLimit(5 req/s)` protects the API.

## Dependencies

### Requires
- 001-awb-creation-on-paid, 002-awb-retry-job

### Enables
- Future automatic refund-on-lost-shipment (out of scope)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Sameday transitions back from `delivered` to `returned` after our transition | Out of scope — admin handles via existing return flow |
| Two API replicas | Both ticks safe; transition guarded by status check on update |

## Out of Scope

- Real-time webhooks (Sameday push) — defer; polling is sufficient for current volume.
