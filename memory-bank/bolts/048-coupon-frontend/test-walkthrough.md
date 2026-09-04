---
stage: test
bolt: 048-coupon-frontend
created: 2026-09-04T16:15:00Z
---

## Test Report: 002-coupon-frontend

### Summary

- **Tests**: 64/64 passed (UI, 5 spec files) · 154/154 passed (API, `PhotoPrint.Tests.Unit.Services.Invoicing`)
- **Coverage**: not measured — no coverage gate in this repo; scope was pinned by file instead
  (every file the implement stage touched has a spec asserting its new behaviour)

Commands run, one test process at a time:

```
node reviews/lib/run-scoped-tests.mjs 048-coupon-frontend --kind green --ui \
  --include "{cart-page,cart.service,coupon-messages,review-step,confirmation-page}" --no-events
node reviews/lib/run-scoped-tests.mjs 048-coupon-frontend --kind green \
  --filter "PhotoPrint.Tests.Unit.Services.Invoicing" --summary --no-events
```

### Test Files

- [x] `src/PhotoPrint.UI/src/app/core/models/coupon-messages.spec.ts` — **new**, 9 tests. Copy is
  chosen by `code` first: `INVALID_COUPON` and `COUPON_EXHAUSTED` keep the Romanian sentence even
  when the server sends a `detail`; only `MIN_SUBTOTAL_NOT_MET` prefers the server text, because
  that one carries the RON threshold. Also the 429 sentence (429 has no JSON body), an unknown code
  falling back to `detail`, an empty body falling back to the default, and `couponMessageFor` for a
  stale reason and for `null`.
- [x] `src/PhotoPrint.UI/src/app/features/cart/pages/cart-page.spec.ts` — **new**, 10 tests. Runs the
  real `CartService` against `HttpTestingController`, so only the HTTP boundary is mocked: apply
  success (discount row `25.00`, code `VARA10`, total `225.00`), no discount row without a coupon,
  the three error sentences, the 429 sentence, a double click sending one request, the stale warning
  with a working `Elimină` (DELETE, input returns), free shipping announced instead of a discount
  row, and a blank code making no request.
- [x] `src/PhotoPrint.UI/src/app/core/services/cart.service.spec.ts` — **updated**, 14 tests (5 new,
  1 extended). `applyCoupon` POSTs to `/cart/coupon` and publishes the recalculated cart; it
  persists for a guest and does not persist when authenticated; `clearCoupon` DELETEs and publishes
  the cleared cart; a restored guest snapshot carrying a `couponCode` re-reads `GET /api/cart` and
  surfaces the stale status. The pre-existing legacy-shape restore now also asserts the totals fall
  back to `subtotal` with a 0 discount and no refresh call.
- [x] `src/PhotoPrint.UI/src/app/features/checkout/pages/review-step.spec.ts` — **updated**, 9 tests
  (3 new). Percentage coupon → `grandTotal()` 244.99 with the discount row; free-shipping coupon →
  `shippingCost()` 0 and total 250.00, the number the order will actually charge; a stale
  free-shipping coupon still charges shipping, shows the warning, and clears it on `Elimină`.
- [x] `src/PhotoPrint.UI/src/app/features/orders/pages/confirmation-page.spec.ts` — **updated**,
  22 tests (2 new). A discounted order renders `.summary-row--discount` with `VARA10` and `25.00`;
  an undiscounted order renders no such row.
- [x] `src/PhotoPrint.Tests/Unit/Services/Invoicing/InvoiceDiscountMathTests.cs` — **new**, 9 facts.
  No discount → no rows. Percent coupon → `Total linii (fără TVA): 226.88` and
  `Reducere VARA10: -21.01`; the second row is the **net** allowance, not the gross 25.00 discount,
  and the two rows sum to `invoice.NetTotalRon`. The key fact builds the real UBL through
  `InvoiceXmlBuilder` and asserts the PDF rows equal `LineExtensionAmount`,
  `AllowanceTotalAmount`, `TaxExclusiveAmount` and `PayableAmount` — the PDF and the file sent to
  ANAF cannot drift apart. Plus the generic allowance wording for a blank code, `LineNetTotal`
  without a discount, `VatRateFromInvoice` deriving ~0.19 from a detached invoice, and a zero-net
  invoice returning 0 instead of dividing by zero.
- [x] `src/PhotoPrint.Tests/Unit/Services/Invoicing/InvoicePdfRendererTests.cs` — **updated**, 1 new
  fact: a discounted order still renders a valid PDF and its totals block gains the two allowance
  rows.

### Acceptance Criteria Validation

- ✅ **Cart page has a `Cod promo` input + `Aplică` button** — `cart-page.spec.ts` drives
  `.coupon-box__input` / `.coupon-box__apply` in every test.
- ✅ **On success a `Reducere: -X,XX` row appears and the total reflects it** — discount row
  `25.00` with the code, total `225.00` (cart), 244.99 (review).
- ✅ **`INVALID_COUPON`, `MIN_SUBTOTAL_NOT_MET`, `COUPON_EXHAUSTED` render their Romanian sentence,
  chosen by `code`** — one test each in `cart-page.spec.ts`, plus the map-vs-detail precedence in
  `coupon-messages.spec.ts`.
- ✅ **The coupon is shown on the review step and on the confirmation page** —
  `review-step.spec.ts` discount row, `confirmation-page.spec.ts` `.summary-row--discount`.
- ✅ **A free-shipping coupon shows the total the order will actually charge** —
  `shippingCost()` 0 and `grandTotal()` 250.00 with the `.free-shipping-note`.
- ✅ **A stale coupon is visible with a working `Elimină` on cart and review** — cart: warning →
  DELETE → input returns; review: shipping still charged, warning cleared on click.
- ✅ **The invoice PDF renders the `Reducere` line with numbers equal to the UBL allowance** —
  `DiscountRows_MatchTheAllowanceNumbersFiledWithAnaf` cross-checks the real XML output.
- ✅ **Scoped tests green** — 64/64 UI, 154/154 API (counts above).
- ✅ **No file from the wave's "do not touch" list is modified** — `git diff origin/main...HEAD
  --name-only` contains no `Directory.Packages.props`, no `.csproj`, and nothing under
  `home-page.*`, `saved-addresses`, `profile` or `delivery-step`.

### Issues Found

- The five coupon tests appended to `cart.service.spec.ts` in the implement stage sat **after** the
  `describe` block's closing brace, so the file did not compile (`Cannot find name 'isAuthSubject'`).
  Moved inside the block; nothing about the tests themselves changed.
- No product defect was found by this stage. Every new test passed on first execution once the file
  above compiled, which is weak evidence — the specs were written against the implementation, so
  they confirm the behaviour is pinned, not that it was independently discovered.

### Notes

- `cart-page.spec.ts` deliberately uses the real `CartService` with `HttpTestingController` rather
  than a stubbed service: a stub would have proved the template renders whatever it is handed, not
  that apply/remove round-trips through the service and localStorage.
- Money literals are en-US (`225.00`, not `225,00`) because the app registers no `LOCALE_ID`, so
  `DecimalPipe` formats with the default locale in tests and in the browser alike.
- The plan listed a detached-invoice case for `InvoiceXmlBuilderDiscountTests`; it lives in
  `InvoiceDiscountMathTests.VatRateFromInvoice_DetachedInvoice_DerivesTheRateFromItsOwnTotals`
  instead, because the lifted helper — not the XML builder, which is always handed its order — is
  what the PDF path calls without an `Order` navigation.
- `payment-step.ts` still shows generic 409 copy that hides coupon conflicts; untouched here and
  re-deferred with the parked cluster, as the plan recorded.

## Fresh-eyes micro-review

Stage-4 gate, run 2026-09-04 over the 048 diff (`f9f0e15^..HEAD`, scope list in
`micro-review-scope.txt`) by two fresh-context subagents — one on the Angular side, one on the
invoice PDF/UBL side. 28 findings: 3 high, 13 medium, 12 low. 14 fixed with proving tests,
12 recorded, 2 rejected.

### Angular side (14)

| # | Where | Sev | Finding | Disposition |
|---|---|---|---|---|
| A1 | `coupon-messages.ts:8` | HIGH | `ORDER_TOTAL_BELOW_MINIMUM` and `NO_DISCOUNT` copy is dead: the coupon endpoints never emit them (the server sends them as a 409 at intent creation) and the only 409 handler is `payment-step.ts`'s generic sentence. | Recorded — the parked PPW-687…690 / PPW-705 cluster, owner ruling 2026-09-03. |
| A2 | `coupon-messages.ts:35` | MEDIUM | A transport failure (`status 0`) or a 500 fell through to the code map and blamed the code for a service outage. | Fixed — `UNAVAILABLE_MESSAGE` for `status === 0 \|\| status >= 500`; two facts in `coupon-messages.spec.ts`. |
| A3 | `coupon-messages.ts:41` | LOW | Preferring the server sentence for `MIN_SUBTOTAL_NOT_MET` trusts server text for customer-facing copy. | Recorded — deliberate: it is the only sentence carrying the RON threshold, and the server writes it in Romanian. |
| A4 | `coupon-messages.ts:43` | MEDIUM | `return detail ?? DEFAULT_MESSAGE` rendered raw server text for any unmapped code, so a non-coupon failure (Kestrel's `BadHttpRequestException`, `exception.Message` in Development) reached the customer. | Fixed — the fallback is now `couponMessageFor(code)`; the old "falls back to the server detail" test is inverted. |
| A5 | `cart.service.ts:134` | MEDIUM | The coupon-refresh path called `loadFromServer()`, whose error branch publishes `EMPTY_CART`, so an offline guest lost the cart he had just restored. | Fixed — new `refreshRestoredCart()` updates only on success; proving test in `cart.service.spec.ts`. |
| A6 | `cart.service.spec.ts:248` | LOW | `TestBed.resetTestingModule()` mid-test leaves the fresh `HttpTestingController` outside the shared `afterEach` verification. | Rejected — that test calls `freshHttp.verify()` itself at its end, and the new failure test does the same. |
| A7 | `cart.model.ts:49` | LOW | `CouponKind \| string` / `CouponStatus \| string` collapse to `string`, so the new unions type-check nothing. | Recorded — the `\| string` arm is what stops an unrecognised server value from breaking the parse; the helpers compare literals, so behaviour is right. Worth a typed-narrowing pass later. |
| A8 | `cart-page.ts:129` | LOW | Coupon error and stale-warning paragraphs carry no `role="alert"`, so a screen reader never announces them. | Fixed — `role="alert"` on both paragraphs in `cart-page.ts` and on `.coupon-warning` / `.coupon-error` in `review-step.ts`. |
| A9 | `cart-page.ts:512` | LOW | `couponError` is cleared only when a new coupon call starts, so a rejection sentence stays pinned after the cart changes. | Recorded — stale copy only, no wrong money; needs a cart-change subscription to clear. |
| A10 | `confirmation-page.ts:52` | LOW | The `Reducere` row was emitted after `Total plătit`, so the discount read as if it applied after the total. | Fixed — the row now precedes the total. |
| A11 | `order.model.ts:33` | MEDIUM | `OrderDetailDto` lacked `couponCode`/`discountRon` although the API record carries them, so the order page printed Subtotal + Transport and a Total that does not add up. | Fixed — fields added, discount row rendered in `order-detail-page.ts`, two facts in its spec. **All three files sit outside the 048 diff.** |
| A12 | `review-step.spec.ts:76` | MEDIUM | The `CartService` stub only ever succeeds, so `removeCoupon`'s error branch, `.coupon-error` and the `couponPending` reset were unreachable by any test. | Fixed for `review-step` (failing-`clearCoupon` test via `TestBed.overrideProvider`); the same gap in `cart-page.spec.ts` is recorded. |
| A13 | `review-step.ts:266` | MEDIUM | `shippingCost()` re-derives a server-owned number for a free-shipping coupon. | Recorded — deliberate plan decision: the cart total excludes shipping, so the display line has to be derived; the existing test pins it. |
| A14 | `review-step.ts:294` | HIGH | `proceed()` navigated to payment with a known-stale coupon (`couponStale()` only warned), and order creation then 409s with no way out. | Fixed — guard in `proceed()`, `[disabled]` on the pay button, proving test. |

### Invoice PDF and UBL side (14)

| # | Where | Sev | Finding | Disposition |
|---|---|---|---|---|
| I1 | `InvoiceXmlBuilder.cs:197` | HIGH | `AllowanceTotalAmount` was emitted between `TaxExclusiveAmount` and `TaxInclusiveAmount`, but `cac:LegalMonetaryTotal` is an `xsd:sequence` where BT-112 precedes BT-107 — every discounted invoice was schema-invalid for ANAF. | Fixed — element moved after `TaxInclusiveAmount`, `PayableAmount` last, plus `LegalMonetaryTotal_KeepsTheUblSequenceOrderWhenAnAllowanceIsPresent`. **This branch came from bolt 047, in a file 048 touched.** |
| I2 | `InvoicePdfRendererTests.cs:126` | MEDIUM | The PDF test's only feature assertion calls `InvoiceDiscountMath.DiscountRows` itself and never inspects the rendered document, so deleting the `foreach` in `ComposeTotals` leaves it green. | Recorded — QuestPDF output is not text-assertable without a PDF text extractor; the math it restates is now pinned by six new facts. |
| I3 | `InvoiceDiscountMath.cs:9` | MEDIUM | `VatRateFromInvoice` returns `order.VatRate` when the navigation is loaded and a rate derived from rounded totals when it is not, so the same invoice can yield different `LineExtensionAmount`/`AllowanceTotalAmount`/`cbc:Percent`. | Recorded — pre-existing logic moved verbatim by the lift; the new detached-vs-attached equality fact pins today's numbers. Making the invoice always read `Order.VatRate` is an owner call. |
| I4 | `InvoicePdfDocument.cs:164` | MEDIUM | "Total linii (fără TVA): 226,88" sits directly above a line table printing VAT-inclusive amounts under unqualified headers, so the customer's rows sum to 269,99 and no visible arithmetic on the page reconciles. | Recorded — needs an owner call on the invoice's basis (net columns vs a gross-labelled total row). |
| I5 | `InvoiceDiscountMathTests.cs:182` | MEDIUM | The detached test asserted only `BeApproximately(0.19, 0.001)`, which passes at 0.1898, and nothing compared `LineNetTotal`/`DiscountRows` attached vs detached. | Fixed — `DiscountRows_DetachedInvoice_MatchTheAttachedOnesToTheCent`. |
| I6 | `InvoicePdfDocument.cs:169` | MEDIUM | The invoice prints the net allowance (−21,01) with no wording marking it net, while cart, review step, confirmation page and `OrderEmailService.cs:41` all print the gross −25,00. | Recorded — same owner call as I4. |
| I7 | `InvoiceDiscountMathTests.cs:31` | MEDIUM | The fixture models only a Percent-shaped discount; no case covers `FreeShipping`, where `DiscountRon == ShippingCostRon` while `ShippingCostRon` stays at full value. | Fixed — `DiscountRows_FreeShippingCoupon_ReconcileWithTheInvoiceNet`. `Fixed`-type coupons land on the same arm as Percent (both are a gross discount on goods), so they need no separate fact. |
| I8 | `InvoiceDiscountMathTests.cs:186` | MEDIUM | Nothing drove `DiscountRows`/`LineNetTotal` at a discount equal to or above the whole payable; the zero-net path also emits `TaxCategory` `S` with `cbc:Percent` 0.00, not a valid CIUS-RO pair. | Fixed for the math — `DiscountRows_DiscountSwallowingTheWholePayable_StillReconcileToZero`. The CIUS-RO pair is recorded: unreachable while `OrderService` rejects a total below `MinimumChargeRon`. |
| I9 | `InvoiceDiscountMath.cs:17` | MEDIUM | The one rounding decision the class owns (4 dp, `AwayFromZero`) had no midpoint case: switching it to `ToEven` left all nine facts green. | Fixed — `VatRateFromInvoice_DerivedRateOnAHalfBasisPoint_RoundsAwayFromZero` (0.18985 → 0.1899; `ToEven` gives 0.1898). |
| I10 | `InvoiceDiscountMathTests.cs:158` | LOW | `AllowanceReason(null)` is tested in isolation, but no test drove `DiscountRows` with a discount and a null coupon code, so the generic row label was unexercised. | Fixed — `DiscountRows_WithADiscountButNoCouponCode_UseTheGenericLabel`. |
| I11 | `InvoiceXmlBuilder.cs:219` | LOW | The lift inverted strip-vs-blank-check order, so a coupon code of non-whitespace XML-illegal characters now yields the generic reason instead of `"Reducere "`. | Recorded — the new order is the better behaviour and is unreachable for validated codes; this corrects the earlier "XML output unchanged" claim, which held for the totals block, not for that branch. |
| I12 | `InvoiceDiscountMath.cs:39` | LOW | `DiscountRows` computes `LineNetTotal` twice and the XML builder three more times per document; no single computed breakdown is exposed. | Rejected — the computation is idempotent, so no drift is possible today. |
| I13 | `InvoicePdfDocument.cs:174` | LOW | The totals block prints two differently-based figures both labelled net: "Total linii (fără TVA): 226,88" and "Total net: 205,87". | Recorded — same owner call as I4. |
| I14 | `InvoicePdfDocument.cs:179` | LOW | The "TVA (x%)" label takes its rate from `_order.VatRate` while the rows above take theirs from `InvoiceDiscountMath`, mixing two rate sources in one block. | Recorded — same root cause as I3. |

Two questions are left for the coordinator, both about the printed invoice rather than the money
charged: whether the PDF's line table should switch to net columns (I4/I6/I13) and whether the
invoice should always read `Order.VatRate` instead of deriving it (I3/I14).
