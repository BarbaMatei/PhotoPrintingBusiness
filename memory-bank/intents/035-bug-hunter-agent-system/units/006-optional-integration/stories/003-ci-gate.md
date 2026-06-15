---
id: 003-ci-gate
unit: 006-optional-integration
intent: 035-bug-hunter-agent-system
status: ready
priority: could
created: 2026-06-10T10:40:14Z
assigned_bolt: 094-optional-integration
implemented: false
---

# Story: 003-ci-gate (guide Optional C)

## User Story

**As** the CI pipeline
**I want** a configurable pass/fail policy over a run's findings, compared against a baseline
**So that** only newly-introduced serious bugs fail a build — pre-existing findings never block

## Acceptance Criteria

- [ ] **Given** Optional C, **When** built, **Then** skill `ci-gate` exists, created via skill-creator, and the brief's three test prompts pass (one new High → fail with summary; all pre-existing → pass; policy tightened to Medium → re-evaluate)
- [ ] **Given** the default policy, **When** gating, **Then** any NEW Critical/High fails, new Medium warns, Low ignored — all configurable
- [ ] **Given** the baseline (prior run/commit via `ledger-io`), **When** comparing, **Then** only newly-introduced bugs can fail the build
- [ ] **Given** output, **When** finishing, **Then** a status, an exit code, and a short PR-comment summary of blocking findings are emitted — rendering only redacted evidence, never raw secret material (v3.3)

## Technical Notes

- ⚠️ Build by pasting **Optional C** from `docs/agent-systems/bug-hunter-build-guide.md` into
  the **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- Repo reality: GitHub Actions CI exists (bolt 040) — wiring the gate into a workflow
  job is buildable today; activation remains the owner's adoption call.

## Dependencies

### Requires
- report-rendering (bolt 085), severity-scoring (bolt 087); baseline via ledger-io

### Enables
- Gated PR/CI workflow

## Out of Scope

- Redesigning CI; auto-blocking merges outside the configured policy.
