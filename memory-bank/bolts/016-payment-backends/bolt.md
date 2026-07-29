---
id: 016-payment-backends
unit: 004-payment-backends
intent: 004-checkout-payment
type: ddd
status: complete
started: 2026-05-21T23:30:00Z
completed: 2026-05-22T00:00:00Z
current_stage: done
stages_completed: [design, implement, test]
stories:
  - 001-order-service
  - 002-stripe-payment-intent
  - 003-stripe-webhook-handler
  - 004-euplatesc-initiate
  - 005-euplatesc-ipn-handler
created: 2026-05-21T23:30:00Z

requires_bolts: ["015-shipping-and-order-core", "013-cart-api", "003-email-infrastructure"]
enables_bolts: ["017-checkout-ui"]
---

## Bolt: 016-payment-backends

### Summary
Payment backend layer: `IOrderService` (creates orders from cart), Stripe PaymentIntent
creation + webhook handler, EuPlatesc redirect initiation + IPN handler.
No Angular UI — that is bolt 017.

### What This Bolt Builds

**IOrderService / OrderService**:
- `CreateFromCartAsync(userId?, guestId?, CreateOrderRequest, ct)` → `Order`
- `GetByPaymentIntentIdAsync(paymentIntentId, ct)` → `Order?`
- `GetByIdAsync(orderId, ct)` → `Order?`
- Loads CartItems with product+tiers, computes unit prices from PricingTiers, creates
  Order + OrderItems with snapshots, generates OrderNumber, saves to DB.

**IStripePaymentGateway / StripePaymentGateway**:
- Wraps `Stripe.PaymentIntentService` (accepts `Stripe.IStripeClient`)
- `CreatePaymentIntentAsync(amountBani, currency, orderId, ct)` → `(ClientSecret, PaymentIntentId)`

**IStripeSignatureVerifier / StripeSignatureVerifier**:
- Wraps `Stripe.EventUtility.ConstructEvent` so it is mockable in tests

**IEuPlatescService / EuPlatescService**:
- `BuildInitiateUrlAsync(order, ct)` → `string` (redirect URL with HMAC params)
- Static: `ComputeHmac(hexKey, fields[])` → hex string (HMAC-MD5 per EuPlatesc v3 spec)
- Static: `ValidateIpnSignature(fields, hexKey)` → bool
- Static: `BuildIpnResponse(hexKey)` → `<epayment>date|hmac</epayment>`

**PaymentsController** (`api/payments`):
- `POST /api/payments/stripe/intent` (DualAuth) → `{ clientSecret, orderId }`
- `POST /api/payments/euplatesc/initiate` (DualAuth) → `{ redirectUrl, orderId }`

**WebhooksController** (`api/webhooks`):
- `POST /api/webhooks/stripe` (AllowAnonymous, reads raw body) → Stripe event processing
- `POST /api/webhooks/euplatesc` (AllowAnonymous, form-encoded) → EuPlatesc IPN processing

### Key Technical Notes
- Stripe: `StripeClient` registered as `Stripe.IStripeClient` singleton; override in tests with `FakeStripePaymentGateway`
- EuPlatesc HMAC: `HMAC-MD5(keyBytes, concatMessage)` where message = `len(field)field` per field
- Webhook raw body: read via `StreamReader(Request.Body)` before any middleware consumption
- Email fire-and-forget: `_ = _emailService.SendTemplatedAsync(...)` (not awaited)
- InMemory DB guard: `OrderService` checks provider for sequence fallback (same pattern as OrderNumberService)
