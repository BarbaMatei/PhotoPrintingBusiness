---
id: 006-general-hunter
unit: 001-phase-1-skeleton
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 086-phase-1-skeleton-agents
implemented: false
---

# Story: 006-general-hunter (guide Prompt 6 — agent-as-skill)

## User Story

**As** the Hunt slot in Phase 1
**I want** one combined hunter that sweeps files AND traces the obvious flows
**So that** the skeleton finds real candidate bugs before any specialist exists

## Acceptance Criteria

- [ ] **Given** Prompt 6, **When** built, **Then** skill `general-hunter` exists (an agent built as a skill defining its procedure), created via skill-creator, and the brief's three test prompts pass (scan a small repo; only-new-vs-ledger; coverage report)
- [ ] **Given** no formal map yet, **When** hunting, **Then** entry points are identified by convention (routes/controllers/`main`/handlers) and main flows traced top-down (validation, auth, error handling, state/transactions per hop) AND files swept for local defects (null handling, boundaries, wrong operators, type coercion, resource leaks, unhandled exceptions, hardcoded secrets)
- [ ] **Given** emission, **When** producing output, **Then** `deduplication` runs first; candidates use the shared shape with `category_guess`; **every plausible lead is surfaced** (no self-censoring — Verify gates); coverage is updated in the ledger
- [ ] **Given** the conventions, **When** operating, **Then** strictly read-only on source

## Technical Notes

- ⚠️ Build by pasting **Prompt 6** from `docs/agent-systems/bug-hunter-build-guide-v3.6.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- Repo grounding: .NET controllers/minimal APIs + BackgroundServices + Angular
  routes are the convention-based entry points here.
- Phase 3 will NOT rewrite this skill — the orchestrator just stops leaning on it
  (fallback role) and 24b adds the oracle seam.

## Dependencies

### Requires
- 003-deduplication, 002-bug-documentation (candidate shape), 001-ledger-io

### Enables
- orchestrator's Hunt slot

## Out of Scope

- Confirming/scoring candidates (Verify slot); specialist techniques (P3).
