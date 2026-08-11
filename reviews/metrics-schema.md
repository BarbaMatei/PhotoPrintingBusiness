---
type: review-metrics-schema
status: active — v3
created: 2026-07-04
updated: 2026-08-03
owner: Matei Barba
---

# Review metrics — what every pass and fix round records, and why

Every review pass (discovery, delta-discovery, *and* verification) appends **one line** to the
target's `reviews/<target>/metrics.jsonl` at synthesis time — after findings are settled, before
the review file is finalized. Since v3 (2026-08-03), every **fix round** appends a line too, at
hand-back. Append-only: never edit a past line; corrections are their own
appended lines (see *Corrections*). Unknown values are `null`, never guessed.

This data answers the open questions in
[self-driving-loop-design.md](self-driving-loop-design.md): does new-serious-per-pass decay?
what does a pass cost? which lenses earn their keep? does fixing create new defects at a lower
rate than before? It cannot be reconstructed later — bolt 035's `cost` fields are `null`
forever because nobody recorded them at the time.

**Scope note (v3, replaces the v2 "passes only" note — owner decision 2026-08-03):** this file
meters **passes and fix rounds**. Synthesis and main-agent verification labor are still not
metered; roll-ups must say which line types they summed. Fix-round lines exist for the speed
question the v2 scope could not answer: where does the wall-clock time go, and is it active
work, waiting on the owner, or nobody at the wheel.

**After appending, run the auditor** — it validates the new line against this schema and
cross-checks the target's records; it must exit clean (legacy drift reports as warnings):

```
node reviews/lib/records-auditor.mjs <target>     # or no args = all targets
```

## v2 fields (one JSON object per line; lines dated ≥ 2026-07-30 are validated strictly)

| Field | Type | Meaning |
|---|---|---|
| `target` | string | the reviewed unit, e.g. `"043-cloud-storage-provider"` |
| `pass` | int | review version (matches `review-v<n>.md`); a certification **pair** writes two lines with the same `pass` and subtypes A/B |
| `type` | `"discovery"` \| `"delta-discovery"` \| `"verification"` | certification passes are `discovery` (they are full-manifest passes and belong on the decay curve) |
| `subtype` | `"certification-pair-A"` \| `"certification-pair-B"` \| `"certification-single"` \| absent | discovery lines only |
| `date` | ISO date | when the pass ran |
| `commit` | string | the commit reviewed |
| `code_tip` | string, optional | tree tip when it differs from the reviewed commit |
| `delta_base` | string, delta passes | base commit of the reviewed diff |
| `lenses` | array of lens keys \| null | keys only (e.g. `"correctness"`), never prose |
| `verdict` | string \| null | the review's verdict |
| `outcome` | `"certified"` \| `"not-certified"` \| absent | certification lines only |
| `mediums_open_at_close` | int | **required when `outcome: "certified"`** — 🟠 count not `fixed`/`verified` at close (mirrors the index-row rule, calibration 2026-07-29) |
| `new_findings` | `{high, medium, low, cleanup}` | **new** problems this pass named (info items count as `cleanup`, note it) |
| `findings` | array | **required on strict discovery/delta lines** — one entry per canonical finding, see below |
| `refinds_identity` | int | same problem as an earlier finding (reconciler judgment) |
| `reraises_of_decided` | int | findings re-raising an accepted wont-fix / deferral / dismissal |
| `refuted` | int | candidate findings recorded as false positives this pass |
| `deferrals_upheld` | int, optional | prior terminal decisions re-affirmed this pass (canonical name) |
| `disputed` | historical only | trace-first verification (2026-07-27) can no longer produce it |
| `verified` | int | findings flipped to `verified` this pass |
| `reopened` | int | findings reopened this pass |
| `tests` | `{passed, failed}` \| null | **combined** suites (backend + frontend) at the reviewed commit; per-suite splits go in `notes` |
| `cost` | `{agents, tokens, agents_by_stage?}` | `tokens` = output tokens the pass's workflow(s) reported (never `subagent_tokens`); `agents_by_stage` keys: `lenses, dedup, skeptics_guard, skeptics_trace, reraise_skipped, budget_skipped, approach_checks` — copy from the discovery script's `_canonical` line; `approach_checks` counts the synthesis-time pre-checks (v3) |
| `runtime` | `{started, ended}`, v3 | ISO timestamps from the loop-driver's `pass-launch` / `pass-records-done` worklog stamps |
| `notes` | string | anything a future analysis will wish it knew |

### `findings[]` — the per-finding record (new in v2)

One entry per **canonical** finding of a discovery/delta pass, including re-raises and refuted
candidates (they carry lens-precision information). Written after reconciliation, so `d` is
known. Sources: the discovery script's output (`agreeingLenses`, `convergence`, `hinted`,
`verdict`) and the reconciler (`d`, `new`, `fix_generated`).

| Key | Type | Meaning |
|---|---|---|
| `f` | `"F<n>"` | the finder's own number for this find, kept as provenance; it exists nowhere else |
| `d` | `"PPW-<n>"` | the ledger id minted at reconciliation |
| `new` | bool | true when `d` was minted this pass — Σ by `sev` over `new: true` entries must equal `new_findings` |
| `sev` | high\|medium\|low\|cleanup | final synthesis severity |
| `lenses` | array | the lenses that independently raised it (`agreeingLenses`) |
| `conv` | int ≥ 1 | convergence count |
| `hinted` | bool | topic planted by shared prompt hints |
| `verdict` | script verdict enum | `confirmed` · `plausible` · `refuted` · `re-raise` · `unverified-*` |
| `fix_generated` | `"PPW-<n>"` \| null | the earlier finding whose **fix** caused this one (reconciler `residual-of` lineage) |
| `sev_delta` | `"<lens-max>-><final>"` \| null | only when synthesis changed the severity vs the lens maximum |

Lines appended before 2026-08-11 carry the old per-target names in `d` and `fix_generated`
(this file is append-only, so they stay); `reviews/archive/id-map.md` translates them.
Two things follow from the global ids: the auditor's `d` check still expects the old shape
and must be widened before the next discovery line is appended, and `f` is now the only
place a finder's own number is ever recorded.

What this buys, after 2–3 more targets: per-lens yield ("which lenses earn their keep") by
grouping on `lenses`; the fix-generativity rate (`fix_generated` non-null / `new`) that tells
whether the 2026-07-22 fixer rules work; and an audit trail on synthesis severity changes
(`sev_delta`), the stop rule's pivot.

## v3 (2026-08-03): the fix-round line and the worklog

Two additions; everything above still holds for pass lines. Lines dated on/after 2026-08-03
are validated strictly against v3.

**The worklog** — `reviews/<target>/worklog.jsonl`, append-only, one JSON event per line,
each with `t` (ISO timestamp) and `ev`. Written **at the moment events happen** by whoever
drives them: the `/fix-review` skill during fix rounds (`round-start`, `triage-done`,
`gate-open`/`gate-closed`, `check-dispatched`/`check-returned`, `test-run`, `finding`,
`micro-review-dispatched`/`-returned`, `round-end`), the loop-driver around passes
(`pass-launch`, `pass-records-done`) and owner gates. It is the crash-safe evidence trail
and the input `reviews/lib/render-records.mjs` computes runtime from.

**The fix-round line** — appended by `render-records.mjs` at hand-back (one per round;
a wrong line is corrected by a correction line, never edited):

| Field | Type | Meaning |
|---|---|---|
| `target` / `type` / `date` | string / `"fix-round"` / ISO date | as for passes |
| `round` | int | matches `resolution-v<n>.md` (fix rounds have no `pass`) |
| `base_commit` | string | the reviewed commit the round answers (review frontmatter) |
| `fixed_commit` | string \| null | the resolution's `fixed_commit` (null while `in-progress`) |
| `findings` | `{fixed, wont_fix, deferred, disputed, false_positive, open}` | counts from the resolution frontmatter map (`in-progress` counts as `open`) |
| `tests` | `{invocations, red_runs, green_runs, final: {passed, failed}}` | from `test-run` events; `final` from the last `kind:final` event, null if none |
| `approach_checks` | `{pre_cleared_consumed, run, tokens}` | review-time verdicts used vs checks this round ran (`check-*` events); `tokens` null if unreported |
| `micro_reviews` | `{count, follow_up_fixes}` | per-cluster micro-reviews and the extra fixes they caused |
| `cost` | `{agents, tokens}` | subagents this round dispatched; tokens null unless known |
| `runtime` | `{started, ended, active_s, blocked_s, idle_s, blocked: [{reason, s}]}` | see derivation below |
| `notes` | string | e.g. `pilot`, deviations, what broke |

**Runtime derivation (declared convention, not precision):** `blocked_s` = Σ
`gate-open`→`gate-closed` spans (each listed in `blocked[]` with its reason — a question the
owner saw an hour later is an hour of `blocked_s`). `active_s` = Σ gaps between consecutive
non-gate events **≤ 30 minutes**; a longer unexplained gap means nobody was at the wheel.
`idle_s` = (`ended` − `started`) − `active_s` − `blocked_s`. The cap deliberately errs
toward **over**-counting active time — a speed metric must not look better by under-counting
work (long silent stretches count as work; only clear absences count as idle).

## Corrections

A past line is never edited. A correction is its own appended line:

```json
{"target":"<t>","date":"<iso>","correction_for":{"pass":7,"field":"new_findings"},"note":"what is wrong and what is authoritative"}
```

This works for **closed targets** too (the v1 rule "a note in the next line" silently failed
once a target stopped producing next lines).

**Correcting a fix-round line** (added 2026-08-05): fix rounds have a `round`, not a `pass`, so
the key is `correction_for.round`:

```json
{"target":"<t>","date":"<iso>","correction_for":{"round":2,"field":"findings"},"note":"…"}
```

This exists because a disposition can change *after* the renderer has written the line — an owner
parking a finding at hand-back is the case that found it. When a correction targets a round's
`findings`, the auditor stops cross-checking that line's tallies against the resolution
frontmatter (they are legitimately out of step) and reports the skip as a warning, so the
supersession stays visible rather than silent.

## Example (strict v2 discovery line, abbreviated)

```json
{"target":"044-observability","pass":1,"type":"discovery","date":"2026-08-02","commit":"abc1234","lenses":["correctness","observability","tests-coverage"],"verdict":"request-changes","new_findings":{"high":1,"medium":2,"low":1,"cleanup":0},"findings":[{"f":"F1","d":"D1","new":true,"sev":"high","lenses":["correctness","observability"],"conv":2,"hinted":false,"verdict":"confirmed","fix_generated":null,"sev_delta":null},{"f":"F2","d":"D2","new":true,"sev":"medium","lenses":["tests-coverage"],"conv":1,"hinted":false,"verdict":"plausible","fix_generated":null,"sev_delta":"high->medium"}],"refinds_identity":0,"reraises_of_decided":0,"refuted":1,"verified":0,"reopened":0,"tests":{"passed":1381,"failed":0},"cost":{"agents":18,"tokens":950000,"agents_by_stage":{"lenses":6,"dedup":1,"skeptics_trace":9,"skeptics_guard":2}},"notes":"first pass"}
```

## Legacy lines (dated before 2026-07-30)

Validated leniently; the auditor reports known v1 drift as aggregated warnings, never errors.
Readers of old lines need this table:

| Legacy form | Where | Read as |
|---|---|---|
| `cost.subagent_tokens` | 042 verification lines | `cost.tokens` |
| `deferred_reaffirmed` / `disputed_upheld` | 042 | `deferrals_upheld` |
| `tests.frontend_passed/_failed` | 042 | separate frontend suite (v2 combines) |
| `base` | 042 | `delta_base`/baseline commit |
| `type: "certification"` | 015 pass 5 | `discovery` + `subtype: certification-single` |
| `certified: "serious-clean"` | 043 pass 9 | `outcome: "certified"` |
| prose in `lenses` | 035 passes 6–8 | composition not recorded |
| free-form `agents_by_stage` keys | 015 passes 3, 5 | pair/replay stage counts |

## Rules

- **Pass lines**: the synthesis step appends them. **Fix-round lines** (v3): the renderer
  appends them at hand-back — the fixer never hand-writes a metrics line.
- `refinds_identity` / `reraises_of_decided` / `fix_generated` use the **`reconcile-findings`
  skill's** judgment (labeling rules per
  [.claude/skills/reconcile-findings/overlap-ground-truth.md](../.claude/skills/reconcile-findings/overlap-ground-truth.md)).
- No global roll-up file: compute cross-feature summaries on demand from the per-feature files
  (they are labeled pass-cost-only); a hand-maintained roll-up would drift.
- Run the auditor after every append.
