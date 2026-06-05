---
id: 004-payments-journeys
unit: 002-e2e-journey-coverage
intent: 032-regression-and-e2e-stabilization
status: draft
priority: must
created: 2026-06-05T11:35:00Z
assigned_bolt: 071-e2e-journey-coverage
implemented: false
---

# Story: 004-payments-journeys

## User Story

**As a** maintainer
**I want** end-to-end specs for both payment providers in test mode
**So that** Stripe and EuPlatesc are both proven to drive an order to Paid

## Acceptance Criteria

- [ ] **Given** Stripe test mode, **When** a success card pays a pending order, **Then** the webhook transitions the order to Paid and the confirmation reflects it
- [ ] **Given** Stripe test mode, **When** a declined card is used, **Then** the order stays unpaid and the UI shows the decline
- [ ] **Given** EuPlatesc test mode, **When** `initiate` is called and a correctly **signed test IPN** is posted, **Then** the order transitions to Paid (amount verified) and the confirmation reflects it
- [ ] **Given** EuPlatesc, **When** an IPN with a bad signature or wrong amount arrives, **Then** it is rejected and the order does not become Paid
- [ ] **Given** these specs, **When** run in CI, **Then** they pass with no live keys (test-mode fixtures from unit 001 story 003)

## Technical Notes

- Stripe webhook idempotency + EuPlatesc amount-check are existing behaviours (intent 004 / 014) — the specs assert them end-to-end.
- EuPlatesc IPN is server-to-server; drive it via the signed-payload fixture rather than the hosted page.

## Dependencies

### Requires
- 003-payment-testmode-fixtures (unit 001)

### Enables
- 003-triage-findings-to-backlog (unit 003)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Duplicate Stripe webhook | Idempotent — no double transition |
| 3DS-required card | 3DS test challenge completed; order Paid |

## Out of Scope

- Refund flows (gated story 007).
