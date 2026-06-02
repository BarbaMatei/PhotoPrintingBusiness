---
id: 005-payment-step
unit: 005-checkout-ui
intent: 004-checkout-payment
status: complete
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 017-checkout-ui
implemented: true
---

# Story: 005-payment-step

## User Story

**As a** customer
**I want** to pay with either an international card (Stripe) or a Romanian card (EuPlatesc)
**So that** I can complete my purchase using whichever payment method works for my bank

## Acceptance Criteria

- [ ] **Given** Step 3 (`/checkout/plata`) is loaded, **When** rendered, **Then** two tabs are shown: `Card internațional (Stripe)` and `Card românesc (EuPlatesc)`
- [ ] **Given** the Stripe tab is active, **When** the user sees it, **Then** `POST /api/payments/stripe/intent` is called and Stripe Elements mounts the embedded card form using the returned `clientSecret`
- [ ] **Given** the Stripe card form is filled and `Plătește` is clicked, **When** `stripe.confirmCardPayment(clientSecret)` succeeds, **Then** the user is routed to `/comanda/{orderId}/confirmare`
- [ ] **Given** `stripe.confirmCardPayment` returns a Stripe error, **When** displayed, **Then** the error message is shown inline below the card form (in Romanian if possible)
- [ ] **Given** the EuPlatesc tab is active and `Plătește cu EuPlatesc` is clicked, **When** `POST /api/payments/euplatesc/initiate` responds, **Then** `window.location.href` is set to the `redirectUrl`
- [ ] **Given** the `Plătește` button is clicked, **When** the API call is in progress, **Then** the button shows a spinner and is disabled to prevent double-submission

## Technical Notes

- `loadStripe(publishableKey)` from `@stripe/stripe-js` (lazy import); `publishableKey` from Angular `environment.stripePublishableKey`
- Stripe Elements: `stripe.elements({ clientSecret })` → `elements.create('payment')` → `element.mount('#stripe-payment-element')`
- `stripeClientSecret` stored in `CheckoutStateService` after `POST /api/payments/stripe/intent` — do not call again on tab switch
- EuPlatesc tab: no form — just a button and logo; call `POST /api/payments/euplatesc/initiate` on click
- On `confirmCardPayment` success: route to `/comanda/{orderId}/confirmare?processor=stripe`
- EuPlatesc return URL (configured on EuPlatesc side): `/comanda/{orderId}/confirmare?processor=euplatesc`

## Dependencies

### Requires
- Story 001-checkout-stepper (CheckoutStateService)
- Bolt 016 (payment-backends — `POST /api/payments/stripe/intent` + `POST /api/payments/euplatesc/initiate`)
- `@stripe/stripe-js` npm package

### Enables
- Story 006-order-confirmation-page (routed to after successful payment)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Stripe API down (intent creation fails) | Error toast: `"Plata nu a putut fi inițiată. Încearcă din nou."` |
| User switches tabs (Stripe → EuPlatesc) | EuPlatesc initiation creates a NEW order (Stripe order abandoned in AwaitingPayment) |
| Stripe 3DS challenge appears | Stripe.js handles 3DS natively in the embedded element |
| EuPlatesc redirect fails (network) | Browser shows network error; user returns via back button to checkout |
| `confirmCardPayment` error code `card_declined` | Show: `"Cardul a fost refuzat. Verificați datele sau folosiți alt card."` |

## Out of Scope

- Saving card for future use
- Apple Pay / Google Pay (future enhancement)
- Split payment (partial Stripe + partial EuPlatesc)
