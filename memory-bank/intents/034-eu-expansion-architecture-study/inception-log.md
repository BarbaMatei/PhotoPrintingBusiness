---
intent: 034-eu-expansion-architecture-study
created: 2026-06-05T12:57:50Z
completed: 2026-06-05T13:14:12Z
status: complete
---

# Inception Log: 034-eu-expansion-architecture-study

## Overview

**Intent**: Research-only intent (roadmap Phase 5 prep) that decides — on evidence — the
EU fulfillment model and site/URL architecture, then turns the owner's decision into
implementation brief(s). Bolts are spike-bolts (knowledge out, no production code).
**Type**: green-field (research/spike)
**Created**: 2026-06-05
**Source feed**: `docs/planning/eu-expansion-research-brief-2026-06-05.md`

## Artifacts Created

| Artifact | Status | File |
|----------|--------|------|
| Requirements | ✅ | requirements.md |
| System Context | ✅ | system-context.md |
| Units | ✅ | units.md / units/{unit}/unit-brief.md (3 units) |
| Stories | ✅ | units/{unit}/stories/*.md (10 stories) |
| Bolt Plan | ✅ | memory-bank/bolts/076–084/bolt.md (9 bolts) |

## Summary

| Metric | Count |
|--------|-------|
| Functional Requirements | 11 |
| Non-Functional Requirements | 13 (across 5 categories) |
| Units | 3 |
| Stories | 10 |
| Bolts Planned | 9 (076–084) |

## Units Breakdown

| Unit | Stories | Bolts | Priority |
|------|---------|-------|----------|
| 001-research-tracks | 7 | 7 (076–082, spike) | Must |
| 002-synthesis-and-decision | 2 | 1 (083, spike) | Must |
| 003-implementation-briefs | 1 | 1 (084, simple/docs) | Must |

## Decision Log

| Date | Decision | Rationale | Approved |
|------|----------|-----------|----------|
| 2026-06-05 | Create intent 034 from EU expansion research brief | Phase 5 prep; two blocking questions need evidence before implementation | Yes (owner fed brief) |
| 2026-06-05 | Intent is research-only; spike-bolts; zero production code | Brief §6 constraints + spike-bolt rules | Yes |
| 2026-06-05 | Target markets: compare both tiers (HU/BG + DE/FR/IT/ES) | Checkpoint 1 owner input | Yes |
| 2026-06-05 | Brand: one brand EU-wide | Checkpoint 1 owner input | Yes |
| 2026-06-05 | Fulfillment: ship everything from Romania (partner = fallback only) | Checkpoint 1 owner input — settles the dominant question; single fulfillment origin | Yes |
| 2026-06-05 | Currency: local currencies (PLN/HUF/CZK/BGN + EUR) | Checkpoint 1 owner input — multi-currency now first-class | Yes |

## Scope Changes

| Date | Change | Reason | Impact |
|------|--------|--------|--------|

## Ready for Construction

**Checklist**:
- [x] All requirements documented
- [x] System context defined
- [x] Units decomposed
- [x] Stories created for all units
- [x] Bolts planned
- [x] Human review complete (Checkpoints 1–3 approved by owner 2026-06-05)

## Next Steps

1. First unit: **001-research-tracks** — bolts 076–082 are a wave-parallel batch (docs-only, conflict-free).
2. Plan the parallel waves: run the **bolt-parallel-planner** agent over bolts 076–082 (then 083, then 084).
3. Construction entry: `/specsmd-construction-agent --unit="001-research-tracks"` (each research bolt should itself fan out parallel web researchers + verifier agents per the method requirement, FR-1).

## Dependencies

Hard internal ordering (from brief §8): Unit 1 (research tracks) → Unit 2 (synthesis &
decision) → Unit 3 (implementation briefs). Each consumes the previous.
