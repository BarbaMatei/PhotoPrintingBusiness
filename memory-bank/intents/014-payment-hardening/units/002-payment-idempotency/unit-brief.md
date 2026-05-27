---
unit: 002-payment-idempotency
intent: 014-payment-hardening
phase: inception
status: draft
created: 2026-05-25T10:05:00Z
updated: 2026-05-25T10:05:00Z
---

# Unit Brief: Payment Idempotency

## Purpose

Make `POST /api/payments/stripe/intent` and `POST /api/payments/euplatesc/initiate` idempotent on a per-request key. A repeated call within 24 h returns the same `OrderId` + `ClientSecret` (Stripe) or redirect URL (EuPlatesc), and Stripe's own idempotency is engaged via `RequestOptions.IdempotencyKey`.

## Scope

### In Scope
- Migration: `Orders.IdempotencyKey` nullable column + filtered unique index
- `Order` entity update
- `Controllers/PaymentsController` — read `Idempotency-Key` header
- `Services/PaymentService` (or local helper) — lookup-or-create flow
- Stripe SDK `RequestOptions.IdempotencyKey` integration
- Tests: integration + unit

### Out of Scope
- Distributed idempotency cache (Redis) — see intent 021
- Refund idempotency (admin uses Stripe's own dashboard / SDK retries today)

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-3 | Stripe payment intent idempotency | Must |
| FR-4 | EuPlatesc payment idempotency | Must |
| FR-5 | Schema migration for IdempotencyKey | Must |

---

## Story Summary

| Story ID | Title | Priority |
|----------|-------|----------|
| 001-idempotency-key-migration | Add nullable `Orders.IdempotencyKey` + partial unique index | Must |
| 002-stripe-intent-idempotency | Wire idempotency to Stripe intent endpoint + SDK request options | Must |
| 003-euplatesc-initiate-idempotency | Reuse persisted EuPlatesc redirect URL on repeat calls | Must |

---

## Dependencies

### Depends On
- 001-shipping-cost-server-side (validator must run first so an idempotent retry sees the same canonical payload)

### Depended By
- intent 015-sameday-shipping (lower risk of duplicate AWB requests)
