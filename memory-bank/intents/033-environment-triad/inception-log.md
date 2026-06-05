---
intent: 033-environment-triad
created: 2026-06-05T12:00:00Z
completed: 2026-06-05T12:50:00Z
status: complete
---

# Inception Log: 033-environment-triad

## Overview

**Intent**: Prepare the infrastructure to run in three distinct states — local testing, a deployable dev environment (the first thing ever deployed; a free-experimentation sandbox), and production — with per-environment config separation, secrets strategy, seeding, and a dev→prod promotion path.
**Type**: brown-field / infrastructure readiness (config + docs + tooling, NO deployment)
**Source**: `docs/analysis/ai-workflow-review-2026-06-05.md` §6 — Phase 4 (The environment triad)
**Created**: 2026-06-05T12:00:00Z

## CRITICAL Framing (enforced throughout)

This intent is **infrastructure readiness only — NOT deployment**. Deployment is the FINAL roadmap phase (Phase 6), after stabilization (Phase 3) and EU-readiness (Phase 5). Every FR explicitly defers standing-up to Phase 6; the dev-env tier is *defined and validated locally*, never deployed. No deployment-pressure language was used in any artifact. Bolt 075 carries an explicit deferral note that counters the "deploy next" default ai-workflow-review §6 warns about.

## Builds From (existing assets — extended, not rewritten)

| Asset | Role |
|-------|------|
| `docker-compose.yml` (local) / `docker-compose.prod.yml` (prod) | The two existing tiers; a third (`docker-compose.dev-env.yml`) is added alongside |
| `appsettings.json` + `appsettings.Development.json` | Layered with a new `appsettings.{tier}.json` |
| `.env.example` | Companioned by `.env.dev-env.example` |
| `--seed` / `--seed-dev` (`ProductCatalogSeed` / `DevDataSeed`) | Reused with a per-tier policy + Production guard |
| `.github/workflows/deploy.yml` | Image-tag flow *referenced* by the promotion runbook (not modified) |
| `docs/DEPLOYMENT.md` (incl. §7 migration caveat) | Cross-linked from the promotion runbook + deferral note |

## Artifacts Created

| Artifact | Status | File |
|----------|--------|------|
| Requirements | ✅ | requirements.md |
| System Context | ✅ | system-context.md |
| Units | ✅ | units.md |
| Unit Briefs | ✅ | 3 unit-brief.md |
| Stories | ✅ | 10 story files |
| Bolt Plan | ✅ | bolts 073, 074, 075 |

## Summary

| Metric | Count |
|--------|-------|
| Functional Requirements | 5 |
| Non-Functional Requirements | 8 (across 3 NFR groups) |
| Units | 3 |
| Stories | 10 |
| Bolts Planned | 3 (073–075) |

## Units Breakdown

| Unit | Stories | Bolt | Type |
|------|---------|------|------|
| 001-config-tiers-and-compose | 4 | 073 | simple |
| 002-secrets-and-seeding | 4 | 074 | simple |
| 003-promotion-readiness | 2 | 075 | simple |

## Decision Log

| Date | Decision | Rationale | Approved |
|------|----------|-----------|----------|
| 2026-06-05 | Infrastructure readiness only — no deployment work | Roadmap Phase 4 explicitly precedes deployment (Phase 6) | Self-validated (owner to review) |
| 2026-06-05 | Dev-env tier is Postgres-backed (prod-shaped); only local stays SQLite | A sandbox that hides the SQLite/PG gap defeats its purpose | Self-validated |
| 2026-06-05 | Reuse existing seed classes + add a Production demo-data guard | No parallel seeder; make demo-in-prod structurally impossible | Self-validated |
| 2026-06-05 | Standalone `docker-compose.dev-env.yml`, prod compose untouched | Clarity + regression safety (Q5) | Self-validated (flagged for owner) |
| 2026-06-05 | All units use simple-construction-bolt | Config/infra/docs work, no domain model | Self-validated |

## Scope Changes

| Date | Change | Reason | Impact |
|------|--------|--------|--------|

## Self-Validation (Checkpoints 1–4)

No human was available mid-run; checkpoints were self-validated for the owner's later review.

- **Checkpoint 1 (clarifying questions)**: Captured as 5 Open Questions (tier name, dev-env email, eventual secret store, promotion-doc home, standalone vs overlay compose). None block inception; all are owner decisions.
- **Checkpoint 2 (requirements)**: 5 FRs, all with binary acceptance criteria. The readiness/NOT-deployment boundary is restated in the overview, in a dedicated Framing section, and inside every FR. NFRs have concrete targets (boot-validation parity, prod-config-unchanged, zero parallel seeders).
- **Checkpoint 3 (artifacts)**: 3 units / 10 stories / 3 bolts. Every FR maps to a unit; every story maps to a bolt; dependency frontmatter present (073→074→075, no external requires). INVEST satisfied.
- **Checkpoint 4 (ready for construction)**: Yes. No hard external bolt dependencies (builds from shipped assets). Owner review of the 5 open questions — especially the tier name (Q1) — recommended before construction.

### Concerns flagged for the owner
- Tier naming (Q1) is the one decision that ripples across appsettings, compose, and docs — worth settling before bolt 073 starts.
- Bolt 075 is documentation-only; its acceptance hinges on *tone* (no deploy-pressure). A human reviewer should confirm the framing reads as intended.

## Ready for Construction

- [x] All requirements documented
- [x] System context defined
- [x] Units decomposed
- [x] Stories created
- [x] Bolts planned
- [ ] Human review complete (Checkpoint 3) — pending owner review

## Next Steps

1. Owner reviews artifacts + the 5 open questions (settle the tier name first).
2. Construction order: 073 → 074 → 075.
3. Deployment remains deferred to roadmap Phase 6 — this intent does NOT trigger it.

## Dependencies

Internal only: 073 (config tiers) → 074 (secrets + seeding) → 075 (promotion readiness). No external bolt dependencies — builds from already-shipped infrastructure assets.
