---
id: 006-t6-payments-checkout
unit: 001-research-tracks
intent: 034-eu-expansion-architecture-study
status: ready
priority: must
created: 2026-06-05T12:57:50Z
assigned_bolt: 081-research-tracks
implemented: false
---

# Story: 006-t6-payments-checkout

## User Story

**As the** owner deciding EU expansion
**I want** to know how payments work across the target markets with multi-currency, and what changes for the existing Stripe/EuPlatesc integrations
**So that** customers can pay with familiar methods in their currency and the payment code's scope of change is clear

## Acceptance Criteria

- [ ] **Given** Stripe is integrated (bolt 016), **When** T6 reports, **Then** it covers Stripe's EU coverage including local methods (iDEAL, Bancontact, Przelewy24, etc.) and what enabling them implies for the existing integration — per market tier
- [ ] **Given** EuPlatesc is Romania-only, **When** T6 reports, **Then** it gives a clear disposition: keep RO-only or retire outside RO, with rationale
- [ ] **Given** multi-currency is decided, **When** T6 reports, **Then** it recommends a **presentment vs settlement** currency model and states its impact on the order and invoice models
- [ ] **Given** any payment-coverage claim, **When** it appears, **Then** it cites Stripe (or the provider's) official documentation with a date

## Technical Notes

- **Method (FR-1)**: researchers on Stripe EU/local methods + multi-currency settlement + EuPlatesc scope; verification of coverage claims against Stripe docs.
- Output: `docs/analysis/eu-expansion/track-6-payments.md`.
- Coordinate conceptually with T5 on settlement currency (the FX/settlement decision spans both).
- Resolves the residual open question: settlement currency & FX handling.

## Dependencies

### Requires
- None (wave-parallel; related to T5)

### Enables
- 001-synthesis-options-paper (Unit 2)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Local method requires per-country Stripe config | Document the enablement cost |
| Presentment ≠ settlement currency | Specify how order/invoice store both |

## Out of Scope

- Implementing payment changes; tax computation (T5).
