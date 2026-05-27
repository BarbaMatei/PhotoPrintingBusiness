---
unit: 002-payment-idempotency
bolt: 035-payment-idempotency
stage: test
status: complete
updated: 2026-05-25T14:10:00Z
---

# Test Report - Payment Idempotency

## Test Summary

| Category | Passed | Failed | Skipped | Coverage |
|----------|--------|--------|---------|----------|
| Unit (this bolt) | 5 | 0 | 0 | — |
| Integration (this bolt) | 3 | 0 | 0 | — |
| **Whole suite** | **457** | **0** | **0** | not measured |

Full `dotnet test src/PhotoPrint.Tests` run: **457 / 457 passed**, 7 s. (449 pre-existing + 8 new from this bolt.)

## Acceptance Criteria Validation

| Story | Criteria | Status |
|-------|----------|--------|
| 001 | Migration adds `IdempotencyKey` nullable + unique index | ✅ `20260527075359_AddOrderIdempotencyKey` |
| 001 | Down-migration drops index + column | ✅ generated `Down` present |
| 002 | Same key + identical body → same OrderId + ClientSecret, one row, one PaymentIntent | ✅ `CreateStripeIntent_SameIdempotencyKey_ReplaysOneOrderAndOneStripeCall` (asserts same OrderId, same secret, `CreateCallCount` delta = 1, one row) |
| 002 | Same key + divergent body → 409 naming divergent fields | ✅ `CreateStripeIntent_SameKey_DivergentProcessor_Returns409` + unit `…DivergentProcessor_ThrowsConflictNamingField` (asserts `divergentFields` contains `paymentProcessor`, excludes `easyboxLockerId`) |
| 002 | Missing key behaves as today + Warning | ✅ existing `CreateStripeIntent_ValidCart_*` tests pass with no key (warning path is logged; not asserted on log text) |
| 002 | Stripe `RequestOptions.IdempotencyKey` set | ✅ `FakeStripePaymentGateway.LastIdempotencyKey` asserted equal to the sent key |
| 003 | Same key on EuPlatesc → same redirect URL + OrderId | ✅ `InitiateEuPlatesc_SameIdempotencyKey_ReturnsSameUrlAndOneOrder` (byte-identical URL, one row) |
| 003 | First-call-fails-before-persist → retry allowed | ✅ covered by design (no row written → resolver returns NewOrder); unit `…NoKey_DoesNotReplay` + stale-key test exercise the create path |

## Unit Tests

`OrderServiceTests` (+5):

- `CreateFromCart_SameKey_SameRequest_ReplaysOriginalOrder` — second call returns `WasIdempotentReplay = true`, same `Order.Id`, exactly one row.
- `CreateFromCart_SameKey_DivergentProcessor_ThrowsConflictNamingField` — throws `IdempotencyConflictException`; `DivergentFields` contains `paymentProcessor`, not `easyboxLockerId`; no second row.
- `CreateFromCart_NoKey_DoesNotReplay_CreatesDistinctOrders` — two key-less calls create two orders.
- `GetByIdempotencyKey_StaleOrder_ReturnsNull` — a key on an order created 25 h ago is invisible to lookup.
- `CreateFromCart_StaleKey_CreatesNewOrderAndFreesOldKey` — reusing a stale key creates a new order and nulls the old row's key.

## Integration Tests

`PaymentControllerIntegrationTests` (+3) via `WebApplicationFactory` + shared `FakeStripePaymentGateway`:

- Stripe replay: two POSTs, same `Idempotency-Key` → same OrderId + secret, one Stripe create, one order row.
- Stripe conflict: same key, `PaymentProcessor` changed → `409 Conflict`.
- EuPlatesc replay: two POSTs, same key → identical redirect URL, one order row.

## Security Tests

No new security-specific tests; this bolt *is* a security hardening (duplicate-charge prevention). Conflict path returns 409 (ADR-004), divergent field **names only** — no values/PII in the response (verified by the exception payload shape).

## Performance Tests

| Metric | Target | Actual | Status |
|--------|--------|--------|--------|
| Extra DB lookup per keyed request | < 5 ms (single indexed read) | Single `FirstOrDefaultAsync` on indexed column | ✅ by design |

Not load-tested (out of scope; intent 021 covers multi-instance + Redis).

## Coverage Report

Not measured (project has no coverage gate configured). All new domain-service branches (replay / conflict / new / stale-reuse / missing-key) are exercised by the 5 unit tests; both controller endpoints' replay + create paths by the 3 integration tests.

## Issues Found

| Issue | Severity | Status |
|-------|----------|--------|
| Test authored two "identical" requests via `MakeRequest()`, which randomizes `EasyboxLockerId` per call → false conflict | Low (test-only) | Fixed — reuse one request instance / use `with` for targeted divergence |

## Ready for Operations

- [x] All acceptance criteria met
- [ ] Code coverage > 80% — not measured (no project gate)
- [x] No critical/high severity issues open
- [x] Performance target (single indexed lookup) met by design
- [x] Security: conflict path leaks no values; gateway-side dedupe engaged

## Notes / follow-ups

- Multi-instance race (two nodes, same key, simultaneous) is arbitrated by the DB unique index. The EF **InMemory** provider used in tests does NOT enforce unique indexes, so the DB-arbitration path is covered by design + the Postgres migration, not by an automated test. A true concurrency test belongs with intent 021 (Redis / multi-instance) where a real Postgres test container is in play.
- `api-conventions.md` should gain a "409 vs 422" subsection (per ADR-004) — recommended doc follow-up, not code.
