---
type: review
version: 5
supersedes: 4
target: Bolt 035 — payment idempotency
branch: feat/bolt-035-payment-idempotency
commit: 224c711e09df787e2cfd5e456f72549ece4ed954
base: main (merge-base 50213b1)
date: 2026-06-19
reviewer: multi-lens (clean-room — lenses blinded to v1–v4)
verdict: approved
blockers: []
findings: { high: 0, medium: 3, low: 6, cleanup: 6 }
recommended_before_deploy: [DB-2, OBS-1]
---

# Review v5 — Bolt 035 payment idempotency (clean-room re-review)

## What this is

A **fresh, independent** multi-lens pass over the full bolt-035 diff at commit `224c711`,
requested explicitly as an *unbiased* review. Every read-only lens (two correctness
finders, security, PR/requirements, quality, DB-parity) ran in an isolated subagent that
was **forbidden from reading `reviews/`** — none saw findings v1–v4. The finding-ID
comments embedded in the code (`BUG-1`, `SEC-1`, `QUAL-3`, …) were handed to the lenses as
the *author's claims to be independently verified*, not as ground truth. The IDs below are
this review's own (v5-scoped); they do **not** map to the comment IDs in the source.

This is therefore not a verification of v4's resolution — it is a clean-room judgment of the
code as it stands. That it lands on the same bottom line as v4 (no blockers) is independent
corroboration, not anchoring.

## Verdict: ✅ Approved — 0 blockers

Build green (0 errors), **466/466 tests pass**. Tenant isolation — the highest-risk surface —
holds on every read path traced (fresh, stale, and post-collision recovery), and is
exercised by real-SQLite tests that enforce the unique index the default InMemory suite
cannot. No High-severity findings.

Three Medium items are non-blocking hardening/parity/observability gaps. Two are cheap and
prod-relevant enough to fold in before deploy (**DB-2**, **OBS-1**); none gates merge.

| Severity | Count |
|---|---|
| 🔴 High | 0 |
| 🟠 Medium | 3 |
| 🟡 Low | 6 |
| ⚪ Cleanup | 6 |

## Findings

| ID | Sev | File | Summary | State |
|----|-----|------|---------|-------|
| DB-1 | 🟠 | Migrations/PhotoPrintDbContextModelSnapshot.cs | SQLite-flavored snapshot → phantom migration on next Npgsql scaffold | Confirmed |
| DB-2 | 🟠 | Models/Order.cs · PhotoPrintDbContext.cs:296 | `StripeClientSecret varchar(255)` zero-headroom → prod-only 500 after a successful charge | Plausible |
| OBS-1 | 🟠 | Middleware/ExceptionHandlerMiddleware.cs:103-133 | 409 `divergentFields` emitted only in non-Development, and no test asserts the body | Confirmed |
| BUG-1 | 🟡 | Services/OrderService.cs:161 (`catch (DbUpdateException)`) | Broad catch infers the failure cause; unrelated DB error can surface as a misleading 409 or rethrow 500 | Plausible |
| SEC-1 | 🟡 | Services/OrderService.cs:235 | Scope predicate degenerates to "any guestless order" if userId AND guestSessionId are both null (not reachable today) | Plausible |
| SEC-2 | 🟡 | Filters/IdempotencyKeyFilter.cs:22-25 | Key length (spec: 1..80) not validated at the filter → >80-char key 500s on prod Postgres | Plausible |
| OBS-2 | 🟡 | (spec ddd-01:61) ExceptionHandlerMiddleware | Reserved `payments.idempotency.conflict` log line is never emitted; 409s only hit the generic warning | Confirmed |
| OBS-3 | 🟡 | Controllers/PaymentsController.cs:117-125 | Recovery-replay (replay + null cached value) re-calls the gateway and skips the `replay` log | Confirmed (by design, unobserved) |
| DOC-1 | 🟡 | Services/OrderService.cs:108-109 | Stale key freed in its own save, not "the same transaction as the insert" as ddd-02 claims | Confirmed |
| DOC-2 | ⚪ | memory-bank/.../ddd-02 | Spec doesn't state Stripe is keyed by the order id (the good implementation choice) — document it | Confirmed |
| QUAL-1 | ⚪ | Services/OrderService.cs | Two near-duplicate idempotency-resolution blocks (pre-INSERT + post-collision) | Confirmed |
| QUAL-2 | ⚪ | DbContext · OrderNumberService · Migration | Provider-name magic strings repeated 4× → `DbProviders` constants | Confirmed |
| QUAL-3 | ⚪ | Controllers/PaymentsController.cs:125 | Controller calls `_db.SaveChangesAsync` directly (altitude; pre-existing pattern) | Confirmed |
| QUAL-4 | ⚪ | Tests/Integration · Tests/Unit | Test-helper duplication (`SendStripeIntent`, `MakeRequest`, SQLite factory setup) | Confirmed |
| QUAL-5 | ⚪ | Services/OrderNumberService.cs:33 | EF1002 interpolated SQL (safe year int, not injection) + Postgres path is untested | Confirmed |

---

### 🟠 DB-1 — Model snapshot is SQLite-flavored; next migration scaffold emits a phantom diff

`PhotoPrintDbContextModelSnapshot.cs` records `IdempotencyKey`/`StripeClientSecret`/
`EuPlatescRedirectUrl` as `TEXT` and the unique index as **unfiltered**. The real Postgres
schema this migration builds is `character varying(N)` + a **filtered** index
(`WHERE "IdempotencyKey" IS NOT NULL`). The snapshot is EF's baseline for diffing the *next*
migration. With the Npgsql provider active, the next `dotnet ef migrations add` will see
model (filtered varchar) vs snapshot (unfiltered TEXT) and emit spurious `AlterColumn` +
`DropIndex`/`CreateIndex` operations — a confusing non-empty "phantom" migration (and a
real index rebuild / potential column rewrite on Postgres).

- **Impact:** scaffold-time trap for the next author; **not** a runtime/ValidateOnStart problem.
  The runtime model and *this* migration's filter strings match exactly, so there is no
  perpetual "model changed" drift at startup for the deployed index.
- **Status:** the migration comment (`AddOrderIdempotencyKey.cs`) discloses this and defers
  the real fix (per-provider migration assemblies). Acceptable as a tracked deferral, but it
  is a latent trap, not cosmetic.
- **Fix hint:** track the per-provider-migration-assembly follow-up explicitly, or regenerate
  the snapshot under Npgsql; at minimum, leave a breadcrumb so the next author expects the diff.

### 🟠 DB-2 — `StripeClientSecret varchar(255)` has zero headroom; prod-only failure after a successful charge

The column (`PhotoPrintDbContext.cs:296`) is sized exactly at Stripe's documented ID ceiling (255). Today's client secrets
(`pi_..._secret_...`) are ~60–90 chars, so it fits — but with no margin, and Stripe has
lengthened IDs before. On SQLite/InMemory (dev/test) `HasMaxLength` is **not enforced**, so an
over-length secret stores silently and every idempotency test stays green. On prod Postgres
`character varying(255)` throws `value too long` on `SaveChangesAsync` — which happens
**after** `CreatePaymentIntentAsync` already created the charge at Stripe, leaving a live
PaymentIntent with no persisted secret and a 500 to the client.

- This is precisely the SQLite-masks-Postgres parity gap the DB lens exists to catch.
- **Fix hint:** widen to `varchar(512)` or `TEXT`. Trivial migration; removes a sharp prod-only edge.

### 🟠 OBS-1 — 409 `divergentFields` is environment-gated and untested

The feature's documented client contract is "409 *naming divergent fields*" (ddd-01:115).
But in `ExceptionHandlerMiddleware.WriteProblemDetailsAsync`, the branch is
`if (exception != null && IsDevContext(context))` → in **Development** the response is the
anonymous diagnostic object (type/message/stackTrace) which has **no `divergentFields`**;
`divergentFields` is added only in the non-dev `else` branch (line 132). So:

1. In local dev the documented contract field is **absent** — a frontend developer building
   against the dev API never sees it.
2. **No test asserts `divergentFields` is in the body** in any environment — the 409 tests
   (`..._Returns409`, `..._DivergentProcessor_Returns409`) check only the status code. A green
   suite that never exercises the contract is itself the finding.

- **Fix hint:** add `divergentFields` to both branches (or move it before the dev/prod split),
  and add a test asserting the 409 body carries the expected field names.

### 🟡 BUG-1 — `catch (DbUpdateException)` infers the cause instead of confirming it

The post-INSERT recovery catches *any* `DbUpdateException` and then reasons about idempotency:
re-resolve the caller's holder, else a global `AnyAsync(key)` → `ConflictException`, else
rethrow. Two narrow misfires:

- If the INSERT fails for an **unrelated** reason (FK, NOT NULL, an `OrderNumber` unique
  collision on Postgres) *and* the same key happens to be held by another order, the code
  throws a misleading idempotency 409 that **masks** the real error.
- If the real cause **is** the idempotency collision but the other tenant's row is freed/deleted
  between the catch and `AnyAsync`, it rethrows as a 500 instead of the clean 409.

Both require a coincidence and are unlikely with fresh-UUID keys, hence Low. **Fix hint:**
scope the catch to the idempotency index — inspect the provider error (Postgres `23505` +
constraint name `ix_orders_idempotency_key`, SQLite `19`) before treating it as a key collision.

### 🟡 SEC-1 — both-null scope predicate degeneracy (defense-in-depth)

`FindKeyHolderAsync` (OrderService.cs:235) scopes with `(userId.HasValue ? o.UserId == userId : o.GuestSessionId == guestSessionId)`.
If a request ever reached this with **both** `userId` and `guestSessionId` null, it collapses to
`o.GuestSessionId == null`, matching every authenticated user's order — a borrowed key could
then resolve an arbitrary user's order. The security lens traced reachability: app-issued JWTs
always stamp a Guid `sub`, the guest handler always sets its claim, and the controller requires
authentication — so exactly one identity is always non-null. **Not exploitable today.** Flagged
only as fragility against future token-shape changes. **Fix hint:** reject-if-both-null guard at
the top of resolution.

### 🟡 SEC-2 — Idempotency-Key length not validated; >80 chars 500s on Postgres

The domain spec sets the key length at **1..80** (ddd-01:35), but `IdempotencyKeyFilter` only
normalizes whitespace-to-null — it does not enforce length. A >80-char key passes dev/test
(SQLite ignores `varchar(80)`) and then fails the prod Postgres INSERT with a truncation
`DbUpdateException`, which the recovery path rethrows as a 500. Self-inflicted (the caller harms
only their own request), no cross-tenant impact. **Fix hint:** validate length in the filter and
reject over-length keys with a 400, matching the documented constraint.

### 🟡 OBS-2 — reserved `payments.idempotency.conflict` log line is never emitted

ddd-01:61 reserves three structured log events so intent-020 observability can later wire metrics
without re-modelling: `replay`, **`conflict`**, `missing-key`. The code emits `replay`
(controller) and `missing-key` (filter) but **never `conflict`** — a 409 only produces the
generic `ExceptionHandlerMiddleware` "Handled exception" warning. Conflicts (a signal of
client bugs or key-reuse abuse) aren't distinctly observable. **Fix hint:** log
`payments.idempotency.conflict` (with correlation id + divergent field names) where the
`IdempotencyConflictException` is thrown or mapped.

### 🟡 OBS-3 — recovery-replay re-calls the gateway and skips the replay log

When the service returns `WasIdempotentReplay = true` but the processor's cached value is null
(an order created by an earlier attempt that died before persisting the secret/URL), the
controller's `cached is not null` guard falls through to `computeAndApplyAsync` and re-invokes
the gateway. This is **safe** — Stripe is keyed by the stable order id, so the same PaymentIntent
is returned (no double charge); EuPlatesc just rebuilds a redirect URL for the caller's own
order. But this completion path is undocumented, emits no `payments.idempotency.replay` line, and
looks like a fresh request in logs/metrics. **Fix hint:** document the recovery path and log it
distinctly.

### 🟡 DOC-1 — stale-key free is not "the same transaction" the design doc claims

ddd-02 (lines 124, 170) specifies the stale-key null-out happens "inside the same transaction as
the insert." The implementation frees it in **its own `SaveChangesAsync`** (OrderService.cs:109),
then inserts in a separate save, with no wrapping transaction — deliberately, to avoid a
per-statement unique-index collision. Practically benign (the spec already accepts losing the
stale row's key for audit), but the free+insert is non-atomic and the design doc is now
inaccurate. **Fix hint:** update ddd-02 to describe the two-save approach (or wrap both saves in
an explicit transaction).

### ⚪ Cleanup

- **DOC-2** — ddd-02 documents the `CreatePaymentIntentAsync` signature change but never states
  the gateway is keyed by `order.Id` (not the client key). The implementation choice is correct
  and well-tested; document it in the spec/OpenAPI to prevent future "why isn't it the header?"
  confusion.
- **QUAL-1** — the pre-INSERT resolution block and the post-collision recovery block are
  near-duplicate (find holder → window check → `DivergentFields` → replay/409). Extract a shared
  `ResolveFreshHolder(holder, request, total, items)` helper.
- **QUAL-2** — `"Npgsql.EntityFrameworkCore.PostgreSQL"` / `"Microsoft.EntityFrameworkCore.Sqlite"`
  are hard-coded across `PhotoPrintDbContext`, `OrderNumberService`, and the migration. Extract a
  `DbProviders` constants class to prevent drift.
- **QUAL-3** — the controller calls `_db.SaveChangesAsync` directly to persist the gateway field;
  persistence sits slightly below the `OrderService` altitude. Pre-existing pattern (the original
  code did the same), so not introduced here — noted for a future tidy.
- **QUAL-4** — `SendStripeIntent`/`SendEuPlatescInitiate`, the `MakeRequest` variants, and the
  SQLite factory setup are duplicated across the new test files. Consolidate into a shared test base.
- **QUAL-5** — `OrderNumberService.cs:33` EF1002 (interpolated SQL): the interpolated value is a
  server-side `int` (`DateTime.UtcNow.Year`), so it is **not** an injection vector — but the
  Postgres-only branch has **zero automated coverage** (no Postgres in the test matrix). Suppress
  EF1002 with a justifying comment or parameterize, so the warning stays meaningful.

---

## Probed and refuted (do not re-raise)

- **`HttpContext.Items[key]` throws KeyNotFoundException** (raised by a correctness finder) —
  **false positive.** ASP.NET Core's `HttpContext.Items` indexer returns `null` for a missing
  key; the pre-existing `context.GetCorrelationId() ?? Guid.NewGuid()` fallback in
  `ExceptionHandlerMiddleware` relies on exactly this. The filter also always runs for these
  controller actions, so the key is present regardless.
- **Cross-tenant IDOR via a borrowed key (any path)** — refuted. `FindKeyHolderAsync` is
  owner-scoped on every read (initial resolution, stale-free, post-collision re-resolve). A
  second tenant presenting tenant A's key receives a new order or a clean 409, never A's
  order/secret. Verified by `SecondTenantReusesAnothersKey_DoesNotReceiveTheirOrder` (InMemory)
  and `...Returns409_OnRealUniqueIndex` (real SQLite unique index).
- **409 leaking field VALUES / another tenant's data** — refuted. `DivergentFields` appends only
  literal field-name strings, surfaced only for the caller's own prior order; the cross-caller
  path throws a fixed-message `ConflictException`.
- **Secret logged or returned to the wrong caller** — refuted. The only logs are
  `payments.idempotency.replay` (processor + order_id) and the missing-key warning (endpoint +
  correlation_id); secrets are returned only from the owner-scoped resolved order.
- **Stripe keyed by the client header (recycled-key collision at Stripe)** — refuted. The
  controller keys Stripe by `o.Id.ToString()` (server-generated), asserted by
  `...ReplaysOneOrderAndOneStripeCall`.
- **Multiple-NULL / duplicate-key index semantics differ across providers** — refuted. Postgres
  (filtered unique) and SQLite (plain unique, NULLs distinct) both allow many key-less orders and
  both reject duplicate non-null keys.
- **24h-window boundary inverted / off-by-one** — refuted. `CreatedAt > UtcNow - 24h` is exactly
  the spec's `CreatedAt + 24h > UtcNow` (ddd-01:47).
- **`ItemsSignature` collisions** — refuted. Order-independent `{ProductId:N}:{UploadId:N}:{Qty}`
  joined on `|`; Guid `:N` is bijective, so distinct carts can't collide and identical carts can't
  diverge.
- **Concurrent same-owner same-key race double-creates / 500s** — refuted. The loser catches the
  unique violation, re-resolves the winner owner-scoped, and replays; one row persists. Verified by
  `ConcurrentSameOwnerSameKey_LoserReplaysWinner_OneOrder` (real SQLite + a SaveChanges interceptor).
- **`DivergentFields` missing the cart-items comparison (silent wrong-photo replay)** — refuted.
  Items participate via `ItemsSignature`; `SameTotalDifferentItems_ThrowsConflictNamingItems`
  proves equal-total-different-photo 409s.
- **EuPlatesc URL or key-namespace griefing** — refuted / accepted design. `EuPlatescRedirectUrl`
  fits `varchar(1000)` with wide margin; global-unique keys are an inherent property of any
  globally-scoped idempotency key, mitigated by the tracked require-key escalation (the
  `bolt-035-followup` TODO).
- **EuPlatesc-at-Stripe-endpoint cross-processor replay** — refuted as a security issue. A
  mismatched body normally 409s on `paymentProcessor`; the benign edge only ever builds a value for
  the caller's own order. No cross-tenant disclosure.

## Verification

- **Build:** `dotnet build PhotoPrint.sln -c Debug` → **0 errors**, 6 warnings
  (NU1603 Stripe.net 46.3.0→47.0.0 resolve ×4; CS1998 in an unrelated Razor test; EF1002 at
  `OrderNumberService.cs:33` — see QUAL-5).
- **Tests:** `dotnet test --no-build` → **466 passed, 0 failed, 0 skipped** (~15 s).
- **Coverage note (positive):** the new `SqlitePaymentFactory` /
  `OrderServiceIdempotencyConcurrencyTests` / `OrderNumberServiceSqliteTests` run against a real
  SQLite database, so the unique-index collision → 409 and the concurrent-race paths are genuinely
  exercised — the default InMemory provider cannot enforce unique indexes and would give a false
  green here. This is the right call and closes the parity gap for those paths.
- **Coverage gaps found:** the 409 `divergentFields` body (OBS-1) and the Postgres `OrderNumber`
  sequence branch (QUAL-5) have no assertions/coverage.
