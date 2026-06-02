---
stage: model
bolt: 037-awb-and-tracking-jobs
created: 2026-06-02T16:05:00Z
---

# Stage 1 — Domain Model: AWB & Tracking Jobs

## Bounded Context

The **Sameday Integration** bounded context (introduced in bolt 036)
gains its operational lifecycle in this bolt:

- The `Order` aggregate's transition to `Paid` triggers an **AWB
  creation** workflow whose only externally visible effect is that
  the existing `Order.AwbNumber` and (new in bolt 036)
  `Order.AwbLabelUrl` become populated.
- The `Order.Status == Shipped` cohort is observed by a periodic
  **tracking poll**; an observed `Delivered` state from Sameday
  transitions the order to `Delivered`, sets `DeliveredAt`, and
  triggers the existing customer-facing delivery email via the
  reused `IOrderEmailService` + `EmailQueue` infrastructure.

The context boundary is unchanged: every interaction with the
Sameday HTTP API still goes through `ISamedayClient` (bolt 036), and
no vendor JSON escapes the anti-corruption layer. What this bolt
adds is the *orchestration* — who calls `ISamedayClient.CreateAwbAsync`
and `GetTrackingAsync`, when, and what they do with the result.

Bolt 037 is structurally isomorphic to bolt 051's photo-promotion
lifecycle: an in-process `Channel<T>` queue + a `BackgroundService`
consumer + a periodic recovery / retry job that re-derives the work
to be done from the durable `Order` table. The same trade-offs apply
and the same multi-instance constraints exist; see Multi-instance
Safety below.

---

## Entities

There is one entity change in this bolt — a new persisted field on the
existing `Order` aggregate root.

- **Order** (existing aggregate root, extended)
  - `DeliveredAt: DateTimeOffset?` *(new — timestamptz, nullable)*
    - UTC timestamp at which the **tracking job** observed a Sameday
      `delivered` state and transitioned this order to `Delivered`.
    - Set exactly once, atomically with `Status = Delivered`.
    - Invariant: `DeliveredAt is not null ⇔ Status == Delivered` for
      orders that reached delivery via the tracking job. Existing
      manual `Delivered` transitions (set by admin actions today)
      may have `DeliveredAt == null`; the tracking job tolerates this.
  - Reads from `AwbNumber` (existing), `AwbLabelUrl` (bolt 036),
    `LastTrackingSyncAt` (bolt 036), `PaidAt` (existing),
    `ShippedAt` (existing).

No new aggregates. AWB creation and tracking are operations on the
existing `Order` aggregate; they do not introduce new aggregates of
their own. (See "Why no new aggregate?" below.)

### Why no new aggregate?

An obvious-looking alternative is a separate `AwbAttempt` aggregate
(or `AwbCreationJob`, `TrackingPoll`) that owns the per-order
lifecycle. Rejected for two reasons:

1. **Source-of-truth duplication.** The state we'd need to persist
   on an `AwbAttempt` aggregate (`Pending / Failed / Succeeded`) is
   already derivable from `Order` columns: `Paid AND AwbNumber IS
   NULL → pending`, `AwbNumber IS NOT NULL → succeeded`. Mirror of
   ADR-010's reasoning for the promotion queue.
2. **Crash safety is free.** Because the work-to-do is derivable
   from `Order`, the retry job IS the recovery scanner. A separate
   aggregate would need a parallel "is this attempt still alive?"
   reconciliation path.

Both of those are exactly the trade-offs we already accepted in ADR-010
for the photo-promotion queue. Bolt 037 inherits that posture.

---

## Value Objects

All immutable, equal by value, validated at construction.

- **`AwbCreationRequest`** *(declared in bolt 036; consumed here)*
  - Fields: `PickupPointId`, `RecipientName`, `RecipientPhone`,
    `RecipientAddress`, `RecipientCity`, `RecipientCounty`,
    `RecipientPostalCode`, `ParcelWeightKg`, `ParcelCount`,
    `CodAmountRon`, `Observations`.
  - **Construction site (new in this bolt):** the application-layer
    `OrderToAwbRequestMapper` translates an `Order` aggregate into
    this value. Two recipient sources:
    - `DeliveryType == Easybox`: recipient fields come from the
      attached `EasyboxLocker` row.
    - `DeliveryType == Courier`: recipient fields come from
      `Order.ShippingAddress` (`RecipientName`, `Phone`,
      Street + Number, City, County, PostalCode).
  - Validation that fires at construction (not the wire):
    - `ParcelWeightKg > 0`, `ParcelCount >= 1`.
    - All recipient address fields non-empty.

- **`AwbCreationResult`** *(declared in bolt 036; consumed here)*
  - Fields: `AwbNumber`, `LabelUrl`, `CalculatedPrice`.

- **`TrackingSnapshot`** *(declared in bolt 036; consumed here)*
  - Fields: `AwbNumber`, `State` (`TrackingState`), `ObservedAt`
    (Sameday-side timestamp), `History: IReadOnlyList<TrackingEvent>`.

- **`TrackingState`** *(enum, declared in bolt 036)*
  - Members: `Unknown`, `Pending`, `InTransit`, `OutForDelivery`,
    `Delivered`, `Failed`, `Cancelled`.

- **`ParcelWeight`** *(new in this bolt)*
  - Encapsulates the parcel-weight heuristic from FR-3:
    `weightGrams = totalPrintCount × 50 + 50`.
  - Construction: `ParcelWeight.FromOrder(order)`, sums
    `OrderItems.Sum(qty)` and applies the formula.
  - Exposes both grams and kilograms (`Sameday` API takes kg).
  - Invariant: `ParcelWeight.Grams >= 50` (the +50 g floor).
  - Rationale for a value object rather than an inline calculation:
    the heuristic is the kind of thing that *will* change (per
    `ProductSize` weights in intent 016+) and a single chokepoint
    keeps the change blast-radius small.

- **`AwbJob`** *(new in this bolt — channel payload)*
  - Fields: `OrderId: Guid`, `Attempt: int`, `EnqueuedAt:
    DateTimeOffset`.
  - Used by the in-process `Channel<AwbJob>` queue to dispatch work
    from the `Paid`-transition hook to the AWB-creation consumer.
  - Carries an attempt counter so a failed channel item can be
    re-enqueued with a back-off (mirror of bolt 051's
    `PromotionJob`).

- **`AwbCreationOutcome`** *(new in this bolt — return value)*
  - Discriminated union of:
    - `Created(AwbNumber, LabelUrl)` — happy path.
    - `Skipped(reason)` — order no longer eligible (cancelled,
      AWB already exists).
    - `RetryLater(reason, isTransient)` — Sameday unreachable,
      protocol failure, validation failure; the bool tells the
      caller whether to re-enqueue or to log give-up.
    - `GiveUp(reason)` — terminal failure that should *not* be
      retried (e.g. validation failure: missing pickup point,
      recipient address rejected by Sameday). Distinct from
      `RetryLater` precisely so the retry-job's "24-h give-up"
      check is separate from "is this attempt transient or
      permanent."
  - Returned by `IAwbCreator.CreateForOrderAsync`.

- **`TrackingPollOutcome`** *(new in this bolt — return value)*
  - Discriminated union of:
    - `NoChange` — Sameday still reports a non-terminal state;
      `LastTrackingSyncAt` is updated, status is not.
    - `Delivered(deliveredAt)` — Sameday reports `delivered`;
      caller transitions the order and fires the delivery email.
    - `PollingStopped(reason)` — order older than 30 days from
      `ShippedAt`; the job emits a one-shot Warning and stops
      polling this order.
    - `Failed(reason, isTransient)` — Sameday call failed.

---

## Aggregates

`Order` remains the single aggregate. Its invariants are extended
(not replaced) by this bolt:

- **AWB creation atomicity.** A single `SaveChangesAsync` persists
  both `AwbNumber` and `AwbLabelUrl` together. Either both fields
  go from null to populated, or neither does. The order's `Status`
  is *not* touched by this bolt — AWB creation is observable only
  in the two new fields; the existing manual `Paid → Shipped`
  transition path remains the admin's prerogative.

- **Tracking transition atomicity.** A single `SaveChangesAsync`
  persists all four of: `Status` (`Shipped → Delivered`),
  `DeliveredAt`, `LastTrackingSyncAt`, and any side-effect row
  (the `EmailQueue` entry produced by `IOrderEmailService`). The
  transition is guarded by a compare-and-swap on `Status`: the
  UPDATE only fires if `Status == Shipped` at the moment of
  persistence; a concurrent admin transition to `Cancelled` (or
  another replica's already-applied `Delivered`) loses the race
  silently. This is the multi-instance-safe story for transitions
  until intent 021's leader-election or row-level locks land.

- **Tracking sync is non-decreasing.** `LastTrackingSyncAt` is
  monotonically non-decreasing per order; the job rejects a write
  whose `ObservedAt` is earlier than the stored value.

---

## Domain Events

This bolt is the first place in intent 015 where domain events
naturally land. Following the project's existing pattern of
"observable persistence + log + email-queue trigger" rather than
an event bus, the events below are *conceptual* — they correspond
to specific lines of code that emit structured logs + (in one case)
enqueue an outbound email. No new dispatcher infrastructure.

- **`AwbCreated`**
  - **Trigger:** `IAwbCreator.CreateForOrderAsync` returns
    `AwbCreationOutcome.Created` and `SaveChangesAsync` succeeds.
  - **Payload:** `OrderId`, `AwbNumber`, `LabelUrl`, `Attempt`.
  - **Observers:**
    - Structured log: `Information sameday.awb.created
      order_id={id} awb={awb} attempt={n}`.
    - No customer email (the order-confirmation email already fired
      earlier in the checkout flow, per story 001's explicit
      out-of-scope).

- **`AwbCreationGivenUp`**
  - **Trigger:** Retry job observes an order that has been
    `Paid AND AwbNumber IS NULL` for > 24 h and emits a one-shot
    give-up event for it.
  - **Payload:** `OrderId`, `PaidAt`, `LastAttemptError`.
  - **Observers:**
    - Structured log: `Error sameday.awb.give-up order_id={id}
      paid_at={ts} last_error={msg}`.
    - Admin notification (out of scope here — wired in a future
      intent; the log entry is the audit trail until then).

- **`AwbCreationPermanentlyFailed`**
  - **Trigger:** `IAwbCreator` returns `AwbCreationOutcome.GiveUp`
    (e.g. Sameday returned `SamedayValidationException` — our
    request is malformed: bad postal code, weight over the
    courier ceiling, unknown pickup point).
  - **Payload:** `OrderId`, `Reason`, `HttpStatus`.
  - **Observers:**
    - Structured log: `Error sameday.awb.permanent-fail
      order_id={id} reason={reason}`.
    - **NOT re-enqueued** — distinct from `RetryLater`. Order
      stays in `Paid` with no AWB. Admin manual path takes over.

- **`ShipmentDelivered`**
  - **Trigger:** `ShipmentTrackingJob` observes `TrackingState ==
    Delivered` for an order and the compare-and-swap transition
    to `Delivered` succeeds.
  - **Payload:** `OrderId`, `AwbNumber`, `DeliveredAt`.
  - **Observers:**
    - Structured log: `Information sameday.shipment.delivered
      order_id={id} awb={awb}`.
    - Enqueues the existing customer "your order has been
      delivered" email via the reused `IOrderEmailService`.

- **`ShipmentPollingStopped`**
  - **Trigger:** Tracking job sees an order with
    `ShippedAt < now - 30d` still in `Shipped` and emits a
    one-shot warning before excluding it from future polls.
  - **Payload:** `OrderId`, `ShippedAt`.
  - **Observers:**
    - Structured log (once per order): `Warning
      sameday.tracking.polling-stopped order_id={id}
      shipped_at={ts}`.
    - The order stays `Shipped`; admin handles manually.

---

## Domain Services

The Sameday context (bolt 036) provided the *transport* services
(`ISamedayClient`, `ISamedayTokenProvider`, the HTTP pipeline).
Bolt 037 introduces the *lifecycle* services that orchestrate them
against the `Order` aggregate.

- **`IAwbCreator`** *(application service)*
  - Operation: `Task<AwbCreationOutcome> CreateForOrderAsync(Guid
    orderId, int attempt, CancellationToken ct)`.
  - Responsibilities:
    - Load the `Order` aggregate (including `Items`, `EasyboxLocker`).
    - Guard the eligibility check: still in `Paid` AND `AwbNumber
      IS NULL` (skip otherwise → `Skipped`).
    - Build `AwbCreationRequest` via `OrderToAwbRequestMapper`.
    - Call `ISamedayClient.CreateAwbAsync(request, ct)`.
    - On success: persist `AwbNumber` + `AwbLabelUrl` atomically,
      return `Created`.
    - On `SamedayUnreachableException`: return `RetryLater
      (transient: true)`.
    - On `SamedayProtocolException`: return `RetryLater
      (transient: false)` — the contract changed; retry won't
      help in the short term, but a manual ops fix might.
    - On `SamedayValidationException`: return `GiveUp` — *our*
      request is malformed; retrying with the same input won't
      help.
    - On `SamedayAuthException`: return `RetryLater
      (transient: false)` — credentials need ops attention; the
      retry job will keep polling for the operator to fix the
      flag.

- **`OrderToAwbRequestMapper`** *(domain service)*
  - Single static `ToRequest(Order, SamedaySettings)` method.
  - Picks the recipient source based on `DeliveryType`:
    - `Easybox` → use `Order.EasyboxLocker` row.
    - `Courier` → use `Order.ShippingAddress` snapshot fields.
  - Applies `ParcelWeight.FromOrder(order)`.
  - Validates *before* the call: throws `ArgumentException` if the
    derived request fails the value-object invariants (zero items,
    missing locker, etc.). The job catches this and surfaces it as
    `AwbCreationOutcome.GiveUp("invalid request")` — a permanent
    failure that needs an operator, not a retry.

- **`IAwbJobQueue`** *(infrastructure-flavoured domain service —
  in-process `Channel<AwbJob>`)*
  - Operations:
    - `ValueTask EnqueueAsync(AwbJob job, CancellationToken ct)`.
    - `IAsyncEnumerable<AwbJob> DequeueAllAsync(CancellationToken
      ct)`.
  - One singleton per host. Bolt 051's `IPromotionQueue` is the
    template. The channel is **unbounded** for now (each AWB job
    is one HTTP call + one DB row; 1000-order spike costs ~30 MB
    of pending payloads).
  - **Multi-instance posture:** each replica has its own channel.
    The `Paid → enqueue` path is single-replica-correct *because*
    the `OrderStatusMachine` hook fires inside the request the
    payment webhook handles; only one replica observes the
    transition. The retry job is the safety net for "the replica
    that enqueued went down before its consumer drained the job."

- **`IAwbDispatcher` : `BackgroundService`** *(new)*
  - Consumes `IAwbJobQueue`, calls `IAwbCreator.CreateForOrderAsync`
    per job, and handles the outcomes:
    - `Created` → log + done.
    - `Skipped` → log + done (order no longer eligible).
    - `RetryLater(transient: true)` → re-enqueue with exponential
      backoff up to a per-process cap (5 attempts; 30 s / 120 s /
      300 s / 900 s / 3600 s — same pattern as bolt 051's
      `OrderPhotoArchive:BackoffSeconds`).
    - `RetryLater(transient: false)` / `GiveUp` → log + done.
      The retry job (below) handles re-discovery if the order
      should still be retried later.
  - Cap on concurrent in-flight Sameday calls: **5** (per story
    002 technical note). Implemented via a `SemaphoreSlim` inside
    the dispatcher; bolt 036's resilience handler will gain the
    declarative rate-limit policy on `Polly.RateLimiting` in this
    bolt now that high-frequency callers exist.

- **`AwbRetryJob` : `BackgroundService`** *(new)*
  - Periodic timer; default 1 h (config:
    `Sameday:Jobs:AwbRetryIntervalMinutes`).
  - Per tick, queries:
    `WHERE Status == Paid AND AwbNumber IS NULL AND PaidAt > now
    - 24h`.
  - For each, **enqueues an `AwbJob`** rather than calling
    `IAwbCreator` directly. This funnels every AWB creation
    through the same dispatcher (one place to test, one place to
    rate-limit, one place to log).
  - For orders **older** than 24 h from `PaidAt` that still have
    no AWB: emits `AwbCreationGivenUp` once (deduped by a sentinel
    `Internal Notes` marker or a one-shot `MemoryCache` entry per
    order id) and stops re-enqueueing.

- **`ShipmentTrackingJob` : `BackgroundService`** *(new)*
  - Periodic timer; default 15 min (config:
    `Sameday:Jobs:TrackingIntervalMinutes`).
  - Per tick, queries:
    `WHERE Status == Shipped AND ShippedAt > now - 30d AND
    AwbNumber IS NOT NULL AND
    (LastTrackingSyncAt IS NULL OR LastTrackingSyncAt < now -
    TrackingIntervalMinutes)`.
  - For each, calls
    `ISamedayClient.GetTrackingAsync(order.AwbNumber, ct)`.
    - On `TrackingState.Delivered`: compare-and-swap transition
      to `Delivered`, set `DeliveredAt`, set `LastTrackingSyncAt`,
      enqueue delivery email. Returns `TrackingPollOutcome.Delivered`.
    - On any other terminal-but-not-Delivered state (`Failed`,
      `Cancelled`): updates `LastTrackingSyncAt` only; admin
      manual.
    - On `Pending` / `InTransit` / `OutForDelivery` / `Unknown`:
      updates `LastTrackingSyncAt` only.
    - On `SamedayUnreachableException`: skip this order this
      tick; the next tick retries.
  - For orders with `ShippedAt < now - 30d` that are still
    `Shipped`: emits `ShipmentPollingStopped` once per order (same
    dedup mechanism as the AWB give-up) and excludes them from
    subsequent polls via the `ShippedAt > now - 30d` clause.
  - Concurrency: caps in-flight polls at **5** via `SemaphoreSlim`
    (same rationale as the dispatcher).

- **`IOrderStatusMachine` (existing — extended)**
  - The existing `AfterTransitionAsync` hook (per story 001
    technical note) gets one new handler: on
    `AwaitingPayment → Paid` (or whatever the existing transition
    name is), enqueue `AwbJob(orderId, attempt: 1, enqueuedAt:
    now)`.
  - The hook does *not* block on the AWB call; it returns
    immediately after `Channel.Writer.TryWrite(job)`.

---

## Repository Interfaces

No new repositories. Every read/write goes through the existing
`PhotoPrintDbContext.Orders` `DbSet<Order>` + the existing services
that wrap it.

---

## Configuration Surface

Extension to `SamedaySettings` (or a sibling `SamedayJobsSettings`,
TBD in Stage 2) with the following keys:

- `Sameday:Jobs:Enabled` — global on/off for *all three jobs*
  (the existing `Sameday:Enabled` flag controls the typed-client
  registration in bolt 036; this is an orthogonal gate so a
  deployment can wire credentials but still keep the jobs paused
  during initial rollout).
- `Sameday:Jobs:AwbRetryIntervalMinutes` — default 60.
- `Sameday:Jobs:AwbGiveUpHours` — default 24.
- `Sameday:Jobs:TrackingIntervalMinutes` — default 15.
- `Sameday:Jobs:TrackingMaxAgeDays` — default 30.
- `Sameday:Jobs:MaxConcurrentSamedayCalls` — default 5.
- `Sameday:Jobs:DispatchBackoffSeconds` — default `[30, 120, 300,
  900, 3600]`. Bound on per-process retry inside `IAwbDispatcher`
  before the retry job becomes the sole driver.

When `Sameday:Enabled == false` OR `Sameday:Jobs:Enabled == false`,
none of the three new services register and the
`OrderStatusMachine` hook silently no-ops. The default `appsettings.json`
ships with both flags `false`.

---

## Multi-Instance Safety

This is the load-bearing constraint of the bolt and the reason
several design choices look the way they do. Until intent 021
introduces leader-election or distributed locks (the same crossing
point that supersedes ADR-010 and ADR-013), every job has to be
*correct* under multi-replica deployment, even if not optimal.

The three jobs handle multi-instance correctness as follows:

1. **AWB creation (channel + dispatcher).** Inherently single-replica:
   the payment-webhook request only hits one replica, so only that
   replica observes `AwaitingPayment → Paid`. Its hook enqueues to
   its *own* channel. No cross-replica coordination needed for the
   happy path. Crash safety is provided by the retry job below.

2. **AWB retry job.** Multi-replica unsafe at the *enqueue* layer:
   two replicas could each enqueue the same order ID. **Mitigation
   accepted, not eliminated:** Sameday's external-reference handling
   (the order ID is passed as `awbPayment` per story 001) makes
   duplicate `CreateAwb` calls *idempotent on the vendor side*; the
   second call returns the same AWB number. Our code defends with
   the `Status == Paid AND AwbNumber IS NULL` re-check inside
   `IAwbCreator` — the second dispatcher sees the AWB already
   persisted and returns `Skipped`. Either way, no harm.

3. **Tracking job.** Multi-replica safe by **compare-and-swap on
   Status**: the UPDATE that transitions `Shipped → Delivered` has
   `WHERE Status = 'Shipped'`. The losing replica's UPDATE affects
   0 rows; it logs an Info-level "race lost, status already
   advanced" and moves on. EF's optimistic-concurrency story already
   supports this if we set the `WHERE` clause via
   `EntityEntry.OriginalValues` or a stored procedure / SQL update.
   `LastTrackingSyncAt` updates do NOT need a CAS — the
   monotonically-non-decreasing invariant is enforced in code
   before the UPDATE fires.

ADRs in Stage 3 will likely formalize at least the AWB-duplication
trade-off (item 2 above) so future readers know it was deliberate.

---

## Error Taxonomy (consumed, not extended)

Bolt 037 does not introduce new exception types. It consumes the
Sameday exception taxonomy from bolt 036 and maps them onto the
`AwbCreationOutcome` / `TrackingPollOutcome` discriminated unions.

| Source exception | Outcome | Behaviour |
|---|---|---|
| `SamedayUnreachableException` | `RetryLater(transient: true)` (AWB) / skip-tick (tracking) | Re-enqueue with backoff or wait for next tick. |
| `SamedayAuthException` | `RetryLater(transient: false)` (AWB) / log + skip (tracking) | Credentials need ops; retry job keeps polling so once ops fixes the flag/secret, work resumes. |
| `SamedayProtocolException` | `RetryLater(transient: false)` (AWB) / log + skip (tracking) | Vendor contract drift; retry job keeps polling. |
| `SamedayValidationException` | `GiveUp` (AWB) / log + skip (tracking) | Our request is wrong; retrying with the same input won't help. AWB give-up does not roll up to the 24-h timer — it is terminal immediately. |
| Internal `ArgumentException` from `OrderToAwbRequestMapper` | `GiveUp("invalid request")` (AWB) | Same as validation; the request never even reached Sameday. |

---

## Ubiquitous Language

| Term | Definition |
|---|---|
| **AWB job** | An in-process `AwbJob` record on the `Channel<AwbJob>` — represents one pending attempt to create an AWB for a specific order. |
| **AWB dispatcher** | The `BackgroundService` that drains the channel, calls `IAwbCreator`, and handles the outcome. |
| **AWB retry job** | The periodic safety-net job that re-enqueues orders that the dispatcher missed (crash, replica restart, give-up reset, …). |
| **Give-up window** | The 24-h envelope after `Order.PaidAt` during which the retry job will keep re-enqueueing failed AWB creations. After 24 h the retry job logs one `AwbCreationGivenUp` event per order and excludes it from future ticks. The order stays in `Paid` until admin manual intervention. |
| **Tracking poll** | One call to `ISamedayClient.GetTrackingAsync` from `ShipmentTrackingJob`. |
| **Tracking window** | The 30-day envelope after `Order.ShippedAt` during which the tracking job polls. Orders older than 30 days still in `Shipped` get one `ShipmentPollingStopped` event and are excluded from subsequent polls. |
| **Tracking sync** | The act of updating `Order.LastTrackingSyncAt` (which happens on *every* successful poll, whether or not the status changed). |
| **CAS transition** | A `WHERE Status = 'Shipped'`-guarded UPDATE — the compare-and-swap that keeps `Shipped → Delivered` multi-replica-safe until intent 021 lands real locks. |
| **Parcel weight heuristic** | `weightGrams = totalPrintCount × 50 + 50`. Lives in `ParcelWeight.FromOrder(order)`. Will be replaced when intent 016+ adds per-`ProductSize` weights. |

---

## Story Coverage

- **001-awb-creation-on-paid** — covered by `OrderStatusMachine`
  hook + `IAwbJobQueue` (channel) + `IAwbDispatcher` (consumer) +
  `IAwbCreator` (per-order workflow) + `OrderToAwbRequestMapper` +
  `ParcelWeight` + the `AwbCreated` event.
- **002-awb-retry-job** — covered by `AwbRetryJob :
  BackgroundService` and the 24-h `AwbCreationGivenUp` envelope.
  The job enqueues to the same dispatcher as story 001, so behaviour
  is one path with one set of tests.
- **003-shipment-tracking-job** — covered by `ShipmentTrackingJob
  : BackgroundService`, the CAS transition rule, the
  `DeliveredAt` field, the `LastTrackingSyncAt` monotonic invariant,
  the `ShipmentDelivered` and `ShipmentPollingStopped` events, and
  the reused `IOrderEmailService` delivery-email path.

---

## Completion Checklist

- [x] All domain entities identified (`Order` extended;
      `DeliveredAt` added).
- [x] Business rules captured (CAS transition, monotonic
      tracking sync, 24-h give-up envelope, 30-d tracking
      envelope, parcel-weight heuristic).
- [x] Aggregate boundary intact (no new aggregates; everything
      hangs off `Order`).
- [x] Domain events specified (`AwbCreated`,
      `AwbCreationGivenUp`, `AwbCreationPermanentlyFailed`,
      `ShipmentDelivered`, `ShipmentPollingStopped`).
- [x] Domain services designed (`IAwbCreator`, `IAwbJobQueue`,
      `IAwbDispatcher`, `AwbRetryJob`, `ShipmentTrackingJob`,
      `OrderToAwbRequestMapper`).
- [x] Repository interfaces — no new ones; existing path used.
- [x] Multi-instance safety analysed for every job and the design
      annotated with the mitigations.
- [x] All three stories (001 / 002 / 003) covered by the model.

---

## ⛔ Human Checkpoint

Stage 1 (Domain Model) is drafted. Please review and approve before
I move to Stage 2 (Technical Design).

**Ready to proceed?**

- **1** — Approve and continue to Stage 2.
- **2** — Need changes (specify which section).
