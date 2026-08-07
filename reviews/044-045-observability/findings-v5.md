---
type: findings
target: 044-045-observability
version: 5
for: review-v5.md
commit: d37f867
date: 2026-08-07
---

# Findings detail — v5 verification of the 044-045 v4 fix round

Companion to [review-v5.md](review-v5.md). Canonical identities in [ledger.md](ledger.md).
Severity vocabulary and the re-arm rule are in [reviews/README.md](../README.md).

Every measurement below was taken by the main agent at `d37f867` (source identical to `52a0cb9`,
the resolution's `fixed_commit` — the two commits after it touch `reviews/**` only), scoped to the
observability namespaces — `Integration` +
`Unit.{Observability,Middleware,Configuration,Validators,Services,Controllers,BackgroundJobs}`,
**1137 tests**, 10 MinIO skips — per the repo's scoped-run rule. That is v4's 1133 plus the four
tests this round added. "0 red" always means the full 1137 stayed green with the mutation applied.

---

## F1 · 🟡 · D121 · The `or vector(0)` guards this round added to SLO 4 and SLO 5 are pinned by nothing

**Files:** `memory-bank/operations/slos.md:142`, `:173`,
`ops/dashboards/fototipar-overview.json:271`, `:310`,
`src/PhotoPrint.Tests/Integration/DashboardMetricNamesTests.cs:133`
**Cause:** fix-caused (D105) — the guards were added mid-round by the round's second micro-review.

D103's fix is a **class rule**: for every query in `slos.md` and the dashboard, each side of the
division carrying N added terms must show N `or vector(0)` occurrences. It works — deleting SLO 3's
two guards reddens it (M1 below). But the rule opens with `if (terms == 1) continue;`, so a side
with no `+` is never examined, and the guards the same round added to the **SLO 4 and SLO 5
numerators** are exactly that shape: a single `sum(...)` term.

**Measured (M2):** deleting `or vector(0)` from the SLO 4 numerator in **both** copies leaves the
suite green — 5 of 5 `DashboardMetricNamesTests` pass, no test anywhere sees it.

**Failure scenario.** Identical to D103's, at the two sites this round created. A later author
simplifies `(sum(awb_creation_total{result="ok"}) or vector(0)) / …` to
`sum(awb_creation_total{result="ok"}) / …`. Suite green, build green. On a fresh process where no
AWB has yet succeeded — expired Sameday credentials is the case the fixer named — `{result="ok"}`
matches no series, the numerator is an empty vector, and the AWB panel reads **"No Data" instead of
a red 0%** while every order is failing to get a label.

**Recorded rather than smoothed over:** the fixer disclosed this hole in
[resolution-v4](resolution-v4.md#the-second-micro-review-broke-one-of-my-two-remaining-fixes-and-i-confirmed-it-myself)
and argued that broadening the rule to "every literal-valued `sum()` needs a guard" would red
legitimate panels. That argument holds for SLO 2's latency histogram and SLO 1's `!~` numerator, so
this is not "the fixer was lazy" — it is a disclosed limit that still leaves two new sites
unpinned. A per-panel list ("these ratio numerators must be guarded") pins them without the
false positives; that is the suggested shape, not a demand for a PromQL parser.

---

## F2 · 🟡 · D122 · The acceptance criterion still says SLO 4 excludes only `skipped`, and attaches the `retry_later` reason to it

**File:**
`memory-bank/intents/020-observability-stack/units/002-error-tracking-and-slos/stories/002-slo-documentation-and-dashboard.md:27-29`
**Cause:** fix-caused (D105).

The shipped SLO 4 denominator is
`sum(awb_creation_total{result!="skipped",result!="retry_later"})` — **two** exclusions — and the
round also minted `orphaned` specifically so one `skipped`-shaped case stays *inside* the
denominator. The acceptance criterion the bolt is judged against reads (at HEAD):

> AWB auto-creation ≥ 98% (`ok` over all results except `skipped` — amended 2026-08-06: a
> `skipped` outcome means no label was needed, so counting it would flag the retry loop the 2%
> budget exists to protect)

Two errors in three lines. The exclusion set is understated (`retry_later` is missing), and the
rationale given for `skipped` — "would flag the retry loop" — is the rationale for excluding
`retry_later`; `skipped` is excluded because no label was needed. `orphaned` is not mentioned at
all. The doc was amended in `b0718d8`, before the second owner gate added `retry_later` in
`9112aa8`, and the second amendment never landed.

**Failure scenario.** The story file is the bolt's own record of what was agreed. A reader
reconciling doc against query sees an exclusion in the query that the criterion does not authorise
and cannot tell which is the mistake — the same confusion the round's own micro-review hit when it
found the acceptance doc unamended the first time (recorded in resolution-v4 as "The acceptance doc
was missed"). This is that defect's second instance, one gate later.

---

## F3 · 🟡 · D123 · The outcome union's only doc comment now describes the orphaned case as a plain skip

**File:** `src/PhotoPrint.API/Services/Sameday/AwbCreationOutcome.cs:9`
**Cause:** fix-caused (D105).

`Skipped` gained a flag that changes which SLO bucket the outcome lands in:
`Skipped(string Reason, bool Orphaned = false)`, where `Orphaned: true` is counted as an AWB
**failure** and plain `Skipped` is excluded from the ratio entirely. The `<list>` documenting the
union was not touched, and still reads:

> `Skipped` — order no longer eligible (cancelled, AWB already exists).

"cancelled" is now the *one* case that must **not** be a plain skip: an order cancelled after the
vendor created the label is the orphaned case (`AwbCreator.cs:269-273`). So the sentence names the
failure case as the benign example, mentions neither the flag nor the metric consequence, and sits
next to `RetryLater`, whose two flags *are* documented in the same list at
`AwbCreationOutcome.cs:10`. Under CLAUDE.md's comment rule this is one of the two places a comment
is allowed (a behaviour description on a type contract), so the fix here is to correct it, not
delete it.

**Failure scenario.** A future author adds a seventh `Skipped` site — a second path where a
billable label exists but the order moved on — reads this list, sees no flag, and returns plain
`Skipped`. The orphaned label is then excluded from SLO 4 on both sides, which is exactly the defect
`9112aa8` was written to close, re-introduced with a green suite and no reviewer signal.

---

## Measured confirmations that are not findings

Recorded because each settles a claim the resolution or a lens made, and a later pass should not
re-measure them.

- **Negative-matcher label *values* are outside the build-check net, as documented.** Mutating
  `result!="skipped"` to `result!="skippedX"` in both copies leaves 5 of 5
  `DashboardMetricNamesTests` green (M6); mutating the label **name** to `resultx!="skipped"`
  reddens `Every_queried_label_exists_on_the_series_it_filters` (M7). Both match `slos.md`'s status
  block, which states this globally, and the fixer's claim in resolution-v4's F3 note.
- **The F1 guard rule cannot pass by finding nothing.** Collapsing SLO 3's two-term numerator to a
  single `result=~"ok|duplicate"` matcher in both copies reddens the guard test on its
  `addedTermsSeen >= 4` floor (M1b), not on the guard assertion.
- **The `.gitleaksignore` fingerprints are correct at the byte level.**
  `44c3e2de…:src/PhotoPrint.Tests/Unit/Configuration/SentryDataScrubbersTests.cs:16` and
  `295a51cc…:src/PhotoPrint.Tests/Integration/SentryOptionsWiringTests.cs:19` — the literal sits on
  exactly those lines in exactly those commits, read out of the commits themselves.
- **Correction to a resolution-v4 boundary claim.** It records the old literal as "still sitting in
  three tracked `reviews/**` docs, and `reviews/**` is not in `.gitleaks.toml`'s allowlist" as an
  unswept risk. It is in **five** such docs (`findings-v1.md:105`, `:107`, `findings-v4.md:131`,
  `resolution-v4.md:13`, `:149`, `summary-v4.md:46`) — and gitleaks does **not** flag any of them:
  the pull-request scan is green at `d37f867` with all five present in the PR's commit range. The
  risk is measured absent, not merely unswept.
- **No sibling `Skipped` site hides a failure.** All six construction sites in `AwbCreator.cs`
  were read (`:91`, `:95`, `:97`, `:123`, `:264`, `:272`); the first five are genuinely "no label
  was needed" and only `:272` creates a billable label, which is the one carrying `Orphaned: true`.
  This is the class question D105's fix had to answer, and it answers it.

---

## Deferrals — all stand

The fix round changed 14 source files. Every deferred/backlog ledger row citing one of them was
re-read against this round's diff; the rest stand mechanically at `dc203c7` from
[review-v4](review-v4.md#deferrals).

| Row | Cited file this round touched | Verdict |
|---|---|---|
| D37 | `Observability/MetricNames.cs` | stands — the round added `orphaned`, which *is* emitted; the ANAF vocabulary D37 names is untouched |
| D46 | `memory-bank/operations/slos.md` | stands, still owner-parked — SLO 1's query and the dilution prose were not touched |
| D59 | `Services/Sameday/AwbCreator.cs` | stands — the shutdown carve-out at `:50` is unchanged; the round edited `:67-72` and `:269-273` |
| D68 | `Tests/Unit/Observability/MetricsCardinalityTests.cs` | stands — only the expected count moved 5 → 6 |
| D85 | `Tests/Integration/SentryOptionsWiringTests.cs` | stands — only the fixture constant was renamed |
| D86 | `memory-bank/operations/metrics.md` | stands — the round edited the AWB result-value table, not the add-a-metric procedure |
| D88 | `Tests/Integration/DashboardMetricNamesTests.cs` | stands, extended — the new guard test consumes `DashboardQueries()`/`SloQueries()` and inherits their reach limits (`templating`/`annotations` unwalked, untagged fences only). Left in D88's family per the fixer's note rather than minted as a new row |
| D107 | `Tests/Integration/SentryOptionsWiringTests.cs` | stands — rename only |
| D110 | `memory-bank/operations/slos.md:8-12`, dashboard `:60` | stands — **still on the operator's wall**, untouched by this round, and still the one minor worth the owner's eye |
| D111 | `memory-bank/operations/slos.md` | stands — SLO 4 and SLO 5 were rewritten and still carry no time window, while their dashboard twins use `rate(…[7d])`/`[30d]`. The rewrite neither fixed nor worsened it |
| D112 | `Services/Sameday/AwbCreator.cs` | stands — `:166` and `ShipmentTrackingJob` untouched |
| D117 | `ops/dashboards/fototipar-overview.json` | stands — the Availability panel `description` was not touched |
