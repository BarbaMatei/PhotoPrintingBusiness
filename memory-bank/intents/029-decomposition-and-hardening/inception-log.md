---
intent: 029-decomposition-and-hardening
created: 2026-06-05T09:00:00Z
completed: 2026-06-05T10:00:00Z
status: complete
---

# Inception Log: 029-decomposition-and-hardening

## Overview

**Intent**: Decompose the god-classes into the new layered shape — split AuthService into 3, thin WebhooksController + OrderService (move GetOrderPhotosAsync out), per-entity DbContext configs — plus a global rate limit and a centralised admin-role policy constant.
**Type**: brown-field / refactor + security hardening
**Source**: `docs/analysis/architect-review-2026-06-03.md` — Group 5 (P08, P13, P14, P15)
**Created**: 2026-06-05T09:00:00Z

## Proposals Covered

| Proposal | FR | Priority |
|----------|----|----------|
| P08 — Global rate limit + admin policy constant | FR-1 | Should (soft pre-launch) |
| P13 — Decompose AuthService into 3 services | FR-2 | Should |
| P14 — Decompose WebhooksController + OrderService god-methods | FR-3 | Should |
| P15 — Per-entity IEntityTypeConfiguration<T> | FR-4 | Could |

## Artifacts Created

| Artifact | Status | File |
|----------|--------|------|
| Requirements | ✅ | requirements.md |
| System Context | ✅ | system-context.md |
| Units | ✅ | units.md |
| Unit Briefs | ✅ | 3 unit-brief.md |
| Stories | ✅ | 5 story files |
| Bolt Plan | ✅ | bolts 063, 064, 065 |

## Summary

| Metric | Count |
|--------|-------|
| Functional Requirements | 4 |
| Non-Functional Requirements | 4 |
| Units | 3 |
| Stories | 5 |
| Bolts Planned | 3 (063–065) |

## Units Breakdown

| Unit | Stories | Bolt | Type |
|------|---------|------|------|
| 001-access-hardening | 2 | 063 | simple |
| 002-service-decomposition | 2 | 064 | simple |
| 003-persistence-config | 1 | 065 | simple |

## Decision Log

| Date | Decision | Rationale | Approved |
|------|----------|-----------|----------|
| 2026-06-05 | Schedule after intent 027 | Decomposed files land in the layered shape | Yes |
| 2026-06-05 | Scope P14 to residuals (OrderPhotoQueryService + cleanup) | P25/P11 in 027 already extract CreateFromCartAsync + fan-out | Yes |
| 2026-06-05 | P08 depends on 025 P05 | Global limiter keys on the real client IP | Yes |

## Scope Changes

| Date | Change | Reason | Impact |
|------|--------|--------|--------|

## Ready for Construction

- [x] All requirements documented
- [x] System context defined
- [x] Units decomposed
- [x] Stories created
- [x] Bolts planned
- [x] Human review complete (Checkpoint 3 — approved 2026-06-05)

## Dependencies

After **027** (bolts 064/065 require 059); **063** requires **054** (025 P05 ForwardedHeaders).
