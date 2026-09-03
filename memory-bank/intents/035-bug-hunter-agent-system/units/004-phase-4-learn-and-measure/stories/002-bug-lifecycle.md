---
id: 002-bug-lifecycle
unit: 004-phase-4-learn-and-measure
intent: 035-bug-hunter-agent-system
status: ready
priority: should
created: 2026-06-10T10:40:14Z
assigned_bolt: 092-phase-4-learn-and-measure
implemented: false
---

# Story: 002-bug-lifecycle (guide Prompt 26)

**Status:** claimed satisfied by `reviews/lib/records/schema.mjs` and `reviews/lib/records/ledger.mjs` — statuses, reopen and lineage (2026-09) — verified in bolt 092-phase-4-learn-and-measure's plan stage before that bolt builds around it.

## User Story

**As** the ledger's integrity over time
**I want** every bug's status managed through defined transitions with evidence
**So that** fixed bugs close honestly, moved bugs keep resolving, and regressions scream

## Acceptance Criteria

- [ ] **Given** Prompt 26, **When** built, **Then** skill `bug-lifecycle` exists, created via skill-creator, and the brief's three test prompts pass (code removed → propose Fixed with evidence; fixed signature returns → regression flag; moved function → location update, still Confirmed) *(2026-09: a component that extends the review loop is a script or skill edit under `reviews/lib` / `.claude/skills`, built and tested there; skill-creator applies only to a new standalone skill — FR-1 as amended)*
- [ ] **Given** transitions, **When** managing, **Then** the allowed set holds: `New → Confirmed | Dismissed`; `Confirmed → Fixed` (evidence from `git-revision-tracking`; from Phase 5, `fix-verification` runs first); `Fixed → Reopened`; location updates when code moves
- [ ] **Given** a reappearing Fixed signature (v3.3), **When** judging, **Then** it is a regression **candidate**, not an identity — hypotheses/lines/trigger conditions are compared; same defect → `Reopened` (high-priority regression), different defect → NEW linked via `related` (no false regression, no false ticket reopen)
- [ ] **Given** self-closing, **When** acting, **Then** it **proposes with evidence** and either auto-applies with an audit trail or requires confirmation (configurable) — never silently; approved transitions apply via `ledger-io`

## Technical Notes

- ⚠️ Build by pasting **Prompt 26** from `docs/agent-systems/bug-hunter-build-guide.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- Phase 5 seam to leave open: the "mark Fixed" step gets gated by `fix-verification`
  (Prompt 31 extends this skill).

## Dependencies

### Requires
- ledger-io (built — the review loop), git-revision-tracking (bolt 087)

### Enables
- curator-agent step (2); fix-verification (P5); issue-sync (Optional B)

## Out of Scope

- Re-running proving tests (fix-verification, P5).
