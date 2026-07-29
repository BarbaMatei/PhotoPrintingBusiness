---
id: 003-euplatesc-initiate-idempotency
unit: 002-payment-idempotency
intent: 014-payment-hardening
status: complete
priority: must
created: 2026-05-25T10:05:00Z
assigned_bolt: 035-payment-idempotency
implemented: true
implemented_at: 2026-05-25T14:15:00Z
---

# Story: 003-euplatesc-initiate-idempotency

## User Story

**As** a checkout user paying via EuPlatesc
**I want** clicking "Pay" twice to redirect me to the same EuPlatesc page
**So that** I don't end up with two orders awaiting payment

## Acceptance Criteria

- [ ] **Given** two `POST /api/payments/euplatesc/initiate` calls with the same `Idempotency-Key`, **Then** the response body returns the same redirect URL and the same `OrderId`.
- [ ] **Given** the first call fails before persisting the order (e.g. HMAC compute error), **When** the client retries with the same key, **Then** a fresh attempt is allowed (no stuck state).
- [ ] **Given** divergent body for the same key, **Then** 409 ProblemDetails as in story 002.
- [ ] Missing header behaves as today + logs warning.

## Technical Notes

- EuPlatesc has no server-side idempotency primitive. The only guarantee is that we never persist two `Orders` for the same `Idempotency-Key`.
- The signed HMAC-MD5 payload is deterministic from `(orderId, total, currency, merchant)` so the redirect URL is reproducible if we keep the order ID stable.
- Persist the constructed redirect URL on `Order` (new nullable `EuPlatescRedirectUrl` column may be desirable — decide in bolt plan; otherwise reconstruct on the fly from the persisted order).

## Dependencies

### Requires
- 002-stripe-intent-idempotency (lookup helper)

### Enables
- Intent 015-sameday-shipping (lower-noise downstream)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Customer returns from EuPlatesc cancellation flow with the same key | Reuse the existing order + URL |
| IPN already received against the original order | Repeat initiate is harmless — IPN owns status |

## Out of Scope

- IPN handler changes (already idempotent via signature + amount check).
