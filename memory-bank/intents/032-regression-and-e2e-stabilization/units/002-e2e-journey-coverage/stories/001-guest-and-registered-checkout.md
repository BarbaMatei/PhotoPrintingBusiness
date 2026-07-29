---
id: 001-guest-and-registered-checkout
unit: 002-e2e-journey-coverage
intent: 032-regression-and-e2e-stabilization
status: draft
priority: must
created: 2026-06-05T11:35:00Z
assigned_bolt: 071-e2e-journey-coverage
implemented: false
---

# Story: 001-guest-and-registered-checkout

## User Story

**As a** maintainer launching a payment site
**I want** end-to-end specs for both the guest and the registered checkout journeys
**So that** the two primary money paths are proven before launch

## Acceptance Criteria

- [ ] **Given** a guest, **When** they upload → pick format/finish → add to cart → enter guest details → pay via Stripe test mode → reach confirmation, **Then** the order is created and shows the correct total (incl. VAT + shipping)
- [ ] **Given** the guest checkout, **When** a declined test card is used, **Then** the UI surfaces the failure and no Paid order is created
- [ ] **Given** a registered user, **When** they log in → upload → cart → checkout → pay → view order in `/comenzi`, **Then** the order appears in their history with the correct status
- [ ] **Given** both specs, **When** they run in CI, **Then** they pass on the shared harness using `data-testid` selectors and condition-based waits

## Technical Notes

- Extends bolt 066's `guest-checkout.spec.ts` smoke into the full path + the decline branch; reuses unit-001 fixtures + payment fixtures (story 003).

## Dependencies

### Requires
- 002-builder-backed-fixtures, 003-payment-testmode-fixtures (unit 001)
- bolt 066 (smoke spec extended)

### Enables
- 003-triage-findings-to-backlog (unit 003) consumes the result

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Empty cart at checkout | Checkout blocked with a clear message |
| Guest abandons after intent created | No Paid order; pending order eventually cleaned |

## Out of Scope

- EuPlatesc-specific flow (story 004); coupon at checkout (story 007).
