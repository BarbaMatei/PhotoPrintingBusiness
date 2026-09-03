---
id: 092-phase-4-learn-and-measure
unit: 004-phase-4-learn-and-measure
intent: 035-bug-hunter-agent-system
type: simple-construction-bolt
status: planned
stories:
  - 001-suppression-learning
  - 002-bug-lifecycle
  - 003-eval-corpus
  - 004-eval-metrics
  - 005-curator-agent
  - 006-orchestrator-learn-ext
created: 2026-06-10T10:40:14Z
started: null
completed: null
current_stage: null
stages_completed: []
time_box: 5h

requires_bolts: [089-phase-3-specialists-a, 090-phase-3-specialists-b]
enables_bolts: [093-phase-5-remediation, 094-optional-integration]
requires_units: [002-phase-2-trust, 003-phase-3-breadth-and-scale]
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 1
---

# Bolt: 092-phase-4-learn-and-measure

## Overview

Tooling-only bolt. **Re-scoped 2026-09:** make the engine measure itself. Three gaps of
Prompts 25–29b — a **standing eval corpus with a poison fixture** (27; there is a seeded-run
protocol, no standing corpus), **recall and escape metrics** (28; `metrics.jsonl` and a track
record exist, recall is unproven), and **curator automation** (29, 29b; the system self-review
and the speed report are run by hand). `bug-lifecycle` (26) is **satisfied**;
`suppression-learning` (25) is **superseded** — the loop never suppresses a finding, it
attaches the prior decision to it. Runs after the specialists and before the remediation
hand-off; it no longer waits on the oracle tier.

## ⚠️ Construction Method (owner mandate + guide Part I — read before Stage 1)

Each component **extends the review loop** (`reviews/lib`, `.claude/skills`) at the seam named
in its story; build it as a skill or script in that tree, with a test under
`reviews/lib/tests`, following `reviews/README.md`'s conventions. Here that is the fixture
builder, `reviews/lib/measure/` and the speed report; 29b wires the Learn step into the pass
router, so re-run the router's tests. The guide's Prompt N stays the specification of each
piece's behaviour.

## Stories Included (build in this order)

1. ~~**001-suppression-learning** (Prompt 25)~~ — **superseded**: the loop attaches the prior
   decision to a re-found finding instead of suppressing it (guide Prompt 25, contract §6.5);
   no work in this bolt
2. ~~**002-bug-lifecycle** (Prompt 26)~~ — **satisfied** by the loop's statuses, reopen and
   lineage (`reviews/lib/records/schema.mjs`, `reviews/lib/records/ledger.mjs`); no work here
3. **003-eval-corpus** (Prompt 27, Should) — fixtures under `bug-hunting/eval/`
4. **004-eval-metrics** (Prompt 28, Should) — recall vs seeded; precision via dismissals
5. **005-curator-agent** (Prompt 29, Should) — Learn/Reconcile/Measure/Summarize
6. **006-orchestrator-learn-ext** (Prompt 29b, Should — EXTENSION)

## Bolt Type

**Type**: Simple Construction Bolt (tooling)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Read stories + briefs + unit brief
- [ ] **2. implement**: Close the three gaps in order at their seams
- [ ] **3. test**: All test prompts green incl. orchestrator re-runs; a curation pass
      after a run with dismissals produces validated pattern proposals + metrics + the
      health summary

## Dependencies

### Requires
- 089 + 090 (the specialists whose output the corpus scores). **Re-ordered 2026-09:** this
  bolt no longer waits on the oracle tier (091), which is now last — measuring the engine does
  not need contract grounding.

### Enables
- 093 (fix-verification extends bug-lifecycle); 094 (issue-sync follows lifecycle)

## Success Criteria

- [ ] The three gaps closed at their seams, each with a test under `reviews/lib/tests`
- [ ] Recall measured against a standing corpus, not asserted; a poison fixture in it that a
      pass must not "find"; escapes counted
- [ ] Safety properties hold: self-closes evidence-based and audited; regressions flagged;
      each eval run records the model it ran on

## Notes

**Time-box: 5h.** Spec of record: guide Part II Phase 4.
