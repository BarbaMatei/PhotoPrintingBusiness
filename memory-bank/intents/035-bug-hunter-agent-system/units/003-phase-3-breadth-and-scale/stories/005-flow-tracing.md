---
id: 005-flow-tracing
unit: 003-phase-3-breadth-and-scale
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 088-phase-3-map-and-reachability
implemented: false
---

# Story: 005-flow-tracing (guide Prompt 15)

**Workbench seam:** `reviews/lib/discovery-review.wf.js` and the lens prompts — partial by design: the lenses trace flows by prompt. No bolt work planned.

## User Story

**As** the shared procedure behind the flow and concurrency hunters
**I want** one rigorous way to walk a single flow top-down and inspect every handoff
**So that** integration/state bugs hiding between layers get found consistently

## Acceptance Criteria

- [ ] **Given** Prompt 15, **When** built, **Then** skill `flow-tracing` exists, created via skill-creator, and the brief's three test prompts pass (checkout trace flags dropped errors; password-reset authz per step; partial-commit transaction found) *(2026-09: a component that extends the review loop is a script or skill edit under `reviews/lib` / `.claude/skills`, built and tested there; skill-creator applies only to a new standalone skill — FR-1 as amended)*
- [ ] **Given** a flow from the map + `code-index`, **When** tracing, **Then** the real call path is followed and each hop checks: input validation/sanitization, authn/authz, layer **contracts** (types, nullability, units, invariants), error propagation vs swallowing, **state/transaction** correctness (partial writes, missing rollback, ordering), unhappy paths (timeouts, empty/malformed results, dependency failure)
- [ ] **Given** output, **When** emitting, **Then** one candidate per suspect handoff tagged with `flow_position`; one flow at a time; coverage summarized to the ledger

## Technical Notes

- ⚠️ Build by pasting **Prompt 15** from `docs/agent-systems/bug-hunter-build-guide.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- Prime targets in this repo: checkout (cart→order→payment→webhook→status machine),
  uploads (validate→store→thumbnail→cleanup jobs), invoicing (order→UBL→ANAF→retry).

## Dependencies

### Requires
- 002-code-index, 001-app-mapping

### Enables
- flow-tracer-agent (P17); concurrency-auditor-agent (P22)

## Out of Scope

- Dispatch/prioritization (the agents own that).
