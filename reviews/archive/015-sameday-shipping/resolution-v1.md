---
type: resolution
target: 015-sameday-shipping
review-version: 1
status: resolved
fixed_commit: 835e932
closed: 2026-07-27
findings:
  F1:  { status: fixed, commit: edd49f7, note: "clientInternalReference = OrderNumber (per-order), not the shop-wide PickupPointId; wire test pins it. Design got an adversarial approach-check." }
  F2:  { status: fixed, commit: edd49f7, note: "guarded ExecuteUpdate (WHERE AwbNumber IS NULL AND Status != Cancelled) — one writer wins the DB row; per-order key (F1) covers the vendor side. No void API, so a genuine orphan is logged." }
  F3:  { status: fixed, commit: d6744f1, note: "per-order scope+DbContext opened AFTER the semaphore gate (live contexts <= MaxConcurrentSamedayCalls); tick reads projected AsNoTracking. Approach-checked." }
  F4:  { status: fixed, commit: 835e932, note: "Easybox contact (name+phone) captured at checkout (frontend 835e932, prefilled from account/guest), required server-side (validator e8d4b53), and re-validated in the mapper (edd49f7) — a blank recipient now fails locally as GiveUp instead of reaching Sameday null." }
  F5:  { status: fixed, commit: edd49f7, note: "AwbCreationRequest carries the delivery-type ServiceId + locker SamedayId; client sends service + lockerLastMile. Service ids are now per-merchant config (LockerServiceId/CourierServiceId) — see decisions." }
  F6:  { status: fixed, commit: e8d4b53, note: "RecordingAwbCreationNotifier + StripeWebhook_PaymentSucceeded_EnqueuesAwbCreation — deleting NotifyPaidAsync now reddens the suite." }
  F7:  { status: fixed, commit: d6744f1, note: "CAS test now seeds a genuinely Shipped order and advances it out of Shipped mid-poll via a separate scope, so the ExecuteUpdate WHERE Status==Shipped actually runs and returns 0; removing the predicate reddens it." }
  F8:  { status: fixed, commit: ef8d323, note: "guard was attempt >= Length (skipped the last delay); extracted NextDispatchDelay(attempt, backoffs) as a pure tested function, exhausted only past the last entry." }
  F9:  { status: fixed, commit: 1a240b7, note: "one SlidingWindowRateLimiter per handler (was new-per-call → inert + timer leak), surfaced by BuildPipeline and disposed via Dispose(bool). QueueLimit kept unbounded — jobs are semaphore-bounded and the request path doesn't route through this limiter today (noted)." }
  F10: { status: fixed, commit: 010c6dc, note: "admin ->Shipped only overwrites AwbNumber/TrackingUrl when the field is non-empty; test seeds a machine AWB and asserts it survives an omitted field." }
  F11: { status: fixed, commit: 010c6dc, note: "admin AwaitingPayment->Paid stamps PaidAt and calls IAwbCreationNotifier.NotifyPaidAsync (Null no-op when jobs off). Owner decision: no new cash/COD UI. Does NOT fire the confirmation email — see decisions." }
  F12: { status: fixed, commit: edd49f7, note: "guard uses Status != Cancelled (not == Paid) per the approach-check: a mid-call Paid->Printing keeps its label instead of being orphaned + stranded." }
  F13: { status: fixed, commit: e8d4b53, note: "courier RecipientName/Phone/Street/Number/City/County/PostalCode NotEmpty + MaximumLength + phone regex; tests cover blank + overlong + bad-format." }
  F14: { status: fixed, commit: d6744f1, note: "SamedayUnreachableException in the tracking poll now logs a Warning with order id before returning." }
  F15: { status: fixed, commit: edd49f7, note: "the billable AwbNumber is logged BEFORE the DB write; a persist throw returns transient RetryLater and is tested (drop-table injection)." }
  F16: { status: fixed, commit: edd49f7, note: "AwbCreatorTests moved to SQLite; the happy-path read-back goes through a FRESH context, so a missing persist reddens it." }
  F17: { status: fixed, commit: 010c6dc, note: "tests assert ShippedAt on ->Shipped and DeliveredAt on ->Delivered (the tracking job depends on ShippedAt != null)." }
  F18: { status: fixed, commit: 835e932, note: "clearing the city search no longer tears down the pipe — the fetch is wrapped in catchError(() => of([])) inside switchMap." }
  F19: { status: fixed, commit: 835e932, note: "the init prime is a startWith('') inside the same switchMap stream (immediate, but cancellable), replacing the rival standalone subscription that could overwrite a filtered result." }
  F32: { status: fixed, commit: edd49f7, note: "folded into F4 — the mapper now rejects a blank recipient name/phone with ArgumentException -> GiveUp." }
  F34: { status: fixed, commit: 1a240b7, note: "folded into F9 — a finite-limit test now exercises the production limiter branch the suite previously skipped (int.MaxValue)." }
  F20: { status: deferred, commit: null, note: "ledger backlog (Low) — not in this fix round" }
  F21: { status: deferred, commit: null, note: "ledger backlog (Low) — not in this fix round" }
  F22: { status: deferred, commit: null, note: "ledger backlog (Low, dual-DB parity) — not in this fix round" }
  F23: { status: deferred, commit: null, note: "ledger backlog (Low, dual-DB parity) — not in this fix round" }
  F24: { status: deferred, commit: null, note: "ledger backlog (Low) — not in this fix round" }
  F25: { status: deferred, commit: null, note: "ledger backlog (Low) — not in this fix round" }
  F26: { status: deferred, commit: null, note: "ledger backlog (Low) — not in this fix round" }
  F27: { status: deferred, commit: null, note: "ledger backlog (Low, plausible) — not in this fix round" }
  F28: { status: deferred, commit: null, note: "ledger backlog (Low) — not in this fix round" }
  F29: { status: deferred, commit: null, note: "ledger backlog (Low) — not in this fix round" }
  F30: { status: deferred, commit: null, note: "ledger backlog (Low) — not in this fix round" }
  F31: { status: deferred, commit: null, note: "ledger backlog (Low) — not in this fix round" }
  F33: { status: deferred, commit: null, note: "ledger backlog (Low) — the tracking-job reload was removed as a side effect of F3 (delivered email now uses the loaded order), but the finding itself stays backlog" }
  F35: { status: deferred, commit: null, note: "ledger backlog (Low) — not in this fix round" }
  F36: { status: deferred, commit: null, note: "ledger backlog (Cleanup) — not in this fix round" }
  F37: { status: deferred, commit: null, note: "ledger backlog (Cleanup) — not in this fix round" }
  F38: { status: deferred, commit: null, note: "ledger backlog (Cleanup) — not in this fix round" }
  F39: { status: deferred, commit: null, note: "ledger backlog (Cleanup) — not in this fix round" }
  F40: { status: deferred, commit: null, note: "ledger backlog (Cleanup) — not in this fix round" }
  F41: { status: deferred, commit: null, note: "ledger backlog (Cleanup) — not in this fix round" }
  F42: { status: false-positive, commit: null, note: "refuted at review — JsonContent re-serializes per attempt; test-gap folded into F34" }
---

# Resolution v1 — 015-sameday-shipping

Fixer response to [review-v1.md](review-v1.md). All 19 serious findings (🔴 F1–F5, 🟠 F6–F19) are
`fixed` with regression tests, plus F32/F34 folded into their clusters. The 22 Low/Cleanup
(F20–F41) stay in the ledger `backlog`; F42 is a recorded false-positive. **Status: `resolved`** —
handed back for re-review.

**Tests:** backend `892/892` (+10 skipped MinIO), frontend `450/450`, both green at `835e932`.

**Process:** the two blocker clusters were design changes on the fixer trigger list, so each got an
adversarial approach-check **before** implementation — the AWB write path (F1/F2/F12/F15) and the
tracking-scope + limiter (F3/F9). Both returned SOUND-WITH-CHANGES; the required changes were folded
in (write guard `!= Cancelled` not `== Paid`; read-back on `affected==0` to avoid a void-warning on a
converged AWB; open the per-order scope after the gate; surface + dispose the single limiter; add the
finite-limit test). The full fix diff then got a two-agent fresh-eyes micro-review, which found one
regression (fixed: F4 recap render) and one coverage gap (fixed: F15 persist-failed test).

## Fix commits

| Commit | Cluster | Findings |
|---|---|---|
| `edd49f7` | AWB idempotency + guarded write + recipient/service mapping | F1, F2, F4, F5, F12, F15, F16, F32 |
| `d6744f1` | Tracking poll per-order scope + swallowed-outage log + real CAS test | F3, F7, F14 |
| `1a240b7` | Rate limiter built once + disposed + finite-limit test | F9, F34 |
| `ef8d323` | Dispatcher backoff off-by-one | F8 |
| `010c6dc` | Admin Paid enqueue + preserve AWB + timestamp tests | F10, F11, F17 |
| `e8d4b53` | Recipient server-validation + webhook-enqueue test | F4, F6, F13 |
| `835e932` | Easybox contact capture + resilient locker search + recap fix | F4, F18, F19 |

## Findings

| ID | Sev | Status | Commit | How |
|----|-----|--------|--------|-----|
| F1 | 🔴 | fixed | `edd49f7` | per-order `OrderNumber` reference |
| F2 | 🔴 | fixed | `edd49f7` | guarded `ExecuteUpdate` write |
| F3 | 🔴 | fixed | `d6744f1` | per-order `DbContext` after the gate |
| F4 | 🔴 | fixed | `835e932` | Easybox contact captured + validated (also `e8d4b53`, `edd49f7`) |
| F5 | 🔴 | fixed | `edd49f7` | service id + locker OOH id on the wire |
| F6 | 🟠 | fixed | `e8d4b53` | recording notifier + webhook test |
| F7 | 🟠 | fixed | `d6744f1` | CAS test reaches the CAS |
| F8 | 🟠 | fixed | `ef8d323` | `NextDispatchDelay` boundary fix |
| F9 | 🟠 | fixed | `1a240b7` | single shared, disposed limiter |
| F10 | 🟠 | fixed | `010c6dc` | preserve machine AWB on `→Shipped` |
| F11 | 🟠 | fixed | `010c6dc` | admin Paid stamps `PaidAt` + enqueues AWB |
| F12 | 🟠 | fixed | `edd49f7` | write guard `!= Cancelled` |
| F13 | 🟠 | fixed | `e8d4b53` | courier recipient server-validation |
| F14 | 🟠 | fixed | `d6744f1` | log the swallowed unreachable |
| F15 | 🟠 | fixed | `edd49f7` | log AWB before write; persist-fail → RetryLater (tested) |
| F16 | 🟠 | fixed | `edd49f7` | SQLite fresh-context read-back |
| F17 | 🟠 | fixed | `010c6dc` | assert `ShippedAt`/`DeliveredAt` |
| F18 | 🟠 | fixed | `835e932` | `catchError` keeps the search alive |
| F19 | 🟠 | fixed | `835e932` | single-stream prime + search |
| F32 | 🟡 | fixed | `edd49f7` | mapper rejects blank recipient (folded into F4) |
| F34 | 🟡 | fixed | `1a240b7` | finite-limit test (folded into F9) |

## Decisions & rationale

- **F5 (service code):** the Sameday service ids are per-merchant vendor config, not universal
  constants, so they are now `Sameday:LockerServiceId` / `Sameday:CourierServiceId` (default `7`,
  documented in `appsettings.json`). **The owner must set the real courier + locker service ids from
  their Sameday contract before enabling the jobs** — the code differentiates by delivery type and
  sends the locker OOH id, which was the concretely-fixable defect; the actual numbers are vendor data
  not in the repo.
- **F9 (QueueLimit):** kept unbounded. The jobs are already concurrency-capped by the `SemaphoreSlim`
  (same number as the permit limit), so queue depth is naturally bounded, and `SamedayShippingService`
  (the request path) delegates locker/cost to the static fallback and does **not** route through this
  limiter today. If a future request-path caller does, a finite `QueueLimit` (reject-fast) should be
  reconsidered so checkout calls don't queue to `HttpClient.Timeout`.
- **NEW — admin-Paid confirmation email (spotted during the micro-review, NOT in a finding):** the
  webhook Paid path fires the order-confirmation email and enqueues photo promotion; the admin Paid
  path (F11) now sets `PaidAt` + enqueues the AWB but does **neither**. Photo promotion self-heals via
  the `PromotionRecoveryScanner` sweep, but the **confirmation email has no backstop** — an order
  reconciled to Paid by an admin never sends one. Left as a conscious decision for the re-reviewer /
  owner (it was outside F11's scope and an admin may prefer to handle that comms manually). If wanted,
  it is a one-line `FireOrderConfirmedEmail` call in `AdminOrderService`'s Paid branch.
- Backlog (F20–F41) and the F42 false-positive: see the frontmatter notes.

## Hand-back

Next step is a **re-review** (verification pass) against `fixed_commit` `835e932` to produce
`review-v2.md`, which is what flips these fixes to `verified` (or reopens them). The fixer does not
self-verify.
