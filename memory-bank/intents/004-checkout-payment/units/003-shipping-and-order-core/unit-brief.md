---
id: 003-shipping-and-order-core
intent: 004-checkout-payment
type: backend
bolt_type: ddd-construction-bolt
bolts: ["015-shipping-and-order-core"]
status: draft
created: 2026-05-21T12:00:00Z
---

# Unit Brief: shipping-and-order-core

## Purpose

The two foundational backend schemas needed by the payment flow: (a) the Easybox locker catalog with shipping API, and (b) the Order + OrderItem entity schema with order number generation and status machine. This unit has no payment logic itself — it establishes the domain model that bolts 016 and 017 build on.

## Why One Bolt?

Shipping API and Order entity are both needed before any payment bolt can be built (payment creates an Order, which references a locker or address). They share the same dependency (bolt 013) and can be built together.

## Key Technical Challenges

- Seeding ~200 EasyboxLocker rows in an EF Core migration without bloating migration history
- Order number generation (`FT-YYYYNNNN`) must be concurrency-safe (DB sequence or atomic counter)
- `OrderStatus` state machine must be enforced at the service layer, not the controller

## Stories

| # | Story | FRs | Bolt |
|---|-------|-----|------|
| 001 | easybox-locker-catalog | FR-10 | 015 |
| 002 | shipping-endpoints | FR-11 | 015 |
| 003 | order-entity-schema | FR-12 | 015 |
| 004 | order-status-machine | FR-13 | 015 |

## Dependencies

- **Requires**: Bolt 013 (cart-api — CartItems needed to create OrderItems from cart)
- **Enables**: Bolt 016 (payment-backends — Order entity + status machine needed for payment)
