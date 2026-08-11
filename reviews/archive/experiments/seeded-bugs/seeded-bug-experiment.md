---
type: experiment-protocol
status: run 1 complete (2026-07-04) — see seeded-run-results.md; run 2 queued (different implanter model, harder seeds)
created: 2026-07-04
owner: Matei Barba
tests: the three core assumptions in self-driving-loop-design.md
---

# Seeded-bug calibration experiment — protocol

Plant known bugs in a copy of a finished feature, run blinded discovery passes over it, and
measure — instead of guessing — what a pass actually catches. One run answers, with numbers:

1. **Per-pass recall, by severity.** What fraction of planted bugs does one discovery pass
   find? (Assumption 3: "serious problems surface early" — currently resting on one data point.)
2. **Severity calibration.** When a planted bug is found, is its severity judged right?
   (Assumption 2: the stop rule keys entirely on serious-vs-minor.)
3. **Independence.** Do two differently-framed passes miss the *same* planted bugs
   (shared blind spot — bad) or different ones (independent-ish — good)?
   (Assumption 1: "passes agreeing means clean" is only as strong as their diversity.)
4. **Bonus:** which lenses catch which bug classes — data for the manifest.

## The blinding trick: a sealed answer key

The orchestrating agent must not know the bugs, or its synthesis is biased and the scoring is
soft. So the **implanter is its own subagent**:

- The implanter creates the seeded branch and writes the answer key
  (`seeded-bugs-key.md`: per bug — file, exact change, class, intended severity, the failure
  it causes) to the experiment folder.
- The orchestrator **does not read the key** until both passes' syntheses are finalized and
  committed to file. Scoring happens only after that, and says so explicitly.
- Review lenses are blinded as usual (no `reviews/`, no git history in the worktree).

## Implant rules (the implanter's contract)

- **Target:** the bolt-035 payment feature files at the current branch tip — the code the
  ground truth knows best, so seed findings and real findings can be told apart.
- **Quota: 10 bugs.** 2 serious (a concurrency/atomicity break, a tenant-scoping/security
  break), 4 medium (wrong behavior under realistic-but-specific conditions: a boundary flip,
  a dropped validation, a provider-divergence, a wrong-field comparison), 4 minor/cleanup
  (a misleading comment, a dropped log event, dead code, a contract/doc mismatch).
- **Every bug must compile AND leave the full test suite green.** A seed the suite catches is
  CI's job, not review's — replace it with a different site. If a planned bug *can't* be made
  to survive the suite, record that: it means the suite genuinely covers that class (good news,
  and data).
- Bugs must be **plausible** — the kind a tired author writes: inverted condition, `>=` vs `>`,
  removed `when` filter arm, swapped field, deleted guard clause. No `/* BUG */` markers, no
  absurdities.
- Work in a **fresh worktree on a scratch branch** (`experiment/seeded-035`), one commit,
  **never pushed, never merged**. The worktree gets `reviews/` deleted and `.git` hidden from
  lenses per the blinding rules.

## Run protocol

1. Implanter agent: create worktree → implant 10 bugs → verify build green + full suite green →
   write `seeded-bugs-key.md` → report only "ready, N bugs implanted, suite green".
2. **Pass A:** blinded multi-lens discovery over the seeded worktree — the README manifest
   lenses, *skeptics off* (recall is the measurand; precision is scored against the key).
   Synthesis written to `seeded-run-pass-A.md`.
3. **Pass B:** same breadth, deliberately different framing (different lens phrasings, different
   reading order, e.g. "review as an incident post-mortem author" vs "review as a new
   maintainer") — the diversity knob assumption 1 depends on. Synthesis to `seeded-run-pass-B.md`.
4. Only now: open the key. Score both passes (sheet below). Non-seed findings are set aside
   and triaged separately — they may be *real* bugs (free discovery), and they must not
   contaminate recall numbers.
5. Delete the worktree and scratch branch. Results outlive the code.

## Scoring rules (fixed now, before anyone sees results)

- A finding **matches a seed** only if it names the same file and the same defect mechanism.
  "Something is off near that function" is not a catch. When in doubt: not a catch.
- **Recall** = seeds caught / seeds planted, reported per severity tier and overall, per pass.
- **Severity accuracy** = of caught seeds, fraction rated within one tier of intended.
- **Miss correlation** = of seeds missed by pass A, what fraction were also missed by pass B
  (compare against what independent misses would predict from each pass's recall).
- **Per-lens attribution** = which lens's finding caught each seed.
- Results + raw numbers go to `reviews/experiments/seeded-bugs/seeded-run-results.md`; one summary line goes into the
  design doc's assumptions section, replacing "untested".

## Cost (estimate before launching, per README cost discipline)

1 implanter + 2 passes × ~7 lenses + 2 syntheses ≈ **17–18 agents**, no adversarial-skeptic
stage. Roughly comparable to one v8-style audit. Re-run (cheaper, single pass) whenever the
review approach changes materially.

## What would make the results actionable

- Serious-tier recall high (both passes catch both serious seeds) → weak-but-real support for
  the stop rule; keep building.
- Serious-tier recall < 100% on either pass → the stop rule cannot rest on "two quiet passes"
  alone at current breadth; increase diversity (different models/framings) before trusting it.
- High miss correlation → passes are not independent samples; saturation math needs the
  diversity fix *architecturally*, not as an option.
- Severity misjudged on any serious seed → the severity devil's advocate moves up the build
  order.
