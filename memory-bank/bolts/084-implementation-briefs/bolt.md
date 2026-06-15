---
id: 084-implementation-briefs
unit: 003-implementation-briefs
intent: 034-eu-expansion-architecture-study
type: simple-construction-bolt
status: planned
stories:
  - 001-author-implementation-briefs
created: 2026-06-05T12:57:50Z
started: null
completed: null
current_stage: null
stages_completed: []
time_box: 4h

requires_bolts: [083-synthesis-and-decision]
enables_bolts: []
requires_units: [002-synthesis-and-decision]
blocks: false

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 1
---

# Bolt: 084-implementation-briefs

## Overview

Docs-only bolt that authors the implementation brief(s) (D4) from the ADR — the inception
feed for the next cycle. Closes the research→implementation loop.

## Objective

Produce `docs/planning/i18n-readiness-brief-<date>.md` (+ more if the decision splits the
work): the ADR translated into concrete, ordered **readiness requirements** (seam prep only,
no translations), authored in the source brief's style and complete enough to hand to inception.

## Stories Included

- **001-author-implementation-briefs**: Author D4 brief(s) from the ADR (Must)

## Bolt Type

**Type**: Simple Construction Bolt (docs only — output is documentation, not code)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Pending → outline of brief(s) from ADR + T7 sizing
- [ ] **2. implement**: Pending → write `docs/planning/i18n-readiness-brief-<date>.md` (+ splits)
- [ ] **3. test**: Pending → completeness check (hand-to-inception readiness)

## Dependencies

### Requires
- 083-synthesis-and-decision (the ADR)

### Enables
- A future implementation intent (via feeding D4 back into inception)

## Success Criteria

- [ ] At least one brief at `docs/planning/i18n-readiness-brief-<date>.md`
- [ ] Translates the ADR into concrete, ordered readiness requirements (seam prep only)
- [ ] Complete enough to create the implementation intent(s) with no extra context
- [ ] States explicitly that deployment remains Phase 6

## Notes

**Time-box: 4h.** Docs only — no production code. Mirror the structure of
`docs/planning/eu-expansion-research-brief-2026-06-05.md`. Pull retrofit sizing from T7 and
scope from the chosen bundle.
