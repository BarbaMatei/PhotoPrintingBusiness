---
type: code-review
target: bolt-035-payment-idempotency
version: 3
supersedes: 2
branch: feat/bolt-035-payment-idempotency
commit: b6198b6
base: b52f4b6
reviewed: 2026-06-19
reviewer: Claude (multi-lens re-review — independent, clean context)
lenses: [correctness, security, pr-requirements, quality-altitude, tests-verification, db-parity]
verdict: approved-with-followups
blockers: []
---

# Review v3 — Bolt 035: Payment Idempotency (verify the four post-v2 follow-ups)

Independent adversarial re-review of the v3 round of work (commits `0b0fa04`, `2f1872c`,
`b6198b6`) against `b6198b6`, base `b52f4b6`. After v2 (`approved-with-followups`), four
previously deferred/raised items were implemented: **QUAL-3, QUAL-4** (action filter +
generic replay helper), **BUG-5** (provider-aware migration), **INFO-1** (relational
cross-tenant 409 test). I did not write the code; every verdict is derived from reading
the code at `b6198b6` and exercising the tests, not from the resolution notes.

## TL;DR

**All four v3 items verify.** The QUAL-3/QUAL-4 refactor is genuinely behavior-preserving
(labels, logs, replay-vs-compute semantics, and the null-cached recovery fall-through are
intact); BUG-5's SQLite output is byte-identical and the Npgsql branch is correct; the
INFO-1 relational test is **non-vacuous** — I proved it exercises the real SQLite unique
index by disabling the 409 branch and watching it turn into a 500. No previously-verified
finding regressed in behavior. The adversarial sweep on the new test infrastructure turned
up **two NEW findings** — one a real dev-environment bug the new SQLite factory *surfaced*.

- **Verdict: approved-with-followups.** No blockers.
- QUAL-3, QUAL-4, BUG-5, INFO-1 → **verified**. INFO-2 → **accept-wontfix**.
- 2 NEW findings: 🟠 **BUG-6** (OrderNumberService has no SQLite branch — Development env
  uses SQLite, so order creation 500s), 🟡 **DOC-4** (OPS-1's grep-able TODO token dropped
  by the QUAL-3 refactor). Neither blocks merge; BUG-6 should be a near-term follow-up.

---

## Build & test

- `dotnet build` → **0 errors** (6 pre-existing warnings: 4× NU1603 Stripe.net version
  resolution, 1× EF1002 OrderNumberService raw SQL, 1× CS1998 RazorTemplateServiceTests).
- `dotnet test PhotoPrint.Tests` → **464 passed / 0 failed / 0 skipped** (~18s). Matches
  the resolution's 464/464 claim.
- Payment + idempotency subset (29 tests) → 29/29 pass against clean source.
- INFO-1 relational test in isolation → 1/1 pass (real SQLite, ~5s).

---

## Per-item verdicts

| ID | Sev | prior status | v3 verdict | Evidence |
|----|-----|--------------|-----------|----------|
| QUAL-3 | 🟠 Med | deferred (v2) | **verified** | `IdempotencyKeyFilter.cs` is an `IActionFilter` registered controller-wide via `[ServiceFilter]` (`PaymentsController.cs:17`), scoped DI (`Program.cs:40`). `OnActionExecuting` reads the header once, whitespace-normalizes to null (`:24-25`), stashes into `HttpContext.Items` (`:26`), logs the missing-key warning with correlation id (`:30-33`). Actions read via `HttpContext.GetIdempotencyKey()` (`PaymentsController.cs:109`, `HttpContextExtensions.cs:25-26`). `EndpointLabel` (`:39-46`) reproduces the prior labels exactly: `api/payments/stripe/intent`→`stripe.intent`, `api/payments/euplatesc/initiate`→`euplatesc.initiate`. Filter fires only for the two payment actions (the only actions on the controller). On the 401 path authz short-circuits before action filters; on an invalid-model 400 the `[ApiController]` ModelState filter (order −2000) short-circuits before this filter (order 0) — both match the old behavior, where the warning sat in the action body and ran only after auth + model binding. |
| QUAL-4 | 🟠 Med | deferred (v2) | **verified** | `CreateIntentAsync<TResponse>` (`PaymentsController.cs:99-127`) holds the resolve→replay→compute→persist→respond shape once; Stripe/EuPlatesc are thin adapters (`:46-65`, `:73-89`). Replay-vs-compute semantics preserved: `var cached = cachedValue(order); if (WasIdempotentReplay && cached is not null) return Ok(...)` else `computeAndApplyAsync` + single `SaveChangesAsync` + return. The **"replay but cached value is null" recovery fall-through** is intact and now applies symmetrically to both processors (Stripe previously had it; behavior identical). BUG-4 preserved: Stripe keyed by `o.Id.ToString()` (`:59`). Log line preserved as `payments.idempotency.replay processor={Processor} order_id={OrderId}` (`:120`) — the EuPlatesc literal `processor=EuPlatesc` became a `{Processor}` placeholder, same rendered output. |
| BUG-5 | 🟡 Low | deferred (v2) | **verified** | Migration `Up` (`20260527075359_AddOrderIdempotencyKey.cs:20-48`) gates on `migrationBuilder.ActiveProvider == "Npgsql..."`. Npgsql → `character varying(N)` (respects maxLength) + filtered unique index `filter: "\"IdempotencyKey\" IS NOT NULL"` matching the runtime `HasFilter` (`PhotoPrintDbContext.cs:307-308`). SQLite/else → `type:"TEXT"`, `filter:null` — **byte-identical to the original**. `Down` unchanged; model snapshot NOT touched (consistent with the documented residual). Editing the applied migration is safe here: SQLite dev DBs use `EnsureCreated` not migrations, and no Postgres DB has applied it pre-deployment. **Does NOT fix:** the snapshot is still SQLite-flavored, so scaffold-time drift under Npgsql is only reduced; and the `decimal`/`numeric` column-typing parity (a v2 follow-up suggestion) is out of this finding's scope (the DbContext already sets `decimal(10,2)` for non-SQLite at runtime, `:311-313`). |
| INFO-1 | ⚪ Info | raised (v2) | **verified** | `PaymentIdempotencyRelationalTests.cs` + `SqlitePaymentFactory.cs` run the cross-tenant scenario against a real shared-in-memory SQLite DB whose schema is built by `EnsureCreated()` in `CreateHost` (`SqlitePaymentFactory.cs:52-61`), which materializes the unique `ix_orders_idempotency_key`. Test asserts B gets **409** (`:54`), exactly one order carries the key and it is A's (`:59-63`). **Proven non-vacuous:** I disabled the `collidesWithOtherCaller` 409 branch in `OrderService.cs` and the relational test went **409→500** (the raw `DbUpdateException` rethrow path), confirming B's INSERT genuinely collides on the enforced index and the 409 originates from that path. Reverted; tree clean. |
| INFO-2 | ⚪ Info | raised (v2) | **accept-wontfix** | Stale cross-tenant key → 409 to the second tenant remains the accepted consequence of the global unique index (`OrderService.cs:184-188`). GUID keys → astronomically unlikely; the 409 is safe and non-disclosing. Resolves only with `(owner,key)` uniqueness, correctly batched with BUG-5's deferred residual. Rationale sound. |

**4 verified · 1 accept-wontfix · 0 reopened.**

---

## NEW findings (surfaced / introduced by the v3 work)

### 🟠 BUG-6 — `OrderNumberService` has no SQLite branch; the Development env runs on SQLite → order creation 500s
`src/PhotoPrint.API/Services/OrderNumberService.cs:15-45`, `src/PhotoPrint.API/Program.cs:26,137`,
`src/PhotoPrint.API/appsettings.Development.json:2`, surfaced by `SqlitePaymentFactory.cs:41-45`.

**Confirmed (latent, reachable in Development).** `GenerateAsync` has exactly two branches:
InMemory → `Orders.CountAsync`; **else → Postgres-only SQL** (`DO $$ … pg_sequences …
CREATE SEQUENCE`, then `SELECT nextval('"…"')`). There is **no SQLite branch**, yet:
- `appsettings.Development.json` sets `"DatabaseProvider": "Sqlite"`, and `Program.cs`
  has a whole SQLite startup-schema block (`:137-169`) — SQLite is a first-class runtime
  provider, not just a test artifact (cf. MEMORY: dual DB SQLite+Postgres, SQLite for
  local/dev/test).
- Running the API locally (Development → SQLite) and creating a payment intent calls
  `CreateFromCartAsync` → `_orderNumberService.GenerateAsync` → the `DO $$` / `nextval`
  Postgres syntax → SQLite parse error → unhandled → **500 on every order creation**.

This is exactly the "why didn't the real code path just work?" signal the brief asked me
to chase: **`SqlitePaymentFactory` had to fake `IOrderNumberService`** (`:41-45`,
`SequentialOrderNumberService`) precisely *because the real service cannot run on SQLite*.
The fake makes the INFO-1 test pass while papering over a real defect. No existing test
exercises the real service on SQLite (the concurrency tests mock it; the relational
factory fakes it), which is why this stayed hidden. Not introduced by v3 — but v3 is the
first thing to make it visible, and it's a genuine dev-experience break.
**Fix:** add a SQLite branch (e.g. the InMemory count-based fallback is provider-agnostic
and already exists, or a SQLite-native sequence/`max+1` in a transaction). Add a test that
runs the *real* `OrderNumberService` against SQLite.

### 🟡 DOC-4 — QUAL-3 refactor dropped OPS-1's grep-able `TODO(bolt-035-followup)` token and the ddd-02 pointer
`src/PhotoPrint.API/Filters/IdempotencyKeyFilter.cs:11-13` (was `PaymentsController.cs:117-120` @ `b52f4b6`).

**Confirmed (observability/tracking regression — behavior unchanged).** OPS-1 was verified
at v2 on a comment that carried a structured, grep-able marker:
`// … TODO(bolt-035-followup): enforce required key.` plus a pointer to
`memory-bank/bolts/035-payment-idempotency (ddd-02)`. The QUAL-3 refactor moved the warning
into `IdempotencyKeyFilter` and replaced that comment with prose only — *"See OPS-1: the
warning escalates to a 400 once the FE always sends a key."* `grep -rn "TODO(bolt-035-followup)"`
now returns **nothing**, and the ddd-02 pointer is gone. The missing-key→400 breaking change
is still documented in prose, but the affordance OPS-1 explicitly asked for (a backlog-sweep
hit) has regressed. The warning *behavior* is fully preserved, so OPS-1 itself stays
verified; this is a fresh low-severity doc finding against the refactor.
**Fix:** restore a `// TODO(bolt-035-followup): …` line (+ the ddd-02 reference) in the
filter's missing-key branch.

---

## Regression checks on previously-verified findings touched by v3

The v3 controller refactor sits on top of the BUG-1/SEC-1/BUG-3 service logic, which it did
**not** modify (`OrderService.cs` unchanged between `b52f4b6` and `b6198b6`). Spot-checked:

- **BUG-1 / SEC-1** — service-layer catch + owner-scoped resolution untouched; the
  SQLite concurrency regression tests (`OrderServiceIdempotencyConcurrencyTests`) remain
  intact and meaningful (real `ix_orders_idempotency_key`). No regression.
- **BUG-4** — Stripe still keyed by `order.Id.ToString()` after the refactor
  (`PaymentsController.cs:59`); integration assertion `dto1.OrderId.ToString()` still green.
- **QUAL-5** — the filter reuses `HttpContext.GetCorrelationId()` (`IdempotencyKeyFilter.cs:32`);
  no raw-string `Items` access reintroduced.
- **DOC-1 / DOC-2** — DbContext + OrderService comments unchanged.

## Adversarial probes (results)

- **INFO-1 non-vacuity** — disabled the 409 branch → relational test 409→**500**. The test
  genuinely depends on the enforced unique index. (Decisive.)
- **Filter scope / spurious warnings** — `PaymentsController` has only the two payment
  actions; no third action gets a spurious missing-key warning. Webhooks live on a separate
  controller, unaffected.
- **Filter on 401 / invalid-model paths** — authz and the ModelState filter short-circuit
  before `IdempotencyKeyFilter`; warning timing matches the old in-action-body behavior.
- **`SizeId` seed workaround** (`PaymentFactory.cs:141`) — added to satisfy the SQLite
  `CartItem.SizeId` FK that InMemory ignored. Verified benign: pricing uses
  `Product.Sizes.FirstOrDefault(IsActive)`, **not** `CartItem.SizeId` (`OrderService.cs:60-61`),
  so totals are identical across InMemory and SQLite tests. Strictly more-correct seed data.
- **Double schema-creation** — the `Program.cs` SQLite startup block is gated on
  `DatabaseProvider=="Sqlite"`; `SqlitePaymentFactory` leaves config at the default
  (Postgres), so only the factory's `CreateHost` `EnsureCreated()` builds the schema. No
  conflict. (This same gate is what makes BUG-6 reachable in Development.)
- **`GetIdempotencyKey` when the filter never ran** — `Items[...] as string` → null → treated
  as no-key. Safe degradation.

---

## Recommendation

**Approve with follow-ups.** The four v3 items are correctly implemented and verified; the
refactor is behavior-preserving and the new relational test is real, not vacuous. Nothing
regressed. Two follow-ups for the backlog:

1. 🟠 **BUG-6** — give `OrderNumberService` a SQLite branch (the Development env runs on
   SQLite and currently 500s on order creation); add a test exercising the *real* service on
   SQLite rather than faking it. Near-term — it breaks the documented local-dev path.
2. 🟡 **DOC-4** — restore the grep-able `TODO(bolt-035-followup)` token (+ ddd-02 pointer) in
   `IdempotencyKeyFilter`.
3. (Carried) BUG-5 residual (per-provider migration assemblies + decimal-typing parity) and
   INFO-2 both resolve with `(owner,key)` uniqueness — one batched dual-DB parity pass.
