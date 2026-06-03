---
bolt: 039-efactura-anaf
created: 2026-06-03T12:30:00Z
status: accepted
---

# ADR-024: Implicit Attempt Count from `(now - CreatedAt)`, No Persisted `RejectionCount` Column

## Context

`InvoiceUploadJob` enforces a bounded retry budget for ANAF rejections:
`1h, 4h, 16h, 64h` then `Failed` (per story 002 acceptance criteria).
At each `Submitted → Rejected` transition the worker has to answer:
"is this the *N*th rejection? If N=4, escalate to `Failed` instead of
back to `Pending` and let the worker retry on the next tick."

The naïve shape is a persisted counter:

```sql
ALTER TABLE Invoices ADD COLUMN RejectionCount int NOT NULL DEFAULT 0;
-- on every Submitted → Rejected transition:
UPDATE Invoices SET RejectionCount = RejectionCount + 1
                  , AnafStatus     = CASE WHEN RejectionCount + 1 >= 4 THEN 'Failed' ELSE 'Rejected' END
                  , ...
WHERE Id = @id AND AnafStatus = 'Submitted';
```

This is unambiguous, easy to query, and matches how most retry
schedulers shape this counter.

The alternative is **implicit**: don't persist a counter; compute
"is this the 4th rejection?" from the row's `CreatedAt` timestamp
against the cumulative backoff schedule:

```text
hoursSinceCreated = (now - invoice.CreatedAt).TotalHours
budgetExhausted   = hoursSinceCreated > sum(BackoffHours)   // 1 + 4 + 16 + 64 = 85h
target            = budgetExhausted ? Failed : Rejected
```

Both shapes work. The decision is which to ship in bolt 039.

## Decision

**Bolt 039 ships the implicit attempt count. No `Invoice.RejectionCount`
column is added. The worker computes "is this the 4th rejection?" by
comparing `(now - CreatedAt)` against the cumulative backoff schedule.**

Concretely:

- No DDL added in bolt 039 (consistent with "no migration" posture
  in the technical design — bolt 038 staged all the schema).
- `InvoiceUploadJob.PollSubmitted` computes the budget-exhausted
  flag inline:
  ```csharp
  var sumHours = anafSettings.BackoffHours.Sum();   // 85 for default config
  var elapsed  = DateTimeOffset.UtcNow - invoice.CreatedAt;
  var target   = elapsed.TotalHours > sumHours
               ? InvoiceAnafStatus.Failed
               : InvoiceAnafStatus.Rejected;
  ```
- Admin retry resets the budget by setting `UpdatedAt = NOW()`
  alongside the status flip — but the budget is anchored on
  `CreatedAt`, not `UpdatedAt`. **Admin retry does NOT extend the
  budget**; if the invoice has been `Rejected` for >85h when admin
  retries, the next worker tick will escalate to `Failed` again.
  This is the correct behaviour: admin retry means "try once more
  in case the world changed", not "reset the regulated clock."

If incidents show the implicit count is too lossy (e.g. clock skew,
or the ops team needs a quick "how many rejections did this
invoice have?" query), a follow-up bolt can add the column. **That
addition must engage with this ADR's trade-off**, not silently
"fix" what looks like missing state.

## Rationale

The decision is a deliberate application of YAGNI to a state-machine
column. The implicit count is correct for the happy retry path and
trades one small failure mode (clock skew) for a meaningful
simplicity gain.

### What the implicit count buys

- **No migration in bolt 039.** Schema stays exactly as bolt 038
  shipped it. Less change surface, fewer rollback concerns.
- **No CAS complexity.** A persisted counter would need to be
  incremented in the same `ExecuteUpdateAsync` call as the status
  flip (per ADR-016). Atomic increments in `ExecuteUpdateAsync`
  are doable but awkward (EF 8 supports `SetProperty(i =>
  i.RejectionCount, i => i.RejectionCount + 1)`); leaves more
  surface for off-by-one bugs.
- **The dataset never gets contradictory.** A persisted counter
  could drift from reality (worker crashes between status-write
  and counter-write — though the same tx fixes this; a manual
  ops `UPDATE` that touches one but not the other; etc.). A
  timestamp can't drift from itself.
- **Reduces the lifecycle's state surface.** The state machine
  defined in Stage 1 has 5 statuses. Adding a counter adds an
  implicit "× retry count" dimension. Keeping it implicit means
  the worker reads `(status, CreatedAt)` and that's the whole
  state.

### What the implicit count costs

- **Clock skew risk** (real but bounded). If the DB clock and the
  worker's host clock drift by 10 minutes, an invoice at the
  exact 85h mark may be classified one way on one replica and the
  other way on another. In practice: cloud DBs and worker hosts
  are NTP-synced to sub-second; the 85h boundary itself is fuzzy
  (the actual rejection might happen anywhere in the 64h-after-the-
  16h-attempt window); a few seconds of skew at the boundary is
  noise.
- **No "how many rejections did this row have?" query.** The
  audit answer to that question is "look at the log events for
  this `InvoiceId`" (the worker emits `InvoiceAnafStatusChanged`
  at every transition; bolt 044's metrics counter aggregates
  across rows). If a per-row history becomes a frequent operational
  need, that's the signal to add the column.
- **Manual ops complexity.** "I want to bump this one invoice
  back to its full budget" is harder. You'd have to update
  `CreatedAt` (which would be a Bad Idea — `CreatedAt` is the
  audit trail of when the row was created) or accept that admin
  retry doesn't extend the budget. The decision: admin retry
  doesn't extend the budget. If a regulator-driven need for
  budget-extension materialises, that's a strong signal for the
  column.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|---|---|---|---|
| **Implicit (chosen)** | No migration; no CAS complexity; cleaner state machine; correct for the happy retry path. | Clock-skew at boundary; no per-row history query; admin retry doesn't extend budget. | — |
| **Persisted `RejectionCount` column** | Unambiguous counter; admin can reset to extend budget; easy per-row history queries. | New column means new migration; CAS write must include the increment; one more piece of state to keep coherent. | Cost > value at current scale; can be added later without disturbing the implicit approach (the column would shadow the timestamp computation rather than replace it, in the worst case). |
| **Per-rejection event table** | Full audit; per-row history; queryable cleanly. | Significant schema addition; over-engineered for a 4-tier retry budget. | Disproportionate. |
| **Backoff schedule per row** (read from row, not config) | Operator can tune one invoice's retry policy. | Massive complexity for negligible benefit (operators don't tune individual invoices). | Out of scope. |
| **External job-tracking system (Hangfire job state)** | Built-in retry tracking; dashboard. | Pulls in Hangfire dependency rejected by ADR-023. | Out of scope. |

## Consequences

### Positive

- **No migration in bolt 039.** The technical design's "no DDL"
  posture is preserved.
- **State machine stays clean.** Five statuses; no per-status
  counter shadow.
- **Admin retry has clear semantics: "try once more, don't reset
  the regulated clock."** Maps cleanly to the legal posture —
  the budget is about how long we keep trying before declaring
  defeat, not about how many manual attempts the operator made.
- **Behaviour is fully derivable from the persisted row.** Any
  reader (logs, dashboard, audit) can answer "is this row past
  budget?" from `(CreatedAt, AnafStatus, NOW())` without consulting
  a counter that might be wrong.

### Negative

- **The 85h boundary is fuzzy.** Two replicas processing the
  same row near the 85h mark might disagree by seconds. The CAS
  (ADR-016) resolves this — only one transition succeeds — but
  the chosen target (`Rejected` vs `Failed`) depends on which
  replica's clock won the race. Acceptable noise; the retry is
  going to "give up" within the same minute either way.
- **No per-row "how many rejections" query.** Operators must
  consult the log stream for individual invoices. Mitigation: the
  worker logs structured events with `InvoiceId`; Sentry / log
  aggregation surfaces them. If this becomes a frequent ops
  workflow, that's the signal to add the column.
- **Admin retry doesn't extend the budget.** A failed invoice
  that's been `Rejected` for 80h then gets admin-retried will be
  in budget for only 5 more hours before the next worker tick
  escalates. Mitigation: admin tooling shows `(now - CreatedAt)`
  prominently, so the operator knows what budget remains. If the
  business case "needs to extend the budget" emerges, the column
  is the right answer at that point.

### Risks

- **Risk: a future PR adds `RejectionCount` "for an audit query"
  without engaging with this trade-off.** Mitigation: this ADR;
  PR reviewer flags the column addition and consults the rationale.
  The right path forward (if the column is needed) is a new ADR
  superseding this one.
- **Risk: clock skew incident produces a regulator-facing anomaly
  ("invoice gave up 15 minutes early").** Mitigation: NTP sync is
  the operational baseline; the 85h boundary is itself flexible
  (the schedule is `1+4+16+64` hours after the prior failure, not
  a precise wall-clock deadline). The legal SLA is "5 business
  days", which is ~120 hours; the worker gives up at 85h with
  ~35h of safety margin. A 15-min clock-skew incident doesn't
  breach the SLA.
- **Risk: `BackoffHours` config changed mid-flight invalidates
  the implicit count.** If an operator changes `BackoffHours` from
  `[1,4,16,64]` to `[2,8,32,128]` mid-week, existing in-flight
  invoices' budget changes silently. Mitigation: the sum is the
  load-bearing value; both shapes give a sum of 85 and 170, and
  the change is rarely retroactive. Document in DEPLOYMENT.md:
  "Changing `Anaf:BackoffHours` affects all invoices, not just
  newly-created ones."

## Related

- **Stories**: 002-anaf-spv-client (the immediate consumer; the
  budget enforcement happens in `InvoiceUploadJob.PollSubmitted`).
- **Previous ADRs**: ADR-016 (CAS via `ExecuteUpdateAsync`) — the
  CAS shape is what makes the implicit count safe under
  concurrency; ADR-020 (Postgres SEQUENCE accepting gap-on-rollback) —
  same "the simple answer wins; document the trade-off" stance.
- **Future ADRs**: if `Invoice.RejectionCount` is ever added, that
  PR must include an ADR superseding this one and explain the
  triggering operational need.
- **Read when**: working on `InvoiceUploadJob.PollSubmitted` or the
  backoff schedule logic; reviewing PRs that add a counter column
  to `Invoices`; reviewing PRs that change `Anaf:BackoffHours`;
  debugging "why did this invoice escalate to Failed at hour 86";
  designing admin-retry behaviour for similar lifecycle workers;
  reasoning about the regulated 5-business-day SLA vs the
  worker's give-up boundary.
