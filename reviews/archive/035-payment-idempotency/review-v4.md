---
type: code-review
target: bolt-035-payment-idempotency
version: 4
supersedes: 3
branch: feat/bolt-035-payment-idempotency
commit: 650f615
base: b6198b6
reviewed: 2026-06-19
reviewer: Claude (multi-lens re-review — independent, clean context)
lenses: [correctness, tests-verification, db-parity, pr-requirements, quality-altitude]
verdict: approved
blockers: []
---

# Review v4 — Bolt 035: Payment Idempotency (verify the two v3 follow-ups)

Independent adversarial re-review of the v4 round (commits `3415ec7` BUG-6, `650f615` DOC-4)
against branch tip `650f615`, base `b6198b6`. v3 (`approved-with-followups`) raised two NEW
non-blocking findings — 🟠 **BUG-6** (`OrderNumberService` had no SQLite branch → Development
env order creation 500s) and 🟡 **DOC-4** (the QUAL-3 refactor dropped OPS-1's grep-able TODO
token). Both were then fixed. I did not write the code; every verdict is derived from reading
the code at `650f615`, building, running the tests, and adversarial probing — not the
resolution's notes.

## TL;DR

**Both v4 items verify. Verdict: approved, 0 blockers.**

- **BUG-6 → verified.** `OrderNumberService` now routes SQLite through the count-based branch
  alongside InMemory; the Postgres sequence path is **byte-identical** to `b6198b6`. The new
  `OrderNumberServiceSqliteTests` runs the **real** service against a real SQLite DB and is
  **proven non-vacuous**: reverting the SQLite branch flips both new tests to fail (the
  Postgres `DO $$ … nextval` SQL throws on SQLite at `OrderNumberService.cs:32`) **and** flips
  `PaymentIdempotencyRelationalTests` 409→500 — proving the factory now drives the real service
  end-to-end after the fake was removed. The `SequentialOrderNumberService` fake is gone with
  no dangling references.
- **DOC-4 → verified.** The grep-able `TODO(bolt-035-followup): enforce required key` token +
  the `ddd-02` / `memory-bank/bolts/035-payment-idempotency` pointer are restored in the
  filter's missing-key branch. Logging behavior unchanged → OPS-1 stays verified.
- **No NEW findings.** The count-based-on-concurrency caveat is a pre-existing, explicitly
  scoped-out dev/test characteristic (production uses the unchanged Postgres sequence) — an
  observation, not a finding.

---

## Build & test

- `dotnet build` → **0 errors**, 4 NU1603 (Stripe.net 46.3.0→47.0.0) warnings (pre-existing;
  the EF1002/CS1998 warnings noted in v3 did not surface in this incremental build but are
  unchanged source-side).
- `dotnet test PhotoPrint.Tests` → **466 passed / 0 failed / 0 skipped** (~8s). Matches the
  resolution's 466/466 claim (464 at v3 + the 2 new `OrderNumberServiceSqliteTests`).
- New tests in isolation → 2/2 pass against clean source.

---

## Per-item verdicts

| ID | Sev | prior status | v4 verdict | Evidence |
|----|-----|--------------|-----------|----------|
| BUG-6 | 🟠 Med | open (v3) | **verified** | `OrderNumberService.GenerateAsync` (`OrderNumberService.cs:23-28`) now gates the count-based branch on `ProviderName is "Microsoft.EntityFrameworkCore.InMemory" or "Microsoft.EntityFrameworkCore.Sqlite"`. The Postgres else-branch (`:30-48`, `DO $$ … CREATE SEQUENCE` + `nextval`) is **byte-identical** to `b6198b6` (diffed: only the comment + the `if` condition changed). `appsettings.Development.json:2` confirms `"DatabaseProvider": "Sqlite"`, so the Development env hit the Postgres SQL before → 500 on order creation; now it takes the count path. Real service registered scoped (`Program.cs:102`), used by `OrderService.cs:122`. New `OrderNumberServiceSqliteTests` constructs the **real** `OrderNumberService` over a real `:memory:` SQLite `DbContext` (`EnsureCreated`) — not the EF InMemory provider. `SqlitePaymentFactory` no longer registers the `SequentialOrderNumberService` fake (`RemoveAll`/`AddSingleton` lines removed, `:38`), so `PaymentIdempotencyRelationalTests` now exercises the real service end-to-end. **Non-vacuity proven** (see probes). No dangling fake references (`grep` clean). |
| DOC-4 | 🟡 Low | open (v3) | **verified** | `IdempotencyKeyFilter.cs:30-33`: the missing-key branch now carries `// … TODO(bolt-035-followup): enforce required key.` plus `// Tracked in memory-bank/bolts/035-payment-idempotency (ddd-02) …`. `grep -rn "TODO(bolt-035-followup)" src/PhotoPrint.API/` → hits `IdempotencyKeyFilter.cs:33` (was empty at `b6198b6`). The ddd-02 pointer is present (`:32-33`). The `_logger.LogWarning(...)` call (`:34-36`) is unchanged → missing-key warning behavior identical → **OPS-1 stays verified**. |

**2 verified · 0 reopened.**

---

## Adversarial probes (results)

- **BUG-6 non-vacuity (decisive).** Temporarily reverted the SQLite branch (removed
  `or "Microsoft.EntityFrameworkCore.Sqlite"`), rebuilt, ran the affected tests:
  - `OrderNumberServiceSqliteTests` — **both fail**: the Postgres `DO $$ … nextval` SQL throws
    on SQLite (`SqliteException` originating at `OrderNumberService.cs:32`, the
    `ExecuteSqlRawAsync` call). Proves the test would have caught the original bug.
  - `PaymentIdempotencyRelationalTests` — **fails 409→500** (tenant A's first intent now 500s
    instead of OK). Proves the factory genuinely runs the real `OrderNumberService` after the
    fake's removal, not a stub. Result: **3 failed / 0 passed**. Restored the branch;
    `git diff -- 'src/**/*.cs'` empty, `git status` shows only `reviews/` + `.claude/`. Tree clean.
- **Postgres path unchanged.** Diffed `b6198b6:OrderNumberService.cs` vs tip — the sequence
  SQL, connection handling, and `FormatOrderNumber` are unchanged. The production code path
  carries zero behavioral risk from this fix.
- **Count-based safety / `OrderNumber` backstop.** `ix_orders_order_number` is unique
  (`PhotoPrintDbContext.cs:278-280`). There is **no** soft-delete / `HasQueryFilter` on
  `Orders` (grep clean), so `Orders.CountAsync()` reflects the true row count — count-based
  numbering is internally consistent (no hidden rows make the count understate the max). The
  count scheme is identical to the long-standing InMemory branch.
- **Concurrency caveat on count-based numbering (observation, NOT a finding).** Two
  *simultaneous* count-based generations could read the same count → duplicate `OrderNumber`
  → `DbUpdateException` from `ix_orders_order_number`. In `OrderService`'s catch
  (`OrderService.cs:161-191`), if the colliding requests don't share an idempotency key the
  winner lookup returns null and `collidesWithOtherCaller` is false, so it `throw;` →
  surfaces as a 500. **Why this is not a new finding:** (a) it is a pre-existing property of
  the count-based branch, not introduced by BUG-6 — InMemory already behaved this way;
  (b) the fix comment explicitly scopes it out ("these providers don't carry production
  write-concurrency; the unique index is the backstop"); (c) production uses the unchanged
  Postgres sequence (collision-free); (d) the Development env is single-developer. The chosen
  fix is the right altitude for a dev-env defect — reusing the existing provider-agnostic
  count path rather than inventing a SQLite sequence emulation.
- **DOC-4 token grep.** `grep -rn "TODO(bolt-035-followup)"` over `src/PhotoPrint.API/`
  returns exactly the restored line; `ddd-02` referenced in the filter (and pre-existing in
  `Order.cs:30`). Backlog sweep affordance restored.
- **Fake removal cleanliness.** No remaining `SequentialOrderNumberService` references; the
  surviving `IOrderNumberService` mocks (`OrderServiceTests.cs`,
  `OrderServiceIdempotencyConcurrencyTests.cs:51`) are unit-test mocks that legitimately
  don't exercise the SQLite path — the gap the new dedicated test now closes.

---

## Regression checks

- **OrderNumberService is used broadly** (every order creation via `OrderService.cs:122`).
  The full suite (466) is green, including all order-creation integration tests, so the
  InMemory and Postgres behaviors are unaffected; only the SQLite provider gained a branch
  it previously lacked.
- **INFO-1 (verified v3) strengthened, not regressed.** It now runs the real service on SQLite
  end-to-end and still asserts the cross-tenant 409 + single-keyed-order invariants
  (`PaymentIdempotencyRelationalTests.cs:54-63`). The non-vacuity check confirms the path is
  live (reverting the fix breaks it).
- **OPS-1, QUAL-3 (verified) intact.** The filter's warning logging is unchanged; only a
  comment was added.
- All previously-verified findings (BUG-1/3/4/5, SEC-1, QUAL-1/4/5, DOC-1/2) sit on code
  untouched by this round.

---

## Recommendation

**Approve.** Both v3 follow-ups are correctly and minimally implemented; the BUG-6 regression
test is real (proven non-vacuous via revert), the Postgres production path is byte-identical,
and DOC-4 restores the requested grep-able affordance with no behavior change. No finding
regressed; no new finding. The bolt-035 resolution loop is complete — every finding is
terminal (verified / accepted wont-fix / accepted deferred).

Carried, unchanged from v3 (not blockers): BUG-5's residual (per-provider migration assemblies
+ decimal-typing parity) and INFO-2 (stale cross-tenant key → 409) both resolve together with
a future `(owner, key)` uniqueness / dual-DB parity pass; DOC-3 (ddd-02 historical-sketch
reconciliation) stays batched into a docs pass.
