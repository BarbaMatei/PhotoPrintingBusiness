---
unit: 001-coupon-domain-and-api
intent: 022-coupon-promo-codes
created: 2026-09-03T20:42:00Z
last_updated: 2026-09-03T20:42:00Z
---

# Construction Log: coupon-domain-and-api

## Original Plan

**From Inception**: 1 bolt planned
**Planned Date**: 2026-05-25

| Bolt ID | Stories | Type |
|---------|---------|------|
| 047-coupon-domain-and-api | 001-coupon-schema, 002-cart-coupon-endpoints, 003-redemption-on-order-create, 004-admin-coupon-crud | ddd-construction-bolt |

## Execution Log

- **2026-09-03T20:42:00Z**: 047-coupon-domain-and-api started - Stage 1: Domain Model
- **2026-09-03T20:50:00Z**: 047-coupon-domain-and-api stage-complete - domain-model -> technical-design
- **2026-09-04T00:30:00Z**: 047-coupon-domain-and-api stage-complete - technical-design (adversarial design check run: 6 blockers folded in) -> adr-analysis
- **2026-09-04T00:55:00Z**: 047-coupon-domain-and-api stage-complete - adr-analysis (ADR-025, ADR-026) -> implement
- **2026-09-04T02:15:00Z**: 047-coupon-domain-and-api SOFT-STOPPED mid stage-5 (test). Implementation + tests complete and green (868 unit, 267 integration, concurrency gate mutation-checked); ddd-03 written; stage-4 fresh-eyes micro-review ran and reported 11 findings (1 blocker F1 PDF discount line, 4 serious F2/F6/F8/F9) which are RECORDED BUT NOT FIXED. Deviation: bolt-complete.cjs deliberately not run (coordinator standing instruction); status stays in-progress, NOT review-pending. Next: fix or route F8/F9, add the two missing test classes (F6), then bolt 048 which owns F1.

## Session cost

| Date | Bolt | Stage | Turns | Tools | Fresh | Cache read | Output | Misses |
|---|---|---|---|---|---|---|---|---|
| 2026-09-04T13:28:34Z | 047-coupon-domain-and-api | test | 159 | 91 | 1.6M | 10.6M | 0.2M | 4 |

## Stage exit — 047-coupon-domain-and-api — test — 2026-09-04T14:45:00Z

- Done: Stage 5 closed. **F8 (redemption transfer):** `OrderService.CreateFromCartAsync` now reads
  the abandoned holder's `CouponRedemptions` row before resolving (only while the holder is
  `AwaitingPayment`/`PaymentFailed`), passes `heldCouponId` into
  `ICouponService.ResolveForOrderAsync` so the held slot is validated against `RedemptionsCount - 1`,
  and — when the replacement resolves to the same coupon — repoints that row onto the replacement
  inside the replacement's own transaction while `OrderStatusMachine.Abandon` marks the holder
  `Cancelled` in the same unit of work; no CAS, count unchanged. Release survives only where nothing
  replaces the order. **F9 (stale coupon visible):** `ResolveForCartAsync` returns a `CartCouponView`
  (`src/PhotoPrint.API/Services/Coupons/ICouponService.cs`), `CartResponseDto` gained
  `CouponStatus`/`CouponReason` (`Services/Coupons/CouponCartStatus.cs`: `valid`/`stale`), reads and
  `POST /api/cart` re-validate and report with a zeroed discount, and nothing auto-clears.
  **F2:** `Reducere` row in `EmailTemplates/OrderConfirmed.cshtml` + `DiscountRon`/`CouponCode` on
  `OrderConfirmedEmailModel`. **F6:** `Tests/Unit/Services/Coupons/AdminOrderServiceCouponTests.cs`
  and `Tests/Integration/AdminCouponRelationalTests.cs`. **F7:** `memory-bank/standards/api-conventions.md`
  gained the `code` error-envelope section and the re-validated-read paragraph. **F10:** the rename
  duplicate catch now also matches an unwrapped `PostgresException`. **F11:** the empty cart reports
  the configured VAT rate. New tests: `Tests/Unit/Services/Coupons/CartServiceCouponTests.cs` (6),
  `AdminOrderServiceCouponTests` (3), 2 in `OrderEmailServiceTests`, 4 `[PG]` in
  `Tests/Integration/CouponRedemptionRelationalTests.cs`, 3 `[PG]` in `AdminCouponRelationalTests`.
  ddd-01 invariant 7, ddd-02 (five-row decision table, status guard, failure-mode rows, adversarial
  check 2) and ddd-03 (real test names, dispositions, deferred docs) updated; `bolt.md` set to
  `review-pending` by hand. Runs: `Unit.Services` 879/879, `Integration` 272/282 (10 MinIO skips).
- Decisions: Transfer, not release-and-re-redeem — the count never moves, so no window exists where
  a discounted order stands with a free slot (coordinator direction). Repoint as a tracked-entity
  write, not `ExecuteUpdateAsync` — PostgreSQL checks FKs per statement and `ExecuteUpdateAsync`
  throws on InMemory. Held-slot handling guarded to unpaid holders, because the pre-existing
  unconditional release was already refunding paid orders' slots. Reads report and never write, so
  a stale coupon is visible and removable instead of a checkout dead end. `OrderStatusMachine.Abandon`
  as a named method with `ValidTransitions` byte-identical, so admin cancellation of unpaid orders
  stays refused. F1 (PDF `Reducere` line) left to bolt 048 story 001; F5 + the rate-limiter residual
  routed to bolt 055 by owner ruling 2026-09-04.
- Dead ends: Interface `///` docs on the two new `ICouponService` members were written and then
  deleted — the pre-commit gate lists every added `///` line and only `COMMENTS_OK=1` clears it,
  which this wave refuses; the intended text is parked in ddd-03's "Deferred interface docs".
  A test for the rename-duplicate race was not written: forcing the window between the duplicate
  pre-check and the CAS needs an EF command interceptor, which this repo has no helper for.
  Asserting the late-payment webhook end to end was dropped in favour of the coupon-side assertions —
  the double-charge cluster PPW-687…PPW-690 is parked by owner ruling.
- Next: bolt complete for 047 (`review-pending`, no PR). Next session starts bolt 048
  (`memory-bank/bolts/048-coupon-frontend/bolt.md`) at stage 1, whose story 001 owns the invoice-PDF
  `Reducere` line (F1) and must render `couponStatus`/`couponReason` with a remove-coupon action.
