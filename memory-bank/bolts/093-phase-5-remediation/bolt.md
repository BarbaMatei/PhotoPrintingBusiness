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

Tooling-only bolt. **Re-scoped 2026-09:** two gaps of Prompts 30–33. **`regression-harvest` by
a non-fixer** (30) — today the fixer writes its own red-first test and a test-meaning audit
checks it; the tripwire that survives must come from someone who did not write the fix. And
**`fix-request-emit`** (33) — missing, because today's fixer sits inside the loop; an
out-of-loop fixer needs an idempotent fix-request store keyed by `correlation_id` (records carry
the `fix_status` lifecycle; bug-bolts carry the `correlation_id` in `bolt.md` frontmatter —
Integration Contract §4). `fix-verification` (31) and the run-open mailbox scan (31b) are
**satisfied**; `fix-proposal` (32) is left as-is, since the loop's fixer applies patches
directly, by design.

## ⚠️ Construction Method (owner mandate + guide Part I — read before Stage 1)

Each component **extends the review loop** (`reviews/lib`, `.claude/skills`) at the seam named
in its story; build it as a skill or script in that tree, with a test under
`reviews/lib/tests`, following `reviews/README.md`'s conventions. Here that is the hand-back
gates (`reviews/lib/fix/handback-gates.mjs`) for the non-fixer harvest and the records tree for
the fix-request store — do not rebuild the verification pass or the router's scan, which
already exist. The guide's Prompt N stays the specification of each piece's behaviour.

## Stories Included (build in this order)

1. **001-regression-harvest** (Prompt 30, Should) — the ONE allowed new-file write
   into the codebase (test code, owner-approved per test)
2. ~~**002-fix-verification** (Prompt 31)~~ — **satisfied** by the loop's verification pass
   (`reviews/lib/verify/verify-fixes.mjs`); no work in this bolt
3. ~~**005-orchestrator-remediation-ext** (Prompt 31b)~~ — **satisfied** by the router's
   verification row (`reviews/lib/drive/rows.mjs`); no work in this bolt
4. ~~**003-fix-proposal** (Prompt 32)~~ — partial by design: the loop's fixer applies patches
   directly; no work in this bolt
5. **004-fix-request-emit** (Prompt 33, Should) — idempotent store at
   `bug-hunting/fix-requests/` (D3)

## Bolt Type

**Type**: Simple Construction Bolt (tooling)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Read stories + briefs + unit brief; agree the AI-DLC consumption
      convention for `bug-hunting/fix-requests/` with the owner (how fix-bolts get
      created from records is the AI-DLC side; this bolt owns format + idempotency)
- [ ] **2. implement**: Close the two gaps in order at their seams
- [ ] **3. test**: A test per gap under `reviews/lib/tests` + the verification pass's own tests
      re-run; gate behavior demonstrated both ways (pass → Fixed + verified-fixed; fail →
      Confirmed, no signal); fix-requests update-not-duplicate

## Dependencies

### Requires
- 092-phase-4-learn-and-measure (the metrics the gate reads)
- bolt 087's execution proof (the proving test and its guards are reused here)

### Enables
- The AI-DLC bug→fix→verified-fixed loop (consumed by the specsmd flow)

## Success Criteria

- [ ] The two gaps closed at their seams, each with a test under `reviews/lib/tests`
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
