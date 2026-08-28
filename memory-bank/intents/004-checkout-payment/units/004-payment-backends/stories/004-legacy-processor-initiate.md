---
id: 004-legacy-processor-initiate
unit: 004-payment-backends
intent: 004-checkout-payment
status: complete
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 016-payment-backends
implemented: true
---

# Story: 004-legacy-processor-initiate

## User Story

**As a** customer choosing to pay by Romanian card
**I want** to be redirected to the legacy processor's hosted payment page
**So that** I can pay with my Romanian bank card without entering card details on FotoTipar

## Acceptance Criteria

- [ ] **Given** a valid cart and delivery selection, **When** `POST /api/payments/legacy-processor/initiate` is called, **Then** an `Order` in `AwaitingPayment` status is created and a `{ redirectUrl, orderId }` response is returned
- [ ] **Given** the `redirectUrl` is returned, **When** Angular navigates to it (`window.location.href`), **Then** the legacy processor's hosted payment page loads with FotoTipar's order details pre-filled
- [ ] **Given** the HMAC-MD5 signature is computed, **When** it is included in the redirect URL parameters, **Then** the signature matches the legacy processor's expected format (exact field order per spec)
- [ ] **Given** the amount is included in the HMAC, **When** the payment page loads, **Then** the amount is in RON with 2 decimal places (e.g., `"49.50"`) — NOT bani
- [ ] **Given** the legacy processor keys are missing from configuration, **When** the application starts, **Then** `IOptions` validation fails and the app does not start

## Technical Notes

- `LegacyProcessorOptions`: `{ MerchantId, SecretKey, LiveMode (bool), PaymentUrl }`
- HMAC-MD5 field order (per the legacy processor spec): `amount|currency|invoice|description|externalRef|fp_hash` — **exact order is critical**
- `fp_hash = HMAC-MD5(fields, SecretKey).ToUpperCase()`
- Amount format: `order.TotalRon.ToString("F2", CultureInfo.InvariantCulture)` (e.g., `"49.50"`)
- Currency: `"RON"` (string literal, per the legacy processor spec)
- `externalRef`: `order.Id.ToString()` — used for IPN reconciliation
- Redirect URL: `{PaymentUrl}?amount={amount}&currency=RON&...&fp_hash={hash}`
- `Order.LegacyProcessorExternalRef` stored for IPN matching

## Dependencies

### Requires
- Story 001-order-service (IOrderService.CreateFromCartAsync)

### Enables
- Story 005-legacy-processor-ipn-handler (IPN references `externalRef` = orderId)
- Bolt 017 (checkout-ui — Angular redirects to the legacy processor URL)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| the legacy processor payment URL unreachable | Frontend receives `redirectUrl`; unreachability is discovered when browser redirects |
| Amount has more than 2 decimal places due to floating point | `Math.Round(totalRon, 2, MidpointRounding.AwayFromZero)` before formatting |
| `LiveMode = false` | Use the legacy processor sandbox URL and test credentials |

## Out of Scope

- the legacy processor 3D Secure flow details (handled by the legacy processor hosted page)
- Saving the legacy processor payment method for future use
