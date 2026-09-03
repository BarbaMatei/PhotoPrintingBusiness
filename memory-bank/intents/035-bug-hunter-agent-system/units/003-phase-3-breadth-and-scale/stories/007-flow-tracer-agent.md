---
id: 007-flow-tracer-agent
unit: 003-phase-3-breadth-and-scale
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 089-phase-3-specialists-a
implemented: false
---

# Story: 007-flow-tracer-agent (guide Prompt 17 — agent-as-skill)

**Workbench seam:** `reviews/lib/discovery-review.wf.js` — partial: the lenses hunt top-down already. No bolt work planned.

## User Story

**As** the Hunt slot's top-down specialist
**I want** flows iterated in priority order with the rigorous tracing procedure
**So that** integration/contract/state candidates surface from the riskiest flows first

## Acceptance Criteria

- [ ] **Given** Prompt 17, **When** built, **Then** skill `flow-tracer-agent` exists (agent-as-skill), created via skill-creator, and the brief's three test prompts pass (hunt the 3 highest-risk flows; skip flows already in the ledger; report coverage + depth)
- [ ] **Given** assigned flows, **When** hunting, **Then** highest risk class first, `flow-tracing` per flow, `deduplication` before emitting, **candidates only** (no confirm/score — the Verifier's job), coverage updated
- [ ] **Given** conventions, **When** operating, **Then** every plausible lead surfaced; strictly read-only

## Technical Notes

- ⚠️ Build by pasting **Prompt 17** from `docs/agent-systems/bug-hunter-build-guide.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- 24b later adds the oracle seam (contract-contradiction candidates) — leave the
  per-hop examination step recognizable as that seam.

## Dependencies

### Requires
- 005-flow-tracing; deduplication, bug-documentation, ledger-io (built — the review loop);
  002-code-index

### Enables
- orchestrator specialist dispatch (24d); hunters-contract-ext (24b)

## Out of Scope

- Verification/scoring; map building.
