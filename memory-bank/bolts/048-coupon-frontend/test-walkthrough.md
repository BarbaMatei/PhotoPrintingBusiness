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
