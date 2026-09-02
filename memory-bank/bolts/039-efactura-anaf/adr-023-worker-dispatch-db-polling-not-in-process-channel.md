---
bolt: 039-efactura-anaf
created: 2026-06-03T12:30:00Z
status: accepted
---

# ADR-023: `InvoiceUploadJob` Uses DB Polling, Not In-Process `Channel<T>`

## Context

Bolt 039 introduces `InvoiceUploadJob : BackgroundService`, the
worker that drives an `Invoice` row through its ANAF lifecycle
(`Pending → Submitted → Accepted | Rejected → Failed`). The worker
needs a *dispatch model*: how does it discover work, react to new
work, and survive restarts.

There is a directly comparable prior decision in this codebase.
**ADR-010** (in-process `Channel<T>` + startup recovery scan instead
of a durable work-queue table) settled the dispatch model for the
**photo promotion worker** in intent 024. That worker reacts to
"customer just paid → promote their uploads to cloud storage" with
sub-second latency expectations: a customer should see their order
confirmation immediately after payment, and the photos must already
be in the cloud by the time the next page loads. ADR-010 chose a
`Channel<PromotionJob>` consumed by a single `BackgroundService`,
with crash-safety via a startup `PromotionRecoveryScanner` that
re-derives pending work from `Upload.StorageLocation`.

The `InvoiceUploadJob` faces a superficially similar problem
(react to "customer just paid" → submit invoice to ANAF), but the
SLA is fundamentally different:

- **Photo promotion**: sub-second user-facing latency required.
- **ANAF submission**: legal requirement to submit within **5
  business days** of issue. The customer never sees this happen.

If we naively re-applied ADR-010, we'd build the same
`Channel<InvoiceJob>` + recovery scanner machinery for the ANAF
worker. The question is whether that machinery's benefits transfer
when the SLA shifts from sub-second to days.

## Decision

**`InvoiceUploadJob` uses a `PeriodicTimer`-driven DB poll every
`Anaf:PollIntervalMinutes` (default 30 minutes), not an in-process
`Channel<T>`. ADR-010's reasoning does NOT transfer to this worker;
the SLA shift justifies a different dispatch model.**

Concretely:

- The worker is a single `BackgroundService` registered conditionally
  on `Anaf:Enabled == true`.
- Each tick:
  1. Query: `SELECT ... FROM Invoices WHERE AnafStatus IN ('Pending', 'Submitted') ORDER BY CreatedAt LIMIT @MaxBatchSize`.
  2. For each row, call the dispatch handler (`UploadPending` or
     `PollSubmitted`).
- No in-memory queue, no producer-side enqueue calls, no recovery
  scanner. The DB query at each tick IS the recovery: if the worker
  crashed mid-tick, the next tick re-fetches and re-runs the
  affected rows. The CAS predicate on every status mutation
  (ADR-016) ensures duplicate processing is safe.

Restated as invariants:

- **The DB is the source of truth for pending work.** No "in-flight"
  state lives only in worker memory.
- **No producer-side coupling.** Other code paths (Stripe webhook,
  admin retry endpoint) write to `Invoices` and don't notify the
  worker — the worker discovers the work on its next tick. This
  decouples the worker from every place that creates or retries
  invoices.
- **Polling cadence is configurable, but the floor is 1 minute.**
  Sub-minute polling would be a smell — at that point, in-process
  signalling becomes worth the complexity. The `AnafSettings`
  validator enforces `PollIntervalMinutes ∈ [1, 1440]`.

## Rationale

ADR-010's three load-bearing reasons for `Channel<T>` over polling:

1. **Sub-second reaction latency** (customer-visible)
2. **No DB load from a polling table**
3. **Simpler code** (no scheduler / cron interleaving)

Examining each against the ANAF worker's needs:

1. **Sub-second latency** is irrelevant for ANAF. The 5-business-day
   SLA gives a single 30-minute poll cycle ~240× headroom. Even a
   1-hour cadence would meet the SLA with 120× headroom. The
   customer never sees this latency. Reason 1 doesn't transfer.
2. **DB load**. The promotion worker would have polled `Uploads`
   (a hot table written by every upload). The ANAF worker polls
   `Invoices` (one write per paid order, ~tens to low-hundreds per
   day at projected scale). The polling query has an index on
   `AnafStatus` (shipped by bolt 038) and a `LIMIT 50`. Cost is
   trivial. Reason 2 doesn't transfer.
3. **Code simplicity**. Inverted here: a polling worker is the
   *simpler* shape. `Channel<T>` requires a producer-side enqueue
   from every code path that creates an invoice (Stripe webhook,
   admin retry, future replay tools, future bulk-import). Polling
   removes that coupling entirely — the worker's contract is "look
   at the DB on a timer; act on what you find." Reason 3 inverts.

There's a second class of benefits specific to polling:

- **Admin retry, multi-replica safety, and future replay tools
  ALL benefit from the same dispatch model.** With a `Channel<T>`,
  admin retry would have to enqueue (or signal). With polling,
  admin retry just flips `AnafStatus = Pending` and waits for the
  next tick. One write, no orchestration.
- **Resilience to mass scenarios.** If ANAF goes down for an hour,
  with `Channel<T>` we'd accumulate jobs in memory (or drop them).
  With polling, the DB holds the backlog naturally; when ANAF
  recovers, the next tick picks up everything from where it left
  off. No bookkeeping.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|---|---|---|---|
| **DB polling (chosen)** | Decoupled from producers; survives crashes naturally; trivial DB cost at scale; simple admin-retry semantics. | Up-to-30-minute discovery latency for new invoices (irrelevant given SLA); requires a tick-based timer. | — |
| **In-process `Channel<T>` + recovery scan (ADR-010 shape)** | Sub-second reaction latency; no periodic timer code. | Requires producer-side enqueue from every code path that creates an invoice; coupling grows with each producer; multi-replica race conditions need explicit handling; admin retry needs explicit signal. | Latency benefit doesn't matter; coupling cost is real. |
| **PostgreSQL `LISTEN`/`NOTIFY`** | Sub-second reaction without producer coupling (DB triggers fire on INSERT); decoupled like polling. | Requires Postgres-specific code (no PostgreSQL-path equivalent); reconnect logic; not idiomatic in EF Core. | Provider lock-in; sub-second latency is unneeded. |
| **External queue (Redis / RabbitMQ / SQS)** | Persistent queue with durability + at-least-once semantics. | New infrastructure dependency; bolt 046 (Redis backplane) is explicitly deprioritized. | Infrastructure cost wildly disproportionate to a 30-min-cadence task. |
| **Hangfire / Quartz / external scheduler** | Battle-tested job scheduling; dashboard for free. | Adds a third-party dependency; Hangfire's own job table is essentially the polling pattern in disguise (we'd be paying for features we don't use). | Over-engineered for one BackgroundService. |
| **Cron + standalone CLI** | Truly stateless worker; easy to reason about. | Loses the master `Anaf:Enabled` flag pattern; per-host cron setup adds ops surface. | Doesn't compose with the existing in-process master-flag pattern (bolts 036/045). |

## Consequences

### Positive

- **One worker, zero producer coupling.** Stripe webhook,
  admin retry endpoint, and any future invoice-creation path don't
  need to know about the worker. They write to `Invoices`; the
  worker picks up.
- **Crash safety is free.** No recovery scanner, no
  "in-flight-but-not-persisted" state. If the process restarts
  mid-tick, the next tick re-fetches the affected rows.
- **Multi-replica safety is free.** Per ADR-015 / ADR-016, the
  CAS predicate on every status mutation ensures duplicate
  processing is safe. Two replicas polling the same row land in
  the same dispatch handler; only one CAS succeeds; the other's
  no-op logs at Info.
- **Admin retry is one line.** `UPDATE Invoices SET AnafStatus =
  'Pending' WHERE Id = @id AND AnafStatus IN ('Rejected', 'Failed')`.
  The next tick processes the row. No signal, no enqueue.
- **ANAF outage absorbs naturally.** Backlog accumulates in the
  DB; when ANAF recovers, the worker drains. No queue-overflow
  story to manage.

### Negative

- **Up-to-30-min discovery latency for new invoices.** Acceptable
  given the 5-business-day SLA, but worth being explicit about:
  an invoice created at 10:00 may not be uploaded until 10:29. The
  customer's order-confirmation email goes out immediately
  (existing behaviour); the ANAF round-trip is invisible to them.
- **Pattern divergence from the promotion worker.** Future
  contributors may ask "why does the promotion worker use
  `Channel<T>` but the invoice worker polls?" This ADR is the
  answer; link from `InvoiceUploadJob`'s doc comment.
- **Cadence is configurable but not adaptive.** The worker polls
  every 30 minutes regardless of backlog size. If there are 5000
  invoices in `Pending` (after a long outage), the worker
  processes 50 per tick → 50 ticks → 25 hours to drain. Mitigation:
  the operator can temporarily increase `MaxBatchSize` or lower
  `PollIntervalMinutes` to drain faster. The default is right-sized
  for steady state.

### Risks

- **Risk: a future contributor adds a `Channel<T>` "for low-latency
  admin retry feedback" without realising the existing model
  already handles it.** Mitigation: this ADR; admin retry endpoint
  documentation states "Retry takes effect on the next worker
  tick (max 30 minutes)" so the expectation is set.
- **Risk: polling cadence reduced to ~seconds, defeating the
  purpose of the pattern.** Mitigation: validator floor of 1 minute;
  this ADR documents that sub-minute polling is a smell.
- **Risk: `MaxBatchSize` accidentally set to 0 by misconfiguration.**
  The worker would silently process zero rows per tick. Mitigation:
  validator range `[1, 500]`; metrics (bolt 044) include batch-size
  histograms to surface anomalies.
- **Risk: a long backlog after an ANAF outage exceeds the 5-business-day
  SLA.** Mitigation: alerting on `invoice_anaf_status_total{status="failed"}`
  (bolt 044 + bolt 045 Sentry hook); operator drains by increasing
  cadence temporarily. This is the same recovery shape as any other
  outage.

## Related

- **Stories**: 002-anaf-spv-client (the immediate consumer).
- **Previous ADRs**: ADR-010 (in-process `Channel<T>` for promotion
  worker) — this ADR explicitly diverges; ADR-015 (accept duplicate
  vendor calls + DB re-check) and ADR-016 (CAS via
  `ExecuteUpdateAsync`) — make the polling-without-coordination
  safe.
- **Future ADRs**: if bolt 046 (Redis backplane, currently
  deprioritized) lands and we move to a multi-replica scaling
  posture with leader election, this ADR may be revisited. The
  current polling model survives multi-replica via ADR-015/ADR-016
  acceptance; with leader election, a `Channel<T>` becomes feasible
  again.
- **Read when**: working on `InvoiceUploadJob`; reviewing PRs that
  add invoice-creation paths (need to remember: just write to DB,
  no notification needed); reviewing PRs that touch dispatch
  cadence; debugging "why didn't my admin retry kick off
  immediately"; planning multi-replica scaling (bolt 046);
  designing the next BackgroundService and choosing between
  polling and `Channel<T>` (the SLA distinction is the rule).
