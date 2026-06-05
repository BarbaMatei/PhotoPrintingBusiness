---
intent: 028-test-architecture
created: 2026-06-05T09:00:00Z
completed: 2026-06-05T10:00:00Z
status: complete
---

# Inception Log: 028-test-architecture

## Overview

**Intent**: Promote a shared IntegrationTestBase/TestApplicationFactory, add fluent Builders, reclassify the 25 misnamed "unit" tests, and finish adopting TimeProvider across the 35 older files.
**Type**: brown-field / test refactor (zero behaviour change)
**Source**: `docs/analysis/architect-review-2026-06-03.md` — Group 4 (P27, P28)
**Created**: 2026-06-05T09:00:00Z

## Proposals Covered

| Proposal | FR | Priority |
|----------|----|----------|
| P28 — TimeProvider adoption (ship first) | FR-1 | Should |
| P27 — Shared factory base + Builders + reclassification | FR-2 | Should |

## Artifacts Created

| Artifact | Status | File |
|----------|--------|------|
| Requirements | ✅ | requirements.md |
| System Context | ✅ | system-context.md |
| Units | ✅ | units.md |
| Unit Briefs | ✅ | units/001-test-infrastructure/unit-brief.md |
| Stories | ✅ | 4 story files |
| Bolt Plan | ✅ | memory-bank/bolts/062-test-infrastructure/bolt.md |

## Summary

| Metric | Count |
|--------|-------|
| Functional Requirements | 2 |
| Non-Functional Requirements | 4 |
| Units | 1 |
| Stories | 4 |
| Bolts Planned | 1 (062) |

## Units Breakdown

| Unit | Stories | Bolt | Type |
|------|---------|------|------|
| 001-test-infrastructure | 4 | 062 | simple |

## Decision Log

| Date | Decision | Rationale | Approved |
|------|----------|-----------|----------|
| 2026-06-05 | Ship P28 before P27 | TimeProvider adds ctor params; Builders hide them | Yes |
| 2026-06-05 | Reclassify by folder move (Option A), not repositories | Intent 027 P24 rejects repositories | Yes |
| 2026-06-05 | Lockstep / interleaved with intent 027 | Avoid writing the base against the old folder shape | Yes |

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

Lockstep with **027-architectural-layering** (bolts 059–061 ↔ bolt 062, interleaved).
