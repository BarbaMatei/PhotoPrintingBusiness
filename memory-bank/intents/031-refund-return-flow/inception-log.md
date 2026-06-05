---
intent: 031-refund-return-flow
created: 2026-06-05T09:00:00Z
completed: 2026-06-05T10:00:00Z
status: complete
---

# Inception Log: 031-refund-return-flow

## Overview

**Intent**: Wire the full server-side refund flow — order/invoice schema + OrderStatus.Refunded, an admin-initiated refund endpoint, Stripe + EuPlatesc refund execution, and an ANAF credit-note (UBL type 381). Legal requirement under EU Directive 2011/83/EU.
**Type**: brown-field / feature (regulated)
**Source**: `docs/analysis/architect-review-2026-06-03.md` — Group 7 (P09). P20 (coupon) excluded — covered by existing intent 022.
**Created**: 2026-06-05T09:00:00Z

## Proposals Covered

| Proposal | FR | Priority |
|----------|----|----------|
| P09 — Order/Invoice refund schema + state machine | FR-1 | Must |
| P09 — Refund service (full/partial, Stripe + EuPlatesc) | FR-2 | Must |
| P09 — ANAF credit-note generation + submission | FR-3 | Must |
| P09 — Admin refund endpoint | FR-4 | Must |
| P20 — Discount/coupon engine | — | **Excluded** → [[022-coupon-promo-codes]] |

## Artifacts Created

| Artifact | Status | File |
|----------|--------|------|
| Requirements | ✅ | requirements.md |
| System Context | ✅ | system-context.md |
| Units | ✅ | units.md |
| Unit Briefs | ✅ | 2 unit-brief.md |
| Stories | ✅ | 5 story files |
| Bolt Plan | ✅ | bolts 068 (DDD), 069 (simple) |

## Summary

| Metric | Count |
|--------|-------|
| Functional Requirements | 4 |
| Non-Functional Requirements | 6 |
| Units | 2 |
| Stories | 5 |
| Bolts Planned | 2 (068–069) |

## Units Breakdown

| Unit | Stories | Bolt | Type |
|------|---------|------|------|
| 001-refund-domain-and-api | 4 | 068 | ddd |
| 002-refund-return-flow-ui | 1 | 069 | simple (frontend) |

## Decision Log

| Date | Decision | Rationale | Approved |
|------|----------|-----------|----------|
| 2026-06-05 | Exclude P20 (coupon) | Already covered by intent 022; maintainer flagged it | Yes (Checkpoint 1) |
| 2026-06-05 | DDD bolt for unit 001 | Genuine new domain (refund + credit-note) | Yes |
| 2026-06-05 | Best landed after intent 027 | Layered shape; intersects regulated ANAF + archive paths | Yes |

## Scope Changes

| Date | Change | Reason | Impact |
|------|--------|--------|--------|
| 2026-06-05 | Removed coupon (P20) from Group 7 | Pre-existing intent 022 | -1 proposal |

## Ready for Construction

- [x] All requirements documented
- [x] System context defined
- [x] Units decomposed
- [x] Stories created
- [x] Bolts planned
- [x] Human review complete (Checkpoint 3 — approved 2026-06-05)

## Dependencies

Best after **027**; **068** requires **059** (layering) + **063** (Policies.Admin); intersects bolt 039 (ANAF) and bolt 052 (archive retention) — dedicated review.
**Pre-launch must-have IF the launch market includes EU consumers** (requirements Q1).
