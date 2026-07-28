---
bolt: 037-awb-and-tracking-jobs
created: 2026-06-02T17:10:00Z
status: amended
---

# ADR-015: Accept Duplicate `CreateAwb` Calls on Multi-Replica (Rely on Vendor Idempotency + DB Re-Check)

## Amendment (2026-07-27, review 015-sameday-shipping v1 + v3)

**This ADR's "accept duplicates" stance is superseded — reviews found the original design did not
actually enforce the safety it assumed.** The current mechanism, in order of defence:

1. **Per-order vendor reference.** The idempotency key sent to Sameday is
   `clientInternalReference = Order.OrderNumber` (NOT `awbPayment`, which is a constant payment-type
   flag = 1). The original text below naming `awbPayment` as the key was wrong.
2. **Durable per-order claim (D45).** Before the vendor call, `AwbCreator` atomically claims the order
   (`Orders.AwbClaimedAt`, TTL `Sameday:Jobs:AwbClaimTtlMinutes`, reclaimable after the TTL). A
   concurrent creator (retry re-enqueue, second replica, duplicate webhook) finds a fresh claim and
   `Skipped`s **before** billing a second label. This closes the *concurrent* double-call.
3. **Guarded DB write.** Persist is `ExecuteUpdate WHERE Id AND AwbNumber IS NULL AND Status != Cancelled`;
   `affected == 0` reads the value back to tell benign vendor-dedup convergence from a genuine orphan
   (Error-logged for manual reconciliation — there is no vendor void endpoint in the client).

**Honest residual (NOT closed):** the **crash/timeout window** — a worker that bills an AWB then dies
before persisting — is reclaimed after the TTL and re-created; whether that mints a *second* billable
label still rests entirely on Sameday deduping on `clientInternalReference`, which is **unverified**.
There is no client-side close without a vendor "AWB-by-reference" lookup (not implemented). **Verify
Sameday's create-idempotency before flipping `Sameday:Jobs:Enabled=true`.** The claim TTL must exceed
one vendor round-trip (`RequestTimeoutSeconds`) with margin and is deliberately decoupled from the
retry cadence.

The original decision text is kept below for history.

---

## Context

`AwbRetryJob` is a `BackgroundService` that fires every 60 minutes
and re-enqueues every order in the cohort
`Status == Paid AND AwbNumber IS NULL AND PaidAt > now - 24h`. With
multiple API replicas in front of the same database (the eventual
target topology — bolt 046 introduces leader-election / Redis locks
that supersede this; bolt 037 ships *before* that), each replica
runs its own copy of the job and each replica's job will discover
the same eligible orders.

Without coordination, this means:

- Replica A's tick at time T0 enqueues order X.
- Replica B's tick at time T0+5s enqueues order X.
- Replica A's `AwbDispatcher` calls `Sameday /api/awb` for order X.
- Replica B's `AwbDispatcher` calls `Sameday /api/awb` for order X
  again, ~milliseconds later.

We had to decide whether to *prevent* duplicate calls (locks,
leader election, claim-on-enqueue table, distributed mutex) or to
*tolerate* them and verify the system remains correct under
duplication.

The decision affects: scale-out posture today vs. when intent 021
lands, what infrastructure has to ship before we can flip the jobs
on, and where the load-bearing safety invariants actually live.

## Decision

**Duplicate `CreateAwb` calls under multi-replica are accepted, not
eliminated. Correctness rests on two load-bearing properties that
MUST both hold:**

1. **Sameday vendor-side idempotency.** Sameday's
   `POST /api/awb` accepts an `awbPayment`-keyed external reference
   tied to our order ID. A second call against the same external
   reference returns the same AWB number rather than minting a
   second AWB. This is the *vendor-side* half of the invariant.

2. **Application-side re-check.** `IAwbCreator.CreateForOrderAsync`
   re-reads the `Order` and short-circuits to
   `AwbCreationOutcome.Skipped` if either condition fails:
   - `Status != OrderStatus.Paid` (e.g., the order was cancelled
     between enqueue and dispatch).
   - `AwbNumber is not null` (another worker has already persisted
     the result).
   This is the *application-side* half of the invariant.

A future PR that breaks *either* property silently breaks
correctness. This ADR exists to make the dependency loud.

## Rationale

The alternatives were all materially worse for what bolt 037 needs
to ship right now:

- **Leader election / distributed locks** add a new piece of
  infrastructure (`Redis`, `etcd`, or a SQL-backed advisory lock).
  Bolt 046 already plans to introduce Redis for this exact purpose;
  bolt 037 would either block on bolt 046 (intent 015 stalls) or
  duplicate the work and then have to be un-duplicated.

- **Claim-on-enqueue table** (UPDATE `Orders SET ClaimedBy = …, …
  WHERE Id = … AND ClaimedBy IS NULL`) requires a new column, a
  new index, a TTL story for stale claims, and a recovery path for
  replicas that crashed mid-claim. Substantially more moving parts
  than the actual problem warrants.

- **Single hosted replica only** (deploy with `replicas=1`) erases
  the problem but also erases the scale story; intent 015 should
  not be the bolt that forces a deployment-topology constraint.

The "live with it + verify safety" choice is cheap because the
verification is itself cheap: the two-property invariant is
testable, the vendor property is documentable, the application
property is one extra `WHERE` clause already in the design.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|---|---|---|---|
| **Redis-backed leader election (advisory lock per order id)** | Eliminates duplicate calls entirely. Aligns with bolt 046's planned target architecture. | Requires Redis (not yet in project). Bolt 037 blocks on bolt 046. Adds a new operational dependency. | Premature. Bolt 046 will land this; until then, the cost of duplicate calls is zero (vendor + re-check absorb it). |
| **`Orders.ClaimedBy` column + TTL** | Pure SQL — no new infra. Visible in the DB so debugging is easy. | New column + index. Stale-claim recovery is its own state machine. Doubles the per-job latency (UPDATE-then-call-then-UPDATE). | More machinery than the problem warrants. Vendor-side idempotency already does the work. |
| **Single-replica deployment** | Trivially correct. | Imposes a deployment-topology constraint via this one feature. Defeats the scale story. | Wrong direction; intent 015 should not foreclose scaling. |
| **`SELECT … FOR UPDATE` skip-locked enqueue** | Database-native. No new infra. | Postgres-specific (we'd need a fallback for SQLite-dev or accept divergence). Per-row pessimistic locking is heavier than necessary. | Vendor + app re-check already give us the same result with less code. |

## Consequences

### Positive

- **Zero new dependencies.** Bolt 037 ships without Redis, without
  a new column, without a leader-election library.
- **Multi-replica deployment works today.** No "scale out by
  setting replicas=1" carve-out for this feature.
- **The invariant is explicit and testable.** A unit test asserts
  `IAwbCreator.CreateForOrderAsync` short-circuits to `Skipped`
  when a second call sees the AWB already populated. A separate
  integration test (deferred — needs CI-gated Sameday sandbox
  access) verifies the vendor-side idempotency.
- **Composes with bolt 036's other "in-process now, durable later"
  decisions.** Same scaling-crossover point (intent 021's Redis
  introduction) as ADR-010 and ADR-013.

### Negative

- **Two replicas double the auth call rate.** Each replica's
  `SamedayTokenProvider` cache is independent (per ADR-013), so
  on N replicas we authenticate up to N times per token cycle.
  At our rate-limit ceiling (5 req/s) this is comfortably absorbed.
- **Logs duplicate.** Each replica that processes a re-enqueued
  duplicate emits its own log lines. Filterable by `order_id`,
  but it does inflate log volume modestly (≈ 2× during retry
  sweeps if running 2 replicas).
- **Vendor lock-in on idempotency semantics.** If Sameday ever
  changes external-reference handling — e.g. starts minting a new
  AWB per call rather than returning the existing one — this
  invariant breaks silently. **Mitigation**: the integration
  end-to-end test referenced in bolt 036's test-report (deferred
  to CI-gated sandbox) is what catches this. The day vendor
  behaviour drifts is the day we land bolt 046's leader-election.

### Risks

- **Risk: someone refactors `IAwbCreator` and drops the re-check.**
  Highest-likelihood failure mode. Mitigation: the application-side
  half of the invariant is pinned by a unit test
  (`AwbCreator_returns_Skipped_when_AwbNumber_already_populated`).
  A future PR that removes the re-check breaks that test.
- **Risk: vendor idempotency assumption is wrong from day one.**
  Mitigation: the first production order is a controlled shadow
  shipment (per `requirements.md` assumptions). If the first prod
  AWB workflow ever sees a duplicate AWB number minted, this ADR
  is superseded immediately and the feature flag flips back to
  `false` while bolt 046 is accelerated.
- **Risk: we acquire a third writer to `Orders.AwbNumber`.** Today
  it's the dispatcher + the retry job. A future intent that writes
  the column from a fourth path (admin manual override?) would
  need the same re-check. Mitigation: this ADR codifies the
  re-check as a *contract*, not an implementation detail of one
  service.

## Related

- **Stories**: 001-awb-creation-on-paid, 002-awb-retry-job.
- **Previous ADRs**: ADR-010 (same in-process-now stance for the
  promotion queue), ADR-013 (same per-replica token-cache stance).
- **Future ADRs**: bolt 046's Redis introduction will likely
  supersede this — at that point distributed locks become cheap
  and the duplicate-call surface shrinks to zero.
- **Read when**: working on `AwbRetryJob` or `IAwbCreator`;
  modifying the `AwbNumber` write path; refactoring the
  `Status == Paid AND AwbNumber IS NULL` re-check (don't —
  it's the load-bearing half); reasoning about scale-out before
  bolt 046; vendor behaviour drift post-mortem; debugging
  "why are there two `sameday.awb.created` logs for the same
  order id?"; designing similar acceptance-of-duplication
  trade-offs for other vendor integrations.
