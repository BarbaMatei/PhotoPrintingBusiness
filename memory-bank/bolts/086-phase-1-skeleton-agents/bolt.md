---
id: 086-phase-1-skeleton-agents
unit: 001-phase-1-skeleton
intent: 035-bug-hunter-agent-system
type: simple-construction-bolt
status: planned
stories:
  - 006-general-hunter
  - 007-orchestrator-skeleton
created: 2026-06-10T10:40:14Z
started: null
completed: null
current_stage: null
stages_completed: []
time_box: 2h

requires_bolts: []
enables_bolts: []
requires_units: []
blocks: false
notes: verification bolt — confirms the review loop satisfies these stories; builds nothing new; run 085 then 086 (edges left empty on purpose: nothing gates on them)

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 0
  testing_scope: 1
---

# Bolt: 086-phase-1-skeleton-agents (verification)

## Objective

Confirm on the record, story by story, that the review loop satisfies guide Prompts 6–7 — the
hunter and the six-slot coordinator — or name exactly where it does not. Nothing is built.

## Overview

The review loop — `reviews/` plus the skills `loop-driver`, `fix-review`,
`reconcile-findings` and `owner-summary` — is **claimed** to satisfy this bolt's two stories;
each story's `**Status:**` line names the seam said to satisfy it. This bolt checks that claim
story by story and records the verdict. It builds nothing.

This bolt keeps the simple-construction type's records; its stages below map onto the type's
plan → implement → test sequence with "implement" meaning "verify".

A gap found here becomes a **new story** under the unit, assigned to bolt `087-phase-2-trust`
(or a proposal for a new construction bolt) — never work done inside this bolt.

## Stories verified (in this order)

1. **006-general-hunter** (guide Prompt 6) — claimed seam: the core six lenses,
   `reviews/lib/records/schema.mjs` (`CORE_LENSES`) and their prompts
2. **007-orchestrator-skeleton** (guide Prompt 7) — claimed seam:
   `.claude/skills/loop-driver/SKILL.md`, `reviews/lib/drive/route-next-pass.mjs`,
   `reviews/lib/discovery-review.wf.js`

## Stages

- [ ] **1. plan** → `implementation-plan.md` (here, the verification plan): read both stories,
      their `**Status:**` lines, the rows for Prompts 6–7 in the guide's
      "Implementation status (2026-09)" table (`docs/agent-systems/bug-hunter-build-guide.md`),
      and each brief's three test prompts
- [ ] **2. verify** (the type's "implement" stage): for each story open the seam path(s) and
      check — by reading, and by running where the behaviour can be run — that what the brief's
      test prompts describe is present. Write a per-story verdict: **satisfied** ·
      **satisfied with a gap** (name the gap) · **not satisfied**
- [ ] **3. record** → `test-walkthrough.md` in this bolt's folder: the verdict table and its
      evidence (file:line, command output). Each gap becomes a new story file under
      `memory-bank/intents/035-bug-hunter-agent-system/units/001-phase-1-skeleton/stories/` with
      `assigned_bolt: 087-phase-2-trust`, **and its id is appended to bolt 087's `stories:`
      list** — the cross-unit assignment is intended, 087 extends the same components. Every gap
      is listed in the report
- [ ] **4. review**: stage 6 of `memory-bank/standards/bolt-process.md`, at the docs tier —
      `reviews/README.md` "Entry tiers" gives docs-only work one quick pass. The pass is
      required, not optional: `bolt-process.md` allows `status: complete` only after stage 6's
      first pass. The verdict report is the reviewed artifact
- [ ] **5. complete**: each satisfied story → `status: complete`, `implemented: true`; this bolt →
      `status: complete` with `completed:` set. Unit 001's `unit-brief.md` → `status: complete`
      only when every one of its stories is `complete` **or** re-assigned to the bolt that will
      close it; if story 002's gap (no three-audience record) is confirmed, re-assign it to 087
      the same way

## Bolt Type

**Type**: Simple Construction Bolt, used here as a **verification bolt** — it confirms an
equivalence claim and produces a verdict report, not new components.
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Dependencies

### Requires
- (none — it runs straight after 085, and neither waits on a construction bolt)

### Enables
- (nothing gates on the verdict; `requires_bolts`, `enables_bolts` and `requires_units` are empty
  on purpose. The construction bolts are cheaper to trust once it exists, but none waits for it)

## Success Criteria

- [ ] Both stories carry a recorded verdict in `test-walkthrough.md`, each with its evidence
      (file:line, command output)
- [ ] No part of the June skeleton was built — no `bug-hunting/` tree, no new hunter or
      orchestrator skill
- [ ] Every confirmed gap exists as a new story with `assigned_bolt: 087-phase-2-trust` and its id
      appears in bolt 087's `stories:` list
- [ ] Unit 001's status is truthful: `complete` only once every story is complete or re-assigned

## What this bolt must not do

- Build or copy the June skeleton (a `bug-hunting/` ledger, hunter skills) beside the review loop.
- Change anything under `reviews/**`, except this bolt's own review records under
  `reviews/086-phase-1-skeleton-agents/` — a needed change to the loop itself is a new story for
  another bolt.
- Mark a story complete without a recorded verdict in `test-walkthrough.md`.

## Notes

**Time-box: 2h.** Spec of record: `docs/agent-systems/bug-hunter-build-guide.md` Part II Phase 1
and its "Implementation status (2026-09)" table. Unit brief:
`memory-bank/intents/035-bug-hunter-agent-system/units/001-phase-1-skeleton/unit-brief.md`.
Runs after 085; together they close unit 001.
