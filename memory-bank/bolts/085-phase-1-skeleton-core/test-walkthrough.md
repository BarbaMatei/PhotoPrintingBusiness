---
stage: test
bolt: 085-phase-1-skeleton-core
created: 2026-09-03T21:20:00Z
updated: 2026-09-04T00:40:00Z
---

## Verdict Report: 085-phase-1-skeleton-core (stories 001–005, guide Prompts 1–5)

### Summary

| Story | Guide brief | Verdict | The gap, in one line |
|---|---|---|---|
| 001-ledger-io | Prompt 1 | **satisfied with a gap** · seam misattributed | Two worktrees mint the same `PPW-<n>` into different targets and nothing catches it (reproduced, run A5); and records are published with a plain overwrite, no temp-file-then-rename |
| 002-bug-documentation | Prompt 2 | **not satisfied** | Three of five criteria absent: no per-defect plain-language summary, no reproduction recipe, no required-field validation. The three-audience record does not exist |
| 003-deduplication | Prompt 3 | **satisfied with a gap** | The trust gate is out of date: the only recorded ground-truth score is 2026-07-27 and the matching rules changed materially on 2026-09-02 |
| 004-report-rendering | Prompt 4 | **satisfied with a gap** | No confidence value survives into a published record, so the floor sorts on severity — the brief's v3.2 axis is confidence |
| 005-triage-intake | Prompt 5 | **satisfied with a gap** · seam misattributed | The awaiting-owner queue never ages, and no decision records who made it |

**Tests:** 836 assertions in `node reviews/lib/tests/run-tests.mjs`, all passed, plus eleven
adapted brief-test runs. Every run is reproducible: `evidence/rerun-evidence.sh` re-runs all of
them against a throwaway tree, and `evidence/evidence-log.txt` is its captured output. Nothing
was built — this bolt's whole diff is Markdown under `memory-bank/`.

**Corrections made to the records under test.** Two `**Status:**` lines named a component that
cannot do the job; both have been rewritten in the story files (details in each section). Story
002 is re-assigned to bolt 087.

### How each criterion was judged

Unit 001's five stories carry **28 acceptance criteria**. Each has its own row below, on exactly
one of six grounds — a criterion with two halves is split into lettered sub-rows rather than
given two grounds, because a half-graded row makes the roll-up unfalsifiable:

- **present** — the loop has the behaviour, in running code.
- **present (instruction only)** — the behaviour is written in a Markdown skill or runbook and
  nothing enforces it. A rule stated is not a rule obeyed; this ground is capped here and only
  becomes **present** when a production record shows the instruction followed.
- **substitute** — a different mechanism serves the same purpose; the row names it.
- **absent** — nothing does this job. Counts against the story.
- **N/A** — the criterion describes standing-sweep plumbing the pre-merge engine has no place
  for, and a written ruling says so. Never given to anything the loop's own contract documents
  as unimplemented.
- **divergence** — the loop refuses this on purpose, and the row cites the ruling. Without a
  ruling to cite, the row is `absent`, not a divergence.

Roll-up: *satisfied* = no `absent` row · *satisfied with a gap* = the story's core mechanism is
present and in use, with ≥ 1 `absent` row · *not satisfied* = the core mechanism is absent, or
the absent rows carry the story's stated purpose.
*Seam misattributed* is added when the `**Status:**` line names a file that does not carry the
claim; the row then names the component that does, and the story file is corrected.

**What is and is not graded.** The guide's status table
(`docs/agent-systems/bug-hunter-build-guide.md:125-166`) grades the review loop as the same
engine in pre-merge mode, so skill names and the skill-creator build route are out of scope —
the stories' own 2026-09 amendment says exactly that, inside criterion 1 of each story
(e.g. `001-ledger-io.md:24`). That amendment does **not** cover the pinned `bug-hunting/` paths,
which are a different criterion; those are graded N/A on a different authority —
`docs/agent-systems/integration-contract.md:112-115` rules that "`reviews/**` is deliberately
different — its records are part of the change under review, so they ride the feature branch".

---

## 001-ledger-io — satisfied with a gap · seam misattributed

**Claimed seam:** `reviews/lib/records/ledger.mjs`, `reviews/lib/review/mint-id.mjs`,
`reviews/lib/records/render-records.mjs`.

**Misattribution, corrected.** `reviews/lib/records/ledger.mjs` is 36 lines and its own header
says "this file only reads" (`reviews/lib/records/ledger.mjs:5`); it cannot carry a story about
*owning all reads and writes*. The ledger has three writers: `reviews/lib/review/mint-id.mjs:116`
(scaffolds a row and a detail block), `reviews/lib/records/render-records.mjs:178` (the only
writer of status flips and History lines), and the `reconcile-findings` agent, which fills the
scaffolded `<fill in>` lines by hand. Story 001's `**Status:**` line now says so.

| # | Criterion | Ground | Evidence |
|---|---|---|---|
| 1a | Test (a): a fresh ledger, two defects, the human view | **present** | Run A1: `scaffold-ledger` built a ledger from the template and appended two defects, each with its table row and detail block |
| 1b | Test (b): overlapping writes merge with no lost data and no duplicate ids | **absent** | Run A2 shows the same id refused *within one target* (exit 2), but run A5 shows two worktrees minting `PPW-9000` into different targets, both exit 0. See row 4 |
| 1c | Test (c): list what has never been examined | **substitute** | Run A3: the router reads coverage and reports "every manifest lens has run"; the loop's unit of coverage is the lens, not the file, because it hunts a diff rather than a codebase |
| 2a | The pinned `bug-hunting/` JSON store plus a generated Markdown view | **N/A** | `integration-contract.md:112-115` rules the pre-merge store deliberately different. The store is Markdown-native — `reviews/<target>/ledger.md` per `reviews/templates/ledger.md` — so there is no JSON/mirror pair to keep in step, and `regenerate_markdown_view` has nothing to regenerate |
| 2b | A top-level `schema_version`; a loader refuses a newer major | **absent** | Versioning is dated, not numbered: `reviews/rules/metrics-schema.md` v2/v3/v4 sections with grandfathering cut-offs at `reviews/lib/records/schema.mjs:78,82`. No reader refuses anything for being newer than it knows. First graded a divergence and regraded absent at the stage-4 gate: no ruling exists that chose this. Filed in story 012 |
| 2c | The brief's sections exist | **substitute** | `bug_index` → the Findings table; `dismissed` → the terminal statuses (`schema.mjs:24`); `coverage` → `lenses` / `lenses-not-run` (`reviews/templates/review.md:10-11`); `runs` → `metrics.jsonl` plus `reviews/state/index.md`. `application_map` has no equivalent (there is no Map stage) and `correlation_id` is N/A — the fixer is in-loop, so there is no external bolt to correlate to |
| 3a | Temp-file-then-rename with Windows retry-with-backoff | **absent** | Every writer is a plain `writeFileSync`: `mint-id.mjs:116`, `render-records.mjs:174`, `:178`. Nothing addresses the Windows case the brief names — a rename over a file a reader holds open. The partial substitute is that the renderer runs every check before its first write, so a *refusal* leaves the records untouched (`render-records.mjs:12-14`), and git is the restore point; neither helps once writing has begun. **Filed as story 012** |
| 3b | Growth handled by explicit versioned archival, never silent pruning | **present** | Archive-on-close moves a closed target to `reviews/archive/<target>/` with contents unchanged, as the last step of recording a close (`reviews/rules/doc-contracts.md:57-60`) |
| 4 | Staging files + single-writer merge; **atomic** `next_bug_id`; writes never drop data | **absent** | There are no staging files because there is no merge: the loop is single-writer by construction. `next_bug_id` is an unlocked read-then-write (`mint-id.mjs:48-53`). Six concurrent mints in one checkout produced six disjoint ranges (run A4) — process startup serialises them — but two *worktrees* both minted `PPW-9000` and both scaffolded it, exit 0 (run A5). `integration-contract.md:106-112` already documents this: the reservation is "not something already in place", and the guard "compares a new id against the same target's ledger only". **Filed as story 008** |
| 5a | `load` tolerates a first-run empty ledger | **present** | `reviews/lib/records/ledger.mjs:19` returns `null` when the file does not exist, and callers fall back to pre-ledger behaviour |
| 5b | A content hash warns on an out-of-band write | **substitute** | The job is done against git and more strictly: any edited line of an existing detail block is refused, not warned about — `reviews/lib/records/doc-gate.mjs:280-292`, demonstrated in run A6 (`detail block PPW-9001 line 1 changed — blocks are append-only`) |
| 5c | A corrupt file is refused, with restore-from-git instructions | **absent** | Refusal happens — `render-records.mjs:89` refuses an unparseable **worklog**, and the doc gate lists violations and "judges only — it never edits". No refusal anywhere names the remedy the brief asks for: which file, and that git history holds the last good copy. **Filed in story 012** |
| 5d | The remaining operations | **present** | `next_bug_id` → run A1 · `upsert_bug` / `set_status` → `scaffold-ledger` plus the renderer's flips (`render-records.mjs:126-181`) · `record_dismissal` → the resolution's Decisions block, gate-enforced (run A7) · `update_coverage` → `lenses-not-run` · `append_run_summary` → the metrics line and index row. `add_suppression_pattern` is a **divergence**: the loop never suppresses, ruling at `reviews/README.md:229-231` citing integration contract §6.5 |
| 6a | The `Reopened` status | **substitute** | A reopened fix is a count on the pass line that re-arms the loop (`reviews/lib/drive/rows.mjs:56`, `:103-108`), not a status word. The information is carried; the enum value is not |
| 6b | Each index entry embeds the full record | **present** | The describe-once rule: the detail block "is the defect's only full description anywhere" (`reviews/runbooks/runbook-discovery.md:119-121`) |
| 6c | `runs` carries `oracle_as_of_commit` and per-run eval metrics | **N/A** for the oracle, **present** for metrics | There is no oracle — the guide's own table grades `intent-lookup` ✗ (`bug-hunter-build-guide.md:163`). Per-run metrics are `metrics.jsonl`, schema at `reviews/rules/metrics-schema.md` |
| 7 | Single-history rule: runs only in the integration worktree on `main` | **divergence** | Ruled, verbatim: "The single-history rule above governs `bug-hunting/**` and `knowledge/**`; `reviews/**` is deliberately different — its records are part of the change under review, so they ride the feature branch" (`integration-contract.md:112-115`) |

**Roll-up:** 3 absent rows (1b/4 — the same defect; 2b and 3a/5c — the same publish-and-load
surface), against a mechanism that is present and in daily use across seven targets →
**satisfied with a gap**, two gaps filed (008, 012).

---

## 002-bug-documentation — not satisfied

**Claimed seam:** `reviews/lib/records/render-records.mjs`, already recorded partial. The
partial is confirmed, and the record's real home is `reviews/templates/ledger.md` plus the
describe-once rule in `reviews/rules/doc-contracts.md`; the renderer only appends status and
History.

| # | Criterion | Ground | Evidence |
|---|---|---|---|
| 1 | Built; the three test prompts pass (a full record; a missing reproduction flagged; contract-cited vs intent-unconfirmed) | **absent — untested** | No adaptation exists. A record missing its reproduction cannot be flagged, because no record has a reproduction field |
| 2 | Every required field enforced; a record missing one is refused | **absent** | The canonical record is four bullets — What · Evidence · Suggested fix · History (`reviews/templates/ledger.md:18-27`). The gate enforces shape and size only: the 20-line cap, the status vocabulary, append-only against HEAD (`reviews/lib/records/doc-gate.mjs:266-292`). Of the brief's required fields, `category`, `confidence`, `risk_score`, `reachable`, `plain_summary`, `location[]`, `flow_position`, `reproduction` and `impact` do not exist; `commit_sha` exists as the ledger row's Affirmed cell; `correlation_id` is N/A, as in story 001 row 2c |
| 3 | `expected_behavior` cites a knowledge-ledger contract, else "intent-unconfirmed" | **N/A** | There is no knowledge ledger and no oracle in this repo; the guide grades `intent-lookup` ✗ (`bug-hunter-build-guide.md:163`). The nearest thing is the `requirements` lens reading the bolt's story docs directly (`reviews/lib/discovery-review.wf.js:149-153`), which is not a contract citation |
| 4 | Audience validation: plain summary jargon-free, developer detail technical, reproduction runnable | **substitute**, one audience of three | The developer is served: the fix brief carries files:lines, the traced failing path, and an assertion-spec test shape (`reviews/runbooks/runbook-discovery.md:121-133`). The non-technical reader is served **per pass, not per defect** — `summary-v<n>.md`, four sections, 60-line cap, plain words enforced by the judge (`reviews/rules/doc-contracts.md:11-22`). The tester is not served: a test shape to write is not a reproduction recipe, and it exists only for serious findings |
| 5 | A secret-involving record carries location + fingerprint, never the value | **absent** | No redaction rule anywhere in the record contract. The only backstop is repo-wide and after the fact: `.github/workflows/secret-scan.yml:1` runs gitleaks over committed files, `reviews/**` included. It catches a leak; it does not shape a record |

**Roll-up:** three absent rows, and they carry the story's stated purpose — "one canonical
structured record serving all three audiences" (`002-bug-documentation.md:19`). The loop does
keep one canonical, gate-enforced, describe-once record per defect, but that is the developer's
record, not the three-audience one. **Not satisfied.** This matches the guide's own ◐ grading
(`bug-hunter-build-guide.md:140`) and the story's pre-existing seam note. Per this bolt's stage
5, story 002 is **re-assigned to bolt 087-phase-2-trust** rather than duplicated as a gap story.

*(This verdict was "satisfied with a gap" in the first draft. The stage-4 fresh-eyes review
called it a ◐ laundered into a ✓ against the report's own roll-up rule, and it was right.)*

---

## 003-deduplication — satisfied with a gap

**Claimed seam:** `.claude/skills/reconcile-findings/SKILL.md`. Correct.

| # | Criterion | Ground | Evidence |
|---|---|---|---|
| 1 | Built; the four test prompts pass | **absent — untested (currently)** | One blind ground-truth run is recorded: 2026-07-27 PASS, "0 over-merges across hard cases 1–7" (`SKILL.md:118-126`). The skill's own rule is to re-score "after any material change to the rules above" (`SKILL.md:109-110`), and `git log` shows two later changes: `c09675d` (2026-08-13, the id scheme the matching rules key on) and `0c6938c` (2026-09-02, a new required lineage rule with a "write `null`, never guess" clause). There is no current passing score to cite. **Filed as story 009** |
| 2 | A normalized signature; a verdict plus matched id and rationale | **substitute** | No computed signature string. The rule is "same defect mechanism at the same site", with same file / same theme / same id string explicitly not enough (`SKILL.md:44-47`); output is one row per finding with a confidence and a one-line reason (`SKILL.md:73-75`). A moved line still matches, because the match is on mechanism rather than line number. Note the verdict set differs: the brief's is `{new | duplicate | dismissed | suppressed}`, the loop's is `PPW-<n>` or NEW — the two missing values are the suppression path row 3 rules out |
| 3 | Duplicates link; `dismissed` entries drop; suppression matches drop | **divergence** | The loop refuses to drop, on purpose: a decided item is matched, its prior decision attached verbatim, and it is re-argued by a fresh skeptic (`SKILL.md:57-60`; `reviews/README.md:229-231`; integration contract §6.5). The guide's own v3.7 note supersedes this criterion identically (`bug-hunter-build-guide.md:634`, `:642`, `:646`). It is the story text that needs amending, not the loop |
| 4 | "Same area" is never "same bug" | **present** | `SKILL.md:44-47` states it as the first matching rule |
| 5 | The collision guard: a signature match is a candidate only | **present**, and stronger | The rule is split-when-unsure, with the asymmetry spelled out: a wrong merge stops the loop early and ships a bug, a wrong split costs another look (`SKILL.md:61-63`). Lineage instead of merging is in production use: `reviews/archive/038-039-invoicing/metrics.jsonl` carries 22 non-null `fix_generated` values across 13 distinct parent ids, `seed_round: 15` on 24 findings, and `hinted: true` on 36 |

**Roll-up:** the mechanism is present, in use, and evidenced by production records; its own
trust gate is two material rule-changes out of date, so no current score can be cited.
**Satisfied with a gap** (story 009).

---

## 004-report-rendering — satisfied with a gap

**Claimed seam:** `reviews/templates/review.md`, `reviews/templates/summary.md`,
`reviews/lib/records/doc-gate.mjs`. Correct.

| # | Criterion | Ground | Evidence |
|---|---|---|---|
| 1 | Built; the three test prompts pass (Low → appendix; a zero-bug run; a second run writes a new file) | **absent — untested** | Test (c) passes: a second pass writes `review-v<n+1>.md` and never touches v`<n>` — files are numbered and immutable once the gate passes (`reviews/rules/doc-contracts.md:195`), and the ledger's append-only rule is machine-enforced (run A6). Test (a) cannot be run at all: there is no appendix (row 3). Test (b) has no adaptation — no template carries a zero-findings note (row 2) |
| 2 | A new dated file per run; a Run Summary (scope, counts, uncovered areas, an explicit zero-bug note); findings sorted by **risk** descending | **substitute** | A new numbered file per pass, yes. The rest is close but not the same: `reviews/templates/review.md:12-15` gives counts, tests and verdict; `:11` gives uncovered **lenses**, not uncovered areas; no template has a zero-findings note; and "ranks worst first" (`doc-contracts.md:200`) is worst-by-**severity**, not by a risk score — the loop has no risk score, which is the Phase 2 brief the guide grades ◐ |
| 3 | The reporting floor **on the confidence axis**; Low-confidence in an appendix; a body callout for a Critical/High parked there | **absent** | The loop floors on severity: 🟡/⚪ enter the ledger as `backlog` and appear in the summary's "Filed automatically" (`reviews/README.md:167-173`, `reviews/templates/summary.md:25-27`). Confidence exists only inside the per-lens schema (`reviews/lib/discovery-review.wf.js:135`, `:214`, `:216`) and never reaches a published record — `grep -rn "confidence" reviews/templates reviews/rules/doc-contracts.md` returns nothing. So a low-confidence 🔴 is foregrounded exactly like a proven one, and the brief's "⚠ unconfirmed but Critical if real" callout (`bug-hunter-build-guide.md:673-675`) has nothing to attach to. **Filed as story 010** |
| 4 | A record carrying `injection_suspected` surfaces the flag | **absent** | No such flag exists in `reviews/lib`, `reviews/templates` or `reviews/rules`. The `security` lens hunts injection as a defect class (`reviews/lib/records/schema.mjs:132`), which is a different thing. **Filed in story 010** |
| 5 | Only redacted evidence renders; never raw secret material | **absent** | As story 002 row 5: no rule, CI gitleaks as the only backstop. **Filed in story 010** |
| 6 | An optional non-defect Observations section | **absent** | No Observations section in any template. The nearest is the Refuted table (`reviews/templates/review.md:26-30`), which is for suspicions disproved, not observations. **Filed in story 010** |
| — | The v3.7 extension: an owner summary ≤ 60 lines in four sections, every claim linked; lint plus model judge; record quality graded apart from review truth | **present** | Sections and cap are machine-enforced at `reviews/lib/records/doc-gate.mjs:189-193`, demonstrated in run A8: padding a conforming summary past the cap is refused with `body is 74 non-empty lines — cap is 60`. The two-part gate and the separate grading of record quality are stated at `reviews/rules/doc-contracts.md:11-22` |

**Roll-up:** the report mechanism, its caps and its gate are present and enforced, and the v3.7
extension is met outright; four criteria are absent, one of them structural. **Satisfied with a
gap** (story 010, which carries all four).

---

## 005-triage-intake — satisfied with a gap · seam misattributed

**Claimed seam:** `reviews/lib/drive/gates.mjs`.

**Misattribution, corrected.** That file is 40 lines of exported constants and its header says
"Reads nothing" (`reviews/lib/drive/gates.mjs:4`) — it is the gate vocabulary shared by the
router and the policy, not the intake channel. The channel is four parts: the owner-gate
protocol at `.claude/skills/loop-driver/SKILL.md:38-45`; the `gate-open` / `gate-closed` /
`gate-parked` worklog events (`reviews/lib/records/schema.mjs:35-37`, writable only through the
stamper); the written delegation in `reviews/lib/drive/autonomy-policy.mjs:2-5` ("the written
delegated-decision rules, executable … Fail closed"); and the doc gate's rationale rule. Story
005's `**Status:**` line now says so.

| # | Criterion | Ground | Evidence |
|---|---|---|---|
| 1 | Built; the four test prompts pass | **present** | (a) a dismissal with a reason recorded with provenance — real `gate-parked` events carrying `kind`, `default` and `reason` (`reviews/archive/038-039-invoicing/worklog.jsonl:210`, `:228`, `:245`, `:291`); (c) a reason-less dismissal refused — run A7; (b) suppression approval is the divergence of story 003 row 3; (d) see row 5 |
| 2 | Decisions validated and applied through the ledger's owner | **present** | Decisions land as a Decisions block per finding in `resolution-v<n>.md` (`reviews/templates/resolution.md`, `## Decisions`), and the renderer — the sole writer — turns them into ledger flips and History lines (`reviews/lib/records/render-records.mjs:126-181`). An id with no ledger row is reported, never invented (`:129`) |
| 3a | Provenance: when, and against which commit | **present** | Every worklog event is timestamped by the stamper, never by hand (`reviews/rules/doc-contracts.md:152`); the commit is `fixed_commit` in the resolution frontmatter and `Affirmed` on the ledger row |
| 3b | Provenance: **who** | **absent** | There is no actor field on any event or record. One owner and one session make it implicit, which is not the same as recorded. **Filed into story 011** |
| 3c | A bare "dismissed" is rejected | **present** | `reviews/lib/records/doc-gate.mjs:256-261` refuses any non-fixed status with no Decisions block. Run A7: `PPW-9002 status wont-fix has no Decisions block — every non-fixed status needs its rationale` |
| 3d | A status change is checked legal | **absent** | The gate checks that a non-fixed status has a rationale and that the status word is in the vocabulary (`doc-gate.mjs:266-274`); nothing checks that a *transition* is legal. **Filed into story 011** |
| 4 | An awaiting-a-person queue: capped, digest-grouped, **age-escalating** | **absent** (ageing) | The queue exists and is batched: every delegated decision is parked and listed in the run-end report with the default taken (`.claude/skills/loop-driver/SKILL.md:263`, `:313-314`), and `run-end` carries the count — `{"ev":"run-end","passes":4,"parked":5}` (`worklog.jsonl:246`). Nothing ages. It has already bitten: "which fiscal address a parcel-locker order carries is an owner decision; **asked twice, unanswered**" (`worklog.jsonl:291`), and the target closed with it open. **Filed as story 011** |
| 5 | Intake during a live run queues instead of racing the close-merge | **N/A** | There is no inbound channel to race with: decisions do not arrive mid-run, the run stops and asks. `gate-open` is stamped when the question is relayed and `gate-closed` by the invocation that consumes the answer, and that span is the measured blocked-on-owner time (`.claude/skills/loop-driver/SKILL.md:42-45`). Nothing writes while the loop waits. Graded "substitute" in the first draft; the stage-4 review was right that the cited mechanism runs in the opposite direction, so it is N/A-by-architecture, not a demonstrated substitute |

**Roll-up:** the human-decision channel exists, is enforced, and is visible in production
records; three absent rows, all in the same record contract. **Satisfied with a gap** (story 011,
extended to carry rows 3b and 3d).

---

## Commands run, and what each proves

Everything below re-runs from `evidence/rerun-evidence.sh`, whose captured output is
`evidence/evidence-log.txt`. The script is read-only on the repo: its writes go to a throwaway
tree under `$TMPDIR`, removed on exit.

| # | What it runs | Result |
|---|---|---|
| A1 | `scaffold-ledger` twice into a fresh root | A ledger built from the template, two defects appended with rows and detail blocks — Prompt 1 test (a) |
| A2 | the same id a second time in one target | `ERROR PPW-9001 already exists …`, exit 2 |
| A3 | the router on a loop-quiet fixture | `every manifest lens has run — loop quiet (row 6)`, exit 2 at the certification gate — the coverage read |
| A4 | six concurrent `mint --count 2` on one counter | six disjoint ranges, counter `9000 → 9012`; no collision within one checkout |
| A5 | two roots, each `mint --count 1`, then `scaffold-ledger` into **different** targets | both print `PPW-9000..PPW-9000`, both scaffold, exit 0 — **the id gap, reproduced** |
| A6 | edit one line of a published ledger block, then the doc gate | `detail block PPW-9001 line 1 changed — blocks are append-only`, exit 1 |
| A7 | a resolution row flipped to `wont-fix` with its Decisions block deleted | `PPW-9002 status wont-fix has no Decisions block — every non-fixed status needs its rationale`, exit 1 |
| A8 | a conforming summary padded past the cap | `body is 74 non-empty lines — cap is 60`, exit 1 — the guide's v3.7 test for Prompt 4 |
| A9 | the doc gate on the untouched fixtures | `DOC GATE: clean` twice, exit 0 — the baseline A6–A8 are measured against |
| A10 | the `loop-driver` eval suite's read-only verify lines | auditor exit 0 on `094-quiet` and `091-fixround`, exit 1 on `096-rotten` (`review-v2.md has no metrics line`), warnings on `095-postcert` and `097-mockpass`; router exit 3 on `094-quiet` at `GATE_KIND: delta-worthiness` (bolt 086 evidence) |
| A11 | two test processes at once | the second exits 3: `another test process is running (pid …) — the machine takes one at a time` (bolt 086 evidence) |
| T1 | `node reviews/lib/tests/run-tests.mjs` | `836 assertions, all passed` |
| T2–T5 | the same with `--only route-next-pass` · `doc-gate` · `mint-id` · `records` | 51 · 46 · 43 · 171 assertions, all passed. `--only` is a substring match on the filename, so `records` sweeps `records.test.mjs`, `records-auditor.test.mjs` and `render-records.test.mjs` |

**What the fixture suite does not prove.** `reviews/lib/tests/unit/mint-id.test.mjs` has no
concurrency, parallel, race or reuse case (`grep -c` → 0) and there is no `ledger.test.mjs` at
all. The suite covers argument handling and refusals; every write-safety claim above rests on
runs A4–A6, which is why they are committed as a re-runnable script rather than described.

### Acceptance criteria of this bolt

- ✅ All five stories carry a recorded verdict, rolled up from per-criterion rows, with
  `file:line` evidence and reproducible command output.
- ✅ No part of the June skeleton was built — no `bug-hunting/` tree, no new skill, no engine
  code. The whole diff is Markdown under `memory-bank/` plus the evidence script and its log.
- ✅ Every confirmed gap exists as a new story with `assigned_bolt: 087-phase-2-trust`, and its
  id appears in bolt 087's `stories:` list. Story 002's gap is filed once, by re-assigning story
  002 itself.
- ✅ Unit 001's status is truthful: 001, 003, 004 and 005 are `complete` with their gaps carried
  by named successor stories; 002 is `ready` and re-assigned to 087; the unit is not complete.

### Gaps filed

| Story | Assigned | From |
|---|---|---|
| `008-id-reservation-parallel-worktrees` | 087-phase-2-trust | 001 rows 1b, 4 |
| `012-atomic-record-publish` | 087-phase-2-trust | 001 rows 2b, 3a, 5c |
| `009-reconciler-trust-gate-rescore` | 087-phase-2-trust | 003 row 1 |
| `010-confidence-axis-reporting-floor` | 087-phase-2-trust | 004 rows 3–6 |
| `011-owner-queue-age-escalation` | 087-phase-2-trust | 005 rows 3b, 3d, 4 |
| `002-bug-documentation` (re-assigned, not duplicated) | 087-phase-2-trust | 002 rows 1, 2, 5 |

### Hand-off to the coordinator — records this bolt may not touch

1. **`memory-bank/story-index.md` is stale.** It reads "Unit: 001-phase-1-skeleton (**7
   stories**)" at line 1767; the unit now has 12, and every Phase 1 story's "claimed satisfied"
   annotation is now a recorded verdict. This bolt is forbidden to edit any index file.
2. **The guide's summary line contradicts its own table.**
   `docs/agent-systems/bug-hunter-build-guide.md:128` says "Phase 1 is done" while the table at
   line 140 grades `bug-documentation` ◐ — and this bolt grades that story *not satisfied*. The
   table is right; the summary line is not. `docs/` is outside this bolt's write set.
3. **Two more table rows are now overstated.** `ledger-io` ✓ (line 139) and `deduplication` ✓
   with "ground-truth scored" (line 141): the first has two confirmed gaps, the second's score is
   two material rule-changes old.

### Human validation checkpoints (specsmd)

Self-validated by the executing session under the wave-1 coordinator addendum.

- **Stage 1 (plan):** approved, then amended after the stage-2 adversarial design check — ten
  attacks, all folded in (`implementation-plan.md`, "Design-check amendments"). The check changed
  the method materially: per-criterion rows instead of per-story, adapted brief tests instead of
  a reading-only pass, and two misattributed seams caught.
- **Stage-4 gate (fresh-eyes micro-review):** run as a separate subagent over the first draft of
  this report. It checked 40+ citations and found four wrong pointers (all corrected), one claim
  citing real code for the wrong thing (row 5c, rewritten), a count wrong by 4× (row 5, now 22
  across 13 parent ids), and — the serious one — that the report asserted record edits and a met
  acceptance criterion that had not been done. Those edits have now been made, and the report
  says so only because they are on disk. It also overturned the verdict on story 002 and found a
  second unfiled absent on story 001, both accepted above. A sixth ground, *present (instruction
  only)*, comes from the parallel check on bolt 086 and is now declared.
- **Stage 3 (record, this file):** approved after those corrections.
