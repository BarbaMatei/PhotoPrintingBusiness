---
id: 001-awb-creation-on-paid
unit: 002-awb-and-tracking-jobs
intent: 015-sameday-shipping-integration
status: complete
priority: must
created: 2026-05-25T10:10:00.000Z
assigned_bolt: 037-awb-and-tracking-jobs
implemented: true
---

# Story: 001-awb-creation-on-paid

## User Story

**As** a customer who just paid
**I want** the shipping AWB created automatically
**So that** my parcel ships without operator intervention

## Acceptance Criteria

- [ ] **Given** an order transitions to `Paid`, **When** the post-transition hook fires, **Then** the system asynchronously calls `SamedayClient.CreateAwbAsync(order)` and persists `Order.AwbNumber` + `Order.AwbLabelUrl` on success.
- [ ] Parcel weight is `OrderItems.Sum(qty) * 50 + 50` (grams).
- [ ] Recipient defaults: from `Order.EasyboxLocker` when delivery is Easybox, from `Order.ShippingAddress` when Courier.
- [ ] On any Sameday failure (network, 4xx, 5xx), the order remains in `Paid` and an entry is queued for the retry job (see story 002). No customer-facing failure occurs.
- [ ] Order confirmation email already sent earlier in the flow — this story does not retry or duplicate it.

## Technical Notes

- Hook lives in `OrderStatusMachine.AfterTransitionAsync` (extend existing).
- Use `Channel<Guid>` (in-process) or simply `Task.Run(...)` queued via a hosted service so the controller responds immediately. Recommend the hosted-service channel pattern, similar to existing `EmailRetryJob`.
- Sameday request shape (REST, JSON):
  - `awbPayment: 1` (paid)
  - `parcels: [{ weight, length=20, width=15, height=2 }]` (heuristic)
  - `pickupPoint: settings.PickupPointId`
- On success, log `Information sameday.awb.created order_id={id} awb={awb}`.

## Dependencies

### Requires
- 001-sameday-api-client (entire unit)

### Enables
- 002-awb-retry-job, 003-shipment-tracking-job

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Order with 0 items (impossible today) | Skip AWB; log Warning |
| Customer cancels before AWB created | Order moves to `Cancelled`; pending AWB request becomes a no-op |
| Sameday returns AWB but without label URL | Persist AWB number, schedule label fetch retry |

## Out of Scope

- AWB cancellation on refund (deferred).
