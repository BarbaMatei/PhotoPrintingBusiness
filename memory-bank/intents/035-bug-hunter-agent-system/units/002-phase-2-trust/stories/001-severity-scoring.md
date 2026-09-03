---
id: 001-severity-scoring
unit: 002-phase-2-trust
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 087-phase-2-trust
implemented: false
---

# Story: 001-severity-scoring (guide Prompt 8)

**Workbench seam:** `reviews/lib/records/schema.mjs` — four severity levels and a convergence weight exist; the risk score and the reachability weight do not.

## User Story

**As** the Triage stage
**I want** every confirmed bug scored severity × confidence into a 0–100 risk score
**So that** findings triage in true priority order — a low-confidence Critical doesn't outrank a certain High

## Acceptance Criteria

- [ ] **Given** Prompt 8, **When** built, **Then** skill `severity-scoring` exists, created via skill-creator, and the brief's three test prompts pass (High/High score; Critical/Low ranked below it with explanation; re-score & sort five) *(2026-09: a component that extends the review loop is a script or skill edit under `reviews/lib` / `.claude/skills`, built and tested there; skill-creator applies only to a new standalone skill — FR-1 as amended)*
- [ ] **Given** a bug, **When** scoring, **Then** severity follows the brief's worst-case-impact ladder (Critical/High/Medium/Low), category comes from the fixed list, and `risk_score = severity × confidence` with documented, tunable numeric weights normalized to 0–100
- [ ] **Given** output, **When** emitted, **Then** `{severity, category, risk_score, scoring_rationale}` is returned and the rationale explains the ordering
- [ ] **Given** planned seams, **When** authored, **Then** the formula is explicitly easy to extend — Phase 3 adds **reachability** as a third factor (14b) and lets contract corroboration raise the `confidence` input (24c) — no rewrite needed then

## Technical Notes

- ⚠️ Build by pasting **Prompt 8** from `docs/agent-systems/bug-hunter-build-guide.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.

## Dependencies

### Requires
- (none per the brief; consumes `confidence` from the Verifier at runtime)

### Enables
- bug-verifier step 6; orchestrator Triage wiring (11b); reachability extension (14b)

## Out of Scope

- Reachability factor (story 004 of unit 003 — a seam extension, not part of this build).
