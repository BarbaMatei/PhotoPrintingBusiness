---
id: 005-orchestrator-verify-wiring
unit: 002-phase-2-trust
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 087-phase-2-trust
implemented: false
---

# Story: 005-orchestrator-verify-wiring (guide Prompt 11b — EXTENSION)

**Status:** satisfied by `reviews/lib/drive/rows.mjs` — the pass router’s rows (2026-09)

## User Story

**As** the six-slot pipeline
**I want** the Verify and Triage slots pointed at the real Verifier and scorer (+ commit tracking at open/close)
**So that** Phase 2's trust capabilities flow through the existing structure with zero restructuring

## Acceptance Criteria

- [ ] **Given** Prompt 11b, **When** applied, **Then** the existing `orchestrator` skill is **re-opened and extended at its seams** (no restructuring), via skill-creator, and the brief's three test prompts pass (run carries confidence + risk scores; Low still reported in appendix; deleted bug's code proposed Fixed at close)
- [ ] **Given** the Verify slot, **When** extended, **Then** the Phase 1 pass-through is replaced by a call to `bug-verifier`, and the report drops the blanket "unverified" label in favor of per-finding confidence
- [ ] **Given** the Triage slot, **When** extended, **Then** `severity-scoring` runs after `deduplication` so bugs order by real risk
- [ ] **Given** run open/close, **When** extended, **Then** the commit SHA is captured at open and `git-revision-tracking` reconciliation runs at close (proposing fixed/moved updates)
- [ ] **Given** the additive rule (NFR-2), **When** done, **Then** all Phase 1 test prompts still pass

## Technical Notes

- ⚠️ This is an **extension brief**: paste **Prompt 11b** from
  `docs/agent-systems/bug-hunter-build-guide.md` into the **skill-creator** skill (`Skill` tool →
  `skill-creator:skill-creator`) to re-open `orchestrator` and top up the named
  seams. Run the brief's tests AND re-run Prompt 7's tests. STOP and report if
  skill-creator is unavailable.

## Dependencies

### Requires
- 003-bug-verifier, 001-severity-scoring, 004-git-revision-tracking (this unit);
  the Phase 1 orchestrator (built — the review loop)

### Enables
- The Phase 2 milestone: trustworthy end-to-end runs

## Out of Scope

- Specialist dispatch, cost control, oracle wiring (24d, Phase 3).
