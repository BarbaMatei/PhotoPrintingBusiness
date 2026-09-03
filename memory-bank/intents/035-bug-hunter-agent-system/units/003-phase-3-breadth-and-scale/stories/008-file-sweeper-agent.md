---
id: 008-file-sweeper-agent
unit: 003-phase-3-breadth-and-scale
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 089-phase-3-specialists-a
implemented: false
---

# Story: 008-file-sweeper-agent (guide Prompt 18 — agent-as-skill)

**Workbench seam:** `reviews/lib/discovery-review.wf.js` — partial: the lenses sweep, but without a tools-first pass. No bolt work planned.

## User Story

**As** the Hunt slot's bottom-up specialist
**I want** an exhaustive per-file pass that runs cheap tools first
**So that** local defects get swept up without re-grinding deeply-covered files

## Acceptance Criteria

- [ ] **Given** Prompt 18, **When** built, **Then** skill `file-sweeper-agent` exists (agent-as-skill), created via skill-creator, and the brief's three test prompts pass (sweep five files; linter-first then add what it missed; skip already-deep files and say which) *(2026-09: a component that extends the review loop is a script or skill edit under `reviews/lib` / `.claude/skills`, built and tested there; skill-creator applies only to a new standalone skill — FR-1 as amended)*
- [ ] **Given** assigned files, **When** sweeping, **Then** deeply-covered files are skipped/shallow-passed (ledger coverage), deterministic findings come first via `tool-ingest`, then inspection for: null handling, boundaries, wrong operators, type coercion, resource leaks, unhandled exceptions, dead code hiding logic errors, hardcoded secrets, unsafe API usage
- [ ] **Given** emission, **When** done, **Then** `deduplication` first, candidates only, coverage updated

## Technical Notes

- ⚠️ Build by pasting **Prompt 18** from `docs/agent-systems/bug-hunter-build-guide.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.

## Dependencies

### Requires
- 002-code-index; tool-ingest (bolt 087); deduplication, bug-documentation,
  ledger-io (built — the review loop)

### Enables
- orchestrator specialist dispatch (24d); hunters-contract-ext (24b)

## Out of Scope

- Flow-level reasoning (flow-tracer-agent).
