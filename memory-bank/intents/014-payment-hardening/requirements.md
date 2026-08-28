---
intent: 014-payment-hardening
phase: inception
status: complete
created: 2026-05-25T10:05:00Z
updated: 2026-05-25T10:05:00Z
source: docs/architecture-analysis-2026-05-25.md#2
priority_score: 23
---

# Requirements: Payment Hardening

## Intent Overview

Two concrete attack surfaces on the checkout path:

1. **Client-trusted shipping cost** — `CreateOrderRequest.ShippingCostRon` flows from the Angular client straight into `order.TotalRon` with no server validation. POST `"ShippingCostRon": -100` yields a discounted real charge.
2. **No payment idempotency** — every call to `POST /api/payments/stripe/intent` creates a fresh `Order` row and Stripe `PaymentIntent`. Double-clicking "Pay" produces duplicate paid orders.

This intent closes both holes: shipping cost is resolved server-side from the chosen `DeliveryType`, and both payment-intent endpoints honour an `Idempotency-Key` header (forwarded to Stripe).

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Eliminate client-side price tampering | Server rejects `CreateOrderRequest` carrying a `ShippingCostRon` field (422 or silently ignored, see Q1) | Must |
| Eliminate duplicate orders/charges from retries | Two calls with the same `Idempotency-Key` within 24 h return the same `OrderId` + `ClientSecret`; one Stripe charge | Must |
| Block invalid delivery configurations server-side | Easybox requires `EasyboxLockerId`; Courier requires `ShippingAddress`; mismatch → 422 with field errors | Must |

---

## Functional Requirements

### FR-1: Server-side shipping cost resolution
- **Description**: Remove `ShippingCostRon` from the accepted DTO. `OrderService.CreateFromCartAsync` calls `IShippingService.GetShippingCostAsync(request.DeliveryType, request.ShippingAddress?.CountyCode)` and adds the resolved value to `order.TotalRon`.
- **Acceptance Criteria**:
  - Sending `ShippingCostRon: -100` does not reduce `Order.TotalRon`.
  - For `DeliveryType.Easybox`, server uses Easybox rate (currently 20 RON from config).
  - For `DeliveryType.Courier`, server uses Courier rate (currently 25 RON from config).
  - For unknown delivery type, request fails 422.
- **Priority**: Must
- **Related Stories**: US-014-1

### FR-2: CreateOrderRequest validator
- **Description**: Introduce `CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>` enforcing conditional field requirements per `DeliveryType`.
- **Acceptance Criteria**:
  - `Easybox` + null `EasyboxLockerId` → 422 with `errors:[{field:"EasyboxLockerId", message}]`.
  - `Courier` + null/blank `ShippingAddress` → 422.
  - `Courier` with `ShippingAddress.PostalCode` missing → 422.
  - `PaymentProcessor` must be defined enum value, else 422.
- **Priority**: Must
- **Related Stories**: US-014-2

### FR-3: Stripe payment intent idempotency
- **Description**: `POST /api/payments/stripe/intent` accepts an `Idempotency-Key` request header (UUID v4 recommended). First call with a given key creates `Order` + `PaymentIntent` and persists the key on the order; later calls within 24 h return the previously created `OrderId` + `ClientSecret`.
- **Acceptance Criteria**:
  - Same key, same payload → identical response, no new `Order`, no new `PaymentIntent`.
  - Same key, different `PaymentProcessor` / total → 409 Conflict with explanatory ProblemDetails.
  - Missing key → endpoint behaves as today (creates new order/intent) **but** logs a warning until the FE always sends one.
  - The same key is passed through to Stripe via `RequestOptions.IdempotencyKey` so duplicate Stripe charges are also blocked at the gateway.
- **Priority**: Must
- **Related Stories**: US-014-3

### FR-4: the legacy processor payment idempotency
- **Description**: `POST /api/payments/legacy-processor/initiate` accepts the same `Idempotency-Key` header; reuses `Order` + redirect URL within 24 h on a repeat call. the legacy processor has no idempotency primitive, so only the server-side order de-duplication applies.
- **Acceptance Criteria**:
  - Repeat call within 24 h returns the same redirect URL.
  - The persisted the legacy processor order ref + HMAC payload is reused.
- **Priority**: Must
- **Related Stories**: US-014-4

### FR-5: Schema migration for IdempotencyKey
- **Description**: Add nullable `IdempotencyKey` column + filtered unique index on `Orders` so the same key cannot map to two orders.
- **Acceptance Criteria**:
  - EF Core migration `20260526_*_AddOrderIdempotencyKey` applied cleanly.
  - Unique index `ix_orders_idempotency_key` is partial (`WHERE IdempotencyKey IS NOT NULL`).
- **Priority**: Must
- **Related Stories**: US-014-5

---

## Non-Functional Requirements

### Security
| Requirement | Standard | Notes |
|-------------|----------|-------|
| Price integrity | Server-side authority | Only the server's shipping table is trusted |
| Replay safety | RFC-style idempotency window | 24 h; first response cached against the key |

### Performance
| Requirement | Metric | Target |
|-------------|--------|--------|
| Extra DB roundtrip per payment call | p95 added latency | < 5 ms (single indexed lookup) |

### Compatibility
| Requirement | Notes |
|-------------|-------|
| Frontend transitional period | Accept and ignore `ShippingCostRon` for one release to avoid coordinated deploy break |

---

## Constraints

### Technical Constraints
- Must integrate with existing `OrderService.CreateFromCartAsync` flow; do not rewrite payment lifecycle.
- Stripe.NET 46.3 already in use — supports `RequestOptions.IdempotencyKey` natively.

### Business Constraints
- Ship before intent 015 (Sameday) to prevent fraudulent orders triggering real AWB calls.

---

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| Frontend will adopt `Idempotency-Key` headers in the next release | Without it, idempotency is best-effort | Log warnings until 100 % of intent calls carry a key |
| Shipping rate table is appropriate single source of truth | Real Sameday rates may differ per locker — out of scope here | Intent 015 replaces static rates |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| Q1: Reject (422) or silently ignore unexpected `ShippingCostRon` field for one release? | Backend + FE | 2026-06-01 | Recommend silently ignore + log; reject in following release |
