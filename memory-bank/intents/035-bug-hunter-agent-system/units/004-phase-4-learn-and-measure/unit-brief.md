---
unit: 004-phase-4-learn-and-measure
intent: 035-bug-hunter-agent-system
phase: inception
status: ready
unit_type: tooling
default_bolt_type: simple-construction-bolt
created: 2026-06-10T10:40:14Z
updated: 2026-06-10T10:40:14Z
---

# Unit Brief: Phase 4 — Learn & Measure

## Purpose

Make the system improve itself. The Curator fills the Learn slot: it turns the
owner's dismissal *reasons* (arriving through `triage-intake` since Phase 1) into
validated suppression patterns, keeps the ledger honest (evidence-based self-closing,
moved locations, regression flags), and measures quality against a ground-truth
corpus so a change that hurts recall is visible.

## Scope

### In Scope — 6 briefs (guide Prompts 25–29b)
| Component | Brief | Role |
|-----------|-------|------|
| `suppression-learning` | 25 | Dismissals → proposed patterns; validated vs Confirmed set; blast radius |
| `bug-lifecycle` | 26 | Status machine; propose-don't-silently-close; regression flagging |
| `eval-corpus` | 27 | Labeled real + seeded synthetic bugs; hit matcher; fixtures only |
| `eval-metrics` | 28 | Recall vs seeded corpus; precision via dismissal rate; trend; pinned eval model/temp |
| `curator-agent` | 29 | Learn → Reconcile → Measure → Summarize (agent-as-skill) |
| orchestrator ext | 29b | Point the Learn slot at the Curator |

### Out of Scope
- Fix verification (P5 extends `bug-lifecycle` at its "mark Fixed" seam).

---

## ⚠️ Construction Method (owner mandate + guide Part I — MUST follow)

**Each component MUST be created with the `skill-creator` skill** (`Skill` tool →
`skill-creator:skill-creator`): paste Prompt N from
`docs/agent-systems/bug-hunter-build-guide.md`, build, **run the brief's three test prompts**,
fix, then move on — in order. Prompt 29b **re-opens** `orchestrator` (seam extension).
If skill-creator is unavailable, **STOP and report**.

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-6 | Phase 4 learn & measure (Prompts 25–29b) | Should |
| FR-1, FR-2 | Cross-cutting | Must |

## Story Summary

- **Total Stories**: 6 — **Should**: 6

### Stories
- [ ] **001-suppression-learning** — Should — Planned
- [ ] **002-bug-lifecycle** — Should — Planned
- [ ] **003-eval-corpus** — Should — Planned
- [ ] **004-eval-metrics** — Should — Planned
- [ ] **005-curator-agent** — Should — Planned
- [ ] **006-orchestrator-learn-ext** — Should — Planned

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 001–003 | Consumes triage-intake dismissals, git-revision-tracking, the full pipeline's findings |

### Depended By
| Unit | Reason |
|------|--------|
| 005-phase-5-remediation | fix-verification extends bug-lifecycle's "mark Fixed" step |

## Technical Context

- **Safety property (binding)**: suppression patterns are **proposed, never
  auto-activated** — approval flows through `triage-intake`; every proposal is
  validated against the Confirmed set (an over-broad suppression hides real bugs).
- **Lifecycle transitions**: `New → Confirmed | Dismissed`; `Confirmed → Fixed`
  (evidence; P5 gate first once built); `Fixed → Reopened` (regression — high
  priority). Self-closing proposes with evidence; auto-apply is configurable and
  always audited.
- **Metrics nuance (binding)**: a reported bug missing from the corpus is NOT a false
  positive; recall is measured against the **seeded** corpus, precision proxied by
  the human-dismissal rate. Seeded bugs live strictly in fixtures under
  `bug-hunting/eval/`, never shippable code.

## Constraints

- Read-only on app source; eval fixtures live under `bug-hunting/eval/` (D3).

## Success Criteria

- [ ] All 6 briefs built via skill-creator; three test prompts each, passing.
- [ ] After a run with dismissals: proposed patterns with blast radius +
      no-true-bug-suppressed confirmation; metrics recorded with trend; a reappearing
      fixed signature flagged as regression; runs end by curating (29b).

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 092-phase-4-learn-and-measure | simple-construction-bolt | all 6 | Fill the Learn slot + eval harness |
