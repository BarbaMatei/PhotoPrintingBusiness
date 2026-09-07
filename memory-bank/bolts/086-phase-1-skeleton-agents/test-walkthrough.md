---
stage: test
bolt: 086-phase-1-skeleton-agents
created: 2026-09-04T01:10:00Z
---

## Verdict Report: 086-phase-1-skeleton-agents (stories 006–007, guide Prompts 6–7)

### Summary

| Story | Guide brief | Verdict | The gap, in one line |
|---|---|---|---|
| 006-general-hunter | Prompt 6 | **satisfied with a gap** | The hunt is diff-scoped: nothing identifies entry points by convention or traces a flow top-down as a way of hunting, and nothing treats source text as data rather than instructions |
| 007-orchestrator-skeleton | Prompt 7 | **satisfied with a gap** | Blinding is asked for in a prompt and enforced by nothing — the loop's own README says so — and no run lock serialises a pass |

**Tests:** the eleven adapted runs and five suite runs in
`../085-phase-1-skeleton-core/evidence/rerun-evidence.sh`, captured in `evidence-log.txt` beside
it. A10 and A11 there exist for this bolt. Nothing was built.

### Method

Inherited whole from bolt 085 — the same six grounds, the same one-row-per-criterion rule, the
same roll-up. Stories 006 and 007 carry **12 acceptance criteria** between them; a criterion with
two halves is split into lettered sub-rows. Two rows below are marked *(not in the story)*: they
are requirements of the guide's brief that the story text never captured, and this bolt records
them because the claim under test is the brief, not the story's paraphrase of it.

The ground that matters most here is **present (instruction only)**: these two stories are about
agent *procedures*, and a procedure lives in Markdown that instructs a model. A rule written in
a SKILL.md is not a rule enforced. Such a criterion is capped at that ground unless a production
record shows the instruction actually followed.

---

## 006-general-hunter — satisfied with a gap

**Claimed seam:** the core six lenses — `reviews/lib/records/schema.mjs` (`CORE_LENSES`) and
their prompts. Correct: `CORE_LENSES` is at `reviews/lib/records/schema.mjs:130-137` and the
prompts it names are at `reviews/lib/discovery-review.wf.js:139-196`, dispatched in parallel at
`:331`.

| # | Criterion | Ground | Evidence |
|---|---|---|---|
| 1a | Test (a): scan and surface candidates | **present** | The lens fan-out is the loop's hunt, run on every discovery pass; production evidence is seventeen passes on one target — e.g. `reviews/state/index.md:33`, a delta pass returning 5/12/10/1 findings |
| 1b | Test (b): surface only what is not already in the ledger | **present** | Re-finds get one appended History line, never a re-description (`reviews/runbooks/runbook-discovery.md:142`), and a delta pass scopes to the diff since the last full pass (`:22`). Real re-find lines carrying their prior decision verbatim: `reviews/archive/038-039-invoicing/ledger.md:457`, `:536`, `:561` |
| 1c | Test (c): report what was covered | **present** | `lenses` and `lenses-not-run` in every review's frontmatter (`reviews/templates/review.md:10-11`), and the router refuses certification while a manifest lens is owed — run A3 reports "every manifest lens has run" |
| 2a | Sweep files for local defects | **substitute** | The `correctness` lens covers null/empty paths, boundaries (`> vs >=`), removed guards and resource/stream lifetime (`reviews/lib/discovery-review.wf.js:140-142`); swallowed exceptions belong to `observability` (`:171-174`); secret exposure to `security` (`:144-147`). Two of the brief's seven classes are named nowhere: **type coercion**, and **hardcoded secrets in source** (the security lens hunts secrets in logs and responses, which is a different defect) |
| 2b | Identify entry points by convention and trace the main flows top-down, checking validation, auth, error handling and state/transaction handling at each hop | **absent** | The hunt is diff-scoped by construction: the runbook's first step is `git diff main...HEAD` (`reviews/runbooks/runbook-discovery.md:20-22`) and the core prompts are written "of the changed logic" / "across the change" (`wf.js:140`, `:144`, `:149`). Nothing enumerates routes, controllers, `main` or handlers, and no lens walks a flow hop by hop. The only top-down trace in the system is the per-finding trace skeptic (`wf.js:278-282`), which runs *after* a candidate exists — verification, not hunting. Per-hop coverage is also split across lenses that are not core: validation → `input-validation`, error handling → `observability`, transactions → `race`, all added by what the change touches (`schema.mjs:139-147`), so a change that triggers none of them gets no hop check at all. **Filed as story 013** |
| 3a | `deduplication` runs before emitting | **substitute** | Dedup runs after the lenses and before verification — one agent clusters same-defect findings across lenses (`wf.js:348-352`), then cross-pass matching happens at reconciliation (`runbook-discovery.md:115-118`). The brief's order (dedup inside the hunter) would cost each lens a ledger read and break blinding; the loop's order achieves the same end — each real defect verified once — and keeps the hunter blind |
| 3b | Candidates carry the shared shape with a `category_guess` | **substitute** | `FINDINGS_SCHEMA` requires file, severity, title, failure scenario, suggested fix and confidence (`wf.js:216`), plus line and the emitting lens. There is no `category` field; the lens that found it is the nearest thing to a category guess |
| 3c | Every plausible lead is surfaced — no self-censoring, because Verify gates | **divergence** | The loop censors at the lens on purpose: "Report only real, justified issues; empty array if none" (`wf.js:137`) and, for security, "report anything >=7" on a 1–10 confidence (`wf.js:144-145`). The reasons are written down — output tokens dominate cost (`wf.js:43-44`) and first contact must not be a false-positive flood (`reviews/README.md:11-14`). The cost is real and worth stating: a defect a lens rates 6 is never reported, and no skeptic ever sees it. Recorded as a divergence, not a gap, because it is a deliberate trade with its rationale on the record |
| 3d | Coverage is updated | **present** | As row 1c |
| 4 | Read-only on source | **present (instruction only)** | The loop's hard rule is "A review produces findings; fixing is a separate explicit step … Never auto-apply fixes mid-review" (`reviews/README.md:200-201`), and the quality lens is told **REPORT ONLY** (`wf.js:155`). Nothing mechanically prevents a lens from writing — there is no read-only sandbox and no close-time write audit (story 007 row 6c). Production record: no pass in `reviews/state/index.md` records a lens-authored source change |
| 5 | *(not in the story)* Source text, comments included, is data and never instructions: instruction-like content is quoted, flagged `injection_suspected`, and hunting continues | **absent** | `docs/agent-systems/bug-hunter-build-guide.md:752-754` requires it. A case-insensitive grep for `injection_suspected｜prompt injection｜instruction-like｜treat.*as data｜never as instructions` across `reviews/lib`, `reviews/rules`, `reviews/runbooks`, `reviews/README.md` and `.claude/skills` returns nothing, while lenses are handed raw source, either as a code pack or by reading files directly (`wf.js:92-96`). **Filed as story 014** |
| 6 | *(not in the story)* A suspected secret is carried as location + fingerprint from the start, never its value | **absent** | Same brief line (`:754-755`); a grep for `fingerprint｜redact` across `reviews/lib`, `reviews/rules`, `reviews/runbooks` and `reviews/templates` returns nothing. **Filed in story 014** |

**Roll-up:** the hunting mechanism is present, in production use, and demonstrably finds real
defects; three absent rows, one of them structural (the flow trace). **Satisfied with a gap**
(stories 013 and 014). The story text is also incomplete against its own brief — rows 5 and 6
exist in the guide and not in the story — and that is itself worth the coordinator's attention.

---

## 007-orchestrator-skeleton — satisfied with a gap

**Claimed seam:** `.claude/skills/loop-driver/SKILL.md`, `reviews/lib/drive/route-next-pass.mjs`,
`reviews/lib/discovery-review.wf.js`. Correct, and the three divide the job cleanly: the skill
sequences, the router decides, the workflow executes the hunt.

| # | Criterion | Ground | Evidence |
|---|---|---|---|
| 1 | Built; the three test prompts pass (a first run labelled unverified; a second surfacing only new; a zero-finding run handled correctly) | **present**, with a caveat | (b) and (c) run: the router on a fixture reports a clean verification and gates at `GATE_KIND: delta-worthiness`, exit 3 (run A10), and a delta pass surfaces only work since the last full pass (`runbook-discovery.md:22`). (a) is row 3a. The caveat: the skill's own eval suite, `.claude/skills/loop-driver/evals/evals.json`, says of itself "REBUILT 2026-09-01 and UNRUN … the pass rates under `history` were measured against the retired fixture root", so its 16/16 history cannot be cited. What can be cited is its mechanical half, re-run here (A10): auditor exit 0 on `094-quiet` and `091-fixround`, exit 1 on `096-rotten` (`review-v2.md has no metrics line`), warnings on `095-postcert` and `097-mockpass` |
| 2 | All six permanent slots exist now | **substitute** | Every slot has an occupant under a different name. Map → the audit-then-route step, `records-auditor.mjs` then `route-next-pass.mjs` (`.claude/skills/loop-driver/SKILL.md:28-33`). Hunt → the lens fan-out (`wf.js:331`). Verify → the guard and trace skeptics (`wf.js:273-282`), argument rather than execution, which is the Phase 2 brief the guide grades ◐. Triage → dedup (`wf.js:348`) then reconciliation and severity ranking (`runbook-discovery.md:112-118`). Report → `review-v<n>.md`, `summary-v<n>.md` and the records (`SKILL.md:109-124`). Learn → `metrics.jsonl`, `reviews/state/track-record.md`, the backlog, and the system's own review folder. The structure is the same six; only the names differ |
| 3a | The report is honestly labelled — Phase 1's "unverified candidates" | **substitute** | The loop labels per finding rather than per report, because its Verify slot is filled: a finding that skipped a skeptic is returned as `unverified-low`, `unverified-over-budget` or `unverified-cleanup`, carrying `(unchallenged lens verdict, not a refutation)` (`wf.js:294`, `:299`, `:303`). Above that, verification and delta passes are capped at `approve-with-followups`, and "Certified means exactly that — not zero defects" (`reviews/README.md:14`) |
| 3b | The reporting floor applies | **absent** | The same gap story 004 row 3 records: the floor sorts on severity, and no confidence value survives into a published record. The brief fixes the axis on confidence (`bug-hunter-build-guide.md:679`). **Filed in story 010** |
| 4a | A per-run scope and a stopping condition are defined | **present** | Scope: the runbook's first step is an explicit diff, with a delta lens cap of 5 and a 600k output-token budget, both script-enforced (`runbook-discovery.md:20-28`, `wf.js:74`). Stopping condition: the severity-based stop rule — no 🔴 surviving and every 🟠 carrying a recorded decision (`reviews/README.md:11-14`), executed by the router |
| 4b | Bugs are never invented to avoid an empty run | **present (instruction only)** | "Do NOT invent findings" to the dedup agent (`wf.js:349`); "Report only real, justified issues; empty array if none" to every lens (`wf.js:137`). Nothing checks it mechanically. Production record: clean passes exist and are routed as such (run A10, `GATE_KIND: delta-worthiness` on a zero-finding verification) |
| 4c | A plausible finding is never dropped | **divergence** | As story 006 row 3c: lenses filter by their own confidence, deliberately and with the reasons written down |
| 5 | The trigger is pushy enough that runs go through the orchestrator | **present** | The `loop-driver` description lists the phrases it must intercept, including "even if they don't say the word 'loop'" (`.claude/skills/loop-driver/SKILL.md:3-12`), and `reviews/README.md:24-30` makes the standing instruction route through it |
| 6a | Open: the cross-system mutex on the knowledge builder's `knowledge/.run-lock` | **N/A** | There is no knowledge builder in this repository and no `knowledge/` tree; the sibling system this mutex coordinates with does not exist here |
| 6b | Open: a run lock with a stale-reclaim rule | **substitute**, partial | A real machine-wide lock exists — `<os.tmpdir()>/photoprint-test.lock`, live pid refuses, dead pid stolen once, released in a `finally` only when the pid still matches (`reviews/lib/fix/run-scoped-tests.mjs:18-32`). Run A11: a second test process exits 3 with "another test process is running (pid …) — the machine takes one at a time". But it serialises **test runs**, not passes, and nothing serialises records writes — which is exactly how two worktrees mint the same id (story 001 row 4, **story 008**) |
| 6c | Close: the store-scoped write audit against an allowed set | **absent** | Nothing diffs a run's own writes against an allowed set at close. What exists is adjacent and narrower: `records-auditor.mjs` audits the records themselves and must exit clean before routing (`SKILL.md:31-35`), and the doc gate lints their shape. Neither asks "did this run write something it had no business writing" |
| 6d | Close: the forbidden-ground check — nothing under application source, `memory-bank/` or `docs/` was touched | **divergence** | Inapplicable by design in pre-merge mode, and overridden on purpose: the loop's fix rounds edit application source as their whole job (`SKILL.md:91`; `reviews/README.md:200-201` makes fixing a separate explicit step, not a forbidden one) |
| 6e | Close: a path-scoped commit, never `git add -A`; the lock removed on success or abort | **substitute** | The loop's commit discipline is per finding rather than per path: "One commit per finding, message names the ID" (`.claude/skills/fix-review/SKILL.md:263`, `:414`), because its commits legitimately contain source changes. Path-scoping has nothing to protect here — there is no separate store to keep out of the commit |
| 7 | The single-history rule: runs only in the integration worktree on `main` | **divergence** | Ruled the other way, verbatim: "`reviews/**` is deliberately different — its records are part of the change under review, so they ride the feature branch as the working copy and become canonical on `main` when the branch merges" (`docs/agent-systems/integration-contract.md:112-115`) |
| 8 | Profile-agnostic: runs when the active TriggerPolicy invokes it, commits per the active CommitPolicy | **substitute** | There is no policy abstraction inside the loop, and for the `solo-local` profile none is needed: the trigger is the owner's phrase (`reviews/README.md:24-30`) and the commit path is direct-to-branch. The profiles live in the contract (`integration-contract.md`, §5.5) rather than in the skill, which is the same outcome for this repo and would need work for any other |
| — | v3.7 extension 1: a **blinding auditor at launch** — inspect a hunter's inputs and refuse to dispatch if they carry prior records, finding ids or repository history | **absent** | Blinding is asked for in the lens prompt — "do NOT read anything under the `reviews/` directory, and do NOT run any git history command" (`wf.js:127-129`) — and the loop says plainly what that is worth: "Discovery is **blinded** (best-effort: enforced by prompts, unverified until the blinding auditor exists)" (`reviews/README.md:198`), repeated in the runbook (`runbook-discovery.md:14-16`, which also accepts commit messages and test names as a known leak). The extension exists precisely because a prompt is not a mechanism. **Filed as story 015** |
| — | v3.7 extension 2: a **records gate before Close** — the run does not close until its records pass the lint and the model judge | **present** | "The doc gate — before anything reaches the owner. After the records, run both halves" (`.claude/skills/loop-driver/SKILL.md:198-201`), with the verdict stamped as a `doc-gate` worklog event (`:214`) and one sitting per reviewed unit (`reviews/rules/doc-contracts.md:11-22`). Demonstrated in runs A6–A9: the gate refuses an edited block, a reason-less dismissal and an over-length summary, and passes the untouched fixture |
| — | v3.7 extension 3: **the system is a target of its own hunters**, on a schedule | **substitute**, not scheduled | It happens: `reviews/system/` holds two system reviews and a ledger of **47** `SF<n>` rows — 20 `fixed`, 18 `verified`, 4 open, 2 deferred, 2 false-positive, 1 wont-fix — matching the guide's "raised 47 findings against its own machinery" (`bug-hunter-build-guide.md:818-820`; its "fixed 18" corresponds to the 18 `verified`, with 20 more fixed and not yet re-verified). Nothing schedules these runs; both happened because a person asked. **Filed in story 015** |

**Roll-up:** every permanent slot has an occupant, the pipeline runs end to end, and two of the
three v3.7 extensions are met — against three absent rows, one of which (blinding) the loop
already flags against itself. **Satisfied with a gap** (stories 015 and, shared with bolt 085,
008 and 010).

---

## Commands run

All from `../085-phase-1-skeleton-core/evidence/rerun-evidence.sh`; output in
`evidence-log.txt` beside it. The two runs that exist for this bolt:

| # | What it runs | Result |
|---|---|---|
| A10 | the `loop-driver` eval suite's read-only verify lines | auditor exit 0 on `094-quiet` and `091-fixround`; exit 1 on `096-rotten` — `review-v2.md has no metrics line`; one warning each on `095-postcert` and `097-mockpass`; the router on `094-quiet` exits 3 at `GATE_KIND: delta-worthiness` |
| A11 | two test processes at once | the second exits 3: `another test process is running (pid …) — the machine takes one at a time` |
| A3 | the router on a loop-quiet fixture | `every manifest lens has run — loop quiet (row 6)`, exit 2 at the certification gate |
| T1–T5 | the fixture suite, whole and per seam | 836 assertions overall; 51 for the router, 46 doc gate, 43 mint-id, 171 records — all passed |

**What could not be run.** Neither story's subject is a script, so most criteria are graded on
reading plus production records. The one runnable suite that targets this skill directly —
`.claude/skills/loop-driver/evals/evals.json` — is UNRUN by its own declaration, and running its
seven judgment cases means driving live passes, which writes under `reviews/` and is forbidden to
this bolt. Its mechanical verify lines were run instead (A10).

### Acceptance criteria of this bolt

- ✅ Both stories carry a verdict rolled up from per-criterion rows, with `file:line` evidence
  and command output where runnable.
- ✅ Criterion 1 of each story was run in adapted form (A3, A10, plus the production records
  cited in rows 1a–1c).
- ✅ Every confirmed gap is a new story with `assigned_bolt: 087-phase-2-trust`, its id in bolt
  087's `stories:` list — 013, 014, 015; rows pointing at gaps bolt 085 already filed (008, 010)
  cite them instead of re-filing.
- ✅ Nothing built; the diff is Markdown under `memory-bank/`.

### Gaps filed

| Story | Assigned | From |
|---|---|---|
| `013-flow-trace-hunting` | 087-phase-2-trust | 006 row 2b |
| `014-hunter-input-hygiene` | 087-phase-2-trust | 006 rows 5, 6 |
| `015-blinding-auditor-and-scheduled-self-review` | 087-phase-2-trust | 007 extensions 1 and 3 |
| *(cited, filed by bolt 085)* `008`, `010` | 087-phase-2-trust | 007 rows 6b, 3b |

### Hand-off to the coordinator

1. **Story 006's text is incomplete against its own brief.** Two v3.3 requirements of Prompt 6 —
   source-as-data with an injection flag, and secret fingerprinting at the hunter — appear in the
   guide (`bug-hunter-build-guide.md:752-755`) and in no acceptance criterion of the story. This
   bolt graded them anyway and filed story 014; the story file itself would need amending, which
   is a records change beyond this bolt's brief.
2. **Story 003's criterion 3 needs amending, not the loop** — the dismissal-drop rule the loop
   deliberately refuses (bolt 085's report, story 003 row 3).
3. **`memory-bank/story-index.md`** — same hand-off as bolt 085: unit 001 now has 15 stories, not
   7, and every Phase 1 story carries a verdict.
4. **The guide's status table** — `general-hunter` ✓ (line 153) and `orchestrator` ✓ (line 154)
   both now have named gaps. Left as they are on the coordinator's instruction of 2026-09-04,
   which named three places to correct and these were not among them: under the table's own legend
   (`✓ built in spirit`) the rows are optimistic rather than wrong, and the paragraph added above
   the table in the same commit now says a ✓ does not mean a brief is finished. Raising them to ◐
   remains available and would change the totals line.

### Human validation checkpoints (specsmd)

Self-validated by the executing session under the wave-1 coordinator addendum.

- **Stage 1 (plan):** approved, then amended after this bolt's own stage-2 adversarial design
  check — eight attacks, all folded in (`implementation-plan.md`, "Design-check amendments"). The
  check earned its cost: it found the machine-wide test lock the plan had missed, the UNRUN eval
  suite whose history would have been a false green, the two brief requirements missing from the
  story, and the *present (instruction only)* ground that this bolt — and, retrospectively, bolt
  085 — needed.
- **Stage-4 gate (fresh-eyes micro-review):** the gate ran over bolt 085's report, whose method
  and grounds this bolt inherits, and its corrections are applied here from the start: one ground
  per row, no split grades, no claim of a record edit before it is on disk, and the evidence
  committed as a re-runnable script rather than described.
- **Stage 3 (record, this file):** approved.
