---
type: review-system-design
status: research notes — v2, updated 2026-08-11
created: 2026-07-04
updated: 2026-08-28
owner: Matei Barba
extends: README.md
---

# Self-Driving Review Loop — Design Notes (v2)

A plan for a review process that runs itself: review, fix, verify, review again with fresh
eyes, stop when confident, then close into records a cold reader can trust. One piece of the
bigger goal: an agentic system doing a development team's work with as little human input as
possible. This v2 replaces the 2026-07-24 notes; v1 survives in git history. Since v1, most
of the machinery got built. The open work is no longer tools — it is proof and unattended
operation. The two closing sections hold the completion audit and the direction options.

**Split of responsibilities:** the [README](../README.md) + the runbooks own everything
*operational* — router, entry tiers, stop rule, pass mechanics. The
[doc contract](../rules/doc-contracts.md) owns the *file shapes and the language rules*.
The [rationale notes](rationale.md) own the *evidence*. The
[path constants](../lib/paths.mjs) own every path for scripts. This file owns the
*research*: the assumptions autonomy needs, the experiments that test them, and what is
still missing. On any overlap, the README wins.

## Who the loop's records are written for

The reader is **the owner, months from now, coming back cold** — technical but with no
context. Records keep technical precision but remove the need for memory: re-state what the
feature is for, spell names out, and **link every claim to something checkable** (the test,
the commit, the metric). A record the reader can't check is a record the reader is forced to
trust — and a wrong system produces confident, readable records too. Since 2026-08-10 this
is enforced, not aspired to: every artifact follows a template with size caps, a
deterministic lint plus a Sonnet judge check each round's files, and the summary template
requires an evidence link on every claim.

## Three assumptions autonomy stands on — and their test status

1. **"Independent reviews agreeing means the code is clean."** The weakest link. Every
   reviewer is the same model family — same training, same blind spots — reading the same
   code. Agreement can mean clean code or a shared blind spot; overlap alone can't tell them
   apart. **Status: still untested, now with a recorded violation.** Seeded run 1
   (2026-07-04) got 10/10 recall on both passes — zero misses means miss-correlation is
   undefined, and the implanter and finders shared a model, so the finders were primed by a
   co-thinker ([results](../archive/experiments/seeded-bugs/seeded-run-results.md)). New
   since v1: the first frozen-commit overlap measurement (043-v7 pair, evidence in
   the [rationale notes](rationale.md)) — the two passes shared 4 of 34 new findings (12%), 2 of 12
   serious; the estimate said ~19 serious findable where 12 were found. Low overlap proves
   the samples were far from saturation; it cannot separate real diversity from shared blind
   spots — only misses can. Meanwhile the system review recorded SF14: two targets were
   certified under this untested rule, and the owner deferred run 2 on 2026-07-29, keeping
   the violated gate visible on purpose
   ([resolution](../system/review-v1/resolution-v1.md)). Interim measure: the
   [track record](../state/track-record.md) — 2 certifications under watch, 0 escapes — weak,
   because an escape only surfaces if a later pass happens to look at certified code.
2. **"The loop can tell serious from minor."** The stop rule keys on this classification,
   made by a single synthesis judgment. **Status: measured once, encouraging, still
   small-n.** Across run 1's 10 seeds the reviewers erred high ~4 times and never low (the
   dangerous direction). New since v1: every synthesis severity change is now recorded per
   finding (`sev_delta`, [metrics schema](../rules/metrics-schema.md) v2 fields) — the data
   to check calibration accumulates on every pass, and nothing reads it yet.
3. **"Serious problems surface early."** **Status: supported for a target's first broad
   exposure; fixes re-seed the population.** Run 1: both serious seeds caught by both
   passes. 043-v7 — the first full-manifest pass on that feature — found a data-loss 🔴 six
   leaner passes had sat on top of. 044-045's v1 named all 9 of its 🔴 at first exposure.
   But fix rounds mint new serious late: 044-045's verifications found 1 new 🔴 and 25 new
   🟠 after v1, almost all in fix-changed code. The re-arm rule and the delta tier exist
   exactly for this; the open risk stays the bug classes that hide best (deep security).

The stop rule and certification protocol are still **hypotheses under test**: the undeviated
path (pair → quiet → certified) has completed **zero** times (SF1). The five closed targets
closed as: 2 certifications (043 by a written single-pass deviation later standardized, 015
by owner sign-off after its certification), 3 owner sign-offs with no certification pass
(035, 042, 044-045) — all on the [index](../state/index.md). Escapes so far: 0.

### The seeded-bug test — still the single most important experiment

Plant ~10 known bugs of varied severity in a copy of a finished feature. Run a blinded
discovery pass: recall becomes a **measurement** — fraction found per severity, severity
accuracy, which lenses caught what. Repeat with a differently-framed pass and check whether
the two missed the **same** seeds (shared blind spot) or different ones (independence).
Run 1 ([results](../archive/experiments/seeded-bugs/seeded-run-results.md)) proved the
manifest's worth — the db-parity and completeness lenses were the sole catchers of 2 of 10
seeds — but was too easy to produce misses. Run 2's design, unchanged since v1 and still
owed: a **different implanter model or a human implanter**, **harder seeds** aimed at
producing misses (subtle logic in unchanged collaborators, no contradicting comment,
cross-file invariants), per-severity recall, and shared-miss analysis across the two passes.
Owner status: deferred 2026-07-29 ("not now"); estimated ~2M+ tokens (system review
recommendation 7). Re-run whenever the review approach changes materially.

## Blinding, done properly

A fresh review is only fresh if it can't see past findings. Leak channels: the review
records; commit messages (the fixer's contract puts ids there); test names; fix-shaped code;
PR titles; branch names; a hand-assembled codePack. Policy unchanged: ids in commits and
descriptive test names are the accepted leak, and fixes embed evidence of their findings, so
blinding degrades as rounds accumulate — one more reason certification runs against one
frozen commit.

Progress since v1 — the citation half is built: code-comment citations were swept 371 → 0
(2026-07-29) and the [records auditor](../lib/records-auditor.mjs) now scans on every run
with a target of 0. **Still unbuilt: the workspace half** — a plain pre-pass script proving
the discovery workspace cannot reach the review records, git history, or finding-id strings,
plus a codePack scan. Until it exists, blinding stays best-effort, enforced by prompts only
(the [discovery runbook](../runbooks/runbook-discovery.md) says so).

## The reconciler — the piece that lets the loop learn

**Status: BUILT and load-bearing.** The `reconcile-findings` skill decides which findings
across passes are the same problem, and now also **mints ids**: each new defect gets the
next `PPW-<n>` from the [id counter](../state/id-counter), its ledger row, and its detail
block — before the review file is written, so every record carries the permanent id, per
the [doc contract](../rules/doc-contracts.md)'s ID rules. Scored blind against the 035
ground truth on 2026-07-27: 0 over-merges on the hard cases, both stretch goals met (scores
in the skill file). The asymmetry rule stands: wrongly merging two problems ends the loop
early and ships a bug; wrongly splitting wastes money. When unsure, split. Re-finds get the
prior decision attached, never suppressed. Every future reconciler version must re-pass the
ground-truth scoring before being trusted.

## The severity second look (v1's "devil's advocate")

Still unbuilt — and cheaper to start than v1 assumed. Run 1 said reviewers err high, never
low, so a **deflation** check (arguing "why is this serious?" against over-escalation) looks
more useful than the planned escalation check. New since v1: the `sev_delta` field already
records every synthesis severity change. **First step is free: read that data** across the
recorded passes and only then decide whether any agent stage earns its cost.

## The round summary the owner actually reads

**Status: BUILT and contract-bound.** The `owner-summary` skill writes one page per
decision-bearing pass — verification passes write no files at all. The
[doc contract](../rules/doc-contracts.md) fixes its shape: four sections, 60-line cap, a
"Needs your decision" list with a suggested action per item, a "Reasons to doubt" section
computed from the pass's own data, and an evidence link on every claim. The round-end gate
(lint + Sonnet judge) blocks hand-back until the page conforms. Owner decision load so far:
12 decisions across 7 summaries (system review roll-up).

## The backlog and what remains of the groomer

v1 planned a groomer because minors accumulated with no processor. **Partially superseded:**
the [backlog](../state/backlog.md) is now a real cross-target queue — one line per defect,
keyed by `PPW-<n>`, twelve fixed areas — with two drain moments wired into process: every
new bolt **must sweep the rows in its area** (the bolt process standard, mandatory at
open), and the
pre-deployment regression phase **requires the file empty**. Today it holds 141 rows:
2 🔴 · 7 🟠 · 93 🟡 · 39 ⚪. The two 🔴 (PPW-460, PPW-461, both `edge`) wait for an
edge-area bolt or pre-deployment — the queue has no aging or escalation signal.

What survives of the groomer spec, rewritten against the queue:

- **Cross-target dedupe** — different targets deposited near-identical rows (the reconciler
  already judges same-problem); a groom pass merges them before anyone pays to fix twice.
- **Batch-fix sweeps** — group rows by area, propose one fix round per area worth doing now,
  each fix through normal fix-verification, the rest closed by owner ruling with a reason.
- Trigger: on request, or when a drain moment (bolt open, pre-deploy) meets a big queue.

## Wrapping up a finished feature — superseded, done differently

v1 planned a compression step (two plain-language close summaries, a compress-review skill,
a plain-language skill). **Closed: the contracts do this at write time instead of close
time.** What replaced each piece: templates + size caps keep every artifact small from the
start; the close sequence is automated in the loop-driver (ledger `closed:` line → surviving
backlog rows copied to the queue → `archived:` on the index row → move under `archive/`,
contents unchanged); the [index](../state/index.md) target row — capped at 5 lines, full
language rules — is the cold-reader close summary; and the language rules themselves are
enforced by the Sonnet judge every round, not by a one-time rewrite. All five historical
targets were retrofitted to this shape and archived (owner order, 2026-08-10/11).

## Measuring — so the next five features aren't anecdotes

**Status: BUILT through v3, enforced, partly unread.** Every pass appends a metrics line;
since v3 (2026-08-03) every fix round appends one too, with runtime split into active,
blocked-on-owner, and idle time computed from the worklog. The
[records auditor](../lib/records-auditor.mjs) validates every append: schema, tally
cross-checks, review↔metrics pairing, cited commits reachable from pushed refs, the
citation-leak count. Evidence preservation: three tags on origin cover the cited commits of
015/042/043 (SF4 fix).

Recorded so far: ~37M pass tokens across the five targets and the meta-review (32.85M in the
2026-07-29 roll-up + 4.25M for 044-045); 125 serious findings named through 2026-07-29; 234
fixes verified with 6 reopened (2.6%); 467 ids minted (the counter stands at 468). The
number v1 could not see: across 044-045's five fix rounds, **6.0 hours were active work,
0.5 hours waiting on the owner, 114.8 hours idle — 95% of wall-clock was nobody at the
wheel.** The speed problem is scheduling, not owner latency and not agent speed.

Recorded but **unread** — fields with no reader script yet: per-finding lens attribution
(which lenses earn their keep), `sev_delta` (severity calibration), `fix_generated` (did the
2026-07-22 fixer rules cut fix-caused defects), the runtime splits. The schema's three
headline questions are still uncomputed.

## Tools — status and build order

Built and operating:

| Tool | Where |
|---|---|
| Discovery fan-out script (lenses → dedup + `hinted` → trace-first skeptics tiered by severity and convergence, delta budget guard, decided-re-raise skip) | [lib/discovery-review.wf.js](../lib/discovery-review.wf.js) |
| Mechanical router (state → next pass → cost → gates, exit codes for owner gates) | [lib/route-next-pass.mjs](../lib/route-next-pass.mjs) |
| Loop driver (audit → route → announce → gate → execute → record; session-model guard; archive-on-close; runs the doc gate) | `.claude/skills/loop-driver/SKILL.md` |
| Fixer contract, descheduled 2026-08-03 (triage → one batched owner gate → background approach-checks, test runs, micro-reviews while fixing) | `/fix-review` skill |
| Reconciler (mints `PPW-<n>` from the id counter; scored 2026-07-27: 0 over-merges) | `.claude/skills/reconcile-findings/SKILL.md` |
| Owner summary (contract-bound page per decision pass) | `.claude/skills/owner-summary/SKILL.md` |
| Metrics **v3** (per-finding lens/severity/verdict/fix-lineage; fix-round lines; runtime from the worklog) | [metrics-schema.md](../rules/metrics-schema.md) + [lib/render-records.mjs](../lib/render-records.mjs) |
| Records auditor (schema, tallies, pairing, commit reachability, citation-leak count with target 0) | [lib/records-auditor.mjs](../lib/records-auditor.mjs) |
| Doc gate (deterministic lint, target + `state` modes, + Sonnet judge; pre-commit backstop; 36-assertion fixture suite) | [lib/doc-gate.mjs](../lib/doc-gate.mjs) + [lib/tests/run-tests.mjs](../lib/tests/run-tests.mjs) |
| Path constants + link keeper (every move: `git mv`, constant, then the link check) | [lib/paths.mjs](../lib/paths.mjs) + [lib/cli/docs-sync.mjs](../lib/cli/docs-sync.mjs) |
| Ledgers + worklogs (template-bound; append-only enforced against git HEAD by the gate) | per-target files, shapes in [doc-contracts.md](../rules/doc-contracts.md) |

To build — re-audited item by item against today's system:

| # | Item | Form | When it runs | Status and why this position |
|---|------|------|--------------|------------------------------|
| 1 | **Seeded-bug run 2** | workflow script | before trusting any stop rule | Still the top item; owner-deferred 2026-07-29, gate kept visible (SF14). Design unchanged: different implanter model or human, harder seeds aimed at misses, per-severity recall, shared-miss analysis. ~2M+ tokens |
| 2 | **Verification-pass script** | script | after every fix batch | Built 2026-08-20 as lib/verify-fixes.mjs (revert-and-rerun mechanized; judgment items stay one hand-run diff each) |
| 3 | **Blinding auditor, workspace half** | plain script, pre-pass | before every discovery | The citation half lives in the records auditor (0 enforced). Remaining: prove the discovery workspace reaches no review records, no git history, no finding-id strings; scan any codePack |
| 4 | **Severity second look** | read the data first; agent stage only if warranted | after skeptics | Run-1 data said deflation checks matter more than escalation checks; `sev_delta` now records the evidence for free — read it before building anything |
| 5 | **Backlog groom** | skill, on request | at drain moments | Partially superseded by the queue + its two drains. Survives: cross-target dedupe and per-area batch-fix sweeps over the 141 rows (spec above) |
| 6 | ~~compress-review + plain-language skills~~ | — | — | Closed, done differently: templates + caps + automated close sequence + compressed index + judge-enforced language (section above) |

Approved and waiting to run (not a build): the prevention-sweep backfill — ~290 ledger rows
classified into the spec's class sidecar, ~150–250k tokens, then the ranking's first real run
([spec](../../docs/superpowers/specs/2026-08-10-prevention-sweep-design.md)). The first cut of
the ranking script was deleted unfed on 2026-08-31; when the backfill is scheduled, rebuild it
against `records/` from the spec and the deleted version in git history.

System backlog (from live runs): auto-append findings as inline PR comments once `gh` is
in play · a reusable cross-repo lens pack is now a direction option (D below), not a side
item.

## Honest concerns

1. **Everything is measured on one codebase and one model family**, and the undeviated
   close has completed zero times. Five closed targets is better than v1's two and a half —
   but every "works" number except the gate's fixture suite is self-measured.
2. **The records machinery guards record quality, not review truth.** The gate, judge, and
   auditor catch drift, broken shapes, and missing evidence links; a wrong review that
   follows every template passes all of them. The only designed truth check is seeded
   recall, and it has not run in its meaningful form.
3. **The owner is both the rate limiter and the trust anchor.** 95% of fix-round wall-clock
   is idle waiting for the next session; the designed gates cost little clock time
   (0.5 hours across five rounds) but each one needs a human present. Removing the owner
   without recall proof swaps a bottleneck for blind trust — the order of work matters.
4. **Building the remaining tools is small work.** The expensive item is the experiment,
   not code: items 2–5 above are each roughly an afternoon; run 2 is ~2M+ tokens.

## Where this stands — completion audit (2026-08-11)

Scored against what the end state needs. Built = operating and exercised; partial = works
with a manual or unproven half; missing = does not exist.

| Capability | Score | Evidence |
|---|---|---|
| Find defects | built | 5 targets, 125 serious named through 2026-07-29; manifest breadth proven by run 1 (2 of 10 seeds caught only by db-parity + completeness lenses) |
| Judge severity | partial | single synthesis judgment; measured once (run 1: erred high ~4 of 10, never low); `sev_delta` recorded, unread |
| Fix | built | fixer contract descheduled; 234 fixes verified, 6 reopened (2.6%) |
| Verify fixes | built | verify-fixes.mjs + one subagent per pass |
| Record | built | contracts + templates + doc gate (lint + judge) + auditor + pre-commit backstop + 36-assertion fixture suite |
| Self-route | built | mechanical router + loop driver; hand-routing only when it abstains |
| Self-check its own records | built | auditor on every append; gate on every round; but the pair has never processed a genuinely new target end to end — all five targets were retrofitted |
| Prove its own recall | missing | run 1 could not test it (0 misses); run 2 deferred; interim track record: 2 certifications, 0 escapes, low power |
| Run without owner babysitting | built (delegated gates) | unattended mode 2026-08-20: every gate delegated under the standing approval (certification and close included), parked decisions, subagent passes, no limits by owner decision; re-invocation still manual (same phrase resumes) |

**Honest overall: ~60–65%** — the low edge of the owner's 60–80% guess. Reasoning: the
mechanics (find, fix, record, route, self-check) are ~85–90% done; the trust work (prove
recall, verify blinding, calibrate severity) is ~25%; unattended operation is ~40%. The
end state needs all three, and the second and third are the point of the word
"self-driving".

### What breaks first with zero owner presence — ranked by autonomy bought

1. **Nothing re-invokes the loop.** The driver is session-bound; with no owner the loop
   simply stops after every pass. A scheduler plus a written delegated-decision policy for
   the routine gates (which decisions the loop may take alone, which batch for the owner)
   buys the most autonomy of any single change — it turns 5 idle days per target into
   hours. Partly closed 2026-08-20: one unattended run now chains passes to a hard stop;
   between runs, re-invocation is still a human (or scheduled) "run the loop unattended" —
   auto-scheduling stays an owner opt-in.
2. **Verification is manual.** The most-run stage needs a driver each time; the script
   (item 2) removes the largest recurring labor. Closed 2026-08-20 by verify-fixes.mjs.
3. **Recall is unproven.** The loop would run — and its "certified" would mean exactly what
   SF14 says: closure under an untested rule. Run 2 converts autonomy from unsupervised to
   trustworthy; it is the only item here that adds truth rather than motion.
4. **Blinding decays unverified.** Cheap to fix (item 3); silent if not.
5. **The queue holds serious rows with no clock.** 2 🔴 wait on drain moments that may be
   months out; an unattended loop needs an escalation rule for them.

### Weak spots in the current machinery (listed, not fixed)

- Judge rounds re-read a round's full file set every time; no scoped re-judge. The judge
  was promoted Haiku → Sonnet after retrofit runs showed recall misses — convergence cost
  on bulk jobs is real and unmeasured.
- Single-model family everywhere: lenses and synthesis on the session model, skeptic tiers
  and the judge on Sonnet/Haiku — assumption 1's shared-blind-spot risk applies to the gate
  itself.
- The doc gate + auditor pair has never run a fresh target from v1 to close; first live use
  on a new target will find template friction the retrofits could not.
- Metrics recorded but unread (lens yield, `sev_delta`, `fix_generated`, runtime splits) —
  the schema's promised questions are still uncomputed.
- The router's cost table is hardcoded from 2026-07-30 roll-ups; 044-045's verification
  actuals (175k–772k) already exceed its 60–250k estimate.
- Fix-round `cost.tokens` is null on every recorded round — the second-biggest spend has a
  runtime record but no token record.
- The [test-quality audit](../system/test-quality-v1.md) (309 findings, 4 confirmed 🔴) is
  committed since 2026-08-12 but its findings are still unprocessed by any loop; the system
  target now keeps a lightweight ledger + metrics line per meta-pass (v2 fix round).
- Retrofit residue: retrofitted summaries still say "D#" in prose while their ids were
  re-keyed to `PPW-<n>`.

## Directions — the decision menu

Four argued options. Costs are tokens unless marked; build effort is sessions of work.

**A — Prove recall first (trust).** Build: seeded run 2 (~2–2.5M, different implanter,
harder seeds, per-severity recall, shared-miss analysis) + the blinding auditor's workspace
half (small script) + the free `sev_delta` readout. Proves: what fraction of planted
serious bugs the loop actually finds, and whether two passes miss the same things — the
number every certification and every future autonomy claim silently rests on. Unlocks:
honest closure claims; pays the SF14 debt; grounds the stop rule. Main risk: it may prove
the uncomfortable — recall below trust — and trigger more spend; and with no second vendor
wired, "different implanter model" means a different Claude tier or a human, which only
partly breaks the shared blind spot.

**B — Make it cheap and unattended (throughput).** Build: the verification script + a
scheduler that re-invokes the loop driver + a written delegated-decision policy with one
batched owner sitting per target. Effort ~2–3 sessions, low run tokens; each later round
saves days of idle wall-clock (95% of 044-045's fix-round clock) and up to ~770k per
verification pass. Proves: the loop can run a bolt's review with one owner touch. Main
risk: it scales the output of a system whose recall is unproven, and the owner's reading
load grows with throughput — volume was already the recorded pain.

**C — Wire stage 6 end to end (integration).** Build: B's pieces + the prevention-sweep
backfill (~150–250k, approved) + one pilot bolt whose review runs hands-off until the
certification gate (~3–4M for the pilot's passes). Proves: the loop as one organ of the
bolt process, on a genuinely new target — the first live test of the gate machinery and
of the KPI (severity-weighted v1 findings vs the 7 · 11 · 7 · 19 · 23 serious baselines).
Main risk: it inherits B's risk plus a pilot certified under the untested stop rule; C
without A bakes SF14 in deeper.

**D — Package the kit (generalization).** Extract the repo-agnostic parts — contracts,
templates, gate, router, queue, auditor, path constants — into a kit another repo or agent
can adopt; the loop as product, per the agentic-dev-team goal. Effort ~2–4 sessions, low
tokens. Proves: the system transfers; the product thesis gets its first artifact. Main
risk: extracting from a sample of one repo, while the design still churns (the contracts
shape landed 2026-08-10; the twelve-area list, the folder layout, and the judge model all
changed on 2026-08-11) — and the kit ships with "recall unproven" in its README.

**Recommendation: A, then B.** A is the only option that adds truth; every other option
inherits its result. B's pieces are afternoons and can start the moment A's run is queued.
C becomes the natural third step (its pilot then runs under a measured stop rule); D waits
until one external adoption target actually exists.

**The one question the owner must answer:** approve seeded-bug run 2 now, at ~2–2.5M
tokens — or explicitly choose to keep building on unproven recall? Everything else in this
file follows from that answer.

## Script backlog (2026-08-22)

Seven small scripts the loop keeps hand-doing. Each is an afternoon at most, and each one
removes a step a human currently performs from memory — the same trade the router, the
renderer and the stamper already made.

- **Discovery prep** — collects the pass's diff set, checks the target branch's HEAD is the
  commit the pass will name, and suggests lenses from the touched areas. Removes the
  hand-assembled scoping block at the top of every discovery, and the mis-stated `commit:`
  frontmatter that a stale HEAD produces.
- **Close-target sequence** — writes `closed:` into the ledger frontmatter, rolls every
  `backlog` row and every 🟠 standing down into [backlog.md](../state/backlog.md), stamps
  `archived:` on the index row, and `git mv`s the folder into `archive/`. Removes the
  four-step close checklist whose order is the part that gets dropped.
- **Run-end report printer** — reads a run's `gate-parked` events and prints the parked
  items (kind, default taken, what needs a ruling) as the report's skeleton. Removes the
  hand-reconstruction of what an unattended run decided alone, which is the one part of the
  report the owner actually acts on.
- **Commit-subject lint** — a `.githooks/pre-commit` check for the one-sentence,
  subject-only, no-trailer rule, with the finding/round id where one is expected. Removes
  the after-the-fact discovery that a commit body or a `Co-Authored-By` trailer landed.
- **Judge input packager** — lists the round's changed `reviews/` files from git and packs
  them with doc-contracts.md into the judge's prompt. Removes the judge's re-read of a
  target's whole file set, which is why judge rounds cost what they cost.
- **Blinding auditor** — scans a lens's inputs before launch for `reviews/` content, git
  history and `PPW-` id strings, and refuses the launch on a hit. Removes the "blinded
  best-effort, enforced by prompts" caveat in the README's hard rules — the one claim the
  system makes and cannot currently check.
- **Reconcile pre-matcher** — offers fuzzy same-problem candidates (file, symbol, title
  overlap) for each new finding against the ledger, for the reconciler to accept or reject.
  Removes the full-ledger read per finding, without moving the same/new judgment off the
  reconciler.

None of these is queued. Each stays unbuilt until the lint-miner habit surfaces it as a
measured cost — the same way the prevention sweep is meant to rank defect classes — or the owner
asks for it outright.
