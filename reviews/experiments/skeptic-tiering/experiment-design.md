---
type: experiment-design
status: run 2026-07-27 — owner adopted low→sonnet (the strict gate had failed)
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

## Result (launched 2026-07-24, completed 2026-07-27)

All 17 v8 Lows replayed on Sonnet against frozen `e2093bd`, original prompts/schemas/tier
logic (runs `wf_f89f4e8f-90d`). A session-limit crash forced a resume that re-ran 13 findings
live while the first run was still finishing — an accidental second independent Sonnet sample
for those 13 (it measures same-model rerun noise; total cost ~1.7M tokens vs the ~400k a clean
single run would have been).

| Outcome | Findings |
|---|---|
| Match (13/17) | F8 F9 F10 F13 F14 F15 F17 F18 F19 F20 F21 F22 F24 |
| Flip, stable across both Sonnet runs (3) | F12 F16 F23 — all `plausible → confirmed` |
| Unstable between the two Sonnet runs (1) | F11 — Opus `confirmed`; Sonnet run 1 `plausible`, run 2 `confirmed` |

- Every flip is `plausible → confirmed` on a coverage-gap/doc finding: Sonnet accepts a
  mutation-style trace ("apply this edit and the suite stays green") as a failing execution;
  Opus declined to. A systematic judgment-convention difference, not a capability gap — the
  Sonnet traces were accurate and detailed (F10's full 500-path trace is as good as the
  original).
- Zero `confirmed ↔ refuted`, zero new `disputed`. No flip would have changed the v8 review's
  ranking or any blocker call — all four are Lows that survive into the review either way.

**Pre-registered outcome: tiering NOT adopted.** The strict gate above was "zero enum flips"
and 3–4 occurred, so the adopt condition fails; the keep-Opus condition ("a flip that changed
a synthesis call") also never triggered — the result lands between the two registered
outcomes. The rule missed a case: enum flips that change no synthesis call. What would settle
it: an **Opus-replay control** on the same 17 to measure the same-model noise floor (F11's
instability proves rerun noise exists; without the control, model difference and noise are
inseparable).

**Owner decision (2026-07-27): adopted anyway.** The flips were one-directional, Lows-only,
and changed no synthesis call, and the ~5× price difference won: 🟡 skeptics now run on
Sonnet (`SKEPTIC_MODEL` in [lib/discovery-review.wf.js](../../lib/discovery-review.wf.js));
⚪ continue to skip skeptics, with Haiku designated if they ever get one. The Opus-replay
noise control stays open for whoever wants to fund it.
