---
intent: 027-architectural-layering
created: 2026-06-05T09:00:00Z
completed: 2026-06-05T10:00:00Z
status: complete
---

# Inception Log: 027-architectural-layering

## Overview

**Intent**: Codify Presentation / Application / Domain / Infrastructure layering inside the single PhotoPrint.API assembly (no new csproj), introduce the Abstractions/ convention, lock in the no-repository policy, add the handler-per-use-case pattern, and record the "no four-project split" ADR.
**Type**: brown-field / structural refactor (zero behaviour change)
**Source**: `docs/analysis/architect-review-2026-06-03.md` — Group 3 (P21, P22, P23, P24, P25; folds P06, P11, P16)
**Created**: 2026-06-05T09:00:00Z

## Proposals Covered

| Proposal | FR | Priority | Note |
|----------|----|----------|------|
| P22 — "No clean-arch split" ADR | FR-1 | Could | Ship first |
| P21 — Four-layer folder structure | FR-2 | Should | Folds P06 + P16 |
| P06 — Services feature folders | FR-3 | Should | Folded → P21-PR4 |
| P16 — Domain layer extraction | FR-4 | Could | Folded → P21-PR1 |
| P23 — Abstractions/ subfolders | FR-5 | Should | |
| P24 — No-repository policy + IQueryable analyzer | FR-6 | Should | |
| P25 — Handler-per-use-case (no MediatR) | FR-7 | Should | Folds P11 |
| P11 — OrderPaidEventDispatcher | FR-8 | Should | Folded → P25 canonical handler |

## Artifacts Created

| Artifact | Status | File |
|----------|--------|------|
| Requirements | ✅ | requirements.md |
| System Context | ✅ | system-context.md |
| Units | ✅ | units.md |
| Unit Briefs | ✅ | 3 unit-brief.md |
| Stories | ✅ | 11 story files |
| Bolt Plan | ✅ | bolts 059, 060, 061 |

## Summary

| Metric | Count |
|--------|-------|
| Functional Requirements | 8 (P21,P22,P23,P24,P25 + folded P06,P11,P16) |
| Non-Functional Requirements | 4 |
| Units | 3 |
| Stories | 11 |
| Bolts Planned | 3 (059–061) |

## Units Breakdown

| Unit | Stories | Bolt | Type |
|------|---------|------|------|
| 001-layering-foundation | 5 | 059 | simple |
| 002-conventions-and-policy | 2 | 060 | simple |
| 003-handler-pattern | 4 | 061 | simple |

## Decision Log

| Date | Decision | Rationale | Approved |
|------|----------|-----------|----------|
| 2026-06-05 | Fold P06, P11, P16 INTO this intent | P21 subsumes P06/P16; P25 subsumes P11 (per review) | Yes (Checkpoint 1) |
| 2026-06-05 | Folder + namespace layering, NOT four csproj | Pre-deployment single-team monolith; P22 ADR records it | Yes |
| 2026-06-05 | Lockstep with intent 028 (tests) | Every structural PR breaks ~25 test files otherwise | Yes |

## Scope Changes

| Date | Change | Reason | Impact |
|------|--------|--------|--------|
| 2026-06-05 | Absorbed first-pass P06, P11, P16 | Superseded by P21/P25 in second-pass review | -3 standalone intents |

## Ready for Construction

- [x] All requirements documented
- [x] System context defined
- [x] Units decomposed
- [x] Stories created
- [x] Bolts planned
- [x] Human review complete (Checkpoint 3 — approved 2026-06-05)

## Dependencies

Lockstep with **028-test-architecture**. Sequence P22 → P21-PR1..PR5 (bolt 059) → P23/P24 (bolt 060) → P25 (bolt 061). Build + test green after every PR.
