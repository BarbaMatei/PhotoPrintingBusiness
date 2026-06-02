---
id: 002-stripe-payment-intent
unit: 004-payment-backends
intent: 004-checkout-payment
status: complete
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 016-payment-backends
implemented: true
---

# Story: 002-stripe-payment-intent

## User Story

**As a** customer choosing to pay by card
**I want** to initiate a Stripe payment from the checkout page
**So that** I can enter my card details securely in the embedded Stripe Elements form

## Acceptance Criteria

- [ ] **Given** a valid cart and delivery selection, **When** `POST /api/payments/stripe/intent` is called with JWT or guest token, **Then** an `Order` in `AwaitingPayment` status is created and a `{ clientSecret, orderId }` response is returned
- [ ] **Given** the `clientSecret` is returned, **When** Angular initializes Stripe Elements, **Then** the element can be mounted using the `publishableKey` from environment config
- [ ] **Given** the order `TotalRon` is computed, **When** the Stripe `PaymentIntent` is created, **Then** the amount is passed in **bani** (RON × 100, integer) with currency `"ron"`
- [ ] **Given** the cart is empty, **When** the endpoint is called, **Then** 400 is returned with `"Coșul este gol"`
- [ ] **Given** the Stripe API key is invalid or the API is unreachable, **When** the endpoint is called, **Then** 502 is returned (logged internally, no Stripe error details exposed to client)

## Technical Notes

- Install `Stripe.net` NuGet package
- `StripeClient` configured via `IOptions<StripeOptions>` (`SecretKey`, `PublishableKey`, `WebhookSecret`)
- `PaymentIntentService.CreateAsync` with `{ Amount: totalBani, Currency: "ron", Metadata: { orderId } }`
- `totalBani = (int)(order.TotalRon * 100)` — must be integer bani
- `PaymentIntent.Id` stored on `Order` as `StripePaymentIntentId` for webhook reconciliation
- Endpoint does NOT require raw body — that is only needed on webhook endpoint

## Dependencies

### Requires
- Story 001-order-service (IOrderService.CreateFromCartAsync)
- Stripe.net NuGet package

### Enables
- Story 003-stripe-webhook-handler (webhook references the PaymentIntent created here)
- Bolt 017 (checkout-ui — Angular initializes Elements with clientSecret)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Stripe API rate limit (429) | Retry once after 1s; if still failing, return 502 |
| `TotalRon` rounds to 0 bani | Rejected: Stripe minimum is 1 RON (100 bani) |
| Same user calls endpoint twice (double-click) | Second call creates a second order — frontend must disable button on first click |

## Out of Scope

- Saving payment methods
- Stripe Radar fraud detection configuration
- Stripe Checkout hosted page (ADR-1: Stripe Elements chosen)
