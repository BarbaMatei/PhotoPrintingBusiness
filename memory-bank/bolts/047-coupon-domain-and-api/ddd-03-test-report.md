---
stage: test
bolt: 047-coupon-domain-and-api
created: 2026-09-04T02:10:00Z
status: incomplete — stage-4 micro-review findings open, see "Micro-review findings"
---

## Test Report: coupon-domain-and-api

> **State when this was written.** The bolt was soft-stopped by the coordinator mid-stage-5.
> Implementation and tests are complete and green; the stage-4 fresh-eyes micro-review has run
> and reported 11 findings (1 blocker, 4 serious, 6 minor) which are recorded below and **not
> yet fixed**. This bolt is therefore **not** at `review-pending`. Read "Micro-review findings"
> before treating any part of this as finished.

### Summary

- **Unit tests (coupon namespaces)**: 45/45 passed — `PhotoPrint.Tests.Unit.Services.Coupons`
- **Unit tests (whole touched namespace)**: 868/868 passed — `PhotoPrint.Tests.Unit.Services`
- **Integration tests**: 267/277 passed, 10 skipped (the MinIO `SkippableFact` suite, gated on
  `STORAGE_TEST_*`) — `PhotoPrint.Tests.Integration`
- **Concurrency gate**: passed, and mutation-checked (below)
- **Coverage**: not measured; this repo has no coverage gate and none was added.

Commands run, one process at a time (machine rule 6):

```text
dotnet test src/PhotoPrint.Tests --filter "FullyQualifiedName~PhotoPrint.Tests.Integration"
  → Passed! Failed: 0, Passed: 267, Skipped: 10, Total: 277, Duration: 1m 36s
dotnet test src/PhotoPrint.Tests --filter "FullyQualifiedName~PhotoPrint.Tests.Unit.Services"
  → Passed! Failed: 0, Passed: 868, Skipped: 0, Total: 868, Duration: 48s
dotnet test src/PhotoPrint.Tests --filter "FullyQualifiedName~MigrationChainTests"
  → Passed! Failed: 0, Passed: 4, Total: 4
```

### The gate, and why it is not green for the wrong reason

`CouponRedemptionConcurrencyRelationalTests.ParallelCheckouts_ForCappedCoupon_RedeemExactlyTheCap_AndCreateNoLosingOrders`
runs 100 parallel checkouts against a coupon with `MaxRedemptions = 5` on a real PostgreSQL
database and asserts exactly 5 redemptions, `RedemptionsCount = 5`, exactly 5 orders carrying
the code, and 409 `COUPON_EXHAUSTED` with **no order row** for the other 95.

It first failed for a harness reason, not a design one: 100 contexts exceeded the server's
`max_connections` (`53300: sorry, too many clients already`). Fixed inside the test by pointing
the 100 shopper contexts at one Npgsql pool capped at 20 — all 100 tasks still start together
and contend on the same coupon row; only the physical connections are bounded.

**Mutation check.** With the `ExecuteUpdateAsync` CAS branch disabled so the read-check-increment
path runs on PostgreSQL, the test fails with `Expected succeeded to be 5, but found 100` — 20×
over-redemption. The CAS was restored and the test re-run green. The guarantee is proven by a
test that demonstrably reddens when the mechanism is removed.

### Test files added

| File | Covers |
|---|---|
| `Unit/Services/Coupons/CouponDiscountCalculatorTests.cs` | Per-type discount maths, caps, ADR-019 rounding, and the ADR-026 ordering pinned against the wrong-order figure |
| `Unit/Services/Coupons/CouponServiceTests.cs` | Validation matrix, normalisation, apply/replace/clear, resolve-without-writing, consume, release, guest→user transfer |
| `Unit/Services/Coupons/OrderServiceCouponTests.cs` | Discount on the order, VAT ordering, money invariant, payable floor, replay, declined-card release |
| `Integration/CouponRedemptionConcurrencyRelationalTests.cs` | **The gate** (PostgreSQL) |
| `Integration/CouponRedemptionRelationalTests.cs` | Transactional commit, rollback, replay, both key-reuse release paths, CAS predicate on deactivation/expiry (PostgreSQL) |
| `Integration/CartCouponEndpointsIntegrationTests.cs` | HTTP contract for apply/clear, the `code` extension, dual-auth, guest path |
| `Integration/AdminCouponsIntegrationTests.cs` | Admin CRUD, role gate, duplicate/immutability/soft-delete conflicts, paging clamp, redemption stats |
| `Integration/CouponRateLimitIntegrationTests.cs` | Pins the policy attribute and its default (see the residual below — the policy itself is inert) |
| `Integration/CouponFactory.cs`, `Helpers/TestCoupons.cs` | Seeds |
| `Unit/Services/Invoicing/InvoiceXmlBuilderDiscountTests.cs` | UBL `AllowanceCharge`, totals reconciliation, transport line stays positive, undiscounted output unchanged |
| `Unit/Services/GuestSessionCleanupJobTests.cs` (extended) | Expired guest session takes its `CartCoupon` with it |

### Failure-mode table carried from ddd-02, with the real test names

`[PG]` = runs against real PostgreSQL. **Bold** rows are the ones whose promised test does not exist.

| What can fail | Test that proves it | State |
|---|---|---|
| Concurrent checkouts race for the last slot | `[PG] CouponRedemptionConcurrencyRelationalTests.ParallelCheckouts_ForCappedCoupon_RedeemExactlyTheCap_AndCreateNoLosingOrders` | green + mutation-checked |
| Coupon deactivated between validation and commit | `[PG] CouponRedemptionRelationalTests.Consume_CouponDeactivatedAfterValidation_RefusesWithInvalidCoupon` | green |
| Coupon expires between validation and commit | `[PG] CouponRedemptionRelationalTests.Consume_CouponExpiredAfterValidation_RefusesWithInvalidCoupon` | green |
| Order insert fails after validation | `[PG] CouponRedemptionRelationalTests.Checkout_OrderInsertFails_LeavesNoRedemptionAndNoCountChange` | green |
| Idempotent replay of a coupon order | `[PG] CouponRedemptionRelationalTests.Checkout_IdempotentReplay_DoesNotRedeemTwice`, `OrderServiceCouponTests.Replay_OfDiscountedOrder_DoesNotRedeemTwice` | green |
| Replay of a discounted order judged divergent | `OrderServiceCouponTests.Replay_OfDiscountedOrder_IsNotTreatedAsDivergent`, `…DivergenceCheck_ForCouponFreeOrder_IsUnchangedByTheDiscountAwareComparison` | green |
| Declined card retried with the same key | `[PG] CouponRedemptionRelationalTests.PaymentFailedRetry_ReleasesTheAbandonedRedemption`, `OrderServiceCouponTests.PaymentFailedRetry_ReleasesTheAbandonedRedemption_SoOnePurchaseSpendsOneSlot` | green |
| Stale (>24 h) key reclaimed | `[PG] CouponRedemptionRelationalTests.StaleKeyReclamation_ReleasesTheAbandonedRedemption` | green |
| **Admin cancels an order → redemption released** | **`AdminOrderServiceCouponTests.CancelOrder_ReleasesRedemption_AndIsIdempotent` — does not exist** | **MISSING (micro-review F6)** |
| Discount leaves an uncharageable total | `OrderServiceCouponTests.Checkout_DiscountLeavesTotalBelowStripeMinimum_Returns409_AndWritesNothing` | green |
| Admin creates a 100 % coupon | `AdminCouponsIntegrationTests.CreateCoupon_PercentValueOf100_IsRejected` | green |
| Discount larger than the goods subtotal | `CouponDiscountCalculatorTests.Compute_FixedValueAboveSubtotal_CapsAtSubtotal` | green |
| VAT computed in the wrong order | `OrderServiceCouponTests.Checkout_WithDiscount_ExtractsVatAfterDiscount_NotBefore`, `CouponDiscountCalculatorTests.DiscountThenVat_DiffersFromVatThenDiscount_AndIsTheDeclaredFigure` | green |
| Rounding drifts from ADR-019 | `CouponDiscountCalculatorTests.Compute_PercentWithHalfBani_RoundsAwayFromZero` | green |
| `FreeShipping` with zero shipping cost | `OrderServiceCouponTests.Checkout_FreeShippingWithZeroShippingCost_RedeemsNothing` | green |
| `FreeShipping` exceeds the goods subtotal | `CouponDiscountCalculatorTests.Compute_FreeShipping_IsCappedAtPayableGross_NotGoods` | green |
| Unknown / inactive / expired code applied | `CartCouponEndpointsIntegrationTests.ApplyCoupon_UnknownCode_Returns422WithInvalidCouponCode`, `…ApplyCoupon_InactiveAndExpiredCodes_AreIndistinguishableFromUnknown` | green |
| Coupon applied to an empty cart | `CartCouponEndpointsIntegrationTests.ApplyCoupon_EmptyCart_Returns422WithEmptyCartCode` | green |
| Second code applied over a first | `CartCouponEndpointsIntegrationTests.ApplyCoupon_Twice_ReplacesSilently` | green |
| Cart shrinks below the minimum after apply | `CouponServiceTests.ResolveForCart_WhenCouponStopsQualifying_ReportsNothing_AndWritesNothing`, `…ResolveForCart_WhenAskedToDelete_RemovesTheUnusableRow` | green |
| Guest applies a code then logs in | `CouponServiceTests.TransferGuestCoupon_MovesTheCodeOntoTheUsersCart`, `…WhenTheUserAlreadyHasOne_KeepsTheUsersAndDropsTheGuests` | green (service level; no HTTP merge test) |
| **Code enumeration probing → 429** | **`CouponRateLimitIntegrationTests.ApplyCoupon_BeyondPerIdentityLimit_Returns429` — removed; the policy is inert** | **NOT ENFORCED (see residual)** |
| Admin creates a duplicate code | `AdminCouponsIntegrationTests.CreateCoupon_DuplicateCodeDifferingOnlyInCase_Returns409` | green |
| **Admin renames a code while a checkout redeems it** | `AdminCouponsIntegrationTests.UpdateCoupon_CodeChangeAfterRedemption_Returns409` covers the sequential case; **`AdminCouponRelationalTests.RenameRacingARedemption_Fails_AndLeavesTheCodeIntact` does not exist** | **PARTIAL** |
| Admin deletes an already-inactive coupon | `AdminCouponsIntegrationTests.DeleteCoupon_IsSoft_AndSecondCallReturns409` | green |
| Non-admin calls an admin coupon endpoint | `AdminCouponsIntegrationTests.AdminEndpoints_NonAdminUser_Return403` | green |
| Discounted invoice reaches ANAF | `InvoiceXmlBuilderDiscountTests.Build_OrderWithDiscount_ReconcilesTaxExclusiveAgainstLinesMinusAllowance`, `…KeepsTransportLinePositive`, `…LineAmountsSumToLineExtensionTotal` | green |
| Undiscounted invoice regresses | `InvoiceXmlBuilderDiscountTests.Build_OrderWithoutDiscount_EmitsNoAllowanceCharge` + the 21 pre-existing `InvoiceXmlBuilderTests` | green |
| Guest session expires with a coupon applied | `GuestSessionCleanupJobTests.ExpiredSession_WithAnAppliedCoupon_TakesTheCartCouponWithIt` | green |

### Acceptance-criteria validation

- ✅ **001-coupon-schema** — `Coupons`, `CouponRedemptions`, `CartCoupons` and the two `Orders`
  columns exist in the edited baseline migration; `MigrationChainTests` proves the chain applies
  to an empty PostgreSQL database and that the snapshot matches the model. Deviations: `Code` is
  `varchar(20)` (matches its validator, not the story's `varchar(50)`), and there is **no
  `RowVersion`** (ADR-025).
- ✅ **002-cart-coupon-endpoints** — apply/clear implemented with dual auth, case-insensitive
  matching via normalisation, silent replace, and the three `code` values. `DELETE` returns 200
  with the recomputed cart (recorded deviation).
- ⚠️ **003-redemption-on-order-create** — atomic redemption, discount-then-VAT, and the
  concurrency guarantee are implemented and proven. **Open:** micro-review F8 (the release at the
  top of the retry path commits before the replacement order exists) and F9 (a stale cart coupon
  is invisible on read but 409s at checkout) both live on this story's path.
- ⚠️ **004-admin-coupon-crud** — all five endpoints, role gate, soft delete, code immutability and
  redemption stats work. **Open:** F6 (no test for the admin-cancel release) and F10 (a rename
  collision can 500 instead of 409).
- ➖ **001-cart-coupon-ux** — bolt 048, not started.

### Issues found during testing (already fixed)

1. **Coupon resolved before the abandoned slot was released.** `OrderServiceCouponTests.PaymentFailedRetry_…`
   failed: a declined-card retry on a `MaxRedemptions = 1` coupon 409'd `COUPON_EXHAUSTED`, because
   `ResolveForOrderAsync` ran before the idempotency block released the failed order's redemption.
   Fixed by moving coupon resolution to **after** the idempotency block, so a replay never touches
   coupon state at all.
2. **DI composition test missed the new dependency.** `AdminOrderServicePaidRaceTests.TheServiceStillResolvesWithNoSentryHubRegistered`
   builds its own container; it needed `ICouponService` registered. A caller-sweep miss on my part —
   the sweep covered constructors the compiler checks, not hand-built containers.

### Micro-review findings (stage-4 gate) — OPEN

One fresh Explore agent over `git diff origin/main...HEAD`, asked the three `bolt-process.md`
questions. Findings are recorded verbatim in intent; **none are fixed yet.**

| # | Severity | Finding | Disposition |
|---|---|---|---|
| F1 | **blocker** | The discount reaches the UBL XML but not the PDF: `InvoicePdfDocument` prints undiscounted lines and a post-discount total, so a discounted invoice shows an unexplained gap. ADR-026 requires a `Reducere` line. | **Bolt 048's story 001 owns the PDF line.** Must be fixed there before either bolt is review-pending. |
| F2 | serious | Order-confirmation email renders subtotal/shipping/total with no discount row, so the customer's email arithmetic does not add up. `OrderConfirmedEmailModel` has no discount member. | Open. Same class as F1; decide whether it belongs to 048 or a follow-up. |
| F6 | serious | `AdminOrderService.CancelOrderAsync` releases the redemption but no test covers it; the two test class names ddd-02 promised (`AdminOrderServiceCouponTests`, `AdminCouponRelationalTests`) do not exist. | Open — write the tests. |
| F8 | serious | The release at the top of the retry path commits its own transaction before the replacement order exists; if the replacement then fails (e.g. payable floor), the slot is refunded while the discounted order still stands and its intent may later confirm. | Open — needs a design answer, not just a patch. |
| F9 | serious | A coupon that goes stale is invisible on cart reads (reports `couponCode: null`) but 409s at checkout, giving a dead end the client cannot see or clear. Read and checkout paths diverge on the identical condition. | Open. Directly contradicts the "reads never write" residual as designed — revisit that decision. |
| F3 | minor | `ix_coupon_redemptions_user_coupon` has no reader (it exists for a future per-user cap). | Accepted; stated in ddd-02. |
| F5 | minor | The rate-limit default 15 is only a C# initializer — absent from `appsettings.json`, no stated rationale, no saturation signal. | Open, but subsumed by the residual below. |
| F7 | minor | `api-conventions.md` still documents the error envelope without `code`, and does not distinguish the two 422 shapes. | Open — one-paragraph doc edit. |
| F10 | minor | `AdminCouponService.RenameOrThrowAsync` uses `ExecuteUpdateAsync`, whose `PostgresException` is not wrapped in `DbUpdateException`, so a rename collision can 500 instead of 409; the rename also commits separately from the rest of the edit. | Open. |
| F11 | minor | `CartResponseDto.Empty` reports `VatRate: 0`; an empty cart never auto-clears a stranded `CartCoupon`. | Open. |

Explicitly clean per the micro-review: the edited baseline migration (all 42 `EasyboxLocker` seed
rows, the raw-SQL `uq_invoices_series_year_number` index and the `invoice_seq_ft_2026` sequence
survive; `Down` is FK-correct), `InvoiceXmlBuilder`'s residual for the undiscounted case,
`CreateFromCartAsync`'s detach/savepoint paths, and `GuestSessionCleanupJob`.

### What this suite cannot prove

- **Atomicity on EF InMemory.** The CAS and real transactions do not exist there; every redemption
  guarantee is proven only by the `[PG]` classes. An InMemory test asserting redemption semantics
  would be theatre and none was written.
- **ANAF's own acceptance of the `AllowanceCharge` shape.** The tests assert schema-level
  reconciliation (`TaxExclusiveAmount = LineExtensionAmount − AllowanceTotalAmount`, no negative
  line); the regulator's validator is not run and no XSD is pulled in — the same limitation the
  pre-existing `InvoiceXmlBuilderTests` states.
- **Redemptions held by orders that are abandoned and never retried.** There is no sweeper; the
  slot stays spent. Stated in ddd-02 as an accepted residual with its abuse vector.
- **The per-identity coupon rate limit.** It does not execute — see the residual below.
- **Per-user redemption caps.** Out of scope (requirements assumption set).
- **The cart-vs-order pricing-basis divergence.** `CartService` and `OrderService` compute the
  goods subtotal on different bases (pre-existing, documented in ddd-02); no test pins how a
  coupon behaves across that gap.

### Stated residual: the coupon rate-limit policy does not execute

`[EnableRateLimiting(SecurityExtensions.CouponRateLimitPolicy)]` on `POST /api/cart/coupon` is
inert. `app.UseSecurityBaselines()` (`Program.cs:383`) calls `app.UseRateLimiter()`
(`Extensions/SecurityExtensions.cs:145`), which runs **before** `app.UseRouting()`
(`Program.cs:396`) and `app.UseAuthentication()` (`Program.cs:397`). The rate-limiting middleware
resolves a named policy from `HttpContext.GetEndpoint()`, which is null before routing, so the
attribute is never read; and `CouponRateLimitPartitionKey` reads `context.User`, unauthenticated
at that point, so even if it were read the partition would be per-IP rather than per-identity.

Proven, not inferred: a test firing 16 requests at the endpoint (policy = 15/min) got **422 on the
16th, not 429**. That test was replaced by two that pin the attribute's presence and its default,
so the wiring cannot be silently dropped when the ordering is fixed.

**What this costs today.** ddd-02 names this limiter as the compensating control for the
`MIN_SUBTOTAL_NOT_MET` code-existence oracle: a valid, live code below its minimum answers
differently from an unknown one, and the message interpolates the threshold. The stories fix both
the code and its Romanian copy, so it cannot be collapsed into `INVALID_COUPON`. With the policy
inert the only brake is the global fixed window of **100 requests/minute per IP**
(`RateLimit:Public:PermitLimit`), i.e. ~6 000 guesses per hour per IP against
human-memorable codes, and a guest token is free to mint. The control goes live the moment
`UseRateLimiter()` moves after `UseRouting()`; nothing else about the coupon code changes.

**Routed to bolt 055 by owner ruling 2026-09-04.**

### Proposed findings (outside this bolt)

**PF-1 — Every `[EnableRateLimiting]` endpoint policy in the application is inert, including the
auth brute-force limiters.** Same cause as the residual above: `Program.cs:383` →
`SecurityExtensions.cs:145` runs `UseRateLimiter()` before `UseRouting()` at `Program.cs:396`.
Affected policies: `AuthRateLimitPolicy` (10/min on login), `RegisterRateLimitPolicy` (5/hour),
`ResendConfirmationRateLimitPolicy` and `ForgotPasswordRateLimitPolicy` (3/hour each), all on
`AuthController`, plus this bolt's coupon policy. Login is therefore protected only by the global
100/min-per-IP limiter. Evidence: the 16-request test above returned 422 on the 16th rather than
429. Pre-existing; not introduced by this bolt. Group P-A found and proved the same defect
independently. **Routed to bolt 055 (Program.cs pipeline rewrite) by owner ruling 2026-09-04**;
the fix is a one-line move whose blast radius is four dormant auth policies going live at once,
so it needs its own test fallout budget.

### Deferred interface docs

Written comment-free by wave instruction (the pre-commit comment gate flags `///` on interface
members and `COMMENTS_OK=1` is refused this wave). Intended one-line docs, for a possible
docs-only commit later:

| Member | Intended doc |
|---|---|
| `ICouponService.ApplyToCartAsync` | Stores the code against the caller's cart, replacing any previous one; throws `CouponRejectedException` (422) when it cannot be used. |
| `ICouponService.ClearCartCouponAsync` | Removes the caller's applied code; a no-op when none is applied. |
| `ICouponService.ResolveForCartAsync` | The applied coupon re-validated against the current cart, or null. Writes nothing unless `deleteWhenUnusable` is set, so cart reads stay reads. |
| `ICouponService.ResolveForOrderAsync` | The applied coupon re-validated at checkout with shipping known; throws `CouponConflictException` (409) when it no longer applies. |
| `ICouponService.ConsumeOrThrowAsync` | Takes one redemption slot atomically; throws `CouponConflictException` (409) naming the real reason when refused. |
| `ICouponService.ReleaseForOrderAsync` | Returns the slot held by an abandoned order; a no-op when the order redeemed nothing. |
| `ICouponService.TransferGuestCouponAsync` | Moves a guest's applied code onto the user's cart during a login merge; the user's own code wins if both have one. |
| `IAdminCouponService.*` | CRUD over coupons for admins; `DeactivateAsync` is a soft delete, `ListRedemptionsAsync` reads redemption rows rather than the counter. |
| `IErrorCoded.ErrorCode` | Machine-readable reason surfaced as the `code` member of the ProblemDetails body. |

### Recommendations

1. Fix F1 in bolt 048 (the `Reducere` PDF line) — it is that bolt's acceptance criterion and this
   bolt's ADR-026 requirement.
2. Answer F8 and F9 with a design decision before patching; both are on the money path.
3. Add the two missing test classes named in ddd-02 (F6) rather than quietly dropping them.
4. A reclaim job for abandoned `AwaitingPayment` orders holding redemptions is the recommended
   follow-up bolt; it is a new periodic mechanism and deliberately out of scope here.
