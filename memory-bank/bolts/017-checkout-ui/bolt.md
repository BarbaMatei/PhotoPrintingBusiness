---
id: 017-checkout-ui
unit: 005-checkout-ui
intent: 004-checkout-payment
type: simple
status: complete
started: 2026-05-22T00:00:00Z
completed: 2026-05-22T07:07:00Z
current_stage: done
stages_completed: [design, implement, test]
stories:
  - 001-checkout-stepper
  - 002-delivery-step
  - 003-locker-map-component
  - 004-order-review-step
  - 005-payment-step
  - 006-order-confirmation-page
---

## Summary

Angular checkout flow with 4 pages and supporting services/components.

## Deliverables

### Models
- `core/models/shipping.model.ts` — LockerDto, ShippingCostDto, DeliveryType, ShippingAddressForm, DeliveryState, ROMANIAN_COUNTIES
- `core/models/payment.model.ts` — CreateOrderRequest, StripeIntentResponse, EuPlatescInitiateResponse, OrderDto, OrderStatus

### Services
- `core/services/shipping.service.ts` — getLockers, getShippingCost
- `core/services/payment.service.ts` — createStripeIntent, initiateEuPlatesc, getOrder
- `core/services/checkout-state.service.ts` — BehaviorSubject with sessionStorage persistence

### Components / Pages
- `features/checkout/components/checkout-shell.ts` — 3-step stepper with router outlet
- `features/checkout/components/locker-map.ts` — Leaflet map (lazy-loaded) with locker pins
- `features/checkout/pages/delivery-step.ts` — Easybox/Courier selection, city search, address form
- `features/checkout/pages/review-step.ts` — Cart summary, delivery info, terms checkbox
- `features/checkout/pages/payment-step.ts` — Stripe Elements + EuPlatesc tabs
- `features/orders/pages/confirmation-page.ts` — Order success page with status stepper

### Routes
- `checkout.routes.ts` — shell with livrare/recapitulare/plata child routes
- `app.routes.ts` — added `comanda/:orderId/confirmare` route

## Test Results
- 277/277 Angular tests passing (32 test files)
- 9 new delivery-step tests, 5 review-step tests, 4 payment-step tests, 6 confirmation-page tests

## Technical Notes
- Vitest (not Jasmine): use `vi.fn()`, no `fakeAsync`/`tick`
- Leaflet and @stripe/stripe-js are lazy-loaded via dynamic import()
- Angular 21 control flow: `@if`, `@for` (not `*ngIf`, `*ngFor`)
- Signal inputs: `fixture.componentRef.setInput('name', value)` before detectChanges
