---
type: review-resolution
target: bolt-035-payment-idempotency
review_version: 8
status: resolved             # open | in-progress | resolved
fixed_commit: 01b5264        # branch tip; per-finding fix commits in the map below
opened: 2026-07-04
closed: 2026-07-04
# Per-finding state. status ∈ open | in-progress | fixed | verified | wont-fix | deferred | disputed | false-positive
# `verified` is set ONLY by the re-review (review-v9.md) — a fixer cannot self-verify.
# v8 finding IDs are v8-scoped (clean-room) and do NOT map to the v1–v7 comment IDs.
findings:
  DB-1:   { status: deferred, commit: 01b5264, note: "Re-raise of the standing v5→v7 accepted deferral. App uses EnsureCreated(), never Migrate(), so no test exercises the Postgres arm; a Testcontainers-Postgres regression belongs to the roadmap's migration/deploy (3-env) phase. Migration breadcrumb refreshed to spell out both the phantom diff (DB-2) and the zero Postgres coverage (DB-1)." }
  OBS-1:  { status: fixed, commit: 21a295a, note: "Introduced IdempotencyKeyTakenException (subtype of ConflictException → still 409); OrderService throws it on the cross-tenant recovery path; middleware maps it + emits the reserved payments.idempotency.cross-tenant-conflict event (correlation id only). Regression test asserts 409 + the reserved event fires (test references the new type → inherently non-vacuous). Tightened the cross-tenant concurrency test to assert the specific subtype." }
  BUG-1:  { status: fixed, commit: 6a370e0, note: "Shared PhotoPrintDbContext.IdempotencyKeyIndexName between the index + the Postgres ConstraintName match; SQLite arm now keys off the extended code 2067 (SQLITE_CONSTRAINT_UNIQUE) + nameof(Order.IdempotencyKey) — a rename of either breaks at compile time. Regression test pins both SQLite premises (extended code + column-in-message)." }
  SEC-1:  { status: deferred, commit: 01b5264, note: "Durable fix = per-tenant composite unique index = schema/migration + snapshot churn → deferred to the migration/deploy phase (with DB-1/DB-2). Accepted-residual threat note added at the index. Exploitability LOW (client-chosen GUID keys, self-limiting probe). Shares root cause with REQ-1." }
  BUG-2:  { status: deferred, commit: 01b5264, note: "Durable fix (re-read persisted URL under a Postgres SELECT … FOR UPDATE row lock) needs the not-yet-built Postgres arm → deferred. Did the review's stated minimum: documented the Stripe-vs-EuPlatesc gateway-dedupe asymmetry + the concurrent-recovery URL caveat at the EuPlatesc build path. No double charge today (stable invoice_id maps both retries to one order)." }
  SEC-2:  { status: fixed, commit: b76eede, note: "IdempotencyKeyFilter trims the key before the null/length checks; whitespace-only still → null; length cap applies to the trimmed key. New filter unit tests set the raw header directly (HTTP transport strips OWS, which would mask this at the integration layer): padded/tabbed keys trim, padded==plain dedupe, over-length-after-trim → 400." }
  BUG-3:  { status: fixed, commit: 0d5a721, note: "Stale-key free now flushes in the SAME SaveChanges as the new-order INSERT (one transaction) — EF's unique-index-aware ordering emits the UPDATE before the INSERT (no within-batch collision, verified on SQLite). A failing INSERT now rolls the free back with it. Non-vacuity proven: restored the intermediate save → the rollback test went red. ddd-02 v5 DOC-1 notes updated to as-built." }
  BUG-4:  { status: fixed, commit: f71041f, note: "Wrapped the persist in a bounded retry loop: an OrderNumber unique violation (racy SQLite COUNT+1) regenerates the number and retries the still-tracked order; the idempotency recovery is unchanged and terminal. Added OrderNumberIndexName constant + IsOrderNumberViolation. Regression test (real OrderNumberService + injected same-key/same-number winner) proven non-vacuous (retries=0 → 500). Re-pointed the BUG-1 'unrelated failure propagates' test to an FK violation (order-number is now a handled transient)." }
  REQ-1:  { status: fixed, commit: 6de2e58, note: "Doc-only (behavior already as-intended): the 24h window / stale reclamation is owner-scoped, so a stale key is freed only for its original owner; cross-caller the global unique index keeps it reserved → 409. Aligned ddd-01 (invariant + glossary), Order.IdempotencyKey, and IOrderService.CreateFromCartAsync; the truly-per-caller window needs the deferred SEC-1 composite index." }
  DB-2:   { status: deferred, commit: 01b5264, note: "SQLite-flavored model snapshot → phantom migration diff; already acknowledged/deferred in the migration comment. Same migration/deploy-phase item as DB-1; breadcrumb refreshed. (This is what the v5 DB-1 note originally described.)" }
  QUAL-2: { status: fixed, commit: f7a314a, note: "Extracted PricingTierResolver.Resolve(tiers, quantity); CartService + OrderService delegate, keeping their own (deliberately different) tier-source + quantity semantics. Corrected the misleading 'mirrors CartService' comment. Behavior-preserving — 57 order/cart/concurrency tests green." }
  QUAL-1: { status: fixed, commit: b8238a9, note: "Removed dead GetByIdempotencyKeyAsync (interface + impl); IsFresh/FindKeyHolderAsync retained (used by CreateFromCartAsync). Dropped the two GetBy tests already duplicated by CreateFromCart_StaleKey_* / CreateFromCart_OtherTenantsKey_*; retargeted the unique SEC-1 both-null guard test at CreateFromCartAsync. ddd-01 interface listing updated." }
  QUAL-3: { status: fixed, commit: 71255b1, note: "Hoisted the canonical cart graph into TestCartSeed.Build(...); OrderServiceTests, OrderServiceIdempotencyConcurrencyTests, and PaymentFactory delegate. Fixed the drift (SizeId now set consistently; harmless on InMemory). 60 tests green." }
  QUAL-4: { status: fixed, commit: 4bbd9c5, note: "Concurrency-test winners now built via the real CreateFromCartAsync (InjectWinnerViaRealFlowAsync) so totals/items come from the service, not hardcoded 6.00/20.00/26.00. Only the OrderNumber is pinned (control knob: distinct → idempotency-only; equal → order-number-first for BUG-4). Also cleans up the winner my BUG-4 test added." }
  QUAL-5: { status: fixed, commit: 694788a, note: "Collapsed the two WasIdempotentReplay checks + duplicated log shapes in CreateIntentAsync into one switch over (WasIdempotentReplay, cached is not null). Behavior-preserving — 19 payment integration tests green." }
  QUAL-6: { status: fixed, commit: cfa23af, note: "WriteProblemDetailsAsync is now an instance method using the injected _environment; deleted the IsDevContext RequestServices service-locator hop. Test CreateContext no longer stubs a RequestServices env. 12 middleware tests green." }
  OBS-2:  { status: fixed, commit: b37d322, note: "Added IdempotencyConflictProblemDetails documenting the 409 body; both endpoints' ProducesResponseType(409) now type it. DivergentFields is optional in the contract (present for the same-caller divergent conflict, absent for the cross-tenant IdempotencyKeyTakenException). Documentation-only — runtime body unchanged." }
  OBS-3:  { status: fixed, commit: 8c3987a, note: "Transitional missing-key event logs at Information (was Warning) — it is the expected state on ~100% of requests during FE migration, so Warning was constant alert noise. ddd-01/ddd-02 references updated. Raises back to Warning when the key becomes required (OPS-1)." }
---

# Resolution — Bolt 035: Payment Idempotency (review v8)

Fixer's response to [review-v8.md](review-v8.md), the fresh clean-room discovery audit at
`50fc692`. One row per finding ID. The review file is immutable; this file is where the fix
work is recorded. v8 was **approve-with-followups, 0 blockers** — none gated merge — but every
finding is now at a terminal state: **14 fixed, 4 deferred**.

**Scope decided with the owner (2026-07-04):** *full sweep* — fix every tractable finding now
with the regression test the review asked for; **keep the DB/migration/schema findings deferred**
(DB-1, DB-2, SEC-1, BUG-2's row-lock) to the roadmap's migration/deploy phase, consistent with
the standing v5→v7 decision.

`recommended_before_deploy (review): [DB-1, OBS-1]` — OBS-1 fixed; DB-1 stays deferred (the
same Postgres/migration infra the roadmap parks in the 3-env phase).

| ID | Sev | Status | Fix commit | How / rationale |
|----|-----|--------|-----------|-----------------|
| DB-1  | 🟠 M | **deferred** | 01b5264 | Postgres arm exercised by no test (EnsureCreated, never Migrate). Testcontainers-Postgres regression → migration/deploy phase. Migration breadcrumb refreshed. |
| OBS-1 | 🟠 M | **fixed** | 21a295a | Distinct `IdempotencyKeyTakenException` (409 subtype) + reserved `payments.idempotency.cross-tenant-conflict` log; regression test. |
| BUG-1 | 🟡 L | **fixed** | 6a370e0 | Shared index-name constant + SQLite extended code 2067 + `nameof` column → rename/re-word is a compile break; premises test. |
| SEC-1 | 🟡 L | **deferred** | 01b5264 | Per-tenant composite unique index = migration churn → migration/deploy phase. Accepted-residual threat note added; LOW exploitability. Root-shared with REQ-1. |
| BUG-2 | 🟡 L | **deferred** | 01b5264 | Row-lock hardening needs the unbuilt Postgres arm → deferred; documented the gateway-dedupe asymmetry (review's stated minimum). No double charge today. |
| SEC-2 | 🟡 L | **fixed** | b76eede | Trim the key before null/length checks; filter unit tests (raw header, bypassing OWS-strip). |
| BUG-3 | 🟡 L | **fixed** | 0d5a721 | Free + INSERT in one SaveChanges (atomic); rollback test proven non-vacuous; ddd-02 updated. |
| BUG-4 | 🟡 L | **fixed** | f71041f | Bounded retry on OrderNumber collision (SQLite); real-generator regression test; BUG-1 test re-pointed to FK. |
| REQ-1 | 🟡 L | **fixed** | 6de2e58 | Doc: 24h reclamation is owner-scoped; cross-caller the key stays reserved (global index). |
| DB-2  | 🟡 L | **deferred** | 01b5264 | SQLite-flavored snapshot phantom diff — already deferred in the migration comment; same phase as DB-1. |
| QUAL-2| 🟡 L | **fixed** | f7a314a | Shared `PricingTierResolver`; misleading "mirrors" comment corrected. Behavior-preserving. |
| QUAL-1| ⚪ C | **fixed** | b8238a9 | Removed dead `GetByIdempotencyKeyAsync`; tests retargeted at `CreateFromCartAsync`. |
| QUAL-3| ⚪ C | **fixed** | 71255b1 | Hoisted `TestCartSeed`; fixed the SizeId drift across the 3 fixtures. |
| QUAL-4| ⚪ C | **fixed** | 4bbd9c5 | Concurrency-test winners built via the real flow — no more magic totals. |
| QUAL-5| ⚪ C | **fixed** | 694788a | One switch over `(replay, cached)` in `CreateIntentAsync`. Behavior-preserving. |
| QUAL-6| ⚪ C | **fixed** | cfa23af | Middleware uses injected `_environment`; service-locator hop deleted. |
| OBS-2 | ⚪ C | **fixed** | b37d322 | Typed `IdempotencyConflictProblemDetails` on the 409 `ProducesResponseType`. Doc-only. |
| OBS-3 | ⚪ C | **fixed** | 8c3987a | Missing-key logs at Information, not Warning. |

## Decisions for the re-reviewer

- **No blockers existed** — v8 is `approve-with-followups`. This pass drove all 18 findings to
  terminal: **14 fixed with fail-first regression tests / behavior-preserving refactors, 4 deferred.**
- **The 4 deferrals (DB-1, DB-2, SEC-1, BUG-2 row-lock) are the migration/deploy cluster.** DB-1/DB-2
  are re-raises of the v5→v7 accepted deferral (the review's own note flags DB-2 as "known/deferred");
  SEC-1's composite index and BUG-2's `FOR UPDATE` both require the not-yet-built Postgres/migration
  arm. Fixing them now would mean the premature schema churn the owner has consistently parked, so
  the breadcrumbs/threat notes were refreshed instead (commit `01b5264`). Push back if you want the
  composite index or a Testcontainers fixture built in this pass.
- **Two committed tests were deliberately changed as a consequence of fixes, not silently:**
  - the cross-tenant concurrency test now asserts `IdempotencyKeyTakenException` (OBS-1's subtype),
    because xUnit `Assert.ThrowsAsync` is exact-type;
  - the BUG-1 "unrelated failure propagates" test was re-pointed from an OrderNumber collision (now a
    handled/retryable transient under BUG-4) to an FK violation, preserving its "don't mask unrelated
    failures as a 409" guarantee.
- **BUG-3 relies on EF Core's unique-index-aware command ordering** (UPDATE-free before INSERT within
  one batch). This contradicts the v5 DOC-1 assumption ("one transaction would still collide
  per-statement"), which was wrong for the EF path — verified on SQLite by the happy-path test. The
  atomicity is proven by the rollback test (non-vacuous: red when the intermediate save is restored).
- **BUG-4's retry is bounded** (`MaxOrderNumberRetries = 3`); a genuine persistent number clash still
  surfaces. It is effectively SQLite/dev-only — Postgres uses a per-year sequence.
- **REQ-1, OBS-2 are documentation** (no runtime behavior change); QUAL-2/1/3/4/5/6 are
  behavior-preserving refactors guarded by the existing suite.

## Verification (fixer's own checks — NOT self-verification; the re-review owns `verified`)

- **Build:** `dotnet build PhotoPrint.sln -c Debug` → **0 errors** (only the pre-existing NU1603
  Stripe.net + one CS1998 warning, both unrelated).
- **Tests:** `dotnet test PhotoPrint.sln` → **487 passed / 0 failed / 0 skipped** (baseline 474; +13
  net new: OBS-1 ×1, BUG-1 ×1, BUG-3 ×2, BUG-4 ×1, SEC-2 ×10 filter cases, QUAL-1 −2).
- **Non-vacuity proven** for the two trickiest behavioral fixes by revert-and-rerun: BUG-3 (restore
  the intermediate save → rollback test red) and BUG-4 (retries=0 → collision test 500s).
- **Self-review of the diff (fix-generativity):** no leftover temp markers; the only remaining
  `GetByIdempotencyKeyAsync` reference is an explanatory comment; docs (ddd-01/ddd-02, migration,
  Order.cs, IOrderService) updated to match code; no guards removed (both-null + unrelated-failure
  guarantees retained and retested).

**Next step:** a **verification re-review → `review-v9.md`** against `fixed_commit` (`01b5264`), which
flips the 14 fixed findings to `verified` (or reopens them) and agrees/pushes back on the 4 deferrals.
The fixer does not self-verify.
