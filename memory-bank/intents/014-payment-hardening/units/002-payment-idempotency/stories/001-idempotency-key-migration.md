---
id: 001-idempotency-key-migration
unit: 002-payment-idempotency
intent: 014-payment-hardening
status: complete
priority: must
created: 2026-05-25T10:05:00Z
assigned_bolt: 035-payment-idempotency
implemented: true
implemented_at: 2026-05-25T14:15:00Z
---

# Story: 001-idempotency-key-migration

## User Story

**As** the backend
**I want** an indexed slot on `Orders` for the request idempotency key
**So that** a repeat payment-intent call finds the prior order in O(1)

## Acceptance Criteria

- [ ] EF Core migration adds `IdempotencyKey` nullable `varchar(80)` to `Orders`.
- [ ] Filtered unique index `ix_orders_idempotency_key` enforces uniqueness only when the column is not null.
- [ ] Migration applies cleanly to both Postgres and the SQLite dev fallback (skip index syntax differences via `migrationBuilder.HasFilter(...)` Postgres-only path; SQLite emits a plain unique index — acceptable).
- [ ] Down-migration drops the index and column.

## Technical Notes

```csharp
// Migrations/20260526_AddOrderIdempotencyKey.cs
migrationBuilder.AddColumn<string>(
    name: "IdempotencyKey",
    table: "Orders",
    type: "varchar(80)",
    maxLength: 80,
    nullable: true);

migrationBuilder.CreateIndex(
    name: "ix_orders_idempotency_key",
    table: "Orders",
    column: "IdempotencyKey",
    unique: true,
    filter: "\"IdempotencyKey\" IS NOT NULL");
```

- `Order` entity: add `public string? IdempotencyKey { get; set; }`.
- Model builder: configure max length + the filtered unique index.

## Dependencies

### Requires
- bolt 015-shipping-and-order-core (Orders table)

### Enables
- 002-stripe-intent-idempotency, 003-euplatesc-initiate-idempotency

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Existing rows on prod | Backfill not required — `NULL` is valid |
| Concurrent migrations | Standard EF migration locks; no manual coordination |

## Out of Scope

- Idempotency for refunds (admin path, lower volume).
