---
unit: 001-phase-1-skeleton
intent: 035-bug-hunter-agent-system
phase: inception
status: satisfied
unit_type: tooling
default_bolt_type: simple-construction-bolt
created: 2026-06-10T10:40:14Z
updated: 2026-06-10T10:40:14Z
---

# Unit Brief: Phase 1 — Skeleton

## Purpose

The smallest complete bug-hunting system, end-to-end from the first run: persistent
concurrency-safe memory, canonical bug records, dedup against prior runs, floored
Markdown reports, a human-decision channel, one general hunter, and the Orchestrator
that defines all six permanent pipeline slots (Map/Verify/Learn minimal or
placeholder). Output is explicitly labeled **"unverified candidates — high
false-positive rate until Phase 2."**

## Scope

### In Scope — 7 skills (guide Prompts 1–7)
| Skill | Brief | Role |
|-------|-------|------|
| `ledger-io` | Prompt 1 | Concurrency-safe ledger R/W; single source of truth |
| `bug-documentation` | Prompt 2 | Canonical 3-audience bug record |
| `deduplication` | Prompt 3 | new / duplicate / dismissed / suppressed verdicts |
| `report-rendering` | Prompt 4 | Per-run Markdown report with reporting floor |
| `triage-intake` | Prompt 5 | Human decisions front door (provenance + reasons) |
| `general-hunter` | Prompt 6 | Combined top-down + file-sweep hunting (agent-as-skill) |
| `orchestrator` | Prompt 7 | The 6-slot coordinator (agent-as-skill) |

### Out of Scope
- Verification, scoring, specialists, learning, remediation (later phases fill those
  slots — the slots themselves ARE defined here).
- Any write outside `bug-hunting/` (+ `.claude/skills/` at construction time).

---

## ⚠️ Construction Method (owner mandate + guide Part I — MUST follow)

**Each component MUST be created with the `skill-creator` skill** (`Skill` tool →
`skill-creator:skill-creator`): paste the component's brief (Prompt N) from
`docs/agent-systems/bug-hunter-build-guide.md`, build, **run the brief's three test prompts**,
fix, and only then take the next component — in master-build-order. If skill-creator
is unavailable, **STOP the bolt and report**. Hand-rolled skills violate FR-1.

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-3 | Phase 1 skeleton (Prompts 1–7) | Must |
| FR-1, FR-2 | skill-creator build loop + shared conventions (cross-cutting) | Must |

## Story Summary

- **Total Stories**: 7 — **Must Have**: 7

### Stories
- [ ] **001-ledger-io** — Must — Planned
- [ ] **002-bug-documentation** — Must — Planned
- [ ] **003-deduplication** — Must — Planned
- [ ] **004-report-rendering** — Must — Planned
- [ ] **005-triage-intake** — Must — Planned
- [ ] **006-general-hunter** — Must — Planned
- [ ] **007-orchestrator-skeleton** — Must — Planned

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| (none) | First phase |

### Depended By
| Unit | Reason |
|------|--------|
| 002–006 | Everything builds on the skeleton's seams |

## Technical Context

- **Pinned output paths (requirements D3)**: ledger at
  `bug-hunting/bug-ledger.json` (+ generated `bug-hunting/bug-ledger.md`); reports at
  `bug-hunting/reports/bug-report-run-NN-<YYYYMMDD-HHMM>.md`.
- **Candidate shape** (binding for hunters): `{hypothesis, category_guess,
  location:{file,start_line,end_line,symbol}, flow_position, evidence_snippet,
  source_hunter}`.
- **Six slots are permanent**: the orchestrator defines Map/Hunt/Verify/Triage/
  Report/Learn now; later phases fill/extend — never restructure.
- v3 specifics to not miss: `ledger-io` staging-files + single-writer merge + atomic
  IDs + `correlation_id` field; `bug-documentation` sources `expected_behavior` from
  a contract when one exists (tags "intent-unconfirmed" otherwise);
  `report-rendering` floors Low findings into an appendix with optional top-N/budget;
  `triage-intake` rejects reason-less dismissals.

## Constraints

- Read-only on application source (NFR-1). Pure-markdown skills; runtime shell-outs
  must work on the Windows host (NFR-6).

## Success Criteria

- [ ] All 7 skills exist under `.claude/skills/`, built via skill-creator, each
      brief's three test prompts passing.
- [ ] A first full run on this repo completes: ledger created, report written, label
      "unverified candidates" present; a second run surfaces only new findings.

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 085-phase-1-skeleton-core | simple-construction-bolt | 001–005 | Foundation skills |
| 086-phase-1-skeleton-agents | simple-construction-bolt | 006–007 | Hunter + Orchestrator; first end-to-end run |

085/086 — satisfied by the review loop (`reviews/`), bolts retired 2026-09; the stories carry
their Status lines.
