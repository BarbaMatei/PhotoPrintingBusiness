---
id: 001-report-rendering-sarif-ext
unit: 006-optional-integration
intent: 035-bug-hunter-agent-system
status: ready
priority: could
created: 2026-06-10T10:40:14Z
assigned_bolt: 094-optional-integration
implemented: false
---

# Story: 001-report-rendering-sarif-ext (guide Optional A — EXTENSION)

**Workbench seam:** `reviews/lib/records/render-records.mjs`.

## User Story

**As** machine consumers of run results (CI, code-scanning UIs)
**I want** a SARIF twin emitted beside every Markdown report
**So that** the same findings flow into standard tooling without a second source of truth

## Acceptance Criteria

- [ ] **Given** Optional A, **When** applied, **Then** `report-rendering` is **re-opened and extended** via skill-creator, and the brief's test passes (3-bug run's SARIF twin with agreeing counts)
- [ ] **Given** rendering, **When** a run reports, **Then** a `run-NN.sarif` file is emitted where each bug is one SARIF `result`: ruleId = category; level mapped (Critical/High → error, Medium → warning, Low → note); message = `plain_summary`; locations from the record; risk_score / confidence / reachable / bug-id / correlation_id in `properties`
- [ ] **Given** the parity rule, **When** comparing outputs, **Then** Markdown and SARIF describe the same bugs; **Given** NFR-2, **Then** Prompt 4's original tests still pass

## Technical Notes

- ⚠️ This is an **extension brief**: paste **Optional A** from
  `docs/agent-systems/bug-hunter-build-guide.md` into the **skill-creator** skill (`Skill`
  tool → `skill-creator:skill-creator`) to re-open `report-rendering`. Re-run
  Prompt 4's tests after. STOP and report if skill-creator is unavailable.
- Build on owner adoption (GitHub code scanning accepts SARIF uploads — natural
  pairing with ci-gate).

## Dependencies

### Requires
- report-rendering (built — the review loop)

### Enables
- ci-gate consumption; GitHub code-scanning upload

## Out of Scope

- Tracker tickets (issue-sync); gate policy (ci-gate).
