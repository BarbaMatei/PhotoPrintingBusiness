---
type: experiment-design
status: not-run
created: 2026-07-14
owner: Matei Barba
---

# Skeptic model-tiering replay

Validates the README's held-in-reserve lever: can low-severity skeptics run on a cheaper model
(Sonnet) without flipping any outcome-changing verdict? Decided by replay against recorded data,
not by argument.

## Question

The skeptic layer is ~80–90% of a discovery pass's agents. The tiering proposal keeps Opus for
lenses + High/Medium skeptics and moves the Low-severity guard/trace skeptics to Sonnet. The
README deferred it for a "small confidence cost" — this experiment measures whether that cost is
real.

## Protocol (one-shot, roughly 300–500k tokens)

1. **Input:** bolt-042 review-v8's canonical findings and their recorded skeptic verdicts
   (findings-v8.md + the v8 run journal). Frozen commit:
   `e2093bdd596107d2e67ff4a4135c47e4530f6eb4`.
2. **Re-run ONLY the low-severity guard/trace skeptic calls** against that same commit, with
   `model: 'sonnet'` on the agent calls. Same prompts, same schemas, same tiering logic —
   nothing else changes.
3. **Diff the verdicts** against v8's recorded ones, finding by finding.

## Pre-registered decision rule (written before running)

- **Zero outcome-changing flips** (`confirmed` ↔ `refuted`/`plausible`, or any new `disputed`)
  → adopt tiering: add a severity-based `model` opt to `traceAgent`/`guardAgent` in
  [lib/discovery-review.wf.js](../../lib/discovery-review.wf.js) (low → sonnet).
- **Any flip that would have changed a synthesis call** → keep Opus for all skeptics, record the
  flip here, close the question.

Verdict-text wording differences don't count; only the verdict enum and anything that would have
changed the review's ranking or a blocker call.

## Sequencing

Run only **after measure #5** (decided re-raises skip skeptics) has landed in a real pass — #5
removes many low-tier skeptic runs, shrinking both the cost and the benefit of tiering; measure
what is actually left.

## Result

*(not yet run)*
