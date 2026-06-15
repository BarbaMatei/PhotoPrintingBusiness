---
id: 005-curator-agent
unit: 004-phase-4-learn-and-measure
intent: 035-bug-hunter-agent-system
status: ready
priority: should
created: 2026-06-10T10:40:14Z
assigned_bolt: 092-phase-4-learn-and-measure
implemented: false
---

# Story: 005-curator-agent (guide Prompt 29 — agent-as-skill, fills the Learn slot)

## User Story

**As** the Learn slot
**I want** an agent that learns from feedback, keeps the ledger honest, and measures quality after each run
**So that** the system gets better with use instead of accumulating noise

## Acceptance Criteria

- [ ] **Given** Prompt 29, **When** built, **Then** skill `curator-agent` exists (agent-as-skill), created via skill-creator, and the brief's three test prompts pass (curate after a run; record metrics + trend call-out; health summary)
- [ ] **Given** a run close (or schedule), **When** curating, **Then** the four steps run: (1) **Learn** — pull new `triage-intake` dismissals, run `suppression-learning`, present proposals for approval (each validated vs Confirmed), activate approved; (2) **Reconcile** — `bug-lifecycle` (self-close with evidence, update moved, flag regressions); (3) **Measure** — `eval-corpus` + `eval-metrics`, record trend, call out drops coinciding with recent changes; (4) **Summarize** — short health report (FP-rate/recall trends, open bugs by severity, new patterns, regressions, **ledger size + growth with threshold callout — the compaction watcher (v3.3)**, a **model-changed flag that schedules an eval (v3.3)**, recommendation)
- [ ] **Given** boundaries, **When** operating, **Then** read-only on source; writes only ledger + summary

## Technical Notes

- ⚠️ Build by pasting **Prompt 29** from `docs/agent-systems/bug-hunter-build-guide.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.

## Dependencies

### Requires
- 001-suppression-learning, 002-bug-lifecycle, 003-eval-corpus, 004-eval-metrics;
  ledger-io (bolt 085)

### Enables
- 006-orchestrator-learn-ext (29b)

## Out of Scope

- Closing bugs on fix proof (fix-verification gate, P5).
