---
type: code-review
target: bolt-035-payment-idempotency
version: 1
supersedes: null
branch: feat/bolt-035-payment-idempotency
commit: 691e23de9d60c9728e8111618fc06854659a829c
base: main
reviewed: 2026-06-18
reviewer: Claude (multi-lens parallel review system)
lenses: [correctness, security, pr-requirements, quality-altitude, tests-verification]
verdict: request-changes
blockers: [BUG-1, SEC-1]
---

# Review — Bolt 035: Payment Idempotency

Makes Stripe + EuPlatesc payment-intent creation idempotent so a double-clicked
"Pay" yields one order and one charge. 24 files, +1738/-37. Run via the
[multi-lens review system](../README.md): 5 isolated-subagent lenses + build/test
verification, synthesized here.

## TL;DR

The implementation is coherent and the happy paths are well-built and tested — all
three stories (001–003) and both ADRs (004, 005) are genuinely delivered, the build
is clean, and the 8 new tests pass. **But the passing tests only cover single-tenant,
single-threaded happy paths.** Two real defects sit in exactly the scenarios
idempotency exists for (concurrent retry; multi-tenant), and neither is tested.

- 🔴 **1 High** — BUG-1 (concurrent same-key → 500)
- 🟠 **6 Medium** — SEC-1 (tenant isolation), BUG-3 (item-divergence), OPS-1 (header-required tracking), QUAL-1/3/4 (quality/altitude)
- 🟡 **5 Low** — BUG-4, BUG-5, DOC-1/2/3
- ⚪ **3 Cleanup** — QUAL-2/5/6

**15 findings total.**

**Disposition: request changes** on BUG-1 and SEC-1; the rest can be follow-ups.

Cross-lens convergence (unbiased signal): SEC-1 was independently raised by the
security, test-coverage, **and** correctness lenses; BUG-1 by correctness and
test-coverage.

---

## A. Correctness & concurrency

### 🔴 BUG-1 — Concurrent same-key requests return 500 instead of replay/409
`src/PhotoPrint.API/Services/OrderService.cs:392-429`, `Middleware/ExceptionHandlerMiddleware.cs`
**Confirmed.** The canonical idempotency scenario — double-submit or in-flight retry —
races: both requests pass `GetByIdempotencyKeyAsync` (find nothing), both `INSERT`.
The unique index correctly stops the duplicate *order*, but the losing insert throws
`DbUpdateException` (unique violation), which is **not** in the middleware's exception
map → unhandled **500**. The non-atomic two-step "free stale key, then insert" (two
separate `SaveChangesAsync`) widens the window. The feature whose whole purpose is to
make retries safe fails on the canonical retry.
**Fix:** catch the unique-violation `DbUpdateException`, re-resolve the key, and return
the now-existing order as a replay (or map to 409). Consider doing the resolve+insert in
one transaction.

### 🟡 BUG-4 — Stale-key reuse forwards the client key to Stripe with a possibly-different amount
`src/PhotoPrint.API/Services/OrderService.cs:408-414`, `Services/StripePaymentGateway.cs:31`
**Plausible (boundary).** Local window (24h) ≈ Stripe's key window (~24h). At the
boundary a freed-and-reused key with a new amount can reach Stripe while Stripe still
holds the key → Stripe rejects mismatched params (→ 500) or returns the stale intent →
order/intent amount mismatch.
**Fix:** derive Stripe's idempotency key per-order (e.g. an order GUID) rather than
reusing the client key across distinct orders.

### 🟡 BUG-5 — Postgres partial-index model drift
`src/PhotoPrint.API/Migrations/20260527075359_AddOrderIdempotencyKey.cs:34`
**Low / parity.** The migration creates the unique index with no filter; `DbContext`
adds `HasFilter("IdempotencyKey IS NOT NULL")` only for Npgsql at runtime. Functionally
fine (Postgres treats NULLs as distinct in unique indexes anyway), but EF reports model
drift if migrations are ever scaffolded under the Npgsql provider, and `type:"TEXT"`
ignores `maxLength` on Postgres. Confirm the prod index matches intent — relevant given
the SQLite/Postgres parity gap.

---

## B. Security

### 🟠 SEC-1 — Idempotency lookup not scoped to the caller (broken tenant isolation / IDOR via header)
`src/PhotoPrint.API/Services/OrderService.cs:437` (`GetByIdempotencyKeyAsync`), `:449` (`DivergentFields`), `Controllers/PaymentsController.cs:62-67`
**Confirmed — security confidence 7/10** (below `/security-review`'s ≥8 auto-report bar,
but real; recorded per system policy). `GetByIdempotencyKeyAsync` matches on
`IdempotencyKey + CreatedAt` only — no `userId`/`guestSessionId` — and `DivergentFields`
never compares the order's owner against the caller. A caller presenting another tenant's
key (with matching processor/delivery/locker/**total**) is handed that order **and its
live `StripeClientSecret`** (which can confirm the victim's PaymentIntent). This is the
lone new query that ignores the repo's otherwise-consistent ownership pattern
(`GetOrderDetailAsync` throws `ForbiddenException` on owner mismatch; `CartService`,
`UploadService` scope by owner).
Knocked below 8 only because exploitation also requires matching the server-recomputed
`totalRon` and knowing the (arbitrary, non-UUID-guaranteed, max-80-char) key — keys are
not auth and shouldn't be treated as such.
**Fix:** scope the lookup to the caller and apply the same predicate to the stale-key free:
```csharp
.FirstOrDefaultAsync(o =>
    o.IdempotencyKey == key && o.CreatedAt > cutoff &&
    (userId.HasValue ? o.UserId == userId : o.GuestSessionId == guestSessionId), ct);
```
Consider scoping uniqueness to `(owner, IdempotencyKey)` if per-tenant key namespaces are
intended (the current unique index is global).

**Cleared by the security lens (not vulnerabilities):**
- `HasFilter("\"IdempotencyKey\" IS NOT NULL")` — a compile-time constant, not injectable; key LINQ queries are parameterized expression trees.
- 409 `divergentFields` body and all log lines emit field **names** / `order.Id` / processor only — no request values, secrets, or PII.
- Stale-key **write** vector (nulling another tenant's key) — **false positive (3/10)**: that branch only runs when no order holds the key *within* the window, so it can only null an already-expired (functionally dead) key. No security impact.
- The `Idempotency-Key` header does not influence authn/authz; `[Authorize(DualAuthPolicy)]` still applies. The issue is authorization *scoping*, not an auth bypass.

---

## C. Data integrity / semantics

### 🟠 BUG-3 — `DivergentFields` ignores cart contents
`src/PhotoPrint.API/Services/OrderService.cs:449`
**Confirmed.** Divergence checks 4 scalar fields (processor, deliveryType, easyboxLockerId,
totalRon) but **not the items**. With uniform per-unit photo pricing, 5 prints of photo X
and 5 prints of photo Y have identical totals — a reused key silently replays the *wrong
order's images*. Realistic for this domain.
**Fix:** include a stable hash of the cart/items (product + upload + qty) in the divergence
comparison. `ShippingAddress` is intentionally excluded per ADR-005; items are not addressed
by any ADR.

---

## D. Quality / altitude (reuse · simplification · efficiency · right-layer)

> Quality only — no behavioral impact. QUAL-3/4/5 share one theme: idempotency
> handling is copy-pasted per endpoint rather than centralized.

### 🟠 QUAL-1 — Redundant second DB round-trip for the stale row
`src/PhotoPrint.API/Services/OrderService.cs:408-414`
`GetByIdempotencyKeyAsync` already queries by key (with the time filter); on a miss the
code issues a *second* `FirstOrDefaultAsync` by key without the filter to find the stale
row. Fold both into one query (return fresh-match + stale-row) and branch in memory.

### 🟠 QUAL-3 — Header extraction + missing-key warning duplicated across endpoints
`src/PhotoPrint.API/Controllers/PaymentsController.cs:49, 88, 105-114`
Both endpoints copy-paste header read + `WarnIfMissingIdempotencyKey`; a third processor
repeats it. **Deeper fix:** an `[IdempotencyKey]` action filter / small middleware that owns
extraction, the missing-key warning, and the correlation-id read.

### 🟠 QUAL-4 — Replay/compute/persist logic duplicated between Stripe and EuPlatesc branches
`src/PhotoPrint.API/Controllers/PaymentsController.cs:51-67` vs `82-102`
Identical structure (resolve → if replay+cached return → else compute → persist → save);
only the gateway call and cached field differ. Extract a generic
`IdempotentComputation<T>(key, cached, compute, persist)` so replay semantics live in one place.

### ⚪ QUAL-2 — `IdempotencyConflictException` overlaps `ConflictException`
`src/PhotoPrint.API/Exceptions/IdempotencyConflictException.cs`
Both map to 409; the new type exists only to carry `DivergentFields`. Justified, but a
reusable `ConflictException` with an optional `Extensions`/payload would avoid a third
variant later. Low priority.

### ⚪ QUAL-5 — `HttpContext.Items["CorrelationId"]` accessed by raw string key
`src/PhotoPrint.API/Controllers/PaymentsController.cs:109` (also in `ExceptionHandlerMiddleware`)
Untyped string-keyed coupling to middleware, duplicated in 2 places. Add a
`HttpContext.GetCorrelationId()` extension.

### ⚪ QUAL-6 — Two `SaveChangesAsync` round-trips per create
`src/PhotoPrint.API/Controllers/PaymentsController.cs:65, 100`
Order is saved in the service, then again in the controller after the gateway call. Minor;
could batch. (Note: separating them is partly *intentional* — the order must exist before
the gateway call so a crash mid-gateway is recoverable. Weigh against BUG-1's atomicity fix.)

---

## E. Tests & verification

**Verification run (2026-06-18):** `dotnet build` → **0 errors** (3 pre-existing warnings).
Targeted `dotnet test` on the 8 new idempotency tests → **8 passed / 0 failed (2s)**.
Consistent with the commit's "457/457 green" claim for the happy paths.

**The green suite masks the failure modes below** — this is itself a finding.

| # | Untested behavior | Risk | Maps to |
|---|-------------------|------|---------|
| T1 | Two concurrent requests, same key (double-submit race) | Critical — the canonical case; unrepro on SQLite, fails on Postgres | BUG-1 |
| T2 | Second tenant presents another tenant's key | Critical — cross-tenant order + secret leak | SEC-1 |
| T3 | Same total, different items, same key | High — wrong images shipped | BUG-3 |
| T4 | Divergence on `deliveryType` / `easyboxLockerId` / `totalRon` (only `paymentProcessor` tested) | Medium — divergence bug ships undetected | — |
| T5 | EuPlatesc 409-divergent path (only Stripe's 409 tested) | Medium | — |
| T6 | Replay when cached secret/URL is null (recovery fall-through) | Behavior is intentional recovery, but **untested** | see note |
| T7 | Whitespace-only key (treated as "no key") | Low — silent degradation, untested | — |
| T8 | Postgres unique-index-with-filter / true concurrency (tests are SQLite-only) | Critical (prod-only) | BUG-1, BUG-5 |

**Recommended additions:** a Postgres-backed concurrency integration test (T1/T8), a
cross-tenant test (T2), a same-total-different-items test (T3), and the missing divergence
+ EuPlatesc-409 cases (T4/T5).

---

## F. Docs / observability / operability (PR-requirements lens)

**Scope: complete.** Stories 001/002/003 and ADR-004 (409 ≠ 422) / ADR-005 (LogicalRequest
excludes ShippingAddress) are all genuinely implemented and match the code.

### 🟠 OPS-1 — No tracking for the planned "make header required" breaking change
Both ddd-02 and the walkthrough say the missing-key warning is *transitional* ("until the FE
always sends it, then escalate to 400"). There's no TODO, ticket, or follow-up bolt tracking
this future breaking change. Add a TODO in `WarnIfMissingIdempotencyKey` or a follow-up item.

### 🟡 DOC-1 — Stale-key comment attributes per-statement constraint check to Postgres only
`src/PhotoPrint.API/Services/OrderService.cs:405-410`
"(Postgres checks per-statement)" — SQLite enforces the constraint too; a maintainer might
think the null-out is Postgres-only. Reword to cover both providers.

### 🟡 DOC-2 — `DbContext` NULL-behavior comment phrasing
`src/PhotoPrint.API/Data/PhotoPrintDbContext.cs:146-150`
The PR lens flagged "NULLs are distinct in both Postgres and SQLite" as confusing.
*Contested:* "NULLs are distinct" is standard SQL phrasing for "multiple NULLs allowed."
Optional clarity reword ("multiple NULLs are permitted"); not a defect.

### 🟡 DOC-3 — ddd-02 design sketch vs implementation
The design sketched conflict-resolution in the controller; the implementation (better) puts
it in `OrderService` and throws `IdempotencyConflictException`. Already noted as a deliberate
deviation in the walkthrough; reconcile the ddd-02 sketch for future readers.

---

## G. Cleared / dropped false-positives

Recorded so they aren't re-raised:
- **`IsNullOrWhiteSpace(key) ? null : key` ternary** — *not* redundant: it normalizes
  whitespace-only keys to NULL so they don't collide on the unique index. Correct.
- **Replay-but-null-secret falls through to Stripe** (T6) — *intentional, correct
  crash-recovery*: the order exists but the secret was never persisted; re-calling Stripe with
  the forwarded key dedupes at the gateway, so no double charge. Don't "fix" it — but **do**
  add a test (T6).
- **Stale-key cross-tenant write** — false positive (only nulls already-expired keys).
- **All call sites of the changed signatures** (`CreateFromCartAsync` → `OrderCreationResult`,
  `CreatePaymentIntentAsync` + key) were updated: controllers, fakes, seeds, webhooks, 12
  unit-test calls. Cross-file tracing clean.

---

## H. Recommendation

**Request changes**, blocking on:
1. **BUG-1** — handle the concurrent unique-violation (catch → replay/409). Add T1/T8.
2. **SEC-1** — scope the idempotency lookup (and stale-key free) to the caller. Add T2.

Strongly recommended before/with merge: **BUG-3** + T3 (wrong-images risk is domain-specific
and cheap to close). Everything in §D and §F can be fast-follow PRs.
