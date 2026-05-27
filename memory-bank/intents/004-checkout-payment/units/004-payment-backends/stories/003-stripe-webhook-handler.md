---
id: 003-stripe-webhook-handler
unit: 004-payment-backends
intent: 004-checkout-payment
status: draft
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 016-payment-backends
implemented: false
---

# Story: 003-stripe-webhook-handler

## User Story

**As a** developer
**I want** a Stripe webhook endpoint that confirms or fails orders
**So that** orders are only marked as `Paid` after Stripe's authoritative payment confirmation, not based on client-side signals

## Acceptance Criteria

- [ ] **Given** a `payment_intent.succeeded` event is received, **When** the `Stripe-Signature` header is valid, **Then** the order matching `PaymentIntent.Metadata.orderId` is transitioned to `Paid`, `PaidAt` is set, and `IEmailService.SendOrderConfirmedAsync` is called
- [ ] **Given** a `payment_intent.payment_failed` event is received, **When** the signature is valid, **Then** the order is transitioned to `PaymentFailed`
- [ ] **Given** a webhook event is received for an order already in `Paid` status, **When** processed, **Then** 200 is returned and no side effects occur (idempotent)
- [ ] **Given** the `Stripe-Signature` header is missing or tampered, **When** the webhook is called, **Then** 400 is returned and the event is ignored
- [ ] **Given** the raw request body is read for signature verification, **When** ASP.NET Core's body pipeline runs, **Then** JSON deserialization middleware is bypassed on this endpoint (raw body preserved)

## Technical Notes

- Endpoint: `POST /api/webhooks/stripe` — must be **excluded from `[Authorize]`** and JSON body parsing
- Raw body: disable `[ApiController]` automatic binding; use `Request.Body.ReadAllBytesAsync()` before verification
- Signature verification: `EventUtility.ConstructEvent(payload, header, webhookSecret)` — throws `StripeException` on invalid sig
- Idempotency check: `if (order.Status != OrderStatus.AwaitingPayment) return Ok();`
- `IEmailService.SendOrderConfirmedAsync` call: fire-and-forget (non-blocking); email failure must not affect webhook response
- Webhook endpoint must return 200 quickly (Stripe retries if response > 30s)

## Dependencies

### Requires
- Story 002-stripe-payment-intent (Order has `StripePaymentIntentId`)
- Story 004-order-status-machine (OrderStatusMachine.Transition)
- Bolt 003 (email-infrastructure — IEmailService)

### Enables
- Bolt 017 (checkout-ui — confirmation page polls `GET /api/orders/{id}` for Paid status)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Stripe delivers webhook before intent endpoint returns | Order in `AwaitingPayment`; webhook processes normally |
| `orderId` in metadata not found | Log warning, return 200 (Stripe must not retry) |
| `IEmailService` throws | Log error, still return 200 to Stripe |
| Webhook received after order cancelled | `AwaitingPayment` check fails; return 200, no transition |

## Out of Scope

- Stripe `payment_intent.canceled` event (handled as `PaymentFailed` for simplicity)
- Refund events (`charge.refunded`) — future intent
