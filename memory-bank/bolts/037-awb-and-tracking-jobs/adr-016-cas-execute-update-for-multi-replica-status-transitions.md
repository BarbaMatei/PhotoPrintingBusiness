---
bolt: 037-awb-and-tracking-jobs
created: 2026-06-02T17:15:00Z
status: accepted
---

# ADR-016: Compare-and-Swap via `ExecuteUpdateAsync` for Multi-Replica-Safe `Order.Status` Transitions

## Context

`ShipmentTrackingJob` runs on a `PeriodicTimer` and, when it
observes that Sameday reports a parcel as `delivered`, transitions
the corresponding `Order` from `Status = Shipped` to
`Status = Delivered`. Two replicas of the API host running the same
job can race against each other for the same order:

- Replica A: SELECT, sees `Status = Shipped`, calls Sameday, gets
  `Delivered`, prepares to UPDATE.
- Replica B: SELECT, sees `Status = Shipped`, calls Sameday, gets
  `Delivered`, prepares to UPDATE.

If both UPDATEs fire naively, the order gets transitioned twice,
`DeliveredAt` gets set twice (to slightly different timestamps),
and `IOrderEmailService.FireOrderDeliveredEmailAsync` enqueues the
delivery email twice — the customer sees a duplicate "your order
has arrived" email.

Worse, between SELECT and UPDATE, an admin can manually transition
the order to `Cancelled` (a legitimate end-state). A read-then-write
pattern silently overwrites the admin's transition.

We had to decide what general pattern the job uses to safely
transition `Order.Status` while other writers may concurrently be
moving the same row.

## Decision

**`ShipmentTrackingJob` (and, by precedent, any future background
worker that needs to transition `Order.Status`) uses EF 8's
`ExecuteUpdateAsync` with a `WHERE` clause that pins the source
state. This is a database-native compare-and-swap (CAS): the
UPDATE only fires if the row is still in the expected source state
at the moment of execution; the affected-row count is the success
signal.**

The canonical shape:

```csharp
var affected = await db.Orders
    .Where(o => o.Id == orderId && o.Status == OrderStatus.Shipped)
    .ExecuteUpdateAsync(setters => setters
        .SetProperty(o => o.Status,             OrderStatus.Delivered)
        .SetProperty(o => o.DeliveredAt,        observedAt)
        .SetProperty(o => o.LastTrackingSyncAt, observedAt)
        .SetProperty(o => o.UpdatedAt,          clock.GetUtcNow()),
        ct);

if (affected == 0)
{
    logger.LogInformation(
        "sameday.tracking.race-lost order_id={Id} — status already advanced", orderId);
    return; // someone else (admin / other replica) already moved it
}
```

Restated as invariants the code must hold:

- The `WHERE` clause MUST include `Status == <expected source>`
  for any UPDATE that mutates `Status` and any side-effect
  columns whose write semantics are bound to that transition.
- The caller MUST treat `affected == 0` as a legitimate, expected
  outcome (not an error), logged at Info level.
- After a `affected == 0` result, the caller MUST NOT proceed with
  side effects that would have been bound to the transition (no
  email enqueue, no event log).
- The pattern is **not** a substitute for application-level
  re-checks where the *value* matters (see ADR-015 for the
  `AwbNumber is null` re-check on AWB creation). CAS guards the
  *transition*; re-checks guard the *value semantics*.

## Rationale

CAS-on-status is the cheapest correct primitive available with our
current stack (EF 8 + Npgsql) for the multi-replica case:

1. **Single round-trip.** `ExecuteUpdateAsync` emits one `UPDATE
   … WHERE …` statement. No SELECT-then-UPDATE race window.
2. **No entity tracking.** The change tracker is not involved;
   there is no `Detached` / `Modified` lifecycle to reason about.
3. **No new columns or indexes.** We don't need a `RowVersion` or
   `ConcurrencyToken` column; the source-state itself IS the
   token.
4. **Composes with existing indexes.** The `ix_orders_status_created_at`
   index already covers the `Status = …` predicate.
5. **Generalises beyond bolt 037.** Any background worker that
   transitions an `Order.Status` (or any other status-like
   column on any aggregate) can adopt the same pattern. Intent 020
   (observability) and any future admin-side state-machine
   automation are natural reuses.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|---|---|---|---|
| **EF entity-tracking with `RowVersion` concurrency token** | EF's first-class concurrency story. | Requires a new column on `Orders` + migration. Throws `DbUpdateConcurrencyException` rather than returning a row count — control flow is exception-driven, which is the wrong shape for a "race-lost is expected" code path. Substantially more code to handle the exception cleanly. | Wrong primitive: `DbUpdateConcurrencyException` is meant for "user-facing edit conflict" not "two background workers raced for the same transition." |
| **Postgres advisory locks (`pg_advisory_xact_lock(order_id)`)** | Pessimistic — no race window at all. | Postgres-specific (breaks PostgreSQL). Requires manual lock-release reasoning. Sits at the connection layer, awkward to compose with EF. | Stack-coupled and heavier than necessary. |
| **`SELECT … FOR UPDATE SKIP LOCKED` in a transaction** | Database-native. Skip-locked is well-suited to job queues. | Postgres-specific (same as advisory locks). Forces a transaction scope around the job's logical operation — including the Sameday HTTP call inside the transaction is bad practice. | Wraps too much under the lock; the Sameday call must NOT hold a row lock. |
| **Read-then-update with no guard** (today's naive pattern) | Trivial code. | Allows the two failure modes described in Context (double-transition + admin-write overwrite). | Incorrect under multi-replica. |
| **Pre-emptive Redis lock** | Eliminates the race entirely. | Requires Redis (deferred to bolt 046). | Premature. CAS is correct without new infrastructure. |

## Consequences

### Positive

- **Multi-replica correctness without new infrastructure.** Bolt
  037 ships without Redis, without a new column, without a new
  index. Same family as ADR-015 (vendor-side idempotency + app-side
  re-check).
- **Code reads as intent.** `ExecuteUpdateAsync(... Where Status == Shipped)`
  reads exactly as "transition this order from Shipped to
  Delivered." Future maintainers don't have to reverse-engineer
  the concurrency story.
- **Trivially testable.** A unit test can pre-set
  `Status = Cancelled` on a row, fire the transition, and assert
  `affected == 0` plus "no email enqueued."
- **Pattern extends.** Future workers (intent 020 observability;
  intent 022's hypothetical admin-bulk-transition tooling) can
  adopt the same CAS shape without re-litigating the design.

### Negative

- **Race-lost logs are normal.** Future on-call needs to know that
  `sameday.tracking.race-lost` at Info level is healthy, not a
  warning. Mitigation: the message is explicit ("status already
  advanced") and the log level is Info, not Warning.
- **No "what did the other writer set?" visibility.** When
  `affected == 0`, we don't know whether the row is now `Delivered`
  (other replica won), `Cancelled` (admin won), or something else.
  We just know we lost. Mitigation: if future code needs to react
  differently based on the post-race state, it can do a follow-up
  read; today's code paths don't need to.
- **Requires EF 8+ (`ExecuteUpdateAsync`).** The project is on EF
  Core 8.0.11 already, so this is a non-cost today. If a future
  rollback to EF 6 were ever entertained, this pattern would need
  re-implementation via raw SQL.

### Risks

- **Risk: someone uses `ExecuteUpdateAsync` to write `Status`
  without the source-state `Where` clause.** Highest-likelihood
  silent regression. Mitigation: a unit test verifying the
  CAS-lost path is part of `ShipmentTrackingJobTests`; PR review
  is expected to flag any new `ExecuteUpdateAsync` on `Orders`
  that omits the `Status == …` predicate.
- **Risk: someone wraps `ExecuteUpdateAsync` in a transaction that
  also includes the Sameday HTTP call.** Defeats the no-transaction
  rationale (a held row lock during a 1–3 s outbound HTTP call is
  a serious operational hazard). Mitigation: the
  `ShipmentTrackingJob` design isolates the HTTP call OUTSIDE any
  transaction and the only DB interaction inside the
  `SemaphoreSlim`-bounded section is the single CAS UPDATE.
- **Risk: the pattern is misapplied to value-write semantics.**
  CAS does NOT guard "if `AwbNumber` is still null, persist this
  one." That's an AWB-creation concern, not a status-transition
  concern; ADR-015 covers it via application-side re-check. This
  ADR explicitly notes the distinction so a future reader doesn't
  conflate the two.

## Related

- **Stories**: 003-shipment-tracking-job (the immediate consumer);
  intent 020 / intent 021 future bolts that may adopt the same
  pattern.
- **Previous ADRs**: ADR-015 (companion — duplicate-create acceptance);
  ADR-010 (same in-process-now stance for the promotion queue).
- **Future ADRs**: bolt 046's Redis introduction may *augment*
  (not supersede) this; CAS-on-status remains a valid primitive
  even with distributed locks available. The decision-tree
  becomes "use CAS when the source-state predicate is
  sufficient; reach for a distributed lock when ordering /
  fairness / fairness-with-throttling matters."
- **Read when**: writing any `BackgroundService` that mutates
  `Order.Status`; reviewing PRs that add `ExecuteUpdateAsync`
  calls on `Orders`; designing concurrency for an aggregate with
  an enum-style status column; debugging "did the wrong replica
  win this race?"; reasoning about whether to introduce a
  `RowVersion` column (don't, unless the value semantics — not
  just the transition — need protecting).
