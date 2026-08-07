---
type: review-system-design
status: research notes — core assumptions still under test
created: 2026-07-04
updated: 2026-07-24
owner: Matei Barba
extends: README.md
---

# Self-Driving Review Loop — Design Notes

A plan for a review process that runs itself: review, fix, verify, review again with fresh
eyes, stop when confident, then boil everything down to two short plain-English summaries. One
piece of the bigger goal: an agentic system doing a development team's work with as little
human input as possible.

**Split of responsibilities:** [README.md](README.md) + the runbooks own everything
*operational* — router, entry tiers, stop rule, pass mechanics, file shapes.
[rationale.md](rationale.md) owns the *evidence*. This file owns the *research*: the
assumptions that must hold for autonomy, the experiments that test them, and the tools still
to build. On any overlap, the README wins.

## Who the loop's summaries are written for

The reader is **the owner, months from now, coming back cold** — technical but with no
context. Summaries keep technical precision but remove the need for memory: re-state what the
feature is for, spell names out, and **link every claim to something checkable** (the test,
the commit, the metric). A summary the reader can't check is a summary the reader is forced to
trust — and a wrong system produces confident, readable summaries too.

## Three assumptions autonomy stands on — and their test status

1. **"Independent reviews agreeing means the code is clean."** The weakest link. Every
   "independent" reviewer is the same model — same training, same blind spots — reading the
   same code. Agreement can mean clean code or a shared blind spot; overlap alone can't tell
   them apart. *Defenses:* vary the reviewer (models, framings, lens orderings); measure
   recall directly with seeded bugs. **Status: untested.** Seeded run 1 (2026-07-04) got
   10/10 recall on both passes — zero misses means miss-correlation is undefined, and the
   implanter and finders shared a model, so high recall is partly a shared-brain artifact.
   Run 2 needs a different implanter model and harder seeds
   ([results](experiments/seeded-bugs/seeded-run-results.md)).
2. **"The loop can tell serious from minor."** The stop rule keys on this classification, made
   by the same fallible reviewer. One serious bug mislabeled minor goes silently to the
   backlog. **Status: measured once, encouraging** — across 10 seeds the reviewers erred high
   ~4 times and never low (the dangerous direction). A severity *deflation* check therefore
   looks more useful than an inflation check. Small n; recheck with harder seeds.
3. **"Serious problems surface early."** **Status: supported, small-n** — 3/3 serious defects
   (2 seeded + 035's real High) caught on first exposure at correct severity. Too few to
   trust for the bug classes that hide best (deep security). Keep measuring per-severity
   recall.

The stop rule and certification protocol themselves (now operational in the README) are
**hypotheses under test**, not proven results: no feature has yet closed by the rule firing on
its own terms twice. The metrics exist to test them — does new-serious-per-pass decay? does a
certified feature later break?

### The seeded-bug test — the single most important experiment

Plant ~10 known bugs of varied severity and kind in a copy of a finished feature. Run a
blinded discovery pass: recall becomes a **measurement** — fraction found per severity,
severity accuracy, which lenses caught what. Repeat with a differently-framed or
different-model pass and check whether the two missed the **same** seeds (shared blind spot)
or different ones (independent-ish). Re-run whenever the review approach changes materially.
Run 1: [experiments/seeded-bugs/](experiments/seeded-bugs/seeded-run-results.md). Run 2
(different implanter model, harder seeds): queued.

## Blinding, done properly (auditor spec)

A fresh review is only fresh if it can't see past findings. Leak channels: the `reviews/`
folder; **commit messages** (the fixer's contract puts finding IDs there); **test names**
(`Should_not_double_charge_when_retried` announces the bug); fix-shaped code and comments; PR
titles; branch names; a hand-assembled codePack.

Policy: keep IDs in commits and descriptive test names — too valuable to give up. Instead, a
discovery workspace is **the code at the frozen commit with no git history and no `reviews/`
folder**, and its prompt forbids fetching logs or PR data. The **blinding auditor** — a plain
script, not an agent — verifies mechanically before every discovery pass: no finding-ID
strings, no review files, no reachable history, and a scan of the codePack if one is used.
**Until it exists, blinding is best-effort** (the runbook says so). Accepted leak, stated
openly: fixes embed evidence of their findings, so blinding degrades as rounds accumulate —
one more reason certification runs its passes in parallel against one commit.

## The reconciler — the piece that lets the loop learn

**Status: BUILT** as the `reconcile-findings` skill (2026-07-27) and scored blind against the
eval set — 0 over-merges on the hard cases, both stretch goals met (score details in the
skill). The spec below remains the contract it must keep satisfying.

**Job:** decide which findings across passes are the *same problem* and which are *new*.
Without it the loop can't measure overlap, can't tell progress from churn, and re-argues
decisions it already made.

**Spec.** *In:* two or more finding lists (file/line, severity, description) + the code at the
reviewed commit. *Out:* matched groups with a confidence and a one-line reason each;
everything unmatched is new. It maintains the per-target ledger (`D#` rows, per-pass
convergence counts, `hinted` flags — manufactured agreement must not inflate overlap). It
**links** a re-find to the prior decision and hands both to the synthesizer; it never
auto-suppresses a re-find (3 of 5 recorded re-raises overturned the prior call).

**Asymmetric errors — design for them.** Wrongly *merging* two problems inflates overlap → the
loop stops early → a bug ships. Wrongly *splitting* one problem deflates overlap → the loop
runs longer → money wasted. Waste is recoverable; shipped bugs aren't. **When unsure, split.**

**Eval set exists:**
[archive/035-payment-idempotency/overlap-ground-truth.md](archive/035-payment-idempotency/overlap-ground-truth.md)
— 53 IDs → 50 distinct problems, ten hard rulings, a scoring guide. Every reconciler version
gets scored against it before being trusted.

## The severity devil's advocate

After each discovery pass, a separate reviewer takes everything classified *below* serious and
argues the opposite: "make the case this is serious." A plausible case escalates the finding.
This guards assumption 2 — the stop rule's unwatched single point of failure. (Run-1 data says
errors ran in the safe direction, so a *deflation* check on over-escalation may earn its keep
first; keep both in mind.)

## The round summary the owner actually reads

**Status: BUILT** as the `owner-summary` skill (2026-07-27); required output of every pass per
the runbooks; first live page: `043-cloud-storage-provider/summary-v9.md`. The spec below
remains the contract.

One page per round, serious items only, each with a suggested action; minors filed to the
backlog automatically. Two required parts:

- a **dissent section**, built from raw data rather than the review's self-assessment: the
  new-findings-per-pass trend, lenses that didn't run, borderline severity calls, `hinted`
  convergence, skeptic-failure placeholders, known blinding leaks. Reasons to doubt, next to
  reasons to believe.
- the **evidence rule**: every claim links to its test, commit, or metric.

## The backlog needs a drain

Every feature deposits ~15+ minor findings; a list nothing processes is deletion with extra
steps. The **backlog groomer** runs every N features (or on request): dedupes across features
(reconciler again), groups by subsystem, batch-fixes what's worth fixing (each through normal
fix-verification), and *explicitly closes* the rest with a reason so they stop being re-found.

## Wrapping up a finished feature (the compression step)

Boil the review folder down to two short summaries — "what we checked and found" and "what we
did about it" — written for the cold-context reader, following the evidence rule. Move the
detailed files to `archive/`; don't delete them. **Shorten the words, keep the ideas** — every
"we're leaving this for later" decision survives into the summary. The output gets one cheap
check: a separate pass compares the summary against the ledger and lists anything load-bearing
that went missing. Backed by a **compress-review** skill + a general **plain-language** skill.

## Measuring — so the next five features aren't anecdotes

Every pass appends a metrics line ([metrics-schema.md](metrics-schema.md)); per finding:
severity, lens, verdict, fix commit, ever-reopened. Nearly free at review time, impossible to
reconstruct later. This is what eventually answers: does the stop rule work? do
findings-per-pass decay? what does a pass cost? which lenses earn their keep? It also feeds
the **track record** — the only legitimate basis for autonomy: measured seeded-bug recall,
plus the count of certified-clean features that later turned out to have a serious bug
(collector live since 2026-07-30: [track-record.md](track-record.md), enforced by the records
auditor). Trust comes from that record, not from a human reading a summary and nodding.

## Tools — status and build order

Built and operating:

| Tool | Where |
|---|---|
| Discovery fan-out script (lenses → in-pass dedup + `hinted` → trace-first skeptics with severity-tiered models, delta budget guard, decided-re-raise skip) | [lib/discovery-review.wf.js](lib/discovery-review.wf.js) |
| Reconciler (`reconcile-findings` skill — scored vs the 035 ground truth: 0 over-merges) | `.claude/skills/reconcile-findings/SKILL.md` |
| Owner summary (`owner-summary` skill — one page per pass: decisions · dissent · evidence) | `.claude/skills/owner-summary/SKILL.md` |
| Metrics recorder (schema **v2** 2026-07-30: per-finding lens attribution, fix-lineage, severity-delta) | [metrics-schema.md](metrics-schema.md) |
| Records auditor (schema validation · tally cross-check · review↔metrics pairing · commit reachability from pushed refs · citation-leak count) | [lib/records-auditor.mjs](lib/records-auditor.mjs) |
| Loop driver (audit → route → announce → gate → execute → record; session-model guard + resume protocol; eval-tested + independently reviewed 2026-07-30) | `.claude/skills/loop-driver/SKILL.md` + [lib/route-next-pass.mjs](lib/route-next-pass.mjs) |
| Verification runbook | [runbook-verification.md](runbook-verification.md) |
| Fixer contract | `/fix-review` skill |
| Per-target ledgers (hand-maintained) | `reviews/<target>/ledger.md` |

To build, in order — **owner-facing output first** (the owner is the loop's current
bottleneck: finding volume and token cost, not recall):

| # | Tool | Form | When it runs | Why this position |
|---|------|------|--------------|-------------------|
| 1 | **Seeded-bug run 2** | workflow script | before trusting any stop rule | Different implanter model + harder seeds; tests all three assumptions |
| 2 | **Verification-pass script** | small script or `/fix-review` handback stage | after every fix batch | Most-run stage; spec already written (runbook) |
| 3 | **Blinding auditor** | plain script, pre-pass | before every discovery pass | Mechanical; the citation-leak half now lives in the records auditor — remaining: the discovery-workspace reachability check |
| 4 | **Severity devil's advocate** | subagent stage | after skeptics, before synthesis | Guards the stop rule's weak point |
| 5 | **Backlog groomer** | skill, every N features | periodic | Backlog is already accumulating; needs the reconciler |
| 6 | **compress-review + plain-language** | skills | once per finished feature | Uses the owner-summary machinery; least load-bearing standalone |

System backlog (from live runs): a reusable DB/migration-parity lens (dual SQLite/Postgres is
a recurring risk) · auto-append findings as inline PR comments once `gh` is available.

## Honest concerns

1. **Everything rests on two and a half worked examples** (payments end to end; thumbnails and
   cloud storage mid-loop). Remedy in motion: seeded runs, the metrics file, and revisiting
   the entry tiers once more features have real numbers.
2. **The human-approval step gets weaker over time, by design.** The owner reads only
   summaries and knows less about the code every month. Approval-by-reading can never be the
   trust mechanism: the summary is written by the same AI that did the review. Trust must come
   from the measured track record plus reviewer diversity — and, at the highest-risk points,
   the owner occasionally reading the actual code.
3. **Building this is real work** — but ordered, and the first three tools are each roughly an
   afternoon.

## Next steps

1. Seeded-bug run 2 with a different implanter model (tool 1).
2. Keep reviewing real PRs under the runbooks — each is another datapoint for whether
   "independent reviews settling down" tracks clean code, and the first live measurement of
   the 2026-07-27 cost rules. The reconciler and owner summary run on every pass from here.
