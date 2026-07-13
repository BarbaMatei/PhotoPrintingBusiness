---
type: code-review
target: bolt-035-payment-idempotency
version: 2
supersedes: 1
branch: feat/bolt-035-payment-idempotency
commit: b52f4b6
base: 691e23d
reviewed: 2026-06-18
reviewer: Claude (multi-lens re-review — independent, clean context)
lenses: [correctness, security, pr-requirements, quality-altitude, tests-verification]
verdict: approved-with-followups
blockers: []
---

# Review v2 — Bolt 035: Payment Idempotency (re-review of the fixes)

Independent adversarial re-review of the resolution to [review-v1.md](review-v1.md),
against `b52f4b6` (fixes in `2093302` + `b52f4b6`), base `691e23d`. I did not write the
fixes; every verdict below is from reading the code at `b52f4b6` and exercising the tests,
not from the fixer's notes.

## TL;DR

**Both blockers are genuinely fixed, with regression tests that I confirmed actually
reproduce the failure mode.** The build is clean and the full suite is green. The
deferrals/won't-fixes all have sound rationale. I found no new blocking bugs; two
low/informational items are recorded below.

- **Verdict: approved-with-followups.** No remaining blockers.
- 13 findings **verified**, 4 **accept-deferral**, 2 **accept-wontfix**, 0 reopened.
- 2 NEW informational findings (INFO-1, INFO-2) — neither blocks merge.

The decisive check: I temporarily removed the `catch (DbUpdateException)` recovery block
in `OrderService.CreateFromCartAsync` and re-ran the BUG-1 tests — **both failed with the
exact SQLite unique-constraint `DbUpdateException`** the fix exists to handle (stack ends
at `OrderService.cs:157` → `ix_orders_idempotency_key`). The SQLite concurrency tests are
real, not vacuous. Restored afterward; working tree clean.

---

## Build & test

- `dotnet build` → **0 errors** (4 NU1603 Stripe.net version-resolution warnings, pre-existing).
- `dotnet test PhotoPrint.Tests` → **463 passed / 0 failed / 0 skipped** (~23s).
  - Note: the resolution file's verification note predicted "462/463 with one
    `ReliableEmailServiceTests` flake." On this run the full suite was **463/463 green** —
    the flake did not recur. No action needed.
- BUG-1 SQLite tests in isolation → 2/2 pass; against catch-removed code → **2/2 fail**
  with the unique-index violation (proves they exercise the race).

---

## Per-finding verdicts

| ID | Sev | v1 status | v2 verdict | Evidence |
|----|-----|-----------|-----------|----------|
| BUG-1 | 🔴 High | fixed | **verified** | `OrderService.cs:156-191` — `try/catch (DbUpdateException)`: detach failed insert → re-resolve owner-scoped winner → replay (or 409 on divergence); cross-tenant collision → clean `ConflictException` (409); unrelated `DbUpdateException` → **rethrown** (still 500, correctly not masked). Tests: `OrderServiceIdempotencyConcurrencyTests` (real SQLite, shared-connection + one-shot `SaveChangesInterceptor` to inject the winner). Confirmed both tests fail against old-behavior code. |
| SEC-1 | 🟠 Med | fixed | **verified** | `FindKeyHolderAsync` (`:229-235`) and `GetByIdempotencyKeyAsync` (`:211-220`) now require `o.UserId == userId` / `o.GuestSessionId == guestSessionId`. Stale-free (`:108`) acts only on the caller's own row. Both `CreateFromCartAsync` callers pass principal-derived ids; no other prod caller. Tests: `GetByIdempotencyKey_KeyOwnedByAnotherUser_ReturnsNull`, `CreateFromCart_OtherTenantsKey_DoesNotReplayOrLeakOrder` (asserts `StripeClientSecret` not leaked), integration `CreateStripeIntent_SecondTenantReusesAnothersKey_DoesNotReceiveTheirOrder`. |
| BUG-3 | 🟠 Med | fixed | **verified** | `DivergentFields` (`:244-255`) now compares `ItemsSignature` (`:263-267`): ordered `product:upload:qty`, order-independent (`OrderBy(ProductId).ThenBy(UploadId)`). Test `CreateFromCart_SameKey_SameTotalDifferentItems_ThrowsConflictNamingItems` asserts `items` in divergent set and `totalRon` absent. No false-pos/neg found; ordering stable. |
| OPS-1 | 🟠 Med | fixed | **verified** | `PaymentsController.cs:120-123` — `TODO(bolt-035-followup)` on `WarnIfMissingIdempotencyKey` referencing ddd-02 + walkthrough; tracks the missing-key→400 breaking change. |
| QUAL-1 | 🟠 Med | fixed | **verified** | Two-query fresh-then-stale folded into one owner-scoped `FindKeyHolderAsync` round-trip; caller branches on the 24h window in memory (`:92-110`). |
| QUAL-3 | 🟠 Med | deferred | **accept-deferral** | Header-extraction filter is a cross-cutting refactor touching both endpoints; matches review §D "fast-follow." Sound. |
| QUAL-4 | 🟠 Med | deferred | **accept-deferral** | `IdempotentComputation<T>` extraction — sensible to defer until BUG-1's per-path replay nuance settles, per review §D. Sound. |
| BUG-4 | 🟡 Low | fixed | **verified** | `PaymentsController.cs:74-75` keys Stripe by `order.Id.ToString()` (stable, unique per order) for both metadata + idempotency key. Lost-response retry still dedupes (same logical order → same `order.Id` → same Stripe key); distinct orders can no longer collide on a recycled client key. Integration assertion updated to `dto1.OrderId.ToString()`. |
| BUG-5 | 🟡 Low | deferred | **accept-deferral** | Migration still `type:"TEXT"` unfiltered; runtime `HasFilter` Npgsql-only (`PhotoPrintDbContext.cs:304-308`). Pre-existing single-migration-set dual-DB parity issue, functionally correct today. Larger than this bolt; index-backlog item. Sound. |
| DOC-1 | 🟡 Low | fixed | **verified** | `OrderService.cs:107` comment now says "both SQLite and Postgres enforce it per-statement." |
| DOC-2 | 🟡 Low | fixed | **verified** | `PhotoPrintDbContext.cs:300-303` reworded to "multiple NULLs are permitted." |
| DOC-3 | 🟡 Low | deferred | **accept-deferral** | ddd-02 sketch vs impl — deviation already in the walkthrough; low-value historical-doc churn. Sound. |
| QUAL-2 | ⚪ Clean | wont-fix | **accept-wontfix** | `IdempotencyConflictException` carries the `DivergentFields` payload; BUG-1 now also throws plain `ConflictException` for cross-tenant collisions, so both types coexist meaningfully. Reasonable. |
| QUAL-5 | ⚪ Clean | fixed | **verified** | `HttpContextExtensions.GetCorrelationId()` + `CorrelationIdItemKey` const; raw `Items["CorrelationId"]` reads replaced in controller (`:130`) and `ExceptionHandlerMiddleware.cs:56`, setter in `CorrelationIdMiddleware.cs:21`. |
| QUAL-6 | ⚪ Clean | wont-fix | **accept-wontfix** | Two-phase save is intentional crash-recovery (order before gateway). The BUG-1 catch preserves order-first; collapsing the saves would reintroduce the mid-gateway unrecoverable window. Sound. |

**13 verified · 4 accept-deferral · 2 accept-wontfix · 0 reopened.**

---

## Scrutiny of the fixer's flagged design decisions

- **Kept the global unique index + 409 on cross-tenant collision (not `(owner, key)`).**
  Accepted. Re-resolution is owner-scoped, so a cross-tenant collision can never *replay*
  another tenant's order — it returns a generic 409 with no order id/secret in the body or
  logs. Per-owner uniqueness would need two filtered partial indexes on two nullable
  columns (UserId/GuestSessionId) — the exact dual-DB partial-index complexity BUG-5 is
  about — for a collision that is astronomically unlikely with GUID keys. The tradeoff is
  sound and documented. (See INFO-2 for the one residual edge.)
- **Changed stale-key semantics (free only the caller's own stale key).** Correct and
  strictly more secure: the prior behavior could null another tenant's row. The one unit
  test that seeded an ownerless stale order was updated to the owner-scoped scenario
  (`OrderServiceTests.cs:433` adds `UserId = userId`) — tightened to the secure behavior,
  not weakened.
- **SQLite (not EF InMemory) for the concurrency tests.** Correct call — InMemory does not
  enforce unique indexes and cannot reproduce the race. Verified the SQLite harness genuinely
  triggers `ix_orders_idempotency_key`.

---

## New findings (introduced by / surviving the fixes)

### ⚪ INFO-1 — Cross-tenant integration test passes on InMemory, which doesn't enforce the unique index
`PaymentControllerIntegrationTests.cs:79-114` (`...SecondTenantReusesAnothersKey...`),
`UploadFactory.cs:66-67` (`UseInMemoryDatabase`).
The WebApplicationFactory uses EF InMemory, which does **not** enforce
`ix_orders_idempotency_key`. So when tenant B reuses tenant A's key, B's INSERT *succeeds*
(creating a second row carrying the same key) instead of colliding. The test therefore
genuinely verifies **SEC-1 non-disclosure** (B receives only B's own order, not A's secret)
— which is its stated purpose and is correct — but it does **not** exercise the real-DB
cross-tenant **409 collision** path. That path *is* covered by the SQLite unit test
`CreateFront_CrossTenantKeyCollisionOnInsert_Returns409_NotServerError`, so overall coverage
is adequate. Informational: on Postgres this exact scenario would return 409, not 200; the
divergence is a known InMemory/relational parity gap (cf. MEMORY: dual-DB parity), not a code
defect. No action required for merge.

### ⚪ INFO-2 — A stale cross-tenant key collision returns 409 to the second tenant
`OrderService.cs:184-188`.
If tenant A holds a **stale** (>24h) key globally and tenant B presents the same key value,
B's owner-scoped resolution sees nothing, B inserts, collides with A's stale row on the
global unique index, and the catch returns a 409 (`AnyAsync` finds A's row regardless of
window/owner). Net effect: an expired key value owned by A blocks B from ever using that
exact string. Astronomically unlikely with GUID keys, safe and non-disclosing (generic 409).
This is an accepted consequence of the global-index decision, recorded for completeness. If
per-tenant key namespaces are ever wanted, this resolves itself with `(owner, key)`
uniqueness (tracked alongside BUG-5).

---

## DbContext-state / masking checks (adversarial probes — all clear)

- **Catch masking unrelated DB errors:** No. Only same-owner-winner → replay and
  *confirmed* other-caller key-holder → 409; everything else `throw;` (preserves the
  original `DbUpdateException` → 500 for genuine server errors).
- **Context usability after failed SaveChanges:** Confirmed — the SQLite cross-tenant test
  passes, proving the post-failure `FindKeyHolderAsync` + `AnyAsync` reads execute fine.
- **`DetachFailedInsert` completeness:** Detaches each `OrderItem` then the `Order`
  (snapshotting the collection first to avoid EF fix-up mutating it mid-enumeration). The
  owned `ShippingAddress` rides with the order; Include-loaded cart/product/upload rows are
  `Unchanged` and won't persist. So the controller's later `SaveChangesAsync` (on the
  *winner*) can't resurrect the orphaned graph. The `candidateItems = order.Items.ToList()`
  snapshot before detaching correctly preserves items for the divergence check.
- **BUG-4 vs lost-response dedup:** Preserved — keying Stripe by `order.Id` keeps retries
  for the same logical order on one Stripe key while distinct orders never share a recycled
  key.
- **Whitespace-only key (T7):** Treated as no-key throughout (`IsNullOrWhiteSpace` gate +
  null-normalized persist) — consistent.

---

## Recommendation

**Approve with follow-ups.** Both blockers are correctly fixed and backed by regression
tests that demonstrably catch the original failure modes; the security scoping is real and
consistently applied; no new blocking defects. The deferrals and won't-fixes are
well-reasoned and map to the review's own §D/§F fast-follow disposition.

Follow-ups (non-blocking), for the backlog:
1. QUAL-3 / QUAL-4 — centralize idempotency header extraction + replay computation.
2. BUG-5 — migration/Postgres partial-index + `decimal` typing parity pass (also resolves
   INFO-2 if `(owner, key)` is chosen).
3. INFO-1 — optionally add a relational-DB integration test (or note the InMemory gap) so
   the cross-tenant 409 is exercised at the HTTP layer, not only in the service unit test.
