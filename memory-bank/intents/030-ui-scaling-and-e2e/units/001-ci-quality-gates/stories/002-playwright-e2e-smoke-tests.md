---
id: 002-playwright-e2e-smoke-tests
unit: 001-ci-quality-gates
intent: 030-ui-scaling-and-e2e
status: draft
priority: must
created: 2026-06-05T09:30:00Z
assigned_bolt: 066-ci-quality-gates
implemented: false
---

# Story: 002-playwright-e2e-smoke-tests

## User Story

**As a** maintainer launching a payment-processing site
**I want** automated e2e on the three real-money paths
**So that** a regression in checkout/admin/real-time is caught before launch

## Acceptance Criteria

- [ ] **Given** `@playwright/test`, **When** added, **Then** three specs exist: `guest-checkout.spec.ts` (guest → Stripe test mode → confirmation), `admin-login.spec.ts`, `realtime-order.spec.ts` (admin sees SignalR broadcast)
- [ ] **Given** `playwright-e2e.yml`, **When** CI runs, **Then** it boots API+UI via docker-compose and runs the specs using the official Playwright action
- [ ] **Given** the suite, **When** it runs, **Then** it completes within ~3 min and is stable (no flakes)

## Technical Notes

- Guest checkout uses Stripe test mode against a seeded test product.

## Dependencies

### Requires
- 001-bundle-size-budget (shared CI workflow)

### Enables
- 030/002 page breakups (e2e guards regressions)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| SignalR race in e2e | Await the broadcast with a bounded timeout |

## Out of Scope

- Full e2e coverage (smoke tests only).
