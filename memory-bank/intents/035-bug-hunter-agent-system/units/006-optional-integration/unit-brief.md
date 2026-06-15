---
unit: 006-optional-integration
intent: 035-bug-hunter-agent-system
phase: inception
status: ready
unit_type: tooling
default_bolt_type: simple-construction-bolt
created: 2026-06-10T10:40:14Z
updated: 2026-06-10T10:40:14Z
---

# Unit Brief: Optional — Integration

## Purpose

Connect the system to outside tooling **when the owner adopts it**: a machine-readable
SARIF twin of every report, idempotent issue-tracker sync, and a baseline-aware CI
gate. The guide is explicit: a one-person team with no production rarely needs these
yet — build on adoption, not by default.

## Scope

### In Scope — 3 briefs (guide Prompts A–C)
| Component | Brief | Role |
|-----------|-------|------|
| report-rendering ext | A | SARIF twin (`run-NN.sarif`) describing the same bugs |
| `issue-sync` | B | Tracker tickets (create/update/close), idempotent via ledger links |
| `ci-gate` | C | Pass/fail vs baseline: fail only NEW Critical/High by default |

### Out of Scope
- Choosing/configuring the tracker itself; CI pipeline redesign (the repo's GitHub
  Actions stays as-is; ci-gate plugs into it).

---

## ⚠️ Construction Method (owner mandate + guide Part I — MUST follow)

**Each component MUST be created with the `skill-creator` skill** (`Skill` tool →
`skill-creator:skill-creator`): paste the brief from
`docs/agent-systems/bug-hunter-build-guide-v3.6.md`, build, **run its test prompts**, fix, then move
on. Brief A **re-opens** `report-rendering` (seam extension). If skill-creator is
unavailable, **STOP and report**.

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-8 | Optional integration (A–C) | Could |
| FR-1, FR-2 | Cross-cutting | Must |

## Story Summary

- **Total Stories**: 3 — **Could**: 3

### Stories
- [ ] **001-report-rendering-sarif-ext** — Could — Planned
- [ ] **002-issue-sync** — Could — Planned
- [ ] **003-ci-gate** — Could — Planned

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 001, 002 | report-rendering + severity-scoring exist |
| 004 | issue-sync follows bug-lifecycle transitions |

### Depended By
| Unit | Reason |
|------|--------|
| (none) | Terminal tier |

## Technical Context

- Repo reality: GitHub Actions CI exists (bolt 040) → `ci-gate` is buildable today;
  GitHub Issues is the zero-cost tracker option for `issue-sync` (`gh` CLI present).
- SARIF mapping per the brief: ruleId = category; level from severity
  (Critical/High → error, Medium → warning, Low → note); risk_score / confidence /
  reachable / bug-id / correlation_id in `properties`; Markdown and SARIF must agree
  on counts.
- `ci-gate` policy is configurable; default fails only **newly-introduced**
  Critical/High vs the baseline run/commit — pre-existing findings never fail a build.

## Constraints

- Build only on explicit owner adoption (this unit's bolt stays parked until then).
- Idempotency: ticket links recorded in the ledger; re-runs update, never duplicate.

## Success Criteria

- [ ] All 3 briefs built via skill-creator; test prompts passing.
- [ ] SARIF/Markdown parity; ticket lifecycle follows bug lifecycle; gate passes a
      no-new-findings run against the baseline.

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 094-optional-integration | simple-construction-bolt | all 3 | SARIF + tracker + CI gate (on adoption) |
