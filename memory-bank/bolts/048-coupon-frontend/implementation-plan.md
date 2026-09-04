---
stage: plan
bolt: 048-coupon-frontend
created: 2026-09-04T15:10:00Z
design_check: passed-with-changes (all findings folded in below)
---

## Implementation Plan: coupon-frontend

### Objective

Make the coupon that bolt 047 built visible and usable to the customer: a "Cod promo" input on the
cart page, Romanian error copy chosen by the machine-readable `code`, a `Reducere` row on the cart
summary / checkout review / order confirmation, and the `Reducere` line on the fiscal invoice PDF
(047's finding F1, routed here). One story: `001-cart-coupon-ux`.

Backend surface already exists and is not re-designed here:
`POST /api/cart/coupon` (`ApplyCouponRequest { code }` -> `CartResponseDto`, 422 + `code`,
429 under `CouponRateLimitPolicy`), `DELETE /api/cart/coupon` -> `CartResponseDto`,
`GET/POST /api/cart` carrying `couponCode` / `couponType` / `couponStatus` (`valid` | `stale`) /
`couponReason` / `discountRon` / `totalRon` / `netTotalRon` / `vatRon` / `vatRate`, and
`OrderDetailDto` / `OrderPaymentStatusDto` carrying `couponCode` + `discountRon`.

### Deliverables

1. **Cart contract in the SPA** - `src/app/core/models/cart.model.ts`: the seven coupon/total fields
   added to `CartResponseDto` as **optional** members, `EMPTY_CART` extended, `ApplyCouponRequest`
   added. Optional because a guest cart restored from `localStorage` was written by an older build
   and has none of them; every read is defaulted (`totalRon ?? subtotal`, `discountRon ?? 0`).
   `review-step.spec.ts` builds a three-field cart literal, which keeps compiling under this shape.
2. **Coupon copy map** - `src/app/core/models/coupon-messages.ts`: `code` -> Romanian sentence for
   `INVALID_COUPON`, `MIN_SUBTOTAL_NOT_MET`, `COUPON_EXHAUSTED`, `EMPTY_CART`,
   `ORDER_TOTAL_BELOW_MINIMUM`, `NO_DISCOUNT`, plus a 429 sentence and a default. Resolution order
   is **map first**: the server `detail` is used only for `MIN_SUBTOTAL_NOT_MET`, otherwise the map
   wins and `detail` is the fallback. The middleware always fills `detail` from the exception
   message, so a detail-preferring order would make the map dead in production and its specs would
   only pass against a response the server cannot emit. 429 is its own branch, not a `code` branch:
   the rate limiter returns plain text with no JSON body at all.
3. **`CartService`** - `applyCoupon(code)` (POST) and `clearCoupon()` (DELETE), both tapping the
   returned cart into the cart subject and re-persisting to `localStorage` for guests exactly as
   `setCart` does; no new state, no new stream. Plus: `loadFromLocalStorage` follows a restored cart
   that has a `couponCode` with `loadFromServer()` - guests otherwise never re-read the cart, so a
   coupon that went stale after the snapshot was written would stay displayed as valid forever
   (`GET /api/cart` accepts guests).
4. **Cart page** (`features/cart/pages/cart-page.ts`) - a reactive-form `Cod promo` input +
   `Aplică` button under the groups list; when a coupon is applied, the code with an `Elimină`
   action instead of the input; inline error paragraph; summary rows become
   `Subtotal` / `Reducere -X,XX lei` (only when `discountRon > 0`) / `Total` (from `totalRon`);
   a stale `couponStatus` warning showing the `couponReason` copy with the same `Elimină` action,
   because reads report staleness and never repair it. A valid coupon of type `FreeShipping` has
   `discountRon === 0` at cart level (shipping is unknown there), so it renders as
   `Transport gratuit cu codul <COD>` instead of a `Reducere` row.
5. **Checkout review step** (`features/checkout/pages/review-step.ts`) - a `Reducere` row between
   `Subtotal` and `Total`, the stale warning + `Elimină` action, and `grandTotal()` changed from
   `subtotal + shipping` to `(totalRon ?? subtotal) + (freeShipping ? 0 : shipping)`, where
   `freeShipping` means a valid coupon of type `FreeShipping`. Without that branch a free-shipping
   coupon displays a shipping cost the order will not charge (worked example below). The `Transport`
   row shows `0,00 RON` with the code in that case.
6. **Confirmation page** (`features/orders/pages/confirmation-page.ts`) - a
   `Reducere (COD): -X,XX RON` row above the `din care TVA` line when `discountRon > 0`;
   `OrderPaymentStatusDto` in `core/models/payment.model.ts` gains the two fields the API sends.
7. **Invoice PDF** (`Services/Invoicing/InvoicePdfDocument.cs`) - `ComposeTotals` renders, when
   `order.DiscountRon > 0`: `Total net` (net **before** discount), the allowance reason line
   (`Reducere <COD>`, or the commercial-discount wording when the code is blank - the same string
   the UBL uses) as a negative net amount, `TVA (x%)`, `Total de plată`. Unchanged output when there
   is no discount. The row list comes from a pure function returning label/amount pairs; formatting
   stays inside `InvoicePdfDocument` so its `Ro` culture field remains the only money formatter
   (`InvoicePdfCultureTests` reflects on it).
8. **Shared discount math** (`Services/Invoicing/InvoiceDiscountMath.cs`) - `LineNetTotal`,
   `VatRateFromInvoice` and the allowance subtraction and its reason string lifted out of
   `InvoiceXmlBuilder` and consumed by both the XML builder and the PDF document, so the UBL file
   filed with ANAF and the PDF handed to the customer cannot disagree. `VatRateFromInvoice` moves
   too: it is what makes the XML numbers reproducible for a detached `Invoice` (no `Order`
   navigation loaded), where `VatRon / NetTotalRon` rounds to a different rate than
   `order.VatRate` - taking `order.VatRate` in the shared helper instead would shift
   `LineExtensionAmount`, `AllowanceTotalAmount` and the residual pushed onto the last line.
9. **New spec files** - `features/cart/pages/cart-page.spec.ts` does not exist and is created here
   (TestBed for an OnPush component injecting `CartService` with `HttpTestingController` and an
   `UploadService` stub, since the page fetches a preview blob per item); `coupon-messages.spec.ts`
   is new; `cart.service.spec.ts`, `review-step.spec.ts` and `confirmation-page.spec.ts` are
   extended.

### Dependencies

- **047-coupon-domain-and-api** (same branch): every endpoint, DTO field and error code above.
- **014-upload-format-cart-ui**: the cart page and its summary aside that this bolt edits.
- **039-efactura-anaf**: `InvoiceXmlBuilder` (source of the shared math) and `InvoicePdfDocument`.
- `VatCalculator.ExtractBreakdown` - reused via the shared math helper, never re-implemented.

### Technical Approach

- **Copy resolution.** One helper renders a coupon failure, map-first as in deliverable 2, so the
  sentences the acceptance criteria pin are owned and tested in the SPA while
  `MIN_SUBTOTAL_NOT_MET` still shows the server's interpolated RON threshold (the only place that
  threshold exists - no endpoint exposes the coupon's minimum subtotal). Branching stays on `code`,
  never on `detail` (api-conventions.md).
- **No new client state.** The applied coupon is server state, read back from the cart response; the
  SPA keeps no separate signal for it, so cart-merge on login and backend auto-clearing need no
  frontend work (both story edge cases are already handled server-side by the guest-coupon transfer
  and the clear-on-empty path).
- **Guest persistence.** `applyCoupon` / `clearCoupon` re-save the whole returned cart under
  `fotoTipar_cart` for guests - the merge-preserving path, definition-of-done class 11 - and a
  restored cart carrying a coupon is refreshed from the server (deliverable 3).
- **Angular 21 house style.** Standalone, `OnPush`, inline template + styles as these files already
  are; `ReactiveFormsModule` on the cart page; `DecimalPipe` for money; Romanian strings inline.
  Cart page keeps its `ChangeDetectorRef.markForCheck()` pattern; no zone.js, Prettier only.
- **PDF.** QuestPDF tree only - no new dependency, no layout rework; the discount rows are added
  inside the existing `ComposeTotals` container.

### Worked money examples (pinned by tests)

Subtotal 250.00, courier 19.99, VAT 19%.

| Coupon | cart discount / total | review total must show | order charges |
|---|---|---|---|
| Percent 10 | 25.00 / 225.00 | 244.99 | 244.99 |
| FreeShipping | 0.00 / 250.00 | **250.00** (shipping row 0,00) | 250.00 |
| none | - / 250.00 | 269.99 | 269.99 |

Invoice for the Percent-10 case: line net = net(269.99) = **226.88**, allowance net =
226.88 - 205.87 = **21.01**, `invoice.NetTotalRon` = **205.87**, `VatRon` = **39.12**,
`TotalRon` = **244.99**. PDF rows must read 226.88, -21.01, 39.12, 244.99 - identical to the UBL
`LineExtensionAmount` / `AllowanceTotalAmount` / `TaxExclusiveAmount` / `PayableAmount`. The PDF row
is the **net** allowance (21.01), not the gross discount (25.00).

### Decisions

- **Copy is chosen by `code`, with the server sentence only where it carries data.** See
  deliverable 2. Rejected: preferring `detail` everywhere (makes the map untestable), and adding a
  minimum-subtotal ProblemDetails extension (a new error-payload mechanism on a money path, from a
  frontend bolt).
- **Totals come from the backend, never recomputed** - except the two client-side compositions that
  must exist: adding shipping on the review step and zeroing it for a free-shipping coupon.
  `discountRon`, `totalRon`, `vatRon` are read as sent (ADR-026 fixes discount-then-VAT ordering
  server-side).
- **`couponType` is consumed, not just carried.** The cart resolves a free-shipping coupon against a
  zero shipping cost and the order resolves it against the real one, so the type is the only thing
  that tells the review step which total is truthful.
- **PDF mirrors the UBL, not the cart.** The invoice shows net-before-discount plus a net allowance
  because that is what the filed XML already declares; one shared helper is what keeps them equal.
- **The stale coupon is shown, not auto-removed.** Reads never write (047 F9); the customer removes
  it, and checkout remains the authority that refuses with 409 and the same `code`.
- **The checkout 409 dead-end is left in place and reported, not fixed here.** `payment-step.ts`
  maps a coupon 409 onto its generic "reload the page" message, which is unactionable: reloading
  does not detach the coupon. Naming the coupon there is exactly the parked PPW-705 row (swallowed
  409 message) inside the parked PPW-687..690 cluster, so under the owner's 2026-09-03 ruling this
  bolt does not widen into it. Consequence to state at hand-back: a coupon that goes stale between
  the review step and payment still produces a vague error, and `ORDER_TOTAL_BELOW_MINIMUM` is
  rendered nowhere - its sentence is pinned by `coupon-messages.spec.ts` only.

### Caller-impact sweep

| Consumer of a touched contract | Disposition |
|---|---|
| `core/services/cart.service.ts` | **updated** - two methods added; guest restore refreshes a couponed cart; `EMPTY_CART` reshaped |
| `core/services/cart.service.spec.ts` | **updated** - apply/clear, guest persistence, legacy-shape restore, couponed-restore refresh |
| `features/cart/pages/cart-page.ts` | **updated** - input, error, summary rows, stale warning, free-shipping note |
| `features/cart/pages/cart-page.spec.ts` | **new** - no spec exists for this page today (deliverable 9) |
| `features/checkout/pages/review-step.ts` | **updated** - `Reducere` row, free-shipping branch, `grandTotal()` |
| `features/checkout/pages/review-step.spec.ts` | **updated** - discounted and free-shipping totals |
| `features/orders/pages/confirmation-page.ts` | **updated** - `Reducere` row |
| `features/orders/pages/confirmation-page.spec.ts` | **updated** - discount row rendering |
| `core/models/payment.model.ts` | **updated** - `couponCode` + `discountRon` on the status DTO |
| `layout/header/header.ts` / `header.spec.ts` | **unaffected** - reads only the item count; the spec pushes its cart literal through `as any` |
| `features/checkout/pages/payment-step.ts` | **affected but deliberately untouched** - its generic 409 copy hides coupon conflicts; re-deferred with the parked cluster (see Decisions) |
| `features/checkout/pages/delivery-step.ts` | **unaffected** - shipping only, and owned by another group this wave |
| `Services/Invoicing/InvoiceXmlBuilder.cs` | **updated** - `LineNetTotal`, `VatRateFromInvoice`, the allowance subtraction and its reason move to the shared helper; XML output byte-identical |
| `Services/Invoicing/InvoicePdfRenderer.cs` | **unaffected** - façade; the `InvoicePdfDocument` constructor signature stays |
| `Tests/Unit/Services/Invoicing/InvoiceXmlBuilderDiscountTests.cs` | **unaffected** - asserts XML output, which does not change; a detached-invoice case is added to prove the lift |
| `Tests/Unit/Services/Invoicing/InvoicePdfCultureTests.cs` | **unaffected** - reflects on the `Ro` static field, which stays the only formatter |
| `Services/CartService.cs`, `Controllers/CartController.cs`, `Services/Coupons/*` | **unaffected** - consumed as-is, no backend contract change |

### Failure-mode table

| What can fail | What should happen | Which test proves it | Log line |
|---|---|---|---|
| Apply returns 422 `INVALID_COUPON` | inline "Codul introdus nu este valid sau a expirat.", cart untouched | `cart-page.spec.ts` - mapped copy on 422 | none (client) |
| Apply returns 422 `MIN_SUBTOTAL_NOT_MET` | the server sentence with its RON threshold is shown | `coupon-messages.spec.ts` - `detail` wins for this code only | none |
| 422 arrives with an unknown `code` and no `detail` | default sentence, never a blank error box | `coupon-messages.spec.ts` - default fallback | none |
| Apply returns 429 (plain-text body, no `code`) | the too-many-attempts copy, no retry loop, button re-enabled | `cart-page.spec.ts` - 429 branch | server-side |
| Two applies in flight (double click) | button disabled while in flight; the response updates the cart once | `cart-page.spec.ts` - second click blocked | none |
| Coupon went stale in the cart | warning + `Elimină`, discount 0, total = subtotal, checkout not silently blocked | `cart-page.spec.ts` + `review-step.spec.ts` - stale rendering | none |
| Guest reloads after the coupon went stale server-side | the restored cart with a `couponCode` is refreshed from `GET /api/cart`, then shows the stale warning | `cart.service.spec.ts` - couponed restore refreshes | none |
| Guest cart restored from `localStorage` without the new fields | totals fall back to `subtotal`, no `NaN`, no discount row, no refresh call | `cart.service.spec.ts` - legacy-shape restore | none |
| Guest applies a coupon and reloads | coupon still shown (cart re-saved to `localStorage`) | `cart.service.spec.ts` - persistence after apply | none |
| Free-shipping coupon at the review step | total excludes shipping and equals what the order charges (250.00 in the worked example) | `review-step.spec.ts` - free-shipping total | none |
| Discounted order's invoice PDF | `Reducere` row present, `Total net` before discount, `TVA` and `Total de plată` unchanged, values equal to the UBL amounts | `InvoicePdfDiscountTests` (row list) + a shared-math test asserting PDF and XML read the same numbers | none |
| Discounted order with a blank coupon code | the PDF reason matches the string the UBL emits | `InvoicePdfDiscountTests` - blank-code reason | none |
| Detached invoice (no `Order` navigation) | the shared `VatRateFromInvoice` derives the same rate the XML builder used; `LineExtensionAmount` and the residual are unchanged | `InvoiceXmlBuilderDiscountTests` - detached-invoice case | none |
| Undiscounted order's invoice PDF | totals block unchanged, no empty row | `InvoicePdfDiscountTests` - no-discount case | none |

The PDF assertion cannot read text out of QuestPDF's compressed streams (the existing
`InvoicePdfRendererTests` only checks header, trailer and size). The rows are therefore produced by
a pure function returning ordered label/amount pairs, asserted directly, with the document composing
and formatting from it; a rendered-bytes smoke test covers the discounted document too.

### Backlog sweep (`reviews/state/backlog.md`; areas touched: cart, checkout UI, invoice PDF)

- **PPW-690** cart never cleared on the 409 "already paid" redirect - `re-deferred: owner ruling
  2026-09-03` (the parked PPW-687..690 double-charge cluster).
- **PPW-688** and **PPW-705** payment-intent reuse and the swallowed 409 message - `re-deferred:
  owner ruling 2026-09-03`; PPW-705 is also the reason the coupon 409 copy in `payment-step.ts` is
  left alone (see Decisions).
- **PPW-640** `/checkout/recapitulare` has no delivery-complete guard and mislabels a null method
  as courier - `re-deferred: unrelated to the coupon story; it is a delivery-state guard in a file
  this bolt edits, and fixing it would widen into the delivery step another group owns this wave`.
- **PPW-501** buyer-name fallback duplicated between `InvoiceXmlBuilder` and the PDF renderer -
  `re-deferred: this bolt de-duplicates the discount math only; the buyer-name copy is a separate
  refactor in a closed target's area`.
- **PPW-656** third copy of the mandatory-address list in `checkout-state.service.ts` -
  `re-deferred: file not touched`.
- **PPW-621** and **PPW-681** invoice-PDF cache headers - `re-deferred: this bolt changes PDF
  content, not the delivery endpoint or its caching`.

Nothing under `reviews/state/` is edited by this session; the coordinator writes the re-deferral
notes onto the rows at merge time.

### Acceptance Criteria

- [ ] Cart page has a `Cod promo` input + `Aplică` button below the items list.
- [ ] On success a `Reducere: -X,XX` row appears and the cart total reflects it.
- [ ] `INVALID_COUPON`, `MIN_SUBTOTAL_NOT_MET` and `COUPON_EXHAUSTED` each render their Romanian
      sentence, chosen by `code`.
- [ ] An applied coupon is shown on the checkout review step and on the order confirmation page.
- [ ] A free-shipping coupon shows the review total the order will actually charge.
- [ ] A stale coupon is visible with a working `Elimină` action on cart and review.
- [ ] The invoice PDF renders the `Reducere` line above the VAT total when the order has a discount,
      with numbers equal to the UBL allowance.
- [ ] Scoped tests green: `cart*.spec.ts`, `review-step.spec.ts`, `confirmation-page.spec.ts`,
      `coupon-messages.spec.ts`, and
      `--filter "FullyQualifiedName~PhotoPrint.Tests.Unit.Services.Invoicing"`.
- [ ] No file from this wave's "do not touch" list is modified.

### Self-validation (specsmd human checkpoint)

Validated by this session against the story's acceptance criteria, the 047 hand-off (finding F1 and
its stale-status requirement) and the backend contracts read from source.

Adversarial design check (bolt-process.md gate, fresh subagent, 2026-09-04): **passed with
changes** - 8 findings, all folded into this plan. The four that changed the design: the
free-shipping total mismatch (deliverables 4 and 5), the guest cart that never re-reads and so
never learns a coupon went stale (deliverable 3), the copy resolution inverted to map-first
(deliverable 2), and `VatRateFromInvoice` moving with the lifted math (deliverable 8). Two were
gaps in the plan's own bookkeeping (the missing `cart-page.spec.ts`, now deliverable 9; the shared
allowance reason string). One is recorded as a deliberate non-fix with its consequence stated (the
checkout 409 dead-end, parked with PPW-705). The check also confirmed the worked money examples
above against source.

Outcome: **approved** - proceed to stage 2 (implement).
