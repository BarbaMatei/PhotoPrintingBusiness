---
type: findings
target: 015-sameday-shipping
version: 1
commit: 1765918
pass-type: discovery
date: 2026-07-27
---

# Findings v1 — 015-sameday-shipping

Full per-finding detail behind [review-v1.md](review-v1.md). Each block: severity · file:line ·
convergence (independent lenses) · verdict · scenario → fix → evidence (the skeptic's trace, or the
reviewer's own read where noted). `hinted` marks a topic seeded by the shared project context.

---

## 🔴 High — blockers

### F1 (D1) — AWB vendor idempotency key wired to constant `PickupPointId`, not per-order
`Services/Sameday/SamedayClient.cs:104` · conv **6** (correctness, security, requirements, tests-coverage, race, completeness-critic) · **confirmed** (6-lens agreement + reviewer read)

- **Scenario.** Two replicas' retry sweeps dispatch order X within ms, before either persists
  `AwbNumber`; both pass the app-side re-check and call `/api/awb`. Sameday cannot dedup (the order id
  is absent), so two AWBs are minted — or, if it dedups on the shared `PickupPointId`, order N gets
  order 1's AWB → orphan AWB, double courier cost, or mis-shipment.
- **Fix.** Carry `OrderId`/`OrderNumber` in `AwbCreationRequest` and send it as the vendor external
  reference (`clientInternalReference`), not `PickupPointId`; add a test pinning the reference == order
  id. Pair with F2's guarded write.
- **Evidence.** `SamedayClient.cs:104`: `ClientInternalReference = request.PickupPointId, // vendor
  uses this as our idempotency key (ADR-015)`. `request.PickupPointId` comes from
  `OrderToAwbRequestMapper.cs:39` = `settings.PickupPointId` — a shop-wide constant, identical for
  every order. ADR-015's multi-replica safety rests entirely on this being per-order.

### F2 (D2) — Concurrent AWB creators for one order produce duplicate AWBs (check-then-act)
`Services/Sameday/AwbCreator.cs:69` · conv 2 (correctness, race) · **confirmed** (trace)

- **Scenario.** Two replicas' `AwbRetryJob` startup sweeps (or two concurrent webhook redeliveries)
  enqueue two jobs for one Paid order. Both `AwbCreator`s load it `AwbNumber`-null, pass the re-check,
  both call `CreateAwbAsync` → two real AWBs. Last `SaveChanges` wins (no unique index, no token); the
  other AWB is a paid, unreclaimable orphan parcel.
- **Fix.** A stable per-order vendor key (F1) so Sameday dedups, **plus** a guarded write
  (`ExecuteUpdate … WHERE AwbNumber IS NULL`) or a unique index. In-process dedup can't stop a
  multi-replica double-create.
- **Evidence.** Replica A loads X (null), passes re-check (`AwbCreator.cs:51-54`), calls
  `CreateAwbAsync` (69) → AWB1. Replica B, separate `DbContext`, loads X still null before A commits,
  passes re-check → AWB2. Both `SaveChanges` (96); no unique index (`DbContext:306` only
  `HasMaxLength`), no concurrency token.

### F3 (D3) — Single `DbContext` shared across concurrent tracking-poll tasks
`BackgroundJobs/ShipmentTrackingJob.cs:87` · conv 2 (correctness, completeness-critic) · **confirmed** (trace)

- **Scenario.** `RunOneTickAsync` resolves one `db`, then `Task.WhenAll` runs up to
  `MaxConcurrentSamedayCalls` (default 5) `PollOneAsync` concurrently, each issuing
  `ExecuteUpdateAsync`/`FirstAsync` on that same `db`. With ≥2 shipped orders in a tick, EF's
  concurrency detector throws "second operation started on this context"; the tick faults and no
  delivery is recorded.
- **Fix.** Create a scope + `DbContext` per order inside `PollOneAsync` (as `AwbDispatcher` already
  does), or serialize the DB writes.
- **Evidence.** Semaphore init=5, so no serialization. Op A opens EF's concurrency-detector critical
  section and awaits the round-trip; op B's `ExecuteUpdateAsync` starts during that await, sees
  `_inCriticalSection`, throws. Exception faults `WhenAll`; tick logged failed (`line 58`).

### F4 (D4) — Easybox AWB requests carry null recipient name/phone → permanent give-up
`Services/Sameday/OrderToAwbRequestMapper.cs:60` · conv 1 (input-validation) · **confirmed** (trace)

- **Scenario.** Customer picks Easybox: `delivery-step.ts:362` sets `shippingAddress` only for Courier,
  so the order POSTs `shippingAddress=null`. `OrderService:142` substitutes `new
  ShippingAddressSnapshot()` with all-null fields, so the mapper's `?? throw` guard (which checks the
  object, not its fields) never fires. The AWB goes out with `name=null, phoneNumber=null` → Sameday
  4xx → `GiveUp`; every paid Easybox order silently gets no label.
- **Fix.** Reject empty `RecipientName`/`Phone` in `EasyboxRecipient` (throw `ArgumentException` →
  `GiveUp`) instead of forwarding nulls, and capture + validate recipient name/phone at checkout for
  Easybox (frontend + server).
- **Evidence.** `CreateOrderRequestValidator:14-18` requires only `EasyboxLockerId` for Easybox, so
  null passes. `OrderService.cs:142` → non-null empty snapshot. `Mapper:60` `?? guard` sees the
  non-null snapshot, passes, returns `addr.RecipientName=null, addr.Phone=null`.

### F5 (D5) — Easybox locker `SamedayId` dropped; wire `Service` hardcoded to 7
`Services/Sameday/OrderToAwbRequestMapper.cs:66` + `Services/Sameday/SamedayWireDtos.cs:35` · conv 1 (completeness-critic) · **confirmed** (trace + reviewer read)

- **Scenario.** `EasyboxRecipient` maps only the locker's street `Address/City/County` and drops
  `EasyboxLocker.SamedayId`; `CreateAwbAsync` never sets `LockerLastMile` and leaves `Service` at its
  DTO default `7`. Easybox AWBs reach Sameday with no locker OOH code (unroutable to the chosen
  locker); Courier orders also ship with the locker service code `7`.
- **Fix.** Add locker `SamedayId` + `DeliveryType` to `AwbCreationRequest`; set `lockerLastMile` and
  the correct `service` per type; assert both on the wire for Easybox and Courier.
- **Evidence.** `SamedayWireDtos.cs:35` `Service {get;set;} = 7` and `:42` `LockerLastMile` never set
  by `CreateAwbAsync` (`SamedayClient.cs:97-117`). Mapper `EasyboxRecipient` (63-69) omits `SamedayId`.
  Mapper tests only assert address fields.

---

## 🟠 Medium

### F6 (D6) — Webhook→AWB enqueue wiring has no test
`Controllers/WebhooksController.cs:192` · conv 1 (tests-coverage) · **confirmed** (trace) · *test-integrity (lens rated High; downgraded — the wiring is present and correct, the risk is undetected regression)*

- **Scenario.** Delete the `_awbNotifier.NotifyPaidAsync` calls (lines 192/237) or fire them on the
  wrong transition: all 862 tests stay green. `PaymentFactory` registers a `RecordingPhotoPromoter`
  but **no** recording `IAwbCreationNotifier`; default `NullAwbCreationNotifier` no-ops. Paid orders
  silently enqueue no AWB.
- **Fix.** Add a `RecordingAwbCreationNotifier` double in `PaymentFactory` and a
  `StripeWebhook_PaymentSucceeded_EnqueuesAwb` test mirroring the existing `…EnqueuesPhotoPromotion`
  (`PaymentControllerIntegrationTests.cs:418`).

### F7 (D7) — ADR-016 CAS race-lost test never reaches the CAS
`Tests/Unit/Services/Sameday/ShipmentTrackingJobTests.cs:136` · conv 1 (tests-coverage) · **confirmed** (trace) · *test-integrity (lens rated High; downgraded — the CAS code is correct, only its test is vacuous)*

- **Scenario.** `ADR_016_CAS_race_lost` seeds `Status=Cancelled`, so the in-window query
  (`ShipmentTrackingJob.cs:79` `WHERE Status==Shipped`) filters the order out — `GetTracking`,
  `ExecuteUpdate`, and the `affected==0` branch never run. Removing `&& o.Status==Shipped` from the CAS
  (`line 151`) keeps the test green while re-flipping advanced orders to Delivered.
- **Fix.** Seed a **Shipped** order; have `GetTrackingAsync`'s callback advance the row via a separate
  scope before returning Delivered, then assert `affected==0` (status unchanged, email NOT fired).

### F8 (D8) — `AwbDispatcher` backoff off-by-one: last entry unreachable
`BackgroundJobs/AwbDispatcher.cs:124` · conv 3 (correctness, tests-coverage, completeness-critic) · **confirmed** (3-lens agreement)

- **Scenario.** Guard `job.Attempt >= backoffs.Length` (default length 5) lets only attempts 1–4
  re-enqueue (indices 0–3). Attempt 5 is treated as exhausted, so `backoffs[4]` (3600s) is never
  applied — 4 in-process retries instead of 5, handing off to the hourly retry job earlier than
  configured.
- **Fix.** Change the guard to `job.Attempt > backoffs.Length` (or index by `Attempt`, not
  `Attempt-1`) so all configured entries are exercised; add a test over the full backoff schedule.

### F9 (D9) — Rate limiter re-created inside the per-execution delegate → inert + timer leak
`Services/Sameday/SamedayPolicies.cs:44` · conv 2 (quality, completeness-critic) · **confirmed** (trace)

- **Scenario.** Polly invokes the `RateLimiter` delegate per request; it `new`s a fresh
  `SlidingWindowRateLimiter` each call, so every Sameday call sees a full empty window (no throttling),
  and each abandoned limiter's auto-replenish `Timer` is never disposed.
- **Fix.** Create one `SlidingWindowRateLimiter` outside the delegate (captured in `BuildPipeline`);
  the delegate should only call `AcquireAsync` on that shared instance.
- **Evidence.** 100 requests each call `_pipeline.ExecuteAsync` → `new SlidingWindowRateLimiter`
  (PermitLimit 5, fresh window) → `AcquireAsync(1)` always succeeds instantly; `AutoReplenishment=true`
  default leaks a timer per call.

### F10 (D10) — Admin `→Shipped` overwrites machine-created `AwbNumber` with null
`Services/AdminOrderService.cs:117` · conv 1 (correctness) · **confirmed** (trace)

- **Scenario.** Bolt 037 auto-populates `order.AwbNumber` while the order is Paid. Admin later marks it
  Shipped; `UpdateStatusAsync` unconditionally sets `order.AwbNumber = request.AwbNumber`
  (`UpdateOrderStatusRequest.AwbNumber` is optional, no `[Required]`). If the admin form omits the AWB,
  the auto-created one is nulled → the tracking job's `AwbNumber!=null` filter excludes the order and
  the label reference is lost.
- **Fix.** Only overwrite `AwbNumber`/`TrackingUrl` when the admin supplies a non-empty value;
  otherwise preserve the existing machine-created values.

### F11 (D11) — AWB enqueue wired into webhooks only, not the transition chokepoint
`Services/AdminOrderService.cs:113` · conv 1 (requirements) · **confirmed** (trace)

- **Scenario.** Both design docs enqueue AWB on the `Paid` transition (the `OrderStatusMachine` hook).
  The implementation enqueues only in the two payment webhooks. An admin marking
  `AwaitingPayment→Paid` (offline / bank-transfer reconciliation) never enqueues an AWB and, because
  that path leaves `PaidAt` null, `AwbRetryJob`'s `PaidAt != null` filter also excludes it — no AWB, no
  give-up log, silently.
- **Fix.** Enqueue from a single transition chokepoint (or also call the notifier + set `PaidAt` in
  `AdminOrderService`'s Paid path); ensure the retry sweep can find admin-Paid orders.

### F12 (D12) — AWB persisted onto an order cancelled during the Sameday call
`Services/Sameday/AwbCreator.cs:93` · conv 1 (race) · **confirmed** (trace)

- **Scenario.** Order is Paid; the job loads it and passes the re-check, then `CreateAwbAsync` runs for
  several seconds. An admin cancels the order in that window. `SaveChanges` writes
  `AwbNumber`/`AwbLabelUrl` onto the now-Cancelled order (EF updates only those columns, leaving
  `Status=Cancelled`); a real parcel now exists at Sameday for a cancelled order, no compensation.
- **Fix.** Persist via conditional `ExecuteUpdate WHERE Id==orderId AND Status==Paid AND AwbNumber IS
  NULL`; on 0 rows affected treat as skipped and consider voiding the just-created AWB.

### F13 (D13) — Courier recipient name/phone/street/number unvalidated → AWB give-up
`Validators/Payments/CreateOrderRequestValidator.cs:27` · conv 1 (input-validation) · **confirmed** (trace)

- **Scenario.** The validator requires only `City/County/PostalCode` `NotEmpty` for Courier. A courier
  order with blank/whitespace (or absurdly long) `RecipientName/Phone/Street/Number` passes validation
  and payment; the AWB then carries blank/oversized fields → Sameday 4xx → `GiveUp`; the paid order
  never gets a label.
- **Fix.** Add `NotEmpty` + `MaximumLength` (and a phone-format check) for those `ShippingAddress`
  fields, matching Sameday's mandatory-field and length limits.

### F14 (D14) — `SamedayUnreachableException` swallowed with no log — tracking stalls silently
`BackgroundJobs/ShipmentTrackingJob.cs:128` · conv 1 (observability) · **confirmed** (trace)

- **Scenario.** Sameday tracking is down for hours. Every tick, every Shipped order hits `catch
  (SamedayUnreachableException) { return; }` — zero log output. The only related log (polling-stopped)
  fires after `TrackingMaxAgeDays` (30d). Other Sameday faults log at Warning, so the commonest one is
  the only silent one.
- **Fix.** Log the unreachable case (Debug/Information minimum, or a rate-limited Warning) with
  order_id + endpoint before returning, so a sustained outage is visible.

### F15 (D15) — Created AWB number not logged before `SaveChanges` — failed save orphans AWB invisibly
`Services/Sameday/AwbCreator.cs:96` · conv 1 (observability) · **confirmed** (trace)

- **Scenario.** `CreateAwbAsync` succeeds (billable AWB created), then `SaveChangesAsync` throws
  (transient DB drop). The exception bubbles to `AwbDispatcher`'s catch-all, which logs only
  order_id + attempt — never `result.AwbNumber`. The orphaned AWB is nowhere in logs; the F1/F2
  re-check still sees `AwbNumber` null, so the retry creates a duplicate billable AWB.
- **Fix.** Log the returned `AwbNumber` immediately after `CreateAwbAsync`, before the DB write.

### F16 (D16) — `AwbCreator` persistence test passes even if `SaveChangesAsync` removed
`Tests/Unit/Services/Sameday/AwbCreatorTests.cs:141` · conv 1 (tests-coverage) · **confirmed** (trace) · *test-integrity*

- **Scenario.** The happy-path test reads back via `db.Orders.FindAsync` on the **same** `DbContext`
  the creator used; `FindAsync` returns the tracked entity from the identity map (already mutated).
  Deleting `await _db.SaveChangesAsync(ct)` leaves the test green; prod never persists.
- **Fix.** Assert through a fresh `PhotoPrintDbContext` over the same InMemory database name (new
  scope), so a missing `SaveChanges` reddens the test.

### F17 (D17) — Admin `ShippedAt`/`DeliveredAt` assignment untested
`Services/AdminOrderService.cs:119` · conv 1 (tests-coverage) · **confirmed** (trace) · *test-integrity*

- **Scenario.** `AdminOrderServiceTests` exercise `Printing→Shipped` and `Shipped→Delivered` but assert
  only purge/email/broadcast — none assert `ShippedAt`/`DeliveredAt`. Removing `order.ShippedAt=…`
  stays green, yet the tracking job requires `ShippedAt != null`, so admin-shipped orders would never
  be delivery-polled.
- **Fix.** Assert `ShippedAt` is set on `→Shipped` and `DeliveredAt` on `→Delivered` (and that a manual
  `Delivered` with an existing `DeliveredAt` is preserved).

### F18 (D18) — Clearing the city search can permanently kill the locker-search pipe
`UI/src/app/features/checkout/pages/delivery-step.ts:332` · conv 1 (frontend-ux) · **confirmed** (trace)

- **Scenario.** User types "Cluj", then clears the box. The empty branch calls `getLockers('')`; if
  that 500s or network-drops (status 0), the `switchMap` inner errors, propagates to the
  single-arg `.subscribe` (no error handler), and terminates `valueChanges`. Every later keystroke is
  ignored — search is dead until a full page reload.
- **Fix.** Wrap the inner `getLockers` in `catchError(() => of([]))` inside `switchMap` so an error
  can't tear down the outer subscription.

### F19 (D19) — Init priming `getLockers('')` races the city-search `switchMap`
`UI/src/app/features/checkout/pages/delivery-step.ts:317` · conv 1 (frontend-ux) · **confirmed** (trace)

- **Scenario.** Priming (largest payload, slowest) is a separate subscription dispatched at init. A
  fast typer's debounced `getLockers('Cluj')` is dispatched later but returns first, setting the
  filtered list; the slower prime `''` response then lands and overwrites `lockers()` with the full
  list — the user sees all pins despite having typed a city.
- **Fix.** Feed priming through the same stream (e.g. `startWith('')` on `valueChanges`) so `switchMap`
  owns and cancels the initial `''` fetch. (Fixing F18 the same way addresses both.)

---

## 🟡 Low (ledger `backlog`)

### F20 (D20) — `MaxConcurrentSamedayCalls` overloaded as concurrency gate AND req/s rate limit
`Services/Sameday/SamedayResilienceHandler.cs:25` · conv 2 (requirements, quality) · **confirmed** (trace)
One knob feeds both the `SemaphoreSlim` job cap and the Polly sliding-window permit. Raising it to,
say, 20 for throughput also sets the outbound rate to 20 req/s — above Sameday's ~10 req/s ceiling the
036 model cites; the validator permits up to 50. **Fix:** separate the two settings (or cap the derived
rate at the vendor ceiling) and tighten the validator bound.

### F21 (D21) — Raw vendor error body embedded in exception and logged at Error (PII)
`Services/Sameday/SamedayClient.cs:140` · conv 1 (security) · **confirmed** (trace)
On a 4xx, `SafeReadAsync` reads the full vendor body into `SamedayValidationException.Message`;
`AwbCreator` returns it as `GiveUp`; `AwbDispatcher` logs `reason={Reason}` at Error. Shipping
validation errors commonly echo recipient name/phone/address → PII in logs, despite the taxonomy's
"never the request body" promise. **Fix:** truncate/redact the body before it enters the message, or
log only endpoint+status.

### F22 (D22) — `AwbLabelUrl` created as unbounded Postgres `text`, not `varchar(500)`  *(hinted)*
`Migrations/20260602141429_AddSamedayOrderFields.cs:23` · conv 1 (db-parity) · **confirmed** (trace)
`AddColumn<string>(type:"text", maxLength:500)` — Npgsql ignores `maxLength` when an explicit `type` is
set, so prod gets `text`, not `character varying(500)`. The model (`DbContext:310 .HasMaxLength(500)`)
declares `varchar(500)` → the deployed column diverges (cap unenforced) and a fresh `migrations add`
scaffolds a spurious `AlterColumn`. **Fix:** use the provider-aware pattern from
`AddOrderIdempotencyKey`/`AddUploadThumbnailPath` (`type: isNpgsql ? "character varying(500)" :
"TEXT"`) and correct the migration comment's false "capped to 500 chars" claim.

### F23 (D23) — Dual-DB parity: migrations + `timestamptz` CAS never run on Postgres  *(hinted)*
`Tests/Unit/Services/Sameday/OrderSamedayFieldsTests.cs:21` · conv 1 (tests-coverage) · **confirmed** (trace)
The suite round-trips only `AwbLabelUrl`/`LastTrackingSyncAt` on InMemory; `ShippedAt`/`DeliveredAt`
get no round-trip even there. Migration DDL, the `timestamptz` columns, and `ExecuteUpdate` CAS are
never exercised on Postgres. Concrete risk: Sameday can return `ObservedAt` with a `+03:00` offset;
Npgsql **rejects** nonzero-offset `DateTimeOffset` writes to `timestamptz`, while SQLite/InMemory
silently accept — so this throws only in prod. **Fix:** extend the round-trip test and add a
Testcontainers Postgres migrate+CAS smoke test, or document the accepted gap.

### F24 (D24) — Tracking `observedAt` fabricated to `UtcNow` when the vendor omits timestamps
`Services/Sameday/SamedayClient.cs:224` · conv 1 (input-validation) · **confirmed** (trace)
A `delivered` status with null `deliveredAt`/`observedAt` defaults `observedAt = UtcNow`, so
`DeliveredAt` is persisted with the poll time, not the real delivery time. (The delivered email carries
no timestamp, so the customer-facing text is unaffected — the DB value is wrong.) **Fix:** when state ==
Delivered, require a real timestamp; treat delivered-without-timestamp as `SamedayProtocolException` or
skip the write.

### F25 (D25) — `expire_at_utc` bound to `DateTimeOffset` without a UTC guarantee
`Services/Sameday/SamedayClient.cs:90` · conv 1 (input-validation) · **confirmed** (empirical trace)
If Sameday emits `expire_at_utc` without a `Z`/offset, `System.Text.Json` attaches the host's local
offset. On a non-UTC host the token validity window shifts → premature re-auth or use past real expiry
→ extra 401 round-trips. **Fix:** parse the expiry as UTC explicitly (`AssumeUniversal`/
`AdjustToUniversal`, or reject offset-less values). Confidence 4 (depends on vendor emitting offset-less
strings).

### F26 (D26) — Monotonic guard can silently drop a legitimate `Delivered` transition
`BackgroundJobs/ShipmentTrackingJob.cs:132` · conv 1 (tests-coverage) · **confirmed** (trace)
The `snapshot.ObservedAt < prev` guard runs before the Delivered check. If a prior in-transit poll set
`LastTrackingSyncAt` via the `UtcNow` fallback (F24) to a time later than the real delivered timestamp,
a subsequent Delivered snapshot is skipped → order stuck `Shipped` until the 30-day stop. Only the
InTransit-backwards case is tested. **Fix:** add a test with a Delivered snapshot `ObservedAt` earlier
than the stored sync; decide whether delivery must still transition, then pin it.

### F27 (D27) — Non-delivered tracking write touches `LastTrackingSyncAt` unconditionally
`BackgroundJobs/ShipmentTrackingJob.cs:182` · conv 1 (race) · **plausible** (one leg refuted)
The non-delivered `ExecuteUpdate` uses `WHERE Id` only (no status/monotonic predicate), so a
late-committing replica can push `LastTrackingSyncAt` backward — the raw overwrite is real. **But** the
claimed consequence (earlier re-poll) is **refuted**: `inWindow` also requires `Status==Shipped`, and a
delivered row has already left the poll set, so it is never re-polled regardless. Kept as Low for the
missing DB-level monotonic guard. **Fix (if pursued):** add `WHERE Status==Shipped AND
(LastTrackingSyncAt IS NULL OR LastTrackingSyncAt < newValue)`.

### F28 (D28) — AWB-enqueue logged at Debug — below the Information floor, never emits
`Services/Sameday/AwbCreationNotifier.cs:32` · conv 1 (observability) · **confirmed** (trace)
`LogDebug("sameday.awb.enqueued …")` with Serilog `MinimumLevel.Default = Information` in both
appsettings → the workflow entry point is invisible everywhere. **Fix:** raise to `LogInformation`, or
lower the Sameday `SourceContext` floor to Debug.

### F29 (D29) — Polly retry has no `OnRetry` callback — transient retries invisible
`Services/Sameday/SamedayPolicies.cs` (retry strategy) · conv 1 (observability) · **confirmed** (trace)
`RetryStrategyOptions` sets `MaxRetryAttempts=3` (1/4/16s) with no `OnRetry`, so a degraded-but-
recovering vendor leaves no trace and per-call latency silently balloons to ~21s; only full exhaustion
surfaces (and in the tracking path that's swallowed, F14). **Fix:** add an `OnRetry` delegate logging
attempt, delay, and status/exception at Information/Warning.

### F30 (D30) — Documented `/health` `sameday:enabled` surface not delivered
`HealthChecks/HealthCheckResponseWriter.cs:36` · conv 1 (requirements) · **confirmed** (trace)
The 036 tech-design states `/health` adds a `"sameday":"enabled"` field when the flag is on (its only
externally-visible surface change); neither the writer nor `AddHealthChecks` has any Sameday awareness.
**Fix:** add the passive flag to the health payload when `Sameday:Enabled=true`, or delete the claim
from the design doc.

### F31 (D31) — `GenerateAwbAsync` still returns "generate manually" with a stale pre-037 comment
`Services/SamedayShippingService.cs:52` · conv 1 (requirements) · **confirmed** (trace)
With `Sameday:Enabled=true`, `POST /shipping/awb` returns `AwbResultDto{Manual:true, "…integrarea
automată este în curs de finalizare"}` even though bolt 037's workflow is on the branch; the message and
the `// before bolt 037 lands the workflow` comment both contradict the delivered feature. **Fix:**
update the comment/message to reflect event-driven AWB creation, or make the endpoint report the real
workflow state.

### F32 (D32) — `AwbCreationRequest` documented as a validated value object but has none
`Services/Sameday/AwbCreationRequest.cs:11` · conv 1 (requirements) · **confirmed** (trace)
The 037 domain model says construction validates recipient fields non-empty, `ParcelWeightKg>0`,
`ParcelCount>=1`; it is a plain record and the mapper only null-guards the address object. Invalid
fields reach the wire and return `SamedayValidationException→GiveUp` instead of a local
`ArgumentException` as documented (overlaps F4/F13). **Fix:** validate in the mapper/`ParcelWeight`, or
amend the domain model to say validation happens at the wire.

### F33 (D33) — Tracking job re-queries the already-loaded order; `inWindow` tracked-but-unused
`BackgroundJobs/ShipmentTrackingJob.cs:172` · conv 1 (quality) · **confirmed** (trace)
Each delivered order triggers a second `Include(User, EasyboxLocker)` query though `inWindow` already
loaded those nav props; and `inWindow` is loaded tracked while every write goes through
`ExecuteUpdate`, so per-tick change-tracking is pure overhead. **Fix:** `AsNoTracking()` on `inWindow`;
after the CAS, set the fields in memory on the already-loaded order and pass it to the email service.

### F34 (D34) — Production resilience pipeline (rate limiter active) never exercised
`Tests/Unit/Services/Sameday/SamedayPoliciesTests.cs:40` · conv 1 (tests-coverage) · **confirmed** (trace)
Every test sets `MaxConcurrentSamedayCalls=int.MaxValue`, the sentinel that skips the
`SlidingWindowRateLimiter`. Production always builds **with** the limiter (default 5, outermost,
wrapping the retries). A limiter misconfig or a raw `RateLimiterRejectedException` would ship untested.
Includes the F42 POST-path gap. **Fix:** add a test with a small `PermitLimit` asserting the handler
throttles bursts and never surfaces a raw `RateLimiterRejectedException`, over a POST with `JsonContent`.

### F35 (D35) — Locker list fetched on every init even for Courier-only users
`UI/src/app/features/checkout/pages/delivery-step.ts:317` · conv 1 (frontend-ux) · **confirmed** (trace)
`getLockers('')` fires unconditionally in `ngOnInit`; a user who selects "Livrare la ușă" never sees
the map yet still pays the request, and a 5xx shows a misleading "Eroare de server" toast for data they
never use. **Fix:** defer the priming fetch until Easybox is selected (guarded in `selectMethod`, or
gate `ngOnInit` on `deliveryMethod()==='Easybox'`).

---

## ⚪ Cleanup (ledger `backlog`)

### F36 (D36) — `TrackingPollOutcome` is dead code
`Services/Sameday/TrackingPollOutcome.cs:15` · conv 1 (requirements) · unverified-cleanup
The domain model/tech design list it as the per-tick return type, but `PollOneAsync` returns void and
nothing constructs any case (incl. an undesigned `RaceLost`). **Fix:** return it from `PollOneAsync`
(and assert on it), or delete it and update the design docs.

### F37 (D37) — `LogRedactor` defined but never referenced
`Services/Sameday/LogRedactor.cs:13` · conv 1 (observability) · unverified-cleanup
The intended redaction chokepoint for outbound request/response tracing was never wired, so the HTTP
transport boundary emits no request/response logs at all. **Fix:** wire it into a trace log in
`SamedayClient`/handlers, or delete it as dead code.

### F38 (D38) — `TrackingStopRegistry` is a copy of `AwbGiveUpRegistry`
`Services/Sameday/TrackingStopRegistry.cs:9` · conv 1 (quality) · unverified-cleanup
Identical except the cache-key prefix and `EntryLifetime`; the one-shot dedup logic can drift. **Fix:**
extract one `OneShotRegistry(keyPrefix, lifetime)` and register two configured instances.

### F39 (D39) — Hand-constructs `StaticShippingService` instead of injecting it
`Services/SamedayShippingService.cs:35` · conv 1 (quality) · unverified-cleanup
Takes `PhotoPrintDbContext` + `IConfiguration` only to `new StaticShippingService(db, config)`;
duplicates its wiring outside DI and breaks at the call site if that service gains a dependency.
**Fix:** register `StaticShippingService` in DI and inject it.

### F40 (D40) — New migration designer snapshots embed stale `StripeClientSecret` length  *(hinted)*
`Migrations/20260602190046_…Designer.cs:365` · conv 1 (db-parity) · unverified-cleanup
Both new `.Designer.cs` files record `StripeClientSecret HasMaxLength(255)` while the master snapshot
and live model use 512 (a rebase artifact). No runtime impact (EF diffs the master snapshot), but it
signals the migrations were scaffolded from a stale model. **Fix:** re-scaffold, or hand-align the
designer to 512.

### F41 (D41) — Per-print gram weight is a bare literal `50` colliding with `MinimumGrams`
`Services/Sameday/ParcelWeight.cs:35` · conv 1 (quality) · unverified-cleanup
`totalPrints * 50 + MinimumGrams` — the unnamed `50` (per-print grams) sits next to the named `+50`
floor; a reader can't tell them apart. **Fix:** add a named `GramsPerPrint = 50` const.

---

## Refuted (recorded, dropped from the review)

### F42 (D42) — "5xx retry unsafe for POST bodies"
`Services/Sameday/SamedayResilienceHandler.cs:33` · conv 1 (completeness-critic) · **refuted**
Claim: retrying `POST /api/awb` re-executes `base.SendAsync` with the same `HttpRequestMessage`, so a
transient 5xx could become a hard failure if the content can't resend. **Refuted:** `SamedayClient.cs:122`
builds the body with `Content = JsonContent.Create(body)`, which re-serializes the retained POCO on
every `SerializeToStreamAsync` — there is no one-shot stream to exhaust (unlike `StreamContent`). The
skeptic reproduced the exact shape standalone: 3× replay, identical body, no exception. The only real
residue is a missing POST-path resilience test → folded into **F34**.
