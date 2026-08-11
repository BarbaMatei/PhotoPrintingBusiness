---
type: review
target: 015-sameday-shipping
version: 1
supersedes: null
commit: 1765918
branch: feat/bolt-036-sameday-api-client
pass-type: discovery
date: 2026-07-27
lenses: [correctness, security, requirements, quality, tests-coverage, race, db-parity, input-validation, observability, frontend-ux, completeness-critic]
lenses-not-run: []
verdict: request-changes
blockers: [PPW-240, PPW-241, PPW-242, PPW-243, PPW-244]
findings: { high: 5, medium: 14, low: 16, cleanup: 6, refuted: 1 }
tests: { dotnet: "862/862 (+10 skipped MinIO)", frontend: "448/448" }
---

# Review v1 — 015-sameday-shipping

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-240 | 🔴 | AWB vendor idempotency key wired to constant `PickupPointId`, not per-order (breaks ADR-015) | `Services/Sameday/SamedayClient.cs:104` | yes |
| PPW-241 | 🔴 | Concurrent AWB creators double-create (check-then-act, no DB guard) | `Services/Sameday/AwbCreator.cs:69` | yes |
| PPW-242 | 🔴 | One `DbContext` shared across concurrent tracking-poll tasks → tick faults | `BackgroundJobs/ShipmentTrackingJob.cs:87` | yes |
| PPW-243 | 🔴 | Easybox AWB carries null recipient name/phone (dead null-guard) → permanent give-up | `Services/Sameday/OrderToAwbRequestMapper.cs:60` | yes |
| PPW-244 | 🔴 | Easybox locker `SamedayId` dropped + wire `Service` hardcoded 7 → unroutable / wrong service | `Services/Sameday/OrderToAwbRequestMapper.cs:66` | yes |
| PPW-245 | 🟠 | Webhook→AWB enqueue wiring untested (green suite hides removal) | `Controllers/WebhooksController.cs:192` | yes |
| PPW-246 | 🟠 | ADR-016 CAS race-lost test seeds Cancelled → never reaches the CAS | `Tests/…/ShipmentTrackingJobTests.cs:136` | yes |
| PPW-247 | 🟠 | `AwbDispatcher` backoff off-by-one: last entry unreachable | `BackgroundJobs/AwbDispatcher.cs:124` | yes |
| PPW-248 | 🟠 | Rate limiter re-created per request → throttle inert + timer leak | `Services/Sameday/SamedayPolicies.cs:44` | yes |
| PPW-249 | 🟠 | Admin `→Shipped` nulls machine-created `AwbNumber` when field omitted | `Services/AdminOrderService.cs:117` | yes |
| PPW-250 | 🟠 | AWB enqueue in webhooks only, not the transition hook → admin-Paid never creates AWB | `Services/AdminOrderService.cs:113` | yes |
| PPW-251 | 🟠 | AWB persisted onto an order cancelled mid-call (no re-check before save) | `Services/Sameday/AwbCreator.cs:93` | yes |
| PPW-252 | 🟠 | Courier recipient name/phone/street/number unvalidated → AWB give-up | `Validators/Payments/CreateOrderRequestValidator.cs:27` | yes |
| PPW-253 | 🟠 | `SamedayUnreachableException` swallowed with no log → tracking stalls silently | `BackgroundJobs/ShipmentTrackingJob.cs:128` | yes |
| PPW-254 | 🟠 | Created AWB number not logged before `SaveChanges` → orphan billable AWB invisible | `Services/Sameday/AwbCreator.cs:96` | yes |
| PPW-255 | 🟠 | `AwbCreator` test green even if `SaveChangesAsync` removed (identity-map read) | `Tests/…/AwbCreatorTests.cs:141` | yes |
| PPW-256 | 🟠 | Admin `ShippedAt`/`DeliveredAt` assignment untested | `Services/AdminOrderService.cs:119` | yes |
| PPW-257 | 🟠 | Clearing city search can permanently kill the locker-search pipe on transient error | `UI/…/delivery-step.ts:332` | yes |
| PPW-258 | 🟠 | Init priming `getLockers('')` races city-search `switchMap`, overwrites filter | `UI/…/delivery-step.ts:317` | yes |
| PPW-259 | 🟡 | `MaxConcurrentSamedayCalls` overloaded as concurrency gate AND req/s rate limit | `Services/Sameday/SamedayResilienceHandler.cs:25` | no |
| PPW-260 | 🟡 | Raw vendor error body in exception + logged at Error (conditional PII) | `Services/Sameday/SamedayClient.cs:140` | no |
| PPW-261 | 🟡 | `AwbLabelUrl` migration hardcodes `text` → unbounded on Postgres, diverges from model | `Migrations/20260602141429_AddSamedayOrderFields.cs:23` | no |
| PPW-262 | 🟡 | Dual-DB parity: migrations + `timestamptz` CAS never run on Postgres | `Tests/…/OrderSamedayFieldsTests.cs:21` | no |
| PPW-263 | 🟡 | Tracking `observedAt` fabricated to `UtcNow` when vendor omits timestamps → wrong `DeliveredAt` | `Services/Sameday/SamedayClient.cs:224` | no |
| PPW-264 | 🟡 | `expire_at_utc` bound without UTC guarantee (non-UTC host shifts token expiry) | `Services/Sameday/SamedayClient.cs:90` | no |
| PPW-265 | 🟡 | Monotonic guard can drop a legitimate `Delivered` snapshot (untested) | `BackgroundJobs/ShipmentTrackingJob.cs:132` | no |
| PPW-266 | 🟡 | Non-delivered tracking write not monotonic across replicas | `BackgroundJobs/ShipmentTrackingJob.cs:182` | no |
| PPW-267 | 🟡 | AWB-enqueue logged at Debug, below Information floor → never emits | `Services/Sameday/AwbCreationNotifier.cs:32` | no |
| PPW-268 | 🟡 | Polly retry has no `OnRetry` callback → transient retries invisible | `Services/Sameday/SamedayPolicies.cs` (retry) | no |
| PPW-269 | 🟡 | Documented `/health` `sameday:enabled` field not delivered | `HealthChecks/HealthCheckResponseWriter.cs:36` | no |
| PPW-270 | 🟡 | `GenerateAwbAsync` returns stale "generate manually" + pre-037 comment | `Services/SamedayShippingService.cs:52` | no |
| PPW-271 | 🟡 | `AwbCreationRequest` documented as validated value object but has no validation | `Services/Sameday/AwbCreationRequest.cs:11` | no |
| PPW-272 | 🟡 | Tracking job re-queries already-loaded order; `inWindow` tracked-but-unused | `BackgroundJobs/ShipmentTrackingJob.cs:172` | no |
| PPW-273 | 🟡 | Production rate-limiter path never exercised in tests | `Tests/…/SamedayPoliciesTests.cs:40` | no |
| PPW-274 | 🟡 | Locker list fetched on every init even for Courier-only users | `UI/…/delivery-step.ts:317` | no |
| PPW-275 | ⚪ | `TrackingPollOutcome` dead code (declared return type, never constructed) | `Services/Sameday/TrackingPollOutcome.cs:15` | no |
| PPW-276 | ⚪ | `LogRedactor` defined but never referenced → no HTTP transport tracing | `Services/Sameday/LogRedactor.cs:13` | no |
| PPW-277 | ⚪ | `TrackingStopRegistry` is a near-copy of `AwbGiveUpRegistry` | `Services/Sameday/TrackingStopRegistry.cs:9` | no |
| PPW-278 | ⚪ | Hand-constructs `StaticShippingService` instead of injecting | `Services/SamedayShippingService.cs:35` | no |
| PPW-279 | ⚪ | New migration designer snapshots embed stale `StripeClientSecret` 255 vs 512 | `Migrations/20260602190046_…Designer.cs:365` | no |
| PPW-280 | ⚪ | Per-print gram weight bare literal `50` colliding with `MinimumGrams` | `Services/Sameday/ParcelWeight.cs:35` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| Retrying `POST /api/awb` cannot resend its body, so a transient 5xx becomes a hard failure | `SamedayClient.cs:122` builds the body with `JsonContent.Create`, which re-serializes the retained object on every attempt; the skeptic replayed it three times with an identical body. The one real residue, a missing POST-path test, folds into PPW-273. |

## Notes for the fixer

- Nothing here is live. `Sameday:Enabled` and `Sameday:Jobs:Enabled` are both `false`, so every finding is a gate on turning Sameday on, not a production incident today.
- Fix PPW-240 first. PPW-241 and PPW-254 partly depend on it. The fix is a per-order vendor reference plus a guarded write; in-process deduplication cannot stop a second replica.
- PPW-243, PPW-244, PPW-252 and PPW-271 are one recipient-mapping cluster. Decide once whether recipient validation lives at checkout or in the mapper, then apply it consistently. PPW-243's dead guard hinges on the order service injecting a non-null empty address snapshot.
- PPW-242 mirrors the dispatcher pattern that is already right: open a scope and a context per order inside the poll.
- Five findings are themselves tests that pass for the wrong reason — PPW-245, PPW-246, PPW-255, PPW-256, PPW-273. Fixing them means the new test must go red when its invariant is broken.
- Every fix needs a regression test that fails when the fix is reverted.
- PPW-259 to PPW-280 enter the ledger at backlog and do not gate this round. PPW-261, PPW-262 and PPW-279 are the dual-database ones worth grooming before enabling on Postgres.
- This is a full-loop-tier target: a new external courier interface, two migrations, multi-replica concurrency and the paid-order path. It owes a certification pass before closure.
