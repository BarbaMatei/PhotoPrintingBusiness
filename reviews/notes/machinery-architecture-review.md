---
type: design-note
status: draft for owner approval — phase 7a output, 2026-08-28
created: 2026-08-28
owner: Matei Barba
charter: machinery-restructure-plan.md
---

# Review machinery — architecture review (phase 7a)

## In plain words — read this first

This document is the stock-taking you asked for before we rebuild the machine's structure.
This section says everything in plain language; the numbered sections after it are the same
content in precise form, for the agents that will do the work. You only need this section
to answer the six questions at the end.

**What the machine is today.** Seven different jobs grew up inside one folder, all called
"the review machinery": finding problems (Review) · proving a fix really fixed something
(Verify) · doing the fixing (Fix) · deciding what happens next (Drive) · keeping the books
(Records) · measuring the process itself (Measure) · blocking bad commits (Enforce). Because
they share one name and one folder, nobody — human or agent — can see where one job ends and
the next begins.

**What we found when we took stock.**

- The same small routine is written in up to six places (24 cases counted). Fix one copy and
  the other copies stay wrong. This caused real bugs this month.
- Sixteen rules are written in more than one place — in the code and again in the guides.
  Three of the copies disagree with each other right now.
- Thirteen pieces of "tolerance" code exist only to accept old record formats that can never
  occur again. About ninety lines do nothing but tolerate history.
- Two tools we built this month are connected to nothing (nobody calls them), and one older
  tool reads a file that has never existed.
- The tests grew the same way: about forty-five test folders where six would do, and a
  couple of test blocks pasted in twice. The safety suite takes 2–3 minutes; it could run
  in under one.

**What we propose (the restructure itself, phase 7b).** Sort every piece into exactly one of
the seven jobs. Give each job its own folder, named for what it does. Move every routine and
every rule to exactly one home; the guides then quote the code instead of restating it — and
a new check fails the test suite whenever a guide and the code drift apart, which is the
disease that keeps causing rework. Delete what nothing uses. All of it in small steps, with
the full safety suite green after every step, so nothing hard-won is lost.

**The six decisions that are yours** (details in section 9; answer like "1: yes, 2: …"):

1. **The redesign brake.** When two fix rounds in a row keep causing new serious problems in
   the same area, the machine refuses a third patch and asks you for a redesign of that
   component. The guide used to claim this brake applies to every path into a fix round; the
   code applies it only on some paths. The guide has been corrected to match the code — the
   open question is whether the code itself should brake on every path. *Recommendation:
   decide after one real project run; the narrow brake has not misfired yet.*
2. **Old closed projects.** Keep re-auditing their bookkeeping forever, or only keep the
   check that no defect number is ever reused? *Recommendation: keep only the number check —
   the closed books never change, and dropping the rest deletes most of the tolerance code.*
3. **The lesson-mining tool** was built, never fed, never run. Delete it and rebuild when the
   work is actually scheduled? *Recommendation: delete — the idea itself stays written down.*
4. **The AI proofreader** that checks wording currently also reviews internal notes you never
   read. Limit it to the pages written for your eyes? *Recommendation: wait for one real run
   to see what it actually catches elsewhere, then cut.*
5. **The comment scan** that counts review-ticket numbers left in code comments has found
   zero for a month. Move it into the commit gate, or delete it? *Recommendation: move it —
   it costs nothing there.*
6. **Two stale test folders** for the skills point at files that no longer exist. Delete them
   after copying their last results into the notes? *Recommendation: yes.*

Standing recommendation on top of all six: run **one real feature** through the rebuilt loop
before the deletions become final — the run shows which gates fire and what is truly dead,
so we cut on evidence instead of reading.

---

Read-only inventory of everything under `reviews/`, the four review skills and the hook, at
branch tip `ee828e6` (loop-speed phases 1–5 landed). Every piece gets one of the seven systems (Review · Verify · Fix · Drive ·
Records · Measure · Enforce) and a verdict. The suite measured today: 565 assertions in 13 files,
all green, 2–3 minutes wall (the handoff's figure; not re-timed here). No real target run has happened since phase 6 (038-039 closed
2026-08-27), so every verdict that needs live-run data is marked `provisional`.

## 1. Dependency map

Imports between modules (`a → b` = a imports b):

| Module | Imports | Reads | Writes |
|---|---|---|---|
| `vocab.mjs` | — | — | — |
| `paths.mjs` | — | — | — |
| `ledger.mjs` | — | `ledger.md` rows, `resolution-v*.md` frontmatter | — |
| `wl.mjs` | — | `worklog.jsonl`, `resolution-v<n>.md` (exists check) | `worklog.jsonl` |
| `route-next-pass.mjs` | ledger, vocab | ledger, `metrics.jsonl`, resolution fm, review fm, file list | stdout only |
| `autonomy-policy.mjs` | ledger, vocab | ledger, metrics, review fm, resolution rows, `worklog.jsonl` (`run-start`), `state/overrides.jsonl` | stdout only |
| `records-auditor.mjs` | paths, vocab | metrics, worklog, resolutions, reviews, ledger, `state/index.md`, `state/track-record.md`, `state/id-counter`, git | stdout only |
| `render-records.mjs` | paths | worklog, resolution, review fm, ledger | `metrics.jsonl`, `state/index.md`, `ledger.md` |
| `doc-gate.mjs` | paths, vocab | review/summary/resolution/ledger, `state/backlog.md`, `state/index.md`, git HEAD ledger, git cat-file | stdout only |
| `verify-fixes.mjs` | wl | resolution rows + fm, git | worklog (`verify-result`), working tree (revert/restore) |
| `run-scoped-tests.mjs` | wl | — | worklog (`test-run`), `<tmp>/photoprint-test.lock` |
| `mint-id.mjs` | paths | `state/id-counter`, `templates/ledger.md`, `templates/resolution.md`, review rows | `id-counter`, `ledger.md`, `resolution-v<n>.md` |
| `speed-report.mjs` | — | worklog, metrics | stdout only |
| `summary-data.mjs` | vocab | metrics | stdout only |
| `gate-miner.mjs` | — | every worklog (live + archive) | stdout only |
| `ledger-miner.mjs` | paths | `state/defect-classes.jsonl` (absent) | `memory-bank/standards/definition-of-done.md` |
| `fix-links.mjs` | paths | every `.md` under reviews/, skills, hook | the same files (`--apply`) |
| `discovery-review.wf.js` | — (Workflow sandbox) | args only | returns findings |
| `tests/lib.mjs` | paths | fixtures | — |

Who invokes which script (prose → code):

| Caller | Invokes |
|---|---|
| `loop-driver` skill | records-auditor ×3, route-next-pass, autonomy-policy, wl (`pass-launch`, `pass-records-done`, `gate-*`, `run-*`), verify-fixes, render-records ×2, doc-gate, Sonnet judge (Agent) |
| `fix-review` skill | wl (12 event kinds), run-scoped-tests; explicitly forbids render/auditor/doc-gate |
| `runbook-discovery.md` | discovery-review.wf.js (Workflow), reconcile-findings skill, owner-summary skill, records-auditor |
| `runbook-verification.md` | verify-fixes, render-records `--verification`, records-auditor |
| `owner-summary` skill | none (summary-data exists for it but the skill never names it) |
| `reconcile-findings` skill | none (mint-id exists for it but the skill hand-edits `id-counter`) |
| `.githooks/pre-commit` | doc-gate (target + `state`), `tests/run-tests.mjs`, appends `state/overrides.jsonl` |
| nobody | speed-report, gate-miner, ledger-miner, fix-links, summary-data, mint-id |

Prose that restates a code rule: see section 4.

## 2. Inventory with verdicts

Confidence: H = evidence in this file, M = evidence but a live run could change it, L = judgment.

### Scripts (18)

| Piece | System | Verdict | Evidence | Conf |
|---|---|---|---|---|
| `vocab.mjs` (16 lines) | Records | keep — grows into `records/schema.mjs` | already the shared authority for lenses, areas, V4 cut-off; `doc-gate.mjs:31` still hardcodes AREAS instead of importing it | H |
| `paths.mjs` | Records | merge into `records/schema.mjs` (repo-root object) | 10 of 17 exports have zero importers (`README`, `CONTRACTS`, `METRICS_SCHEMA`, both `RUNBOOK_*`, `RATIONALE`, `LOOP_DESIGN`, `ID_MAP`, `ARCHIVE`, `targetDir`); paths are absolute to the live repo, so 4 scripts re-base them for `--root` (`records-auditor.mjs:488`, `doc-gate.mjs:49`, `ledger-miner.mjs:110`, `render-records.mjs:133`) | H |
| `ledger.mjs` | Records (reader) + Drive (`standsDown`) — **two systems** | split: `readLedger`/`openIds` → `records/ledger.mjs`; `standsDown` → `model/` | `ledger.mjs:28-39` is a routing rule, not a reader | H |
| `wl.mjs` | Records | keep | the one writer; stamper rules pinned by 83 assertions | H |
| `route-next-pass.mjs` | Drive | keep; move state derivation to `model/` | lines 99-136 and 167-219 compute state the policy recomputes (`autonomy-policy.mjs:39-65, 91-98`) | H |
| `autonomy-policy.mjs` | Drive | keep; consume `model/` | duplicates router logic verbatim (`certificationBlocker` = router 121-128 + 238-239) | H |
| `records-auditor.mjs` (550 lines) | Records (validator) + Fix (hand-back gates 402-484) + Enforce (citation scan 517-544) — **three systems** | split: schema validation → `records/validate.mjs`; R1-R4 hand-back gates → `fix/handback-gates.mjs`; citation scan → `enforce/` (or delete, see 7) | the file header already lists three unrelated jobs | H |
| `render-records.mjs` (440) | Records | keep; import span pairing from `model/spans.mjs` | `loadEvents` 82-97 and `pairSpans` 102-122 are re-implemented in speed-report 66-76, 96-123 | H |
| `doc-gate.mjs` (368) | Records (lint) | keep; import vocab/regexes from `records/` | own AREAS (31), own STATUSES (276), own frontmatter parser (141), own ledger-row and block regexes (279, 284) | H |
| `verify-fixes.mjs` | Verify | keep | only mechanical verifier; 7 + 26 assertions | H |
| `run-scoped-tests.mjs` | Fix | keep | wrapper the fixer contract mandates; 32 assertions | H |
| `mint-id.mjs` | Review (reconciliation scaffolds) | keep; wire into `reconcile-findings` | 42 assertions; no skill or runbook invokes it — the reconciler still edits `id-counter` by hand (`reconcile-findings/SKILL.md:131-133`) | H |
| `speed-report.mjs` | Measure | keep; consume `model/spans.mjs` | 82 assertions pin the baseline; duplicates renderer's event loading and gate-miner's disapproval rule | H |
| `summary-data.mjs` | Review | keep; wire into `owner-summary` | 35 assertions; `owner-summary/SKILL.md` never names it, so the skill re-derives "Reasons to doubt" by hand | H |
| `gate-miner.mjs` | Measure | merge into `speed-report.mjs` as `--disapprovals` | same input, same `isDisapproval` rule (`gate-miner.mjs:44` = `speed-report.mjs:170`); 27 assertions move with it | M |
| `ledger-miner.mjs` | Measure | delete (owner ruling, section 7) | its only input `reviews/state/defect-classes.jsonl` does not exist; no test, no invoker; last touched in the bolt-044 merge `c09675d` | H |
| `fix-links.mjs` | Records (docs) | merge check half into `docs-sync`; delete `--apply` + `MOVES` | `MOVES` (10-15) lists four moves completed 2026-08-11; no test | H |
| `discovery-review.wf.js` | Review | keep; import lens keys from `records/schema` | `LENS_LIBRARY` keys (139-196) = `MANIFEST_LENSES` by hand; header comment cites a README "Cost discipline" section that no longer exists (line 35) | H |

### Tests (15 files, 565 assertions)

| Piece | System | Verdict | Evidence | Conf |
|---|---|---|---|---|
| `run-tests.mjs`, `lib.mjs` | — (harness) | keep; `scrubbedGitEnv` moves to a shared module | `lib.mjs:32-42` duplicates `verify-fixes.mjs:54-58` | H |
| `integration.test.mjs` (112) | mixed | split by flow: router-rows (24 assertions, lines 11-124) → `drive`; renderer index/ledger/verification (128-402) → `records`; policy sweep (404-454) → `drive`; verify-fixes throwaway repo (456-616) → `verify`; hook override (618-643) → `enforce` | the file is five unrelated suites under one name | H |
| `render-records.test.mjs` (42) | Records | keep; delete the duplicate block | lines 15-85 and 280-356 are the same "frontmatter and worklog edge shapes" block twice; the second adds one round-review assertion | H |
| `records-auditor.test.mjs` (21) | Records + Fix | split with the auditor; delete duplicates | lines 19-23 and 116-120 repeat three identical `check`s; lines 13-18 run against live archive targets (`044-045`, `043`) — a real-repo dependency | H |
| `route-next-pass.test.mjs` (22), `autonomy-policy.test.mjs` (15) | Drive | keep, merge into one `drive` flow file | both drive the same fixture targets | M |
| `doc-gate.test.mjs` (45) | Records | keep | planted-violation list is the lint's spec | H |
| `wl.test.mjs` (83) | Records | keep | stamper contract | H |
| `mint-id.test.mjs` (42) | Review | keep | scaffold contract; reads real templates | H |
| `speed-report.test.mjs` (82) | Measure | keep | frozen baseline; the only numbers the acceptance criteria are judged on | H |
| `summary-data.test.mjs` (35) | Review | keep | — | H |
| `gate-miner.test.mjs` (27) | Measure | merge with gate-miner | — | M |
| `verify-fixes.test.mjs` (7) | Verify | keep; absorb the 26 verify assertions from integration | the file header says the rest "lives in integration.test.mjs" | H |
| `run-scoped-tests.test.mjs` (32) | Fix | keep | — | H |

### Fixture targets (40 dirs under `fixtures/repo/reviews`, 2 under `bad-state`, 1 `speed-report`)

| Group | Pins | Verdict | Evidence | Conf |
|---|---|---|---|---|
| `901-good-target`, `902-broken-target`, `bad-state/*` | doc-gate clean/violations, state lint | keep as `records-good` / `records-broken` | 902 still carries a retired `findings-v1.md` on purpose (`doc-gate.mjs:186`) | H |
| `903-closed`, `904-clean-verification`, `909-certified`, `913-loop-quiet`, `914-resolution-above-review` | router rows 1-6 | merge into one `drive-states` fixture with several metrics tails | each is 1-3 files differing in one line | M |
| `910-delta-worthy`, `911-patch-grade`, `912-recert`, `952-*` | policy delta-worthiness | merge into `drive-states` | same shape | M |
| `915-queued-mediums`, `916-medium-batch`, `917-sweep-before-cert`, `918-open-blocker`, `919-reopened-latest`, `941-949` (7 dirs) | ledger-driven rows, queue, regression lineage, loop-close | merge into `drive-ledger` (one ledger, metrics tails per case) | 13 dirs for one rule family | M |
| `915-lens-debt`, `916-unmeasured-seed`, `917-non-convergent`, `918-design-capped` | convergence rule | keep as `drive-convergence` | numbers collide with the queue fixtures (two 915s, 916s, 917s, 918s, 919s, 952s) — rename | H |
| `905-dup-ledger`, `907-correction`, `908-verification-lineage` | auditor id uniqueness, corrections, lineage shape | fold into `records-good`/`records-broken` | 905 is never named by a test; it exists only to trip `duplicate id PPW-9001` | H |
| `921-gates-bad`, `922-gates-good`, `923-newshape`, `924-oldshape` | V4 hand-back gates and resolution shape | keep as `fix-handback` | the only fixtures for the audit R1-R4 rules | H |
| `919-override-clean`, `919-override-stop` | policy override stop | fold into `drive-states` | two one-line worklogs | H |
| `fixtures/speed-report/*` | acceptance baseline | keep, frozen | verbatim copy of the live 038 records (483 + 57 lines, identical to `reviews/038-039-invoicing/*`) | H |
| inline `mkdtemp` fixtures (920, 921-spans, 938, 950, 960-975, 999…) | renderer spans, verify repo, wrapper, summary-data | keep as code | they are the tests' own arrange step | H |

### Rules, docs, runbooks, templates

| Piece | System | Verdict | Evidence | Conf |
|---|---|---|---|---|
| `README.md` | Drive (router, re-arm, tiers) + Review (severity, verdicts) + Records (files) | keep; router table and vocabulary lists become generated (section 4) | 17-row router table restates `route-next-pass.mjs`; one row disagrees (section 4) | H |
| `rules/doc-contracts.md` | Records | keep; vocabulary list, area table, event list generated | 333 lines; lines 22-30 still say archives "are being retrofitted" (done 2026-08-11); line 175 defines the class sidecar that does not exist | H |
| `rules/metrics-schema.md` | Records + Measure | keep; field tables generated from validator key sets | `TOP_KEYS`/`FIX_KEYS`/`RUNTIME_KEYS` in the auditor are the same tables by hand | H |
| `runbooks/runbook-discovery.md` | Review | keep | lens table names "Per-provider symmetry" and "Accessibility / UX" — neither is a `LENS_LIBRARY` key | H |
| `runbooks/runbook-verification.md` | Verify | keep | short, one home | H |
| `templates/*.md` (5) | Records | keep | read at runtime by mint-id; the doc gate checks against them | H |
| `notes/rationale.md` | Measure | keep | evidence, not rules | H |
| `notes/self-driving-loop-design.md` | Measure (research) | keep; refresh stale numbers | line 206 says "36-assertion fixture suite"; line 208 says the miner is built and unfed | M |
| `notes/loop-speed-plan.md`, `loop-speed-handoff.md` | Measure (history) | keep as history; no rules live here | — | H |
| `CLAUDE.md` map row → README | Enforce | keep | one line | H |

### Skills

| Piece | System | Verdict | Evidence | Conf |
|---|---|---|---|---|
| `loop-driver/SKILL.md` (306) | Drive | keep; strip restated rules to citations | restates the queue rule (46-51), the close sequence (266-281), the unit sequence (124-151) | H |
| `loop-driver/evals/evals.json` | Drive | provisional delete | references a fixture root `scratchpad/router-fixture` that no longer exists; last status 2026-07-30 | M |
| `fix-review/SKILL.md` (408) | Fix | keep; event table (102-112) generated from `wl.mjs` | the "sole owner of the fixer contract" — right home | H |
| `fix-review/evals/*.json` | Fix | provisional delete | same staleness as above | M |
| `owner-summary/SKILL.md` | Review | keep; cite `summary-data.mjs` | never names the script that computes its section 2 | H |
| `reconcile-findings/SKILL.md` + `overlap-ground-truth.md` | Review | keep; cite `mint-id.mjs` | the ground truth is the reconciler's trust gate | H |

### State, records, enforcement

| Piece | System | Verdict | Evidence | Conf |
|---|---|---|---|---|
| `state/index.md` | Records | keep | renderer writes Passes rows; doc-gate lints | H |
| `state/backlog.md` (137 rows) | Records | keep as markdown (owner ruling, section 7) | consumed by `bolt-process.md:42` sweep; 25-line prose preamble carries history | M |
| `state/id-counter` | Records | keep | one number | H |
| `state/track-record.md` | Measure | keep | read by the auditor for certified targets | H |
| `state/overrides.jsonl` (untracked) | Enforce writes, Drive reads | keep | 3 entries today, all `DOCGATE_OK` from the stale-hook workaround | H |
| per-target set (`review/resolution/summary-v<n>.md`, `ledger.md`, `worklog.jsonl`, `metrics.jsonl`) | Records | keep | — | H |
| `reviews/system/*` | Measure | keep; stays outside every gate | excluded by all four `TARGETLESS` lists; `review-v1/resolution-v1.md` is the last frontmatter-map resolution | H |
| `archive/*` (5 targets) | Records | keep; validation scope is an owner ruling (section 7) | archive metrics still carry 15 legacy-field lines and `D<n>` ids (044-045) | H |
| `archive/id-map.md` (479 lines) | Records | keep | referenced from 6 files; the only translation of pre-2026-08-11 names | H |
| `archive/experiments/*` | Measure | keep | evidence for rationale.md | H |
| `.githooks/pre-commit` | Enforce | keep; comment gate is repo policy, not machinery | runs doc-gate, the suite, logs overrides | H |

Totals: 71 pieces · keep 52 · merge/split 14 · delete 5 (2 firm: `ledger-miner.mjs`, the `fix-links --apply` half; 3 provisional: two eval folders, the duplicate test blocks counted once).

## 3. Duplication ledger

| Logic | Copies (file:line) | Proposed home |
|---|---|---|
| Read `metrics.jsonl`, drop corrections | `route-next-pass.mjs:105-106` · `autonomy-policy.mjs:39-45` and again `91-98` · `summary-data.mjs:34-35` · `render-records.mjs:198-202` · `speed-report.mjs:223-227` · `records-auditor.mjs:132-165` | `records/metrics.mjs` `readMetrics(dir) → {lines, corrections}` |
| Load worklog + void filter | `wl.mjs:57-84` (deepEqual) · `render-records.mjs:82-97` (JSON.stringify) · `speed-report.mjs:66-76` (copy) · no void filter at all in `records-auditor.mjs:363`, `gate-miner.mjs:37-42`, `autonomy-policy.mjs:73-78` | `records/worklog.mjs` `readEvents(dir) → live events`; one equality rule |
| Pair spans (round-start/end, pass-launch/done) | `render-records.mjs:102-122` (strict, aborts) · `speed-report.mjs:96-123` (lenient, resumes) | `model/spans.mjs` with a `strict` flag; both behaviours pinned |
| Ledger row parse | `ledger.mjs:10` · `doc-gate.mjs:284` · `records-auditor.mjs:442, 501` | `records/ledger.mjs` |
| Ledger detail-block parse | `doc-gate.mjs:279` · `records-auditor.mjs:440` | `records/ledger.mjs` |
| Resolution Findings rows | `render-records.mjs:245-250` · `records-auditor.mjs:104-108` · `verify-fixes.mjs:69-71` · `autonomy-policy.mjs:147` · `records-auditor.mjs:430` · `doc-gate.mjs:221-231` | `records/resolution.mjs` |
| Frontmatter parse (3 different implementations) | `indexOf('\n---', 3)`: `records-auditor.mjs:89, 100, 413`, `render-records.mjs:241` · `/^---\r?\n…/`: `ledger.mjs:36`, `route-next-pass.mjs:64`, `autonomy-policy.mjs:138`, `verify-fixes.mjs:75` · CRLF-normalised: `doc-gate.mjs:141` | `records/frontmatter.mjs` |
| `## Findings` section split | `doc-gate.mjs:179, 221`, `mint-id.mjs:168`, `records-auditor.mjs:104`, `render-records.mjs:245` (+ Decisions/Scope at `doc-gate.mjs:237, 248`) | `records/frontmatter.mjs` `section(body, name)` |
| Newest `resolution-v<n>` / `review-v<n>` | resolution: `autonomy-policy.mjs:132`, `ledger.mjs:34`, `records-auditor.mjs:351, 409`, `render-records.mjs:233, 431`, `route-next-pass.mjs:167`, `verify-fixes.mjs:62` · review: `ledger.mjs:35`, `records-auditor.mjs:124`, `route-next-pass.mjs:99` | `model/target.mjs` `versions(dir)` |
| Targetless folder list | `records-auditor.mjs:70` · `route-next-pass.mjs:49` · `doc-gate.mjs:32` · `gate-miner.mjs:18` | `records/schema.mjs` |
| Target folder lookup (live then archive) | `wl.mjs:137-138` · `autonomy-policy.mjs:33` · `verify-fixes.mjs:48` · `doc-gate.mjs:133-134` · `summary-data.mjs:30` · `route-next-pass.mjs:44-61` · unused `paths.mjs:44-48` | `model/target.mjs` |
| Repo root + `--root` parsing | 11 scripts derive root from `import.meta.url` (`autonomy-policy.mjs:28` … `wl.mjs:194`); 10 hand-parse `--root` | `cli/args.mjs` |
| Convergence rule (lens debt, seed rate) | `route-next-pass.mjs:120-136, 238-239` · `autonomy-policy.mjs:49-65` | `model/convergence.mjs` |
| Stands-down / open-work rule | `ledger.mjs:28-39` · re-derived at `route-next-pass.mjs:185-219` · `autonomy-policy.mjs:104-112` | `model/open-work.mjs` |
| Disapproval detection | `gate-miner.mjs:44` · `speed-report.mjs:170-171` | `measure/gates.mjs` |
| Pluralize | `render-records.mjs:60` · `route-next-pass.mjs:69` · `summary-data.mjs:16` | `cli/text.mjs` |
| Git-env scrub | `verify-fixes.mjs:54-58` · `tests/lib.mjs:32-42` | `verify/git.mjs` |
| Sha regex | `records-auditor.mjs:64, 337, 431` · `render-records.mjs:252, 327, 346` · `verify-fixes.mjs:70` · `doc-gate.mjs:346` | `records/schema.mjs` |
| Severity emoji set | `doc-gate.mjs:28` · `mint-id.mjs:23` · `ledger.mjs:10` · `records-auditor.mjs:443` | `records/schema.mjs` |
| Status vocabulary | `ledger.mjs:8-9` · `doc-gate.mjs:220, 276` · `render-records.mjs:262` · `records-auditor.mjs:111-117` | `records/schema.mjs` |
| Areas | `vocab.mjs:11` · `doc-gate.mjs:31` | `records/schema.mjs` (doc-gate imports) |
| Lens keys | `vocab.mjs:5` · `discovery-review.wf.js:78, 139-196` | `records/schema.mjs`; the workflow imports or is generated from it |
| CRLF-preserving insert | `render-records.mjs:136-137, 177` · `mint-id.mjs:97-98` | `records/text.mjs` |
| Duplicate test blocks | `render-records.test.mjs:15-85 = 280-356` · `records-auditor.test.mjs:19-23 = 116-120` | delete the first copy of each |

24 duplicate blocks; the plan named 3.

## 4. Rules ledger

| Rule | Copies | Agree? | Single home → prose cites |
|---|---|---|---|
| Router table (state → next pass) | `route-next-pass.mjs` · `README.md:79-97` (17 rows) · `loop-driver/SKILL.md:85-92` | **No.** README row 86 says the design pass "intercepts any fix-round answer above"; the code reaches `routeFixRound` only from rows at 234-235, 250, 254 — the armed row (203), the batch row (207) and the sweep row (259) call `finish` directly and skip the design-pass check | `drive/rows.mjs` exports the row list with `state`/`next`/`gate` text; README table generated |
| Queue threshold 3 | `route-next-pass.mjs:42` · `README.md:84-85, 101` · `doc-contracts.md:129` · `loop-driver:46` (no number) | yes | constant in `drive/rows.mjs`; prose cites `QUEUE_THRESHOLD` |
| Convergence rule (seed rate ≥ 0.3, lens debt, one design pass) | `route-next-pass.mjs:137-165` · `autonomy-policy.mjs:52-65` · `README.md:126-140` · `doc-contracts.md:169-171` · `metrics-schema.md:187-199` | yes today; three prose copies | `model/convergence.mjs`; README note ³ keeps the why, cites the module for the numbers |
| Re-arm triggers (🔴, fix-caused 🟠, reopened) | `route-next-pass.mjs:194-204` · `README.md:83, 142-144` · `rationale.md:132-134` | yes | `drive/rows.mjs` |
| Gate kinds | router emits 7 (`loop-close`, `delta-worthiness`, `certification-go-ahead`, `design-pass`, `no-metrics`, `records-broken`, `no-row-matched`); policy handles 4 + fail-closed; loop-driver names 2; README names 1 | yes (fail-closed covers the rest) | `drive/gates.mjs` enum; skill table generated |
| Policy answer vocabulary | `autonomy-policy.mjs:12-14` · `loop-driver:226-231` · `README.md:44-46` | yes | `drive/gates.mjs` |
| Event vocabulary + required fields | `wl.mjs:21-51` (25 events) · `doc-contracts.md:248-275` · `metrics-schema.md:103-124` · `fix-review:102-112` (12 rows) · `loop-driver:113-116` | **Partly.** `metrics-schema.md:105-108` still lists `micro-review-*` as the fixer's events (retired 2026-08-28); the live 038 worklog carries `supplemental-check` and `loop-closed`, which `wl.mjs` refuses and the auditor never checks (`records-auditor.mjs:365` checks `t` and `ev` only) | `records/schema.mjs` `EVENTS`; the three tables generated; the auditor validates `ev` against it |
| Status vocabulary + terminal set | `ledger.mjs:8-9` · `doc-gate.mjs:220, 276` · `render-records.mjs:262` · `records-auditor.mjs:111-117` · `doc-contracts.md:233-234` · `fix-review:341-342` | yes (`backlog` → `deferred` bucket agreed since SF20) | `records/schema.mjs` |
| Twelve areas | `vocab.mjs:11` · `doc-gate.mjs:31` · `doc-contracts.md:287-302` | yes | `records/schema.mjs`; doc-contracts table generated (the "Covers" column stays prose in the schema object) |
| Lens manifest | `vocab.mjs:5-9` · `discovery-review.wf.js:78, 139-196` · `runbook-discovery.md:45-66` | **No.** runbook rows "Per-provider / per-entry-point symmetry" and "Accessibility / UX" have no key; `frontend-ux`, `db-parity`, `input-validation`, `observability`, `race` appear only as prose | `records/schema.mjs` `LENSES` with key + question; runbook table generated |
| Metrics field lists (v2/v3/v4) | `records-auditor.mjs:33-42` · `metrics-schema.md:36-64, 132-144` | yes | validator key sets; schema tables generated from them |
| Size caps (60/120/200/240/15/20/50/5) | `doc-gate.mjs:178, 201, 229, 236, 241, 293, 101, 115` · `render-records.mjs:68` · `doc-contracts.md:106-114, 323-329` | yes | `records/schema.mjs` `CAPS`; doc-contracts table generated |
| Grandfathering cut-offs | `records-auditor.mjs:31-32` (V2, V3) · `vocab.mjs:16` (V4) · `doc-contracts.md:79-86` · `metrics-schema.md:36, 100, 189` | yes | `records/schema.mjs` `CUTOFFS` |
| Reviewed-unit sequence (round → verify → render → audit → one gate) | `README.md:99-104` · `loop-driver:124-151` · `fix-review:355-373` · `doc-contracts.md:41-42` · `runbook-verification.md:1-5, 211-233` | yes | stays prose; one home = `loop-driver`; the others cite it |
| Close sequence (4 steps) | `doc-contracts.md:48-51` · `loop-driver:266-281` · `self-driving:163-166` | yes | one home = `doc-contracts.md` until a close script exists |
| Cost estimates | `route-next-pass.mjs:32-40` only | — | already one home |
| Judge scope and one-sitting rule | `doc-contracts.md:11-16` · `loop-driver:170-186` | yes | one home = `doc-contracts.md` |

16 rules with more than one home; 3 disagree today.

`docs-sync` mechanism: a `cli/docs-sync.mjs` that renders each generated table from the
`records/schema.mjs` and `drive/rows.mjs` objects into the prose file between
`<!-- generated:<name> -->` markers (the pattern `ledger-miner.mjs:146` already uses), and a
test that fails when the rendered text differs from the file. `fix-links.mjs`'s check half
runs inside it. Generated: README router table, README policy vocabulary, doc-contracts
vocabulary/area/cap/event lists, metrics-schema field tables, runbook lens table, fix-review
event table, loop-driver gate-kind table.

## 5. Legacy ledger

| Path | Tolerates | Still needed by | Verdict |
|---|---|---|---|
| V2 cut-off `2026-07-30` (`records-auditor.mjs:31, 216-263`): lenient pass lines, `LEGACY_TOP` fields, `certification` type, prose lenses, `frontend_*`, `subagent_tokens` | pre-v2 metrics lines | archive only: 015 (1 line), 042 (7), 043 (1) — no live target | keep while archives are validated; delete with ruling 3 |
| V3 cut-off `2026-08-03` (`records-auditor.mjs:32, 167, 267, 355`): fix-round lines optional before it, `runtime` refused before it | pre-v3 targets | archive 035/042/043/015 resolutions with no fix-round line | same |
| V4 cut-off `2026-08-28` (`vocab.mjs:16`; `records-auditor.mjs:419`, `doc-gate.mjs:247`) | all 11 resolved 038 rounds (`closed:` 08-13 … 08-27), all with the retired Approach-check column | the live 038 target (closed) and every archive | keep — it is the one cut-off with live records behind it |
| `D<n>` ids in `findings[].d` (`records-auditor.mjs:289, 292, 310, 316`) | pre-2026-08-11 lines | archive 044-045 only (strict-era dates, so the allowance is exercised) | keep while archives are validated |
| Frontmatter findings map on resolutions (`render-records.mjs:250-256`, `records-auditor.mjs:107-109`, `mapRe`) | old resolution shape | only `system/review-v1/resolution-v1.md`, and `system` is excluded from every gate (`TARGETLESS`) | **delete** — no audited or rendered record can hit it |
| `D\d+` alternative in resolution row regexes (`render-records.mjs:247`, `records-auditor.mjs:106`) | pre-PPW resolution rows | none: every live and archived resolution row is `PPW-<n>` (0 `| D` rows across 7 ledgers) | **delete** |
| `micro-review-*` in `wl.mjs:24, 41-42` (writer) and `render-records.mjs:322-323` (reader) | pre-2026-08-28 rounds | reader: 48 events in the live 038 worklog; writer: nothing should stamp them now | split: keep the reader, drop the two events from the stamper's vocabulary |
| Archive grandfathering `strictTier = warn` (`records-auditor.mjs:123`) | any shape error in `archive/*` | every archived target | owner ruling 3 |
| `id-map.md` | pre-2026-08-11 names in append-only records | 044-045 metrics, 035 ground-truth file, rationale.md | keep |
| `LEGACY_TOP`, `deferrals_upheld` alias, `disputed` verdict (`records-auditor.mjs:36, 39`; `metrics-schema.md:57-58`) | 042/015 lines; pre-trace-first verdicts | archive only | keep while archives are validated |
| `fix-links.mjs MOVES` (10-15) | four 2026-08-11 file moves | nothing — the moves are done | **delete** |
| doc-contracts `class sidecar` entry (175-176); `paths.mjs DEFECT_CLASSES` | a file that never existed | `ledger-miner.mjs` only | delete with the miner |
| `paths.mjs targetDir` and the 9 unused path exports | — | nobody | **delete** |
| `discovery-review.wf.js:35` "README Cost discipline" reference; `self-driving:206` "36-assertion" | old prose | nobody | fix in 7c |

13 legacy paths: 5 firm deletes, 6 tied to ruling 3, 2 keep.

## 6. Target architecture

The plan's sketch stands with three changes: (a) `records/` is split into `records/` (readers,
writers, validator, schema) and `model/` as sketched, but `model/` is smaller than the plan
implies — six derived facts, not a framework; (b) the auditor's hand-back gates move to `fix/`,
not `records/`, because they enforce the fixer contract; (c) `enforce/` is not a folder under
`reviews/lib/` — the hook stays in `.githooks/` and calls `cli/` entry points.

```
reviews/lib/
  records/  schema.mjs      events + required fields, statuses, severities, areas, lenses,
                            caps, cut-offs, targetless folders, sha regex (vocab.mjs grows into it)
            frontmatter.mjs one parser, one section splitter
            worklog.mjs     readEvents (void-filtered, one equality rule) · appendEvent (from wl.mjs)
            metrics.mjs     readMetrics → {lines, corrections} · appendLine · alreadyHas
            ledger.mjs      rows, blocks, flipRow
            resolution.mjs  frontmatter scalars, Findings rows, tallies
            validate.mjs    the auditor's schema half (v2/v3/v4 key sets, cut-offs)
            lint.mjs        doc-gate's per-file checks
            render.mjs      render-records' line builders + index/ledger writes
  model/    target.mjs      resolve dir (live/archive), versions, newest resolution/review
            spans.mjs       pairSpans(strict|lenient), sliceSpans, runtime split
            open-work.mjs   readLedger + openIds + standsDown + atLoopClose
            convergence.mjs lens union, owed lenses, seed rates, substantive rounds, design-pass cap
            unit.mjs        the reviewed unit: round + its verification, findings tallies
            queue.mjs       queued mediums vs batch vs sweep
  review/   discovery-review.wf.js · summary-data.mjs · mint-id.mjs (scaffolds)
  verify/   verify-fixes.mjs · git.mjs (env scrub, shared with tests)
  fix/      run-scoped-tests.mjs · handback-gates.mjs (auditor R1-R4)
  drive/    rows.mjs (router rows as data) · gates.mjs (gate kinds, policy vocabulary) ·
            route.mjs · policy.mjs · cost.mjs
  measure/  speed-report.mjs (+ disapproval listing) · track-record reader
  cli/      one file per command name; args.mjs (--root, --dry-run) · text.mjs (pluralize)
  tests/    unit/<module>.test.mjs · flows/{handback,verification,certification,close,unattended}.test.mjs
            fixtures/{records-good,records-broken,drive-states,drive-ledger,drive-convergence,fix-handback}
            fixtures/speed-report (frozen 038 baseline)
```

What `model/` computes once and who consumes it: `target` (every CLI) · `spans` (render,
speed-report) · `open-work` (route, policy, close step) · `convergence` (route, policy,
summary-data) · `unit` (render, auditor gates, speed-report per-round) · `queue` (route, policy,
loop-driver's sweep stall detector). Every consumer today recomputes at least one of these.

Command names stay stable: `reviews/lib/<name>.mjs` becomes a two-line shim under
`reviews/lib/cli/` and the old path is kept as a re-export for one release, so every skill,
runbook and hook line keeps working during 7b; 7c deletes the shims and runs `fix-links`.

Tests: unit per `records/` and `model/` module (in-process imports, no child process); flow tests
spawn the CLIs over the six fixtures; `speed-report` keeps its frozen baseline. Target: under
one minute. Today every assertion goes through `spawnSync` (`tests/lib.mjs:25-29`) — roughly
500 process launches per run; the in-process unit layer is where the time comes from.

## 7. Delete candidates that need an owner ruling

| Candidate | Cost to keep | Breaks if removed | Recommendation |
|---|---|---|---|
| Sonnet judge on non-owner prose (`doc-contracts.md:15-16` scopes it to summaries, Decisions, glance cells) | one Sonnet dispatch per unit; 038 recorded 22 `doc-gate` events, first-pass approval 0.636 — a third of sittings disapproved | language-rule enforcement on Decisions blocks; summaries keep it either way | keep for summaries and glance cells (the owner reads those); drop for Decisions — **provisional**: decide after one real unit shows what the judge catches there |
| Backlog as markdown (`state/backlog.md`, 137 rows, 25-line preamble) | doc-gate lint, hand edits at every close, prose history in the preamble | `bolt-process.md:42` sweep reads it; the owner reads it | keep the markdown; move the 25-line history preamble into git history and generate the table from a `backlog.jsonl` only if a second reader appears — not now |
| Validating archives forever (`records-auditor.mjs:123`, V2/V3 tiers, `D<n>` ids, `LEGACY_TOP`) | ~90 lines of tolerance code; every full auditor run re-reads 5 archived targets | nothing live: the citation scan and id uniqueness (`records-auditor.mjs:494-514`) still need the archive ledgers, but not their metrics | stop validating archived `metrics.jsonl` and resolutions; keep the id-uniqueness scan over archive ledgers; then delete the V2/V3 tiers |
| `ledger-miner.mjs` + prevention-sweep backfill (~150-250k tokens, approved 2026-08-10, never run) | dead script, a `paths.mjs` constant, a doc-contracts vocabulary entry, a table slot in `definition-of-done.md` | the prevention-sweep idea the owner liked | delete the script; keep the spec under `docs/superpowers/specs/`; rebuild from `records/` when the backfill is actually scheduled |
| Citation-leak scan (`records-auditor.mjs:517-544`) | git grep over `src/` on every no-target auditor run | the count of finding-id citations in source comments (target 0, reached 2026-07-29) | move to the pre-commit hook's comment gate (Enforce), which already scans added comment lines; drop it from the auditor |
| Skill eval folders (`loop-driver/evals`, `fix-review/evals`) | 156 lines referencing a fixture root that no longer exists | a record of the 2026-07-30 skill evaluation | **provisional** delete; archive the status lines into `self-driving-loop-design.md` first |
| `fix-links --apply` | 35 lines of one-off move logic | nothing (moves are done) | delete; keep the check inside `docs-sync` |

## 8. Migration order for 7b

Each step leaves all 565 assertions green (the two duplicate test blocks are deleted in step 1,
so the count drops to 555 there and stays).

1. Delete the duplicate test blocks (`render-records.test.mjs:15-85`, `records-auditor.test.mjs:116-120`); rename the colliding fixture numbers. No code change.
2. `records/schema.mjs` from `vocab.mjs`: add events + required fields, statuses, severities, areas, caps, cut-offs, targetless list, sha regex. Point `doc-gate.mjs:31`, `mint-id.mjs:23`, the four `TARGETLESS` lists and `wl.mjs:21-51` at it. Removes duplicates: targetless, areas, severity set, status vocabulary, sha regex.
3. `records/frontmatter.mjs` + `records/worklog.mjs` + `records/metrics.mjs`: replace the 9 frontmatter sites, 6 worklog loaders, 7 metrics readers. Removes: frontmatter, metrics-with-corrections, worklog-with-void. The auditor and gate-miner gain void filtering here (behaviour change, pinned by a new assertion each).
4. `model/target.mjs` + `cli/args.mjs`: the 11 root derivations, 10 `--root` parsers, 7 folder lookups, 11 version listings. Delete the unused `paths.mjs` exports and `targetDir`.
5. `model/spans.mjs`: lift `render-records.mjs:102-129` and `speed-report.mjs:96-147` into one module with a strictness flag; both test files pin the two behaviours unchanged.
6. `model/open-work.mjs` + `model/convergence.mjs` + `model/queue.mjs`: route and policy import them; delete `ledger.mjs`. Fix the README row-86 disagreement here by ruling: either route every fix-round answer through the design-pass check or reword the README row (owner question 1).
7. Split the auditor: `records/validate.mjs`, `fix/handback-gates.mjs`, citation scan → hook (or deleted per ruling). `records-auditor.mjs` becomes a CLI that calls all three. Legacy items deleted here: frontmatter-map fallback, `D\d+` row alternative.
8. `drive/rows.mjs` + `drive/gates.mjs` as data; `route.mjs` iterates rows. Then `cli/docs-sync.mjs` generates the README router table and the 7 other tables, with the check test. `fix-links --apply` and `MOVES` deleted; the check moves in.
9. Merge `gate-miner` into `speed-report`; delete `ledger-miner.mjs` and its constant/vocabulary entry (per ruling). Wire `summary-data.mjs` into `owner-summary` and `mint-id.mjs` into `reconcile-findings` (skill text only).
10. Move files into the folders; `cli/` shims keep today's command names; `fix-links` check confirms every prose path. Re-cut `integration.test.mjs` into the five flow files; merge the 13 drive fixtures into `drive-states` + `drive-ledger`.
11. 7c: archive-validation scope per ruling 3 (delete V2/V3 tiers, `LEGACY_TOP`, `D<n>` id allowance); stale prose (`doc-contracts.md:22-30, 175`, `self-driving:206, 208`, `discovery-review.wf.js:35`); drop `micro-review-*` from the stamper vocabulary; comment rule and commit style move to `coding-standards.md`.

## 9. Open questions for the owner

1. Router row 86 vs code: should the design-pass check intercept the armed row (open 🔴, reopened, regression) and the batch row too, as the README says — or is the README wrong and only rows 2 and the verification rows go through it? The code's current behaviour is pinned by no test either way.
2. Archive validation (section 7, row 3): stop validating archived `metrics.jsonl` and resolutions, keeping only the id-uniqueness scan over archive ledgers? Yes lets 7c delete the V2/V3 tiers.
3. `ledger-miner.mjs`: delete now and rebuild when the backfill is scheduled, or keep the dead script?
4. Sonnet judge scope: keep it on summaries and glance cells only, and drop Decisions — or wait for one real unit's data first?
5. Citation-leak scan: move to the hook's comment gate, or delete (the count has been 0 since 2026-07-29)?
6. Skill eval folders: delete after archiving their status lines, or keep as history?

Everything else in this note is decided as written and needs no answer.
