---
type: review-resolution
target: bolt-035-payment-idempotency
review_version: 1
status: resolved             # open | in-progress | resolved  (v4 re-review verified BUG-6 + DOC-4 — every finding terminal; 0 blockers throughout)
fixed_commit: 650f615        # branch tip (2093302, b52f4b6, 0b0fa04, 2f1872c, b6198b6, 3415ec7, 650f615)
opened: 2026-06-18
closed: 2026-06-19
# Per-finding state. status ∈ open | in-progress | fixed | verified | wont-fix | deferred | disputed | false-positive
# `verified` is set ONLY by the re-review (review-v2.md) — a fixer cannot self-verify.
findings:
  BUG-1:  { status: verified, commit: 2093302, note: "catch unique-index DbUpdateException on INSERT; same-owner→replay, cross-tenant→409, unrelated→rethrow; SQLite race tests. v2: confirmed tests FAIL with the unique-violation when catch removed." }
  SEC-1:  { status: verified, commit: 2093302, note: "scoped GetByIdempotencyKeyAsync + stale-free to userId/guestSessionId; unit + controller cross-tenant no-disclosure tests. v2: scoping real + consistently applied at both callers; secret not leaked." }
  BUG-3:  { status: verified, commit: 2093302, note: "added cart-items signature to DivergentFields; same-total-different-items now 409s on 'items'. v2: signature order-stable, no false pos/neg." }
  OPS-1:  { status: verified, commit: b52f4b6, note: "TODO(bolt-035-followup) in WarnIfMissingIdempotencyKey tracking the missing-key→400 breaking change. v2: present at PaymentsController.cs:120-123." }
  QUAL-1: { status: verified, commit: 2093302, note: "folded fresh-then-stale into one owner-scoped FindKeyHolderAsync round-trip. v2: confirmed single round-trip, in-memory window branch." }
  QUAL-3: { status: verified, commit: 0b0fa04, note: "IdempotencyKeyFilter (header extraction + missing-key warning + Items stash); actions read HttpContext.GetIdempotencyKey(). v3: behavior-preserving — labels/logs preserved, fires for both endpoints only, correct on 401/invalid-model short-circuit paths. (See DOC-4: dropped OPS-1's grep-able TODO token.)" }
  QUAL-4: { status: verified, commit: 0b0fa04, note: "generic CreateIntentAsync<TResponse>; Stripe/EuPlatesc thin adapters. v3: replay-vs-compute + null-cached recovery fall-through preserved (now symmetric across both processors); single save; BUG-4 keying preserved." }
  BUG-4:  { status: verified, commit: b52f4b6, note: "Stripe PaymentIntent now keyed by order.Id, not the client key; integration assertion updated. v2: lost-response dedup preserved." }
  BUG-5:  { status: verified, commit: 2f1872c, note: "migration provider-aware (Npgsql → varchar(N) + filtered unique index; SQLite output byte-identical). v3: confirmed snapshot untouched, Down unchanged, editing applied migration safe pre-deployment. RESIDUAL (deferred): snapshot scaffold-drift + decimal-typing parity — resolve with per-provider migration assemblies." }
  DOC-1:  { status: verified, commit: 2093302, note: "stale-key comment now states both SQLite and Postgres enforce the index per-statement. v2: confirmed OrderService.cs:107." }
  DOC-2:  { status: verified, commit: b52f4b6, note: "reworded unique-index NULL comment to 'multiple NULLs are permitted'. v2: confirmed PhotoPrintDbContext.cs:300-303." }
  DOC-3:  { status: deferred, commit: null, note: "ddd-02 sketch vs impl — deviation already documented in the walkthrough; batch into a docs pass. v2: accept-deferral." }
  QUAL-2: { status: wont-fix, commit: null, note: "distinct type carries DivergentFields payload; BUG-1 also throws plain ConflictException now, so both coexist meaningfully. v2: accept-wontfix." }
  QUAL-5: { status: verified, commit: b52f4b6, note: "HttpContext.GetCorrelationId() + shared item-key const; replaced raw-string reads in controller + exception handler. v2: confirmed all 3 sites." }
  QUAL-6: { status: wont-fix, commit: null, note: "two-phase save is intentional crash-recovery (order before gateway); BUG-1 fix preserves it. v2: accept-wontfix." }
  # ── raised in review-v2 ──
  INFO-1: { status: verified, commit: b6198b6, note: "SqlitePaymentFactory (real SQLite, shared in-memory conn, schema in CreateHost) + PaymentIdempotencyRelationalTests assert the cross-tenant 409 at the HTTP layer. v3: proven NON-VACUOUS — disabling the 409 branch flips the test 409→500, confirming it exercises the enforced unique index." }
  INFO-2: { status: wont-fix, commit: null, note: "stale cross-tenant key 409 is the accepted consequence of the global unique index (GUID keys → astronomically unlikely; safe + non-disclosing). Resolves only with (owner,key) uniqueness, tracked with BUG-5's residual. v3: accept-wontfix." }
  # ── raised in review-v3 (fixed in v4 round; awaiting re-review) ──
  BUG-6:  { status: verified, commit: 3415ec7, note: "v4: OrderNumberService count-based branch now also covers SQLite (was InMemory-only); Postgres sequence path BYTE-IDENTICAL. Regression test OrderNumberServiceSqliteTests runs the REAL service on real SQLite. Dropped the IOrderNumberService fake in SqlitePaymentFactory, so PaymentIdempotencyRelationalTests now exercises the real service end-to-end. v4: PROVEN NON-VACUOUS — reverting the SQLite branch fails both new tests (Postgres SQL throws on SQLite) AND flips the relational test 409→500. Postgres path unchanged; no soft-delete on Orders so count is consistent; unique ix_orders_order_number is the backstop. Count-on-concurrency 500 caveat is pre-existing + dev-only (prod uses the unchanged sequence) — not a new finding." }
  DOC-4:  { status: verified, commit: 650f615, note: "v4: restored the grep-able 'TODO(bolt-035-followup): enforce required key' token + ddd-02 pointer in IdempotencyKeyFilter's missing-key branch (IdempotencyKeyFilter.cs:30-33). v4: grep hits; LogWarning call unchanged → OPS-1 stays verified." }
---

# Resolution — Bolt 035: Payment Idempotency (review v1)

Fixer's response to [review-v1.md](review-v1.md). One row per finding. The reviewer's
findings file is immutable; this file is where the fix work is recorded. When every
finding is `fixed`/`wont-fix`/`deferred`/`disputed`/`false-positive` and the blockers
are addressed, set the top-level `status: resolved` and `fixed_commit`, then trigger a
re-review → `review-v2.md`, which sets the surviving findings to `verified` or reopens them.

**v1→v2:** both blockers (BUG-1, SEC-1) fixed with regression tests; review-v2 verified
13 findings, accepted 4 deferrals + 2 won't-fixes, raised INFO-1/INFO-2. Commits
**2093302** + **b52f4b6**.

**v3 (this round):** on request, the three v2 follow-up recommendations were implemented —
**QUAL-3, QUAL-4** (commit `0b0fa04`), **BUG-5** (`2f1872c`), **INFO-1** (`b6198b6`) — and
INFO-2 recorded as won't-fix. Full suite **464/464 green**.

**v3 re-review** ([review-v3.md](review-v3.md), against `b6198b6`): **approved-with-followups,
0 blockers.** QUAL-3, QUAL-4, BUG-5, INFO-1 → **verified**; INFO-2 → accept-wontfix. The
INFO-1 relational test was proven non-vacuous (disabling the 409 branch flips it 409→500).
Two NEW non-blocking follow-ups raised: 🟠 **BUG-6** (OrderNumberService has no SQLite branch
→ Development env order creation 500s; surfaced by the SqlitePaymentFactory faking the
service) and 🟡 **DOC-4** (the QUAL-3 refactor dropped OPS-1's grep-able TODO token).

**v4 (this round):** on request, both v3 follow-ups were fixed — **BUG-6** (`3415ec7`,
SQLite branch in `OrderNumberService` + real-service-on-SQLite regression test; the
SqlitePaymentFactory fake removed so INFO-1 now runs the real service) and **DOC-4**
(`650f615`, grep-able TODO token + ddd-02 pointer restored). Full suite **466/466 green**.

**v4 re-review** ([review-v4.md](review-v4.md), against `650f615`, base `b6198b6`):
**approved, 0 blockers.** BUG-6 + DOC-4 → **verified**. The BUG-6 regression test was proven
non-vacuous (reverting the SQLite branch fails both new `OrderNumberServiceSqliteTests` and
flips `PaymentIdempotencyRelationalTests` 409→500); the Postgres production path is
byte-identical. No new findings. **Every finding is now terminal** (verified / accepted
wont-fix / accepted deferred) — the bolt-035 resolution loop is complete.

| ID | Severity | Status | Fix commit | How / rationale |
|----|----------|--------|-----------|-----------------|
| BUG-1 | 🔴 High | fixed | 2093302 | Catch the unique-index `DbUpdateException` on the order INSERT. Same-owner race → re-resolve + replay (or 409 on divergence); cross-tenant key collision → clean 409 (no disclosure); unrelated `DbUpdateException` → rethrown, not masked. Kept the global unique index (see decisions). |
| SEC-1 | 🟠 Med | fixed | 2093302 | `GetByIdempotencyKeyAsync` + the stale-key free are now scoped to `userId`/`guestSessionId`. A caller can no longer resolve another tenant's order or its `StripeClientSecret`. |
| BUG-3 | 🟠 Med | fixed | 2093302 | Added a stable cart-items signature (product+upload+qty) to `DivergentFields`. Same-total-different-items now 409s on `items`. |
| OPS-1 | 🟠 Med | fixed | b52f4b6 | `TODO(bolt-035-followup)` on `WarnIfMissingIdempotencyKey` tracking the planned missing-key → 400 breaking change. |
| QUAL-1 | 🟠 Med | fixed | 2093302 | Folded the fresh-then-stale lookups into one owner-scoped `FindKeyHolderAsync` round-trip. |
| QUAL-3 | 🟠 Med | **verified** (v3) | 0b0fa04 | `IdempotencyKeyFilter` owns header extraction + whitespace-normalize + missing-key warning + `Items` stash; actions read `HttpContext.GetIdempotencyKey()`. v3: behavior-preserving (labels/logs/short-circuit paths). |
| QUAL-4 | 🟠 Med | **verified** (v3) | 0b0fa04 | Generic `CreateIntentAsync<TResponse>(cachedValue, computeAndApply, buildResponse)` holds the resolve→replay→compute→persist→respond shape; Stripe/EuPlatesc are thin adapters. v3: replay/compute + null-cached fall-through preserved. |
| BUG-4 | 🟡 Low | fixed | b52f4b6 | Stripe `PaymentIntent` is now keyed by `order.Id` (stable, unique per order) instead of the recyclable client key. Integration assertion updated. |
| BUG-5 | 🟡 Low | **verified** (v3) | 2f1872c | Migration is now provider-aware: Npgsql → `character varying(N)` + filtered unique index; SQLite output byte-identical. Fixes the Postgres-schema-correctness half. v3: snapshot untouched, Down unchanged, edit-in-place safe pre-deployment. **Residual (deferred):** snapshot scaffold-drift + decimal-typing parity. |
| DOC-1 | 🟡 Low | fixed | 2093302 | Stale-key comment now states both SQLite and Postgres enforce the index per-statement. |
| DOC-2 | 🟡 Low | fixed | b52f4b6 | Reworded the unique-index NULL comment to "multiple NULLs are permitted". |
| DOC-3 | 🟡 Low | **deferred** | — | ddd-02 sketch vs impl — the deviation is already documented in the bolt walkthrough; updating the historical design sketch is low-value churn, batch into a docs pass. |
| QUAL-2 | ⚪ Clean | **wont-fix** | — | `IdempotencyConflictException` carries the `DivergentFields` payload surfaced to clients; the BUG-1 fix now *also* throws plain `ConflictException` for cross-tenant collisions, so both types coexist meaningfully. A generic payload refactor isn't worth it. |
| QUAL-5 | ⚪ Clean | fixed | b52f4b6 | `HttpContext.GetCorrelationId()` + shared item-key const; replaced raw-string `Items["CorrelationId"]` reads in the controller and exception handler, and the setter in the middleware. |
| QUAL-6 | ⚪ Clean | **wont-fix** | — | Two-phase save is intentional crash-recovery (the order must exist before the gateway call). BUG-1's fix preserves the order-first design — collapsing the saves would reintroduce the unrecoverable mid-gateway window the review itself flagged. |
| INFO-1 | ⚪ Info | **verified** (v3) | b6198b6 | `SqlitePaymentFactory` + `PaymentIdempotencyRelationalTests` exercise the cross-tenant 409 over HTTP on a real (SQLite) unique index — the path the InMemory factory can't reach. v3: proven non-vacuous (disabling the 409 branch → test 409→500). |
| INFO-2 | ⚪ Info | **wont-fix** | — | Stale cross-tenant key → 409 to the second tenant: accepted consequence of the global unique index (GUID keys → vanishingly unlikely, safe, non-disclosing). Resolves with `(owner,key)` uniqueness alongside BUG-5's residual. v3: accept-wontfix. |
| BUG-6 | 🟠 Med | **verified** (v4) | 3415ec7 | `OrderNumberService` count-based branch now also covers SQLite (Postgres sequence path **byte-identical**). Regression test `OrderNumberServiceSqliteTests` runs the **real** service on real SQLite. Removed the `IOrderNumberService` fake in `SqlitePaymentFactory`, so the relational test exercises the real service end-to-end. v4: **proven non-vacuous** — reverting the branch fails both new tests + flips the relational test 409→500. Count-on-concurrency 500 is a pre-existing dev-only caveat (prod sequence unchanged). |
| DOC-4 | 🟡 Low | **verified** (v4) | 650f615 | Restored the grep-able `TODO(bolt-035-followup): enforce required key` token + ddd-02 pointer in `IdempotencyKeyFilter`'s missing-key branch. v4: grep hits; `LogWarning` unchanged → OPS-1 stays verified. |

## Decisions for the re-reviewer

- **BUG-1 — kept the global unique index, did not move to `(owner, IdempotencyKey)`.**
  The review floated per-tenant key namespaces as an option. I kept the global index and
  handle a cross-tenant key collision by returning a clean 409 (re-resolution is scoped
  to the caller, so it never replays another tenant's order). Rationale: per-owner
  uniqueness needs two filtered partial indexes (UserId vs GuestSessionId) on two nullable
  columns — exactly the dual-DB partial-index complexity BUG-5 is about — for a collision
  that is astronomically unlikely with client GUID keys. The 409 is safe and
  non-disclosing. Flag if you disagree.
- **SEC-1 changed stale-key semantics:** a caller now frees only *their own* stale key,
  not an ownerless/other-tenant row. One existing unit test seeded an ownerless stale
  order freed by a different user; it was updated to the owner-scoped scenario (the secure
  behavior), not weakened.
- **BUG-1 regression tests use real SQLite, not EF InMemory.** InMemory does not enforce
  unique indexes, so it cannot reproduce this bug. Added the `Microsoft.EntityFrameworkCore.Sqlite`
  test package and `OrderServiceIdempotencyConcurrencyTests` (shared-connection in-memory
  SQLite + a one-shot `SaveChangesInterceptor` to inject the winning racer deterministically).
- **Deferrals — now only DOC-3.** QUAL-3, QUAL-4 and BUG-5 were deferred at v2 and are now
  implemented (v3). DOC-3 (ddd-02 historical-sketch reconciliation) remains deferred as
  low-value doc churn.

### v3 round — for the v3 re-review

- **QUAL-3/QUAL-4 are a behavior-preserving refactor.** No endpoint behavior changed; all
  pre-existing payment integration tests stay green. Verify the `IdempotencyKeyFilter`
  fires for both endpoints (and not on the 401 path, where authorization short-circuits
  before action filters) and that `CreateIntentAsync` keeps the replay/compute split.
- **BUG-5 edits an already-applied migration.** Safe here: SQLite output is byte-identical
  (dev DBs unaffected; tests use `EnsureCreated`, not migrations) and no Postgres DB has
  applied it yet (pre-deployment). The model snapshot is still SQLite-flavored, so
  scaffold-time drift under Npgsql is only *reduced*, not eliminated — that needs
  per-provider migration assemblies. Push back if you'd rather this were a new migration.
- **INFO-1 needed a real DB.** `SqlitePaymentFactory` swaps InMemory→SQLite (shared
  connection, schema built in `CreateHost`); it registers a provider-agnostic fake
  `IOrderNumberService` and sets `CartItem.SizeId` in the shared seed (InMemory had
  masked that FK).

## Verification (filled by re-review)

Re-reviewed against `fixed_commit` (b52f4b6) in [review-v2.md](review-v2.md). **Verdict:
approved-with-followups; 0 blockers.**

- Build: 0 errors (4 pre-existing NU1603 Stripe.net warnings).
- Tests: **463 passed / 0 failed / 0 skipped** (full suite). The predicted
  `ReliableEmailServiceTests` flake did not recur this run.
- Decisive check: removed the `catch (DbUpdateException)` block → both BUG-1 SQLite
  concurrency tests **failed** with the `ix_orders_idempotency_key` unique violation,
  confirming the regression tests reproduce the real 500-causing race (not vacuous).
- 13 findings verified, 4 deferrals accepted, 2 won't-fixes accepted, 0 reopened.
- 2 new informational items (INFO-1: cross-tenant integration test runs on InMemory which
  doesn't enforce the unique index, so it proves SEC-1 non-disclosure but not the real-DB
  409 — that path is covered by the SQLite unit test; INFO-2: a stale cross-tenant key
  collision returns 409 to the second tenant). Neither blocks merge.

**v3 round — re-reviewed** in [review-v3.md](review-v3.md), against `b6198b6` (base
`b52f4b6`). **Verdict: approved-with-followups; 0 blockers.**

- Build: 0 errors (6 pre-existing warnings). Tests: **464 passed / 0 failed / 0 skipped**.
- QUAL-3, QUAL-4, BUG-5, INFO-1 → **verified**; INFO-2 → accept-wontfix; 0 reopened.
- Decisive INFO-1 check: disabling the `collidesWithOtherCaller` 409 branch flipped the
  relational test **409→500**, proving it exercises the enforced SQLite unique index (not
  a vacuous pass). Reverted; tree clean.
- QUAL-3/QUAL-4 confirmed behavior-preserving (labels, logs, replay-vs-compute incl. the
  null-cached recovery fall-through, filter scope, 401/invalid-model short-circuit paths).
  BUG-5 SQLite output byte-identical; snapshot untouched.
- 2 NEW non-blocking follow-ups raised: 🟠 **BUG-6** (OrderNumberService no SQLite branch →
  Development order creation 500s) and 🟡 **DOC-4** (dropped OPS-1 TODO token). Both `open`.

**v4 round — re-reviewed** in [review-v4.md](review-v4.md), against `650f615` (base
`b6198b6`). **Verdict: approved; 0 blockers.**

- Build: 0 errors (4 pre-existing NU1603 warnings). Tests: **466 passed / 0 failed / 0 skipped**.
- BUG-6, DOC-4 → **verified**; 0 reopened; 0 new findings.
- Decisive BUG-6 non-vacuity check: reverting the `OrderNumberService` SQLite branch fails
  both new `OrderNumberServiceSqliteTests` (Postgres `DO $$ … nextval` SQL throws on SQLite)
  **and** flips `PaymentIdempotencyRelationalTests` 409→500 (proving the factory now drives
  the real service end-to-end). 3 failed / 0 passed under the revert. Restored; tree clean.
- Postgres production path confirmed byte-identical to `b6198b6`; no soft-delete on `Orders`
  so count-based numbering is consistent; `ix_orders_order_number` is the uniqueness backstop.
  The count-on-concurrency → 500 caveat is pre-existing (InMemory shared it), dev-only, and
  explicitly scoped out by the fix — recorded as an observation, not a finding.
- DOC-4: `grep "TODO(bolt-035-followup)"` now hits `IdempotencyKeyFilter.cs:33`; ddd-02
  pointer present; `LogWarning` unchanged → OPS-1 stays verified.
- **Bolt-035 resolution loop complete** — every finding is terminal.
