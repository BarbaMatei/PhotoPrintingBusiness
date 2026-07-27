---
type: review-resolution
target: bolt-035-payment-idempotency
review_version: 8
status: resolved             # open | in-progress | resolved
fixed_commit: 065a516        # branch tip of the fix work; per-finding fix commits in the map below
opened: 2026-07-04
closed: 2026-07-04
# Per-finding state. status ∈ open | in-progress | fixed | verified | wont-fix | deferred | disputed | false-positive
# `verified` is set ONLY by the re-review (review-v9.md) — a fixer cannot self-verify.
# v8 finding IDs are v8-scoped (clean-room) and do NOT map to the v1–v7 comment IDs.
findings:
  DB-1:   { status: deferred, commit: 01b5264, note: "Re-raise of the standing v5→v7 accepted deferral. App uses EnsureCreated(), never Migrate(), so no test exercises the Postgres arm; Testcontainers-Postgres regression belongs to the migration/deploy (3-env) phase. Migration breadcrumb refreshed. v9: deferral-sound (0 Migrate() calls in tests confirmed)." }
  OBS-1:  { status: verified, commit: 21a295a, note: "IdempotencyKeyTakenException (subtype of ConflictException → 409) + reserved payments.idempotency.cross-tenant-conflict log; regression test. v9 VERIFIED — exact-type 409 mapping entry present (else it would 500), reserved log fires, same-caller path still throws IdempotencyConflictException; tenant isolation intact." }
  BUG-1:  { status: verified, commit: 6a370e0, note: "Shared IdempotencyKeyIndexName (compile-break coupling) + SQLite extended code 2067 + nameof(Order.IdempotencyKey). v9 VERIFIED — coupling is a real code reference (not a comment), premises test pins both, OrderNumber violation routed to a separate predicate." }
  SEC-1:  { status: deferred, commit: 01b5264, note: "Per-tenant composite unique index = schema/migration + snapshot churn → migration/deploy phase (with DB-1/DB-2). Accepted-residual threat note at the index. LOW exploitability. Shares root with REQ-1. v9: deferral-sound (durable fix genuinely needs a migration; LOW argument honest; breadcrumb accurate)." }
  BUG-2:  { status: deferred, commit: 01b5264, note: "Row-lock fix (Postgres SELECT … FOR UPDATE) needs the unbuilt Postgres arm → deferred; documented the Stripe-vs-EuPlatesc gateway-dedupe asymmetry (review's stated minimum). v9: deferral-sound — Stripe keyed by o.Id, EuPlatesc invoice_id==order.Id folds into the HMAC so no double charge; only the verbatim-replay invariant momentarily breaks." }
  SEC-2:  { status: verified, commit: b76eede, note: "Trim before the null/length checks; filter unit tests set the raw header directly (HTTP strips OWS). v9 VERIFIED — trim correctly ordered, padded==plain dedupe proven, 10/10 filter tests green." }
  BUG-3:  { status: verified, commit: 0d5a721, note: "Free + INSERT in one SaveChanges (atomic); EF orders UPDATE before INSERT (no within-batch collision); failing insert rolls the free back. v9 VERIFIED — intermediate save is gone (diff-confirmed), rollback test non-vacuous (red against the two-save code)." }
  BUG-4:  { status: verified, commit: f71041f, note: "Bounded retry on OrderNumber collision (SQLite); real-generator regression test; BUG-1 test re-pointed to FK. v9 VERIFIED — retry bounded at 3 (no infinite loop), test non-vacuous (retries=0 → 500), replay/409 preserved, keyless path also benefits." }
  REQ-1:  { status: verified, commit: 6de2e58, note: "Doc: owner-scoped 24h reclamation; cross-caller the global index keeps the key reserved. v9 VERIFIED — code behaves this way (owner-scoped FindKeyHolderAsync), ddd-01 + Order.cs + IOrderService consistent." }
  DB-2:   { status: deferred, commit: 01b5264, note: "SQLite-flavored snapshot phantom diff; already deferred in the migration comment. Same phase as DB-1. v9: deferral-sound — snapshot genuinely SQLite-flavored (TEXT + unfiltered index) vs Npgsql varchar(80)+filtered; migration comment acknowledges it." }
  QUAL-2: { status: verified, commit: f7a314a, note: "Shared PricingTierResolver; misleading comment corrected. v9 VERIFIED — resolver is byte-for-byte the prior rule; both callers keep their own source+qty; behavior-preserving." }
  QUAL-1: { status: verified, commit: b8238a9, note: "Removed dead GetByIdempotencyKeyAsync; tests retargeted at CreateFromCartAsync. v9 VERIFIED — no live refs, IsFresh/FindKeyHolderAsync retained, both-null guard moved, coverage preserved." }
  QUAL-3: { status: verified, commit: 71255b1, note: "Hoisted TestCartSeed; fixed the SizeId drift. v9 VERIFIED — 3 fixtures delegate, SizeId consistent, order flow reads Product.Sizes so InMemory behavior unchanged." }
  QUAL-4: { status: verified, commit: 4bbd9c5, note: "Concurrency-test winners via the real flow — no magic totals. v9 VERIFIED — winners built via CreateFromCartAsync, distinct/equal order-number pins the intended collision path per test, no 6/20/26 magic totals remain." }
  QUAL-5: { status: verified, commit: 694788a, note: "One switch over (replay, cached) in CreateIntentAsync. v9 VERIFIED — all three branches preserved; (true,true) returns before the gateway call (genuinely skipped)." }
  QUAL-6: { status: verified, commit: cfa23af, note: "Middleware uses injected _environment; service-locator hop deleted. v9 VERIFIED — instance method, IsDevContext removed, dev/prod shape unchanged (both carry divergentFields)." }
  OBS-2:  { status: verified, commit: b37d322, note: "Typed IdempotencyConflictProblemDetails on the 409 ProducesResponseType; DivergentFields nullable; runtime body unchanged. v9 VERIFIED — DTO matches runtime (field present for same-caller conflict, absent for cross-tenant type)." }
  OBS-3:  { status: verified, commit: 065a516, note: "Missing-key logs at Information (was Warning). Code v9-verified; v9 reopened the incomplete doc alignment (4 stale Warning refs: ddd-01:118, ddd-02:117, ddd-02:324, filter class summary), completed in 065a516. v10 VERIFIED by an independent lens — no reference states current behavior as Warning (remaining tokens are historical/future-escalation); residual WARN in frozen stage artifacts judged non-defects." }
---

# Resolution — Bolt 035: Payment Idempotency (review v8)

Fixer's response to [review-v8.md](review-v8.md), the fresh clean-room discovery audit at
`50fc692`, and its verification re-review [review-v9.md](review-v9.md) at `01b5264`. The review
files are immutable; this file records the fix work + the re-review outcome. v8 was
**approve-with-followups, 0 blockers**. Final state (after v9 + v10): **14 verified · 4
accepted-deferred · 0 open — resolution loop complete.**

**Scope decided with the owner (2026-07-04):** *full sweep* — fix every tractable finding now
with the regression test the review asked for; **keep the DB/migration/schema findings deferred**
(DB-1, DB-2, SEC-1, BUG-2's row-lock) to the roadmap's migration/deploy phase, consistent with
the standing v5→v7 decision.

| ID | Sev | Status | Fix commit | How / rationale |
|----|-----|--------|-----------|-----------------|
| DB-1  | 🟠 M | **deferred** | 01b5264 | Postgres arm exercised by no test (EnsureCreated, never Migrate) → migration/deploy phase. v9: deferral-sound. |
| OBS-1 | 🟠 M | **verified** | 21a295a | Distinct `IdempotencyKeyTakenException` (409) + reserved cross-tenant log; test. v9-verified. |
| BUG-1 | 🟡 L | **verified** | 6a370e0 | Shared index-name constant + SQLite extended 2067 + `nameof` → rename is a compile break. v9-verified. |
| SEC-1 | 🟡 L | **deferred** | 01b5264 | Composite per-tenant index = migration churn → deferred; threat note added; LOW exploitability. v9: deferral-sound. |
| BUG-2 | 🟡 L | **deferred** | 01b5264 | Row-lock needs the unbuilt Postgres arm → deferred; asymmetry documented; no double charge today. v9: deferral-sound. |
| SEC-2 | 🟡 L | **verified** | b76eede | Trim before null/length; filter unit tests bypass OWS-strip. v9-verified. |
| BUG-3 | 🟡 L | **verified** | 0d5a721 | Free+INSERT atomic; rollback test non-vacuous. v9-verified. |
| BUG-4 | 🟡 L | **verified** | f71041f | Bounded OrderNumber-collision retry; real-generator test. v9-verified. |
| REQ-1 | 🟡 L | **verified** | 6de2e58 | Doc: owner-scoped reclamation. v9-verified. |
| DB-2  | 🟡 L | **deferred** | 01b5264 | SQLite-flavored snapshot phantom diff → migration phase. v9: deferral-sound. |
| QUAL-2| 🟡 L | **verified** | f7a314a | Shared `PricingTierResolver`. v9-verified. |
| QUAL-1| ⚪ C | **verified** | b8238a9 | Removed dead `GetByIdempotencyKeyAsync`. v9-verified. |
| QUAL-3| ⚪ C | **verified** | 71255b1 | Hoisted `TestCartSeed`; SizeId drift fixed. v9-verified. |
| QUAL-4| ⚪ C | **verified** | 4bbd9c5 | Real-flow winners, no magic totals. v9-verified. |
| QUAL-5| ⚪ C | **verified** | 694788a | Replay-branch switch. v9-verified. |
| QUAL-6| ⚪ C | **verified** | cfa23af | Middleware `_environment`, no service-locator. v9-verified. |
| OBS-2 | ⚪ C | **verified** | b37d322 | Typed 409 body. v9-verified. |
| OBS-3 | ⚪ C | **verified** | 065a516 | Missing-key at Information. Code v9-verified; v9 reopened incomplete doc alignment → completed in 065a516; **v10-verified** by an independent lens. |

## Decisions for the re-reviewer

- **No blockers existed** — v8 is `approve-with-followups`. This pass drove all 18 findings to
  terminal: **14 fixed with fail-first regression tests / behavior-preserving refactors, 4 deferred.**
- **The 4 deferrals (DB-1, DB-2, SEC-1, BUG-2 row-lock) are the migration/deploy cluster** — all
  re-affirmed sound by v9. Breadcrumbs/threat notes refreshed in `01b5264`.
- **Two committed tests were deliberately changed as documented consequences of fixes:** the
  cross-tenant concurrency test asserts `IdempotencyKeyTakenException` (OBS-1); the BUG-1 "unrelated
  failure propagates" test was re-pointed to an FK violation (order-number is now a handled transient
  under BUG-4). v9 confirmed both.
- **BUG-3 relies on EF's unique-index-aware command ordering** (contradicting the v5 DOC-1 assumption,
  which was wrong for the EF path) — verified on SQLite.

## v9 re-review outcome (against `01b5264`)

Verification re-review [review-v9.md](review-v9.md) — 4 isolated anchored lenses + build/test.

- **13 fixed findings → `verified`:** OBS-1, BUG-1, BUG-3, BUG-4, SEC-2, REQ-1, OBS-2, QUAL-1, QUAL-2,
  QUAL-3, QUAL-4, QUAL-5, QUAL-6. Build 0 errors, **487/487** tests green. BUG-3 + BUG-4 re-confirmed
  non-vacuous by revert-and-rerun.
- **4 deferrals accepted** (DB-1, DB-2, SEC-1, BUG-2) — rationales verified fact-by-fact (0 `Migrate()`
  in tests; snapshot genuinely SQLite-flavored; composite index genuinely needs a migration; EuPlatesc
  `invoice_id==order.Id` makes concurrent recovery charge-safe).
- **1 NEW finding raised + fixed this pass — OBS-3 reopen (⚪).** The docs lens caught that OBS-3's
  code fix (`LogInformation`) was correct but the doc alignment was incomplete: 4 references still said
  Warning/WARN (`ddd-01:118`, `ddd-02:117`, `ddd-02:324`, the `IdempotencyKeyFilter` class summary),
  contradicting the shipped code. This is the fix-generated-incompleteness class `README.md` flags.
  Fixed in **`065a516`** (grep-confirmed no stale current-behavior refs remain).
- **0 reopened among the 13. No regressions** (tenant isolation intact; no hidden behavior change).

## v10 re-review outcome (against `065a516`)

Narrow verification re-review [review-v10.md](review-v10.md) — one isolated lens confirming the
OBS-3 doc-drift completion. **OBS-3 → `verified`:** the code logs at Information and no reference
states current behavior as Warning (remaining tokens are historical "was Warning" / the future OPS-1
escalation); residual WARN in three frozen stage artifacts (implementation-walkthrough, ddd-03
test-report, upstream intent AC) judged non-defects — they record point-in-time state, not current
behavior.

**Final: 14 verified · 4 accepted-deferred · 0 open — bolt-035 resolution loop complete.** Every v8
fix is confirmed by an independent re-review; the 4 deferrals ride to the migration/deploy phase.
**Closing the *feature* (vs. these fixes) still requires a saturated discovery pass** (K independent
blinded audits agreeing) — tracked separately per the two-loops model.

## Verification (fixer's own checks — the re-review owns `verified`)

- **Build:** `dotnet build PhotoPrint.sln -c Debug` → **0 errors** (pre-existing NU1603 + one CS1998).
- **Tests:** `dotnet test PhotoPrint.sln` → **487 passed / 0 failed / 0 skipped** (baseline 474; +13 net).
- **Non-vacuity** proven by revert-and-rerun for BUG-3 and BUG-4; re-confirmed by the v9 correctness lens.
