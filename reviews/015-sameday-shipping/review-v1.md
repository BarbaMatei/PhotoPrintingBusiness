---
type: review
target: 015-sameday-shipping
version: 1
supersedes: null
commit: 1765918
branch: feat/bolt-036-sameday-api-client
pass-type: discovery
date: 2026-07-27
reviewer: multi-lens (full 11-lens discovery)
lenses: [correctness, security, requirements, quality, tests-coverage, race, db-parity, input-validation, observability, frontend-ux, completeness-critic]
lenses-not-run: []
verdict: request-changes
blockers: [F1, F2, F3, F4, F5]
findings: { high: 5, medium: 14, low: 16, cleanup: 6, refuted: 1 }
tests: { dotnet: "862/862 (+10 skipped MinIO)", frontend: "448/448" }
cost: { agents: 48, tokens: 2583165, agents_by_stage: { lenses: 11, dedup: 1, skeptics_guard: 2, skeptics_trace: 34 } }
---

# Review v1 — 015-sameday-shipping (full discovery pass)

**Scope.** The Sameday courier integration (intent 015), both bolts on this branch (`main…HEAD`,
commit `1765918`): **036** — Sameday auth, in-process token cache, typed `SamedayClient` over
`HttpClient` (auth handler + Polly resilience handler), 5-type exception taxonomy, wire DTOs,
`SamedayShippingService` (`IShippingService`), 2 EF migrations adding `Order.AwbLabelUrl /
LastTrackingSyncAt / ShippedAt / DeliveredAt`; **037** — AWB creation enqueued off the `Paid`
transition (`AwbJobQueue` → `AwbDispatcher`), the `AwbRetryJob` safety net, and the
`ShipmentTrackingJob` `Shipped→Delivered` poller, plus the checkout `delivery-step` locker-priming
tweak.

**Pass type.** First review of this branch → **discovery** (blinded, whole-feature). All 11 manifest
lenses ran in one blinded parallel batch → in-pass dedup → convergence-weighted adversarial verify
([discovery-review.wf.js](../lib/discovery-review.wf.js)); the main agent synthesized. 53 raw findings
→ 42 canonical. **Full-loop tier** (new external courier API, 2 migrations, multi-replica concurrency
per ADR-015/016, touches paid orders), so this pass owes a certification pair before feature closure.

**Not live today.** `Sameday:Enabled=false` **and** `Sameday:Jobs:Enabled=false` in `appsettings.json`,
so the entire AWB/tracking path is dormant and `StaticShippingService` is the active courier.
**Every finding below is a gate on _enabling_ Sameday, not a live production incident** — there is
runway to fix before the flags flip. Severity reflects the behaviour when enabled (the code under
review is the code that will run).

**Verdict: `request-changes`.** Five confirmed **High** blockers (**F1–F5**). They fall into two root
clusters, both in the AWB-creation write path:

- **The multi-replica safety the ADRs promise is not actually enforced (F1, F2, F3).** ADR-015 says
  duplicate AWB creates are safe because Sameday dedups on our external reference — but the reference
  ([SamedayClient.cs:104](../../src/PhotoPrint.API/Services/Sameday/SamedayClient.cs#L104)) is wired to
  the shop-wide constant `PickupPointId`, **identical for every order**. Retries aren't deduped, and if
  Sameday *does* honour the key, order N collides with order 1 (cross-order AWB/label). There is also
  no DB-side guard on the AWB write (no unique index, no `WHERE AwbNumber IS NULL`), and the tracking
  job shares one `DbContext` across concurrent poll tasks, which faults the whole tick.
- **The AWB request is assembled from unvalidated / empty recipient data (F4, F5).** Every Easybox
  order forwards `null` recipient name/phone to Sameday (the mapper's null-guard is dead because
  `OrderService` substitutes a non-null empty `ShippingAddressSnapshot`), and the locker's `SamedayId`
  is dropped while the wire `Service` code is hardcoded to `7`. Result: every paid Easybox order fails
  closed into a permanent give-up or an unroutable label; courier AWBs get the locker service code.

**What's sound.** Secret/token handling is correct — credentials are redacted, the bearer token is
never logged, and `ADR-006` (no secrets in `appsettings`) holds. The exception taxonomy and outcome
model are well-layered; the `Shipped→Delivered` CAS itself (`WHERE Status==Shipped`) is written
correctly (its *test* is the problem, F7, not the code). Both suites are green (862 / 448).

> Full per-finding detail (scenario · evidence · fix) is in [findings-v1.md](findings-v1.md).
> Canonical cross-pass IDs are in [ledger.md](ledger.md) (v1: F# ↔ D# 1:1).
> The one-page owner view is [summary-v1.md](summary-v1.md).

## Build & tests (run by the reviewer at commit `1765918`)

- **.NET:** `862/862` passed, **10 skipped** — the MinIO `[SkippableFact]` S3 integration tests skip
  locally (no `STORAGE_TEST_*`). Duration ~1m10s.
- **Frontend (Vitest):** `448/448` passed (48 files).
- Green, but the tests-coverage lens found load-bearing blind spots: the webhook→AWB enqueue (F6),
  the ADR-016 CAS invariant (F7), admin `ShippedAt/DeliveredAt` (F17), the persistence `SaveChanges`
  (F16), and the production rate-limiter path (F34) are all effectively unproven — several tests stay
  green when the corresponding bug is injected. Tests run on EF **InMemory**, so migration DDL and the
  `timestamptz` CAS writes are never exercised against Postgres (F22, F23).

## Findings

Ranked most-severe first. Convergence = independent lenses that raised it (max 6 this pass, on F1).
Verdict from the adversarial skeptics: **confirmed** (trace built) · **plausible** (realistic, one leg
of the claim refuted) · **refuted** (dropped, recorded). `hinted` = topic seeded by the shared project
context, so its convergence is not independent evidence.

| ID | D# | Sev | Conv | Verdict | Finding | File |
|----|----|-----|------|---------|---------|------|
| **F1** | D1 | 🔴 High | 6 | confirmed | **AWB vendor idempotency key wired to the shop-wide constant `PickupPointId`, not per-order → retries never dedup; cross-order AWB if vendor honours the key. Breaks ADR-015.** *(BLOCKER)* | `Services/Sameday/SamedayClient.cs:104` |
| **F2** | D2 | 🔴 High | 2 | confirmed | **Concurrent AWB creators for one order double-create (check-then-act, no DB guard) → paid orphan parcel + double courier cost.** *(BLOCKER)* | `Services/Sameday/AwbCreator.cs:69` |
| **F3** | D3 | 🔴 High | 2 | confirmed | **One `DbContext` shared across concurrent tracking-poll tasks → EF "second operation" throw faults the tick; deliveries never recorded once ≥2 orders ship.** *(BLOCKER)* | `BackgroundJobs/ShipmentTrackingJob.cs:87` |
| **F4** | D4 | 🔴 High | 1 | confirmed | **Every Easybox AWB carries `null` recipient name/phone (dead null-guard) → Sameday 4xx → permanent give-up; no Easybox order ever gets a label.** *(BLOCKER)* | `Services/Sameday/OrderToAwbRequestMapper.cs:60` |
| **F5** | D5 | 🔴 High | 1 | confirmed | **Easybox locker `SamedayId` dropped + wire `Service` hardcoded to `7` → Easybox unroutable, courier orders get the locker service code.** *(BLOCKER)* | `Services/Sameday/OrderToAwbRequestMapper.cs:66` |
| F6 | D6 | 🟠 Med | 1 | confirmed | Webhook→AWB enqueue wiring untested — deleting `NotifyPaidAsync` keeps 862 green (no recording notifier double). | `Controllers/WebhooksController.cs:192` |
| F7 | D7 | 🟠 Med | 1 | confirmed | ADR-016 CAS "race-lost" test seeds `Cancelled`, so the in-window query excludes it and the CAS never runs — passes for the wrong reason. | `Tests/…/ShipmentTrackingJobTests.cs:136` |
| F8 | D8 | 🟠 Med | 3 | confirmed | `AwbDispatcher` backoff off-by-one: guard `Attempt >= Length` leaves the last `DispatchBackoffSeconds` entry unreachable (4 retries, not 5). | `BackgroundJobs/AwbDispatcher.cs:124` |
| F9 | D9 | 🟠 Med | 2 | confirmed | Rate limiter `new`-ed inside the per-execution delegate → every call sees an empty window (no throttling) and leaks an auto-replenish timer per call. | `Services/Sameday/SamedayPolicies.cs:44` |
| F10 | D10 | 🟠 Med | 1 | confirmed | Admin `→Shipped` unconditionally sets `AwbNumber=request.AwbNumber`; an omitted field nulls the machine-created AWB → tracking job's `AwbNumber!=null` filter drops it. | `Services/AdminOrderService.cs:117` |
| F11 | D11 | 🟠 Med | 1 | confirmed | AWB enqueue lives only in the two webhooks, not the transition hook; admin `AwaitingPayment→Paid` never enqueues **and** leaves `PaidAt` null so `AwbRetryJob` can't find it. | `Services/AdminOrderService.cs:113` |
| F12 | D12 | 🟠 Med | 1 | confirmed | AWB persisted onto an order cancelled during the Sameday call (no status re-check before `SaveChanges`) → real parcel for a cancelled order, no compensation. | `Services/Sameday/AwbCreator.cs:93` |
| F13 | D13 | 🟠 Med | 1 | confirmed | Courier `RecipientName/Phone/Street/Number` unvalidated at checkout → blank/oversized fields reach Sameday → 4xx → give-up; paid order gets no label. | `Validators/Payments/CreateOrderRequestValidator.cs:27` |
| F14 | D14 | 🟠 Med | 1 | confirmed | `SamedayUnreachableException` swallowed with zero log in the tracking poll — the commonest fault is the only silent one; a multi-hour outage is invisible. | `BackgroundJobs/ShipmentTrackingJob.cs:128` |
| F15 | D15 | 🟠 Med | 1 | confirmed | Created AWB number not logged before `SaveChanges`; a save failure orphans a billable AWB invisibly, and the retry re-creates a duplicate (compounds F2). | `Services/Sameday/AwbCreator.cs:96` |
| F16 | D16 | 🟠 Med | 1 | confirmed | `AwbCreator` happy-path test reads back through the same `DbContext` identity map → stays green even if `SaveChangesAsync` is deleted. | `Tests/…/AwbCreatorTests.cs:141` |
| F17 | D17 | 🟠 Med | 1 | confirmed | Admin `ShippedAt/DeliveredAt` assignment untested though the tracking job depends on `ShippedAt!=null`; deleting the assignment stays green. | `Services/AdminOrderService.cs:119` |
| F18 | D18 | 🟠 Med | 1 | confirmed | Clearing the city search routes through `getLockers('')`; a transient error there terminates `valueChanges` (no `catchError`, no error cb) — search dead until reload. | `UI/…/delivery-step.ts:332` |
| F19 | D19 | 🟠 Med | 1 | confirmed | Init priming `getLockers('')` is a rival subscription `switchMap` can't cancel; a slow prime lands after a fast city filter and overwrites it with the full list. | `UI/…/delivery-step.ts:317` |
| F20 | D20 | 🟡 Low | 2 | confirmed | `MaxConcurrentSamedayCalls` overloaded as both the job concurrency gate and the Polly req/s permit; raising it for throughput also lifts the outbound rate past Sameday's ~10 req/s ceiling. | `Services/Sameday/SamedayResilienceHandler.cs:25` |
| F21 | D21 | 🟡 Low | 1 | confirmed | Raw vendor 4xx body embedded verbatim in `SamedayValidationException.Message` and logged at Error → recipient PII in logs when the vendor echoes fields. | `Services/Sameday/SamedayClient.cs:140` |
| F22 | D22 | 🟡 Low | 1 | confirmed (hinted) | `AwbLabelUrl` migration hardcodes `type:"text"`, so Postgres gets unbounded `text` not `varchar(500)`; diverges from model `HasMaxLength(500)` → cap unenforced + phantom `AlterColumn`. | `Migrations/20260602141429_AddSamedayOrderFields.cs:23` |
| F23 | D23 | 🟡 Low | 1 | confirmed (hinted) | Dual-DB parity: migrations + `timestamptz` CAS never run on Postgres; a nonzero-offset `DateTimeOffset` write may throw on Npgsql, undetectable on InMemory/SQLite. | `Tests/…/OrderSamedayFieldsTests.cs:21` |
| F24 | D24 | 🟡 Low | 1 | confirmed | Tracking `observedAt` defaulted to `UtcNow` when the vendor omits timestamps → `DeliveredAt` persisted with a fabricated time (email carries no timestamp, so cosmetic). | `Services/Sameday/SamedayClient.cs:224` |
| F25 | D25 | 🟡 Low | 1 | confirmed | `expire_at_utc` bound to `DateTimeOffset` with no UTC guarantee; an offset-less vendor value on a non-UTC host shifts token expiry → extra 401 round-trips. | `Services/Sameday/SamedayClient.cs:90` |
| F26 | D26 | 🟡 Low | 1 | confirmed | Monotonic `ObservedAt` guard can drop a legitimate `Delivered` snapshot whose real timestamp precedes an earlier `UtcNow`-fallback sync → order stuck `Shipped` until the 30-day stop (untested). | `BackgroundJobs/ShipmentTrackingJob.cs:132` |
| F27 | D27 | 🟡 Low | 1 | plausible | Non-delivered tracking write updates `LastTrackingSyncAt` with `WHERE Id` only (no monotonic/status predicate); raw cross-replica overwrite is real, but the claimed early-repoll consequence is refuted (delivered rows leave the poll set). | `BackgroundJobs/ShipmentTrackingJob.cs:182` |
| F28 | D28 | 🟡 Low | 1 | confirmed | `sameday.awb.enqueued` logged at Debug, below the Information floor in both appsettings → the workflow entry point never emits. | `Services/Sameday/AwbCreationNotifier.cs:32` |
| F29 | D29 | 🟡 Low | 1 | confirmed | Polly retry has no `OnRetry` callback → 3× transient retries (1/4/16s) are invisible; a degraded vendor leaves no trace and latency balloons silently. | `Services/Sameday/SamedayPolicies.cs` (retry) |
| F30 | D30 | 🟡 Low | 1 | confirmed | The 036 tech-design's documented `/health` `sameday:enabled` field is not delivered — ops can't confirm the flag state via `/health`. | `HealthChecks/HealthCheckResponseWriter.cs:36` |
| F31 | D31 | 🟡 Low | 1 | confirmed | With `Sameday:Enabled=true`, `GenerateAwbAsync` still returns "generează manual" + a stale "before bolt 037 lands" comment, contradicting the shipped workflow. | `Services/SamedayShippingService.cs:52` |
| F32 | D32 | 🟡 Low | 1 | confirmed | `AwbCreationRequest` is documented as a validated value object but is a plain record with no validation; invalid fields reach the wire instead of failing locally. | `Services/Sameday/AwbCreationRequest.cs:11` |
| F33 | D33 | 🟡 Low | 1 | confirmed | Tracking job re-queries the order with `Include(User, EasyboxLocker)` after the CAS though `inWindow` already loaded it (and is tracked but only written via `ExecuteUpdate`). | `BackgroundJobs/ShipmentTrackingJob.cs:172` |
| F34 | D34 | 🟡 Low | 1 | confirmed | Every resilience test sets `MaxConcurrentSamedayCalls=int.MaxValue`, the sentinel that skips the rate limiter → the production limiter path is never exercised. | `Tests/…/SamedayPoliciesTests.cs:40` |
| F35 | D35 | 🟡 Low | 1 | confirmed | `getLockers('')` fires on every init regardless of delivery method → Courier-only users pay a wasted request and, on 5xx, see a misleading error toast for a map they never open. | `UI/…/delivery-step.ts:317` |
| F36 | D36 | ⚪ Cleanup | 1 | unverified-cleanup | `TrackingPollOutcome` discriminated union is declared but never constructed — documented artifact shipped unwired. | `Services/Sameday/TrackingPollOutcome.cs:15` |
| F37 | D37 | ⚪ Cleanup | 1 | unverified-cleanup | `LogRedactor` defined but never referenced → no HTTP transport request/response tracing exists. | `Services/Sameday/LogRedactor.cs:13` |
| F38 | D38 | ⚪ Cleanup | 1 | unverified-cleanup | `TrackingStopRegistry` is a near-copy of `AwbGiveUpRegistry`; the one-shot dedup logic can drift between the two. | `Services/Sameday/TrackingStopRegistry.cs:9` |
| F39 | D39 | ⚪ Cleanup | 1 | unverified-cleanup | `SamedayShippingService` hand-`new`s `StaticShippingService(db, config)` instead of injecting it — duplicates DI wiring. | `Services/SamedayShippingService.cs:35` |
| F40 | D40 | ⚪ Cleanup | 1 | unverified-cleanup (hinted) | Both new migration `.Designer.cs` snapshots record `StripeClientSecret` `HasMaxLength(255)` vs the master snapshot's 512 — stale-model scaffold artifact (no runtime impact). | `Migrations/20260602190046_…Designer.cs:365` |
| F41 | D41 | ⚪ Cleanup | 1 | unverified-cleanup | Per-print gram weight is a bare literal `50` sitting next to the named `MinimumGrams` (also 50) — unnamed magic number. | `Services/Sameday/ParcelWeight.cs:35` |
| F42 | D42 | — | 1 | **refuted** | *(dropped)* "5xx retry unsafe for POST bodies": `JsonContent` re-serializes the retained POCO each attempt, so replay reproduces the body — no defect. Only a POST-path test gap remains (folded into F34's class). | `Services/Sameday/SamedayResilienceHandler.cs:33` |

## Notes for the fixer

- **Fix F1 first** — F2 and F15 partly depend on it. The proper fix is a *per-order* vendor external
  reference (`OrderId`/`OrderNumber` in `AwbCreationRequest.ClientInternalReference`) **plus** a
  guarded DB write (`ExecuteUpdate … WHERE Id==id AND Status==Paid AND AwbNumber IS NULL`, or a unique
  index). In-process dedup cannot stop a multi-replica double-create.
- **F4/F5/F13/F32 are one recipient-mapping cluster** — decide whether to validate at checkout
  (frontend + `CreateOrderRequestValidator`) or in `OrderToAwbRequestMapper`/`ParcelWeight`, then fix
  consistently. F4's dead null-guard hinges on `OrderService` injecting a non-null empty snapshot.
- **F3** mirrors the `AwbDispatcher` pattern that's already correct — scope a `DbContext` per order
  inside `PollOneAsync`.
- Regression tests are required per finding and must fail when the fix is reverted; several findings
  (F6, F7, F16, F17, F34) are *themselves* about tests that pass for the wrong reason — fixing them
  means the new test must go red when its invariant is broken.
- 🟡/⚪ (F20–F41) enter the ledger as `backlog`; they don't gate the fix round unless a fix touches
  full-loop-tier code. F22/F23/F40 (dual-DB parity) are the ones worth grooming before enabling on
  Postgres.
