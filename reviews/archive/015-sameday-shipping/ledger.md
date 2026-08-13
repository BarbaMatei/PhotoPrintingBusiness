---
type: review-ledger
target: 015-sameday-shipping
updated: 2026-08-11
closed: 2026-07-29 — owner sign-off @5734021 (no post-fix blinded pass; the pre-enable checklist is the gate)
---

# Ledger — 015-sameday-shipping

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-240 | 🔴 | v1 | AWB vendor idempotency key wired to constant `PickupPointId`, not per-order (breaks ADR-015) | `Services/Sameday/SamedayClient.cs:104` | verified | `727a018` |
| PPW-241 | 🔴 | v1 | Concurrent AWB creators double-create (check-then-act, no DB guard) | `Services/Sameday/AwbCreator.cs:69` | verified | `727a018` |
| PPW-242 | 🔴 | v1 | One `DbContext` shared across concurrent tracking-poll tasks → tick faults | `BackgroundJobs/ShipmentTrackingJob.cs:87` | verified | `727a018` |
| PPW-243 | 🔴 | v1 | Easybox AWB carries null recipient name/phone (dead null-guard) → permanent give-up | `Services/Sameday/OrderToAwbRequestMapper.cs:60` | verified | `727a018` |
| PPW-244 | 🔴 | v1 | Easybox locker `SamedayId` dropped + wire `Service` hardcoded 7 → unroutable / wrong service | `Services/Sameday/OrderToAwbRequestMapper.cs:66` | verified | `727a018` |
| PPW-245 | 🟠 | v1 | Webhook→AWB enqueue wiring untested (green suite hides removal) | `Controllers/WebhooksController.cs:192` | verified | `727a018` |
| PPW-246 | 🟠 | v1 | ADR-016 CAS race-lost test seeds Cancelled → never reaches the CAS | `Tests/…/ShipmentTrackingJobTests.cs:136` | verified | `727a018` |
| PPW-247 | 🟠 | v1 | `AwbDispatcher` backoff off-by-one: last entry unreachable | `BackgroundJobs/AwbDispatcher.cs:124` | verified | `727a018` |
| PPW-248 | 🟠 | v1 | Rate limiter re-created per request → throttle inert + timer leak | `Services/Sameday/SamedayPolicies.cs:44` | verified | `727a018` |
| PPW-249 | 🟠 | v1 | Admin `→Shipped` nulls machine-created `AwbNumber` when field omitted | `Services/AdminOrderService.cs:117` | verified | `727a018` |
| PPW-250 | 🟠 | v1 | AWB enqueue in webhooks only, not the transition hook → admin-Paid never creates AWB | `Services/AdminOrderService.cs:113` | verified | `727a018` |
| PPW-251 | 🟠 | v1 | AWB persisted onto an order cancelled mid-call (no re-check before save) | `Services/Sameday/AwbCreator.cs:93` | verified | `727a018` |
| PPW-252 | 🟠 | v1 | Courier recipient name/phone/street/number unvalidated → AWB give-up | `Validators/Payments/CreateOrderRequestValidator.cs:27` | verified | `727a018` |
| PPW-253 | 🟠 | v1 | `SamedayUnreachableException` swallowed with no log → tracking stalls silently | `BackgroundJobs/ShipmentTrackingJob.cs:128` | verified | `727a018` |
| PPW-254 | 🟠 | v1 | Created AWB number not logged before `SaveChanges` → orphan billable AWB invisible | `Services/Sameday/AwbCreator.cs:96` | verified | `727a018` |
| PPW-255 | 🟠 | v1 | `AwbCreator` test green even if `SaveChangesAsync` removed (identity-map read) | `Tests/…/AwbCreatorTests.cs:141` | verified | `727a018` |
| PPW-256 | 🟠 | v1 | Admin `ShippedAt`/`DeliveredAt` assignment untested | `Services/AdminOrderService.cs:119` | verified | `727a018` |
| PPW-257 | 🟠 | v1 | Clearing city search can permanently kill the locker-search pipe on transient error | `UI/…/delivery-step.ts:332` | verified | `727a018` |
| PPW-258 | 🟠 | v1 | Init priming `getLockers('')` races city-search `switchMap`, overwrites filter | `UI/…/delivery-step.ts:317` | verified | `727a018` |
| PPW-259 | 🟡 | v1 | `MaxConcurrentSamedayCalls` overloaded as concurrency gate AND req/s rate limit | `Services/Sameday/SamedayResilienceHandler.cs:25` | verified | `1816f5f` |
| PPW-260 | 🟡 | v1 | Raw vendor error body in exception + logged at Error (conditional PII) | `Services/Sameday/SamedayClient.cs:140` | verified | `5fc330b` |
| PPW-261 | 🟡 | v1 | `AwbLabelUrl` migration hardcodes `text` → unbounded on Postgres, diverges from model | `Migrations/20260602141429_AddSamedayOrderFields.cs:23` | verified | `5fc330b` |
| PPW-262 | 🟡 | v1 | Dual-DB parity: migrations + `timestamptz` CAS never run on Postgres | `Tests/…/OrderSamedayFieldsTests.cs:21` | deferred | `1816f5f` |
| PPW-263 | 🟡 | v1 | Tracking `observedAt` fabricated to `UtcNow` when vendor omits timestamps → wrong `DeliveredAt` | `Services/Sameday/SamedayClient.cs:224` | verified | `1816f5f` |
| PPW-264 | 🟡 | v1 | `expire_at_utc` bound without UTC guarantee (non-UTC host shifts token expiry) | `Services/Sameday/SamedayClient.cs:90` | verified | `1816f5f` |
| PPW-265 | 🟡 | v1 | Monotonic guard can drop a legitimate `Delivered` snapshot (untested) | `BackgroundJobs/ShipmentTrackingJob.cs:132` | verified | `5fc330b` |
| PPW-266 | 🟡 | v1 | Non-delivered tracking write not monotonic across replicas | `BackgroundJobs/ShipmentTrackingJob.cs:182` | verified | `5734021` |
| PPW-267 | 🟡 | v1 | AWB-enqueue logged at Debug, below Information floor → never emits | `Services/Sameday/AwbCreationNotifier.cs:32` | verified | `5fc330b` |
| PPW-268 | 🟡 | v1 | Polly retry has no `OnRetry` callback → transient retries invisible | `Services/Sameday/SamedayPolicies.cs` (retry) | verified | `1816f5f` |
| PPW-269 | 🟡 | v1 | Documented `/health` `sameday:enabled` field not delivered | `HealthChecks/HealthCheckResponseWriter.cs:36` | verified | `1816f5f` |
| PPW-270 | 🟠 | v1 | `GenerateAwbAsync` returns stale "generate manually" + pre-037 comment | `Services/SamedayShippingService.cs:52` | verified | `5fc330b` |
| PPW-271 | 🟡 | v1 | `AwbCreationRequest` documented as validated value object but has no validation | `Services/Sameday/AwbCreationRequest.cs:11` | verified | `727a018` |
| PPW-272 | 🟡 | v1 | Tracking job re-queries already-loaded order; `inWindow` tracked-but-unused | `BackgroundJobs/ShipmentTrackingJob.cs:172` | false-positive | `1816f5f` |
| PPW-273 | 🟡 | v1 | Production rate-limiter path never exercised in tests | `Tests/…/SamedayPoliciesTests.cs:40` | verified | `727a018` |
| PPW-274 | 🟡 | v1 | Locker list fetched on every init even for Courier-only users | `UI/…/delivery-step.ts:317` | verified | `1816f5f` |
| PPW-275 | ⚪ | v1 | `TrackingPollOutcome` dead code (declared return type, never constructed) | `Services/Sameday/TrackingPollOutcome.cs:15` | verified | `5fc330b` |
| PPW-276 | ⚪ | v1 | `LogRedactor` defined but never referenced → no HTTP transport tracing | `Services/Sameday/LogRedactor.cs:13` | verified | `1816f5f` |
| PPW-277 | ⚪ | v1 | `TrackingStopRegistry` is a near-copy of `AwbGiveUpRegistry` | `Services/Sameday/TrackingStopRegistry.cs:9` | verified | `1816f5f` |
| PPW-278 | ⚪ | v1 | Hand-constructs `StaticShippingService` instead of injecting | `Services/SamedayShippingService.cs:35` | verified | `5734021` |
| PPW-279 | ⚪ | v1 | New migration designer snapshots embed stale `StripeClientSecret` 255 vs 512 | `Migrations/20260602190046_…Designer.cs:365` | deferred | `1816f5f` |
| PPW-280 | ⚪ | v1 | Per-print gram weight bare literal `50` colliding with `MinimumGrams` | `Services/Sameday/ParcelWeight.cs:35` | verified | `5fc330b` |
| PPW-281 | — | v1 | "5xx retry unsafe for POST bodies" — `JsonContent` re-serializes each attempt | `Services/Sameday/SamedayResilienceHandler.cs:33` | false-positive | `1765918` |
| PPW-282 | 🔴 | v3 | Easybox `Continue` never re-enables after typing contact (`canContinue` cannot see `form.valid`) | `UI/…/delivery-step.ts:326` | verified | `5fc330b` |
| PPW-283 | 🔴 | v3 | Slow-Sameday `OperationCanceledException` treated as shutdown → tracking poll loop exits | `BackgroundJobs/ShipmentTrackingJob.cs:54` | verified | `5fc330b` |
| PPW-284 | 🔴 | v3 | No per-order guard before the vendor AWB call; DB CAS blocks only the 2nd DB write | `Services/Sameday/AwbCreator.cs:69` | verified | `1816f5f` |
| PPW-285 | 🟠 | v3 | `isDeliveryComplete()` Easybox gate ignores mandatory contact → stepper skip to payment → 400 | `UI/…/checkout-state.service.ts:51` | verified | `5fc330b` |
| PPW-286 | 🟠 | v3 | Same OCE-as-shutdown bug drops an AWB dispatch job silently | `BackgroundJobs/AwbDispatcher.cs:69` | verified | `5fc330b` |
| PPW-287 | 🟠 | v3 | `LastTrackingSyncAt=UtcNow` fallback + monotonic guard can strand a Shipped order | `BackgroundJobs/ShipmentTrackingJob.cs:139` | verified | `5fc330b` |
| PPW-288 | 🟠 | v3 | EuPlatesc webhook→AWB enqueue untested (Stripe-only from PPW-245) | `Tests/…/PaymentControllerIntegrationTests.cs` | verified | `5fc330b` |
| PPW-289 | 🟠 | v3 | `AwbDispatcher` outcome routing + re-enqueue untested | `BackgroundJobs/AwbDispatcher.cs:83` | verified | `1816f5f` |
| PPW-290 | 🟠 | v3 | `Status != Cancelled` persist guard has no test | `Services/Sameday/AwbCreator.cs:107` | verified | `5fc330b` |
| PPW-291 | 🟠 | v3 | A `429` surviving retries → permanent GiveUp instead of transient | `Services/Sameday/SamedayClient.cs:139` | verified | `5fc330b` |
| PPW-292 | 🟡 | v3 | ADR-015 + 037 domain model name `awbPayment` as the idempotency key — doc trap | `memory-bank/…/adr-015-*.md` | verified | `5fc330b` |
| PPW-293 | 🟠 | v3 | Paid→Cancelled orphan billable AWB — no compensating void or operator alert | `Services/Sameday/AwbCreator.cs:141` | verified | `5fc330b` |
| PPW-294 | 🟠 | v5 | Easybox address fields uncapped → 28 MB storage-exhaustion DoS | `Validators/…/CreateOrderRequestValidator.cs:26` | verified | `1816f5f` |
| PPW-295 | 🟠 | v5 | `AwbLabelUrl` persisted but never surfaced to admin; `GetLabelPdfAsync` has no caller | `DTOs/Admin/AdminOrderDtos.cs:44` | verified | `1816f5f` |
| PPW-296 | 🟠 | v5 | Stale-claim (crashed-worker) reclaim path untested | `Tests/…/AwbCreatorTests.cs:250` | verified | `1816f5f` |
| PPW-297 | 🟠 | v5 | Claim-release-after-failure untested | `Tests/…/AwbCreatorTests.cs:326` | verified | `1816f5f` |
| PPW-298 | 🟠 | v5 | `prefillEasyboxContact` guest/signed-in branches untested | `UI/…/delivery-step.spec.ts` | verified | `1816f5f` |
| PPW-299 | 🟠 | v5 | Vendor `pdfLink` > 500 overflows Postgres `varchar(500)` → re-bill loop | `Services/Sameday/AwbCreator.cs:156` | verified | `1816f5f` |
| PPW-300 | 🟠 | v5 | Phone regex over-accepts digit-poor input → paid AWB call → GiveUp | `Validators/…/CreateOrderRequestValidator.cs:28` | verified | `1816f5f` |
| PPW-301 | 🟠 | v5 | Vendor rejection `ResponseBody` captured but never logged on GiveUp | `Services/Sameday/AwbCreator.cs:136` | verified | `1816f5f` |
| PPW-302 | 🟠 | v5 | Systemic tracking failure logged per-order at Warning, never Error | `BackgroundJobs/ShipmentTrackingJob.cs:148` | verified | `1816f5f` |
| PPW-303 | 🟠 | v5 | `selectMethod` never resets `selectedLockerId` → Easybox 400 dead-end | `UI/…/delivery-step.ts:399` | verified | `1816f5f` |
| PPW-304 | 🟠 | v5 | `Enabled=true` root never booted; token-provider ↔ auth-handler DI cycle unverified | `Program.cs:146` | verified | `1816f5f` |
| PPW-305 | 🟠 | v5 | Local `EasyboxLockers.SamedayId` freshness assumed, no sync → permanent GiveUp | `Services/Sameday/OrderToAwbRequestMapper.cs:48` | verified | `1816f5f` |
| PPW-306 | 🟡 | v5 | Poll-throttle window equals the tick interval, so orders poll every other tick | `BackgroundJobs/ShipmentTrackingJob.cs:74` | verified | `1816f5f` |
| PPW-307 | 🟡 | v5 | Durable claim released on vendor-call timeout — the one unknown-state outcome | `Services/Sameday/AwbCreator.cs:90` | verified | `5734021` |
| PPW-308 | 🟡 | v5 | Client Easybox phone check is presence-only, weaker than the server rule | `UI/…/delivery-step.ts:321` | verified | `1816f5f` |
| PPW-309 | 🟡 | v5 | No response-size cap on untrusted Sameday bodies → out-of-memory risk | `Services/Sameday/SamedayClient.cs:218` | verified | `1816f5f` |
| PPW-310 | 🟡 | v5 | Retry backoff is 1/2/4 s, not the documented 1/4/16 s; the comment is wrong | `Services/Sameday/SamedayPolicies.cs:50` | verified | `5734021` |
| PPW-311 | 🟡 | v5 | New `ShippedAt` column has no backfill, so pre-integration Shipped orders never poll | `Migrations/20260602190046:21` | deferred | `1816f5f` |
| PPW-312 | 🟡 | v5 | FR-4 per-attempt logging partial; no correlation id in any background service | `BackgroundJobs/AwbRetryJob.cs:95` | verified | `1816f5f` |
| PPW-313 | 🟡 | v5 | `prefillEasyboxContact` re-implements the guest-session read | `UI/…/delivery-step.ts:382` | verified | `1816f5f` |
| PPW-314 | 🟡 | v5 | HTTP status classification duplicated 4× and drifting from `SamedayPolicies` | `Services/Sameday/SamedayClient.cs:65` | verified | `1816f5f` |
| PPW-315 | 🟡 | v5 | Parallel multi-order poll fan-out never exercised (every test seeds one order) | `Tests/…/ShipmentTrackingJobTests.cs:117` | verified | `1816f5f` |
| PPW-316 | 🟡 | v5 | Retry sweep tested only on EF InMemory; the fresh-claim skip clause never runs | `Tests/…/AwbRetryJobTests.cs:23` | verified | `1816f5f` |
| PPW-317 | 🟡 | v5 | `setLocker` contact preservation and the Easybox review-step display untested | `UI/…/checkout-state.service.spec.ts:45` | verified | `5734021` |
| PPW-318 | 🟡 | v5 | Signed-in recipient-name prefill is dead code — the user stream never emits | `UI/…/delivery-step.ts:392` | verified | `5734021` |
| PPW-319 | 🟡 | v5 | Transient locker-search 500 shown as "no easybox in this city" | `UI/…/delivery-step.ts:371` | verified | `1816f5f` |
| PPW-320 | 🟡 | v5 | `LockerServiceId`/`CourierServiceId` default to placeholder `7`, unvalidated when enabled | `Validators/SamedaySettingsValidator.cs:38` | deferred | `1816f5f` |
| PPW-321 | 🟡 | v5 | Dispatcher backoff vs 60-minute sweep double-enqueue window untested | `Services/Sameday/AwbCreator.cs:129` | verified | `1816f5f` |
| PPW-322 | ⚪ | v5 | Bundled locker-map behaviour shipped with no story or acceptance criteria | `UI/…/delivery-step.ts:366` | wont-fix | `1816f5f` |
| PPW-323 | ⚪ | v5 | `AwbCreator` loads the order tracked but only reads it | `Services/Sameday/AwbCreator.cs:42` | verified | `1816f5f` |
| PPW-324 | ⚪ | v5 | Tracking poll loads the order tracked but only reads it | `BackgroundJobs/ShipmentTrackingJob.cs:129` | verified | `1816f5f` |
| PPW-325 | ⚪ | v5 | Recipient phone rule and regex duplicated across the Easybox and Courier blocks | `Validators/…/CreateOrderRequestValidator.cs:40` | verified | `1816f5f` |
| PPW-326 | ⚪ | v5 | Magic day-count query floors coupled to the registry lifetimes, unnamed | `BackgroundJobs/AwbRetryJob.cs:252` | verified | `1816f5f` |
| PPW-327 | ⚪ | v5 | `DeliveredAt` written to `timestamptz` from a non-UTC offset — Npgsql handles it | — | false-positive | `1816f5f` |
| PPW-328 | ⚪ | v5 | `GetLabelPdfAsync` has no production caller | — | wont-fix | `1816f5f` |
| PPW-329 | 🟡 | v6 | `ISamedayAuthenticator` singleton captures the transient typed client → handler never rotated | `Extensions/SamedayServiceCollectionExtensions.cs:37` | backlog | `1816f5f` |
| PPW-330 | ⚪ | v6 | `ISamedayClient` doc still claims `NotImplementedException` "until bolt 037" | `Services/Sameday/ISamedayClient.cs:8` | backlog | `1816f5f` |
| PPW-331 | ⚪ | v6 | `AwbNumber` is the unclamped sibling of PPW-299's clamp on the same post-billing persist | `Services/Sameday/AwbCreator.cs:190` | backlog | `1816f5f` |
| PPW-332 | ⚪ | v6 | `Created` outcome reports the unclamped label link while the row stores null | `Services/Sameday/AwbCreator.cs:207` | backlog | `1816f5f` |
| PPW-333 | ⚪ | v6 | `MaxRequestsPerSecond` missing from `appsettings.json`, the validator and ddd-02 | `Configuration/SamedaySettings.cs:52` | backlog | `1816f5f` |
| PPW-334 | ⚪ | v6 | PPW-306's 30 s poll buffer is a flat constant, not scaled to the interval | `BackgroundJobs/ShipmentTrackingJob.cs:77` | backlog | `1816f5f` |
| PPW-335 | ⚪ | v6 | Record accuracy: two wrong counts in resolution-v5, one stale commit in the index | `reviews/015-sameday-shipping/resolution-v5.md:75` | backlog | `1816f5f` |

## Details

### PPW-240 — AWB vendor idempotency key wired to constant `PickupPointId`, not per-order (breaks ADR-015)

- **What:** Every AWB create sent the shop-wide `PickupPointId` as the vendor's duplicate-detection
  reference, so retries were never deduplicated and one order could receive another order's label.
- **History:**
  - v1: found — 6 lenses, the highest agreement of the pass
  - round 1: fixed @`edd49f7` — the reference is now the order number
  - v2: verified @`727a018`

### PPW-241 — Concurrent AWB creators double-create (check-then-act, no DB guard)

- **What:** Two workers could both pass the "AWB number is null" re-check and both call the vendor,
  minting two billable labels for one order.
- **History:**
  - v1: found
  - round 1: fixed @`edd49f7` — guarded update, one writer wins the row
  - v2: verified @`727a018`
  - v3: the vendor-side half survived and was raised as PPW-284

### PPW-242 — One `DbContext` shared across concurrent tracking-poll tasks → tick faults

- **What:** The tick resolved one database context and polled up to five orders on it at once, so EF
  threw and the whole tick recorded no delivery.
- **History:**
  - v1: found
  - round 1: fixed @`d6744f1` — a scope and context per order, opened after the concurrency gate
  - v2: verified @`727a018`

### PPW-243 — Easybox AWB carries null recipient name/phone (dead null-guard) → permanent give-up

- **What:** Easybox orders reached the vendor with a null recipient name and phone, because the
  mapper's guard checked the address object rather than its fields, so every one failed closed.
- **History:**
  - v1: found
  - round 1: fixed @`835e932` — contact captured at checkout, validated on the server, re-checked in the mapper
  - v2: verified @`727a018`
  - v3: the fix introduced two checkout regressions, raised as PPW-282 and PPW-285

### PPW-244 — Easybox locker `SamedayId` dropped + wire `Service` hardcoded 7 → unroutable / wrong service

- **What:** The locker's vendor id never reached the wire and the service code stayed at the DTO
  default, so Easybox labels were unroutable and courier labels carried the locker service.
- **History:**
  - v1: found
  - round 1: fixed @`edd49f7` — service id and locker id sent per delivery type
  - v2: verified @`727a018`

### PPW-245 — Webhook→AWB enqueue wiring untested (green suite hides removal)

- **What:** Deleting the webhook's enqueue calls left all 862 tests green, because the test factory
  registered no recording double for the notifier.
- **History:**
  - v1: found
  - round 1: fixed @`e8d4b53` — recording notifier plus a Stripe webhook test
  - v2: verified @`727a018`
  - v3: the EuPlatesc half was still uncovered and was raised as PPW-288

### PPW-246 — ADR-016 CAS race-lost test seeds Cancelled → never reaches the CAS

- **What:** The race-lost test seeded a Cancelled order, which the in-window query filters out, so
  the guarded update under test never ran.
- **History:**
  - v1: found
  - round 1: fixed @`d6744f1` — the test now seeds a Shipped order and advances it mid-poll
  - v2: verified @`727a018`

### PPW-247 — `AwbDispatcher` backoff off-by-one: last entry unreachable

- **What:** The guard compared attempt against the schedule length with the wrong operator, so the
  final configured delay was never applied and the job retried four times instead of five.
- **History:**
  - v1: found — 3 lenses
  - round 1: fixed @`ef8d323` — the delay extracted as a pure, tested function
  - v2: verified @`727a018`

### PPW-248 — Rate limiter re-created per request → throttle inert + timer leak

- **What:** The sliding-window limiter was built inside the per-call delegate, so every call saw an
  empty window and each abandoned limiter leaked a replenishment timer.
- **History:**
  - v1: found
  - round 1: fixed @`1a240b7` — one limiter per handler, surfaced and disposed
  - v2: verified @`727a018`

### PPW-249 — Admin `→Shipped` nulls machine-created `AwbNumber` when field omitted

- **What:** The admin transition wrote the request's AWB number unconditionally, so an omitted field
  erased the number the job had created and the tracking job then skipped the order.
- **History:**
  - v1: found
  - round 1: fixed @`010c6dc` — the value is overwritten only when the admin supplies one
  - v2: verified @`727a018`

### PPW-250 — AWB enqueue in webhooks only, not the transition hook → admin-Paid never creates AWB

- **What:** An admin marking an order Paid enqueued no AWB and left the paid timestamp null, so the
  retry sweep could not find the order either.
- **History:**
  - v1: found
  - round 1: fixed @`010c6dc` — the admin Paid path stamps the timestamp and calls the notifier
  - v2: verified @`727a018`

### PPW-251 — AWB persisted onto an order cancelled mid-call (no re-check before save)

- **What:** A cancellation during the vendor call still had its AWB saved, leaving a real parcel for
  a cancelled order with no compensation.
- **History:**
  - v1: found
  - round 1: fixed @`edd49f7` — conditional write guarded on the status not being Cancelled
  - v2: verified @`727a018`
  - v3: the orphaned-label half was raised as PPW-293

### PPW-252 — Courier recipient name/phone/street/number unvalidated → AWB give-up

- **What:** Blank or oversized courier recipient fields passed checkout and payment, and the vendor
  then rejected the label, leaving a paid order with nothing.
- **History:**
  - v1: found
  - round 1: fixed @`e8d4b53` — server-side rules on all seven fields plus a phone format check
  - v2: verified @`727a018`

### PPW-253 — `SamedayUnreachableException` swallowed with no log → tracking stalls silently

- **What:** The commonest tracking fault returned from its catch with no log at all, so a multi-hour
  vendor outage produced no signal anywhere.
- **History:**
  - v1: found
  - round 1: fixed @`d6744f1` — the case now logs a warning with the order id
  - v2: verified @`727a018`

### PPW-254 — Created AWB number not logged before `SaveChanges` → orphan billable AWB invisible

- **What:** A save failure after a successful vendor call left a billed AWB nowhere in the logs, and
  the retry then created a second one.
- **History:**
  - v1: found
  - round 1: fixed @`edd49f7` — the number is logged before the write; a persist failure is transient
  - v2: verified @`727a018`

### PPW-255 — `AwbCreator` test green even if `SaveChangesAsync` removed (identity-map read)

- **What:** The happy-path test read back through the same database context the creator used, so it
  stayed green with the persist deleted.
- **History:**
  - v1: found
  - round 1: fixed @`edd49f7` — the tests moved to SQLite and read back through a fresh context
  - v2: verified @`727a018`

### PPW-256 — Admin `ShippedAt`/`DeliveredAt` assignment untested

- **What:** No test asserted either timestamp, though the tracking job only polls orders whose
  shipped timestamp is set.
- **History:**
  - v1: found
  - round 1: fixed @`010c6dc` — both assignments asserted
  - v2: verified @`727a018`

### PPW-257 — Clearing city search can permanently kill the locker-search pipe on transient error

- **What:** An error on the empty-search fetch propagated to a subscription with no error handler and
  terminated the stream, so locker search stayed dead until a page reload.
- **History:**
  - v1: found
  - round 1: fixed @`835e932` — the inner fetch is wrapped so an error cannot tear down the stream
  - v2: verified @`727a018` — the guarding test was added during this pass

### PPW-258 — Init priming `getLockers('')` races city-search `switchMap`, overwrites filter

- **What:** The initial priming fetch was a rival subscription, so a slow full-list response could
  land after a fast filtered one and replace it.
- **History:**
  - v1: found
  - round 1: fixed @`835e932` — priming folded into the same cancellable stream
  - v2: verified @`727a018`

### PPW-259 — `MaxConcurrentSamedayCalls` overloaded as concurrency gate AND req/s rate limit

- **What:** One setting feeds both the job concurrency gate and the transport rate permit. Raising it
  for throughput also lifts the outbound rate past the vendor's ceiling of about 10 per second.
- **Evidence:** `Services/Sameday/SamedayResilienceHandler.cs:25`; the settings validator allows values up to 50.
- **Suggested fix:** Split the two settings, or cap the derived rate at the vendor ceiling, and tighten the validator bound.
- **History:**
  - v1: found — 2 lenses
  - round 1: deferred — ledger backlog, outside the round
  - round 5: fixed @`56320c0` — a separate `MaxRequestsPerSecond` decouples the rate from the gate
  - v6: verified @`1816f5f`
  - 2026-07-29: target closed — row carried to the backlog

### PPW-260 — Raw vendor error body in exception + logged at Error (conditional PII)

- **What:** On a vendor rejection the whole response body was copied into the exception message and
  logged at Error. Shipping rejections commonly echo recipient name, phone and address.
- **Evidence:** `Services/Sameday/SamedayClient.cs:140`; the dispatcher logs the reason at Error.
- **Suggested fix:** Truncate or redact the body before it enters the message, or log only endpoint and status.
- **History:**
  - v1: found
  - round 1: deferred — ledger backlog
  - v3: re-raised by both passes, decision unchanged
  - round 3: fixed @`6606c25` — the vendor body no longer rides on the exception message
  - v4: verified @`5fc330b`
  - 2026-07-29: target closed — row carried to the backlog

### PPW-261 — `AwbLabelUrl` migration hardcodes `text` → unbounded on Postgres, diverges from model

- **What:** The migration sets an explicit column type, so Npgsql ignores the maximum length and
  Postgres gets an unbounded column while the model declares 500 characters.
- **Evidence:** `Migrations/20260602141429_AddSamedayOrderFields.cs:23`; the model sets a 500-character limit. Topic hinted by the shared dual-database context.
- **Suggested fix:** Use the provider-aware pattern already used elsewhere, and correct the migration comment's "capped to 500 chars" claim.
- **History:**
  - v1: found — hinted
  - round 1: deferred — ledger backlog
  - round 3: fixed @`2c434ad` — the migration ships a 500-character column on Postgres
  - v4: verified @`5fc330b`
  - 2026-07-29: target closed — row carried to the backlog

### PPW-262 — Dual-DB parity: migrations + `timestamptz` CAS never run on Postgres

- **What:** Migration statements, the timestamp columns and the guarded update never execute against
  Postgres. Tests run on EF InMemory and SQLite, which accept writes Npgsql may reject.
- **Evidence:** `Tests/…/OrderSamedayFieldsTests.cs:21` round-trips two columns on InMemory only. Topic hinted by the shared dual-database context.
- **Suggested fix:** Extend the round-trip test and add a Postgres container test that migrates and runs the guarded update, or record the accepted gap.
- **History:**
  - v1: found — hinted
  - round 1: deferred — ledger backlog
  - v3: re-raised (cert-A), decision unchanged
  - round 3: deferred again — the Postgres test belongs to the owner's three-environment stage
  - v5: re-raised, deferral re-affirmed
  - v6: re-affirmed @`1816f5f` — now also covers the round-5 label-length migration
  - 2026-07-29: target closed — row carried to the backlog; named in the pre-enable checklist

### PPW-263 — Tracking `observedAt` fabricated to `UtcNow` when vendor omits timestamps → wrong `DeliveredAt`

- **What:** When the vendor omits timestamps the client defaults the observation time to the current
  clock, so the delivered timestamp records the poll time rather than the delivery time.
- **Evidence:** `Services/Sameday/SamedayClient.cs:224`.
- **Suggested fix:** Require a real timestamp for a delivered state; treat a delivered response without one as a protocol error, or skip the write.
- **History:**
  - v1: found
  - round 1: deferred — ledger backlog
  - round 3: fixed @`18e7815` — folded into the PPW-287 poll-clock change
  - v4: verified @`5fc330b`
  - round 5: fixed again @`56320c0` — the observation time is nullable and the job supplies its poll clock
  - v6: verified @`1816f5f`
  - 2026-07-29: target closed — row carried to the backlog

### PPW-264 — `expire_at_utc` bound without UTC guarantee (non-UTC host shifts token expiry)

- **What:** The token expiry is bound with no UTC guarantee, so an offset-less vendor value on a
  non-UTC host shifts the validity window and costs extra re-authentication round trips.
- **Evidence:** `Services/Sameday/SamedayClient.cs:90`.
- **Suggested fix:** Parse the expiry as UTC explicitly, or reject offset-less values.
- **History:**
  - v1: found
  - round 1: deferred — ledger backlog
  - round 5: fixed @`56320c0` — the expiry is normalized to UTC
  - v6: verified @`1816f5f`
  - 2026-07-29: target closed — row carried to the backlog

### PPW-265 — Monotonic guard can drop a legitimate `Delivered` snapshot (untested)

- **What:** The monotonic guard runs before the delivered check, so a delivered snapshot whose real
  timestamp precedes an earlier fabricated sync is dropped and the order stays Shipped.
- **Evidence:** `BackgroundJobs/ShipmentTrackingJob.cs:132`; only the in-transit-backwards case is tested.
- **Suggested fix:** Add a test with a delivered snapshot older than the stored sync, then pin the chosen behaviour.
- **History:**
  - v1: found
  - round 1: deferred — ledger backlog
  - v3: the same interaction was raised at Medium as PPW-287
  - round 3: fixed @`18e7815` — the monotonic guard removed, the sync stamped from the poll clock
  - v4: verified @`5fc330b`
  - 2026-07-29: target closed — row carried to the backlog

### PPW-266 — Non-delivered tracking write not monotonic across replicas

- **What:** The non-delivered write updates the sync timestamp with a key-only predicate, so a
  late-committing replica can push the stamp backwards.
- **Evidence:** `BackgroundJobs/ShipmentTrackingJob.cs:182`. Plausible, not confirmed: the claimed early-repoll consequence was refuted, because a delivered row has already left the poll set.
- **Suggested fix:** Add status and forward-stamp clauses to the update predicate.
- **History:**
  - v1: found — one leg refuted, kept at Low
  - round 1: deferred — ledger backlog
  - round 5: fixed @`16d065b` — the write is guarded on status and a forward stamp
  - v6: reopened @`1816f5f` — the fix was correct but deleting it left both suites green
  - round 6: fixed @`5734021` — two replica-race tests pin both clauses, each proven to redden on revert
  - 2026-07-29: verified @`5734021` — recorded deviation: the fixer was also the verifier for this round
  - 2026-07-29: target closed — row carried to the backlog

### PPW-267 — AWB-enqueue logged at Debug, below Information floor → never emits

- **What:** The enqueue event is logged at Debug while both settings files set the floor at
  Information, so the entry point of the workflow never emits.
- **Evidence:** `Services/Sameday/AwbCreationNotifier.cs:32`.
- **Suggested fix:** Raise the call to Information, or lower the floor for the Sameday source context.
- **History:**
  - v1: found
  - round 1: deferred — ledger backlog
  - round 3: fixed @`6606c25` — the enqueue log raised to Information
  - v4: verified @`5fc330b`
  - 2026-07-29: target closed — row carried to the backlog

### PPW-268 — Polly retry has no `OnRetry` callback → transient retries invisible

- **What:** The retry strategy has no retry callback, so three transient retries leave no trace and
  per-call latency grows silently.
- **Evidence:** `Services/Sameday/SamedayPolicies.cs`, retry strategy options.
- **Suggested fix:** Add a retry callback logging attempt, delay and outcome.
- **History:**
  - v1: found
  - round 1: deferred — ledger backlog
  - v5: re-raised, deferral re-affirmed
  - round 5: fixed @`56320c0` — retries log through a retry callback
  - v6: verified @`1816f5f`
  - 2026-07-29: target closed — row carried to the backlog

### PPW-269 — Documented `/health` `sameday:enabled` field not delivered

- **What:** The 036 technical design states the health endpoint gains a Sameday field when the flag
  is on; neither the response writer nor the health-check registration knows about Sameday.
- **Evidence:** `HealthChecks/HealthCheckResponseWriter.cs:36`.
- **Suggested fix:** Add the field to the health payload when the flag is on, or delete the claim from the design document.
- **History:**
  - v1: found
  - round 1: deferred — ledger backlog
  - round 5: fixed @`56320c0` — resolved the other way: the design document corrected, the field dropped as out of scope
  - v6: verified @`1816f5f`
  - 2026-07-29: target closed — row carried to the backlog

### PPW-270 — `GenerateAwbAsync` returns stale "generate manually" + pre-037 comment

- **What:** With the flag on, the shipping endpoint still answers "generate one manually", so an
  admin can book a second label for an order the job already labelled.
- **Evidence:** `Services/SamedayShippingService.cs:52`, including a comment describing the pre-037 state.
- **Suggested fix:** Report the automatic workflow state instead of the manual fallback.
- **History:**
  - v1: found — Low
  - round 1: deferred — ledger backlog
  - v3: re-raised by both passes and raised to Medium for the duplicate-label risk
  - round 3: fixed @`f3d2508` — the endpoint reports automatic creation
  - v4: verified @`5fc330b`
  - 2026-07-29: target closed — row carried to the backlog

### PPW-271 — `AwbCreationRequest` documented as validated value object but has no validation

- **What:** The 037 domain model says construction validates the recipient fields and the parcel
  figures; the type is a plain record that validates nothing.
- **History:**
  - v1: found
  - round 1: fixed @`edd49f7` — folded into the PPW-243 recipient cluster; the mapper now rejects blanks
  - v2: verified @`727a018`

### PPW-272 — Tracking job re-queries already-loaded order; `inWindow` tracked-but-unused

- **What:** The tick was read as re-querying an order it had already loaded, and loading it tracked
  though every write goes through a direct update.
- **Evidence:** `BackgroundJobs/ShipmentTrackingJob.cs:172`.
- **Suggested fix:** None — the code the finding describes no longer exists; see the History.
- **History:**
  - v1: found
  - round 1: deferred — ledger backlog
  - round 5: recorded false-positive — the tick selects ids only and each poll loads its own order on its own scope, which the parallel design requires; the unused variable the finding cites does not exist
  - v6: re-checked independently @`1816f5f`, disposition upheld
  - 2026-07-29: target closed — row carried to the backlog

### PPW-273 — Production rate-limiter path never exercised in tests

- **What:** Every resilience test set the concurrency knob to the sentinel value that skips the rate
  limiter, which production always builds.
- **History:**
  - v1: found
  - round 1: fixed @`1a240b7` — folded into PPW-248; a finite-limit test exercises the production branch
  - v2: verified @`727a018`

### PPW-274 — Locker list fetched on every init even for Courier-only users

- **What:** The locker list is fetched on every init regardless of delivery method, so a
  courier-only customer pays a request and can see an error message for a map they never open.
- **Evidence:** `UI/…/delivery-step.ts:317`.
- **Suggested fix:** Defer the priming fetch until Easybox is selected.
- **History:**
  - v1: found
  - round 1: deferred — ledger backlog
  - round 3: considered and dropped to bound the round
  - round 5: fixed @`c611a23` — priming runs through the search stream only while Easybox is active
  - v6: verified @`1816f5f` — the change reddened 16 sibling specs, inherent to the helper contract rather than fix-caused
  - 2026-07-29: target closed — row carried to the backlog

### PPW-275 — `TrackingPollOutcome` dead code (declared return type, never constructed)

- **What:** The union is documented as the per-tick return type but nothing ever constructs it, and
  the poll method returns nothing.
- **Evidence:** `Services/Sameday/TrackingPollOutcome.cs:15`.
- **Suggested fix:** Return it from the poll method and assert on it, or delete it and update the design documents.
- **History:**
  - v1: found — cleanup, no skeptic by design
  - round 1: deferred — ledger backlog
  - round 3: fixed @`f3d2508` — the dead union deleted
  - v4: verified @`5fc330b`
  - 2026-07-29: target closed — row carried to the backlog

### PPW-276 — `LogRedactor` defined but never referenced → no HTTP transport tracing

- **What:** The class was written as the redaction point for outbound request and response tracing
  but is never referenced, so no transport tracing exists.
- **Evidence:** `Services/Sameday/LogRedactor.cs:13`.
- **Suggested fix:** Wire it into a transport trace log, or delete it as dead code.
- **History:**
  - v1: found — cleanup
  - round 1: deferred — ledger backlog
  - round 5: fixed @`56320c0` — the unreferenced class removed
  - v6: verified @`1816f5f`
  - 2026-07-29: target closed — row carried to the backlog

### PPW-277 — `TrackingStopRegistry` is a near-copy of `AwbGiveUpRegistry`

- **What:** The two registries differ only in cache-key prefix and entry lifetime, so their one-shot
  duplicate-suppression logic can drift apart.
- **Evidence:** `Services/Sameday/TrackingStopRegistry.cs:9`.
- **Suggested fix:** Extract one registry taking a key prefix and a lifetime, and register two configured instances.
- **History:**
  - v1: found — cleanup
  - round 1: deferred — ledger backlog
  - round 5: fixed @`16d065b` — both share a common base
  - v6: verified @`1816f5f`
  - 2026-07-29: target closed — row carried to the backlog

### PPW-278 — Hand-constructs `StaticShippingService` instead of injecting

- **What:** The service takes a database context and configuration only to construct the static
  shipping service by hand, duplicating wiring that belongs in the container.
- **Evidence:** `Services/SamedayShippingService.cs:35`.
- **Suggested fix:** Register the static service in the container and inject it.
- **History:**
  - v1: found — cleanup
  - round 1: deferred — ledger backlog
  - v5: re-raised, deferral re-affirmed
  - round 5: fixed @`fd59bf2` — the service is injected and the two constructor parameters dropped
  - v6: reopened @`1816f5f` — the composition-root test never resolved the shipping interface, so the new registration was unproven
  - round 6: fixed @`5734021` — the test resolves the interface and asserts its type; dropping the registration reddens it
  - 2026-07-29: verified @`5734021` — recorded deviation: the fixer was also the verifier for this round
  - 2026-07-29: target closed — row carried to the backlog

### PPW-279 — New migration designer snapshots embed stale `StripeClientSecret` 255 vs 512

- **What:** Both new migration designer files record a maximum length of 255 while the master
  snapshot and the live model use 512.
- **Evidence:** `Migrations/20260602190046_…Designer.cs:365`. No runtime effect: EF compares against the master snapshot. Topic hinted by the shared dual-database context.
- **Suggested fix:** Re-scaffold the migrations, or align the designer files to 512, in a bolt-035 groom.
- **History:**
  - v1: found — hinted cleanup
  - round 1: deferred — ledger backlog
  - round 5: deferred again — the drift predates this feature and Stripe secrets are about 66 characters, so the gap is harmless
  - v6: re-affirmed @`1816f5f` — cited files unchanged since `5fc330b`
  - 2026-07-29: target closed — row carried to the backlog

### PPW-280 — Per-print gram weight bare literal `50` colliding with `MinimumGrams`

- **What:** The per-print gram weight is a bare literal sitting beside a named constant of the same
  value, so a reader cannot tell the two apart.
- **Evidence:** `Services/Sameday/ParcelWeight.cs:35`.
- **Suggested fix:** Name the literal, for example as a grams-per-print constant.
- **History:**
  - v1: found — cleanup
  - round 1: deferred — ledger backlog
  - round 3: fixed @`f3d2508` — the constant named
  - v4: verified @`5fc330b`
  - 2026-07-29: target closed — row carried to the backlog

### PPW-281 — "5xx retry unsafe for POST bodies" — `JsonContent` re-serializes each attempt

- **What:** The suspicion that retrying the AWB create cannot resend its body; the content type
  re-serializes the retained object on every attempt, so there is no stream to exhaust.
- **History:**
  - v1: found — refuted by a standalone reproduction, recorded false-positive @`1765918`
  - v1: the one real residue, a missing test on the POST path, folded into PPW-273

### PPW-282 — Easybox `Continue` never re-enables after typing contact (`canContinue` cannot see `form.valid`)

- **What:** The gate is a signal computation that cannot observe the contact form's validity, so
  typing name and phone never re-enabled the button and checkout dead-ended.
- **History:**
  - v3: found — both passes; a regression introduced by round 1's PPW-243 fix that the same-session v2 verification missed
  - round 3: fixed @`aada94b` — the gate reads a mirrored form-validity signal
  - v4: verified @`5fc330b`

### PPW-283 — Slow-Sameday `OperationCanceledException` treated as shutdown → tracking poll loop exits

- **What:** A vendor response slower than the 10-second client timeout throws a cancellation, which
  the poll loop treated as shutdown, so delivery detection stopped until a process restart.
- **History:**
  - v3: found — cert-A only; pre-existing, preserved by the PPW-242 restructure
  - round 3: fixed @`18e7815` — the catch is gated on the stopping token, with a per-poll catch
  - v4: verified @`5fc330b`

### PPW-284 — No per-order guard before the vendor AWB call; DB CAS blocks only the 2nd DB write

- **What:** Nothing guarded the vendor call itself, so a retry or a second replica could create the
  AWB twice; the database check only blocked the second write, not the second billable label.
- **History:**
  - v3: found — both passes, 3 lenses each; residual of PPW-241
  - round 3: fixed @`2c434ad` — a durable per-order claim before the vendor call, approach-checked
  - v4: verified @`5fc330b`
  - v5: crash-window residual re-confirmed and accepted — the skeptic could build no code-only failing trace
  - v6: re-affirmed @`1816f5f` — confirm the vendor's create-idempotency before enabling

### PPW-285 — `isDeliveryComplete()` Easybox gate ignores mandatory contact → stepper skip to payment → 400

- **What:** The Easybox branch ignored the now-mandatory recipient contact, so the stepper unlocked
  payment and the order posted a null shipping address, which the server rejected.
- **History:**
  - v3: found — both passes; a regression from round 1
  - round 3: fixed @`aada94b` — the branch requires the contact
  - v4: verified @`5fc330b`

### PPW-286 — Same OCE-as-shutdown bug drops an AWB dispatch job silently

- **What:** The dispatcher treated a timed-out AWB create as shutdown, swallowing it with no log and
  no in-process retry, so the order waited for the hourly sweep.
- **History:**
  - v3: found — cert-A
  - round 3: fixed @`2c434ad`, with a class sweep over the retry job @`5fc330b`
  - v4: verified @`5fc330b`

### PPW-287 — `LastTrackingSyncAt=UtcNow` fallback + monotonic guard can strand a Shipped order

- **What:** An in-transit poll with no vendor timestamp stored the wall clock, so a later real
  delivered snapshot tripped the monotonic guard and the order never moved to Delivered.
- **History:**
  - v3: found — cert-A, 3 lenses; an elevated interaction of PPW-263 and PPW-265
  - round 3: fixed @`18e7815` — the sync stamp is the poll clock and the monotonic guard is gone
  - v4: verified @`5fc330b`

### PPW-288 — EuPlatesc webhook→AWB enqueue untested (Stripe-only from PPW-245)

- **What:** Only the Stripe webhook's enqueue had a test, so deleting the EuPlatesc one stayed green
  while every order paid that way would get no label.
- **History:**
  - v3: found — both passes; residual of PPW-245
  - round 3: fixed @`19cd0b8` — the EuPlatesc enqueue is asserted
  - v4: verified @`5fc330b`

### PPW-289 — `AwbDispatcher` outcome routing + re-enqueue untested

- **What:** The dispatcher's outcome routing and delayed re-enqueue had no test; only the pure delay
  calculation was covered, so inverting the routing stayed green.
- **Evidence:** `BackgroundJobs/AwbDispatcher.cs:83`.
- **Suggested fix:** Make the re-enqueue schedule and the delay computation testable without a live background service.
- **History:**
  - v3: found — cert-A
  - round 3: deferred — a faithful test needs a background-service harness with an injected delay
  - v4: deferral recorded, not verified
  - v5: re-raised, deferral re-affirmed
  - round 5: fixed @`aa995c1` — the delay computation and the delayed re-enqueue are now unit-tested
  - v6: verified @`1816f5f`

### PPW-290 — `Status != Cancelled` persist guard has no test

- **What:** The load-bearing persist guard had no test, so simplifying it would lose a billed label
  for an order that moved past Paid mid-call, with the suite still green.
- **History:**
  - v3: found — cert-B
  - round 3: fixed @`2c434ad` — a test pins the guard
  - v4: verified @`5fc330b`

### PPW-291 — A `429` surviving retries → permanent GiveUp instead of transient

- **What:** A rate-limit response that survived the retries was mapped to a validation failure and
  gave up permanently instead of being treated as transient.
- **History:**
  - v3: found — cert-B
  - round 3: fixed @`6606c25` — the status maps to the transient exception
  - v4: verified @`5fc330b`

### PPW-292 — ADR-015 + 037 domain model name `awbPayment` as the idempotency key — doc trap

- **What:** Two design documents name the wrong field as the vendor idempotency key while the code
  uses the right one, which is a trap for the next maintainer.
- **History:**
  - v3: found — both passes; plausible, the skeptic refuted the runtime harm
  - round 3: fixed @`ce4941a` — ADR-015 amended to name the field the code uses
  - v4: verified @`5fc330b`

### PPW-293 — Paid→Cancelled orphan billable AWB — no compensating void or operator alert

- **What:** An order cancelled during the vendor call leaves a real billable label; the guard
  correctly refuses the write but only logs it, with no void call and no operator alert.
- **History:**
  - v3: found — both passes; residual of PPW-251
  - round 3: fixed @`2c434ad` — the orphan branch raised to Error, the client has no void endpoint
  - v4: verified @`5fc330b`

### PPW-294 — Easybox address fields uncapped → 28 MB storage-exhaustion DoS

- **What:** Only recipient name and phone are validated for an Easybox order, so a roughly 28 MB
  street value is persisted in the address snapshot and repeated unpaid orders bloat the table.
- **History:**
  - v5: found — security lens, confirmed
  - round 5: fixed @`3764fa0` — the address fields are length-capped in both branches
  - v6: verified @`1816f5f`

### PPW-295 — `AwbLabelUrl` persisted but never surfaced to admin; `GetLabelPdfAsync` has no caller

- **What:** The label link is persisted and the fetch method is built, but no endpoint or DTO returns
  it, so the "downloadable label" goal is stored and never delivered.
- **History:**
  - v5: found — requirements lens, confirmed
  - round 5: fixed @`66c6d50` — the link and both timestamps added to the admin order detail
  - v6: verified @`1816f5f`

### PPW-296 — Stale-claim (crashed-worker) reclaim path untested

- **What:** Only a fresh claim is tested, so dropping the stale-claim clause leaves a crashed
  worker's order skipped forever with the suite green.
- **History:**
  - v5: found — tests lens, confirmed
  - round 5: fixed @`c75003d` — a test seeds a claim older than the lifetime and asserts the reclaim
  - v6: verified @`1816f5f`

### PPW-297 — Claim-release-after-failure untested

- **What:** The claim release after a failure has no test, so a broken release would strand
  in-process retries with every test still green.
- **History:**
  - v5: found — tests lens, confirmed
  - round 5: fixed @`c75003d` — a definitive failure asserts the claim is released
  - v6: verified @`1816f5f`

### PPW-298 — `prefillEasyboxContact` guest/signed-in branches untested

- **What:** The specs clear local storage and supply no signed-in user, so neither prefill branch
  ever runs with data — the guest-state cluster this repo re-finds most.
- **History:**
  - v5: found — hinted, confirmed
  - round 5: fixed @`c611a23` — specs cover guest prefill and malformed stored data
  - v6: verified @`1816f5f`

### PPW-299 — Vendor `pdfLink` > 500 overflows Postgres `varchar(500)` → re-bill loop

- **What:** A label link longer than 500 characters is written verbatim into a 500-character column,
  so on Postgres the write throws after the label is billed and each retry bills again.
- **History:**
  - v5: found — parity and validation lenses, confirmed
  - round 5: fixed @`c75003d` — an over-length link is dropped with a warning so the number still records; the column widened to 2048 by a new migration; approach-checked
  - v6: verified @`1816f5f`

### PPW-300 — Phone regex over-accepts digit-poor input → paid AWB call → GiveUp

- **What:** The phone rule checks character set and length only, so a value like "1-2-3-4" reaches
  the paid AWB call and the vendor rejects it, leaving the order stuck at Paid.
- **History:**
  - v5: found — validation lens, confirmed
  - round 5: fixed @`3764fa0` — the rule requires 9 to 15 real digits
  - v6: verified @`1816f5f`

### PPW-301 — Vendor rejection `ResponseBody` captured but never logged on GiveUp

- **What:** The vendor's field-level reason is captured on the exception but never logged on a
  permanent failure, so operators cannot tell why a label failed.
- **History:**
  - v5: found — observability lens, confirmed
  - round 5: fixed @`56320c0` — the reason is logged, truncated to limit personal data
  - v6: verified @`1816f5f`

### PPW-302 — Systemic tracking failure logged per-order at Warning, never Error

- **What:** A rotated vendor password makes every poll throw, and the broad catch logs it at Warning
  per order, so total failure of delivery detection raises no alert.
- **History:**
  - v5: found — observability lens, confirmed
  - round 5: fixed @`16d065b` — authentication and protocol faults are caught first and logged at Error once per outage window; approach-checked, because a per-order Error would storm
  - v6: verified @`1816f5f`

### PPW-303 — `selectMethod` never resets `selectedLockerId` → Easybox 400 dead-end

- **What:** Switching Easybox, then Courier, then Easybox again leaves the locker signal set while
  the state value is cleared, so payment posts a null locker and the server returns 400.
- **History:**
  - v5: found — frontend lens, confirmed
  - round 5: fixed @`c611a23` — the signal is reset and re-clicking the same method is a no-op; approach-checked
  - v6: verified @`1816f5f`

### PPW-304 — `Enabled=true` root never booted; token-provider ↔ auth-handler DI cycle unverified

- **What:** No test boots the application with the flag on, so a possible resolution cycle between
  the token provider and the authentication handler was never exercised.
- **History:**
  - v5: found — completeness lens, confirmed
  - round 5: fixed @`fd59bf2` — the handler resolves the token provider lazily and the registration moved into one extension; a test resolves the enabled root
  - v6: verified @`1816f5f` — reverting the fix reproduces the cycle, so the flag flip would have thrown on the first vendor call

### PPW-305 — Local `EasyboxLockers.SamedayId` freshness assumed, no sync → permanent GiveUp

- **What:** Label creation trusts the locally stored locker code with no refresh, so a renamed or
  removed locker becomes a permanent give-up with no label.
- **History:**
  - v5: found — completeness lens, confirmed
  - round 5: fixed @`56320c0` — the give-up log now carries the vendor reason, which is the finding's stated alternative; keeping the locker table in step stays a pre-enable operational task
  - v6: verified @`1816f5f`

### PPW-306 — Poll-throttle window equals the tick interval, so orders poll every other tick

- **What:** The eligibility window equals the tick interval, so any positive latency makes each order
  skip alternate ticks.
- **History:**
  - v5: found — confirmed
  - round 5: fixed @`16d065b` — a tick-start clock and an interval-minus-buffer window; approach-checked
  - v6: verified @`1816f5f`

### PPW-307 — Durable claim released on vendor-call timeout — the one unknown-state outcome

- **What:** The claim is released on a vendor-call timeout, the one outcome where the label's state
  is unknown, so a re-attempt inside the claim lifetime risks a second label.
- **History:**
  - v5: found — confirmed
  - round 5: fixed @`c75003d`, extended @`1816f5f` after the micro-review to cover a retryable 5xx on the create call
  - v6: verified @`1816f5f` with a recorded gap — the persist-failure leg had no test
  - round 6: gap closed @`5734021` — the persist failure is driven by closing the connection inside the vendor-call callback
  - 2026-07-29: verified @`5734021` — recorded deviation: the fixer was also the verifier for this round

### PPW-308 — Client Easybox phone check is presence-only, weaker than the server rule

- **What:** The client-side phone check only requires a value, so a digit-poor phone still fails at
  order creation instead of at the form.
- **History:**
  - v5: found — 2 lenses, confirmed
  - round 5: fixed @`c611a23` — the client mirrors the server character set and digit rule on both forms
  - v6: verified @`1816f5f`

### PPW-309 — No response-size cap on untrusted Sameday bodies → out-of-memory risk

- **What:** The vendor HTTP client sets no response-size limit, so a hijacked or faulty multi-gigabyte
  body can exhaust memory, multiplied by the concurrency cap.
- **History:**
  - v5: found — confirmed
  - round 5: fixed @`56320c0` — buffered responses capped at 10 MB; the label PDF still streams
  - v6: verified @`1816f5f`

### PPW-310 — Retry backoff is 1/2/4 s, not the documented 1/4/16 s; the comment is wrong

- **What:** The transport backoff runs at 1, 2 and 4 seconds rather than the documented 1, 4 and 16,
  and the code comment states the documented schedule.
- **History:**
  - v5: found — confirmed
  - round 5: fixed @`56320c0` — an explicit delay generator produces the intended schedule
  - v6: reopened @`1816f5f` — the only retry test does a single retry, 1 second under either schedule, so reverting the fix was invisible
  - round 6: fixed @`5734021` — the delay generator is asserted for attempts 0, 1 and 2 with no real waiting
  - 2026-07-29: verified @`5734021` — recorded deviation: the fixer was also the verifier for this round

### PPW-311 — New `ShippedAt` column has no backfill, so pre-integration Shipped orders never poll

- **What:** An order already Shipped before the integration has no shipped timestamp, so it is never
  polled and never flagged for manual closure.
- **Evidence:** migration `20260602190046:21`; the design document described the column as already existing.
- **Suggested fix:** A one-time backfill in the deploy runbook, if the application ever ships with pre-integration Shipped orders.
- **History:**
  - v5: found — confirmed
  - round 5: deferred — no deployed data exists, so there are no legacy Shipped rows, and the admin transition stamps the column going forward
  - v6: re-affirmed @`1816f5f` — disposition upheld, cited files unchanged since `5fc330b`
  - 2026-07-29: target closed with the deferral standing

### PPW-312 — FR-4 per-attempt logging partial; no correlation id in any background service

- **What:** The retry sweep logs only an aggregate count, and no correlation id is threaded into any
  background service, so a single order's attempts cannot be followed.
- **History:**
  - v5: found — confirmed
  - round 5: fixed @`aa995c1` — the sweep logs the order id per re-enqueue
  - v6: verified @`1816f5f`

### PPW-313 — `prefillEasyboxContact` re-implements the guest-session read

- **What:** The prefill reads the guest session with a hardcoded key and an inline parse instead of
  the shared service, so a key or shape change drifts silently.
- **History:**
  - v5: found — confirmed
  - round 5: fixed @`c611a23` — the prefill uses the shared guest-session accessor
  - v6: verified @`1816f5f`

### PPW-314 — HTTP status classification duplicated 4× and drifting from `SamedayPolicies`

- **What:** Status classification is written out four times in the client and already differs from
  the policy helper, which bounds the server-error range.
- **History:**
  - v5: found — confirmed
  - round 5: fixed @`56320c0` — one chokepoint sharing the policy helper's classification
  - v6: verified @`1816f5f`

### PPW-315 — Parallel multi-order poll fan-out never exercised (every test seeds one order)

- **What:** Every tracking test seeds one order, so the parallel fan-out is never exercised and a
  per-order scope regression would ship green.
- **History:**
  - v5: found — confirmed
  - round 5: fixed @`16d065b` — a two-order tick test exercises the fan-out
  - v6: verified @`1816f5f`

### PPW-316 — Retry sweep tested only on EF InMemory; the fresh-claim skip clause never runs

- **What:** The sweep is tested only on EF InMemory, and every seed leaves the claim empty, so the
  fresh-claim skip clause is short-circuited and never exercised.
- **History:**
  - v5: found — confirmed
  - round 5: fixed @`aa995c1` — the tests moved to SQLite with fresh-claim and stale-claim cases
  - v6: verified @`1816f5f`

### PPW-317 — `setLocker` contact preservation and the Easybox review-step display untested

- **What:** Neither the contact-preserving locker selection nor the Easybox review-step display had
  a test that could fail when the behaviour was reverted.
- **History:**
  - v5: found — confirmed
  - round 5: fixed @`c611a23` — two specs added
  - v6: verified @`1816f5f` with a recorded gap — the review-step assertion could never fail, because Angular renders a missing value as blank
  - round 6: assertion replaced @`5734021` — the spec now seeds a leftover courier address and asserts it is suppressed
  - 2026-07-29: verified @`5734021` — recorded deviation: the fixer was also the verifier for this round

### PPW-318 — Signed-in recipient-name prefill is dead code — the user stream never emits

- **What:** The signed-in name prefill can never run, because the current-user stream is only ever
  set to null and never populated on login.
- **History:**
  - v5: found — confirmed
  - round 5: fixed @`c611a23` — a name claim added on the server and the stream populated on login and session restore
  - v6: reopened @`1816f5f` — nothing asserted the claim or that the stream emits; the guest-and-signed-in prefill cluster
  - round 6: fixed @`5734021` — one backend and three frontend assertions, each proven to redden
  - 2026-07-29: verified @`5734021` — recorded deviation: the fixer was also the verifier for this round

### PPW-319 — Transient locker-search 500 shown as "no easybox in this city"

- **What:** A transient search failure renders as "no easybox found for this city", so the customer
  believes the city is unserved rather than retrying.
- **History:**
  - v5: found — confirmed
  - round 5: fixed @`c611a23` — a distinct error signal with a retry, reset per fetch; approach-checked
  - v6: verified @`1816f5f`

### PPW-320 — `LockerServiceId`/`CourierServiceId` default to placeholder `7`, unvalidated when enabled

- **What:** Both service ids default to a placeholder and are not validated when the feature is
  enabled, so every label would go out under the wrong service.
- **Evidence:** `Validators/SamedaySettingsValidator.cs:38`.
- **Suggested fix:** Validate both ids at boot when the feature is enabled, once the real values from the vendor contract are configured.
- **History:**
  - v5: found — confirmed
  - round 5: deferred — setting the real vendor service ids is a parked pre-enable configuration task and the feature is dormant
  - v6: re-affirmed @`1816f5f` — disposition upheld
  - 2026-07-29: target closed with the deferral standing — named in the pre-enable checklist

### PPW-321 — Dispatcher backoff vs 60-minute sweep double-enqueue window untested

- **What:** The claim is released for the whole dispatcher backoff, up to 3600 seconds, so the hourly
  sweep can enqueue a second in-flight job with the attempt count reset; untested.
- **History:**
  - v5: found — confirmed
  - round 5: fixed @`aa995c1` — the re-enqueue schedule, the lifetime floor and the attempt increment are asserted
  - v6: verified @`1816f5f`

### PPW-322 — Bundled locker-map behaviour shipped with no story or acceptance criteria

- **What:** Three locker-map behaviour changes rode into the diff with no story and no acceptance
  criteria describing them.
- **History:**
  - v5: found — cleanup, no skeptic by design
  - round 5: wont-fix — the behaviour is intentional and is now covered by the round's new specs, so a retrospective story adds nothing
  - v6: disposition upheld @`1816f5f`

### PPW-323 — `AwbCreator` loads the order tracked but only reads it

- **What:** The creator loads the order with change tracking though every write goes through a direct
  update, so the tracking work is pure overhead.
- **History:**
  - v5: found — cleanup
  - round 5: fixed @`c75003d` — the load is untracked
  - v6: verified @`1816f5f` by inspection

### PPW-324 — Tracking poll loads the order tracked but only reads it

- **What:** The poll loads the order with change tracking though it only reads it.
- **History:**
  - v5: found — cleanup
  - round 5: fixed @`16d065b` — the load is untracked
  - v6: verified @`1816f5f` by inspection

### PPW-325 — Recipient phone rule and regex duplicated across the Easybox and Courier blocks

- **What:** The recipient name and phone rules, including the pattern literal, are written twice in
  the same validator.
- **History:**
  - v5: found — cleanup
  - round 5: fixed @`3764fa0` — the rules extracted into one shared method, the pattern hoisted to a constant
  - v6: verified @`1816f5f` by inspection

### PPW-326 — Magic day-count query floors coupled to the registry lifetimes, unnamed

- **What:** The sweep's day-count query floors are unnamed literals implicitly coupled to the
  registry entry lifetimes, so the two can drift apart.
- **History:**
  - v5: found — cleanup
  - round 5: fixed @`aa995c1` — the floor is derived from the registry lifetime
  - v6: verified @`1816f5f` by inspection

### PPW-327 — `DeliveredAt` written to `timestamptz` from a non-UTC offset — Npgsql handles it

- **What:** A suspected write failure for a delivered timestamp carrying a non-zero offset; Npgsql
  maps any-offset values to the UTC instant, so the premise is false.
- **History:**
  - v5: found — refuted at the pass, recorded false-positive
  - v6: re-checked independently @`1816f5f`, disposition upheld

### PPW-328 — `GetLabelPdfAsync` has no production caller

- **What:** The label-fetch method has no production caller.
- **History:**
  - v5: found — cleanup, plausible; tied to the PPW-295 label story
  - round 5: wont-fix — it is the authenticated fetch path kept for a pre-enable admin label-proxy endpoint, because the raw vendor link may need the bearer token
  - v6: disposition upheld @`1816f5f`

### PPW-329 — `ISamedayAuthenticator` singleton captures the transient typed client → handler never rotated

- **What:** The authenticator is registered as a singleton and captures the typed client, which is
  transient, so the client's HTTP handler is never rotated.
- **Evidence:** `Extensions/SamedayServiceCollectionExtensions.cs:37`. Pre-existing; carried into the new extension by the round-5 PPW-304 fix.
- **Suggested fix:** Resolve the client per use, or register the authenticator so its lifetime matches handler rotation.
- **History:**
  - v6: found while verifying the round-5 fixes, not while searching
  - v6: filed at backlog @`1816f5f` — does not re-arm the loop
  - 2026-07-29: target closed — row carried to the backlog

### PPW-330 — `ISamedayClient` doc still claims `NotImplementedException` "until bolt 037"

- **What:** The interface doc comment still says the implementation throws until bolt 037, a claim
  already stripped from the implementing class.
- **Evidence:** `Services/Sameday/ISamedayClient.cs:8`.
- **Suggested fix:** Delete the stale sentence from the interface comment.
- **History:**
  - v6: found while verifying the round-5 fixes
  - v6: filed at backlog @`1816f5f`
  - 2026-07-29: target closed — row carried to the backlog

### PPW-331 — `AwbNumber` is the unclamped sibling of PPW-299's clamp on the same post-billing persist

- **What:** The AWB number column is 100 characters and is written unclamped on the same persist that
  the round-5 PPW-299 fix clamped for the label link.
- **Evidence:** `Services/Sameday/AwbCreator.cs:190`.
- **Suggested fix:** Clamp or validate the number before the persist, as the label-link fix does.
- **History:**
  - v6: found while verifying the round-5 fixes
  - v6: filed at backlog @`1816f5f`
  - 2026-07-29: target closed — row carried to the backlog

### PPW-332 — `Created` outcome reports the unclamped label link while the row stores null

- **What:** The success outcome reports the label link the vendor returned even when it was dropped
  before the write, so the caller and the stored row disagree.
- **Evidence:** `Services/Sameday/AwbCreator.cs:207`.
- **Suggested fix:** Report the value that was actually persisted.
- **History:**
  - v6: found while verifying the round-5 fixes
  - v6: filed at backlog @`1816f5f`
  - 2026-07-29: target closed — row carried to the backlog

### PPW-333 — `MaxRequestsPerSecond` missing from `appsettings.json`, the validator and ddd-02

- **What:** The setting the round-5 PPW-259 fix introduced is absent from the settings file, from the
  settings validator and from the bolt-037 technical design.
- **Evidence:** `Configuration/SamedaySettings.cs:52`.
- **Suggested fix:** Add the key to the settings file and the validator, and record it in the design document.
- **History:**
  - v6: found while verifying the round-5 fixes
  - v6: filed at backlog @`1816f5f`
  - 2026-07-29: target closed — row carried to the backlog

### PPW-334 — PPW-306's 30 s poll buffer is a flat constant, not scaled to the interval

- **What:** The 30-second buffer the round-5 PPW-306 fix introduced is a flat constant rather than a
  fraction of the configured tick interval, so it does not follow a changed interval.
- **Evidence:** `BackgroundJobs/ShipmentTrackingJob.cs:77`.
- **Suggested fix:** Derive the buffer from the configured interval.
- **History:**
  - v6: found while verifying the round-5 fixes
  - v6: filed at backlog @`1816f5f`
  - 2026-07-29: target closed — row carried to the backlog

### PPW-335 — Record accuracy: two wrong counts in resolution-v5, one stale commit in the index

- **What:** Three record errors: resolution-v5 and the index row said the backend suite was 914 where
  the tip measures 916; resolution-v5's prose said 30 fixed where its own map held 41; the index cited
  `66c6d50` rather than the tip `1816f5f`.
- **Evidence:** `reviews/015-sameday-shipping/resolution-v5.md:75` as it stood at `1816f5f`.
- **Suggested fix:** Correct the two counts in the resolution and the commit in the index row.
- **History:**
  - v6: found while verifying the round-5 fixes
  - v6: filed at backlog @`1816f5f`
  - 2026-07-29: target closed — row carried to the backlog
  - 2026-08-11: records converted to the doc contracts — resolution-v5 no longer states either count; the stale index commit is unchanged
