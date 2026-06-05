---
id: 002-create-order-handler
unit: 003-handler-pattern
intent: 027-architectural-layering
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 061-handler-pattern
implemented: false
---

# Story: 002-create-order-handler

## User Story

**As a** developer
**I want** the 145-LOC `CreateFromCartAsync` extracted into a `CreateOrderHandler`
**So that** the checkout use case is legible, testable, and not a god-method

## Acceptance Criteria

- [ ] **Given** `CreateOrderCommand` + `CreateOrderHandler`, **When** implemented, **Then** they own cart-load, idempotency, order-number, VAT calc, persist, and metrics; the result type is the existing `OrderCreationResult`
- [ ] **Given** `IOrderService.CreateFromCartAsync`, **When** refactored, **Then** it delegates a one-liner to the handler
- [ ] **Given** the handler, **When** tested, **Then** it has its own test file and `OrderServiceTests.cs` shrinks proportionally
- [ ] **Given** the change, **When** the payment/webhook suite runs, **Then** it passes (no behaviour change)

## Technical Notes

- Coordinates with intent 029 P14 — that proposal scopes to residuals (OrderPhotoQueryService + cleanup) to avoid re-extracting this.

## Dependencies

### Requires
- 001-command-handler-abstractions

### Enables
- None

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Idempotent re-submit | Same behaviour as before extraction |

## Out of Scope

- The post-Paid fan-out (next story).
