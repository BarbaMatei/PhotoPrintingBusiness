---
type: review
target: 015-sameday-shipping
version: 5
supersedes: 4
commit: 5fc330b
branch: feat/bolt-036-sameday-api-client
pass-type: certification
date: 2026-07-28
lenses: [correctness, security, requirements, quality, tests-coverage, race, db-parity, input-validation, observability, frontend-ux, completeness-critic]
lenses-not-run: []
verdict: approve-with-followups
blockers: []
findings: { high: 0, medium: 17, low: 19, cleanup: 6, refuted: 1 }
tests: { dotnet: "898/898 (+10 skipped MinIO)", frontend: "452/452" }
---

# Review v5 — 015-sameday-shipping

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-294 | 🟠 | Easybox address fields uncapped → 28 MB storage-exhaustion DoS | `Validators/…/CreateOrderRequestValidator.cs:26` | no |
| PPW-295 | 🟠 | `AwbLabelUrl` persisted but never surfaced to admin; `GetLabelPdfAsync` has no caller | `DTOs/Admin/AdminOrderDtos.cs:44` | no |
| PPW-296 | 🟠 | Stale-claim (crashed-worker) reclaim path untested | `Tests/…/AwbCreatorTests.cs:250` | no |
| PPW-297 | 🟠 | Claim-release-after-failure untested | `Tests/…/AwbCreatorTests.cs:326` | no |
| PPW-298 | 🟠 | `prefillEasyboxContact` guest/signed-in branches untested | `UI/…/delivery-step.spec.ts` | no |
| PPW-299 | 🟠 | Vendor `pdfLink` > 500 overflows Postgres `varchar(500)` → re-bill loop | `Services/Sameday/AwbCreator.cs:156` | no |
| PPW-300 | 🟠 | Phone regex over-accepts digit-poor input → paid AWB call → GiveUp | `Validators/…/CreateOrderRequestValidator.cs:28` | no |
| PPW-301 | 🟠 | Vendor rejection `ResponseBody` captured but never logged on GiveUp | `Services/Sameday/AwbCreator.cs:136` | no |
| PPW-302 | 🟠 | Systemic tracking failure logged per-order at Warning, never Error | `BackgroundJobs/ShipmentTrackingJob.cs:148` | no |
| PPW-303 | 🟠 | `selectMethod` never resets `selectedLockerId` → Easybox 400 dead-end | `UI/…/delivery-step.ts:399` | no |
| PPW-304 | 🟠 | `Enabled=true` root never booted; token-provider ↔ auth-handler DI cycle unverified | `Program.cs:146` | no |
| PPW-305 | 🟠 | Local `EasyboxLockers.SamedayId` freshness assumed, no sync → permanent GiveUp | `Services/Sameday/OrderToAwbRequestMapper.cs:48` | no |
| PPW-306 | 🟡 | Poll-throttle window equals the tick interval, so orders poll every other tick | `BackgroundJobs/ShipmentTrackingJob.cs:74` | no |
| PPW-307 | 🟡 | Durable claim released on vendor-call timeout — the one unknown-state outcome | `Services/Sameday/AwbCreator.cs:90` | no |
| PPW-308 | 🟡 | Client Easybox phone check is presence-only, weaker than the server rule | `UI/…/delivery-step.ts:321` | no |
| PPW-309 | 🟡 | No response-size cap on untrusted Sameday bodies → out-of-memory risk | `Services/Sameday/SamedayClient.cs:218` | no |
| PPW-310 | 🟡 | Retry backoff is 1/2/4 s, not the documented 1/4/16 s; the comment is wrong | `Services/Sameday/SamedayPolicies.cs:50` | no |
| PPW-311 | 🟡 | New `ShippedAt` column has no backfill, so pre-integration Shipped orders never poll | `Migrations/20260602190046:21` | no |
| PPW-312 | 🟡 | FR-4 per-attempt logging partial; no correlation id in any background service | `BackgroundJobs/AwbRetryJob.cs:95` | no |
| PPW-313 | 🟡 | `prefillEasyboxContact` re-implements the guest-session read | `UI/…/delivery-step.ts:382` | no |
| PPW-314 | 🟡 | HTTP status classification duplicated 4× and drifting from `SamedayPolicies` | `Services/Sameday/SamedayClient.cs:65` | no |
| PPW-315 | 🟡 | Parallel multi-order poll fan-out never exercised (every test seeds one order) | `Tests/…/ShipmentTrackingJobTests.cs:117` | no |
| PPW-316 | 🟡 | Retry sweep tested only on EF InMemory; the fresh-claim skip clause never runs | `Tests/…/AwbRetryJobTests.cs:23` | no |
| PPW-317 | 🟡 | `setLocker` contact preservation and the Easybox review-step display untested | `UI/…/checkout-state.service.spec.ts:45` | no |
| PPW-318 | 🟡 | Signed-in recipient-name prefill is dead code — the user stream never emits | `UI/…/delivery-step.ts:392` | no |
| PPW-319 | 🟡 | Transient locker-search 500 shown as "no easybox in this city" | `UI/…/delivery-step.ts:371` | no |
| PPW-320 | 🟡 | `LockerServiceId`/`CourierServiceId` default to placeholder `7`, unvalidated when enabled | `Validators/SamedaySettingsValidator.cs:38` | no |
| PPW-321 | 🟡 | Dispatcher backoff vs 60-minute sweep double-enqueue window untested | `Services/Sameday/AwbCreator.cs:129` | no |
| PPW-322 | ⚪ | Bundled locker-map behaviour shipped with no story or acceptance criteria | `UI/…/delivery-step.ts:366` | no |
| PPW-323 | ⚪ | `AwbCreator` loads the order tracked but only reads it | `Services/Sameday/AwbCreator.cs:42` | no |
| PPW-324 | ⚪ | Tracking poll loads the order tracked but only reads it | `BackgroundJobs/ShipmentTrackingJob.cs:129` | no |
| PPW-325 | ⚪ | Recipient phone rule and regex duplicated across the Easybox and Courier blocks | `Validators/…/CreateOrderRequestValidator.cs:40` | no |
| PPW-326 | ⚪ | Magic day-count query floors coupled to the registry lifetimes, unnamed | `BackgroundJobs/AwbRetryJob.cs:252` | no |
| id41 | PPW-328 | ⚪ | `GetLabelPdfAsync` has no production caller | — | no |
| id24, id25 | PPW-284 | 🔴 | No per-order guard before the vendor AWB call; DB CAS blocks only the 2nd DB write | `Services/Sameday/AwbCreator.cs:69` | no |
| id39 | PPW-289 | 🟠 | `AwbDispatcher` outcome routing + re-enqueue untested | `BackgroundJobs/AwbDispatcher.cs:83` | no |
| id44 | PPW-262 | 🟡 | Dual-DB parity: migrations + `timestamptz` CAS never run on Postgres | `Tests/…/OrderSamedayFieldsTests.cs:21` | no |
| id34 | PPW-268 | 🟡 | Polly retry has no `OnRetry` callback → transient retries invisible | `Services/Sameday/SamedayPolicies.cs` (retry) | no |
| id12 | PPW-278 | ⚪ | Hand-constructs `StaticShippingService` instead of injecting | `Services/SamedayShippingService.cs:35` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| `DeliveredAt` carrying a non-zero offset is rejected on a `timestamptz` write (recorded as PPW-327) | Npgsql maps any-offset `DateTimeOffset` values to the UTC instant; the restriction applies to `DateTime` only, so the premise is false. |

## Notes for the fixer

- One blinded full-manifest pass over the frozen fix code, run as a single-pass certification with the owner's recorded approval. No serious defect survives: 0 High, 0 fix-caused regression, 0 reopened fix, so the outcome is certified and the verdict is capped at `approve-with-followups`.
- Nothing on this list can be reached in production. Both flags are `false`, so the whole list is a pre-enable checklist. Work it before flipping either flag, not now.
- The last five rows are already-decided items this pass re-raised with their prior decisions attached. PPW-289, PPW-262, PPW-268 and PPW-278 stand as deferred. PPW-284 is the accepted vendor-idempotency residual: the skeptic could build no code-only failing trace, so what remains is the unverified vendor contract. Confirm Sameday's create-idempotency before enabling.
- Adversarial verification: 30 confirmed with a trace, 3 plausible, 4 re-raises, 1 refuted. Highest cross-lens agreement was 2, so most rows rest on one lens and one skeptic.
- Trap in this pass's own records: the frontmatter counts 17 medium, 19 low and 6 cleanup, while the D# list holds 12 medium, 16 low and 6 cleanup. The counts were never reconciled and `metrics.jsonl` carries the same 17/19/6. Trust the D# list.
- If the round takes the medium rows, PPW-299 and PPW-304 are the two that change mechanisms — an over-length label link and the enabled-root wiring — so both want an adversarial design check before implementation.
