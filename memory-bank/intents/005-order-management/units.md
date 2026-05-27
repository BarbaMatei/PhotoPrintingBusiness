---
intent: 005-order-management
created: 2026-05-22T07:10:00Z
---

# Units: Order Management

## Decomposition Strategy

Phase 4 has two clearly separated layers — backend API and Angular UI — which map directly to two units.

## Units

| # | Unit | Type | Bolt Type | FR Coverage |
|---|------|------|-----------|-------------|
| 001 | `001-orders-api` | backend | ddd-construction-bolt | FR-1, FR-2, FR-5 (shared DTO) |
| 002 | `002-orders-ui` | frontend | simple-construction-bolt | FR-3, FR-4, FR-5 (shared pipe) |

## Dependency

```
[001-orders-api] ──► [002-orders-ui]
```

`002-orders-ui` requires `001-orders-api` to be complete before FE work can reference real endpoints.

## Unit Summaries

### 001-orders-api
ASP.NET Core controller + service that exposes:
- `GET /api/orders` — paginated list (newest first)
- `GET /api/orders/{id}` — ownership-checked detail

Uses existing `Order`/`OrderItem` EF Core entities. No new migrations needed.

### 002-orders-ui
Angular standalone components + pages:
- `/comenzi` — order history list with status badges + pagination
- `/comenzi/:id` — order detail with line items, delivery summary, status stepper
- Shared `OrderStatusPipe` for Romanian labels (reused from confirmation page concept)
