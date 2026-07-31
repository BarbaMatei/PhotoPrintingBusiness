---
type: review-ledger
target: 015-sameday-shipping
updated: 2026-07-29
closed: 2026-07-29 — owner sign-off @5734021 (no post-fix blinded pass; the pre-enable checklist is the gate)
---

<!-- v1 fix round (resolution-v1, commits edd49f7..835e932): D1–D19 + D32/D34 fixed.
v2 verification (review-v2, 2026-07-27 @ 727a018): all 21 verified, 0 reopened. D20–D41 backlog; D42 false-positive.
v3 certification pair (review-v3, 2026-07-27 @ 8584572): NOT certified — 3 High blockers (D43–D45)
+ mediums (D46–D54); D31 re-opened+elevated; D21/D23 re-raised (stand). → fix round.
v3 fix round → v4 verification (review-v4, @ 5fc330b): all held, 0 reopened.
v5 certification (review-v5, single-pass recorded deviation, @ 5fc330b): CERTIFIED — 0 High, 0
regression; 17 med / 19 low / 6 cleanup new (D55–D89) → backlog pre-enable checklist; 4 deferrals
re-affirmed (D50/D23/D29/D39); D45 vendor-idempotency residual re-confirmed (accepted).
v5 fix round (resolution-v5, 3764fa0..1816f5f): 41 fixed / 3 deferred / 2 wont-fix / 2 false-positive.
v6 verification (review-v6, 2026-07-29 @ 1816f5f): 37 verified, **4 reopened (D27/D39/D71/D79 — no
test can go red)**, 7 new (D90–D96), 0 regressions. Loop RE-ARMED.
v6 fix round (resolution-v6, 5734021): all 4 reopens + the D68/D78 gaps closed, test-only, each
revert-proven. **LOOP CLOSED 2026-07-29 by owner sign-off** — no post-fix blinded pass ran; the
pre-enable checklist is the binding gate (D45 vendor idempotency, D23 Postgres DDL, D81 service ids,
D90–D96 backlog). -->

## Loop closed — 2026-07-29 (owner sign-off, commit `5734021`)

The owner elected to close after the v6 fix round rather than run another certification. Recorded
here and in [index.md](../index.md) per [README note ²](../README.md). **What that leaves unproven,
stated plainly:**

- **No blinded lens has searched the fixed code.** v5 certified `5fc330b`; 47 fixes have landed since
  across the v5 and v6 rounds. v6 verified each fix individually but ran no search.
- The 7 findings v6 turned up incidentally (D90–D96) while *not* searching suggest a search would
  find more. The new-finding trend across full passes (41 → 12 → 42) never decayed.
- **D45** — no-double-billing still rests on Sameday's own dedup on `ClientInternalReference`,
  unconfirmed with the vendor. **D23** — migrations and the `timestamptz` CAS have never executed
  against Postgres, and the v5 round added a migration. **D81** — service ids are still placeholder `7`.

Closure is defensible only because the feature is dormant behind `Sameday:Enabled=false` +
`Sameday:Jobs:Enabled=false`: nothing here can bite in production. **The pre-enable checklist, not
this closure, is the gate.** Flipping either flag without working that list re-opens every risk above.

## v6 fix round — test-only (2026-07-29, commit `5734021`)

All 4 reopens closed, plus the two gaps v6 recorded on fixes that held. Detail:
[resolution-v6.md](resolution-v6.md).

| D# | Status | Test that now guards it |
|----|--------|-------------------------|
| D27 | verified | `Does_not_stamp_a_row_another_replica_already_moved_to_Delivered` · `Does_not_move_LastTrackingSyncAt_backwards` |
| D39 | verified | `Enabled_root_resolves_client_creator_and_jobs_without_a_DI_cycle` now resolves `IShippingService` |
| D71 | verified | `Retry_backoff_is_1_4_16_seconds_not_Polly_default_base_2` |
| D79 | verified | `GenerateAccessToken_CarriesTheDisplayNameClaim` + 3 `auth.service.spec` cases |
| D68 | verified | `Preserves_the_claim_when_the_persist_fails_after_the_vendor_created_the_AWB` (gap closed) |
| D78 | verified | `does not render a street-address line for an Easybox order` — rewritten; the old assertion could not fail (gap closed) |

Every one was proven to redden when its fix is reverted, with zero collateral (6 backend + 4 frontend
predicted failures, all matched). **Recorded deviation:** fixer == verifier for this round — the
evidence is a reproducible measurement, not a self-assessment; see resolution-v6. Expiry
resolved at the 2026-07-29 calibration: written into runbook-verification step 1 as the
test-only exemption.

Side effect worth keeping: `FakeTimeProvider` now fakes timers, so the D82 dispatcher test no longer
sleeps 30 real seconds. Backend went from 916 tests in 30 s to **921 in 4 s**.

## v6 — verification (2026-07-29, commit `1816f5f`, approve-with-followups)

Independent anchored verification of the v5 fix round. 19 backend + 6 frontend revert-and-rerun
runs, each with its failure set predicted in advance and matched exactly. Detail:
[review-v6.md](review-v6.md).

**Verified (37):** D55, D56, D57, D58, D59, D60, D61, D62, D63, D64, D65, D66, D67, D68, D69, D70,
D73, D74, D75, D76, D77, D78, D80, D82, D84, D85, D86, D87, D20, D24, D25, D29, D30, D35, D37, D38,
D50. Last affirmed at `1816f5f`.

**Reopened (4) → all fixed + verified in the v6 fix round (`5734021`).** Fix present and
correct-on-inspection, but deleting it left both suites green:

| D# | Sev | Status | What was missing | Site |
|----|-----|--------|------------------|------|
| D27 | 🟡 Low | verified (`5734021`) | no test pinned the monotonic `LastTrackingSyncAt` guard (`Status = Shipped AND stamp < now`) | `BackgroundJobs/ShipmentTrackingJob.cs:228` |
| D39 | ⚪ | verified (`5734021`) | `SamedayCompositionRootTests` never resolved `IShippingService`, so the `AddScoped<StaticShippingService>()` the fix added was unproven | `Extensions/SamedayServiceCollectionExtensions.cs:60` |
| D71 | 🟡 Low | verified (`5734021`) | reverting to Polly base-2 `Exponential` (the original defect) was invisible — the only retry test does a single retry, 1 s under both schedules | `Services/Sameday/SamedayPolicies.cs:54` |
| D79 | 🟡 Low | verified (`5734021`) | nothing asserted the `name` claim or that `currentUser$` emits — the guest/signed-in prefill cluster (CLAUDE.md class 11) | `Services/TokenService.cs:33` · `UI/…/auth.service.ts:55` |

**Recorded gaps on fixes that held → both closed in the v6 fix round (`5734021`):** D68's post-create
persist-fail `PreserveClaim` leg was untested (the timeout + retryable-status legs reddened); D78's
review-step assertion (`not.toContain('undefined')`) could not fail in Angular — a safe-navigation
gate removal reddened nothing, and now does.

**New (D90–D96) — backlog, none re-arms the loop:**

| D# | Sev | Status | Title | Site |
|----|-----|--------|-------|------|
| D90 | 🟡 Low | backlog | `ISamedayAuthenticator` singleton captures the transient typed `ISamedayClient` → handler never rotated (pre-existing, carried into the new extension) | `Extensions/SamedayServiceCollectionExtensions.cs:37` |
| D91 | ⚪ | backlog | `ISamedayClient` doc still claims NotImplementedException "until bolt 037" — stale twin of the claim stripped from `SamedayClient.cs` | `Services/Sameday/ISamedayClient.cs:8` |
| D92 | ⚪ | backlog | `AwbNumber` (varchar(100)) is the unclamped sibling of D60's clamp on the same post-bill persist | `Services/Sameday/AwbCreator.cs:190` |
| D93 | ⚪ | backlog | `Created` outcome reports the unclamped LabelUrl while the row stores null | `Services/Sameday/AwbCreator.cs:207` |
| D94 | ⚪ | backlog | `MaxRequestsPerSecond` missing from appsettings.json, the settings validator, and bolt-037 ddd-02 | `Configuration/SamedaySettings.cs:52` |
| D95 | ⚪ | backlog | D67's 30 s poll buffer is a flat constant, not scaled to the interval | `BackgroundJobs/ShipmentTrackingJob.cs:77` |
| D96 | ⚪ | backlog | Record accuracy: resolution-v5/index.md say "backend 914" (tip = 916) and "fixed: 30" (frontmatter holds 41); index cites `66c6d50` not the tip `1816f5f` | `reviews/015-sameday-shipping/resolution-v5.md:75` |

**Dispositions upheld, last affirmed `1816f5f`:** D72, D81, D40 (deferred — cited files unchanged
since `5fc330b`); D83, D89 (wont-fix; D89 with a caveat — its rationale rests on an untracked
pre-enable label-proxy endpoint); D88, D33 (false-positive, both independently re-checked).
**D45 residual + D23 unchanged and still accepted** — D23 now covers the new `AlterAwbLabelUrlLength`
DDL too.

## v5 — certification (2026-07-28, commit `5fc330b`, **CERTIFIED** · approve-with-followups)

One blinded 11-lens full-manifest pass (recorded single-pass deviation). No serious defect survives:
**0 High, 0 fix-caused regression, 0 reopened.** New findings are the pre-enable checklist — all
dormant behind the two `false` flags. Detail: [findings-v5.md](findings-v5.md).

**Medium (D55–D66, all confirmed — address before enabling):**

Status column updated by the v5 fix round + v6 verification (`1816f5f`).

| D# | Sev | Status | Title | Site |
|----|-----|--------|-------|------|
| D55 | 🟠 Med | verified | Easybox address fields uncapped → 28 MB storage-exhaustion DoS | `Validators/…/CreateOrderRequestValidator.cs:26` |
| D56 | 🟠 Med | verified | AwbLabelUrl persisted but never surfaced to admin; GetLabelPdfAsync no caller (Must goal undelivered) | `DTOs/Admin/AdminOrderDtos.cs:44` |
| D57 | 🟠 Med | verified | Stale-claim (crashed-worker) reclaim path untested | `Tests/…/AwbCreatorTests.cs:250` |
| D58 | 🟠 Med | verified | Claim-release-after-failure untested | `Tests/…/AwbCreatorTests.cs:326` |
| D59 | 🟠 Med | verified | prefillEasyboxContact guest/signed-in branches untested (guest-state cluster) | `UI/…/delivery-step.spec.ts` |
| D60 | 🟠 Med | verified | Vendor pdfLink > 500 overflows Postgres varchar(500) → re-bill loop | `Services/Sameday/AwbCreator.cs:156` |
| D61 | 🟠 Med | verified | Phone regex over-accepts digit-poor input → paid AWB call → GiveUp | `Validators/…/CreateOrderRequestValidator.cs:28` |
| D62 | 🟠 Med | verified | Vendor rejection ResponseBody captured but never logged on GiveUp | `Services/Sameday/AwbCreator.cs:136` |
| D63 | 🟠 Med | verified | Systemic tracking failure logged per-order Warning, never Error | `BackgroundJobs/ShipmentTrackingJob.cs:148` |
| D64 | 🟠 Med | verified | selectMethod never resets selectedLockerId → Easybox 400 dead-end | `UI/…/delivery-step.ts:399` |
| D65 | 🟠 Med | verified | Enabled=true root never booted; token-provider↔auth-handler DI-cycle risk unverified — **real cycle reproduced at v6** | `Program.cs:146` |
| D66 | 🟠 Med | verified | Local EasyboxLockers.SamedayId freshness assumed, no sync → permanent GiveUp | `Services/Sameday/OrderToAwbRequestMapper.cs:48` |

**Low (D67–D82) & Cleanup (D83–D89):** backlog — poll-throttle every-other-tick (D67); claim released
on timeout (D68); client phone gate weaker than server (D69); no response-size cap → OOM (D70); Polly
1/2/4 s not 1/4/16 s + wrong comment (D71); ShippedAt no backfill (D72); FR-4 logging partial (D73);
prefill re-implements GuestAuthService (D74); status-classification dup 4× (D75); parallel poll untested
(D76); retry sweep InMemory-only + fresh-claim untested (D77); setLocker/review-step untested (D78);
signed-in prefill dead code (D79); transient locker error shown as "no easybox" (D80); service-id
defaults `7` unvalidated when Enabled (D81, parked pre-enable task); dispatcher/sweep double-enqueue
window untested (D82). Cleanup: bundled locker-map UX undocumented (D83); two jobs track read-only
(D84/D85); phone rule+regex dup (D86); magic day-count floors (D87); DeliveredAt-timestamptz **refuted**
(D88, Npgsql handles DateTimeOffset); GetLabelPdfAsync dead code (D89, see D56).

**Status after the v5 fix round + v6 verification:** all of D67–D87 `verified` **except** D71 and D79
(**reopened** — no test can go red), D72/D81 (deferred, upheld) and D83 (wont-fix, upheld). D88/D33
false-positive, upheld. D84–D87 verified by inspection (refactor/cleanup, no behavioral assertion).

**Re-affirmed deferrals (prior decision attached, stand):** D50 → **fixed + verified** at v6 (the
dispatcher orchestration test landed). D23 stands, and now also covers the new
`AlterAwbLabelUrlLength` DDL. D29 → **fixed + verified**. D39 → fixed but **reopened** at v6.
**D45 residual re-confirmed + accepted:** AWB-create POST auto-retried; no-double-bill rests on
unverified vendor dedup on `ClientInternalReference`. Skeptic built no code-only trace. **Verify
Sameday create-idempotency before enabling** (ADR-015).

## v3 — certification pair (2026-07-27, commit `8584572`, NOT certified)

New defects the two independent blinded passes surfaced (D43–D54); D31 re-opened. `open` = to fix.

| D# | F# (v3) | Sev | Status | Title | Site |
|----|---------|-----|--------|-------|------|
| D43 | F1 | 🔴 High | verified | Easybox `Continue` never re-enables after typing contact (`canContinue` computed can't see `form.valid`) — regression from v1 F4 | `UI/…/delivery-step.ts:326` |
| D44 | F2 | 🔴 High | verified | Slow-Sameday `OperationCanceledException` treated as shutdown → tracking poll loop exits (pre-existing) | `BackgroundJobs/ShipmentTrackingJob.cs:54` |
| D45 | F3 | 🔴 High | verified | No per-order guard before the vendor AWB call; DB CAS blocks only the 2nd DB write — duplicate-safety rests on unverified vendor dedup (D2 residual; owner decision) | `Services/Sameday/AwbCreator.cs:69` |
| D46 | F4 | 🟠 Med | verified | `isDeliveryComplete()` Easybox gate ignores mandatory contact → stepper skip to payment → 400 (regression from v1 F4) | `UI/…/checkout-state.service.ts:51` |
| D47 | F5 | 🟠 Med | verified | Same OCE-as-shutdown bug drops an AWB dispatch job silently | `BackgroundJobs/AwbDispatcher.cs:69` |
| D48 | F6 | 🟠 Med | verified | `LastTrackingSyncAt=UtcNow` fallback + monotonic guard can strand a Shipped order (never Delivered) | `BackgroundJobs/ShipmentTrackingJob.cs:139` |
| D49 | F7 | 🟠 Med | verified | EuPlatesc webhook→AWB enqueue untested (Stripe-only from v1 F6) | `Tests/…/PaymentControllerIntegrationTests.cs` |
| D50 | F8 | 🟠 Med | deferred | `AwbDispatcher` outcome routing + re-enqueue untested — needs a background-service harness with injected delay; open coverage gap | `BackgroundJobs/AwbDispatcher.cs:83` |
| D51 | F9 | 🟠 Med | verified | `Status != Cancelled` persist guard (v1 F12) has no test | `Services/Sameday/AwbCreator.cs:107` |
| D52 | F10 | 🟠 Med | verified | A `429` surviving retries → permanent GiveUp instead of transient | `Services/Sameday/SamedayClient.cs:139` |
| D53 | F13 | 🟡 Doc | verified | ADR-015 + 037 domain model name `awbPayment` (not `clientInternalReference`) as the idempotency key — doc trap (code correct) | `memory-bank/…/adr-015-*.md` |
| D54 | F11 | 🟠 Med | verified | Paid→Cancelled orphan billable AWB — no compensating void/ops-alert (D12 residual) | `Services/Sameday/AwbCreator.cs:141` |

**Re-opened:** D31 (was backlog Low) → Medium, **fixed** (`f3d2508`).

**v3 fix round (resolution-v3, aada94b..5fc330b) → v4 verification (review-v4, 2026-07-27 @ 5fc330b):**
D43–D54 **verified** (3 blockers revert-and-rerun; rest via the independent fix-diff micro-review +
inspection) except **D50 deferred** (dispatcher-runtime test — harness needed). Backlog folded in +
verified: **D21, D22, D24, D26, D28, D36, D41**. 0 reopened. Still deferred: D20, D23, D25, D27, D29,
D30, D33, D35, D37, D38, D39, D40. Next: single-pass certification (recorded deviation). **D45
crash-window residual accepted+alerted (verify vendor idempotency before enabling — ADR-015).**



# Canonical finding ledger — 015-sameday-shipping

Stable `D#` identities for this target, per the README's persistent-ledger standard. Each real defect
gets a `D#` that lives forever; each pass's pass-local `F#` maps onto a `D#` **after** the blinded pass
completes (finders never see `D#` during the search).

**v1 is the first pass**, so `F#` ↔ `D#` is **1:1** (no reconciliation against a prior ledger — nothing
to match). The pass ran full-manifest (11 lenses) against commit `1765918`, `feat/bolt-036-sameday-api-client`.

Status legend: `open` = confirmed, awaiting fix · `backlog` = triaged Low/Cleanup that does not re-arm
the loop (severity-based stop rule) · `false-positive` = refuted, terminal · (later: `in-progress` /
`fixed` / `verified` / `wont-fix` / `deferred` / `disputed`). Terminal rows feed the discovery script's
`decidedFindings` on the next pass; a re-raise gets the prior decision **attached, never suppressed**.

## v1 — discovery (2026-07-27, commit `1765918`)

| D# | F# (v1) | Sev | Status | Title | Site |
|----|---------|-----|--------|-------|------|
| D1 | F1 | 🔴 High | verified | AWB vendor idempotency key wired to constant `PickupPointId`, not per-order (breaks ADR-015) | `Services/Sameday/SamedayClient.cs:104` |
| D2 | F2 | 🔴 High | verified | Concurrent AWB creators double-create (check-then-act, no DB guard) | `Services/Sameday/AwbCreator.cs:69` |
| D3 | F3 | 🔴 High | verified | One `DbContext` shared across concurrent tracking-poll tasks → tick faults | `BackgroundJobs/ShipmentTrackingJob.cs:87` |
| D4 | F4 | 🔴 High | verified | Easybox AWB carries null recipient name/phone (dead null-guard) → permanent give-up | `Services/Sameday/OrderToAwbRequestMapper.cs:60` |
| D5 | F5 | 🔴 High | verified | Easybox locker `SamedayId` dropped + wire `Service` hardcoded 7 → unroutable / wrong service | `Services/Sameday/OrderToAwbRequestMapper.cs:66` |
| D6 | F6 | 🟠 Med | verified | Webhook→AWB enqueue wiring untested (green suite hides removal) | `Controllers/WebhooksController.cs:192` |
| D7 | F7 | 🟠 Med | verified | ADR-016 CAS race-lost test seeds Cancelled → never reaches the CAS | `Tests/…/ShipmentTrackingJobTests.cs:136` |
| D8 | F8 | 🟠 Med | verified | `AwbDispatcher` backoff off-by-one: last entry unreachable | `BackgroundJobs/AwbDispatcher.cs:124` |
| D9 | F9 | 🟠 Med | verified | Rate limiter re-created per request → throttle inert + timer leak | `Services/Sameday/SamedayPolicies.cs:44` |
| D10 | F10 | 🟠 Med | verified | Admin `→Shipped` nulls machine-created `AwbNumber` when field omitted | `Services/AdminOrderService.cs:117` |
| D11 | F11 | 🟠 Med | verified | AWB enqueue in webhooks only, not the transition hook → admin-Paid never creates AWB | `Services/AdminOrderService.cs:113` |
| D12 | F12 | 🟠 Med | verified | AWB persisted onto an order cancelled mid-call (no re-check before save) | `Services/Sameday/AwbCreator.cs:93` |
| D13 | F13 | 🟠 Med | verified | Courier recipient name/phone/street/number unvalidated → AWB give-up | `Validators/Payments/CreateOrderRequestValidator.cs:27` |
| D14 | F14 | 🟠 Med | verified | `SamedayUnreachableException` swallowed with no log → tracking stalls silently | `BackgroundJobs/ShipmentTrackingJob.cs:128` |
| D15 | F15 | 🟠 Med | verified | Created AWB number not logged before `SaveChanges` → orphan billable AWB invisible | `Services/Sameday/AwbCreator.cs:96` |
| D16 | F16 | 🟠 Med | verified | `AwbCreator` test green even if `SaveChangesAsync` removed (identity-map read) | `Tests/…/AwbCreatorTests.cs:141` |
| D17 | F17 | 🟠 Med | verified | Admin `ShippedAt`/`DeliveredAt` assignment untested | `Services/AdminOrderService.cs:119` |
| D18 | F18 | 🟠 Med | verified | Clearing city search can permanently kill the locker-search pipe on transient error | `UI/…/delivery-step.ts:332` |
| D19 | F19 | 🟠 Med | verified | Init priming `getLockers('')` races city-search `switchMap`, overwrites filter | `UI/…/delivery-step.ts:317` |
| D20 | F20 | 🟡 Low | backlog | `MaxConcurrentSamedayCalls` overloaded as concurrency gate AND req/s rate limit | `Services/Sameday/SamedayResilienceHandler.cs:25` |
| D21 | F21 | 🟡 Low | backlog | Raw vendor error body in exception + logged at Error (conditional PII) | `Services/Sameday/SamedayClient.cs:140` |
| D22 | F22 | 🟡 Low | backlog | `AwbLabelUrl` migration hardcodes `text` → unbounded on Postgres, diverges from model *(hinted)* | `Migrations/20260602141429_AddSamedayOrderFields.cs:23` |
| D23 | F23 | 🟡 Low | backlog | Dual-DB parity: migrations + `timestamptz` CAS never run on Postgres (offset-write may throw) *(hinted)* | `Tests/…/OrderSamedayFieldsTests.cs:21` |
| D24 | F24 | 🟡 Low | backlog | Tracking `observedAt` fabricated to `UtcNow` when vendor omits timestamps → wrong `DeliveredAt` | `Services/Sameday/SamedayClient.cs:224` |
| D25 | F25 | 🟡 Low | backlog | `expire_at_utc` bound without UTC guarantee (non-UTC host shifts token expiry) | `Services/Sameday/SamedayClient.cs:90` |
| D26 | F26 | 🟡 Low | backlog | Monotonic guard can drop a legitimate `Delivered` snapshot (untested) | `BackgroundJobs/ShipmentTrackingJob.cs:132` |
| D27 | F27 | 🟡 Low | backlog | Non-delivered tracking write not monotonic across replicas (early-repoll leg refuted) — *plausible* | `BackgroundJobs/ShipmentTrackingJob.cs:182` |
| D28 | F28 | 🟡 Low | backlog | AWB-enqueue logged at Debug, below Information floor → never emits | `Services/Sameday/AwbCreationNotifier.cs:32` |
| D29 | F29 | 🟡 Low | backlog | Polly retry has no `OnRetry` callback → transient retries invisible | `Services/Sameday/SamedayPolicies.cs` (retry) |
| D30 | F30 | 🟡 Low | backlog | Documented `/health` `sameday:enabled` field not delivered | `HealthChecks/HealthCheckResponseWriter.cs:36` |
| D31 | F31 | 🟡 Low | backlog | `GenerateAwbAsync` returns stale "generate manually" + pre-037 comment | `Services/SamedayShippingService.cs:52` |
| D32 | F32 | 🟡 Low | verified | `AwbCreationRequest` documented as validated value object but has no validation | `Services/Sameday/AwbCreationRequest.cs:11` |
| D33 | F33 | 🟡 Low | backlog | Tracking job re-queries already-loaded order; `inWindow` tracked-but-unused | `BackgroundJobs/ShipmentTrackingJob.cs:172` |
| D34 | F34 | 🟡 Low | verified | Production rate-limiter path never exercised in tests (incl. POST path from F42) | `Tests/…/SamedayPoliciesTests.cs:40` |
| D35 | F35 | 🟡 Low | backlog | Locker list fetched on every init even for Courier-only users (wasted fetch + toast) | `UI/…/delivery-step.ts:317` |
| D36 | F36 | ⚪ Cleanup | backlog | `TrackingPollOutcome` dead code (declared return type, never constructed) | `Services/Sameday/TrackingPollOutcome.cs:15` |
| D37 | F37 | ⚪ Cleanup | backlog | `LogRedactor` defined but never referenced → no HTTP transport tracing | `Services/Sameday/LogRedactor.cs:13` |
| D38 | F38 | ⚪ Cleanup | backlog | `TrackingStopRegistry` is a near-copy of `AwbGiveUpRegistry` | `Services/Sameday/TrackingStopRegistry.cs:9` |
| D39 | F39 | ⚪ Cleanup | backlog | Hand-constructs `StaticShippingService` instead of injecting | `Services/SamedayShippingService.cs:35` |
| D40 | F40 | ⚪ Cleanup | backlog | New migration designer snapshots embed stale `StripeClientSecret` 255 vs 512 *(hinted)* | `Migrations/20260602190046_…Designer.cs:365` |
| D41 | F41 | ⚪ Cleanup | backlog | Per-print gram weight bare literal `50` colliding with `MinimumGrams` | `Services/Sameday/ParcelWeight.cs:35` |
| D42 | F42 | — | false-positive | "5xx retry unsafe for POST bodies" — `JsonContent` re-serializes each attempt; no defect (test-gap → D34) | `Services/Sameday/SamedayResilienceHandler.cs:33` |

**Verified (v2, 2026-07-27):** D1–D19 + D32 + D34 (5 High + 14 Medium + 2 Low) — 0 reopened.
**Backlog:** D20–D31, D33, D35–D41 (14 Low + 6 Cleanup). **Terminal:** D42 (false-positive).

**Cluster notes (for the fixer):**
- **Idempotency/concurrency cluster:** D1 (root) → D2, D12, D15 depend on the per-order key + guarded
  write; D3 is the tracking-side DbContext scoping. Fix D1 first.
- **Recipient-mapping cluster:** D4, D5, D13, D32 — one decision on where recipient validation lives.
