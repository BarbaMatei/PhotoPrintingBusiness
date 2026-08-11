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

| F# | D# | Sev | Title | File | Fix now? |
|---|---|---|---|---|---|
| — | D55 | 🟠 | Easybox address fields uncapped → 28 MB storage-exhaustion DoS | `Validators/…/CreateOrderRequestValidator.cs:26` | no |
| — | D56 | 🟠 | `AwbLabelUrl` persisted but never surfaced to admin; `GetLabelPdfAsync` has no caller | `DTOs/Admin/AdminOrderDtos.cs:44` | no |
| — | D57 | 🟠 | Stale-claim (crashed-worker) reclaim path untested | `Tests/…/AwbCreatorTests.cs:250` | no |
| — | D58 | 🟠 | Claim-release-after-failure untested | `Tests/…/AwbCreatorTests.cs:326` | no |
| — | D59 | 🟠 | `prefillEasyboxContact` guest/signed-in branches untested | `UI/…/delivery-step.spec.ts` | no |
| — | D60 | 🟠 | Vendor `pdfLink` > 500 overflows Postgres `varchar(500)` → re-bill loop | `Services/Sameday/AwbCreator.cs:156` | no |
| — | D61 | 🟠 | Phone regex over-accepts digit-poor input → paid AWB call → GiveUp | `Validators/…/CreateOrderRequestValidator.cs:28` | no |
| — | D62 | 🟠 | Vendor rejection `ResponseBody` captured but never logged on GiveUp | `Services/Sameday/AwbCreator.cs:136` | no |
| — | D63 | 🟠 | Systemic tracking failure logged per-order at Warning, never Error | `BackgroundJobs/ShipmentTrackingJob.cs:148` | no |
| — | D64 | 🟠 | `selectMethod` never resets `selectedLockerId` → Easybox 400 dead-end | `UI/…/delivery-step.ts:399` | no |
| — | D65 | 🟠 | `Enabled=true` root never booted; token-provider ↔ auth-handler DI cycle unverified | `Program.cs:146` | no |
| — | D66 | 🟠 | Local `EasyboxLockers.SamedayId` freshness assumed, no sync → permanent GiveUp | `Services/Sameday/OrderToAwbRequestMapper.cs:48` | no |
| — | D67 | 🟡 | Poll-throttle window equals the tick interval, so orders poll every other tick | `BackgroundJobs/ShipmentTrackingJob.cs:74` | no |
| — | D68 | 🟡 | Durable claim released on vendor-call timeout — the one unknown-state outcome | `Services/Sameday/AwbCreator.cs:90` | no |
| — | D69 | 🟡 | Client Easybox phone check is presence-only, weaker than the server rule | `UI/…/delivery-step.ts:321` | no |
| — | D70 | 🟡 | No response-size cap on untrusted Sameday bodies → out-of-memory risk | `Services/Sameday/SamedayClient.cs:218` | no |
| — | D71 | 🟡 | Retry backoff is 1/2/4 s, not the documented 1/4/16 s; the comment is wrong | `Services/Sameday/SamedayPolicies.cs:50` | no |
| — | D72 | 🟡 | New `ShippedAt` column has no backfill, so pre-integration Shipped orders never poll | `Migrations/20260602190046:21` | no |
| — | D73 | 🟡 | FR-4 per-attempt logging partial; no correlation id in any background service | `BackgroundJobs/AwbRetryJob.cs:95` | no |
| — | D74 | 🟡 | `prefillEasyboxContact` re-implements the guest-session read | `UI/…/delivery-step.ts:382` | no |
| — | D75 | 🟡 | HTTP status classification duplicated 4× and drifting from `SamedayPolicies` | `Services/Sameday/SamedayClient.cs:65` | no |
| — | D76 | 🟡 | Parallel multi-order poll fan-out never exercised (every test seeds one order) | `Tests/…/ShipmentTrackingJobTests.cs:117` | no |
| — | D77 | 🟡 | Retry sweep tested only on EF InMemory; the fresh-claim skip clause never runs | `Tests/…/AwbRetryJobTests.cs:23` | no |
| — | D78 | 🟡 | `setLocker` contact preservation and the Easybox review-step display untested | `UI/…/checkout-state.service.spec.ts:45` | no |
| — | D79 | 🟡 | Signed-in recipient-name prefill is dead code — the user stream never emits | `UI/…/delivery-step.ts:392` | no |
| — | D80 | 🟡 | Transient locker-search 500 shown as "no easybox in this city" | `UI/…/delivery-step.ts:371` | no |
| — | D81 | 🟡 | `LockerServiceId`/`CourierServiceId` default to placeholder `7`, unvalidated when enabled | `Validators/SamedaySettingsValidator.cs:38` | no |
| — | D82 | 🟡 | Dispatcher backoff vs 60-minute sweep double-enqueue window untested | `Services/Sameday/AwbCreator.cs:129` | no |
| — | D83 | ⚪ | Bundled locker-map behaviour shipped with no story or acceptance criteria | `UI/…/delivery-step.ts:366` | no |
| — | D84 | ⚪ | `AwbCreator` loads the order tracked but only reads it | `Services/Sameday/AwbCreator.cs:42` | no |
| — | D85 | ⚪ | Tracking poll loads the order tracked but only reads it | `BackgroundJobs/ShipmentTrackingJob.cs:129` | no |
| — | D86 | ⚪ | Recipient phone rule and regex duplicated across the Easybox and Courier blocks | `Validators/…/CreateOrderRequestValidator.cs:40` | no |
| — | D87 | ⚪ | Magic day-count query floors coupled to the registry lifetimes, unnamed | `BackgroundJobs/AwbRetryJob.cs:252` | no |
| id41 | D89 | ⚪ | `GetLabelPdfAsync` has no production caller | — | no |
| id24, id25 | D45 | 🔴 | No per-order guard before the vendor AWB call; DB CAS blocks only the 2nd DB write | `Services/Sameday/AwbCreator.cs:69` | no |
| id39 | D50 | 🟠 | `AwbDispatcher` outcome routing + re-enqueue untested | `BackgroundJobs/AwbDispatcher.cs:83` | no |
| id44 | D23 | 🟡 | Dual-DB parity: migrations + `timestamptz` CAS never run on Postgres | `Tests/…/OrderSamedayFieldsTests.cs:21` | no |
| id34 | D29 | 🟡 | Polly retry has no `OnRetry` callback → transient retries invisible | `Services/Sameday/SamedayPolicies.cs` (retry) | no |
| id12 | D39 | ⚪ | Hand-constructs `StaticShippingService` instead of injecting | `Services/SamedayShippingService.cs:35` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| `DeliveredAt` carrying a non-zero offset is rejected on a `timestamptz` write (recorded as D88) | Npgsql maps any-offset `DateTimeOffset` values to the UTC instant; the restriction applies to `DateTime` only, so the premise is false. |

## Notes for the fixer

- One blinded full-manifest pass over the frozen fix code, run as a single-pass certification with the owner's recorded approval. No serious defect survives: 0 High, 0 fix-caused regression, 0 reopened fix, so the outcome is certified and the verdict is capped at `approve-with-followups`.
- Nothing on this list can be reached in production. Both flags are `false`, so the whole list is a pre-enable checklist. Work it before flipping either flag, not now.
- The last five rows are already-decided items this pass re-raised with their prior decisions attached. D50, D23, D29 and D39 stand as deferred. D45 is the accepted vendor-idempotency residual: the skeptic could build no code-only failing trace, so what remains is the unverified vendor contract. Confirm Sameday's create-idempotency before enabling (ADR-015).
- Adversarial verification: 30 confirmed with a trace, 3 plausible, 4 re-raises, 1 refuted. Highest cross-lens agreement was 2, so most rows rest on one lens and one skeptic.
- Trap in this pass's own records: the frontmatter counts 17 medium, 19 low and 6 cleanup, while the D# list holds 12 medium, 16 low and 6 cleanup. The counts were never reconciled and `metrics.jsonl` carries the same 17/19/6. Trust the D# list.
- If the round takes the medium rows, D60 and D65 are the two that change mechanisms — an over-length label link and the enabled-root wiring — so both want an adversarial design check before implementation.
