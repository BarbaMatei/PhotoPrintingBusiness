---
stage: implement
bolt: 037-awb-and-tracking-jobs
created: 2026-06-02T19:00:00Z
---

# Stage 4 — Implementation Walkthrough: AWB & Tracking Jobs

## Summary

Bolt 037 wires the complete AWB + tracking lifecycle. With
`Sameday:Enabled = false` (shipped default), zero new services
register and runtime is byte-identical to pre-bolt-036. With
`Sameday:Enabled = true` but `Sameday:Jobs:Enabled = false`,
credentials are validated through the typed client but no lifecycle
automation runs (the two-stage rollout posture). With both flags
on, the three background services are live.

Solution build: **green** (`dotnet build PhotoPrint.sln` → 0 errors,
5 warnings, all pre-existing). Sameday unit tests: **63/63 passing**
(3 bolt-036 stubs rewritten as happy-path smoke tests for the
now-implemented methods).

---

## Files Created

### Domain value objects (`src/PhotoPrint.API/Services/Sameday/`)

- `AwbJob.cs` — channel payload record `(OrderId, Attempt, EnqueuedAt)`.
- `AwbCreationOutcome.cs` — discriminated union: `Created`,
  `Skipped`, `RetryLater(IsTransient)`, `GiveUp`.
- `TrackingPollOutcome.cs` — discriminated union: `NoChange`,
  `Delivered`, `RaceLost`, `PollingStopped`, `Failed(IsTransient)`.
- `ParcelWeight.cs` — value object encapsulating
  `grams = totalPrints * 50 + 50`. `FromOrder(order)` throws
  `ArgumentException` on empty items; the AWB creator surfaces this
  as `GiveUp("invalid request")`.

### Lifecycle services (`src/PhotoPrint.API/Services/Sameday/`)

- `IAwbJobQueue.cs` + `AwbJobQueue.cs` — singleton wrapping
  `Channel<AwbJob>.CreateUnbounded(SingleReader=true)`.
- `IAwbCreator.cs` + `AwbCreator.cs` — scoped per-order workflow:
  load order, ADR-015 re-check, map to request, call Sameday, persist.
- `OrderToAwbRequestMapper.cs` — static; recipient resolution
  (Easybox locker vs. courier shipping-address snapshot) + parcel
  weight.
- `IAwbCreationNotifier.cs` — interface called by the webhook
  handlers.
- `NullAwbCreationNotifier.cs` — default no-op; registered when
  jobs are off.
- `AwbCreationNotifier.cs` — real impl; enqueues to the channel.
  Registered only when `Sameday:Jobs:Enabled = true`.
- `AwbGiveUpRegistry.cs` + `TrackingStopRegistry.cs` —
  `IMemoryCache`-backed once-per-process dedup for the two
  give-up log lines.

### Background services (`src/PhotoPrint.API/BackgroundJobs/`)

- `AwbDispatcher.cs` — drains the channel, runs jobs through a
  `SemaphoreSlim(5)` gate, re-enqueues transient failures with the
  configured exponential backoff (default 30s/120s/300s/900s/3600s).
- `AwbRetryJob.cs` — `PeriodicTimer(60min)`. Re-enqueues orders
  inside the 24-h give-up window; logs `sameday.awb.give-up` once
  per order id for those outside. Runs once at startup so the
  recovery sweep doesn't wait for the first tick.
- `ShipmentTrackingJob.cs` — `PeriodicTimer(15min)`. Polls
  `Shipped` orders, uses `ExecuteUpdateAsync` for the ADR-016 CAS
  transition, enqueues the existing
  `IOrderEmailService.FireOrderDeliveredEmail` on success. Logs
  `sameday.tracking.race-lost` at Info when CAS affects 0 rows.

### Migration

- `Migrations/20260602190046_AddOrderShippedAtAndDeliveredAt.cs` —
  scaffolded against PostgreSQL, hand-edited to Postgres
  `timestamp with time zone`. Same pattern as the previous bolt's
  migrations.
- `Migrations/20260602190046_AddOrderShippedAtAndDeliveredAt.Designer.cs`
  + updated `PhotoPrintDbContextModelSnapshot.cs` — generated.

---

## Files Modified

- `Configuration/SamedaySettings.cs` — added nested
  `SamedayJobsSettings` (7 properties + sensible defaults).
- `Validators/SamedaySettingsValidator.cs` — guards every `Jobs`
  field with `if (options.Jobs.Enabled)`.
- `Services/Sameday/SamedayWireDtos.cs` — added `AwbCreateRequest`,
  `AwbRecipient`, `AwbParcel`, `AwbCreateResponse`,
  `TrackingResponse`, `TrackingHistoryEntry`.
- `Services/Sameday/SamedayClient.cs` — implemented
  `CreateAwbAsync`, `GetLabelPdfAsync`, `GetTrackingAsync` with the
  same error-mapping discipline as `AuthenticateAsync`.
  Vendor-status-code → `TrackingState` mapping lives in a private
  static `MapTrackingState` switch — the anti-corruption boundary.
- `Services/Sameday/SamedayPolicies.cs` — refactored into a
  parameterised `BuildPipeline(maxPerSecond)` plus the original
  `BuildRetryPipeline()` (kept as `BuildPipeline(int.MaxValue)`).
  The new rate-limit layer uses Polly v8's `SlidingWindowRateLimiter`
  from the `Polly.RateLimiting` package.
- `Services/Sameday/SamedayResilienceHandler.cs` — takes
  `IOptions<SamedaySettings>` and pulls `MaxConcurrentSamedayCalls`
  to size the rate limiter.
- `Models/Order.cs` — added `ShippedAt: DateTimeOffset?` and
  `DeliveredAt: DateTimeOffset?`.
- `Data/PhotoPrintDbContext.cs` — added EF mapping for both
  (existing PostgreSQL Unix-ms converter loop covers them automatically).
- `Services/AdminOrderService.cs` — sets `ShippedAt` when admin
  transitions to `Shipped`; sets `DeliveredAt` (if still null) when
  admin transitions manually to `Delivered`. Tracking job sets
  `DeliveredAt` automatically via the CAS UPDATE.
- `Controllers/WebhooksController.cs` — injected
  `IAwbCreationNotifier`; calls `NotifyPaidAsync(order.Id, ct)` in
  both Paid-transition branches (EuPlatesc + Stripe).
- `Program.cs` — registers `NullAwbCreationNotifier` always.
  Under `if (samedayEnabled)`, adds a second nested `if (jobsEnabled)`
  block that overrides with the real notifier and registers
  the queue, creator, registries, and three hosted services.
- `appsettings.json` — `Sameday.Jobs` block added (jobs disabled
  by default, schedules + concurrency + backoff array).
- `PhotoPrint.API.csproj` — `Polly.RateLimiting 8.5.0` package added.
- `src/PhotoPrint.Tests/Unit/Services/Sameday/SamedayPoliciesTests.cs` —
  updated to pass the new `IOptions<SamedaySettings>` ctor arg to
  `SamedayResilienceHandler`; uses the `int.MaxValue` sentinel to
  bypass the rate limiter for retry-only tests.
- `src/PhotoPrint.Tests/Unit/Services/Sameday/SamedayClientTests.cs` —
  three former `throws NotImplementedException` tests rewritten as
  happy-path smoke tests for the now-implemented methods (full
  coverage in the new Stage 5 test files).

---

## DI Wiring Detail

`Sameday:Enabled = false` (shipped default):

- `IAwbCreationNotifier → NullAwbCreationNotifier` registered.
- `IShippingService → StaticShippingService`.
- No Sameday transport, no jobs, no AWB queue. Webhook handler's
  `await _awbNotifier.NotifyPaidAsync(...)` is a `Task.CompletedTask`
  no-op.

`Sameday:Enabled = true` AND `Sameday:Jobs:Enabled = false`:

- Bolt-036 wiring (typed client, auth handler, resilience handler,
  token provider) is active.
- `IAwbCreationNotifier → NullAwbCreationNotifier` (still the
  Null impl — jobs are off).
- AWB queue and three hosted services NOT registered.
- `SamedayShippingService` is the registered `IShippingService`,
  but `GenerateAwbAsync` still returns the manual-fallback DTO
  (per the bolt-036 placeholder).

`Sameday:Enabled = true` AND `Sameday:Jobs:Enabled = true`:

- Everything above, plus:
- `IAwbJobQueue → AwbJobQueue` (singleton).
- `AwbGiveUpRegistry`, `TrackingStopRegistry` singletons.
- `IAwbCreator → AwbCreator` (scoped).
- `IAwbCreationNotifier → AwbCreationNotifier` (singleton — overrides
  the Null impl by being registered later).
- `AwbDispatcher`, `AwbRetryJob`, `ShipmentTrackingJob` hosted
  services start at boot.

---

## Notable Decisions Made During Implementation

1. **`ShippedAt` was not on `Order` and had to be added.** The
   bolt design referenced `Order.ShippedAt` as if it existed; on
   wiring, the compiler caught its absence. Added as a parallel to
   `PaidAt` and `DeliveredAt`; `AdminOrderService.UpdateStatusAsync`
   now sets it whenever an admin transitions an order to `Shipped`.
   No retroactive backfill — existing `Shipped` orders have a
   null `ShippedAt`, and the tracking job's `ShippedAt != null`
   filter excludes them. Acceptable: a single admin tick on each
   such order in the rare backfill case.

2. **`AdminOrderService` now stamps `DeliveredAt` on manual
   transitions to `Delivered`.** Without this, manual-delivered
   orders would have a `null` `DeliveredAt` — fine for the
   tracking job (it's idempotent), but bad for any future analytics
   that needs "when did this order actually get delivered." Cheap
   addition; mirrors the pattern used for `PaidAt` (set by the
   webhook) and `ShippedAt` (set by the admin path).

3. **`IAwbCreationNotifier` introduced as a thin webhook seam.**
   The bolt-design talked about an `OrderStatusMachine.AfterTransitionAsync`
   hook, but the existing state machine is static and has no
   instance for an async hook. Rather than refactor the state
   machine, introduced a small injectable notifier with one method
   and a `NullAwbCreationNotifier` default — same pattern bolt 051
   used for `IOrderPhotoPromoter`. Two call sites in the webhook,
   both right next to existing `_photoPromoter.EnqueueAsync`.

4. **Rate limiter is hard-applied in `SamedayResilienceHandler`,
   even when jobs are off.** The handler reads
   `Sameday:Jobs:MaxConcurrentSamedayCalls` regardless of
   `Jobs.Enabled`. With the bolt-036-only flow, the only outbound
   call is `AuthenticateAsync` (gated by the token provider's
   semaphore), so the rate limit is over-cautious but harmless.
   The alternative — conditionally building the pipeline — would
   complicate the handler's ctor with no operational benefit.

5. **`SamedayPolicies.BuildRetryPipeline()` preserved.** Bolt-036
   tests use it to exercise retry semantics without rate-limit
   interference (the sliding-window rate-limiter's permit
   acquisition adds nondeterministic timing). The `int.MaxValue`
   sentinel skips the rate-limit layer entirely.

6. **Email service receives a freshly-loaded order in
   `ShipmentTrackingJob`.** The CAS UPDATE doesn't update the
   change-tracked entity in-place, so a second `Include`-d read is
   needed to pass a fully-hydrated `Order` to
   `FireOrderDeliveredEmail` (it requires `User` + `EasyboxLocker`
   nav properties per its XML doc). One extra read per
   successful transition; acceptable given the throughput.

7. **`AwbRetryJob` runs an immediate startup tick.** Periodic timers
   delay the first tick by their interval. With a 60-min interval
   that's a 1-hour delay before the recovery sweep runs after host
   restart — too long for crash-safety semantics. The job calls
   `RunOneTickAsync` once before entering the timer loop.

---

## Build + Compile Verification

```text
dotnet build PhotoPrint.sln
  → Build succeeded.  0 Error(s), 5 Warning(s)
  (all pre-existing: NU1603 Stripe.net version, EF1002 in
   OrderNumberService, CS1998 in RazorTemplateServiceTests; plus one
   new harmless CS1998 in AwbDispatcher.ScheduleReEnqueueAsync —
   the inner Task.Run is fire-and-forget by design.)
```

EF migration design-time build succeeded; `dotnet ef migrations add
AddOrderShippedAtAndDeliveredAt` produced clean files.

Existing Sameday tests after the API surface change: **63/63 pass**.

---

## What Bolt 037 Does NOT Do

These items remain scoped out, as flagged in the unit-brief:

- **Outbound webhooks** (Sameday → us push notifications). We poll
  on a 15-min cadence; live tracking via vendor webhooks is a
  later intent.
- **AWB cancellation on refund.** Admin manual today; deferred to
  whichever intent owns the refund flow.
- **Admin notification on `AwbCreationGivenUp`.** Today the Error
  log is the audit trail; a future intent wires it into the admin
  notification surface.
- **Leader election / Redis lock per order ID.** Deferred to bolt
  046 per ADR-015.
- **CI-gated end-to-end Sameday sandbox test.** Same posture as
  bolt 036 — recorded fixtures only until sandbox credentials are
  provisioned in CI.

---

## ⛔ Human Checkpoint

Stage 4 (Implement) is complete and the solution builds. Please
review and approve before I move to Stage 5 (Test).

**Ready to proceed?**

- **1** — Approve and continue to Stage 5.
- **2** — Need changes (specify which file or behaviour).
