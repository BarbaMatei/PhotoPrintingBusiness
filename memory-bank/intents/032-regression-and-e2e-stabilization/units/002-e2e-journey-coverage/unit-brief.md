---
unit: 002-e2e-journey-coverage
intent: 032-regression-and-e2e-stabilization
phase: inception
status: stories-defined
unit_type: frontend
default_bolt_type: simple-construction-bolt
created: 2026-06-05T11:20:00Z
updated: 2026-06-05T11:30:00Z
---

# Unit Brief: E2E Journey Coverage

## Purpose

Extend bolt 066's 3-smoke-test Playwright module into comprehensive coverage of every major user journey, with a stable CI execution model (fast PR tier + scheduled full suite, bounded retries, failure artifacts). This is the bulk of the intent.

## Scope

### In Scope
- Journey specs across all domains: guest + registered checkout, authentication (×3), uploads + cart + guest→user merge, payments (Stripe + EuPlatesc test mode), order history + detail (incl. ownership), account management, admin (order/product/invoice).
- Gated coupon (047/048) and refund (068/069) journeys, authored as `should` / `test.fixme` until those bolts ship.
- CI integration: fast PR tier vs scheduled full suite; Playwright config retries + trace/video/screenshot artifacts; flake elimination (condition-based waits).

### Out of Scope
- Fixtures + data contract (unit 001).
- The Playwright runner + base workflow (bolt 066 — extended, not rebuilt).
- The regression checklist + baseline (unit 003).
- Implementing coupons/refunds (their feature bolts).

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-2 | Comprehensive end-to-end journey coverage | Must (gated coupon/refund = Should) |
| FR-3 | E2e CI integration, stability & reporting | Must |

---

## Domain Concepts

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| Run journey | Drive a full path via `data-testid` selectors | fixture context | pass/fail + artifacts |
| Tier selection | Fast subset on PR, full on schedule/label | CI trigger | targeted run |
| Gate spec | Skip until feature bolt ships, then un-gate | feature availability | active/skipped spec |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 8 |
| Must Have | 6 |
| Should Have | 2 |
| Could Have | 0 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-guest-and-registered-checkout | Guest + registered checkout journeys | Must | Planned |
| 002-authentication-journeys | Email/Google/guest-claim auth journeys | Must | Planned |
| 003-uploads-cart-and-merge | Uploads, cart edits, guest→user merge | Must | Planned |
| 004-payments-journeys | Stripe + EuPlatesc test-mode journeys | Must | Planned |
| 005-orders-and-account-journeys | Order history/detail + account mgmt | Must | Planned |
| 006-admin-journeys | Admin order/product/invoice journeys | Must | Planned |
| 007-gated-coupon-refund-journeys | Gated coupon + refund journeys | Should | Planned |
| 008-e2e-ci-tiers-and-stability | CI tiers, retries, artifacts, flake controls | Must | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 001-e2e-data-strategy | Consumes fixtures + data contract |
| bolt 066 (intent 030) | Playwright module + `playwright-e2e.yml` extended here |

### Depended By
| Unit | Reason |
|------|--------|
| 003-regression-methodology | Checklist marks which checks are automated-by-e2e |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| bolt 047/048 (coupons) | Un-gate coupon journeys | Gated (Should) |
| bolt 068/069 (refunds) | Un-gate refund journeys | Gated (Should) |
| Stripe / EuPlatesc test mode | Payment journeys | Low |

---

## Technical Context

### Suggested Technology
`@playwright/test` (web-first assertions), `data-testid` selectors, GitHub Actions matrix/schedule, docker-compose boot from unit 001.

### Integration Points
| Integration | Type | Protocol |
|-------------|------|----------|
| `playwright-e2e.yml` | CI | extends bolt 066 |
| SignalR admin hub | real-time | awaited with bounded timeout |

---

## Constraints

- Zero fixed `sleep`/`waitForTimeout`; condition-based waits only.
- All selectors via `data-testid` (coding-standards Testing Strategy).
- PR fast tier < ~8 min; full suite < ~25 min.
- Coupon/refund specs stay `test.fixme` until their bolts ship; un-gating is a one-line change.

---

## Success Criteria

### Functional
- [ ] Every FR-2 journey has a passing spec (non-gated) in CI.
- [ ] Declined-card, validation-failure, and cross-user-ownership branches covered.
- [ ] Gated coupon/refund specs exist and skip cleanly.

### Non-Functional
- [ ] Fast tier on PR; full suite scheduled; artifacts on failure.
- [ ] Green across 3 consecutive scheduled runs.

### Quality
- [ ] No flaky specs; no fixed sleeps.

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 071-e2e-journey-coverage | simple | 001–008 | Full journey specs + CI tiers |

---

## Notes

Largest unit. Stories are grouped by domain; each is independently authorable once unit 001's fixtures exist. The 8 stories slightly exceed the 5–6 soft cap, but they are thin, parallel spec-authoring tasks sharing one fixture layer, so they remain a single coherent bolt rather than an artificial split.
