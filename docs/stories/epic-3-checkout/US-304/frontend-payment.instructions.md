# US-304 — Payment — Card Payment Step (Frontend)

## Story
**As a** customer  
**I want to** pay by card (Stripe) and complete the payment

## Type
FRONTEND — Angular

## Epic
EPIC-3 | Checkout & Plată

## Dependencies
- US-305 (Stripe backend)
- US-303 (Review step completed)

## Acceptance Criteria

1. **Payment step** shows the embedded card form directly — no processor selection UI
2. **Card form**: embedded Stripe Elements (card number, expiry, CVC, name) — no redirect
3. **Success**: redirect to `/comanda/{orderId}/confirmare`
4. **Failure**: inline error in Romanian; retry available without losing cart

## Technical Notes

### Component Location
`src/app/features/checkout/payment-step/payment-step.component.ts`

### Implementation Details
- **Stripe flow**:
  1. Call `POST /api/payments/stripe/intent` → receive `{clientSecret, orderId}`
  2. Initialize Stripe Elements with `clientSecret`
  3. On submit: `stripe.confirmCardPayment(clientSecret, { payment_method: { card: elements } })`
  4. On success: navigate to `/comanda/{orderId}/confirmare`
  5. On error: show error message inline
  - Install `@stripe/stripe-js` npm package
  - Load Stripe with publishable key from `environment.ts`

- Error handling: show Romanian error messages; allow retry (Stripe Elements remain mounted)
- Loading states: spinner during API calls and payment processing

### Environment Config
- `environment.ts`: `stripePublishableKey: 'pk_test_...'`

## Files to Create/Modify
- `src/app/features/checkout/payment-step/payment-step.component.ts`
- `src/app/features/checkout/payment-step/payment-step.component.html`
- `src/app/features/checkout/payment-step/payment-step.component.scss`
- `src/app/features/checkout/stripe-form/stripe-form.component.ts`
- `src/app/core/services/payment.service.ts`

## Testing
- Unit test: Stripe Elements initialization
- Unit test: error display on payment failure
- E2E: Stripe payment flow (with test card)
