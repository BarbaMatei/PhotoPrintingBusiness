---
id: 085-phase-1-skeleton-core
unit: 001-phase-1-skeleton
intent: 035-bug-hunter-agent-system
type: simple-construction-bolt
status: planned
stories:
  - 001-ledger-io
  - 002-bug-documentation
  - 003-deduplication
  - 004-report-rendering
  - 005-triage-intake
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
notes: verification bolt — confirms the review loop satisfies these stories; builds nothing new

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 0
  testing_scope: 1
---

# Bolt: 085-phase-1-skeleton-core (verification)

## Overview

The review loop — `reviews/` plus the skills `loop-driver`, `fix-review`,
`reconcile-findings` and `owner-summary` — is **claimed** to satisfy this bolt's five stories;
each story's `**Status:**` line names the seam said to satisfy it. This bolt checks that claim
story by story and records the verdict. It builds nothing.

A gap found here becomes a **new story** under the unit, assigned to bolt `087-phase-2-trust`
(or a proposal for a new construction bolt) — never work done inside this bolt.

## Stories verified (in this order)

1. **001-ledger-io** (guide Prompt 1) — claimed seam: `reviews/lib/records/ledger.mjs`,
   `reviews/lib/review/mint-id.mjs`, `reviews/lib/records/render-records.mjs`
2. **002-bug-documentation** (guide Prompt 2) — claimed seam: `reviews/lib/records/render-records.mjs`,
   already recorded partial (a ledger row plus a fix brief, not the three-audience record)
3. **003-deduplication** (guide Prompt 3) — claimed seam: `.claude/skills/reconcile-findings/SKILL.md`
4. **004-report-rendering** (guide Prompt 4) — claimed seam: `reviews/templates/review.md`,
   `reviews/templates/summary.md`, `reviews/lib/records/doc-gate.mjs`
5. **005-triage-intake** (guide Prompt 5) — claimed seam: `reviews/lib/drive/gates.mjs`

## Stages

- [ ] **1. plan**: read the five stories, their `**Status:**` lines, the rows for Prompts 1–5 in
      the guide's "Implementation status (2026-09)" table
      (`docs/agent-systems/bug-hunter-build-guide.md`), and each brief's three test prompts
- [ ] **2. verify**: for each story open the seam path(s) and check — by reading, and by running
      where the behaviour can be run — that what the brief's test prompts describe is present.
      Write a per-story verdict: **satisfied** · **satisfied with a gap** (name the gap) ·
      **not satisfied**
- [ ] **3. record**: write `ddd-03-test-report.md` in this bolt's folder with the verdict table
      and its evidence (file:line, command output). Each gap becomes a new story file under
      `memory-bank/intents/035-bug-hunter-agent-system/units/001-phase-1-skeleton/stories/`
      with `assigned_bolt: 087-phase-2-trust` (or a proposal for a new bolt), listed in the report
- [ ] **4. review**: stage 6 of `memory-bank/standards/bolt-process.md`, at the docs tier —
      `reviews/README.md` "Entry tiers" gives docs-only work one quick pass, or a skip. The test
      report is the reviewed artifact
- [ ] **5. complete**: each satisfied story → `status: complete`, `implemented: true`; this bolt →
      `status: complete` with `completed:` set; once 085 and 086 are both complete, unit 001's
      `unit-brief.md` → `status: complete`

## Bolt Type

**Type**: Simple Construction Bolt, used here as a **verification bolt** — it confirms an
equivalence claim and produces a test report, not new components.
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## What this bolt must not do

- Build or copy the June skeleton (a `bug-hunting/` ledger, hunter skills) beside the review loop.
- Change anything under `reviews/**` — a needed change there is a new story for another bolt.
- Mark a story complete without a recorded verdict in `ddd-03-test-report.md`.
