---
type: review-system-design
status: notes to build from — core assumptions still untested
created: 2026-07-04
updated: 2026-07-04
owner: Matei Barba
extends: README.md
---

# Self-Driving Review Loop — Design Notes

A plan for a code-review process that runs itself: it reviews the code, fixes what it finds,
checks the fixes held, then reviews again with fresh eyes, and repeats until it's confident the
code is clean — then boils all the review files down to two short plain-English summaries. This is
one piece of the bigger goal: an agentic system that does a development team's work with as little
human input as possible.

**How this file relates to [README.md](README.md):** the README owns the mechanics that already
exist — the review lenses and how to pick them, the severity scale, the verdicts, the
review/resolution file format, the fixer's contract. This file owns what doesn't exist yet: the
rules for running the loop without a person driving, the experiments that must pass before the
loop can be trusted, and the tools to build. Where both talk about the same thing, the README
wins. This file uses the README's names (with a plain explanation the first time) so the two
don't drift apart.

## Who these notes — and the loop's summaries — are written for

The reader is **me, months from now, coming back cold**. This is a side project: the owner
forgets the code, and even forgets how the app is supposed to behave. Long-term, the owner reads
only summaries and trusts the system to do the work. That reader is *technical but has no
context* — which is a different target than "non-technical." So summaries keep technical
precision but remove the need for memory: re-state what the feature is for, spell names out, and
**link every claim to something checkable** (the test, the commit, the metric). A summary the
reader can't check is a summary the reader is forced to trust — and a wrong system produces
confident, readable summaries too.

## What one real example taught us

Everything here comes from one feature reviewed in depth: the payments "don't charge twice"
feature (bolt 035). The feature was reviewed from scratch three separate times, each pass finding
about 15–18 issues — but **not against the same code**: fixes landed between the audits, so 13 of
the first audit's 15 findings were already fixed when the second audit ran. The hand-labeled
overlap of the three audits (now the reconciler's evaluation set) lives at
[035-payment-idempotency/overlap-ground-truth.md](035-payment-idempotency/overlap-ground-truth.md).
What we learned — with honest limits on each lesson:

- **There seem to be two kinds of problem.** Serious ones (could break the feature or hurt a
  customer) are few; minor cleanups are many and never run out — every fresh review turns up a
  different handful. *But note:* the "serious problems get found fast and stay fixed" part rests
  on exactly **one** serious bug, caught in the first pass. One data point is not a pattern. Some
  bug classes — security bugs especially — are known to hide well and be missed consistently.
  Don't build on "serious surfaces early" until it's measured (see the seeded-bug test below).
- **A review going quiet does NOT mean the code is clean.** This feature was once declared "done,
  nothing left," and the next fresh review found 18 more things. "The reviewer stopped finding
  things" measures the reviewer, not the code.
- **Why did each fresh review find different things?** Three reasons, and the data can't rank
  them: (a) the early reviews were too narrow — fix: run all the right specialists from the
  start (the README's lens manifest); (b) any single review, however broad, only catches a
  random sample of what's there — fix: repeat reviews until they agree; (c) the problems
  themselves changed between audits — the hand-labeling showed fixes removed old problems and
  *created* new ones (about a quarter of all 50 distinct problems ever recorded were introduced
  by fixes). An earlier version of these notes claimed (a) was the main cause and called a
  broad first review "the single biggest and cheapest improvement" — the data doesn't actually
  support ranking them. The seeded-bug experiment settles what (c) can't explain.
- **What actually happened across the seven rounds, stated precisely:** the rounds piled up
  because each fresh audit kept finding what earlier ones had missed. Separately, the loop
  declared "done" too early because a narrow fix-check round was allowed to stamp the whole
  feature clean. Two different failures, two different fixes: breadth/repetition for the first,
  keeping the two kinds of review apart for the second. (An earlier version of these notes blamed
  the round count on the mixed-up stops — that was wrong.)
- **Honest limit:** all of this is from one feature, and a payments one. Treat every conclusion
  as "observed once, on payments-like code."

## Three assumptions the design stands on — test them before building

The loop's correctness rests on three claims. All three are currently unproven, and all three are
cheaply testable. Test first, build second.

1. **"Independent reviews agreeing means the code is clean."** This is the weakest link. Every
   "independent" reviewer is the same AI model — same training, same habits, same blind spots —
   prompted similarly, reading the same code. Two such reviews agreeing can mean the code is
   clean, or it can mean they share a blind spot. Overlap alone cannot tell these apart. The
   near-disjoint findings across bolt 035's three audits suggest each pass is a noisy sample
   (which mildly helps — variety for free), but as passes start agreeing, we lose the ability to
   tell "converged on the truth" from "converged on the bias."
   *Defenses:* (a) **vary the reviewer** — different models, different prompt framings, different
   lens orderings per pass; diversity is the only real protection against shared blind spots;
   (b) **measure recall directly** with seeded bugs (below) instead of inferring it from overlap.
   **Run 1 status (2026-07-04): STILL UNTESTED.** The seeded-bug run got 10/10 recall on both
   passes — with zero misses, miss-correlation is undefined, so independence could not be
   measured. Worse, the implanter and finders were the same model (Opus), so high recall is
   partly a shared-brain artifact. Run 2 needs a *different* implanter model and *harder* seeds
   that actually produce misses. See [seeded-run-results.md](experiments/seeded-bugs/seeded-run-results.md).
2. **"The loop can tell serious from minor."** The entire stop rule keys on this classification,
   and it's made by the same fallible reviewer whose recall we already distrust. One serious bug
   mislabeled minor goes silently to the backlog and the loop declares the feature clean.
   **Run 1 status: measured, encouraging.** Across 10 seeds the reviewers erred *high* ~4 times
   and *never* low — i.e. never under-rated a serious as minor, which is the dangerous direction
   for a backlog-and-escalate loop. *Revised defense:* a severity *deflation* check (cut human
   noise from over-escalation) looks more useful than the devil's-advocate *inflation* check,
   since under-rating didn't occur. Small n; recheck with harder seeds.
3. **"Serious problems surface early."** **Run 1 status: supported, small-n.** Both serious seeds
   were caught by both passes at correct severity; with bolt-035's original real High that's 3/3
   serious defects caught on first exposure. Still too few to trust for the bugs that hide best
   (deep security). Keep measuring per-severity recall as more seeds/features run.

### The seeded-bug test — the single most important experiment

Take a copy of a finished feature. Deliberately plant ~10 known bugs of varied severity and kind
(a race, a security hole, an off-by-one, a silent data loss, some cleanups). Run a blinded
discovery pass. Now recall is a **measurement, not a guess**: what fraction of planted bugs were
found, per severity? Were severities judged correctly? Which lenses caught what? Then repeat with
a *differently framed* (or different-model) pass and check whether the two passes missed the
**same** planted bugs (shared blind spot — bad) or different ones (independent-ish — good). One
afternoon of work, and it converts assumptions 1–3 from beliefs into numbers. Re-run it whenever
the review approach changes materially.

## Which changes get the loop at all (entry policy)

A full discovery pass is expensive (≈7 lenses + 2 skeptics per finding ≈ 40+ agents), and
certification needs several. Without an admission rule, a self-driving loop burns that on a
30-line styling tweak — or gets bypassed ad hoc, which defeats "self-driving." Provisional
tiers, to be revised once the metrics (below) say what things actually cost and catch:

| Change touches… | Treatment |
|---|---|
| Money, auth/permissions, data loss, concurrency, migrations, new external input | **Full loop** — fix + verify until certified (protocol below) |
| Ordinary feature work without the above | **One discovery pass** + fixes + per-fix verification; escalate to full loop if it finds anything serious |
| Docs, copy, styling, config with no behavior change | One quick pass, or skip |

Any tier escalates upward if a pass finds something serious. Never de-escalate mid-loop.

## When the loop should stop

Two separate "stops." Mixing them is what let bolt 035 stamp itself done while 18 findings
remained.

1. **Did one fix work?** Certain and easy: break the fix on purpose — a test should fail; put it
   back — it should pass. If both happen, the fix is real.
2. **Is the whole feature clean enough to move on?** Stop when **two fresh, independent discovery
   reviews in a row, against the same frozen code, find no new serious problems.**
   **This rule is a hypothesis, not a proven result.** Bolt 035 never actually reached this
   state — we stopped by judgment, not by the rule firing. The rule would have fired there (the
   second and third audits found no new *serious* problems), but that's one retrospective check,
   not validation. The metrics below exist to test it.

Rules that keep this honest:

- A fix-check can never declare the whole feature done. Only fresh, wide discovery reviews can.
- **The counter resets on any new serious finding — including one a fix introduced.** (In bolt
  035, 2 of the 5 findings raised outside full audits were regressions caused by fixes.) With
  that reset, the loop can in principle run forever, which means in practice the backup stops
  below are the *real* ceiling. Say so; don't pretend otherwise.
- **Backup stops:** a maximum number of rounds, or a round that turns up too little to justify
  its cost. Hitting a backup stop with serious problems still appearing → stop and hand to a
  person; the loop never calls it "done" on its own from a backup stop.
- **Overlap is a rough gauge of how much is still hidden** — two fresh reviews finding mostly
  *different* things means keep going. But the reverse is only weak evidence: two *similar*
  reviewers finding the same things may just share attention habits (assumption 1). Overlap says
  "keep going" reliably; it says "you're done" only as well as the passes are genuinely diverse.

### The certification protocol (what "frozen snapshot" means in practice)

Fixing-as-you-go and judging-against-frozen-code contradict each other unless the mechanics are
explicit:

1. All known serious findings are fixed and verified. Freeze the code at commit X.
2. Run **two independent blinded discovery passes in parallel, both against commit X.** Parallel,
   not sequential — so neither pass sees the other's results, and both judge identical code.
3. The reconciler (below) merges their findings. Any new serious finding → fix it, verify it,
   freeze a new commit, go back to step 1 (counter reset). No new serious findings → **certified
   at commit X**.
4. Minor findings from certification passes go to the backlog. Fixing a backlogged minor later
   needs only normal fix-verification, not re-certification — unless the fix touches code in the
   full-loop tier of the entry policy.

## Blinding, done properly

A fresh review is only fresh if it can't see what past reviews found. Deleting the `reviews/`
folder is not enough. Findings leak through every one of these channels:

- the `reviews/` folder itself (review + resolution files);
- **commit messages** — the fixer's own contract requires finding IDs in them, so `git log` hands
  a "fresh" reviewer the complete finding history;
- **test names** — `Should_not_double_charge_when_retried` announces the old bug;
- code comments left by fixes; PR titles/descriptions; branch names.

Policy: keep the IDs in commits and the descriptive test names — they're too valuable to give up.
Instead, a discovery pass's workspace is **the code at the frozen commit with no git history and
no `reviews/` folder**, and its prompt forbids fetching logs or PR data. A **blinding auditor**
(a plain script, not an agent — see tools) verifies this mechanically before every discovery
pass: no finding-ID strings anywhere, no review files, no reachable history.

Accepted leak, stated openly: test names and fix-shaped code will always hint at past findings.
Blinding therefore *degrades as rounds accumulate* — every fix embeds evidence of its finding.
That's one more reason certification runs its passes in parallel against one commit rather than
one after another.

## How the loop works — the pieces

- **Two kinds of review** (the README's *discovery* vs *verification*): a wide, blinded one that
  looks at the whole feature — run rarely; and a quick, anchored one that checks a specific fix —
  run per fix. The README owns the full contract for both.
- **Front-load breadth** using the README's **lens manifest** — the checklist that maps what the
  change touches (a migration, a second provider, money, concurrency…) to which specialists must
  run. This helps; whether it's the *biggest* lever or repetition matters more is exactly what
  the seeded-bug test measures.
- **A fixer** that fixes each problem, adds the regression test the review asked for, and
  self-reviews its own diff before handing back (fixes caused 2 of bolt 035's 5 out-of-audit
  findings). The README owns the fixer contract; the `/fix-review` skill implements it.
- **The fixer never grades itself.** A separate verification pass checks each fix.
- **A reconciler** (the README's name; earlier notes said "comparer") that matches findings
  across passes — specified below, because it's the piece everything measurable depends on.
- **A severity devil's advocate**: after each discovery pass, a separate reviewer takes
  everything classified *below* serious and argues the opposite — "make the case this is
  serious." Anything it makes a plausible case for gets escalated (to serious, or to the human
  summary). This guards assumption 2, the unwatched single point of failure in the stop rule.
- **A one-page "needs your decision" summary each round** — serious items only, each with a
  suggested action; minors filed to the backlog automatically. With two additions:
  - a **dissent section**, built from raw data rather than the review's self-assessment: the
    new-findings-per-pass trend, lenses that didn't run, borderline severity calls the advocate
    flagged, known blinding leaks. Reasons to doubt, next to reasons to believe.
  - the **evidence rule** from the top of this file: every claim links to its test, commit, or
    metric.
- **Safety default:** the loop asks a person before declaring a feature done — until it has
  *earned* that right through track record (below), not because its summaries sound confident.
  When it pauses, the easy default answer is "keep going," never "done."

## The reconciler — the piece that lets the loop learn

**Job:** after review passes, decide which findings are the *same problem* (found again, or
already deferred) and which are *new*. Without it the loop can't measure overlap, can't tell
progress from churn, and re-argues decisions it already made (bolt 035's third audit unknowingly
re-raised an already-accepted deferral as if it were new).

**Spec:** *In:* two or more finding lists (file/line, severity, description) plus the code at the
reviewed commit. *Out:* matched groups with a confidence and a one-line reason per match;
everything unmatched is new. It also maintains the running ledger per feature (the README's
"canonical ledger"): every distinct problem ever found, its status, and which passes found it —
the memory that provides the overlap numbers and lets a pass see a prior decision before
re-arguing it. **Important nuance from the hand-labeling:** of the 5 times a later pass re-raised
an already-decided item, the re-raise *won* 3 times — the recorded "intentional" or "not a
finding" call was wrong and the code got better. So the ledger must **link** a re-find to the
prior decision and hand that context to the pass; it must never auto-suppress the re-find.

**Its two mistakes are not equally bad — design for that.** Wrongly *merging* two different
problems makes overlap look higher → the loop stops early → a real problem ships. Wrongly
*splitting* one problem into two makes overlap look lower → the loop runs longer → money wasted.
Waste is recoverable; shipped bugs aren't. **When unsure, split.**

**The evaluation set exists (labeled 2026-07-04):**
[035-payment-idempotency/overlap-ground-truth.md](035-payment-idempotency/overlap-ground-truth.md).
Headline: 53 finding IDs collapse to 50 distinct problems; the true cross-audit identity overlap
is **one** problem, not the ~4 previously assumed; the audits ran against different commits, so
capture–recapture math doesn't apply to them at all (only to parallel passes on a frozen commit).
The file includes the ten hard rulings a reconciler must reproduce and a scoring guide. Every
reconciler version gets scored against it before being trusted.

## Measuring — so the next five features aren't anecdotes too

"Use it on more PRs and see if the conclusions hold" is empty without defining what to record.
Every review pass appends a row to a small structured metrics file (per feature, plus a global
roll-up): pass type (discovery/verification), commit, lenses run, count of new serious / new
minor / re-finds / refuted, rough cost (agents, tokens), and overlap with the prior pass (from
the reconciler). Per finding: severity, finding lens, verdict, fix commit, ever reopened.

Nearly free to record at review time; impossible to reconstruct later. This data is what
eventually answers: does the stop rule work? do findings-per-pass actually decay? what does a
pass cost? which lenses earn their keep? It also feeds the **track record** — the only legitimate
basis for the loop earning autonomy: measured seeded-bug recall, plus the count of
certified-clean features that later turned out to have a serious bug. Trust comes from that
record, not from a human reading a summary and nodding (the human knows less about the code every
month — see *Who these notes are for*).

## The backlog needs a drain

Every feature deposits ~15 minor findings. A list nothing ever processes is not "tracked" — it's
deletion with extra steps, and the same style issue gets re-filed by every feature that touches
the area. So: a **backlog groomer** runs every N features (or on request), dedupes across
features (reconciler again), groups by subsystem, batch-fixes the ones worth fixing in one sweep
(each through normal fix-verification), and *explicitly closes* the not-worth-its with a reason,
so they stop being re-found and re-filed.

## Wrapping up a finished review (the compression step)

When a feature's review is done, boil the review files (bolt 035 grew to 14) down to two short
summaries — "what we checked and found" and "what we did about it" — written for the
cold-context technical reader defined at the top, following the evidence rule (claims link to
tests/commits/metrics). Move the detailed files into a side folder; don't delete them.

**The compression rule (learned the hard way): shorten the words, keep the ideas.** Cut
repetition and filler — never a vital idea. In particular, every "we're leaving this for later"
decision must survive into the summary. Since "did the summary drop anything?" is exactly the
kind of claim this system doesn't take on faith, the compress tool's output gets one cheap check:
a separate pass compares the summary against the ledger and lists anything load-bearing that went
missing.

Two skills back this: **compress-review** (runs once per finished feature; input: the feature's
review folder + ledger; output: the two summaries + files tidied away + the omission check) and a
general **plain-language** skill it calls for the actual writing (also useful for PR
descriptions, release notes, status updates). Full specs stay simple deliberately — these are the
least load-bearing tools in the plan and are built last.

## The tools to build — what, where it plugs in, and why this order

| # | Tool | Form | When it runs | Why this position |
|---|------|------|--------------|-------------------|
| 1 | **Reconciler** | skill + hand-labeled bolt-035 eval set | after every discovery pass; during certification; inside the groomer | Everything measurable (overlap, ledger, saturation, dedup) depends on it, and its test data already exists for free |
| 2 | **Seeded-bug calibration** | skill / workflow script | before building the loop; re-run when the review approach changes | Tests all three core assumptions; turns recall and severity accuracy into numbers; settles breadth-vs-repetition |
| 3 | **Metrics recorder** | tiny skill, or a stage in the review workflow | every pass, starting with the very next review | Nearly free now, unrecoverable later; the stop-rule hypothesis is untestable without it |
| 4 | **Blinding auditor** | plain script / pre-pass hook — not an agent | before every discovery pass | Mechanical checks (grep IDs, no `reviews/`, no git history) don't need a model; cheap and strict |
| 5 | **Severity devil's advocate** | subagent stage in the review workflow | after finders + skeptics, before synthesis | Guards the stop rule's single point of failure |
| 6 | **Dissent section** | part of the summary stage | every round summary | Partial fix for the shared-brain approval problem; needs metrics (3) to exist |
| 7 | **Backlog groomer** | skill, run every N features | periodic / on request | Needs the reconciler (1) for cross-feature dedup |
| 8 | **compress-review + plain-language** | skills | once per finished feature | Most specified, least consequential — last |

**How they extend the existing review workflow** (the README's prototyped `Workflow` script):

- *Discovery pass:* blinding audit (4) → fan out manifest lenses → adversarial skeptics →
  severity advocate (5) → synthesis → reconciler (1) updates the ledger → metrics append (3) →
  summary with dissent (6).
- *Verification pass:* fixer (`/fix-review`) → fix-verification → metrics append (3). No blinding,
  no advocate — it's anchored on purpose.
- *Certification:* the two-parallel-passes protocol above, reconciled by (1), recorded by (3).
- *Every N features:* groomer (7). *Per finished feature:* compress (8).

## Honest concerns

1. **Everything still rests on one example** (payments). Now with a concrete remedy instead of a
   shrug: the seeded-bug experiment, the metrics file, and revisiting the entry-policy tiers once
   a few more features have real numbers.
2. **The human-approval step gets *weaker* over time, by design.** The owner reads only summaries
   and knows less about the code every month — that's the goal, not a bug. But it means approval
   by reading can never be the trust mechanism: the summary is written by the same AI that did
   the review, all the reviewers share one brain, and a system with a blind spot writes a
   confident summary anyway. Trust must come from the measured track record (seeded-bug recall,
   certified features that later broke) plus reviewer diversity — and, at the highest-risk
   points, the owner occasionally reading the actual code, not the summary about it.
3. **Building this is real work** — but now ordered, and the first three tools are each roughly
   an afternoon.

## Next steps

1. ~~Hand-label the bolt-035 overlap~~ **Done (2026-07-04):**
   [overlap-ground-truth.md](035-payment-idempotency/overlap-ground-truth.md) — 53 IDs → 50
   distinct problems, cross-audit identity overlap = 1, ten hard rulings + scoring guide.
   Awaiting owner spot-check before freezing.
2. ~~Build the metrics recorder~~ **Done (2026-07-04):** [metrics-schema.md](metrics-schema.md)
   defines the per-pass line; [035's metrics.jsonl](035-payment-idempotency/metrics.jsonl) is
   backfilled with all ten passes; the README's Record step now requires the append on every
   pass.
3. ~~Run the seeded-bug experiment~~ **Run 1 done (2026-07-04):**
   [seeded-run-results.md](experiments/seeded-bugs/seeded-run-results.md) — 10/10 recall both passes; severity errors all
   in the safe (high) direction; DB-parity + completeness lenses were the sole catchers of 2/10
   seeds. But independence is still untested (zero misses) and implanter==finder model — a **run
   2 with a different implanter model and harder seeds** is queued in the results file.
4. Meanwhile, **review the ready PRs with the current README approach** — every one is another
   real example for checking whether independent reviews settling down actually tracks clean
   code.
5. Then build the loop stages in the order in the table.
