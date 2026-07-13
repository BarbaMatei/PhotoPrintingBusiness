---
type: code-review
target: bolt-035-payment-idempotency
version: 6
supersedes: 5
branch: feat/bolt-035-payment-idempotency
commit: 3faaae6
base: 224c711 (the commit review-v5 judged)
reviewed: 2026-06-19
reviewer: Claude (multi-lens re-review — 4 isolated lenses, clean contexts, blinded to reviews/)
lenses: [correctness, security-db-parity, pr-observability-quality, blinded-regression-hunter]
verdict: approved
blockers: []
findings: { verified: 12, deferred_accepted: 3, new: 1 }
---

# Review v6 — Bolt 035: Payment Idempotency (verify the v5 resolution)

Independent re-review of the resolution to the **v5 clean-room** findings, against branch
tip `3faaae6` (resolution work `224c711..738993e`, plus a v6 corrective doc commit `3faaae6`).
v5 was `approved` with 0 blockers but 15 fresh findings; the fixer drove **12 fixed**, **3
deferred**. This pass verifies each.

**Method (independence).** Four read-only lenses ran in **separate, fresh subagent
contexts**, each **forbidden from reading `reviews/`** (no review/resolution files) and from
reading the fixer's commit messages. Each judged only the code at HEAD, the specs under
`memory-bank/bolts/035-payment-idempotency/`, and the saved fix diff. Three lenses verified
the specific findings; a fourth was given **only the diff** (no finding list) and asked to
break it. I (orchestrator) ran the build/tests and synthesized — the verdicts below are the
lenses' independent conclusions, not the fixer's self-assessment.

## TL;DR

**All 12 fixed findings verify. The 3 deferrals are reasonable. Verdict: approved, 0 blockers
— the bolt-035 loop is complete once the one trivial new doc nit below is noted as already
corrected.**

- **12 → verified.** Every fix holds under independent scrutiny; the two non-vacuity-critical
  tests (DB-2, BUG-1) were re-confirmed (DB-2 fails at 255 / passes at 512; BUG-1 flips
  `ConflictException`→`DbUpdateException` when the fix is reverted).
- **3 deferrals accepted** (DB-1, QUAL-3, QUAL-4) — all correctly out-of-scope follow-ups
  with sound rationale; DB-1's breadcrumb is accurate.
- **1 NEW finding (DOC-3, ⚪), found AND corrected this pass.** The PR lens caught that DOC-2's
  edit was incomplete: ddd-02's Integration-Points section was corrected to "Stripe keyed by
  order.Id" but the **controller code sketch** (~ddd-02:265) still forwarded the client `key`,
  leaving the doc self-contradictory. Corrected in `3faaae6`. Non-blocking; the shipped code
  was always correct.
- **No regressions.** The blinded hunter found no introduced defect; tenant isolation
  (the highest-risk surface) is intact on every read path.

---

## Build & test

- `dotnet build PhotoPrint.sln` → **0 errors**, 5 warnings (4 pre-existing NU1603 Stripe.net
  46.3.0→47.0.0; 1 pre-existing CS1998 in a Razor test). **EF1002 is gone** (QUAL-5).
- `dotnet test PhotoPrint.sln` → **474 passed / 0 failed / 0 skipped** (was 466 at v5; +8 new
  regression tests).
- Non-vacuity re-confirmed for the two trickiest fixes (see probes).

---

## Per-finding verdicts

| ID | Sev | v5 status | v6 verdict | Evidence |
|----|-----|-----------|-----------|----------|
| DB-2 | 🟠 | fixed | **verified** | `StripeClientSecret` = `varchar(512)` consistently in model (`PhotoPrintDbContext.cs`), the not-yet-deployed migration's Npgsql branch, and the snapshot. `OrderIdempotencyColumnTests` asserts max length ≥ 512 (provider-independent). In-place migration edit safe (no Postgres DB applied; SQLite ignores maxLength). |
| OBS-1 | 🟠 | fixed | **verified** | `ExceptionHandlerMiddleware` computes `divergentFields` once and emits it in **both** the dev diagnostic object and the prod `ProblemDetails`; names only (no values/PII). Unit tests cover dev+prod; an HTTP-layer test asserts the divergent-processor 409 body names `paymentProcessor`. Integration runs under "Testing" (prod branch); the dev branch is unit-covered. |
| BUG-1 | 🟡 | fixed | **verified** | `catch (DbUpdateException ex) when (IsIdempotencyKeyViolation(ex))` confirms the violated constraint (Postgres `23505` + `ix_orders_idempotency_key`; SQLite code 19 + `"IdempotencyKey"` in message) instead of inferring via `AnyAsync`. Unrelated failures propagate; cross-tenant 409 + same-owner replay preserved. Regression test non-vacuous (revert → `ConflictException` not `DbUpdateException`). |
| SEC-1 | 🟡 | fixed | **verified** | Both-null guard at the top of `FindKeyHolderAsync` (throws) covers all three callers (resolution, post-collision recovery, `GetByIdempotencyKeyAsync`). Regression test confirms a both-null lookup throws instead of disclosing a guestless order. Retargeted stale test preserves its intent. |
| SEC-2 | 🟡 | fixed | **verified** | `IdempotencyKeyFilter` rejects keys > 80 (boundary correct: 80 ok, 81 rejected) with `BadRequestException` → 400 (mapping confirmed), before the action; whitespace/missing-key paths unchanged. Integration test (81-char key, seeded cart) asserts 400. |
| OBS-2 | 🟡 | fixed | **verified** | `payments.idempotency.conflict` (reserved name, ddd-01:61) emitted with correlation id + divergent field names (no values) where `IdempotencyConflictException` maps. Logger-verification test. Scoping to `IdempotencyConflictException` (not the plain cross-tenant `ConflictException`) matches ddd-01's `IdempotencyConflictDetected` event — defensible. |
| OBS-3 | 🟡 | fixed | **verified** | Recovery-replay documented + distinct `payments.idempotency.replay-recovery` log; behavior unchanged and safe (Stripe keyed by order id). Integration test exercises the path (null cached secret → gateway re-called, same order, usable secret, one row), closing its coverage gap. |
| DOC-1 | 🟡 | fixed | **verified** | ddd-02 now states the stale-key free is **two saves** (free commits, then insert), not one transaction — matches the code (per-statement unique-index enforcement). As-built notes added to both relevant sections. |
| DOC-2 | ⚪ | fixed | **verified*** | ddd-02 Integration-Points section documents the gateway is keyed by `order.Id` (matches `PaymentsController.cs` + the `...ReplaysOneOrderAndOneStripeCall` assertion). *The edit was incomplete — see DOC-3; corrected in `3faaae6`. |
| QUAL-1 | ⚪ | fixed | **verified** | `IsFresh()` + `ReplayOrConflict()` extracted; behavior-identical at both resolution call sites and in `GetByIdempotencyKeyAsync`. Full idempotency suite green. |
| QUAL-2 | ⚪ | fixed | **verified** | `DbProviders` constants match the original literals exactly; all runtime call sites use them (grep: 0 literal provider strings outside the definition + csproj); the `is DbProviders.InMemory or DbProviders.Sqlite` constant pattern compiles & behaves identically. Migration kept literal — accepted. |
| QUAL-5 | ⚪ | fixed | **verified** | `#pragma warning disable/restore EF1002` scoped to the year-sequence `ExecuteSqlRawAsync`; justification sound (server-side int, DDL identifier). EF1002 no longer in the build. |

**Deferrals (accepted):**

| ID | Sev | v5 status | v6 verdict | Note |
|----|-----|-----------|-----------|------|
| DB-1 | 🟠 | deferred | **deferral accepted** | Durable fix = per-provider migration assemblies (= BUG-5's v1 residual), correctly out-of-scope. The added migration breadcrumb is **accurate**: it correctly describes the phantom `AlterColumn`(TEXT→varchar) + `Drop`/`CreateIndex`(add NULL filter) the next Npgsql scaffold emits. Confirmed scaffold-time only — the runtime Npgsql model matches what this migration builds, so no `ValidateOnStart`/startup drift. |
| QUAL-3 | ⚪ | deferred | **deferral accepted** | Pre-existing controller altitude pattern (not introduced by this bolt). Relocating persistence would reshape the replay/compute split — a separate tidy. |
| QUAL-4 | ⚪ | deferred | **deferral accepted** | Test-only cross-fixture helper consolidation; low value vs churn. Batch later. |

**New finding (this pass):**

| ID | Sev | status | Note |
|----|-----|--------|------|
| DOC-3 | ⚪ | fixed (`3faaae6`) | DOC-2's edit corrected ddd-02's Integration-Points section but left the controller code **sketch** (~ddd-02:265) forwarding the client `key` to Stripe, making the doc self-contradictory. The shipped code was always correct (`order.Id`); the sketch was brought into line during this re-review. Trivial, non-blocking. |

**12 verified · 3 deferrals accepted · 1 new (corrected) · 0 reopened.**

---

## Adversarial probes (results)

- **DB-2 non-vacuity (decisive).** `OrderIdempotencyColumnTests` fails when the model is 255
  and passes at 512 — a real width guard, not a tautology.
- **BUG-1 non-vacuity (decisive).** Reverting the `OrderService` catch scoping (stash-revert)
  flips the new test from expecting `DbUpdateException` to throwing the old masked
  `ConflictException`. The test's premise (SQLite reports `ix_orders_order_number` — created
  before the idempotency index — first when both are violated) is deterministic for the
  `EnsureCreated` schema. The fix itself does NOT depend on that order: `IsIdempotencyKeyViolation`
  returns true only for the idempotency index regardless of which constraint SQLite reports.
- **BUG-1 false-positive check.** The SQLite message substring `Contains("IdempotencyKey")`
  cannot match the `OrderNumber` index ("…Orders.OrderNumber"); the Postgres branch uses an
  exact constraint-name match. Unrelated `DbUpdateException`s (FK, NOT NULL) hit the `_ => false`
  arm and propagate — confirmed by the correctness + hunter lenses.
- **Tenant isolation (highest-risk).** The security lens re-traced every read path (pre-INSERT
  lookup, stale-free, post-collision re-resolve, the cross-tenant 409, the 409 body): all
  owner-scoped, names-only in `divergentFields`, no secret leakage. The SEC-1 guard closes the
  both-null degeneracy without breaking single-identity flows.
- **QUAL-1 behavior-preservation.** Both extracted helpers are line-for-line equivalent to the
  prior inline logic at all three call sites; full idempotency + payment suites green.
- **Blinded regression hunt.** Given only the diff, the fourth lens found **no introduced
  defect**; it specifically cleared the `when`-filter control flow, the `divergentFields: null`
  serialization on non-idempotency errors, the filter-throw → 400 path, and the `DbProviders`
  constant values.

---

## Recommendation

**Approve. 0 blockers.** All 12 fixes are correctly and minimally implemented with real
(proven non-vacuous where it matters) regression tests; the 3 deferrals are sound and
documented; the single new finding (DOC-3) was a trivial doc self-contradiction left by an
incomplete DOC-2 edit and was corrected this pass. No fix regressed; tenant isolation holds.

**The bolt-035 resolution loop is complete** — every v5 finding is terminal (verified or
accepted-deferred), and the lone new doc nit is fixed.

Carried, unchanged (not blockers, tracked across reviews): the per-provider-migration-assembly
follow-up (DB-1 here / BUG-5's residual in v1) and `(owner, key)` uniqueness for the stale
cross-tenant-key 409 (INFO-2 in v1) — both resolve together in a future dual-DB parity pass.
