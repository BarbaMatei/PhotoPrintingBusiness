---
type: review-resolution
target: bolt-035-payment-idempotency
review_version: 5
status: resolved            # open | in-progress | resolved
fixed_commit: fbb4c7c       # branch tip; fixes: 11e72c1 e957ac1 659056a 6aad926 c7c2b97 8278bbe 8d5b240 1c36ff5 03fa13d 0bc6ecd 24ed333 738993e (+v6: 3faaae6 DOC-3; +post-v6: fbb4c7c QUAL-4)
opened: 2026-06-19
closed: 2026-06-19
# Per-finding state. status ∈ open | in-progress | verified | verified | wont-fix | deferred | disputed | false-positive
# `verified` is set ONLY by the re-review (review-v6.md) — a fixer cannot self-verify.
# v5 finding IDs are v5-scoped (clean-room) and do NOT map to the v1–v4 comment IDs.
findings:
  DB-1:   { status: deferred, commit: 659056a, note: "real fix stays deferred (= BUG-5 v1 residual); breadcrumb added. POST-V6 RE-ASSESSED (user asked if it can be fixed now): app uses EnsureCreated() at startup (Program.cs:143,166), never Migrate() — migrations are Postgres-only artifacts for a not-yet-built migration-based deployment. A clean snapshot fix means either committing a phantom migration or hand-rewriting the whole generated snapshot Npgsql-canonical, and it belongs to the migration/deploy setup (roadmap's 3-env phase). Premature now. Breadcrumb stands." }
  DB-2:   { status: verified, commit: 11e72c1, note: "widened StripeClientSecret to varchar(512) in model + (not-yet-deployed) migration + snapshot; added provider-independent model-metadata regression guard (fails at 255, passes at 512)." }
  OBS-1:  { status: verified, commit: e957ac1, note: "compute divergentFields once, emit in BOTH dev diagnostic + prod ProblemDetails branches; unit tests for dev & prod, HTTP-layer body assertion on the divergent-processor 409. Dev test fails pre-fix, passes post-fix." }
  BUG-1:  { status: verified, commit: 6aad926, note: "added `when (IsIdempotencyKeyViolation(ex))` filter (Postgres 23505+constraint name / SQLite code 19 + 'IdempotencyKey' in message); dropped the AnyAsync inference — unrelated DbUpdateExceptions now propagate. Regression test: OrderNumber collision + key held by another tenant propagates DbUpdateException (pre-fix masked it as ConflictException 409); proven non-vacuous via stash-revert." }
  SEC-1:  { status: verified, commit: c7c2b97, note: "reject-if-both-null guard at the top of FindKeyHolderAsync (throws InvalidOperationException). Retargeted the existing stale-order test off the both-null path to a real owner; added a guard regression test asserting both-null throws rather than disclosing a guestless order." }
  SEC-2:  { status: verified, commit: 8278bbe, note: "IdempotencyKeyFilter now rejects keys longer than MaxKeyLength=80 with a BadRequestException (→400) before the action runs. Integration test sends an 81-char key with a seeded cart (so a pass would be 200) and asserts 400." }
  OBS-2:  { status: verified, commit: 8d5b240, note: "ExceptionHandlerMiddleware now emits `payments.idempotency.conflict` (correlation_id + divergent_fields, names only) when it maps an IdempotencyConflictException, alongside the generic warning. Logger-verification unit test. Scoped to IdempotencyConflictException (the divergent-request conflict ddd-01 reserves), not the plain cross-tenant ConflictException — see decisions." }
  OBS-3:  { status: verified, commit: 1c36ff5, note: "documented the recovery-replay path in CreateIntentAsync + emit distinct `payments.idempotency.replay-recovery` log. Behavior unchanged (gateway re-call was already safe by design). Integration test exercises the previously-unobserved path: null cached secret on a replay → gateway re-called, same order, usable secret, one row. Log line itself not asserted (mirrors the existing un-asserted `replay` log)." }
  DOC-1:  { status: verified, commit: 03fa13d, note: "ddd-02 now states the stale-key free is two saves (free commits first, then insert) — not the same transaction; per-statement unique-index enforcement makes one transaction collide. Noted intentional non-atomicity + owner-scoping." }
  DOC-2:  { status: verified, commit: 03fa13d, note: "ddd-02 Stripe SDK section now documents the gateway is keyed by order.Id (BUG-4), not the client header key; explains why (recycled/replayed key can't collide a different PaymentIntent; recovery-replay returns the same intent)." }
  QUAL-1: { status: verified, commit: 0bc6ecd, note: "extracted IsFresh(order) + ReplayOrConflict(holder,request,total,items); both the pre-INSERT lookup and post-collision recovery now call them (IsFresh also used in GetByIdempotencyKeyAsync). Behavior-preserving; named ReplayOrConflict vs the review's sketched ResolveFreshHolder." }
  QUAL-2: { status: verified, commit: 24ed333, note: "added DbProviders constants (Postgres/Sqlite/InMemory); replaced magic strings in DbContext + OrderNumberService + (beyond the named files) CartService + StaticShippingService. Migration kept literal — see decisions." }
  QUAL-3: { status: deferred, commit: null, note: "pre-existing altitude pattern; review says 'not introduced here'. POST-V6 RE-ASSESSED: confirmed CODEBASE-WIDE — WebhooksController also injects PhotoPrintDbContext and uses it 6x; PaymentsController's single SaveChangesAsync is the same convention. Fixing only PaymentsController (e.g. exposing SaveChangesAsync on IOrderService to drop its _db) would be inconsistent cosmetic churn; the real fix is a repo-wide controller/service boundary decision, out of scope for a bolt-035 fix-review. Stays deferred." }
  QUAL-4: { status: fixed, commit: fbb4c7c, note: "(POST-V6, user-requested) consolidated the duplicated Idempotency-Key POST builders into PaymentRequestHelpers HttpClient extensions; per-class helpers delegate, call sites unchanged. 19 payment integration tests green. Left the unit MakeRequest + SQLite-vs-WAF factory setup (different test layers). Awaiting v7 re-review to verify." }
  QUAL-5: { status: verified, commit: 738993e, note: "suppressed EF1002 with #pragma + justifying comment (seqName is a server-side int only, DDL identifier can't be parameterized). Warning no longer in the build output. Postgres branch still has no automated coverage — unchanged, no Postgres in the test matrix." }
---

# Resolution — Bolt 035: Payment Idempotency (review v5)

Fixer's response to [review-v5.md](review-v5.md), the clean-room re-review at `224c711`.
One row per finding ID. The review file is immutable; this file is where the fix work is
recorded. v5 is **approved with 0 blockers** — none of these gate merge — but every finding
is driven to a terminal state below. When all are terminal, the top-level `status` flips to
`resolved` and a re-review → `review-v6.md` sets the surviving findings to `verified`.

`recommended_before_deploy: [DB-2, OBS-1]` — both fixed in this pass.

| ID | Severity | Status | Fix commit | How / rationale |
|----|----------|--------|-----------|-----------------|
| DB-1 | 🟠 Med | **deferred** | 659056a | The durable fix (per-provider migration assemblies) is the same architectural follow-up deferred as BUG-5's residual in v1 — out of scope for this pass. Did the review's stated *minimum*: strengthened the migration breadcrumb to spell out the exact phantom diff (`AlterColumn` TEXT→varchar + `Drop`/`CreateIndex` to add the NULL filter) so the next author expects and discards it. Scaffold-time trap only — no runtime/`ValidateOnStart` drift for the deployed index. |
| DB-2 | 🟠 Med | verified | 11e72c1 | Widened `StripeClientSecret` to `varchar(512)` (model + migration + snapshot). Headroom above Stripe's 255 ceiling so a longer secret can't 500 prod Postgres after the charge exists. Regression guard asserts the configured max length is ≥512 (provider-independent — no Postgres needed). |
| OBS-1 | 🟠 Med | verified | e957ac1 | `divergentFields` now computed once and emitted in **both** the Development diagnostic shape and the prod `ProblemDetails` (was prod-only → invisible to a FE on the dev API). Tests: unit dev+prod (dev fails pre-fix), plus an HTTP-layer assertion that the divergent-processor 409 body names `paymentProcessor`. |
| BUG-1 | 🟡 Low | verified | 6aad926 | The catch now gates on `IsIdempotencyKeyViolation(ex)` (Postgres `23505` + `ix_orders_idempotency_key`; SQLite constraint code 19 + `IdempotencyKey` in the message) instead of inferring the cause via `AnyAsync(key)`. Unrelated `DbUpdateException`s propagate honestly; the TOCTOU `AnyAsync` race is gone. Regression test forces an `OrderNumber` collision while the key is also held by another tenant — propagates `DbUpdateException` (pre-fix it threw a masked `ConflictException`; non-vacuity confirmed by stash-revert). |
| SEC-1 | 🟡 Low | verified | c7c2b97 | Guard at the top of `FindKeyHolderAsync` throws `InvalidOperationException` if both `userId` and `guestSessionId` are null (covers both callers: resolution + `GetByIdempotencyKeyAsync`). New regression test asserts a both-null lookup throws instead of resolving a guestless owner's order. **Note:** the pre-existing `GetByIdempotencyKey_StaleOrder_ReturnsNull` leaned on the both-null path and was retargeted to a real owner (its stale-window intent is preserved). |
| SEC-2 | 🟡 Low | verified | 8278bbe | Filter rejects keys > 80 chars (`MaxKeyLength` const) with a 400 before the action runs — matching the documented constraint and pre-empting the prod Postgres truncation 500. Integration test (81-char key, seeded cart) asserts 400, not the would-be 200. |
| OBS-2 | 🟡 Low | verified | 8d5b240 | Middleware emits the reserved `payments.idempotency.conflict` event (correlation id + divergent field names) when mapping `IdempotencyConflictException`. Logger-verification unit test asserts it fires. Scoped to the divergent-request conflict (ddd-01's `IdempotencyConflictDetected`), not every `ConflictException` — see decisions. |
| OBS-3 | 🟡 Low | verified | 1c36ff5 | Documented the recovery path and added a distinct `payments.idempotency.replay-recovery` info log. Behavior is unchanged (the gateway re-call was already safe — Stripe keyed by order id). New integration test exercises the path (null cached secret → gateway re-called, same order, usable secret, one row), closing its coverage gap. The log line itself is not asserted (consistent with the existing `replay` log). |
| DOC-1 | 🟡 Low | verified | 03fa13d | ddd-02 corrected: the stale-key free is **two saves** (free commits, then insert), not one transaction — a single transaction would still collide on the per-statement unique index. Added an as-built note in both the behaviour-matrix and Data Model sections. |
| DOC-2 | ⚪ Clean | verified | 03fa13d | ddd-02 Stripe SDK section now states the gateway is keyed by `order.Id` (BUG-4), not the client header key, with the rationale. |
| QUAL-1 | ⚪ Clean | verified | 0bc6ecd | Extracted `IsFresh()` + `ReplayOrConflict()`; both resolution blocks (pre-INSERT + post-collision) and `GetByIdempotencyKeyAsync` reuse them. Behavior-preserving (full idempotency + payment suites green). Named `ReplayOrConflict` rather than the review's sketch `ResolveFreshHolder`. |
| QUAL-2 | ⚪ Clean | verified | 24ed333 | `DbProviders` constants class; magic strings replaced in `PhotoPrintDbContext`, `OrderNumberService`, and (beyond the 3 named files, for completeness) `CartService` + `StaticShippingService`. The migration keeps its literal `ActiveProvider` string — see decisions. |
| QUAL-3 | ⚪ Clean | **deferred** | — | **Post-v6 re-assessed:** confirmed codebase-wide — `WebhooksController` also injects `PhotoPrintDbContext` (6 uses); PaymentsController's single `SaveChangesAsync` is the same convention. Fixing only PaymentsController would be inconsistent cosmetic churn; the real fix is a repo-wide controller/service-boundary decision, out of scope for a bolt-035 fix-review. Stays deferred. |
| QUAL-4 | ⚪ Clean | fixed | fbb4c7c | **Post-v6 (user-requested):** extracted the duplicated payment POST builders into `PaymentRequestHelpers` `HttpClient` extensions; per-class helpers delegate, call sites unchanged; 19 payment integration tests green. Unit `MakeRequest` + SQLite/WAF factory setup left (different test layers). Awaiting v7 to verify. |
| QUAL-5 | ⚪ Clean | verified | 738993e | `#pragma warning disable/restore EF1002` around the year-sequence `ExecuteSqlRawAsync`, with a comment: the interpolated value is a server-side `int` (no injection) and a DDL identifier (can't be parameterized). EF1002 no longer appears in the build. (The Postgres branch's lack of coverage is unchanged — no Postgres in the test matrix.) |

## Decisions for the re-reviewer

- **No blockers existed** — v5 was already `approved`. This pass drove all 15 fresh
  findings to terminal anyway: **12 fixed**, **3 deferred** (DB-1, QUAL-3, QUAL-4). Both
  `recommended_before_deploy` items (DB-2, OBS-1) are fixed.
- **DB-1 deferred, not fixed.** The durable fix is per-provider migration assemblies — the
  same architectural follow-up already deferred as BUG-5's residual in v1. I did the
  review's stated *minimum* (a precise breadcrumb so the next author expects and discards
  the phantom diff). Push back if you want the snapshot regenerated under Npgsql now.
- **BUG-1 regression test depends on SQLite's constraint-check order.** The test forces a
  simultaneous `OrderNumber` + idempotency double-violation and relies on SQLite reporting
  `ix_orders_order_number` first (created before the idempotency index). This is
  deterministic for the EnsureCreated schema and was proven non-vacuous (stash-revert: the
  test throws `ConflictException` pre-fix, `DbUpdateException` post-fix). The fix itself
  does not depend on that order — `IsIdempotencyKeyViolation` only ever returns true for the
  idempotency index regardless of which constraint SQLite happens to report.
- **SEC-1 changed one existing test.** `GetByIdempotencyKey_StaleOrder_ReturnsNull` used the
  now-guarded both-null path purely for convenience; I retargeted it to a real owner. Its
  stale-window intent is unchanged — it was not weakened.
- **OBS-2 scoped to `IdempotencyConflictException`.** The reserved `payments.idempotency.conflict`
  event (ddd-01:58/61) is the divergent-request conflict. The plain cross-tenant
  `ConflictException` (BUG-1 path) is also a "conflict" but isn't distinguishable from other
  `ConflictException`s at the middleware; I left it on the generic warning. Flag if you want
  it to emit the reserved event too.
- **OBS-3 is additive (log + doc).** The recovery-replay behavior was already safe and
  unchanged; the new test characterizes the previously-untested path rather than proving a
  behavior change. The `replay-recovery` log line itself isn't asserted (mirrors the
  existing un-asserted `replay` log).
- **QUAL-2 extended beyond the 3 named files.** The review named DbContext/OrderNumberService/
  Migration; I also converted `CartService` + `StaticShippingService` (same magic strings) so
  no literal remains to drift. I deliberately **kept the migration's literal** `ActiveProvider`
  string — migrations are self-contained historical artifacts and shouldn't couple to a
  runtime constant that a later refactor (e.g. DB-1's per-provider assemblies) might move.
- **QUAL-1 naming.** Helper is `ReplayOrConflict` (what it does), not the review's sketched
  `ResolveFreshHolder`.
- **QUAL-3 / QUAL-4 deferred** (rationale in their rows): a pre-existing altitude pattern and
  a low-value cross-fixture test-helper consolidation, respectively — both batch-later items.

## Verification (filled by re-review)

Fixer's own checks (NOT a self-verification — the re-review owns `verified`):

- **Build:** `dotnet build PhotoPrint.API.csproj` → **0 errors**. EF1002 is gone (QUAL-5);
  only the pre-existing NU1603 Stripe.net resolves remain.
- **Tests:** `dotnet test PhotoPrint.sln` → **474 passed / 0 failed / 0 skipped** (was
  466; +8 new: DB-2 column guard ×1, OBS-1 dev+prod ×2, OBS-2 conflict-log ×1, BUG-1
  rethrow ×1, SEC-1 both-null ×1, SEC-2 over-length ×1, OBS-3 recovery ×1; the OBS-1 body
  assertion was added to an existing test).
- **Non-vacuity proven** for the two trickiest: DB-2 (fails at 255, passes at 512) and
  BUG-1 (stash-reverted to confirm `ConflictException`→`DbUpdateException` flip).

**Next step (done):** re-reviewed in [review-v6.md](review-v6.md) against `3faaae6` (= 738993e
fixes + the DOC-3 corrective doc commit). I did not self-verify — four isolated lenses, each
blinded to `reviews/`, judged the code independently.

### v6 re-review outcome (against `3faaae6`)

- **All 12 fixed findings → `verified`** (statuses flipped above): DB-2, OBS-1, BUG-1, SEC-1,
  SEC-2, OBS-2, OBS-3, DOC-1, DOC-2, QUAL-1, QUAL-2, QUAL-5. Build 0 errors (EF1002 gone),
  **474/474 tests** green. DB-2 and BUG-1 re-confirmed non-vacuous.
- **3 deferrals accepted** (DB-1, QUAL-3, QUAL-4) — rationale upheld; DB-1's migration
  breadcrumb confirmed accurate; scaffold-time-only, no runtime drift.
- **1 NEW finding raised + fixed this pass — DOC-3 (⚪).** The PR lens caught that DOC-2's edit
  was incomplete: ddd-02's controller code sketch (~:265) still forwarded the client `key` to
  Stripe, contradicting the corrected Integration-Points section. Fixed in **`3faaae6`** (sketch
  now passes `order.Id.ToString()`). Recorded in review-v6; the shipped code was always correct.
- **0 reopened. No regressions** (blinded hunter found no introduced defect; tenant isolation
  intact). **Bolt-035 loop complete** — every finding terminal.

### Post-v6 — revisited the 3 deferrals (user asked "can these be fixed now?")

- **QUAL-4 → now `fixed`** (`fbb4c7c`). The duplicated payment POST builders were genuinely
  consolidatable without risk — done. Needs a v7 re-review to flip to `verified`.
- **DB-1 → stays deferred.** The app uses `EnsureCreated()` (not `Migrate()`); migrations are
  Postgres-only artifacts for a migration-based deployment that doesn't exist yet. A clean
  snapshot fix means a phantom migration or a full hand-rewrite of the generated snapshot, and
  it belongs to the migration/deploy setup (roadmap's 3-env phase). Premature now.
- **QUAL-3 → stays deferred.** Confirmed codebase-wide (`WebhooksController` uses
  `PhotoPrintDbContext` 6×). Fixing one controller is inconsistent cosmetic churn; the real fix
  is a repo-wide boundary decision.

So after this round: **12 verified · 1 fixed-awaiting-v7 (QUAL-4) · 2 deferred (DB-1, QUAL-3).**
