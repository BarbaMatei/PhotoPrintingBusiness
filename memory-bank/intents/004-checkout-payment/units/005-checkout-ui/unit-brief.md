---
id: 005-checkout-ui
intent: 004-checkout-payment
type: frontend
bolt_type: simple-construction-bolt
bolts: ["017-checkout-ui"]
status: draft
created: 2026-05-21T12:00:00Z
---

# Unit Brief: checkout-ui

## Purpose

The full Angular checkout experience — a 3-step stepper (Delivery → Review → Payment) plus the confirmation page. Consumes the shipping API, cart state, and payment backends. This unit makes the payment backends visible and usable to customers.

## Why One Bolt?

The checkout steps share a single `CheckoutStateService` that carries state (delivery selection, terms acceptance) across routes. Splitting steps across bolts would produce half-working states. The stepper, steps, and confirmation page are delivered together.

## Key Technical Challenges

- Leaflet.js integration in Angular standalone components (needs `ngx-leaflet` or custom wrapper)
- Stripe Elements must be initialized with `clientSecret` just-in-time (before user submits)
- EuPlatesc redirect must handle browser back-button case gracefully (order in `AwaitingPayment` state)
- Checkout state must survive browser refresh (sessionStorage backup)
- Confirmation page must handle `?processor=euplatesc` query param from EuPlatesc return URL

## Stories

| # | Story | FRs | Bolt |
|---|-------|-----|------|
| 001 | checkout-stepper | FR-16, FR-17, FR-18 | 017 |
| 002 | delivery-step | FR-16 | 017 |
| 003 | locker-map-component | FR-16 | 017 |
| 004 | order-review-step | FR-17 | 017 |
| 005 | payment-step | FR-18 | 017 |
| 006 | order-confirmation-page | FR-18 | 017 |

## Dependencies

- **Requires**: Bolt 015 (shipping-and-order-core — `/api/shipping/*` endpoints), Bolt 016 (payment-backends — payment endpoints), Bolt 014 (upload-format-cart-ui — CartService cart state)
- **Enables**: Future intent 005-order-management (order confirmation page links to order history)
