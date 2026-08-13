---
type: resolution
target: system
version: 2
answers: reviews/system/review-v2/review-v2.md
fixed_commit: 5245b81
status: resolved
closed: 2026-08-12
---

# Resolution v2 — the review system

## Scope

| Cluster | Findings | Kind | Approach |
|---|---|---|---|
| 1 lib repairs | SF16 SF18 SF20 SF21 SF23 SF26 SF29(code) SF32 SF35(code) | behavioral (fixture tests) | small diffs in lib/, red-first fixtures |
| 2 blinding | SF17 | behavioral (prompt) | bar git history in the lens prompt |
| 3 text authority | SF19 SF27 SF28 SF29(prose) SF35(prose) | doc-cleanup | one authority per rule, token-wide sweep |
| 4 system records | SF25 | records | commit audit files, ledger + metrics for system |
| 5 data contracts | SF30 SF33 SF34 | records | sidecar under state/, correction line, 5-cell rows legalized |
| 6 measurability | SF22 | schema | optional findings[] on verification lines |

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| SF16 | fixed | `fa5eda0` | auditor rebases INDEX/TRACK_RECORD/ID_COUNTER from paths.mjs onto its --root; red→green fixture proves a certified target finds the track record |
| SF17 | fixed | `a59faac` | lens prompt bars git log/show/blame/reflog and commit messages; runbook claim now true; workspace auditor stays build item 3 per gate ruling |
| SF18 | fixed | `fa5eda0` | auditor gains global duplicate-id check across all ledgers (cross-target and same-ledger) + id-counter floor; 905 fixture red→green; filtered runs scan the whole corpus on purpose |
| SF19 | fixed | `152de93` | one authority: fixer hand-writes the fix-round index row from the renderer's printed suggestion; README + skill reworded, phantom Status column gone |
| SF20 | fixed | `ff3e64f` | renderer buckets backlog as deferred, matching auditor and gate; 901 fixture dry-run red→green; fixer skill status list now names backlog |
| SF21 | fixed | `866c843` | corrections keyed to the latest line only — round-keyed to fix-round lines, pass-keyed to pass lines; 907 fixture red→green |
| SF22 | fixed | `87c9c75` | verification lines may carry findings[] of {d,new,sev,fix_generated,sev_delta}; auditor validates shape + tally; runbook-verification instructs recording lineage; 908 fixture red→green |
| SF23 | fixed | `fa5eda0` | citation regex covers PPW-<n> and SF<n>; no fixture (regex-only) — revert-provable by scanning a planted comment |
| SF14 | deferred | — | owner 2026-07-29 ruling stands: seeded run 2 "not now"; re-asked in the v2 summary, no new decision this round |
| SF24 | deferred | — | needs 2–3 more discovery passes to exist, not code; notes wording softened is not owed — data accrues from the next discovery pass on |
| SF25 | fixed | `7fd924a` | test-quality files committed verbatim; system ledger (SF registry) + metrics.jsonl created; doc-contracts scope note names the system target's lightweight records |
| SF26 | fixed | `9c2bc31` | --apply re-resolves a moved file's own relative links from its new directory; dead OR clause removed; known limits (file-vs-dir, silent retarget) recorded in Decisions |
| SF27 | fixed | `152de93` | schema points at the Findings body table; stale "must be widened" sentence corrected; auditor messages renamed |
| SF28 | fixed | `152de93` | closed: legalized in doc-contracts' resolution frontmatter list (retrofitted resolutions already carry it) |
| SF29 | fixed | `a612416` | dormant table writer deleted with its dead imports and parse residue; skill prose agrees with the renderer |
| SF30 | fixed | `cc46bc7` | sidecar contracted at reviews/state/defect-classes.jsonl: paths.mjs constant, miner + spec updated, vocabulary entry |
| SF31 | wont-fix | — | owner ruling 2026-08-12, see Decisions |
| SF32 | fixed | `fa5eda0` | finding-id comment removed from the scanner's own header |
| SF33 | fixed | `222f765` | correction line appended: 044-045 v6 cost.tokens reads null, not zero |
| SF34 | fixed | `1528576` | contract states 5-or-7-cell pass rows; state gate checks it; bad-state fixture asserts the violation |
| SF35 | fixed | `152de93` | runbook dates refreshed; renderer 15-min comment → 30; doc-gate header names the Sonnet judge |

## Decisions

### SF31 — backlog aging: owner ruled wont-fix

Owner, 2026-08-12, at this round's gate: "do nothing here. i do not agree with the
proposals. i am fine for the bugs to stockpile until one day i decide to sweep them
all and fix them." No aging rule, no readout, no groom trigger is built. The queue
drains at owner-called sweeps and the existing drain moments only.

### Round scope decisions (owner: "as proposed", 2026-08-12)

- SF19: the fixer hand-writes the fix-round index row at hand-back; the renderer keeps
  printing a suggestion only.
- SF28: `closed:` becomes a legal resolution frontmatter key in doc-contracts.
- SF25: system target gets committed audit files, a ledger, and a metrics line; stays
  outside the per-target doc contracts otherwise.
- SF30: the class sidecar moves under `reviews/state/` with a path constant and a
  contract mention before the backfill runs.
- SF17: prompt-line fix only this round; the workspace blinding auditor stays design-notes
  build item 3.

### Boundaries and known limits left for the re-reviewer

- SF18: a target-filtered auditor run now fails on any target's duplicate id — global
  corruption should fail everywhere, so this is intended; the id-counter floor arm is
  unexercised by fixtures (inert on the real repo: counter 468, max minted PPW-459).
- SF26: the rewriter cannot tell a file from a directory with the same name, and a link
  that happens to resolve from the new directory is left alone even if it now points at a
  different file. Both recorded by the cluster-1 micro-review; judged not worth code now.
- SF23/SF32: the citation scan still covers only `src/**` — a finding-id comment inside
  `reviews/lib/` stays invisible to it (the pre-commit comment hook is the control there).
  Widening the scan scope is a scope decision this round did not take.
- The system fix-round metrics line is hand-computed from the worklog because the renderer
  expects target-root file layout and the system target groups records per pass folder;
  values follow the schema derivation rules.
- Cluster-1 micro-review found 4 follow-ups, folded in at `a612416`. The final micro-review
  (clusters 2–6) ran all three gates clean and found 6 follow-ups, folded in at `5245b81`:
  skeptic prompts now share the git-history bar, verification lineage entries accept
  legacy `D<n>` ids and validate `sev_delta`, the miner's provenance stamp names the moved
  path, and the design notes' weak-spots list no longer describes fixed defects as open.
- Still prose-enforced only: a verifier who names new defects but omits `findings[]` gets
  no auditor error — the runbook sentence is the control. Left for a future calibration.
- The round-end doc gate ran in `state` mode only; the per-target gate and Sonnet judge are
  scoped to contracted artifacts, and `reviews/system/` is outside the doc contracts.
