---
unit: 003-handler-pattern
intent: 027-architectural-layering
phase: inception
status: draft
unit_type: backend
default_bolt_type: simple-construction-bolt
created: 2026-06-05T09:30:00Z
updated: 2026-06-05T09:30:00Z
---

# Unit Brief: Handler-per-Use-Case

## Purpose

Give multi-step use cases a discoverable home with a 30-LOC `ICommandHandler`/`IEventDispatcher` pattern (no MediatR). Migrate the four target use cases, folding in the OrderPaidEventDispatcher (P11) as the canonical first handler.

## Scope

### In Scope
- Handler/dispatcher abstractions; CreateOrderHandler, OrderPaidEventDispatcher, RetryInvoiceUploadHandler, PromoteOrderPhotosHandler.

### Out of Scope
- Converting CRUD endpoints to handlers (bar: 3+ concerns or 50+ LOC).
- MediatR.

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-7 (P25) | Handler-per-use-case (no MediatR) | Should |
| FR-8 (P11, folded) | OrderPaidEventDispatcher | Should |

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|--------|-------------|------------|
| ICommandHandler<TCommand,TResult> | Use-case interface | HandleAsync |
| IEventDispatcher<TEvent> | Fan-out interface | DispatchAsync |
| OrderPaidEvent | Domain event | order context |

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| CreateOrderHandler | Extract 145-LOC CreateFromCartAsync | CheckoutCommand | OrderCreationResult |
| OrderPaidEventDispatcher | Dedupe webhook post-Paid fan-out | OrderPaidEvent | side effects in ADR-020 order |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 4 |
| Must Have | 0 |
| Should Have | 4 |
| Could Have | 0 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-command-handler-abstractions | Handler/dispatcher abstractions | Should | Planned |
| 002-create-order-handler | CreateOrderHandler | Should | Planned |
| 003-order-paid-event-dispatcher | OrderPaidEventDispatcher (P11) | Should | Planned |
| 004-retry-and-promote-handlers | Retry-invoice + promote-photos handlers | Should | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 001-layering-foundation | Handlers land in Application/<Feature>/Handlers |
| 002-conventions-and-policy | Handlers reference Abstractions/ interfaces |

### Depended By
| Unit | Reason |
|------|--------|
| None | (intent 029 P14 scopes to residuals, coordinating with this) |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| None | — | — |

---

## Technical Context

### Suggested Technology
Hand-rolled `ICommandHandler`/`IEventDispatcher` in `Application/Shared/Abstractions/`.

---

## Constraints

- Order of side effects in OrderPaidEventDispatcher is load-bearing (invoice INSERT before SignalR — ADR-020).
- Highest-traffic write path — full payment/webhook integration suite must pass.

---

## Success Criteria

### Functional
- [ ] Four use cases become handlers with their own tests; service methods delegate one-liners.
- [ ] Both webhook paths call the dispatcher; ordering asserted by test.

### Non-Functional
- [ ] No behaviour change.

### Quality
- [ ] `OrderServiceTests.cs` shrinks proportionally; CI green.

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 061-handler-pattern | simple | 001–004 | Abstractions + 4 handlers |

---

## Notes

P25 folds P11. Coordinates with intent 029 P14 (which scopes to OrderPhotoQueryService + cleanup to avoid double-extraction).
