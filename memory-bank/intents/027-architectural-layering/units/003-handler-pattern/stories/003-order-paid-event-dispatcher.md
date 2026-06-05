---
id: 003-order-paid-event-dispatcher
unit: 003-handler-pattern
intent: 027-architectural-layering
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 061-handler-pattern
implemented: false
---

# Story: 003-order-paid-event-dispatcher

## User Story

**As a** developer
**I want** the duplicated Stripe/EuPlatesc post-Paid fan-out extracted into one dispatcher
**So that** the side-effect sequence lives in exactly one place (folds first-pass P11)

## Acceptance Criteria

- [ ] **Given** the duplicated 5-step block in `WebhooksController` (Stripe + EuPlatesc), **When** extracted, **Then** a single `OrderPaidEventDispatcher.DispatchAsync(OrderPaidEvent, ct)` owns it
- [ ] **Given** both webhook handlers, **When** refactored, **Then** each becomes verify-signature → transition order → dispatch
- [ ] **Given** the side-effect ordering (invoice INSERT before SignalR broadcast — ADR-020), **When** documented in XML, **Then** a unit test asserts the order
- [ ] **Given** the change, **When** the webhook suite runs, **Then** it passes

## Technical Notes

- This is first-pass P11, folded into P25 as the canonical first handler/dispatcher.

## Dependencies

### Requires
- 001-command-handler-abstractions

### Enables
- None

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Duplicate webhook delivery | Idempotent dispatch (existing behaviour preserved) |

## Out of Scope

- Thinning the rest of WebhooksController (intent 029 P14).
