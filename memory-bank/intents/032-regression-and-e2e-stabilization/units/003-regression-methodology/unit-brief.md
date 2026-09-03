---
unit: 003-regression-methodology
intent: 032-regression-and-e2e-stabilization
phase: inception
status: stories-defined
unit_type: frontend
default_bolt_type: simple-construction-bolt
created: 2026-06-05T11:20:00Z
updated: 2026-06-05T11:30:00Z
---

# Unit Brief: Regression Methodology

## Purpose

Make stabilization repeatable: a documented regression checklist mapped to every shipped intent, one executed and dated baseline pass against the current build, and triage of every finding into the backlog. The checklist cross-references which checks the unit-002 e2e specs now automate, so the manual surface shrinks over time.

## Scope

### In Scope
- `docs/testing/regression-checklist.md` enumerating checks grouped by shipped intent (001–024 + any shipped 025–031 bolts), each tagged automated-by-e2e / automated-by-integration / manual.
- One executed full pass, recorded with date + build SHA + per-check result.
- Triage: each failure/known-issue linked to a new bolt, an existing planned bolt, or a `KNOWN_FAILURES.md` entry (file from bolt 057).

### Out of Scope
- Writing new automated tests (unit 002 does that) — this unit *references* them.
- Fixing the defects found (they become backlog items).
- The e2e fixtures/specs themselves (units 001/002).

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-4 | Documented regression-pass methodology + executed baseline | Should |

---

## Domain Concepts

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| Build checklist | Map shipped intents → verifiable checks | story-index, shipped bolts | regression-checklist.md |
| Execute pass | Run every check against current build | checklist + running app | dated baseline result |
| Triage finding | Route each failure to backlog | failures | bolt / KNOWN_FAILURES entries |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 3 |
| Must Have | 1 |
| Should Have | 2 |
| Could Have | 0 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-regression-checklist | Regression checklist mapped to shipped intents | Should | Planned |
| 002-execute-regression-baseline | Execute + record one baseline pass | Must | Planned |
| 003-triage-findings-to-backlog | Triage findings into backlog/KNOWN_FAILURES | Should | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 002-e2e-journey-coverage | Checklist marks which checks are now automated-by-e2e |

### Depended By
| Unit | Reason |
|------|--------|
| None | Terminal unit of the intent |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| KNOWN_FAILURES.md (bolt 057) | Home for accepted known-issues | Low |

---

## Technical Context

### Suggested Technology
Markdown checklist under `docs/testing/`; cross-links to e2e spec names and integration test classes; the existing `status-integrity.cjs` / story-index for the shipped-intent inventory.

---

## Constraints

- The checklist is the durable artifact; the executed pass is a point-in-time baseline (date + SHA stamped).
- Findings are routed, never silently dropped.

---

## Success Criteria

### Functional
- [ ] Checklist covers every shipped intent with a tagged check each.
- [ ] One full pass executed + recorded (date, SHA, per-check result).
- [ ] Every failure/known-issue linked to a backlog item or KNOWN_FAILURES entry.

### Non-Functional
- [ ] Checklist is re-runnable cheaply by a future wave.

### Quality
- [ ] Cross-references e2e/integration automation so the manual surface is explicit.

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 072-regression-methodology | simple | 001–003 | Checklist + executed baseline + triage |

---

## Notes

Terminal unit. Best executed after unit 002 so "automated-by-e2e" tags are accurate. The executed-baseline story is `must` (the stabilization gate); the checklist + triage are `should` (the durable scaffolding around it).
