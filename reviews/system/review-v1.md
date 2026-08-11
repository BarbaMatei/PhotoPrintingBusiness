---
type: system-review
target: review-system
version: 1
pass-type: system-review
commit: 1a9c3ad
date: 2026-07-29
verdict: approve-with-followups
findings: 16 raised · 14 stand (0 🔴 · 5 🟠 · 5 🟡 · 4 ⚪) · 2 refuted
checked-by: 2 independent agents (records verifier · steelman), verdicts folded in
---

# System review v1 — the review system itself

The target is the multi-lens review loop: [README.md](../README.md), the runbooks, the
discovery script, the three skills, and every record it produced (035, 042, 043, 015).
Method: full read of the system docs and records, hand-computed cross-target roll-ups,
then two independent checker agents — one re-verified every factual claim and number, one
argued the system's defense and could kill findings. Both reports are folded in below;
severities are post-defense. Like every `review-v*.md`, this file is immutable; respond in
a resolution file.

Baseline scorecard (graded before this review; the fixed comparison point for future
re-grades — re-grade against these same dimensions, not revised ones):

| # | Dimension | Grade /10 |
|---|---|---|
| 1 | Bug-finding power | 6 |
| 2 | False-alarm control | 8 |
| 3 | Severity judgment | 5 |
| 4 | Fix verification | 8 |
| 5 | Reviewer independence | 3 |
| 6 | Cost efficiency | 5 |
| 7 | Autonomy | 3 |
| 8 | Self-measurement | 7 |
| 9 | Rule discipline | 6 |
| 10 | Proven outcomes | 1 (no data possible yet) |

## Findings

SF# is this target's D# (first pass, 1:1). "Checker" column: A = records verifier,
B = steelman defense. Severity is applied to the review system as the product; 🔴 would
mean "breaks the system's core promise (trustworthy autonomous closure)".

| SF# | Sev | Title | Checker outcome |
|----|-----|-------|-----------------|
| SF1 | 🟠 | The undeviated certification path has never completed | A confirmed; B downgraded 🔴→🟠 |
| SF2 | 🟠 | No quiet full pass ever observed; README oversells what "certified" means | A confirmed; B downgraded 🔴→🟠 |
| SF14 | 🟠 | Both certifications issued under a stop rule whose own gating experiment never ran | added by B |
| SF16 | 🟠 | The "certified feature later broke" counter has no collection mechanism | added by B |
| SF4 | 🟠 | Evidence chain is single-machine: review branches never pushed, no tags | A corrected (035 on main; 3 branches local-only); B's downgrade pre-dated the correction |
| SF3 | 🟡 | Discovery script HINTS feed every lens a stale architecture ("cloud is a planned follow-up") | A confirmed; B downgraded 🟠→🟡 |
| SF5 | 🟡 | Metrics drift off-schema; per-finding lens attribution dropped; no validator | A confirmed; B downgraded (2 of 3 headline questions still answerable) |
| SF6 | 🟡 | Fix-generativity (the documented dominant cost driver) has no metric field | A confirmed; B downgraded (lineage exists in ledgers, only aggregation missing) |
| SF9 | 🟡 | Blinding leak growing: 371 finding-ID citations in 118 source files; auditor unbuilt | A confirmed exactly; B's defense lost — comments are not in the accepted-leak set |
| SF15 | 🟡 | fixer==verifier waived at the final 015 round — the one hard independence rule bent at peak closure pressure | added by B |
| SF7 | ⚪ | Per-feature cost understated (fix rounds unmetered) — but per-pass scope is by design | B downgraded 🟡→⚪ |
| SF8 | ⚪ | Stale "3 of 5 re-raises overturned" quoted as current in 3 places; post-mechanism record is 54–56 events, 0 clean overturns | A corrected the counts; B showed the anchoring inference is confounded |
| SF12 | ⚪ | Hand-transcribed numbers drift between the six files a pass touches (D96 et al.) | stands as filed |
| SF13 | ⚪ | No session-model check before launch; resume documented but manual | A confirmed facts; B downgraded (post-rule incidents: zero) |
| SF10 | — | Severity is single-judge at the stop rule's pivot | **refuted by B**: priced-in (design-doc assumption 2, measured safe direction, tool #4 planned). Residue → severity-delta logging, folded into SF5's fix |
| SF11 | — | "The human bottleneck is launching, not deciding" | **refuted by B**: the launching labor is the main agent's; the human's per-pass acts are the designed gates. Residue → the loop-driver, already the design doc's build plan |

### SF1 🟠 — the undeviated certification path has never completed

Both certification pairs ever run ended `request-changes` (015-v3, 043-v7). Both eventual
closures used escape hatches: 043-v9 via the written single-pass deviation (preconditions
genuinely met — [rationale.md](../notes/rationale.md) certification section); 015 via owner
sign-off with **no post-fix blinded pass at all**, on a full-loop-tier target where the
sign-off clause was written for lower tiers ([index.md](../state/index.md) targets table;
[015 ledger closure section](../archive/015-sameday-shipping/ledger.md)). Every deviation is
honestly recorded — that is the system's real strength — but the standard path
(pair → quiet → certified) has completed **zero** times, so its cost/benefit is still a
hypothesis. Fix: a calibration ruling (per the rule-budget rule, replace — don't stack):
either bless "single pass after a recent pair, small verified fix round" as *the* standard
full-loop close, or commit the next full-loop target to the undeviated path. Track closure
type (standard / deviation / sign-off) as an index column.

### SF2 🟠 — "certified" ≠ what the front page promises

Last full pass per target still found new serious findings: 042-v8 five Mediums, 043-v9
five, 015-v5 seventeen. Chapman at 043-v7 estimated ~7 serious still findable; v9 then
found 5 more. Practice (documented in pass notes as "honest scope") is: certified =
0 High + every Medium triaged to an owner-visible state. The README's opening line —
sampling "until the serious-defect population is closed" — promises more than any target
has ever exhibited. Fix: one sentence on the README front page + "mediums open at
closure: N" in every certification index row. Do **not** chase medium-silence with more
passes — that is the documented 2.96M-tokens-for-0-Highs failure the 2026-07-22
recalibration removed.

### SF14 🟠 — certified under an untested stop rule *(checker-added)*

[self-driving-loop-design.md](../notes/self-driving-loop-design.md) names seeded-bug run 2 "the
single most important experiment", build-order **#1**, gated "**before trusting any stop
rule**" — queued since 2026-07-04. 043 and 015 were both certified/closed under that
stop rule anyway, and run 1 explicitly could not test the load-bearing assumption
(10/10 recall ⇒ miss-correlation undefined). The design doc's own precondition is
violated by practice. Fix: owner ruling — run seeded run 2 before the next
certification-grade close, or amend the precondition to say what is actually being
trusted in the meantime.

### SF16 🟠 — the trust metric has no collector *(checker-added)*

The design doc says autonomy trust comes from the measured track record, including "the
count of certified-clean features that later turned out to have a serious bug." No
mechanism exists to increment that counter: nothing watches certified targets after
closure, and no artifact would attribute a later 043 storage break back to the
certification record. Fix (cheap, convention-only): when any future pass finds a serious
defect in a certified target's files, the reconciler marks it `post-cert-escape` in the
ledger and a one-line entry goes to a global track-record file. Zero cost until it fires.

### SF4 🟠 — the evidence chain lives on one machine

Corrected by checker A: 035's `691e23d` **is** reachable from main; the other 9 sampled
commits cited by ledgers/index/metrics are reachable **only** from
`feat/bolt-036-sameday-api-client`, `feat/bolt-042-thumbnail-cache`,
`feat/bolt-043-cloud-storage-provider` — and **none of those three branches exists on
origin**. The 015 v5/v6 evidence tail (including closure commit `5734021`) was never
pushed anywhere. `git tag -l` is empty. One local branch deletion — or one disk — loses
the reproducibility of every revert-and-rerun proof and every evidence link for a
certified feature. Fix: push the three branches (or, better, push lightweight tags per
cited commit, e.g. `review/015-v5-5fc330b`) today; the records auditor then checks every
cited SHA resolves from a pushed ref. One push demotes this finding to 🟡.

### SF3 🟡 — stale HINTS in the discovery script

[discovery-review.wf.js](../lib/discovery-review.wf.js) lines 108–110 tell every lens
"Storage is behind IStorageService (local today; a cloud provider is a planned
follow-up)"; the dedup prompt's hinted-topic list repeats it. Bolt 043 shipped and
certified two-tier cloud storage on 2026-07-22. Mitigations exist (CLAUDE.md outranks the
hint; lenses read real code; storage agreement already gets no convergence discount), so
🟡 not 🟠 — but it violates the "standards are descriptive" hard rule and quietly skews
every future pass on every target. Fix: update HINTS + the dedup topic list in the same
edit; add "HINTS still true?" to the pre-launch checklist.

### SF5 🟡 — metrics records drift, and one stated question is unanswerable

Confirmed drift: `cost.subagent_tokens` (042) vs schema's `cost.tokens`;
`deferred_reaffirmed` vs `deferrals_upheld` (same concept, two names); undocumented
`disputed_upheld` / `outcome` / `certified` / `subtype` / `base` / `code_tip` /
`delta_base` / `tests.frontend_*`; one `lenses` value that is prose; the 043 pass-7B
tally-vs-item-list contradiction recorded in overlap-pair-v7.md
but never corrected by a next-line note as the schema requires. Roll-ups are computable —
today's numbers below prove it — but only by hand-special-casing each target, and
"which lenses earn their keep" is **not** computable: per-finding lens attribution
(`agreeingLenses`) exists in the script output and is dropped at record time. Fix:
schema v2 — standardize names, add per-finding `{lens(es), severity, verdict,
fix_generated, severity_delta}` — plus a records-auditor script that validates every line.

### SF6 🟡 — fix-generativity has lineage but no number

The dominant documented cost driver (042: ~13 of v6's 24 new defects fix-caused; 043-v3
and v5: effectively all; 015-v3: 3 of 12) is recorded as prose lineage in ledgers
(`residual-of`, per the reconciler contract) but never counted, so whether the
2026-07-22 fixer rules actually reduced it is not answerable from the metrics. Fix: a
`fix_generated` count inside `new_findings`; the reconciler already computes the input.

### SF9 🟡 — blinding erodes measurably; nothing enforces it

D89 recorded 67 finding-ID citation occurrences in 27 files (2026-07-22). Today's broader
scan (finding-ID prefixes + "review NNN-vN" + ADR refs, src only, spec files excluded):
**371 occurrences / 118 files** — reproduced exactly by checker A; 20 of 20 sampled hits
were genuine violations of the CLAUDE.md comment rule (e.g. `PaymentsController.cs:132`
"QUAL-5 (review 035-v8)"). Commit messages and test names are the *accepted* leak; code
comments are not — they are flatly forbidden and growing anyway (015-v6 removed 9; the
stock is 371). A blinded lens reading these files is told what past reviews found. Fix:
the comment-hygiene sweep D89 already ordered, then the blinding auditor (design-doc
tool #3 — a plain script: pre-pass, verify no `reviews/` reachability and scan the
lens-visible file set for citation patterns).

### SF15 🟡 — the independence rule bent exactly at closure pressure *(checker-added)*

"You must not be the fixer" ([runbook-verification.md](../runbooks/runbook-verification.md) step 1)
became "Recorded deviation: fixer == verifier" at the 015-v6 fix round — the final round
before closure. Mitigants: test-only round, revert-proofs are reproducible measurements,
expiry recorded ("next calibration"). But the pattern — the one hard rule waived the one
time it was inconvenient — is how rulebooks die. Fix at the named expiry: either a formal
narrow exemption (test-only rounds with predicted-failure-set revert proofs) or a hard no.

### ⚪ tail

- **SF7** — per-feature cost roll-ups count pass tokens only (fix rounds, synthesis,
  main-agent verification unmetered; 015 verifications record `tokens: null`). Per-pass
  scope is by design and the big losses are noted where they happened; label roll-ups
  "pass costs only" or add a coarse per-round line.
- **SF8** — "3 of 5 re-raises overturned" is quoted as current in
  [discovery-review.wf.js:277](../lib/discovery-review.wf.js), reconcile-findings
  SKILL.md, and [README.md](../README.md) ledger bullet. The post-mechanism record is
  54–56 re-raise events with 0 clean overturns — but the populations differ (early:
  contested calls; since: deferred-tail re-finds where uphold is correct), so
  over-anchoring is *unproven, not disproven*. Fix the stale text; optionally have the
  synthesizer judge one pass's re-raises blind-first (≈free) to get a real signal.
- **SF12** — number drift between the six hand-edited files per pass (D96, the v7 tally
  note, "914 vs 916"). All self-caught so far; the records auditor makes it mechanical.
- **SF13** — "launch from Opus" (runbook) is enforced by nothing; resume-from-cache is
  documented but manual. Post-rule incidents: zero. Fold a model check + auto-resume into
  the loop-driver when it's built.

## Cross-target roll-ups (computed 2026-07-29, first time; verified by checker A)

Pass-recorded tokens only (SF7 caveat: fix rounds and synthesis excluded).

| Metric | 035 | 042 | 043 | 015 | Total |
|---|---|---|---|---|---|
| Recorded pass tokens | null | 10,917,563 | 11,827,420 | 10,105,556 (+~2.27M lost run) | ~32.85M |
| New serious (H+M) | 13 | 33 | 32 | 47 | 125 |
| New Highs | 1 | 3 | 2 | 8 | 14 |
| Tokens per new serious | — | ~331k | ~370k | ~215k | ~293k weighted |
| Fixes verified | 46 | 70 | 40 | 78 | 234 |
| Reopened | 1 | 0 | 0 | 5 (v3: 1 · v6: 4) | 6 (2.6%) |
| Candidate findings refuted | 17 | 7 | 5 | 3 | 32 |

Facts a future calibration will want:

- **Serious-per-full-pass curves:** 035: 7→3→2 (decayed) · 042: 11→11→5→5 (plateau) ·
  043: 7→12→5 (pass types differ) · 015: 19→11→17 (no decay). A zero-serious full pass
  has never been observed on any target (SF2).
- **Skeptic layer:** 70–78% of agents on scripted full passes; refutation yield ~3–4% per
  pass plus downgrades (042-v4: 7). Precision insurance, priced accordingly since the
  2026-07-22/27 cuts.
- **Verification:** ~11–18k tokens per verified fix where metered; 6 reopens in 234
  (2.6%), and the 015-v6 four were all "fix correct, no test can redden" out of a 41-fix
  mega-round — batch size, not fix quality, is the reopen driver.
- **Waste incidents:** ~2.27M (platform 500 wave) + ~1.2M (042-v4 void run) + ~1.3M
  (tiering-replay overrun) ≈ 4.8M ≈ 13% of all spend — resilience, not review quality,
  is the second-biggest cost lever after the skeptic cuts.
- **Owner decision load via summaries:** 12 decisions across 7 summaries (5/2/2/0/1/2/0).
- **Re-raises:** 54–56 events since the attach-prior-decision mechanism, 0 clean
  overturns (vs 3 of 5 before it) — see SF8 for why this is not yet evidence of anchoring.

## Recommendations, ranked (cost in effort — none needs a fan-out)

1. **Push the evidence today** (SF4): push the three local-only review branches, or
   lightweight tags per cited commit. One command; removes the single-machine risk.
2. **One calibration sitting** (SF1, SF2, SF14, SF15 + text nits SF3, SF8): five owner
   rulings + four one-line doc edits. Everything is a doc change; the rule-budget rule
   applies (replace, don't stack).
3. **Records auditor** (SF5, SF12, SF4's ongoing check): a plain script — validate every
   metrics line against schema v2, cross-check tallies vs item lists vs index rows,
   verify every cited SHA resolves from a pushed ref, run the citation scan (SF9's
   number becomes a tracked metric). This is design-doc tool #3's sibling; build them
   as one pre-pass gate.
4. **Schema v2** (SF5, SF6, SF10 residue): standardized names + per-finding
   `{lenses, severity, verdict, fix_generated, severity_delta}`. One edit to
   metrics-schema.md + the script's synthesis instructions.
5. **Post-cert escape convention** (SF16): one paragraph in the README + a
   `post-cert-escape` ledger marker. Zero cost until it fires.
6. **Loop-driver skill** (the self-driving increment; absorbs SF13): "continue the loop
   for X" reads the router, states pass + cost, launches, appends records, halts only at
   the owner gates. Build after 1–4 so it drives clean records.
7. **Seeded-bug run 2** (SF14's honest resolution): different implanter model, harder
   seeds; ~2M+ tokens — owner-gated, and per the design doc's own words it belongs
   *before* the next certification-grade close.

## Checker protocol (this review's own verification)

Two independent agents ran against the draft findings: a records verifier (re-derived
every number from the raw files; corrected SF4's 035 claim, SF8's counts, and the reopen
total 5→6) and a steelman (killed SF10 and SF11, downgraded seven, added SF14–SF16).
Checker cost: ~194k tokens combined. Verdicts are recorded per finding above; the killed
findings stay in this file per the system's own never-suppress rule.
