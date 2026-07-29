---
id: 071-e2e-journey-coverage
unit: 002-e2e-journey-coverage
intent: 032-regression-and-e2e-stabilization
type: simple-construction-bolt
status: planned
stories:
  - 001-guest-and-registered-checkout
  - 002-authentication-journeys
  - 003-uploads-cart-and-merge
  - 004-payments-journeys
  - 005-orders-and-account-journeys
  - 006-admin-journeys
  - 007-gated-coupon-refund-journeys
  - 008-e2e-ci-tiers-and-stability
created: 2026-06-05T11:45:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [070-e2e-data-strategy, 066-ci-quality-gates]
enables_bolts: [072-regression-methodology]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 2
  max_dependencies: 3
  testing_scope: 3
---

# Bolt: 071-e2e-journey-coverage

## Overview

The comprehensive Playwright journey specs across every domain — guest + registered checkout, authentication (×3), uploads + cart + merge, payments (Stripe + EuPlatesc test mode), orders + account, admin — plus CI tiering (fast PR tier + scheduled full suite), bounded retries, failure artifacts, and flake elimination. Coupon/refund journeys are authored but gated. Extends bolt 066's module; consumes bolt 070's fixtures.

## Objective

Take e2e from 3 smoke tests to full-application coverage that is deterministic in CI, so the whole app's journeys are provably green before Phase-3 stabilization is declared.

## Stories Included

- **001-guest-and-registered-checkout**: Both money paths + decline branch (Must)
- **002-authentication-journeys**: Email + Google (mocked) + guest-claim (Must)
- **003-uploads-cart-and-merge**: Uploads, cart edits, guest→user merge (Must)
- **004-payments-journeys**: Stripe + EuPlatesc test mode, success + reject (Must)
- **005-orders-and-account-journeys**: History/detail + ownership + account mgmt (Must)
- **006-admin-journeys**: Admin order/product/invoice (Must)
- **007-gated-coupon-refund-journeys**: Authored + gated (Should)
- **008-e2e-ci-tiers-and-stability**: Fast/full tiers, retries, artifacts, no fixed sleeps (Must)

## Bolt Type

**Type**: simple-construction-bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Pending → implementation-plan.md (journey inventory + tier split)
- [ ] **2. implement**: Pending → `e2e/journeys/*.spec.ts`; extended `playwright-e2e.yml`; Playwright config retries + artifacts
- [ ] **3. test**: Pending → fast tier on PR; full suite green across 3 scheduled runs

## Dependencies

### Requires
- **070-e2e-data-strategy** (Required): fixtures + data contract + real-PG boot
- **066-ci-quality-gates** (Required): `playwright-e2e.yml` extended here

### Enables
- 072-regression-methodology (checklist marks automated-by-e2e coverage)

## Success Criteria

- [ ] Every FR-2 journey (non-gated) has a passing spec in CI
- [ ] Declined-card, validation, and cross-user-ownership branches covered
- [ ] Gated coupon/refund specs exist and skip cleanly (`test.fixme`, requires 047/048 + 068/069)
- [ ] Fast PR tier < ~8 min; full suite < ~25 min; artifacts on failure; green ×3

## Notes

8 stories (over the 5–6 soft cap) but they are thin, parallel, domain-sliced spec-authoring tasks over one shared fixture layer — kept as one coherent bolt. Gated coupon/refund specs reference bolts 047/048 + 068/069 but do NOT re-implement them.
