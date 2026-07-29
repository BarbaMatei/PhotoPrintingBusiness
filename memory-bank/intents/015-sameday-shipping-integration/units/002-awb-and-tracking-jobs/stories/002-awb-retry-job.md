---
id: 002-awb-retry-job
unit: 002-awb-and-tracking-jobs
intent: 015-sameday-shipping-integration
status: complete
priority: must
created: 2026-05-25T10:10:00.000Z
assigned_bolt: 037-awb-and-tracking-jobs
implemented: true
---

# Story: 002-awb-retry-job

## User Story

**As** the platform
**I want** a background retry of AWB creation for orders that failed
**So that** transient Sameday outages don't leave orders stuck without an AWB

## Acceptance Criteria

- [ ] `AwbRetryJob : BackgroundService` runs every 1 hour (configurable, `Sameday:RetryIntervalMinutes`).
- [ ] Query selects `Orders` where `Status == Paid AND AwbNumber IS NULL AND PaidAt > now - 24h`.
- [ ] For each, calls `SamedayClient.CreateAwbAsync(...)` and persists results identical to story 001.
- [ ] After 24 h with no success, the order is **left as is** and an Error log emitted (`sameday.awb.give-up order_id=...`); a follow-up intent will wire admin notifications.
- [ ] Job is idempotent — concurrent ticks against the same order are safe (use `RowVersion` optimistic concurrency or skip-locked SELECT).

## Technical Notes

- Reuse the channel pattern from story 001 — failed channel items requeue with a delay; retry job is the safety net for crashes / restarts.
- Cap concurrent in-flight Sameday calls at 5 to stay under their rate limit.

## Dependencies

### Requires
- 001-awb-creation-on-paid

### Enables
- 003-shipment-tracking-job (cleaner state space — only orders with AWBs to track)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| API replicas both run the job | Either use a leader-election library (intent 021 candidate) OR accept duplicate retries — Sameday's idempotency via `awbPayment` external reference handles it |
| Order cancelled between tick and call | Skip (status check before HTTP call) |

## Out of Scope

- Per-order admin override / forced retry (admin UI work).
