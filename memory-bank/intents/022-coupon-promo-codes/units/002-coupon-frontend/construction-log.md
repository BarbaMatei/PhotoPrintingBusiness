---
unit: 002-coupon-frontend
intent: 022-coupon-promo-codes
created: 2026-09-04T15:40:00Z
last_updated: 2026-09-04T15:40:00Z
---

# Construction Log: coupon-frontend

## Original Plan

**From Inception**: 1 bolt planned
**Planned Date**: 2026-05-25

| Bolt | Type | Stories |
|------|------|---------|
| 048-coupon-frontend | simple-construction-bolt | 001-cart-coupon-ux |

## Execution Log

## Stage exit — 048-coupon-frontend — plan — 2026-09-04T15:40:00Z
- Done: wrote memory-bank/bolts/048-coupon-frontend/implementation-plan.md (9 deliverables, caller-impact sweep, failure-mode table, backlog sweep, worked money examples) and ran the bolt-process adversarial design check as a fresh subagent against it; its 8 findings are folded into the plan. bolt.md moved to status in-progress / current_stage implement with plan recorded in stages_completed. No code touched.
- Decisions: coupon error copy resolves map-first, with the server ProblemDetails detail used only for MIN_SUBTOTAL_NOT_MET (it is the only sentence carrying the RON threshold, and the middleware always fills detail, so a detail-first order would leave the map dead in production and only testable against a response the server cannot emit); the review step must consume couponType, because a FreeShipping coupon is resolved against zero shipping in the cart and against the real shipping cost in the order, so a plain totalRon + shipping over-displays the total by the courier price; the guest cart restore refreshes from GET /api/cart when the snapshot carries a couponCode, because guests otherwise never re-read and would never see a coupon go stale; the invoice PDF gets its numbers from a new shared InvoiceDiscountMath (LineNetTotal, VatRateFromInvoice, allowance amount and reason) lifted out of InvoiceXmlBuilder so the ANAF UBL and the customer PDF cannot disagree, and VatRateFromInvoice moves with it because a detached Invoice derives a different rate than order.VatRate; PDF rows come from a pure label/amount function since QuestPDF text is not assertable and the Ro culture field must stay the only formatter.
- Dead ends: adding a minSubtotalRon ProblemDetails extension to carry the threshold to the client - rejected as a new error-payload mechanism on a money path from a frontend bolt; sharing only LineNetTotal without VatRateFromInvoice - rejected, it silently changes the XML for a detached Invoice; fixing the checkout 409 dead-end in payment-step.ts (a coupon conflict shows a generic reload-the-page message that cannot help, and ORDER_TOTAL_BELOW_MINIMUM is therefore rendered nowhere) - deliberately not done, that is the parked PPW-705 row in the parked PPW-687..690 cluster under the owner ruling of 2026-09-03, so it is recorded in the plan as a stated consequence for the coordinator instead. Writing files with bash heredocs fails in this session whenever the content holds emoji (the backlog severity dots) - the shell reports an unexpected EOF; use the Write tool for such docs.
- Next: stage 2 (implement) - start with deliverable 1, adding the seven optional coupon/total fields plus ApplyCouponRequest to src/PhotoPrint.UI/src/app/core/models/cart.model.ts, then deliverable 2 (coupon-messages.ts) before touching any component.

## Session cost

| Date | Bolt | Stage | Turns | Tools | Fresh | Cache read | Output | Misses |
|---|---|---|---|---|---|---|---|---|
| 2026-09-04T15:38:33Z | 048-coupon-frontend | plan | 98 | 62 | 0.5M | 8.1M | 0.1M | 0 |
| 2026-09-04T15:53:58Z | 048-coupon-frontend | implement | 113 | 69 | 0.3M | 9.4M | 0.1M | 0 |

## Stage exit — 048-coupon-frontend — implement — 2026-09-04T15:56:00Z
- Done: cart contract + coupon copy map + coupon calls in `cart.service.ts`; promo-code box and discount/free-shipping/total summary in `features/cart/pages/cart-page.ts`; discount row, free-shipping total and stale-code removal in `features/checkout/pages/review-step.ts`; discount row in `features/orders/pages/confirmation-page.ts` with the two new fields on `core/models/payment.model.ts` (fixture in `confirmation-page.spec.ts` updated); new shared `src/PhotoPrint.API/Services/Invoicing/InvoiceDiscountMath.cs` consumed by `InvoiceXmlBuilder.cs` (output unchanged) and by `InvoicePdfDocument.cs` (two new discount rows); walkthrough at `memory-bank/bolts/048-coupon-frontend/implementation-walkthrough.md`.
- Decisions: Romanian copy comes from one map keyed by server error codes, with the server `detail` used only for MIN_SUBTOTAL_NOT_MET because it carries the threshold; the SPA never recomputes the discount — it reads server totals and only adds shipping or zeroes it for free shipping; the XML builder’s private discount math became the shared class so PDF and XML can never diverge, and the PDF’s row list lives there too so labels and amounts are assertable without rendering a PDF; a restored guest snapshot carrying a code refreshes from the server, since only the server knows if it went stale; `couponCode`/`discountRon` on the order payment status are required and the one existing fixture was updated rather than weakening the contract.
- Dead ends: no `<form (ngSubmit)>` for the coupon input — `ReactiveFormsModule` alone does not bring `NgForm`, so the submit would never fire; a click plus `keyup.enter` handler is used instead. No Prettier pass: untouched SPA files fail `prettier --check` too, so formatting only the changed files would add unrelated churn. `--filter`-scoped test runs were not attempted this stage — no specs exist yet, they are stage 3.
- Next: write the stage-3 specs from the plan’s deliverable 9 — start with `src/PhotoPrint.UI/src/app/features/cart/pages/cart-page.spec.ts` covering apply success, the 422 code-to-copy mapping and the stale-code warning, then run `node reviews/lib/run-scoped-tests.mjs 048-coupon-frontend --kind green --ui --include "cart-page"`.

## Stage exit — 048-coupon-frontend — test — 2026-09-04T16:20:00Z
- Done: wrote `memory-bank/bolts/048-coupon-frontend/test-walkthrough.md`; added `src/PhotoPrint.UI/src/app/core/models/coupon-messages.spec.ts` (9) and `src/PhotoPrint.UI/src/app/features/cart/pages/cart-page.spec.ts` (10); extended `core/services/cart.service.spec.ts` (+5 coupon tests, legacy-restore totals assertion), `features/checkout/pages/review-step.spec.ts` (+3), `features/orders/pages/confirmation-page.spec.ts` (+2); added `src/PhotoPrint.Tests/Unit/Services/Invoicing/InvoiceDiscountMathTests.cs` (9 facts) and one discounted-order fact in `InvoicePdfRendererTests.cs`. UI 64/64 across 5 spec files, API 154/154 in `PhotoPrint.Tests.Unit.Services.Invoicing`; bolt.md now `status: review-pending`, `current_stage: complete`.
- Decisions: cart-page spec drives the real `CartService` over `HttpTestingController` so only HTTP is mocked — a stubbed service would only prove the template renders what it is handed; money literals are en-US (`225.00`) because no `LOCALE_ID` is registered anywhere; the PDF/UBL agreement is pinned by one fact that builds the real XML and compares the four monetary totals against the PDF rows, since a drift there is a fiscal discrepancy.
- Dead ends: the implement stage's five coupon tests in `cart.service.spec.ts` had been appended after the `describe` block's closing brace — the file did not compile until they were moved inside; the plan's detached-invoice case was not added to `InvoiceXmlBuilderDiscountTests` because the XML builder is always handed its order, so the case belongs to the lifted `VatRateFromInvoice` helper the PDF path calls.
- Next: bolt complete.
