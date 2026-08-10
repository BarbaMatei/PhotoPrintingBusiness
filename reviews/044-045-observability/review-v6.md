---
type: review
target: 044-045-observability
version: 6
supersedes: review-v5.md
pass-type: verification
commit: a4eb7e5
code_tip: 6cae573
answers: resolution-v5.md
verdict: approve-with-followups
date: 2026-08-10
---

# Review v6 — 044-045-observability (verification of the v5 fix round)

Anchored, per-fix verification of [resolution-v5.md](resolution-v5.md) at its `fixed_commit`
`a4eb7e5`. The v5 pass named three findings, all 🟡 (D121, D122, D123); the round fixed all three
plus one owner-directed backlog row (D110). This pass asks one question per fix — *did it hold?* —
plus the runbook's three fix-diff questions per cluster. It is **not** a fresh review of the feature.

**Independence.** Run from a fresh session with no fix-round context: the round ran 2026-08-07 →
2026-08-10 in another session, and nothing of it was carried in here. Source at the branch tip
`6cae573` is byte-identical to `a4eb7e5` (the one commit since touches `reviews/**` only), so
verification ran in place rather than on a detached checkout.

**Method deviation, recorded.** No subagents were used: this session runs under a standing
instruction not to spawn them unless asked. The runbook's step 3 (one anchored agent per *changed*
judgment item) and step 4 (fix-diff questions asked by the owning lens) were therefore done by the
main agent, reading each cited file at HEAD and each cited code site. Every claim below is either a
measurement or a file-and-line read recorded here — nothing is taken on a summary.

## Verdict: approve-with-followups

**Four of four fixes hold.** One is proven by measurement (revert-and-rerun, plus the per-source A/B
the fix round's micro-review forced); three are prose with no build check and are proven by reading
them against the code they describe.

| Fix | Ledger | Outcome |
|---|---|---|
| F1 the four hand-named success numerators are pinned, per source | D121 | **verified** — reddens on guard deletion *and* on a deleted panel that a duplicated doc copy tries to cover |
| F2 acceptance criterion names both SLO 4 exclusions | D122 | **verified** — matches the shipped query and the per-attempt counter |
| F3 outcome union + operator log table describe `skipped` and `orphaned` correctly | D123 | **verified** — matches all six `Skipped(...)` sites and the `Error`-level orphaned log |
| D110 dilution figures on the doc and the operator panel | D110 | **verified** — every figure recomputed from the source it names |

**4 verified · 0 declined · 0 reopened · 1 new finding (0🔴/0🟠/0🟡/1⚪).**

**The loop does not re-arm.** The one new finding is ⚪ and is the residual the fixer disclosed:
the pinned list is hand-maintained. It changes nothing that ships today.

## How each fix was proven

Two mutations, both predicted before the run, both matched. Scope: `PhotoPrint.Tests.Integration` +
`Unit.Observability` — **282 passed / 0 failed / 10 MinIO skips** at `6cae573` with a source-clean
tree, before and after every mutation.

| # | Mutation | Predicted | Measured |
|---|---|---|---|
| M1 | D121: `or vector(0)` deleted from the SLO 4 numerator in **both** copies (`slos.md`, dashboard) | 1 | 1 — `Every_hand_named_success_numerator_keeps_its_absent_series_guard`, 281 others green |
| M2 | D121: the whole AWB panel deleted from the dashboard **and** the doc's SLO 4 query duplicated — the exact shape the round's first version let pass | 1 | 1 — same test, failing on `missing` = `awb_creation_total{result="ok"} in the dashboard`, 281 others green |

**M1 is the mutation review-v5 measured green** (its M2) and is now red: the gap D121 named is
closed. **M2 is the fix round's own A/B re-run by someone else** — the micro-review's finding was
that a total-occurrence floor of 2 let two doc copies stand in for a deleted panel; the committed
rule counts per source, and this pass reproduced the red independently.

**One probe not run, and why.** The resolution reports a vacuity probe (both query collectors forced
empty → red). M2 proves the same mechanism from the other side: the `missing` list fires per source
when a copy disappears, so an empty collector cannot read as "nothing to check". Not re-run.

## The judgment items, read against the code

| Fix | Claim in the doc | Checked against | Verdict |
|---|---|---|---|
| F2 | SLO 4 excludes `skipped` **and** `retry_later`; `orphaned` stays in the denominator | `slos.md:146` and `fototipar-overview.json:271` both read `result!="skipped",result!="retry_later"`; `orphaned` is its own value (`MetricNames.cs:68`) so it is inside the denominator and outside the numerator | holds |
| F2 | `retry_later` is counted once per attempt | `AwbCreator.CreateForOrderAsync(orderId, attempt, …)` records one counter row per call (`AwbCreator.cs:61`, `:77`) | holds |
| F3 | `Skipped` = order missing · not `Paid` · already has an AWB · another worker's claim · vendor dedup | the five benign sites: `AwbCreator.cs:91`, `:95`, `:97`, `:123`, `:264` | holds — all five, no sixth |
| F3 | `Orphaned` is the exception, counted against SLO 4 | `AwbCreator.cs:272` sets `Orphaned: true`; `RecordOutcome` maps it to `orphaned` (`:70`) | holds |
| F3 | the new operator row: `sameday.awb.orphaned …`, **Error**, manual void | `AwbCreator.cs:269-271` — `LogError`, marker and both fields match | holds |
| D110 | 5,760 `/metrics` scrapes/day | `scrape_interval: 15s`, `DEPLOYMENT.md:1049` → 86,400 ÷ 15 | holds |
| D110 | 2,880 `/health` checks/day | `HEALTHCHECK --interval=30s`, `Dockerfile:43` → 86,400 ÷ 30 | holds |
| D110 | floor ≈ 94.5% against ~500 customer req/day | `DEPLOYMENT.md:951`; 8,640 ÷ 9,140 = 94.53% | holds |
| D110 | both are counted at all | `ObservabilityExtensions.cs:98` adds ASP.NET Core metrics with **no filter**, so `/health` and the scrape listener's `/metrics` both land in `http_server_request_duration_seconds_count` | holds — the premise is real, which is D46 |

Two line references in the resolution are off by one (`DEPLOYMENT.md:1048` and `:950`; the text is at
`:1049` and `:951`). The shipped documents cite files, not lines, so nothing user-facing is wrong.

## The three fix-diff questions

- **Class or instance.** *Guards:* the four pinned selectors are **every** hand-named literal-value
  selector in either file — enumerated: `payment_webhook_total{result="ok"}` and `{result="duplicate"}`
  (`slos.md:98-99`, dashboard `:232`), `awb_creation_total{result="ok"}` (`:145`, `:271`),
  `invoice_anaf_status_total{status="accepted"}` (`:176`, `:310`). Nothing unpinned is left today; the
  list being hand-maintained is the new ⚪ (D124). *Stale wording:* the `no longer eligible` phrasing
  survives only in `memory-bank/bolts/037-…/ddd-01-domain-model.md:138`, `:324`, which the fixer left
  deliberately as point-in-time bolt records — re-checked, and that is the whole remainder.
  *Wrong figures:* no `99.7%` or bare `5,760` copy survives anywhere outside `reviews/`.
- **New surface at the bar.** The round's only new mechanism is the test itself. It has a sized
  default (a 24-character look-ahead, argued in the resolution and left as chosen), a signal (both
  assertion messages name the source and the selector — M1 and M2 printed them), and it fails on
  absence as well as on breakage. Not documented in `slos.md`'s test description, which still lists
  names/labels/values only — pre-existing since D103's fix, not raised here.
- **Regression.** Nothing production changed: the diff is one test file, three documents, one panel
  `description` and one `///` union comment. Suite runtime moved by 48 ms (the new test). The
  dashboard JSON still parses and every other panel query is untouched.

## Findings

| F# | Sev | D# | Title | Cause |
|---|---|---|---|---|
| F1 | ⚪ | D124 | The guarded-selector list is hand-maintained, so a fifth hand-named success numerator ships unpinned and nothing notices | fix-residual of D121 |

Detail in [findings-v6.md](findings-v6.md).

## Deferrals

**All standing terminal decisions stand — 57, none closes.** The round changed six files; the seven
non-terminal rows citing one of them were re-read against the diff, agent-free:

- **D46 stands, still owner-parked** — SLO 1's query is untouched; only the prose describing its
  dilution was corrected. The claim is now right, the dilution is not fixed.
- **D111 stands** — SLO 4's documented query is still windowless (`slos.md:145-146`) while the panel
  uses `rate(…[7d])`.
- **D116 stands** — `DEPLOYMENT.md:950` still reasons from the availability target as if the
  denominator were customer traffic; the round did not touch §13.9.
- **D117 half-closed, stands** — the unresolvable `Tracked as D46` citation is gone from the panel
  description (D110's fix), but the description and the give-up `status=` log field are still pinned
  by nothing.
- **D88 stands, extended** — the new test consumes the same two query walkers and inherits their
  reach limits.
- **D55, D58 stand** — untouched `DEPLOYMENT.md` sections; D58's line drifts +1 from the added
  operator row.

## Tests

- Local, Windows, scoped to `PhotoPrint.Tests.Integration` + `Unit.Observability`: **282 passed /
  0 failed**, 10 MinIO skips, at `6cae573`, green before and after both mutations. This is a
  **narrower scope than v5's 1137** — the round's diff touches documents, one panel, one doc comment
  and one test file, so the repo's scoped-run rule puts nothing else in reach.
- CI not re-read: no workflow, gate or source file changed since `d37f867`, where v5 recorded all
  four gates green on both events.
- Frontend not run — no frontend file in the diff.
- Manifest lenses `db-parity` and `frontend-ux` remain **owed, not waived**.
