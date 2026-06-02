---
id: 004-order-status-machine
unit: 003-shipping-and-order-core
intent: 004-checkout-payment
status: complete
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 015-shipping-and-order-core
implemented: true
---

# Story: 004-order-status-machine

## User Story

**As a** developer
**I want** an `OrderStatusMachine` that enforces valid status transitions
**So that** no code path can put an order into an invalid state (e.g., `Shipped → AwaitingPayment`)

## Acceptance Criteria

- [ ] **Given** a valid transition (e.g., `AwaitingPayment → Paid`), **When** `OrderStatusMachine.Transition(from, to)` is called, **Then** the transition succeeds and returns the new status
- [ ] **Given** an invalid transition (e.g., `Delivered → Printing`), **When** `OrderStatusMachine.Transition(from, to)` is called, **Then** an `InvalidOrderTransitionException` is thrown
- [ ] **Given** the controller receives an invalid status update request, **When** `InvalidOrderTransitionException` is caught by `ExceptionHandlerMiddleware`, **Then** a 400 response is returned with a Romanian error message
- [ ] **Given** the full state graph, **When** all valid transitions are enumerated, **Then** they match exactly: `AwaitingPayment→Paid`, `AwaitingPayment→PaymentFailed`, `Paid→Printing`, `Printing→Shipped`, `Printing→Cancelled`, `Shipped→Delivered`
- [ ] **Given** `OrderStatusMachine` is used in a unit test, **When** all transitions are tested, **Then** every invalid pair throws and every valid pair succeeds

## Technical Notes

- Implement as a static class `OrderStatusMachine` with a `Dictionary<(OrderStatus from, OrderStatus to), bool>` allowed-transitions lookup
- `InvalidOrderTransitionException` extends the project's base exception class (mapped to 400 by ExceptionHandlerMiddleware)
- State graph (valid transitions):
  - `AwaitingPayment → Paid`
  - `AwaitingPayment → PaymentFailed`
  - `Paid → Printing`
  - `Printing → Shipped`
  - `Printing → Cancelled`
  - `Shipped → Delivered`
- `PaymentFailed` and `Cancelled` and `Delivered` are terminal states — no transitions out
- Romanian error message: `"Tranziția de stare {from} → {to} nu este permisă"`

## Dependencies

### Requires
- Story 003-order-entity-schema (OrderStatus enum)
- Bolt 001 (ExceptionHandlerMiddleware — maps InvalidOrderTransitionException → 400)

### Enables
- Bolt 016 (payment-backends — transitions AwaitingPayment→Paid / PaymentFailed)
- Future intent 005-order-management (admin transitions Paid→Printing, Printing→Shipped, etc.)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Transition from `Delivered` to any state | Throws `InvalidOrderTransitionException` |
| Transition from `Cancelled` to any state | Throws `InvalidOrderTransitionException` |
| Same state transition (e.g., `Paid → Paid`) | Throws (not a valid transition) |

## Out of Scope

- Automated transition triggers (e.g., payment webhook triggers transition — that is in bolt 016)
- Transition history / audit log (future intent)
