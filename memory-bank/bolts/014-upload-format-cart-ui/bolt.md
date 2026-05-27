---
id: 014-upload-format-cart-ui
unit: 002-upload-format-cart-ui
intent: 004-checkout-payment
type: simple-construction-bolt
status: completed
started: 2026-05-21T20:30:00Z
completed: 2026-05-22T00:00:00Z
current_stage: done
stages_completed: [design, implement, test]
stories:
  - 001-upload-page
  - 002-format-finish-selector
  - 003-order-summary-panel
  - 004-cart-page
  - 005-cart-service
created: 2026-05-21T20:30:00Z

requires_bolts: ["013-cart-api", "011-product-catalog-ui", "012-photo-upload-backend", "004-angular-app-shell"]
enables_bolts: ["017-checkout-ui"]
---

## Bolt: 014-upload-format-cart-ui

### Summary
Angular frontend for photo upload, format/finish selection, and cart management. Integrates with
the cart API (bolt 013) and upload API (bolt 012).

### Existing structure (pre-existing work)
- `/tipareste` → catalog-page (product list)
- `/tipareste/:id` → format-selector-page (format/finish selection — partially built)
- CartService stub (itemCount$ only)
- CartPage stub

### What this bolt adds
1. `upload.model.ts`, `cart.model.ts` — API models
2. `UploadService` — POST /api/uploads with progress reporting
3. Full `CartService` — BehaviorSubject state, localStorage for guests, API sync for auth users, mergeOnLogin
4. `PhotoUploadComponent` — drag-and-drop upload zone with validation (type, size, count)
5. `PhotoThumbnailComponent` — preview with quality badge
6. `QuantityStepperComponent` — ± buttons with debounce
7. `OrderSummaryComponent` — sticky panel with live subtotal
8. `quality.utils.ts` — quality badge computation from pixel vs. physical dimensions
9. Enhanced format-selector-page — integrates upload + summary
10. Full cart-page implementation
