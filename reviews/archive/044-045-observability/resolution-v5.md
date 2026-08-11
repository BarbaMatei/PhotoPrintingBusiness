---
type: resolution
target: 044-045-observability
version: 5
answers: review-v5.md
status: resolved
fixed_commit: a4eb7e5
closed: 2026-08-07
---

# Resolution v5 — 044-045-observability

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-456 | fixed | `a4eb7e5` | Two commits: 796a330 the pin, a4eb7e5 the repair. Test-only: each of the four hand-named success selectors needs `or vector(0)` within 24 chars, counted per source. Red proof: the mutation review-v5 measured green now reddens 1 test. |
| PPW-457 | fixed | `d8a63a4` | Doc-only, no test (an acceptance-criteria line has no build check by design). The criterion now names both exclusions — `skipped` (no label needed), `retry_later` (per-attempt counter) — and states `orphaned` stays in the denominator. |
| PPW-458 | fixed | `3c0a13d` | Doc-only, no test. Two sites: the union doc comment (`AwbCreationOutcome.cs:9`) and the operator log table (`DEPLOYMENT.md:771`), which also gained the missing `sameday.awb.orphaned` row. bolt-037 ddd docs left as point-in-time records. |
| PPW-445 | fixed | `9cfbf75` | Owner-directed, outside review-v5's set. Both copies now say ~8,640/day and a ~94.5% floor, each figure naming its source; 'Tracked as PPW-381' dropped. Corrects the claim, not the dilution — PPW-381 stays parked. No test: prose unpinned. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — guard pinning | PPW-456 | `Tests/Integration/DashboardMetricNamesTests.cs` | not needed (test-only) |
| B — stale records | PPW-457, PPW-458, PPW-445 | story AC doc, `AwbCreationOutcome.cs`, `docs/DEPLOYMENT.md`, `memory-bank/operations/slos.md`, `ops/dashboards/fototipar-overview.json` | not needed (doc-only) |

## Decisions

### Fixed rather than backlogged (PPW-456, PPW-457, PPW-458)

All three findings are 🟡 and would normally route to the ledger backlog per the README router.
They are fixed here because the owner judged the v4 round patch-grade and asked the loop to end
naturally, which means clearing the small items rather than parking them. PPW-445 — a v4 backlog
row flagged in three consecutive summaries — was added by owner request; it keeps its D# key in
the frontmatter because it is not a review-v5 finding. No owner gate was needed: nothing adds a
mechanism or changes a key scheme, a concurrency model, a resource budget or retry semantics,
and the one judgment call — how far to broaden the guard rule — was resolved by scoping down
rather than guessing at the owner's intent.

### Four selectors pinned by name instead of the general rule

The obvious class rule — every side whose selector names a value by hand must carry
`or vector(0)` — reds two panels this round has no mandate to touch: SLO 1's
`{http_response_status_code!~"5.."}` numerator and the error-rate panel's `=~"5.."` numerator.
Both would read "No Data" in an edge case (every request 5xx; no request 5xx), and both belong
to the parked PPW-381 question of what SLO 1 measures. So the test pins the four numerators this
repo has decided must never read "No Data" — `payment_webhook` ok and duplicate, `awb_creation`
ok, `invoice_anaf` accepted — requiring `or vector(0)` within 24 characters after each
occurrence, in every query of both copies. PPW-438's class rule could not see these sides: it
counts added terms and skips any side with no `+`. The cost, recorded: the list is
hand-maintained, so a fifth guarded ratio added later ships uncovered until someone adds it —
which is why the test also fails when a listed selector stops appearing twice, not only when a
guard goes missing.

### The per-source counting repair, measured both ways

The micro-review found my first version's occurrence floor counted per pair of files, not per
file: duplicating the doc's numerator while deleting the dashboard panel's query kept the count
at 2 and the test green, with an operator-facing panel gone. Measured both ways against that
same mutation: the committed rule → 6 of 6 green; the per-source rule → 1 red naming
`awb_creation_total{result="ok"} in the dashboard`. Repaired in `a4eb7e5`; the assertion is now
a per-source `missing` list. A vacuity claim was checked and refuted: the counters are seeded
from `GuardedSuccessSelectors`, so both collectors forced empty gives 1 red, not green (probed
on the final version). The 24-character look-ahead stays as chosen: every shipped form needs at
most 20 characters (`[30d])) or vector(0)`), a wider window starts crediting a neighbouring
term's guard — which turns a real defect green — and a guard pushed past 24 characters
false-reds, the safe direction. Round red proof: the exact mutation review-v5 measured green
(delete `or vector(0)` from the SLO 4 numerator in both copies) reddens 1 test, no collateral;
203 Integration green restored.

### PPW-445's arithmetic, and what it does not fix

The corrected figures come from three sources, each now named in the prose: `scrape_interval: 15s`
(`docs/DEPLOYMENT.md:1048`) → 5,760 `/metrics` a day; `HEALTHCHECK --interval=30s`
(`Dockerfile:43`) → 2,880 `/health` a day; ~500 customer requests a day
(`docs/DEPLOYMENT.md:950`). Floor = 8,640 ÷ 9,140 ≈ 94.5%. Both copies corrected — the
`slos.md` status block and the availability panel's `description`. This corrects the claim, not
the dilution: SLO 1's denominator still counts self-monitoring traffic, which is PPW-381, parked
because the fix needs .NET 9 (`IHttpMetricsTagsFeature.MetricsDisabled`). The panel description
also loses its "Tracked as PPW-381" citation — an id no operator can resolve, half of backlog row
PPW-452 — and now tells the operator that 95% here does not mean the monitoring is broken.

### The operator log table and the comment gate

The sweep was token-wide rather than file-wide and found two live sites: the outcome union's
doc comment (`AwbCreationOutcome.cs:9`) and the operator log table (`DEPLOYMENT.md:771`), both
describing the cancelled-order case as a plain skip — precisely the case that must now set
`Orphaned: true`. Deliberate addition, recorded: the table had no row at all for
`sameday.awb.orphaned`, an Error-level log, while its `skipped` row called that case healthy —
the row was added with the manual-void action. Swept and deliberately left:
`memory-bank/bolts/037-awb-and-tracking-jobs/ddd-01-domain-model.md:138`, `:324` carry the old
wording, but bolt docs are point-in-time design records, not descriptive standards. The union
doc comment was shortened once under the comment gate and committed with `COMMENTS_OK=1` as an
allowed type-contract doc; a reviewer who judges a doc comment on an abstract record is not the
allowed case should delete the entry rather than restore the old, now-wrong wording.

### Outside the finding set, left for its own row

`docs/DEPLOYMENT.md:949` still reasons from the availability target as if the denominator were
customer traffic. It survives this round's token sweep because it carries neither of the wrong
figures — it draws a different wrong inference from the same confusion. Left for backlog row
PPW-451; the owner scoped this round to PPW-445.
