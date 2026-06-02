---
id: 002-format-finish-selector
unit: 002-upload-format-cart-ui
intent: 004-checkout-payment
status: complete
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 014-upload-format-cart-ui
implemented: true
---

# Story: 002-format-finish-selector

## User Story

**As a** customer
**I want** to choose a print format and finish that applies to all my photos at once
**So that** I can configure my entire order with a single selection before reviewing per-photo quantities

## Acceptance Criteria

- [ ] **Given** photos have been uploaded, **When** the format/finish selector is shown above the thumbnail grid, **Then** it displays a segmented control for format (`10×15`, `13×18`, `15×21`) and a toggle for finish (`Lucios`, `Mat`)
- [ ] **Given** a different format is selected, **When** the selection changes, **Then** quality badges for all visible thumbnails are immediately recalculated synchronously (no API call needed)
- [ ] **Given** the finish toggle is changed, **When** the product lookup resolves, **Then** the unit price in the summary panel updates to reflect the new product
- [ ] **Given** no photos are uploaded yet, **When** the selector is rendered, **Then** format and finish controls are visible but the `Adaugă în coș` CTA is disabled
- [ ] **Given** the page first loads, **When** no format is pre-selected, **Then** `10×15 / Lucios` is the default selection

## Technical Notes

- Segmented control: custom Angular standalone component with `@Input() options` and `@Output() selectionChange`
- Finish toggle: two-button toggle group (`Lucios` | `Mat`)
- Format/finish state managed in `UploadService` as `Signal<{ format: PrintFormat; finish: Finish }>`
- Product ID is derived from `(format, finish)` combination — 6 products total; looked up from `ProductService`
- Quality badge recalc: computed signal `derived(() => uploads().map(u => computeBadge(u, selectedFormat())))`
- Romanian labels: `Lucios` = Glossy, `Mat` = Matte; `10×15 cm`, `13×18 cm`, `15×21 cm`

## Dependencies

### Requires
- Story 001-upload-page (uploads must exist for badge recalc to be meaningful)
- Bolt 009/011 (product-catalog — product list with format/finish combos and pricing)

### Enables
- Story 003-order-summary-panel (needs selected format/finish + product price)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Product API returns no product for combination | Error toast; current selection retained |
| All photos are Red badge for selected format | No automatic warning shown; user can still proceed |
| Format changed while cart-add is in progress | Cart-add uses the format at time of button click |

## Out of Scope

- Per-photo format selection (one format applies to all)
- Saving format preference between sessions
