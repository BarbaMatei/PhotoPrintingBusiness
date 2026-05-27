# US-307 — Order Confirmation Page (Frontend)

## Story
**As a** customer  
**I want to** see a clear confirmation that my order was placed successfully

## Type
FRONTEND — Angular

## Epic
EPIC-3 | Checkout & Plată

## Dependencies
- US-305/US-306 (Payment must be processed)
- US-403 (Orders API for fetching order data)

## Acceptance Criteria

1. **`/comanda/{orderId}/confirmare`** — shows: order number, photo count, format, total paid, delivery address/locker
2. **Visual status stepper**: `Comandă primită` → `În pregătire` → `Expediată` → `Livrată`
3. **Estimated delivery date** shown
4. **For guests**: `Vrei să-ți salvezi comanda? Creează un cont gratuit` CTA with pre-filled email
5. **For logged-in users**: `Vezi istoricul comenzilor` link
6. **Page rejects access** if orderId not in `Paid` status (redirect to home)

## Technical Notes

### Component Location
`src/app/features/checkout/confirmation/confirmation.component.ts`

### Implementation Details
- Route: `/comanda/:orderId/confirmare`
- On init: call `GET /api/orders/{orderId}` to fetch order details
- Guard: if order status is not `Paid` or higher, redirect to home
- Handle `?processor=euplatesc` query param: same page, different entry point
- Status stepper: reusable component showing 4 stages with current step highlighted
- Guest detection: check if current auth is guest token
  - Show register CTA with pre-filled email from guest session
  - After registration + claim, show `Vezi istoricul comenzilor`
- Clear cart after successful order display
- Clear checkout state

### UI/UX
- Success checkmark animation
- Order summary card with key details
- Stepper with icons for each stage
- Estimated delivery: `Livrare estimată: {date range}`
- All text in Romanian

## Files to Create/Modify
- `src/app/features/checkout/confirmation/confirmation.component.ts`
- `src/app/features/checkout/confirmation/confirmation.component.html`
- `src/app/features/checkout/confirmation/confirmation.component.scss`
- `src/app/shared/components/status-stepper/status-stepper.component.ts`

## Testing
- Unit test: order data display
- Unit test: status stepper current step
- Unit test: guest CTA vs logged-in link
- Unit test: redirect when order not paid
- E2E: confirmation page after Stripe payment
