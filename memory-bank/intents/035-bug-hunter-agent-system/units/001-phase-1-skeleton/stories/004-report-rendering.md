---
id: 004-report-rendering
unit: 001-phase-1-skeleton
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 085-phase-1-skeleton-core
implemented: false
---

# Story: 004-report-rendering (guide Prompt 4)

## User Story

**As** the owner reading run results
**I want** a fresh per-run Markdown report, risk-ordered and floored
**So that** first contact is signal (High/Medium foregrounded) and never a false-positive flood — while nothing is hidden

## Acceptance Criteria

- [ ] **Given** Prompt 4, **When** built, **Then** skill `report-rendering` exists, created via skill-creator, and the brief's three test prompts pass (High/Medium/Low mix → Low in appendix; zero-new-bugs run; second run writes a NEW file)
- [ ] **Given** a run, **When** rendering, **Then** a NEW file `bug-hunting/reports/bug-report-run-<NN>-<YYYYMMDD-HHMM>.md` is written (never append/overwrite), with Run Summary (scope, counts by severity, uncovered areas, explicit zero-new-bugs note when applicable) then bugs sorted by risk descending
- [ ] **Given** the **reporting floor** (v3, axis fixed in v3.2), **When** structuring, **Then** the floor is on **confidence**: High/Medium-confidence findings render in the body with all three audience sections; **Low-confidence** findings land in a separate "Also flagged — low confidence" appendix regardless of severity, never interleaved — and any **Critical/High-severity** item parked there gets a mandatory one-line body callout; optional top-N cap and per-run report budget are supported — prominence changes, nothing is deleted (everything stays in the ledger)
- [ ] **Given** the v3.2 secret-safety rule, **When** rendering, **Then** only the record's redacted evidence appears — never raw secret material
- [ ] **Given** the injection carrier (v3.4, review I9), **When** a rendered record carries `injection_suspected`, **Then** the report surfaces that flag on the finding
- [ ] **Given** a run with observations, **When** rendering, **Then** the optional non-defect Observations section is supported (SARIF twin explicitly NOT built now — Optional A later)

## Technical Notes

- ⚠️ Build by pasting **Prompt 4** from `docs/agent-systems/bug-hunter-build-guide-v3.6.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- Reports dir is pinned by requirements D3 (`bug-hunting/reports/`).

## Dependencies

### Requires
- 002-bug-documentation (records are its input)

### Enables
- orchestrator's Report slot; Optional A (SARIF) extends this skill at its seam later

## Out of Scope

- SARIF output (Optional A); ticket creation (Optional B).
