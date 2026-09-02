---
type: review-ledger
target: system
updated: 2026-08-27
---

# Ledger — the review system (meta-target)

Status registry for `SF<n>` ids — the system target's own sequence, outside `PPW-<n>`
(doc-contracts scope note). Detail lives in the pass folders: v1 findings in
[review-v1](review-v1/review-v1.md) + [resolution-v1](review-v1/resolution-v1.md), v2 in
[review-v2](review-v2/review-v2.md) + [resolution-v2](review-v2/resolution-v2.md).
History goes on the row's History cell, newest first, `;`-separated. Rows first seen
`audit` come from the accepted fix-round audit of 2026-08-27 (owner-accepted, no pass
folder — the audit examined the 038-039 v12/v15 rounds).

## Findings

| ID | Sev | First seen | Title | Status | History |
|---|---|---|---|---|---|
| SF1 | 🟠 | v1 | The undeviated certification path has never completed | fixed | v1: fixed `08e5a3e` |
| SF2 | 🟠 | v1 | README oversells what "certified" means | fixed | v1: fixed `08e5a3e` |
| SF3 | 🟡 | v1 | Stale HINTS in the discovery script | fixed | v1: fixed `08e5a3e` |
| SF4 | 🟠 | v1 | Evidence chain single-machine, no pushed refs | fixed | v1: fixed (3 tags pushed) |
| SF5 | 🟡 | v1 | Metrics drift off-schema, no validator | fixed | v1: fixed `1daedb9` |
| SF6 | 🟡 | v1 | Fix-generativity has no metric field | fixed | v1: fixed `1daedb9`; v2: residuals SF22 SF24 |
| SF7 | ⚪ | v1 | Per-feature cost understated (fix rounds unmetered) | fixed | v1: fixed (scope note) |
| SF8 | ⚪ | v1 | Stale re-raise numbers quoted as current | fixed | v1: fixed `08e5a3e` |
| SF9 | 🟡 | v1 | Blinding leak: 371 finding-id citations in source | fixed | v1: fixed `09173c4`; v2: residuals SF17 SF23 SF32 |
| SF10 | — | v1 | Severity single-judge at the stop rule's pivot | false-positive | v1: refuted by the defense checker |
| SF11 | — | v1 | "Human bottleneck is launching, not deciding" | false-positive | v1: refuted by the defense checker |
| SF12 | ⚪ | v1 | Hand-transcribed numbers drift between files | fixed | v1: fixed `1daedb9` |
| SF13 | ⚪ | v1 | No session-model check before launch | fixed | v1: fixed `d72b1aa` |
| SF14 | 🟠 | v1 | Certified under a stop rule whose gating experiment never ran | deferred | v3: deferral affirmed, rationale on file, re-asked in summary-v2; v2: re-found, quantified (no target ever reached zero new serious); v1: owner "not now" 2026-07-29 |
| SF15 | 🟡 | v1 | fixer==verifier waived at peak closure pressure | fixed | v1: fixed `08e5a3e` |
| SF16 | 🟠 | v1 | The post-cert escape counter has no working collector | verified | v3: verified (revert-and-rerun red→green, clean attribution); v2: REOPENED (regroup killed the auditor's track-record path), re-fixed `fa5eda0`; v1: fixed `878beb6` |
| SF17 | 🟠 | v2 | Runbook claims a git-history bar the lens prompt does not have | verified | v3: verified (lens + skeptic prompts and runbook read at `5245b81`); v2: fixed `a59faac` |
| SF18 | 🟠 | v2 | Duplicate-PPW mint has no working alarm in the parallel-worktree workflow | verified | v3: verified (revert-and-rerun; same-ledger and id-counter-floor arms hand-fired on a scratch root, both error and exit 1); v2: fixed `fa5eda0` + `a612416` |
| SF19 | 🟠 | v2 | Fix-round index-row rule unimplemented; three documents disagree | verified | v3: verified (README, skill, renderer read — one authority); v2: fixed `152de93` (fixer hand-writes the row) |
| SF20 | 🟠 | v2 | Renderer and auditor bucket `backlog` differently | verified | v3: verified (revert-and-rerun; skill status list names backlog); v2: fixed `ff3e64f` + `a612416` |
| SF21 | 🟠 | v2 | Router correction filter cross-matches on undefined | verified | v3: verified (revert-and-rerun — reverted router resurfaces round-99 correction); v2: fixed `866c843` |
| SF22 | 🟠 | v2 | Fix-generativity uncapturable where fix-caused defects surface | verified | v3: verified (revert-and-rerun on the malformed-lineage fixture; runbook sentence read); v2: fixed `87c9c75` (verification lineage entries) |
| SF23 | 🟡 | v2 | Citation scan blind to PPW/SF id shapes | verified | v3: verified (planted PPW-999 comment: invisible reverted, counted restored); v2: fixed `fa5eda0` |
| SF24 | 🟡 | v2 | Per-finding metrics layer written on 1 of 41 pass lines | deferred | v3: deferral affirmed, rationale on file; v2: needs 2–3 more discovery passes, not code |
| SF25 | 🟡 | v2 | System target exempt from the system's own machinery | verified | v3: verified (13 files tracked under reviews/system/, scope note read); v2: fixed `7fd924a` + this ledger, metrics line, contract scope note |
| SF26 | 🟡 | v2 | Link keeper does not rewrite a moved file's own links | verified | v3: verified (hand revert-and-rerun on a scratch move — no automated fixture, gap named SF36); v2: fixed `9c2bc31` |
| SF27 | 🟡 | v2 | Schema names the banned frontmatter map as data source | verified | v3: verified (schema names the Findings body table; stale sentence gone); v2: fixed `152de93` |
| SF28 | 🟡 | v2 | `closed:` key mandated by template, banned by contract | verified | v3: verified (contract lists `closed:`); v2: fixed `152de93` (legalized) |
| SF29 | 🟡 | v2 | Fixer skill self-contradicts on the rendered table; dormant writer | verified | v3: verified (no writeFileSync left in the renderer; skill prose agrees); v2: fixed `ff3e64f` + `152de93` + `a612416` |
| SF30 | 🟡 | v2 | Class sidecar uncontracted, at a banned location | verified | v3: verified (constant, miner stamp, spec and contract all name state/); v2: fixed `cc46bc7` (state/, constant, contract) |
| SF31 | 🟡 | v2 | Backlog drain converges on an unsized pre-deploy cliff | wont-fix | v3: ruling on file, cited backlog.md unchanged in the fix range; v2: owner ruling 2026-08-12 — stockpile until an owner-called sweep |
| SF32 | ⚪ | v2 | Leak scanner's own file carried a finding-id comment | verified | v3: verified (header comment gone at `5245b81`, present on revert); v2: fixed `fa5eda0` |
| SF33 | ⚪ | v2 | A verification line claims zero cost instead of unknown | verified | v3: verified (correction line read — pass 6, field cost, null rule stated); v2: fixed `222f765` (correction line) |
| SF34 | ⚪ | v2 | Index pass rows ragged; gate silent on cell count | verified | v3: verified (revert-and-rerun on the bad-state 6-cell row; contract states 5-or-7); v2: fixed `1528576` (5-or-7 rule + gate check) |
| SF35 | ⚪ | v2 | Stale prose inside the machinery (comments, runbook dates) | verified | v3: verified (30-min comment, Sonnet judge header, 2026-08-12 runbook dates, weak-spots bullets read); v2: fixed `152de93` + `ff3e64f` |
| SF36 | ⚪ | v3 | fix-links.mjs has no automated test — SF26's rewrite logic is verifiable only by hand | open | v3: named at verification of SF26 (fix-generated surface without a fixture) |
| SF37 | 🟠 | v4 | verify-fixes guards a dirty tree at startup but its `reset --hard` destroys any edit made while it runs | open | v4: hit during the 038-039 v13 verification — an index.md edit written mid-run vanished at the next restore point. The pre-flight check reads the tree once; nothing re-checks or warns, and the run holds the tree for the better part of an hour |
| SF38 | 🟡 | v4 | verify-fixes buffers every verdict until exit, so a long run is indistinguishable from a hung one | open | v4: `for (const r of results) console.log(...)` runs after the loop, so a 21-row run wrote nothing for 40+ minutes. A per-row emit would cost nothing and make the run observable |
| SF39 | 🟡 | v4 | A red leg that fails to build counts as red, so a fix touching a file later changed elsewhere verifies for the wrong reason | fixed | 2026-08-27: fixed `0b2cb00` (audit R6) — a red leg counts only when the runner output names a failing test (recorded in `red_evidence`); a compile error or an unattributable non-zero exit is the new verdict `revert-broke-build`; fixture-pinned; v4: reverting one file of a commit whose neighbours moved on can break compilation; the runner reads any non-zero exit as the test reddening. The tool already warns when HEAD is past `fixed_commit`, but the warning does not distinguish a build failure from a failing assertion |
| SF40 | 🟡 | v4 | discovery-review silently ignores an unknown arg key, so a `*Path` typo costs a whole pass its decided-findings list | open | v4: hit launching the 038-039 delta — `decidedFindingsPath` and `changedFilesPath` bound nothing, and the run would have re-reported 97 already-decided rows as new. The script already aborts when no diff binds; the same guard should reject arg keys it does not read |
| SF41 | 🟠 | audit | No stage owns the composed state of a fix round — per-finding fixes, each verified alone, composed into a double charge | fixed | 2026-08-27: fixed `6a76ad9` + `15759f8` + `e7951d2` (audit R1) — protocol-first clusters: a quantified protocol block precedes the cluster's code (`protocol-written`, order-checked by the auditor), the approach-check critiques the spec instead of the patches, and the cluster carries one invariant test over the composed flows |
| SF42 | 🟠 | audit | The trigger-list rule is enforced by the judgment of the agent it constrains — one round wrote "not needed" six times and nothing refused | fixed | 2026-08-27: fixed `15759f8` + `e7951d2` (audit R2) — the auditor refuses `resolved` when a trigger-classified fix has no consumed pre-check verdict and no `check-dispatched` event naming it; the scope table's free-text Approach-check column is retired, so "not needed" has no home |
| SF43 | 🟠 | audit | `COMMENTS_OK`/`DOCGATE_OK` route around the unattended policy's fail-closed design | fixed | 2026-08-27: fixed `42dff22` + `aaf53c0` (audit R2) — the pre-commit hook logs every override to the override log, and the policy answers `stop` at the next gate when one is logged after the run's start |
| SF44 | 🟠 | audit | Per-cluster micro-reviews each see one cluster; cross-cluster interactions are reviewed by nobody | fixed | 2026-08-27: fixed `6a76ad9` + `15759f8` (audit R3) — one round-scope composition review over the whole diff with a fixed brief (pairs sharing state/files/schedules, callers of changed transitions, readers of retired signals), event-gated at hand-back |
| SF45 | 🟠 | audit | The fixer authors and grades its own regression tests — three passed for reasons unrelated to their bug | fixed | 2026-08-27: fixed `6a76ad9` + `15759f8` (audit R4) — a test-meaning audit runs over the round's new tests (assert the literal, fresh-context reads, asynchronous fakes), and the fix brief's suggested test shape is the assertion spec the tests are checked against |
| SF46 | 🟠 | audit | Certification is reachable with owed lenses and an unmeasured seed rate, and no non-convergence verb exists, so the loop can be run indefinitely | fixed | 2026-08-27: fixed `aaf53c0` (audit R5 + convergence rule) — the router refuses row 6 while a manifest lens never ran (lens-coverage pass first) or no blind pass followed the last substantive fix round; `s(r)` is computed from `seed_round`/`area` lineage, missing means unmeasured; two consecutive rounds seeding one component at s ≥ 0.3 gate a design pass, one per component per loop |
| SF47 | 🟡 | audit | Full suites run at every round end, and the verification pass re-proves what the round already proved | fixed | 2026-08-27: fixed `6a76ad9` (audit, stop-doing) — the full suites run once per loop at the certification freeze; a round recording per-fix revert proofs gets an evidence audit (2–3 sampled re-runs, non-reproduction reverts the target to full re-runs) instead of wholesale re-proving |
