---
intent: 032-regression-and-e2e-stabilization
created: 2026-06-05T11:00:00Z
completed: 2026-06-05T11:50:00Z
status: complete
---

# Inception Log: 032-regression-and-e2e-stabilization

## Overview

**Intent**: A full regression pass plus a comprehensive end-to-end testing module covering the ENTIRE application — every major user journey, not just the three smoke tests bolt 066 introduces.
**Type**: brown-field / stabilization (test + verification, no new product behaviour)
**Source**: `docs/analysis/ai-workflow-review-2026-06-05.md` §6 — Phase 3 (Stabilize)
**Created**: 2026-06-05T11:00:00Z

## Builds On (declared dependencies — NOT re-planned)

| Existing bolt | Role | How this intent uses it |
|---------------|------|--------------------------|
| 066-ci-quality-gates (intent 030) | Playwright foundation + `playwright-e2e.yml` + 3 smoke specs + compose boot | Extended — same runner/harness, grown to full coverage. `requires_bolts` on 070 + 071 |
| 062-test-infrastructure (intent 028) | Shared test factory + fluent Builders | Reused for e2e fixtures/data. `requires_bolts` on 070 |
| 057-architecture-and-standards-docs (intent 026) | KNOWN_FAILURES.md | Soft dependency for triage. `requires_bolts` (soft) on 072 |
| 047/048 (coupons), 068/069 (refunds) | Feature targets of gated specs | Journeys authored but gated (`test.fixme`); NOT re-implemented |

## Artifacts Created

| Artifact | Status | File |
|----------|--------|------|
| Requirements | ✅ | requirements.md |
| System Context | ✅ | system-context.md |
| Units | ✅ | units.md |
| Unit Briefs | ✅ | 3 unit-brief.md |
| Stories | ✅ | 15 story files |
| Bolt Plan | ✅ | bolts 070, 071, 072 |

## Summary

| Metric | Count |
|--------|-------|
| Functional Requirements | 4 |
| Non-Functional Requirements | 9 (across 4 NFR groups) |
| Units | 3 |
| Stories | 15 |
| Bolts Planned | 3 (070–072) |

## Units Breakdown

| Unit | Stories | Bolt | Type |
|------|---------|------|------|
| 001-e2e-data-strategy | 4 | 070 | simple |
| 002-e2e-journey-coverage | 8 | 071 | simple |
| 003-regression-methodology | 3 | 072 | simple |

## Decision Log

| Date | Decision | Rationale | Approved |
|------|----------|-----------|----------|
| 2026-06-05 | Build on bolts 066 + 062, do not re-plan them | They already ship the Playwright foundation + Builders; duplicating would fork the test stack | Self-validated (owner to review) |
| 2026-06-05 | Coupon/refund journeys authored but gated (`test.fixme`) | Features (047/048, 068/069) may not have shipped; un-gating is a trivial follow-up | Self-validated |
| 2026-06-05 | All units use simple-construction-bolt | Test/CI/methodology work, no domain model | Self-validated |
| 2026-06-05 | Unit 002 keeps 8 stories in one bolt despite the 5–6 soft cap | Thin, parallel, domain-sliced specs over one shared fixture layer; splitting would be artificial | Self-validated (flagged for owner) |
| 2026-06-05 | E2e runs against real Postgres (not in-memory/SQLite) | Surfaces the DEPLOYMENT.md §7 provider gap; tests production-shaped behaviour | Self-validated |

## Scope Changes

| Date | Change | Reason | Impact |
|------|--------|--------|--------|

## Self-Validation (Checkpoints 1–4)

No human was available mid-run; checkpoints were self-validated for the owner's later review.

- **Checkpoint 1 (clarifying questions)**: Captured as 5 Open Questions in requirements.md (PR/full tier split, full-suite trigger, checklist location, Google e2e approach, real-PG integration profile ownership). None block inception; all are owner decisions.
- **Checkpoint 2 (requirements)**: 4 FRs, all with binary acceptance criteria; NFRs have concrete targets (time budgets, 3-consecutive-green, zero fixed sleeps). Constraints explicitly forbid duplicating bolts 066/062 and re-implementing coupons/refunds.
- **Checkpoint 3 (artifacts)**: 3 units / 15 stories / 3 bolts. Every FR maps to a unit; every story maps to a bolt; dependency frontmatter present on all bolts. INVEST: stories are independent within their fixture layer, valuable, estimable, small, testable.
- **Checkpoint 4 (ready for construction)**: Yes, subject to bolts 066 + 062 shipping first (hard dependencies). Owner review of the open questions recommended before construction.

### Concerns flagged for the owner
- Unit 002's 8 stories exceed the 5–6 soft cap (justified above, but the owner may prefer a 2-bolt split).
- Bolt 072's value (the dated baseline) is strongest only after bolts 066, 062, and the feature backlog are substantially shipped — its scheduling sits late in Phase 3 by design.

## Ready for Construction

- [x] All requirements documented
- [x] System context defined
- [x] Units decomposed
- [x] Stories created
- [x] Bolts planned
- [ ] Human review complete (Checkpoint 3) — pending owner review

## Next Steps

1. Owner reviews artifacts + the 5 open questions.
2. Ensure bolts 066 + 062 are shipped (hard dependencies) before scheduling 070.
3. Construction order: 070 → 071 → 072.

## Dependencies

Execution order: 070 (data strategy) → 071 (journeys + CI) → 072 (regression baseline). Hard external dependencies: bolts 066 + 062. Soft: bolt 057 (KNOWN_FAILURES.md) for 072; bolts 047/048 + 068/069 to un-gate coupon/refund specs.
