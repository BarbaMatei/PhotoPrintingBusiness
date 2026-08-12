---
type: review
target: system
version: 2
supersedes: 1
commit: 94eff97
branch: feat/bolt-045-error-tracking-slos
pass-type: discovery
date: 2026-08-12
lenses: [main-agent critical read, live machinery checks, metrics readout, refute-checker, completeness-checker]
lenses-not-run: []
verdict: approve-with-followups
blockers: []
findings: { high: 0, medium: 7, low: 10, cleanup: 4, refuted: 0, reopened: 1 }
tests: { fixture-suite: "36/36", auditor: "0 errors / 3 warnings", doc-gate-state: "clean" }
---

# Review v2 — the review system

Second meta-review, run blinded from v1: the reviewer read the live system only
(README, rules, runbooks, state, notes, lib, templates, skills), never the v1 files;
dimensions came through a grade-stripping agent; reconciliation to v1 was done by a
separate agent after findings were drafted. Two independent checkers attacked the
draft: one refuting (2 severity cuts, 2 findings demoted to known-open citations,
3 count corrections), one hunting misses (10 new findings, all verified in code).
Ids: re-finds keep their v1 `SF<n>`; new defects continue the sequence at SF17.

## Findings

| ID | Sev | Title | Where | Lineage | Fix now? |
|---|---|---|---|---|---|
| SF16 | 🟠 | REOPENED — certification-watch check dead since the 2026-08-11 regroup: auditor hardcodes the old `reviews/track-record.md` path instead of importing `paths.mjs`, so TRACK is always null, every certified target warns falsely, and the listing check never runs | `lib/records-auditor.mjs:344-346` | v1 fix regressed | yes |
| SF17 | 🟠 | The runbook claims "prompts bar lenses from `reviews/` and git history" but the lens prompt bars only `reviews/` — nothing bars `git log` while every fix commit carries its `PPW-<n>`, so one call hands a blinded lens the prior finding list | `lib/discovery-review.wf.js:126` vs `runbooks/runbook-discovery.md:13` | residual of SF9 | yes |
| SF18 | 🟠 | Duplicate-`PPW-<n>` mint has no working alarm: the documented git-conflict alarm fires only at merge in the owner's 2–3-parallel-worktree workflow — after duplicate ids are burned into two branches' records — and the auditor has no cross-target id-uniqueness check | `rules/doc-contracts.md:49-51`; `lib/records-auditor.mjs` (absent check) | new | yes |
| SF19 | 🟠 | The "fix rounds append their index row at hand-back" rule is unimplemented and unfollowed: the renderer only prints a status suggestion ("printed, never applied"), the fixer skill updates an index "Status column" that no longer exists, and 044-045's five fix rounds have zero index Passes rows | `README.md:109-111` vs `lib/render-records.mjs:5,165` vs `fix-review SKILL.md:235` | new | yes |
| SF20 | 🟠 | Renderer and auditor bucket the legal `backlog` resolution status differently (`open` vs `deferred`), so a rendered fix-round metrics line fails its own audit whenever a round backlogs minors — the router-mandated path; 044-045 round 2 already needed a correction line as a mute | `lib/render-records.mjs:61-63` vs `lib/records-auditor.mjs:95` | residual of SF5 | yes |
| SF21 | 🟠 | The router's correction-surfacing filter compares `correction_for.pass === latest.pass`: round-keyed corrections never surface while the latest line is a pass line, and match every round when it is a fix-round line — the owner-parked-at-hand-back case corrections were built for routes without its note | `lib/route-next-pass.mjs:97-98` | new | yes |
| SF22 | 🟠 | Fix-generativity — the number the 2026-07-22 fixer rules are judged by — is structurally uncapturable: `findings[]`/`fix_generated` exist only on discovery/delta lines, but fix-caused defects surface mainly in verification passes (044-045 v2–v4: 1 new 🔴 + 25 new 🟠, zero lineage records); `fix_generated` is non-null on 0 of 39 entries ever recorded | `rules/metrics-schema.md:53` | residual of SF6 | yes |
| SF14 | 🟡 | RE-FIND (deferred 2026-07-29) — now quantified: new 🔴+🟠 per broad pass never reached zero on any target (015: 19, 11, 17 · 035: 7, 3, 2 · 042: 11, 11, 5, 5 · 043: 7, 5, 3, 6/6, 5 · 044-045: 23); every close rests on skeptic precision plus breadth, never on observed exhaustion | metrics.jsonl, all targets | prior decision attached: seeded run 2 "not now", gate kept visible | owner call |
| SF23 | 🟡 | Citation-leak scan regex covers only legacy id shapes (`BUG-`, `(D<n>`, `(F<n>`, …) — blind to `PPW-<n>`, the only shape minted since 2026-08-11; its "0 occurrences, target 0" is unfalsifiable for current ids (compensating control: the pre-commit comment hook) | `lib/records-auditor.mjs:362` | residual of SF9 | yes |
| SF24 | 🟡 | The per-finding evidence layer is written almost nowhere: 1 of 41 pass lines carries `findings[]`; whole-history `sev_delta` count is 1 — the notes' "accumulates on every pass" overstates; lens-yield and severity-calibration questions stay unanswerable until 2–3 more discovery passes | metrics.jsonl vs `notes/self-driving-loop-design.md:61-62` | residual of SF5 | no — needs passes, not code |
| SF25 | 🟡 | The system target is exempt from the system's machinery: no ledger, no contract, no metrics line, ids outside the global sequence — and the test-quality audit (309 findings, 4 confirmed 🔴) has been an untracked working-tree file since 2026-08-06 (uncommitted half already recorded in the notes' weak spots) | `rules/doc-contracts.md:15-16`; `git status` | new | yes |
| SF26 | 🟡 | The link keeper does not keep the moved file's own links: relative-link rewrite applies only to files at the reviews root (and its second OR clause is dead logic); this round's regroup of the v1 files required hand-fixing 15 broken links the checker caught | `lib/fix-links.mjs:46` | new | yes |
| SF27 | 🟡 | The metrics schema still names "the resolution frontmatter map" as the fix-round data source — the exact shape doc-contracts bans (scalar-only frontmatter) and the doc gate flags as a violation | `rules/metrics-schema.md:119` vs `lib/doc-gate.mjs:208` | new | yes |
| SF28 | 🟡 | The resolution template and the fixer skill mandate a `closed:` frontmatter key that doc-contracts' exhaustive list does not allow; the gate checks neither direction | `templates/resolution.md:8`; `fix-review SKILL.md:213` vs `rules/doc-contracts.md:145-147` | new | yes |
| SF29 | 🟡 | The fixer skill contradicts itself about the rendered table ("no longer generates any table" vs "refreshes the rendered table"), and the renderer still carries a dormant 6-column table writer whose shape violates the contract's 4-column findings table | `fix-review SKILL.md:205,228`; `lib/render-records.mjs:144-156` | new | yes |
| SF30 | 🟡 | The ledger miner hard-requires `reviews/defect-classes.jsonl` — a sidecar at the reviews root, outside the state layout and every contract, colliding with "one-off measurement files are banned"; the approved backfill would create an uncontracted file | `lib/ledger-miner.mjs:24-25` vs `rules/doc-contracts.md:30-31` | new | before the backfill runs |
| SF31 | 🟡 | The backlog's drain design converges on a cliff: 141 rows (2 🔴 · 7 🟠 · 93 🟡 · 39 ⚪) leave only at bolt-open-in-area or the pre-deployment "file empty" gate, so areas without planned bolts pile onto one unsized mass effort at the worst phase (extends the recorded no-aging-signal weakness) | `state/backlog.md` | new | owner call |
| SF32 | ⚪ | The leak scanner's own file carries a finding-id comment ("SF9 tracker") its src-only scan cannot see — the repo-wide comment rule allows none | `lib/records-auditor.mjs:350` | residual of SF9 | yes |
| SF33 | ⚪ | One verification line records `tokens: 0` where the schema mandates null for unknown — a future roll-up sums a false free pass | 044-045 metrics.jsonl, v6 line | residual of SF5 | yes |
| SF34 | ⚪ | The index Passes table is ragged — 28 of 32 rows carry 5 of 7 header columns — and the state gate calls the file clean; either the contract states column-completeness or the gate checks it | `state/index.md` | new | yes |
| SF35 | ⚪ | Stale prose inside the machinery: the renderer comment says "gaps <= 15 min" while the cap is 30; both runbooks' frontmatter says updated 2026-07-24 though they describe post-08-10 mechanisms (the doc-gate Haiku-judge header is already in the notes' weak spots) | `lib/render-records.mjs:94`; runbook frontmatter | new | yes |

## Known-open, cited not re-raised

- Router cost table hardcoded from 2026-07-30 while metered verification actuals run to
  772k vs the announced 60–250k — already in the notes' weak spots, verified still true;
  it prices owner gate decisions wrong by up to 3× and the actuals sit unread in metrics.jsonl.
- Verification main-agent labor unmetered (schema scope decision, SF7 fixed-as-declined);
  10 of 24 verification lines carry no token figure.

## Adjusted by checkers

| Draft claim | Correction |
|---|---|
| "Every broad look found ≥3 new serious" | 035 v8 found 2 — claim weakened to "never zero"; 035 series (7, 3, 2) had been omitted |
| Citation-scan gap at 🟠 | Cut to 🟡 — the pre-commit comment hook is a compensating control |
| Router cost table + backlog-cliff + system-folder items as new 🟠/🟡 findings | Demoted or narrowed — cores already recorded in the notes' weak spots; only the new halves kept (SF25, SF31) |
| fix-links guard "line 52", "~26 ragged rows" | Line 46; 28 rows |

## Extracted metrics (first readout of the recorded-but-unread fields)

- Recorded pass tokens by type: discovery 27.5M · verification 3.7M · delta 3.0M ·
  certification 2.9M (≈37M total; fix-round tokens: null on all 5 recorded rounds).
- Fix-round runtime across 044-045: 6.0 h active · 0.5 h blocked on owner · 114.8 h idle.
- Lens yield exists for exactly one pass (044-045 v1, 39 findings): every manifest lens
  raised ≥4, security/input-validation/completeness-critic carried 6 serious each —
  single-pass sample, no cross-target conclusion possible yet.
- Severity-calibration data: 1 `sev_delta` in history. Fix-lineage data: 0 non-null ever.
- Serious-per-pass series (SF14 row above): no target reached zero before close.

## Recommendations, ranked

1. **Repair the mechanical trust layer** (one session, small diffs): SF16 paths →
   constants import; SF23 PPW-aware regex; SF18 cross-target id-uniqueness check in the
   auditor; SF21 correction filter; SF20 one bucketing rule for `backlog`; delete the
   SF29 dormant writer. Six dead-or-wrong checks come back honest.
2. **One authority per rule** (one session, text only): resolve SF19 (who writes the
   fix-round index row — pick the renderer or the skill, fix README), SF27, SF28, SF29's
   prose half, SF35. The redesign wave outran the texts; the repo's own "standards are
   descriptive" rule demands the sweep.
3. **Close the blinding gap** (SF17): one line in the lens prompt barring git history now;
   the workspace-half auditor (notes' build item 3) as the durable fix.
4. **Make fix-generativity measurable** (SF22): let verification passes append per-finding
   lineage (or a `fix_generated` count), else strike the question from the schema's promises.
5. **Bring the system target under its own law** (SF25): commit the test-quality files,
   give `reviews/system/` a minimal contract — ledger with global ids or a registered SF
   sequence, a metrics line per meta-pass.
6. **Seeded run 2** stays the top trust item (SF14, owner-deferred): the decay readout
   strengthens its case — nothing in five targets ever showed saturation, so recall is
   still an article of faith. ~2–2.5M tokens when the owner wants it.
7. **Backlog aging** (SF31): a per-area age/severity readout at every loop-driver run and
   an escalation rule for 🔴 rows; the groom-pass spec in the notes already covers the rest.

## From the self-driving-loop design — what to take further now

The notes' own A-then-B recommendation stands, with one insertion: run recommendation 1–2
(repair + reconcile texts, ~2 sessions, near-zero run tokens) **before** scaling anything —
B's scheduler and verification script would otherwise automate a layer whose checks are
provably mis-wired. The free `sev_delta`/lens-yield readout the notes call for now exists
(this review's readout script) and can land in `lib/` as-is.

## Notes for the fixer

- SF16, SF18, SF20, SF21, SF23, SF32, SF33 are all inside `lib/` — one cluster, one
  fixture-suite run (`node reviews/lib/tests/run-tests.mjs`) covers them.
- SF19/SF27/SF28/SF29 are text decisions before edits: pick the single authority first,
  then sweep the three files together.
- SF17's one-line prompt fix is independent of the workspace auditor — ship it alone.
- Do not fix SF14/SF31 in code; they are owner decisions.
- The v1 resolution convention applies: respond in `resolution-v2.md`, never edit this file.
