---
id: 005-euplatesc-ipn-handler
unit: 004-payment-backends
intent: 004-checkout-payment
status: complete
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 016-payment-backends
implemented: true
---

# Story: 005-euplatesc-ipn-handler

## User Story

**As a** developer
**I want** an EuPlatesc IPN endpoint that confirms orders after successful Romanian card payments
**So that** orders are reliably moved to `Paid` status even if the customer closes their browser after redirecting to EuPlatesc

## Acceptance Criteria

- [ ] **Given** an IPN POST with `action=0` (success), **When** the HMAC-MD5 signature is valid and the amount matches the stored order amount, **Then** the order is transitioned to `Paid`, `PaidAt` is set, and `IEmailService.SendOrderConfirmedAsync` is called
- [ ] **Given** an IPN POST with `action != 0` (failure), **When** processed, **Then** the order is transitioned to `PaymentFailed`
- [ ] **Given** the HMAC-MD5 signature in the IPN does not match the recomputed value, **When** the IPN is processed, **Then** the request is rejected with 400 and no order state change occurs
- [ ] **Given** the amount in the IPN (`amount` field) differs from `Order.TotalRon`, **When** the IPN is processed, **Then** the order is NOT transitioned; a warning is logged and 400 is returned
- [ ] **Given** the same IPN is delivered twice, **When** the second delivery arrives for an already-`Paid` order, **Then** 200 is returned and no side effects occur (idempotent)
- [ ] **Given** the IPN response format, **When** responding to EuPlatesc, **Then** the response matches the EuPlatesc IPN response specification (exact format required)

## Technical Notes

- Endpoint: `POST /api/webhooks/euplatesc` — must be **excluded from `[Authorize]`**
- IPN fields (from EuPlatesc spec): `amount`, `currency`, `invoice`, `action`, `message`, `approval`, `timestamp`, `nonce`, `fp_hash`
- HMAC recomputation: same field order as initiation — `amount|currency|invoice|...`; compare with `fp_hash` in IPN
- Amount comparison: `Math.Abs(ipnAmount - order.TotalRon) < 0.01m` (float tolerance)
- `externalRef` in IPN maps to `Order.EuPlatescExternalRef` (= `orderId`)
- IPN response body (per EuPlatesc spec): `<EPAYMENT>{timestamp}|{fp_hash}</EPAYMENT>` where `fp_hash` is computed over response fields
- `IEmailService.SendOrderConfirmedAsync`: fire-and-forget (non-blocking)

## Dependencies

### Requires
- Story 004-euplatesc-initiate (Order has `EuPlatescExternalRef`)
- Story 004-order-status-machine (OrderStatusMachine.Transition)
- Bolt 003 (email-infrastructure — IEmailService)

### Enables
- Bolt 017 (checkout-ui — confirmation page polls for Paid status after EuPlatesc return)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| IPN received before customer returns from EuPlatesc | Normal flow — IPN may arrive before redirect; confirmation page polls for Paid |
| `externalRef` not found in DB | Log warning, return valid IPN response (EuPlatesc must not retry indefinitely) |
| `IEmailService` throws | Log error, still return valid IPN response |
| Amount in IPN is 0 | Amount mismatch; reject with 400 |

## Out of Scope

- EuPlatesc refund IPN (`action=3`)
- EuPlatesc partial capture
