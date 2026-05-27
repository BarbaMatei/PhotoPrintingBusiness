# US-503 — Admin — Order Detail & Workflow (Frontend)

## Story
**As an** operator  
**I want to** see all photos I need to print for an order and move it through the workflow

## Type
FRONTEND — Angular

## Epic
EPIC-5 | Panou Admin

## Dependencies
- US-504 (Admin API)
- US-502 (Order queue — side panel integration)

## Acceptance Criteria

1. **Side panel or `/admin/comenzi/{id}`**: full item list, customer info, delivery info, payment total
2. **`Descarcă toate pozele`** button → downloads ZIP of all original files for this order
3. **Status workflow buttons** (shown based on current status): `[Marchează ca În imprimare]` → `[Marchează ca Expediată]` → `[Marchează ca Livrată]`
4. **`Marchează ca Expediată`** opens AWB input field: text input for AWB number (manual) OR `Generează AWB automat` (Sameday API, Phase 2)
5. **`Anulează comanda`** button with reason field → triggers refund confirmation dialog
6. **Internal notes** textarea (saved to order, not visible to customer)

## Technical Notes

### Component Location
`src/app/features/admin/order-detail-panel/order-detail-panel.component.ts`

### Implementation Details
- Side panel: slide-in from right; or separate route `/admin/comenzi/{id}`
- Load order via `GET /api/admin/orders/{id}`
- Download ZIP: `GET /api/admin/orders/{id}/download-zip` — trigger browser download
- Status workflow: show only the next valid transition button based on current status (Appendix D state machine)
  - Paid → show `Marchează ca În imprimare`
  - Printing → show `Marchează ca Expediată`
  - Shipped → show `Marchează ca Livrată`
- Shipped transition: show AWB input field before confirming; call `PATCH /api/admin/orders/{id}/status { status: 'Shipped', awbNumber: '...' }`
- Cancel: confirmation dialog with reason textarea; call `POST /api/admin/orders/{id}/cancel`
- Internal notes: autosave on blur or explicit save button; call `PATCH /api/admin/orders/{id}/notes`

### UI/UX
- Customer info section: name, email, phone
- Delivery section: Easybox locker name + address OR home address
- Items: photo thumbnails grid with quantity badges
- Status transition: prominent action buttons with confirmation
- Cancel: red button with warning dialog
- Notes: textarea at bottom, subtle save indicator

## Files to Create/Modify
- `src/app/features/admin/order-detail-panel/order-detail-panel.component.ts`
- `src/app/features/admin/order-detail-panel/order-detail-panel.component.html`
- `src/app/features/admin/order-detail-panel/order-detail-panel.component.scss`
- `src/app/features/admin/cancel-dialog/cancel-dialog.component.ts`
- `src/app/features/admin/awb-input-dialog/awb-input-dialog.component.ts`

## Testing
- Unit test: workflow buttons display based on status
- Unit test: ZIP download triggers
- Unit test: AWB input on ship transition
- Unit test: cancel dialog with reason
- Unit test: internal notes save
- E2E: move order through workflow
