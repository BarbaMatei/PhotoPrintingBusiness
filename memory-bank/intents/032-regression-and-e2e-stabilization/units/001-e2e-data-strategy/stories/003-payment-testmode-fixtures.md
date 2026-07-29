---
id: 003-payment-testmode-fixtures
unit: 001-e2e-data-strategy
intent: 032-regression-and-e2e-stabilization
status: draft
priority: should
created: 2026-06-05T11:30:00Z
assigned_bolt: 070-e2e-data-strategy
implemented: false
---

# Story: 003-payment-testmode-fixtures

## User Story

**As a** developer authoring payment journeys
**I want** documented Stripe and EuPlatesc test-mode fixtures
**So that** checkout journeys complete payments headlessly with no live keys

## Acceptance Criteria

- [ ] **Given** Stripe test mode, **When** a fixture is provided, **Then** it exposes the success test card, a declined test card, and (if used) a 3DS-required test card, plus a helper to fill the Stripe Elements iframe via stable selectors
- [ ] **Given** EuPlatesc test mode, **When** a fixture is provided, **Then** it can drive `initiate` and post a correctly **HMAC-signed test IPN** payload to the webhook so the order transitions to Paid without a real gateway redirect
- [ ] **Given** CI, **When** the suite runs, **Then** Stripe/EuPlatesc credentials come from CI env/test config only and are never committed
- [ ] **Given** a payment fixture, **When** used by a spec, **Then** the test-mode behaviour matches the documented data contract (story 001)

## Technical Notes

- Stripe Elements runs in an iframe — provide a helper that handles frame locating; rely on Stripe's documented test cards (e.g. `4242…` success, `4000…0002` decline).
- For EuPlatesc, the IPN callback is server-to-server; the fixture signs a test payload per the existing HMAC scheme rather than scripting the hosted page.

## Dependencies

### Requires
- 001-e2e-data-contract

### Enables
- 004-payments-journeys (unit 002)
- 001-guest-and-registered-checkout (unit 002)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Stripe 3DS challenge | Use the 3DS test card; fixture completes the test challenge deterministically |
| EuPlatesc bad signature | IPN rejected; fixture also covers the reject path for the decline branch |

## Out of Scope

- The payment journey specs (unit 002 story 004).
