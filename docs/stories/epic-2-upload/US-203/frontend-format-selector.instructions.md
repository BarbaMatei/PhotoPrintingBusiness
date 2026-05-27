# US-203 — Global Format & Finish Selector (Frontend)

## Story
**As a** customer  
**I want to** choose one format and finish that applies to all uploaded photos, and set quantity per photo

## Type
FRONTEND — Angular

## Epic
EPIC-2 | Upload Fotografii & Selecție Format

## Dependencies
- US-201 (Photo upload must be done first)
- US-204 (Product catalogue API for format/finish data and prices)

## Acceptance Criteria

1. **Above the thumbnail grid**: prominent selector panel — Format (radio or segmented control: `10x15` / `13x18` / `15x21`) and Finish (toggle: `Lucios` / `Mat`)
2. **Changing format/finish** immediately updates ALL quality badges on all thumbnails
3. **Per-thumbnail quantity stepper** (min 1, max 100); default 1
4. **Order summary panel** (sticky right or bottom): subtotal per line, grand subtotal (excl. shipping), total photo count
5. **Price updates in real-time** with debounce 300ms on quantity changes
6. **`Adaugă în coș`** CTA button — disabled if no photos uploaded
7. **Format minimum resolution guide** shown below selector: e.g. `10x15 → min 1200×1800px`

## Technical Notes

### Component Location
`src/app/features/upload/format-selector/format-selector.component.ts`

### Implementation Details
- Load products from `GET /api/products` on component init; cache in service
- Format selector: segmented control or radio group with product names (10×15, 13×18, 15×21)
- Finish selector: toggle switch between `Lucios` and `Mat`
- When format/finish changes: find matching product → update quality badges on all thumbnails by comparing image dimensions to product's `minWidthPx`/`minHeightPx` and `optWidthPx`/`optHeightPx`
- Quantity stepper: `+`/`-` buttons with input field; debounce changes at 300ms before recalculating totals
- Price calculation: call `POST /api/products/calculate` with `[{productId, quantity}]` or calculate client-side (`quantity × priceRon`)
- Order summary: sticky panel showing each photo line (thumbnail, qty, line total) + grand subtotal
- `Adaugă în coș` button: creates/updates cart via `POST /api/cart`

### UI/UX
- Resolution guide: show minimum and optimal resolution for selected format
- Quality badge colors: Green (`≥ optimal`), Yellow (`≥ min but < optimal`), Red (`< min`)
- Responsive: summary panel below on mobile, sidebar on desktop
- All text in Romanian

## Files to Create/Modify
- `src/app/features/upload/format-selector/format-selector.component.ts`
- `src/app/features/upload/format-selector/format-selector.component.html`
- `src/app/features/upload/format-selector/format-selector.component.scss`
- `src/app/features/upload/order-summary/order-summary.component.ts`
- `src/app/core/services/product.service.ts`
- `src/app/core/models/product.model.ts`

## Testing
- Unit test: quality badge recalculation on format change
- Unit test: quantity stepper min/max enforcement
- Unit test: price calculation with debounce
- Unit test: add to cart disabled when no photos
- E2E: select format, change quantities, verify summary updates
