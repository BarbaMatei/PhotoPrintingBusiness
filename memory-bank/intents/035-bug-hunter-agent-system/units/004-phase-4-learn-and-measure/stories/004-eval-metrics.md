---
id: 004-eval-metrics
unit: 004-phase-4-learn-and-measure
intent: 035-bug-hunter-agent-system
status: ready
priority: should
created: 2026-06-10T10:40:14Z
assigned_bolt: 092-phase-4-learn-and-measure
implemented: false
---

# Story: 004-eval-metrics (guide Prompt 28)

## User Story

**As** the quality dashboard
**I want** each run scored against the corpus — precision, recall, F1, FP-rate, with trends
**So that** a change that quietly hurts detection is visible before it compounds

## Acceptance Criteria

- [ ] **Given** Prompt 28, **When** built, **Then** skill `eval-metrics` exists, created via skill-creator, and the brief's three test prompts pass (recall vs corpus; FP-rate from dismissals with the corpus-miss explanation; five-run trend)
- [ ] **Given** the measurement nuance (binding), **When** scoring, **Then** a reported bug NOT in the corpus is **not** auto-counted a false positive (the corpus is incomplete): **recall** measures against the **seeded** corpus (reliable); **precision** is proxied by the **human-dismissal rate**; F1 computed; each metric's limits stated
- [ ] **Given** trend tracking, **When** recording, **Then** per-run metrics + trend (improving/flat/regressing) persist so a post-change drop is visible
- [ ] **Given** variance control (v3.2, aligned with the knowledge builder's eval policy), **When** running evals, **Then** the **model/version and settings are recorded per run and trends compare like-for-like only** — pinning isn't operationally meaningful in this environment; honesty about comparability replaces a pin
- [ ] **Given** eval isolation (v3.2), **When** scoring, **Then** eval runs read/write only the throwaway `bug-hunting/eval-runs/<timestamp>/` ledger
- [ ] **Given** injection-resistance as a metric (v3.4, review I9), **When** scoring, **Then** each adversarial fixture's full `expected_disposition` is graded (found AND flagged AND not obeyed — target zero failures, tracked in the trend like the KB's firewall leak rate); a run that locates the seeded bug but **obeys** the suppression comment scores as a regression, not a pass

## Technical Notes

- ⚠️ Build by pasting **Prompt 28** from `docs/agent-systems/bug-hunter-build-guide.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.

## Dependencies

### Requires
- 003-eval-corpus; run results via ledger-io

### Enables
- curator-agent step (3) + health summary

## Out of Scope

- Acting on trends (Curator summarizes; owner decides).
