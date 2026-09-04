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
