---
stage: implement
bolt: 048-coupon-frontend
created: 2026-09-04T15:55:00Z
---

## Implementation Walkthrough: 002-coupon-frontend

### Summary

The SPA can now apply and remove a promo code from the cart and shows the resulting
discount everywhere money is displayed: cart summary, checkout review step, and the
order confirmation page. The invoice PDF gained the two discount rows the e-Factura
XML already carried, both now fed by one shared calculation so the PDF and the XML
can never disagree.

### Structure Overview

The cart contract in `core/models` grew the coupon and total fields the API returns,
plus small reader helpers (total, discount, free-shipping, stale) that every page uses
instead of re-deriving money. All Romanian coupon copy lives in one message map keyed
by the server's error codes, so a component never writes its own error sentence.
`CartService` owns the two new coupon calls and is the only writer of cart state and
of the guest snapshot. On the backend, the discount math that used to be private to
the UBL XML builder moved into a shared static class in `Services/Invoicing`, consumed
by the XML builder and by the PDF document.

### Completed Work

- [x] `src/PhotoPrint.UI/src/app/core/models/cart.model.ts` - cart contract with the coupon and total fields, an empty-cart default, and the total/discount/free-shipping/stale readers.
- [x] `src/PhotoPrint.UI/src/app/core/models/coupon-messages.ts` - the Romanian copy map keyed by server error code, plus the rate-limit and fallback sentences and the error-to-message resolver.
- [x] `src/PhotoPrint.UI/src/app/core/services/cart.service.ts` - apply-coupon and clear-coupon calls, shared state acceptance for both, and a server refresh when a restored guest snapshot carries a code.
- [x] `src/PhotoPrint.UI/src/app/features/cart/pages/cart-page.ts` - the promo-code box (input, apply, applied state with remove, error and stale warning) and the subtotal / discount / shipping / total summary.
- [x] `src/PhotoPrint.UI/src/app/features/checkout/pages/review-step.ts` - discount row, free-shipping shipping and total handling, and the stale-code warning with a remove action.
- [x] `src/PhotoPrint.UI/src/app/features/orders/pages/confirmation-page.ts` - discount row above the VAT line when the paid order carried a code.
- [x] `src/PhotoPrint.UI/src/app/core/models/payment.model.ts` - order payment status gained the coupon code and discount the API sends.
- [x] `src/PhotoPrint.UI/src/app/features/orders/pages/confirmation-page.spec.ts` - the existing order fixture fills the two new fields so it still matches the contract.
- [x] `src/PhotoPrint.API/Services/Invoicing/InvoiceDiscountMath.cs` - the shared invoice discount math: VAT rate, pre-discount line net total, allowance amount and reason, and the PDF's discount rows.
- [x] `src/PhotoPrint.API/Services/Invoicing/InvoiceXmlBuilder.cs` - now consumes the shared math instead of its own private copies; XML output unchanged.
- [x] `src/PhotoPrint.API/Services/Invoicing/InvoicePdfDocument.cs` - renders the shared discount rows above the net total.

### Key Decisions

- **Copy comes from a map, not from the server**: the map is keyed by the server's error codes and mirrors its sentences; only the minimum-subtotal case uses the server's `detail`, because that sentence carries the actual threshold.
- **The SPA never recomputes the discount**: pages read the server's totals and only add shipping, or zero it for a free-shipping code, so discount-then-VAT stays a server-side decision.
- **One shared invoice math class**: the allowance amount, reason and VAT rate had to be identical in the PDF and the XML; duplicating them was the standing risk, so the XML builder's private copies became the shared source and the PDF calls the same functions.
- **PDF rows come from a pure label/amount function**: the document keeps the Romanian number formatting in one place and only renders what the function returns, which makes the rows assertable without rendering a PDF.
- **A restored guest cart with a code refreshes from the server**: a stored snapshot can be stale (code exhausted or expired meanwhile), and the server is the only judge of that.
- **Order payment status fields are required, not optional**: the API always sends both, and the one existing fixture was updated rather than weakening the contract.

### Deviations from Plan

The plan's shared class was scoped to the VAT rate, line net total and allowance
amount/reason; the PDF's discount-row function landed there too rather than in the PDF
document, so both the rows and the amounts they show are testable in one place.
Deliverable 9 (spec files) is stage 3 work and was not started.

### Dependencies Added

None.

### Developer Notes

- The UI worktree had no `node_modules`; `npm ci` was run to build. Nothing in `package.json` or the lock file changed.
- Prettier reports style issues across the whole SPA, untouched files included, so no formatting pass was run — reformatting only the changed files would have mixed unrelated churn into this bolt.
- The checkout 409 dead-end in `payment-step.ts` (a coupon going stale between review and payment) is deliberately untouched: it belongs to a parked row and stays parked by owner ruling.
- Verified: `dotnet build src/PhotoPrint.API/PhotoPrint.API.csproj` — 0 errors; `npm run build` in `src/PhotoPrint.UI` — bundle generated, 0 errors. No test suite was run in this stage.
