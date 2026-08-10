---
type: resolution
target: 044-045-observability
version: 5
answers: review-v5.md
status: resolved
fixed_commit: a4eb7e5
closed: 2026-08-07
findings:
  F1: { status: fixed, commit: a4eb7e5, note: "two commits: 796a330 (the pin) and a4eb7e5 (the micro-review's repair). test-only. The D103 rule could not see these: it counts added terms per side and skips any side with no `+`, so a lone `sum(metric{result=\"ok\"})` numerator is one term and never examined. New test pins the four hand-named success selectors by name — payment_webhook ok and duplicate, awb_creation ok, invoice_anaf accepted — requiring `or vector(0)` within 24 characters after each occurrence, in every query of both copies, plus a floor of 2 occurrences each (doc + dashboard twin) so deleting or moving a query reddens instead of quietly passing. Deliberately NOT written as the broader class rule 'every literal-valued sum() needs a guard': that would red SLO 1's non-5xx numerator and the error-rate panel, which are neither in scope nor obviously wrong to leave unguarded. Red proof: the exact mutation review-v5 measured green (delete `or vector(0)` from the SLO 4 numerator in both copies) now reddens 1 test, no collateral; 203 Integration green restored. THE MICRO-REVIEW THEN BROKE MY FIRST VERSION and I confirmed it by measurement: the occurrence floor counted 2 across both files rather than one per file, so duplicating the doc's numerator while deleting the dashboard panel's query left it GREEN — measured, then measured RED against the same mutation after the repair. Counting is now per source, and the assertion is a per-source `missing` list, which also fails when both collectors return nothing (probed: red on empty collectors)" }
  F2: { status: fixed, commit: d8a63a4, note: "doc-only, no test (an acceptance-criteria line has no build check by design). The criterion now names both exclusions — `skipped` because no label was needed, `retry_later` because the counter records one row per attempt — and states that `orphaned` stays in the denominator as a failure. The old line's rationale had been attached to the wrong value: it gave the retry-loop reason for excluding `skipped`. Class sweep: this wording exists in exactly one place outside reviews/ (grepped `except \\`skipped\\``)" }
  F3: { status: fixed, commit: 3c0a13d, note: "doc-only, no test. Two sites, swept token-wide rather than file-wide: the outcome union's `<list>` entry (`AwbCreationOutcome.cs:9`) and the operator log table (`DEPLOYMENT.md:771`), both of which described `Skipped` as 'order no longer eligible (cancelled, AWB already exists)' — with `cancelled` being precisely the case that must now set `Orphaned: true`. DELIBERATE ADDITION recorded: the operator table had NO row for `sameday.awb.orphaned`, an Error-level log, while its `skipped` row called that case healthy — so the row was added with the manual-void action. Also swept and deliberately LEFT: `memory-bank/bolts/037-awb-and-tracking-jobs/ddd-01-domain-model.md:138`, `:324` carry the same old wording, but bolt docs are point-in-time design records, not descriptive standards. The union doc had to be shortened once: the comment gate rejected the first version as bloat, and the shorter one was committed with COMMENTS_OK=1 as an allowed type-contract doc" }
  D110: { status: fixed, commit: 9cfbf75, note: "OWNER-DIRECTED, outside review-v5's finding set: a v4 backlog row (v4's F9) the owner asked to clear in this round, flagged in three consecutive summaries. Both copies corrected — the slos.md status block and the availability panel's `description` — from 'roughly 5,760 /health and /metrics requests' and 'cannot read below about 99.7%' to ~8,640/day (5,760 `/metrics` at the 15 s `scrape_interval` in DEPLOYMENT.md:1048 + 2,880 `/health` at the Dockerfile:43 30 s HEALTHCHECK) and a ~94.5% floor (8,640 / 9,140 against the ~500 customer req/day at DEPLOYMENT.md:950). Each figure now names its source in the text so the next drift is checkable. The panel description also loses its `Tracked as D46` citation — an id no operator can resolve, which is half of backlog row D117 — and gains the line an operator actually needs: 95% here does not mean the monitoring is broken. No test: panel descriptions and prose are unpinned by design (D117's other half), and pinning them was not in scope" }
---

# Resolution v5 — 044-045-observability

Fixer's answer to [review-v5.md](review-v5.md) (immutable). The review named three findings, all 🟡
(D121, D122, D123). **Normally all three would go to the ledger backlog per the README router** —
they are fixed here because the owner called the v4 round patch-grade and asked the loop to "end
naturally", which means clearing the small stuff rather than parking it.

**Plus one owner-directed extra:** **D110**, a v4 backlog row (the wrong availability-dilution
figures on the operator-facing panel), named by the owner because it had been flagged in three
consecutive summaries. It is recorded above as its own entry with a `D#` key rather than an `F#`,
because it is not a finding of review-v5.

**Nothing here is `verified`.** Only `review-v6.md` — a re-review by someone who did not fix — can
set that status.

## Fix round scope

| Cluster | Findings | Owner file(s) | Approach-check |
|---|---|---|---|
| A — guard pinning | F1 (D121) | `Tests/Integration/DashboardMetricNamesTests.cs` | not needed (test-only) |
| B — stale records | F2 (D122), F3 (D123), D110 | story AC doc, `AwbCreationOutcome.cs` (doc comment), `docs/DEPLOYMENT.md`, `memory-bank/operations/slos.md`, `ops/dashboards/fototipar-overview.json` | not needed (doc-only) |

**No cluster is trigger-list-shaped and no owner gate was needed.** Nothing here adds a mechanism,
changes a key scheme, a concurrency model, a resource budget or retry semantics: cluster A adds
assertions over queries that already ship, cluster B edits prose, one doc comment and one panel
description. Recorded explicitly because skipping approach-checks is normally the expensive mistake.
The one judgment call that could have needed the owner — how far to broaden the guard rule — was
resolved by scoping *down* rather than by guessing at their intent (see below).

## Decisions

### Why F1 pins four selectors by name instead of stating the general rule

The obvious class rule — "every side of a ratio whose selector names a value by hand must carry
`or vector(0)`" — reds two panels this round has no mandate to touch: SLO 1's
`{http_response_status_code!~"5.."}` numerator and the error-rate panel's `=~"5.."` numerator. Both
would read "No Data" instead of `0` in an edge case (every request 5xx; no request 5xx), and both
belong to the parked D46 conversation about what SLO 1 measures. So the test pins the four numerators
this repo has *decided* must never read "No Data" and leaves the general question open. The cost of
that choice, recorded honestly: **the list is hand-maintained**, so a fifth guarded ratio added later
is not covered until someone adds it — which is why the test also fails when a listed selector stops
appearing twice, rather than only when a guard goes missing.

### D110's arithmetic, and what it does not fix

The corrected numbers come from three sources, each now named in the prose: `scrape_interval: 15s`
(`docs/DEPLOYMENT.md:1048`) → 5,760 `/metrics` a day; `HEALTHCHECK --interval=30s` (`Dockerfile:43`)
→ 2,880 `/health` a day; "~500 req/day" customer traffic (`docs/DEPLOYMENT.md:950`). Floor =
8,640 ÷ 9,140 ≈ 94.5%.

This corrects the **claim**, not the **dilution**: SLO 1's denominator still counts self-monitoring
traffic, which is D46 — parked by the owner because the fix needs .NET 9
(`IHttpMetricsTagsFeature.MetricsDisabled`). An operator reading the panel now gets the true floor
and is told explicitly that 95% is not a monitoring fault.

### The micro-review found a hole in my own test, and the A/B measurement is the evidence

Three items over the round's diff; one changed the code.

1. **The occurrence floor was per *pair of files*, not per file.** My first version required each
   selector to appear at least twice across `slos.md` + the dashboard together — so duplicating the
   doc's numerator while deleting the dashboard panel's query kept the count at 2 and the test
   green, with an operator-facing panel gone. **Measured both ways against the same mutation:** the
   committed rule → 6 of 6 green; the per-source rule → 1 red naming
   `awb_creation_total{result="ok"} in the dashboard`. Repaired in `a4eb7e5`.
2. **A vacuity claim I checked and refuted.** It reported the count assertion as empty-vacuous if
   the collectors return nothing. They cannot: the counters are seeded from
   `GuardedSuccessSelectors`, so an empty collector run leaves every count at zero and reddens.
   Probed anyway on the final version — both collectors forced empty → **1 red**, not green.
3. **Window width, left as chosen (⚪, no change).** It asked whether the 24-character look-ahead
   should be 48. 24 is deliberate: every shipped form needs at most 20 characters
   (`[30d])) or vector(0)`), and a wider window starts catching a *neighbouring* term's guard, which
   turns a real defect green. A guard pushed past 24 characters false-reds, which is the safe
   direction.

### Genuinely new, outside the finding set — NOT fixed

- **`docs/DEPLOYMENT.md:949` still reasons from the availability target as if the denominator were
  customer traffic** ("≤ 1/200 requests is a 5xx → … a handful of error events per day"). That is
  backlog row **D116**, and it survives this round's token sweep because it carries neither of the
  wrong figures — it makes a different wrong inference from the same confusion. Left for its own row
  rather than swept in, since the owner scoped this round to D110.

### Boundaries — for the re-reviewer

- **F2, F3 and D110 are prose with no build check.** Nothing pins an acceptance-criteria line, a doc
  comment or a panel description, so all four fixes are verifiable only by reading them against the
  code — which is what a verification pass does for judgment items.
- **F1's red proof is the strongest evidence in this round** and it is the mutation review-v5
  measured green: deleting the SLO 4 numerator guard in both copies.
- **The union doc comment was shortened under the comment gate** and committed with `COMMENTS_OK=1`.
  If a reviewer judges that a doc comment on an abstract record is not the allowed case (b), the fix
  is to delete the entry rather than restore the old, now-wrong wording.
