---
id: 005-triage-intake
unit: 001-phase-1-skeleton
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: null
implemented: false
---

# Story: 005-triage-intake (guide Prompt 5, NEW in v3)

**Status:** satisfied by `reviews/lib/drive/gates.mjs` — the owner gates, parked decisions, and decisions attached to re-found findings (2026-09)

## User Story

**As** the owner reacting to a report
**I want** a defined, provenance-carrying channel for my decisions (dismiss/confirm/approve)
**So that** the learning loop has a real input — and my dismissal *reasons* become the signal suppression-learning later generalizes from

## Acceptance Criteria

- [ ] **Given** Prompt 5, **When** built, **Then** skill `triage-intake` exists, created via skill-creator, and the brief's four test prompts pass (dismissal with reason → recorded with provenance; suppression-pattern approval → activated; reason-less dismissal → rejected; intake during an active run → queued, not racing the merge — v3.2)
- [ ] **Given** decisions in any low-friction form (report decisions field, decisions file, or run-start Q&A), **When** processing, **Then** each is validated (bug ID exists; status change legal) and applied via `ledger-io` (`record_dismissal`, `set_status`, `add_suppression_pattern`)
- [ ] **Given** provenance, **When** recording, **Then** who / when / against-which-commit are attached — and a dismissal **must carry a reason** (bare "dismissed" is rejected)
- [ ] **Given** unprocessed items, **When** finishing, **Then** an updated queue of anything still awaiting a person is emitted — **capped per session, digest-grouped, and age-escalating** (old items rise to the top of the next report) so the queue can't silently starve (v3.2)
- [ ] **Given** a run mid-flight (run-open lockfile), **When** decisions arrive, **Then** intake **queues them for the next safe point** instead of racing the close-merge (v3.2)

## Technical Notes

- ⚠️ Build by pasting **Prompt 5** from `docs/agent-systems/bug-hunter-build-guide.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's four test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- This is the system's ONLY human-feedback entry point (guide convention "Feedback
  has a front door") — Phase 4's suppression-learning and Phase 5's approvals all
  assume it.

## Dependencies

### Requires
- 001-ledger-io

### Enables
- orchestrator's Learn-slot placeholder (apply decisions); suppression-learning (P4);
  fix-proposal / regression-harvest approvals (P5)

## Out of Scope

- Generalizing dismissals into patterns (suppression-learning, P4).
