---
id: 004-payment-backends
intent: 004-checkout-payment
type: backend
bolt_type: ddd-construction-bolt
bolts: ["016-payment-backends"]
status: draft
created: 2026-05-21T12:00:00Z
---

# Unit Brief: payment-backends

## Purpose

The complete backend payment integration: Stripe (PaymentIntent, webhook) and EuPlatesc (redirect initiate, IPN callback). Both processors share the `OrderService` which creates Orders from carts and fires post-payment side effects (email trigger, upload association).

## Why One Bolt?

Stripe and EuPlatesc share the Order entity, `IOrderService`, and the webhook/IPN pattern. Shared context means they are faster to build together than independently, and the ADR decision to keep them in parallel (same Order model) is easier to enforce in one bolt.

## Key Technical Challenges

- Stripe: raw body must be read before JSON deserialization for webhook signature verification (ASP.NET Core body parsing must be disabled on webhook endpoint)
- EuPlatesc: HMAC-MD5 field order is exact per specification — any ordering mistake breaks all payments
- IPN amount validation: must cross-check IPN amount against stored order amount in RON
- Idempotency: both webhook handlers must check existing order status before applying transition

## Stories

| # | Story | FRs | Bolt |
|---|-------|-----|------|
| 001 | order-service | FR-12 | 016 |
| 002 | stripe-payment-intent | FR-14 | 016 |
| 003 | stripe-webhook-handler | FR-14 | 016 |
| 004 | euplatesc-initiate | FR-15 | 016 |
| 005 | euplatesc-ipn-handler | FR-15 | 016 |

## Dependencies

- **Requires**: Bolt 015 (shipping-and-order-core — Order entity + status machine)
- **Enables**: Bolt 017 (checkout-ui — payment endpoints called from Angular checkout)
