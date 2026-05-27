# US-402 — Order Detail Page (Frontend)

## Story
**As a** customer  
**I want to** see full details of one order and track its delivery

## Type
FRONTEND — Angular

## Epic
EPIC-4 | Istoricul Comenzilor & Tracking

## Dependencies
- US-403 (Orders API backend)
- US-307 (Status stepper component — reuse)

## Acceptance Criteria

1. **`/comanda/{orderId}`** — all items: thumbnail, format, finish, quantity, unit price, line total
2. **Delivery**: locker name + address or home address
3. **Status stepper** (same 4 steps as confirmation page) with current step highlighted
4. **AWB number** + `Urmărește coletul` button (opens Sameday tracking in new tab) — shown after shipping
5. **`Contactează-ne`** mailto link with order number pre-filled in subject
6. **Guest access**: `/comanda/{orderId}?email={email}` — shows order if email matches

## Technical Notes

### Component Location
`src/app/features/orders/order-detail/order-detail.component.ts`

### Implementation Details
- Route: `/comanda/:orderId` with optional `?email=` query param
- For logged-in users: call `GET /api/orders/{orderId}` with Bearer token
- For guests: call `GET /api/orders/{orderId}?email={email}` (no auth header needed)
- Reuse `StatusStepperComponent` from US-307
- AWB tracking: `window.open(order.trackingUrl, '_blank')` — only show button if `awbNumber` is set
- Contact mailto: `mailto:contact@fototipar.ro?subject=Comanda ${order.orderNumber}`
- 403/404 handling: redirect to home or show error page

### UI/UX
- Item list: table on desktop, cards on mobile
- Status stepper at top
- Delivery section with address or locker details
- Payment summary at bottom
- All text in Romanian

## Files to Create/Modify
- `src/app/features/orders/order-detail/order-detail.component.ts`
- `src/app/features/orders/order-detail/order-detail.component.html`
- `src/app/features/orders/order-detail/order-detail.component.scss`

## Testing
- Unit test: order details display
- Unit test: AWB tracking button visibility
- Unit test: guest access with email param
- Unit test: 403 redirect
- E2E: view order detail
