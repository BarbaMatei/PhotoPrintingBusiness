---
type: review-system-rationale
status: reference — the evidence behind the runbook rules
created: 2026-07-24 (extracted from README.md)
owner: Matei Barba
---

# Why the review system is built this way

The rules in [README.md](README.md) and the runbooks are conclusions. This file keeps the
evidence — per feature, dated, with the numbers. Nothing here is needed to *run* a pass;
read it when questioning or recalibrating a rule.

## Bolt 035 (payments, 2026-06): one pass is a sample, not a sweep

The feature was audited blind three times — v1, v5, v8 — but **not on the same code**: fixes
landed between audits (13 of v1's 15 findings were already fixed when v5 ran), so the problem
population was open, not closed. The audits found **15 · 15 · 18** findings, near-disjoint sets.
Across all seven rounds, only **5** findings were ever raised outside a full audit, and **zero
fixes were ever reopened**.

The reading: **verification worked** (cheap anchored fix-checks did their job perfectly).
**Discovery did not converge** — each audit caught a different sample of the population, and the
loop first terminated on "reviewer went quiet", which measures the reviewer, not the code. The
v7 "loop complete, 0 open" verdict preceded an audit that found 18 more problems in the same
code. Two separate failures with two separate fixes:

- Breadth was accreted, not designed — v1 ran 5 lenses, v5 and v8 kept adding more and kept
  finding what the narrower audits missed. Fix: the **lens manifest** (breadth front-loaded).
- Narrow fix-check rounds were allowed to stamp the whole feature done. Fix: the hard
  **discovery vs verification split** — anchoring helps a fix-check and poisons a wide search,
  so the two are distinct activities with distinct exit criteria, and a verification pass may
  never emit `approved`.

## Why parallel isolated lenses

- **No bias cross-contamination.** A lens that hasn't seen the other lenses' conclusions can't
  anchor on them; when isolated lenses independently converge, that's signal (035-v8: one
  fragility was hit by 5 lenses independently; a dead-code method by 3).
- **Caveat — convergence is only as independent as the prompts.** All lenses share base context
  (project hints). Agreement a shared hint planted is manufactured: the dedup agent marks such
  findings `hinted`, and they don't get the convergence discount.
- **Clean main context.** Lenses read whole files and return distilled findings; the
  orchestrator never holds the raw noise.
- **Recall is a draw.** Any single finder surfaces a sample; the same lens re-run with different
  framing finds partly different things. Breadth (lenses) and repetition (passes) push the
  sample toward the population; cross-draw convergence is the only trustworthy completeness
  signal.

## Capture–recapture: what overlap can and cannot say (ground truth, 2026-07-04)

The ecology estimator (pass A finds N_A, pass B finds N_B, sharing M ⇒ population ≈
N_A·N_B/M) is only valid for **parallel blinded passes against one frozen commit**. The 035
audits ran on three different commits — fixes removed old problems and created new ones — so
they cannot feed the estimator at all. Hand-labeling
([archive/035-payment-idempotency/overlap-ground-truth.md](archive/035-payment-idempotency/overlap-ground-truth.md))
shows: 53 finding IDs collapse to **50 distinct problems**; true cross-audit identity overlap is
**1**; about a quarter of all 50 problems were **introduced by fixes**. The qualitative signal
survives measurement: v5's commit already contained ≥14 problems only v8 later named. Use
overlap to decide *whether to keep going* — low overlap reliably means "keep going"; high
overlap means "done" only as far as the passes are genuinely diverse (see the shared-blind-spot
assumption in [self-driving-loop-design.md](self-driving-loop-design.md)).

Re-raise lesson from the same labeling: of 5 times a later pass re-raised an already-decided
item, the re-raise **won 3 times** — the recorded decision was wrong and the code got better.
Hence the ledger rule: attach the prior decision to a re-find, never suppress it.

## Testing the tests: green ≠ proven (035)

The highest-value finding class was *"474/474 green, but the production code path is never
exercised."* Consequences baked into the runbooks: for each named failure mode ask *which test
goes red if I inject this bug*; prove fixes by revert-and-rerun (the cheap mutation test); name
what the suite structurally cannot reach (e.g. a SQLite-only suite says nothing about Postgres).

## Bolt 042 (thumbnails, 2026-07-13): what the skeptic layer is worth

First full pass under the committed script: 12 lenses, ~110 agents, ~3.5M tokens. The lenses
were ~11% of the agents; the **~98 skeptics were ~89%** and a similar token share. Yield: **2
genuine false positives caught (~4%)** and **7 findings correctly downgraded** to
plausible/not-triggerable; the remaining ~40 were corroborated — confidence and traces, no new
information. The single most valuable skeptic ran the real ImageSharp 3.1.11 API to prove
`IdentifyAsync` throws rather than returning null, refuting a "fail-open" finding two lenses
had raised independently. Takeaways:

- Skeptic value is **precision insurance, not recall**, and concentrates in (a) findings
  hinging on a checkable external fact and (b) High/Medium calls. Blanket 2×-per-finding
  over-pays.
- This drove the five cost measures in the script: dedup-before-verify, convergence-weighted
  skeptic tiers, output caps, read-once codePack, decided-re-raise skip (on 042-v8, 15 of 28
  findings were re-raises ⇒ ~40% of the skeptic layer removed).
- Low-tier skeptic models (replay run 2026-07-27,
  [experiment](experiments/skeptic-tiering/experiment-design.md)): Sonnet matched 13/17 Opus
  verdicts; the 4 flips were one-directional (`plausible→confirmed` — Sonnet accepts
  mutation-style traces on coverage-gap findings), all on Lows, and changed no synthesis call.
  The strict zero-flip gate failed, but the owner adopted the trade: **🟡 checks run on
  Sonnet** (~5× cheaper per token at similar token counts). ⚪ keep zero skeptics — Haiku is
  the designated model if they ever get one. Same-model rerun noise is real but unmeasured
  (one finding flipped between two Sonnet runs); an Opus-replay control would settle it.
- Measuring any such change honestly: re-run one frozen commit with and without it; if no
  outcome-changing verdict flips, the saving was free.

## Fix-generativity (042): why the fixer rules exist

Fixes create new review surface, and it became the dominant loop cost: **~13 of v6's 24 new
defects — including 4 of its 5 mediums — traced to earlier rounds' fixes** (one chain ran three
generations deep; one limiter fix alone yielded 3 findings; a mapping shipped without its
failure event; a stale-doc token took four rounds). A fixer that re-seeds the population each
round forces extra ~2M-token discovery passes. Hence the fixer contract's four rules (class
sweep · new-mechanism bar · design-check escalation · fresh-eyes micro-review) — owned by the
`/fix-review` skill. The micro-review exists because self-review reliably answers "no
regressions" over diffs the next pass then mines for a round of findings; the ~20k-token
design check exists because both deep 042 chains were designs pushed through the patch loop
unchecked (~3M tokens of later review to find what the check names up front).

## Why the delta tier exists (2026-07-14)

After a fix round, new defects live almost entirely in the **fix diff** (042-v4's headline
mediums came from a v1 fix; v6's from v4's fixes), yet the only blinded instrument was the ~2M
whole-feature pass. Delta discovery is the middle instrument: blinded lenses over the
cumulative diff since the last full pass, ~400–600k tokens. What it structurally cannot see —
original-population defects outside the fix surface, like 042's SplitQuery mis-paging bug,
found only on the *third* full pass — is exactly what certification exists to catch: the delta
tier replaces the middle full passes, never the safety net.

## Bolt 043 (cloud storage, 2026-07-22): the severity-based stop rule

The old exit ("delta quiet = no new finding of any kind") proved effectively unreachable: a
detector this sensitive finds *something* in any non-trivial diff, every fix round re-armed a
delta, and 043's two deltas cost **2.96M tokens to find 0 Highs** — nearly every finding
generated by our own fixes. Recalibration, all landed 2026-07-22:

- The loop re-arms on exactly three things: new 🔴, fix-caused 🟠 regression, reopened fix
  (the router's last row). Non-regression 🟠 get fixed and verified without re-arming a delta;
  🟡/⚪ go to the ledger backlog.
- Delta passes only after **delta-worthy** fix rounds (fixed a 🔴 / added or converted a
  mechanism / changed a design).
- Skeptic yield across 043's three discovery passes was ~3% (75 skeptics, 2 refutations) ⇒
  delta passes cut skeptic tiers deeper (on 043-v5's mix: ~3 skeptics instead of 15).
- Both deltas ran 6–7 lenses and blew the 400–600k budget 2–3× ⇒ the script now hard-caps
  delta passes at 5 lenses and enforces a 600k `tokenBudget` (skipped skeptics recorded as
  `unverified-over-budget`).

## Certification — and when a single pass may close it (043-v9, 2026-07-22)

The v7 certification pair was the **first full manifest ever run on this feature** — after six
lean/delta/verification passes that all read "merge-ready", it found a data-loss 🔴 (a retried
S3 upload re-sending a truncated stream, then the local original deleted) plus a shared-upload
data-loss class. Lesson: delta and verification passes are structurally blind to whole-feature
issues; risk-tiered certification is what catches them.

The pair's extra value over one pass rests entirely on the capture–recapture "two independent
looks agree ⇒ near-saturated" idea — the system's weakest, still-unproven assumption. So a
**single fresh full-manifest pass** is an acceptable certification close, recorded as an
owner-approved deviation, when (a) an equally broad blinded pass ran recently on near-identical
code and (b) the fix round since was small and independently verified. 043-v9 closed exactly
this way (2.87M tokens, 45 agents): honest scope — it certifies *no serious defect survives*,
not *zero defects remain*. Re-run the full pair when there is no recent broad look or the fix
round was large.

Calibration 2026-07-29 (from `reviews/system/review-v1.md`, SF1/SF2): the single-pass close
after a recent pair + small verified fix round — exercised by 043-v9 and 015-v5 — became the
**standard re-certification** rather than a recorded deviation; a full-loop-tier feature now
always ends with a fresh full-manifest pass after its last fix round (015's sign-off close,
which skipped that, prompted the rule); and every certification index row records the Mediums
still open at close, since no target has ever produced a zero-serious full pass and "certified"
must not read as "saturated".

First frozen-commit overlap measurement
([overlap-pair-v7.md](043-cloud-storage-provider/overlap-pair-v7.md), labeled 2026-07-27): the
v7 passes shared only 4 of 34 new findings (12%); 2 of 12 serious. Pass A alone would have
missed the D49 High. Chapman estimate: ~19 serious findable, 12 found ⇒ ~7 still hidden — a
lower bound, since shared-model blind spots inflate agreement. This settled the policy (owner
decision 2026-07-27): **pair on the full-loop tier, single full pass below it** — the second
pass provably earns its cost exactly where a miss is expensive.

## Where the tokens go, and how they were cut without cutting recall

The waste concentrates in the verification (skeptic) layer, never in lens breadth —
under-provisioning breadth is the documented 035 failure, so every cost measure trims skeptics
or redundant re-reading and none touches how many lenses run. On 042, three headline bugs were
each found by ~5 lenses ⇒ up to 10 skeptic runs per bug before dedup; dedup-before-verify and
convergence tiers remove exactly that. The codePack (when used) goes to lenses only — a skeptic
checks one finding and reads its files directly, so packs never multiply across the skeptic
layer.

Trace-first extended to full passes (2026-07-27), replacing the parallel guard+trace pair on
serious findings and the anti-groupthink guard-hunt on ≥3-convergence findings. Evidence: a
records audit across all passes with per-finding evidence on disk — 44 serious findings and
all 7 non-hinted ≥3-convergence findings — found **zero** cases where the second skeptic
changed an outcome, and zero `disputed` verdicts anywhere in the metrics history. The `disputed`
verdict is therefore no longer producible; it survives only in pre-2026-07-27 records. Roughly
a third fewer skeptic runs per full pass, with the remaining 🟡 runs already on Sonnet.
