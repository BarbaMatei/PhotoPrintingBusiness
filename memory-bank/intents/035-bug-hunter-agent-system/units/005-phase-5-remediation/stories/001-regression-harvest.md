---
id: 001-regression-harvest
unit: 005-phase-5-remediation
intent: 035-bug-hunter-agent-system
status: ready
priority: should
created: 2026-06-10T10:40:14Z
assigned_bolt: 093-phase-5-remediation
implemented: false
---

# Story: 001-regression-harvest (guide Prompt 30)

**Workbench seam:** `reviews/lib/fix/handback-gates.mjs` — partial: the fixer writes its own red-first test and a test-meaning audit checks it; the gap is a tripwire harvested by someone who did not write the fix.

## User Story

**As** the regression safety net
**I want** the Verifier's proving test kept as a permanent, bug-ID-tagged tripwire in the suite
**So that** a fixed bug can never silently return

## Acceptance Criteria

- [ ] **Given** Prompt 30, **When** built, **Then** skill `regression-harvest` exists, created via skill-creator, and the brief's three test prompts pass (harvest + link a null-deref's failing test; statically-only-confirmed bug noted "no test"; never edits app source) *(2026-09: a component that extends the review loop is a script or skill edit under `reviews/lib` / `.claude/skills`, built and tested there; skill-creator applies only to a new standalone skill — FR-1 as amended)*
- [ ] **Given** a dynamically-confirmed bug, **When** harvesting, **Then** the Verifier's proving test is cleaned up, tagged with the bug ID, and **proposed** for the suite — written **only with owner approval** (the system's ONE allowed new-file write into the codebase, test code only; existing app code never altered)
- [ ] **Given** a statically-only-confirmed bug, **When** harvesting, **Then** "no regression test exists" is recorded honestly
- [ ] **Given** the v3.2 pre-approval checklist, **When** proposing, **Then** the proposal states (and the owner checks): **no network calls, no secrets/live credentials, deterministic, fixtures-only** — the test will run on dev machines and CI with full permissions once approved

## Technical Notes

- ⚠️ Build by pasting **Prompt 30** from `docs/agent-systems/bug-hunter-build-guide.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- Approval flows through `triage-intake` (the front door). Suite targets here: the
  xUnit test projects (API) / Angular specs as applicable.

## Dependencies

### Requires
- bug-verifier (bolt 087) — its proving tests are the input

### Enables
- fix-verification (the gate re-runs harvested tests); fix-proposal validation

## Out of Scope

- Writing tests for unconfirmed bugs; touching app source.
