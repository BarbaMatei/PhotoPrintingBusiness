---
id: 002-thin-webhooks-and-order-photo-query
unit: 002-service-decomposition
intent: 029-decomposition-and-hardening
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 064-service-decomposition
implemented: false
---

# Story: 002-thin-webhooks-and-order-photo-query

## User Story

**As a** developer
**I want** `GetOrderPhotosAsync` moved out of OrderService and the webhook controller left thin
**So that** presign logic lives in the right class and webhooks stop orchestrating data access

## Acceptance Criteria

- [ ] **Given** `OrderService.GetOrderPhotosAsync` (pure presign, wrong class), **When** moved to `OrderPhotoQueryService`, **Then** `IOrderService` delegates a one-liner
- [ ] **Given** `WebhooksController`, **When** thinned, **Then** it contains no direct `_db.SaveChangesAsync` / `_db.Entry(...).LoadAsync()` — it routes to handlers/dispatcher (intent 027 P25/P11)
- [ ] **Given** the change, **When** the payment/webhook integration suite runs, **Then** it passes (no behaviour change)

## Technical Notes

- Residual scope: intent 027 P25/P11 already extract `CreateFromCartAsync` and the post-Paid fan-out — do not re-extract.

## Dependencies

### Requires
- 027/003 handlers (CreateOrderHandler + OrderPaidEventDispatcher)

### Enables
- None

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Presign for 30 photos | Same behaviour; can adopt Task.WhenAll separately (review §Scalability #2) |

## Out of Scope

- AuthService split (previous story); the presign N+1 perf fix (separate).
