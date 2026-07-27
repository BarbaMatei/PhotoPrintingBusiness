---
type: review
version: 8
supersedes: 7
target: Bolt 035 — payment idempotency
branch: feat/bolt-035-payment-idempotency
commit: 50fc692
date: 2026-06-19
reviewer: multi-lens harness (Opus 4.8) — 7 lenses + build/test verify, 2 adversarial skeptics per finding
method: FRESH CLEAN-ROOM AUDIT — performed without reading v1–v7 or any resolution file; findings derived only from the code + tests at commit 50fc692. IDs below are v8-local and do not map to earlier versions.
verdict: approve-with-followups
blockers: []
findings: 0 H / 2 M / 9 L / 7 Cleanup (18 deduped from 28 raw; 1 refuted)
---

# Review v8 — Bolt 035 payment idempotency (independent fresh audit)

> This is a deliberately **unbiased re-audit**: every lens and verifier was barred from
> reading `reviews/` so nothing anchored on the prior v1–v7 record. Each finding was raised
> by an isolated lens and then put through **two independent adversarial skeptics** (one
> hunting for an existing guard, one trying to construct the concrete failing trace under
> Postgres). Verdicts: **confirmed** (trace constructible from the code), **plausible**
> (realistic, not provable either way), **refuted** (an existing guard prevents it).

## Verdict

**Approve with follow-ups. No high-severity or blocking defects.** The feature is correctly
implemented on the path the tests exercise, and I verified the one fact that could have made
this a blocker: the hard-coded Postgres constraint literal `pg.ConstraintName ==
"ix_orders_idempotency_key"` ([OrderService.cs:199](../../src/PhotoPrint.API/Services/OrderService.cs#L199))
**exactly matches** the configured index name (`HasDatabaseName("ix_orders_idempotency_key")`,
[PhotoPrintDbContext.cs:310](../../src/PhotoPrint.API/Data/PhotoPrintDbContext.cs#L310), and
the migration's `CreateIndex name:` at [line 57](../../src/PhotoPrint.API/Migrations/20260527075359_AddOrderIdempotencyKey.cs#L57)).
So production correctness is *plausibly* sound — the problem is it is **entirely unverified
against the real provider** (see DB-1).

Recommended before relying on this in production: close **DB-1** (a Postgres-backed
regression) and **OBS-1** (cross-tenant collision observability). **BUG-1**'s five-lens
convergence makes it the highest-value LOW to harden. Nothing here blocks merge.

## Verification & build

| Check | Result |
|-------|--------|
| `dotnet build PhotoPrint.sln -c Debug` | ✅ 0 errors, 4 warnings (all the benign `NU1603` Stripe.net 46.3.0→47.0.0 NuGet notice, unrelated to this change) |
| `dotnet test PhotoPrint.Tests` | ✅ **474 passed / 0 failed / 0 skipped** (~4s) |
| Postgres coverage | ❌ **Zero** — no Npgsql/Testcontainers fixture exists; every test runs on SQLite (`:memory:`) or EF InMemory |

The green run is **real** — the suite cleverly substitutes a real shared-connection SQLite DB
wherever EF InMemory can't enforce the unique index, which is what makes the cross-tenant 409
and loser-replay tests meaningful. But **SQLite is a single-writer engine and production is
PostgreSQL**, so the entire Postgres arm of the feature is dead code from the suite's
perspective. *A green suite that only proves SQLite single-writer behavior is itself the
finding* (DB-1/BUG-4).

## Cross-lens convergence (independent corroboration)

Findings that ≥3 isolated lenses landed on without seeing each other — strong real-signal:

- **BUG-1** (SQLite message-substring detection) — flagged by **5 lenses** (correctness×3,
  db-parity, quality). The strongest convergence in this audit.
- **QUAL-1** (`GetByIdempotencyKeyAsync` dead code) — flagged by **3 lenses** (2 correctness + quality).
- **BUG-2** (EuPlatesc replay regenerates a different URL) — flagged by **3 lenses** (concurrency, contract, security).

## Findings

| ID | Sev | Verdict | Title | Location |
|----|-----|---------|-------|----------|
| DB-1  | 🟠 M | confirmed | Entire Postgres production DB path is unexercised by tests | tests / `OrderService.cs:197-199` |
| OBS-1 | 🟠 M | confirmed | Cross-tenant key-collision 409 has no distinct structured log | `OrderService.cs:178` |
| BUG-1 | 🟡 L | plausible | SQLite idempotency-violation keyed off a message substring, not a structured code (×5 convergence) | `OrderService.cs:194-196` |
| SEC-1 | 🟡 L | confirmed | Global single-column key uniqueness = cross-tenant existence oracle + key-squatting | `OrderService.cs:162-180`, `PhotoPrintDbContext.cs:308-310` |
| BUG-2 | 🟡 L | confirmed | EuPlatesc recovery-replay regenerates a *different* redirect URL (no gateway idempotency key) | `PaymentsController.cs:131-138` |
| SEC-2 | 🟡 L | confirmed | Idempotency-Key not trimmed → whitespace-padded variants bypass dedupe | `IdempotencyKeyFilter.cs:23-31` |
| BUG-3 | 🟡 L | confirmed | Stale-key free + new-order INSERT are not atomic (no enclosing transaction) | `OrderService.cs:99-159` |
| BUG-4 | 🟡 L | plausible | Concurrent same-key INSERT can collide on the OrderNumber index first (SQLite) → 500 not replay | `OrderService.cs:162-201`, `OrderNumberService.cs:23-26` |
| REQ-1 | 🟡 L | confirmed | Expired key reclaimed only by its original owner → 24h-window contract not honored cross-caller | `OrderService.cs:99-104` |
| DB-2  | 🟡 L | confirmed | Model snapshot is SQLite-flavored → next Npgsql migration scaffolds a phantom diff (known/deferred) | `PhotoPrintDbContextModelSnapshot.cs:314-374` |
| QUAL-2| 🟡 L | confirmed | `OrderService.ResolveUnitPrice` duplicates & *diverges* from `CartService.ResolveUnitPrice` | `OrderService.cs:413-429` |
| QUAL-1| ⚪ C | confirmed | `GetByIdempotencyKeyAsync` is dead production code (×3 convergence) | `OrderService.cs:220-229`, `IOrderService.cs:33` |
| QUAL-3| ⚪ C | confirmed | Cart-seeding graph duplicated across 3 test fixtures (already drifting) | `OrderServiceTests.cs:43-91` +2 |
| QUAL-4| ⚪ C | confirmed | Concurrency test hand-builds the "winner" order graph with magic-number totals | `OrderServiceIdempotencyConcurrencyTests.cs:166-210` |
| QUAL-5| ⚪ C | confirmed | `CreateIntentAsync` replay-logging branches duplicate event-shape / double-check replay state | `PaymentsController.cs:115-138` |
| QUAL-6| ⚪ C | confirmed | `ExceptionHandlerMiddleware` resolves `IHostEnvironment` two different ways per request | `ExceptionHandlerMiddleware.cs:88-160` |
| OBS-2 | ⚪ C | confirmed | 409 `divergentFields` body undocumented in `ProducesResponseType` | `PaymentsController.cs:45,72` |
| OBS-3 | ⚪ C | confirmed | Transitional missing-key event logged at **Warning** on every request (log noise) | `IdempotencyKeyFilter.cs:49-52` |
| — | 🟡 L | **refuted** | Stale free-then-insert Postgres interleaving (folded into BUG-3 — self-heals to 409) | `OrderService.cs:99-143` |

---

### 🟠 DB-1 — Entire Postgres production DB path is unexercised by tests · *confirmed*

The whole production (Postgres) surface of this feature is verified by **nothing**:

1. **The Npgsql arm of `IsIdempotencyKeyViolation`** ([OrderService.cs:197-199](../../src/PhotoPrint.API/Services/OrderService.cs#L197)) — the `SqlState == "23505" && ConstraintName == "ix_orders_idempotency_key"` branch that turns the canonical concurrent double-submit into a clean 409/replay — is only reachable under Postgres. Tests only ever hit the SQLite arm.
2. **The filtered unique index** `WHERE "IdempotencyKey" IS NOT NULL` ([PhotoPrintDbContext.cs:312](../../src/PhotoPrint.API/Data/PhotoPrintDbContext.cs#L312)) is Npgsql-only.
3. **The migration DDL itself is never run.** Every fixture and `Program.cs` use `EnsureCreated()` (builds schema from the *model*), never `Migrate()`. A repo-wide grep for `Migrate(`/`MigrateAsync`/`GetPendingMigrations` in tests returns **zero** matches. So the migration's provider-aware `AddColumn` type strings, its filter expression, and its `Down()` reversibility are exercised by nothing — drift between model and migration would surface only at deploy time.

**Concrete failure:** if Npgsql ever surfaces `ConstraintName` differently than the EF-default literal (a future EF/Npgsql version, a quoting/truncation difference), the cross-tenant double-submit in prod falls through `_ => false`, the raw `DbUpdateException` propagates, and the client gets a **500 on the exact request this bolt exists to make idempotent** — and CI stays green. (I confirmed the literal matches the configured name *today*, so this is residual risk, not a present mismatch.)

**Fix:** add at least one Testcontainers-Postgres regression that (a) applies the real migration via `db.Database.Migrate()`, (b) drives two genuinely concurrent same-key `CreateFromCart` calls and asserts exactly one order + a 409/replay for the loser, and (c) asserts the live Npgsql `ConstraintName` equals the hard-coded literal. *(Merges raw F13 + F14; corroborated by the build/test verifier.)*

### 🟠 OBS-1 — Cross-tenant key-collision 409 has no distinct structured log · *confirmed*

Only the **same-caller divergent-payload** path (`IdempotencyConflictException`) gets the reserved `payments.idempotency.conflict` structured event ([ExceptionHandlerMiddleware.cs:70-73](../../src/PhotoPrint.API/Middleware/ExceptionHandlerMiddleware.cs#L70)). When a key is held by a **different** caller, `OrderService` throws a plain `ConflictException` ([OrderService.cs:178](../../src/PhotoPrint.API/Services/OrderService.cs#L178)), which the middleware logs only as the generic `"Handled exception ConflictException"` warning — indistinguishable from any unrelated 409.

**Concrete failure:** the borrowed/guessed-key / IDOR-probe abuse signal — the single thing you'd want to grep and alert on during a duplicate-charge incident — is invisible in the logs, with no key and no collision-class marker. **Fix:** make it a first-class observable — a dedicated `IdempotencyKeyTakenException` mapped to 409, or a reserved `payments.idempotency.cross-tenant-conflict` event with the correlation id, mirroring the existing treatment of `IdempotencyConflictException`. *(Raw F18.)*

### 🟡 BUG-1 — SQLite idempotency-violation detection keys off a message substring · *plausible · ×5 convergence*

[`IsIdempotencyKeyViolation`](../../src/PhotoPrint.API/Services/OrderService.cs#L191) identifies providers **asymmetrically**: Postgres matches the structured `ConstraintName == "ix_orders_idempotency_key"` (robust), but SQLite matches `sqlite.Message.Contains("IdempotencyKey")` — a human-readable free-text substring. SQLite exposes no structured constraint name, so the catch depends on the message wording `UNIQUE constraint failed: Orders.IdempotencyKey`.

**Concrete failure:** if a `Microsoft.Data.Sqlite` upgrade re-words the message, or the column is ever renamed, the `when` filter silently returns false, the `DbUpdateException` propagates, and the canonical double-submit degrades from a clean 409 to a **500 on the dev/test (and any SQLite-backed) provider** — with no compile-time signal. One skeptic noted the dependency is pinned at 8.0.11 so there is **no present bug** (hence *plausible*, not confirmed), but five independent lenses surfaced this as the load-bearing fragility. **Fix:** match the SQLite extended result code `SQLITE_CONSTRAINT_UNIQUE` (2067) via `SqliteExtendedErrorCode`, and/or share a single index-name constant between the DbContext config and this matcher so a rename is a compile break, not a silent regression. Add a SQLite test asserting a real unique-index collision classifies as an idempotency violation. *(Merges raw F3, F5, F9, F16, F25.)*

### 🟡 SEC-1 — Global key uniqueness is a cross-tenant existence oracle + key-squatting vector · *confirmed*

The **lookup** is correctly tenant-scoped (`FindKeyHolderAsync` filters on `userId`/`guestSessionId` — good), but the **uniqueness constraint is global single-column** ([PhotoPrintDbContext.cs:308-310](../../src/PhotoPrint.API/Data/PhotoPrintDbContext.cs#L308)). An authenticated attacker presenting another tenant's key finds nothing in their scoped lookup, their INSERT collides on the global index, and they get a 409 — whereas a free key yields 200.

**Concrete failure:** by observing **409-vs-200** an attacker learns whether a guessed key is globally in use (an existence oracle), and by pre-submitting predicted keys can **squat** them so a victim's legitimate first request 409s (DoS). Real-world exploitability is **low** because keys are client-chosen GUIDs (unpredictable) and the 200 probe self-limits (it creates a real order + charge on the attacker's own account). **Fix:** scope uniqueness per-tenant — composite `(UserId, IdempotencyKey)` / `(GuestSessionId, IdempotencyKey)` index — and update `IsIdempotencyKeyViolation` accordingly; or document as an accepted residual in the bolt's threat notes. Shares a root cause with REQ-1. *(Raw F10.)*

### 🟡 BUG-2 — EuPlatesc recovery-replay regenerates a *different* redirect URL · *confirmed · ×3 convergence*

Stripe forwards an explicit `RequestOptions.IdempotencyKey` keyed by `order.Id` → gateway-side dedupe. The **EuPlatesc path has no gateway idempotency key**; its only protection is persisting the URL once and replaying it. But on the recovery-replay branch (`WasIdempotentReplay == true` with a null cached value, [PaymentsController.cs:131-138](../../src/PhotoPrint.API/Controllers/PaymentsController.cs#L131)) it rebuilds the URL via `BuildInitiateUrl`, which embeds a fresh timestamp + nonce each call.

**Concrete failure:** order created but `EuPlatescRedirectUrl` not yet persisted (crash-before-persist); two concurrent retries with the same key both see `cached == null`, both rebuild → two **different** signed URLs, last-writer-wins on the row, and each caller receives its own URL. No double-charge (stable `invoice_id == order.Id` makes EuPlatesc map both to one order), but the documented *"replay returns the stored value verbatim"* invariant is violated, and that gateway-side dedupe is implicit/undocumented. **Fix:** re-read the persisted URL inside the recovery scope and reuse it if present, or take a short row lock (`SELECT … FOR UPDATE` on Postgres); at minimum document the Stripe/EuPlatesc asymmetry. *(Merges raw F2, F8, F12.)*

### 🟡 SEC-2 — Idempotency-Key not trimmed → whitespace variants bypass dedupe · *confirmed*

[`IdempotencyKeyFilter`](../../src/PhotoPrint.API/Filters/IdempotencyKeyFilter.cs#L23) normalizes whitespace-**only** values to null (`IsNullOrWhiteSpace`) and length-caps, but never **trims** a key that contains non-whitespace. The raw value is stored verbatim and used as the exact unique-index key, so `"abc"`, `" abc"`, and `"abc "` are three distinct keys.

**Concrete failure:** a client or buggy proxy/retry layer that resends the same logical key once padded defeats idempotency — the second request matches no holder, the INSERT doesn't collide, and a **second order + second Stripe PaymentIntent** are created: the exact double-charge the feature prevents. **Fix:** `var key = raw.Trim(); key = string.IsNullOrEmpty(key) ? null : key;` before the null/length checks. *(Raw F11.)*

### 🟡 BUG-3 — Stale-key free + new-order INSERT are not atomic · *confirmed*

When a stale (>24h) holder owns the key, the code nulls its `IdempotencyKey` and commits in its **own** `SaveChangesAsync` ([OrderService.cs:103](../../src/PhotoPrint.API/Services/OrderService.cs#L103)), then INSERTs the new order in a **separate** `SaveChangesAsync` ([line 159](../../src/PhotoPrint.API/Services/OrderService.cs#L159)). `CreateFromCartAsync` has no enclosing transaction (confirmed: no `BeginTransaction`/`TransactionScope` anywhere in `OrderService`).

**Concrete failure (stale-window path only):** after the key is freed, if the process crashes or the new INSERT fails for an unrelated reason (FK violation, deadlock, cart concurrently emptied), the stale order has permanently **lost its key linkage and no replacement exists** — a subsequent retry finds no holder and creates yet another order; the key no longer dedupes. Bounded to already-expired keys, hence LOW. **Fix:** wrap free+INSERT in one transaction, or null the holder's key on the in-memory entity and let it flush in the **same** `SaveChanges` as the INSERT. *(Raw F1.)*

> **Refuted variant (F17):** the *concurrent-interleaving* version of this — two parallel writers reusing a stale key on Postgres producing nondeterministic replay-vs-409 — was **refuted**: both skeptics traced that the post-collision recovery path self-heals to a clean 409 (exactly one order persists either way). The atomicity gap is real (above); the concurrency-nondeterminism angle is not a defect.

### 🟡 BUG-4 — Concurrent same-key INSERT can collide on the OrderNumber index first (SQLite) → 500 · *plausible*

The recovery catch only treats a `DbUpdateException` as idempotent when `IsIdempotencyKeyViolation` matches `ix_orders_idempotency_key`. But on SQLite, [`OrderNumberService.GenerateAsync`](../../src/PhotoPrint.API/Services/OrderNumberService.cs#L23) uses `Orders.CountAsync() + 1`, so two racing same-key requests can also generate the **same OrderNumber**; the INSERT then violates `ix_orders_order_number`, which `IsIdempotencyKeyViolation` returns false for → the exception propagates as a **500** instead of replaying the winner.

Production Postgres is safe (per-year sequence yields unique numbers, so only the idempotency index can collide) — the gap is **SQLite-only**, and the concurrency test masks it by mocking `IOrderNumberService` with `Interlocked.Increment` (always-unique). **Fix:** widen recovery to recognize an OrderNumber unique-violation and retry number generation, or add a concurrency test using the **real** `OrderNumberService` against shared SQLite. Related to DB-1. *(Raw F4.)*

### 🟡 REQ-1 — Expired key reclaimed only by its original owner · *confirmed*

The contract documents a 24h idempotency window after which the key is free (`Order.cs`, `IOrderService` XML doc). But stale-key reclamation (nulling the holder's key) only runs when the **same** caller resubmits ([OrderService.cs:99-104](../../src/PhotoPrint.API/Services/OrderService.cs#L99)). A stale row owned by caller A is never freed for anyone else, and the global unique index then **permanently blocks** any other caller from that key.

**Concrete failure:** caller B submits A's expired key K; B's scoped lookup returns null, B's INSERT collides on the global index, and B gets a 409 for a key the contract says is *free*. Improbable with UUID keys but contradicts the stated semantics. **Fix:** either document that keys are globally unique forever (not freed after 24h except for self-reuse), or reclaim stale rows regardless of owner (guarded against cross-tenant data leakage). Shares a root cause with SEC-1 (global single-column index + caller-scoped lookup). *(Raw F19.)*

### 🟡 DB-2 — Model snapshot is SQLite-flavored → phantom migration diff · *confirmed (known/deferred)*

[`PhotoPrintDbContextModelSnapshot`](../../src/PhotoPrint.API/Migrations/PhotoPrintDbContextModelSnapshot.cs) records the idempotency columns as `TEXT` and the unique index with **no** `HasFilter`, while the runtime Npgsql model is `character varying(N)` + a filtered index. EF diffs the next migration against this snapshot, so scaffolding any future Npgsql migration emits spurious `AlterColumn (TEXT→varchar)` + `Drop/CreateIndex` operations on already-correct prod columns — and on a large `Orders` table that index rebuild is a lock/rewrite.

This is **already acknowledged** as deferred in the migration's own comment ([lines 19-28](../../src/PhotoPrint.API/Migrations/20260527075359_AddOrderIdempotencyKey.cs#L19)), so it is a known accepted cost, not an unflagged defect — but it remains a live trap for the next author. **Fix (deferred):** per-provider migration assemblies, or a CI guard that fails when an unexpected Orders idempotency `AlterColumn`/`Drop-CreateIndex` appears in a scaffolded migration. *(Raw F15.)*

### 🟡 QUAL-2 — `ResolveUnitPrice` duplicates and *diverges* from `CartService` · *confirmed*

[`OrderService.ResolveUnitPrice`](../../src/PhotoPrint.API/Services/OrderService.cs#L413) is commented *"mirrors CartService.ResolveUnitPrice"*, but they are **not** mirrors: `CartService` resolves the pricing tier from the per-group **total** copies; `OrderService` resolves it from a single cart item's quantity. The tier logic is otherwise identical, so the duplication is a maintenance trap and the comment hides a behavioral difference.

The divergent tier-basis is **pre-existing** (not introduced by this bolt), flagged here because the idempotency replay's `DivergentFields` check now compares `TotalRon` computed from this path. **Fix:** extract one shared tier-resolution helper both services call with their own quantity semantics; correct or remove the misleading comment. *(Raw F24. One skeptic refuted the secondary "idempotency-flip" sub-claim; the finding already hedged it, so it stands at LOW on the duplication core.)*

---

### ⚪ Cleanup

- **QUAL-1 · `GetByIdempotencyKeyAsync` is dead production code** *(confirmed, ×3 convergence)* — declared on `IOrderService` ([:33](../../src/PhotoPrint.API/Services/IOrderService.cs#L33)) and implemented ([OrderService.cs:220-229](../../src/PhotoPrint.API/Services/OrderService.cs#L220)) but the only callers are tests; production resolves idempotency entirely inside `CreateFromCartAsync` via the private `FindKeyHolderAsync`. Remove it (retarget its SEC/staleness tests at `CreateFromCartAsync`), or add the intended caller. *(Merges F6, F7, F22.)*
- **QUAL-3 · Cart-seeding graph duplicated across 3 test fixtures** *(confirmed)* — `OrderServiceTests`, `OrderServiceIdempotencyConcurrencyTests`, and `PaymentFactory` each rebuild the identical `Product/ProductSize/PricingTier/ProductFinish/Upload/CartItem` graph, and they have **already drifted** (`OrderServiceTests`' `CartItem` omits `SizeId`; the SQLite fixtures set it). Hoist a single `TestCartSeed` helper. *(Raw F23.)*
- **QUAL-4 · Concurrency test hand-builds the "winner" with magic-number totals** *(confirmed)* — `…LoserReplaysWinner_OneOrder` hardcodes `SubtotalRon=6.00/Shipping=20.00/Total=26.00`, an implicit copy of the service's `2.00×3 + 20.00` math; a seed/shipping tweak silently flips the test from replay to 409 for the wrong reason. Build the winner by calling the real `CreateFromCartAsync` on a second context. *(Raw F28.)*
- **QUAL-5 · `CreateIntentAsync` replay-logging branches duplicate** *(confirmed)* — the cached-replay and recovery branches ([PaymentsController.cs:115-138](../../src/PhotoPrint.API/Controllers/PaymentsController.cs#L115)) repeat the event-shape and check `WasIdempotentReplay` twice; collapse to one `switch` over `(WasIdempotentReplay, cached is not null)`. *(Raw F26.)*
- **QUAL-6 · `ExceptionHandlerMiddleware` resolves `IHostEnvironment` two ways** *(confirmed)* — it holds the injected `_environment` field yet `WriteProblemDetailsAsync` re-resolves env via `RequestServices` (because it's `static`). Make it an instance method using `_environment`; delete the service-locator hop. *(Raw F27.)*
- **OBS-2 · 409 `divergentFields` body undocumented** *(confirmed)* — both endpoints declare `ProducesResponseType(Status409Conflict)` with no body type, but the runtime 409 always carries a `divergentFields` ProblemDetails extension. Generated clients never see the one field that tells the FE which inputs to fix. Add a typed problem-details DTO. *(Raw F20.)*
- **OBS-3 · Transitional missing-key warning is log noise** *(confirmed)* — while the FE doesn't yet send keys, `payments.idempotency.missing-key` logs at **Warning** on 100% of payment requests, which can trip warning-rate alerts. Log at Information until the key becomes required. *(Raw F21.)*

## What I checked and did **not** flag

- The idempotency **lookup** is correctly tenant-scoped (`FindKeyHolderAsync`) — the marquee IDOR (one user reading another's order via a replayed key) is **not** present; the cross-tenant tests prove it on SQLite. SEC-1 is about the *uniqueness* scope, a weaker oracle/DoS surface.
- Stripe's gateway-side idempotency (keyed by `order.Id`) correctly prevents a double **charge** even where this code has a replay race (BUG-2).
- The post-collision recovery path self-heals to a deterministic single order (the F17 concurrency-interleaving concern was refuted).
- DI lifetimes, filter ordering, and the `IOrderService`/`IStripePaymentGateway` signature changes — all call sites updated; no broken contracts surfaced.

---

*Process: [reviews/README.md](../README.md). This v8 is a fresh clean-room audit; the fixer responds in `resolution-v8.md`, and only a re-review (v9) may flip a finding to `verified`.*
