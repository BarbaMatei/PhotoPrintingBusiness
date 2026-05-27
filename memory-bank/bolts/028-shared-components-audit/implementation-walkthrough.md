---
stage: implement
bolt: 028-shared-components-audit
created: 2026-05-25T00:00:00Z
---

## Implementation Walkthrough: 028-shared-components-audit

### Summary

Audited all feature pages for inline text-based loading states and empty-state divs. Replaced qualifying patterns with `<app-spinner>` and `<app-empty-state>` from the shared component library. Skeleton loading UIs (admin-page, admin-products, admin-orders, pricing-page) were intentionally left untouched as they provide superior UX.

### Structure Overview

Five pages were updated — each received the relevant shared component imports and had their inline template blocks and now-dead CSS removed.

### Completed Work

- [x] `features/orders/pages/order-detail-page.ts` — added `SpinnerComponent` import; replaced `<p class="state-loading">` with `<app-spinner [showLabel]="true">`; removed `.state-loading` CSS (was hardcoded `#6c757d`)
- [x] `features/orders/pages/confirmation-page.ts` — added `SpinnerComponent` import; replaced `<div class="state-loading">` with `<app-spinner [showLabel]="true">`; removed `.state-loading` CSS (was hardcoded `#6c757d`)
- [x] `features/account/pages/profile/profile-page.ts` — added `SpinnerComponent` import; replaced `<p class="state-loading">` with `<app-spinner [showLabel]="true">`; removed `.state-loading` CSS (was hardcoded `#6b7280`)
- [x] `features/account/pages/saved-addresses/saved-addresses-page.ts` — added `SpinnerComponent` + `EmptyStateComponent` imports; replaced `<p class="state-loading">` with `<app-spinner [showLabel]="true">`; replaced inline empty-state div (with button) with `<app-empty-state (action)="openAddForm()">`; removed both dead CSS blocks
- [x] `features/admin/pages/order-detail/admin-order-detail-page.ts` — added `SpinnerComponent` import to `imports` array
- [x] `features/admin/pages/order-detail/admin-order-detail-page.html` — replaced `<div class="loading">Se încarcă...</div>` with `<app-spinner [showLabel]="true">`
- [x] `features/admin/pages/order-detail/admin-order-detail-page.scss` — removed `.loading` rule; preserved `.error-banner` with its full style block

### Key Decisions

- **`[showLabel]="true"` not `showLabel`**: Angular signal inputs typed as `boolean` require property binding syntax. The attribute shorthand `showLabel` is treated as a string `""` and causes a type error.
- **Skeleton screens excluded**: `admin-page`, `admin-products`, `admin-orders`, and `pricing-page` all implement CSS skeleton loading — this is intentionally better UX than a generic spinner and was preserved.
- **`<app-empty-state (action)="openAddForm()">`**: The saved-addresses empty state triggered a button click (not a navigation), so `actionLink` was not used. The `action` output wires to the existing `openAddForm()` method.
- **`.error-banner` padding preserved**: When the `.loading` + `.error-banner` shared rule was removed, the `.error-banner` padding/radius/font-size styles were re-inlined on the rule itself to avoid a regression.

### Deviations from Plan

None — all 5 in-scope pages updated as planned.

### Dependencies Added

None.

### Developer Notes

The `showLabel` signal input quirk is a common footgun when using Angular's new signal-based inputs with boolean attribute shorthand. Always use `[input]="true"` for boolean signal inputs.
