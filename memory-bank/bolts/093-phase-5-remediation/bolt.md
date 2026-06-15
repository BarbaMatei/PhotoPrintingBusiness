---
id: 093-phase-5-remediation
unit: 005-phase-5-remediation
intent: 035-bug-hunter-agent-system
type: simple-construction-bolt
status: planned
stories:
  - 001-regression-harvest
  - 002-fix-verification
  - 003-fix-proposal
  - 004-fix-request-emit
  - 005-orchestrator-remediation-ext
created: 2026-06-10T10:40:14Z
started: null
completed: null
current_stage: null
stages_completed: []
time_box: 4h

requires_bolts: [092-phase-4-learn-and-measure]
enables_bolts: []
requires_units: [002-phase-2-trust, 004-phase-4-learn-and-measure]
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 2
  max_dependencies: 3
  testing_scope: 2
---

# Bolt: 093-phase-5-remediation

## Overview

Tooling-only bolt. Phase 5 in full — guide Prompts 30–33 **+ 31b (v3.3, review H1: the run-open
fix-request mailbox scan — the mechanism by which "fix done" is noticed)**: keep proving tests as
regression tripwires, gate bug closure on re-running them (**the loop's verification
gate**, writing `fix_status: verified-fixed` onto the fix-request record, keyed by
`correlation_id` — Integration Contract §4), draft suite-validated patch proposals
(never applied), and hand confirmed bugs to AI-DLC through the idempotent fix-request
store (records carry the `fix_status` lifecycle; bug-bolts carry the `correlation_id`
in `bolt.md` frontmatter).

## ⚠️ Construction Method (owner mandate + guide Part I — read before Stage 1)

**Each component MUST be created with the `skill-creator` skill** (`Skill` tool →
`skill-creator:skill-creator`): paste Prompt N from
`docs/agent-systems/bug-hunter-build-guide.md`, build, **run the brief's test prompts**,
fix, then next — in order. Prompt 31 also **re-opens** `bug-lifecycle` (re-run
Prompt 26's tests after), and Prompt 31b **re-opens** the `orchestrator` at its Open
seam. If skill-creator is unavailable, **STOP and report**.

## Stories Included (build in this order)

1. **001-regression-harvest** (Prompt 30, Should) — the ONE allowed new-file write
   into the codebase (test code, owner-approved per test)
2. **002-fix-verification** (Prompt 31, Should — GATE + bug-lifecycle EXTENSION)
3. **005-orchestrator-remediation-ext** (Prompt 31b, Should — orchestrator EXTENSION,
   NEW in v3.3) — the run-open mailbox scan; build right after 31
4. **003-fix-proposal** (Prompt 32, Should) — sandbox-validated diffs, proposal only
5. **004-fix-request-emit** (Prompt 33, Should) — idempotent store at
   `bug-hunting/fix-requests/` (D3)

## Bolt Type

**Type**: Simple Construction Bolt (tooling)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Read stories + briefs + unit brief; agree the AI-DLC consumption
      convention for `bug-hunting/fix-requests/` with the owner (how fix-bolts get
      created from records is the AI-DLC side; this bolt owns format + idempotency)
- [ ] **2. implement**: Build via skill-creator in order
- [ ] **3. test**: All test prompts green incl. Prompt 26 re-run; gate behavior
      demonstrated both ways (pass → Fixed + verified-fixed; fail → Confirmed, no
      signal); fix-requests update-not-duplicate

## Dependencies

### Requires
- 092-phase-4-learn-and-measure (bug-lifecycle to extend)
- bolt 087's Verifier + sandbox (proving tests + guards reused)

### Enables
- The AI-DLC bug→fix→verified-fixed loop (consumed by the specsmd flow)

## Success Criteria

- [ ] 5 components via skill-creator, all test prompts passing
- [ ] Discovery demonstrated: a completed bug-bolt is noticed by the 31b run-open scan
      (`open` → `fix-reported` → `verified-fixed`) with no manual prompt
- [ ] Closure discipline: bugs close only via the gate (or explicitly "unverified"
      fallback); `fix_status: verified-fixed` written on the fix-request record,
      keyed by `correlation_id` (Integration Contract §4); never on AI-DLC's word
      alone
- [ ] Patches never touch the real repo; regression tests land only with owner
      approval

## Notes

**Time-box: 4h.** Spec of record: guide Part II Phase 5.
