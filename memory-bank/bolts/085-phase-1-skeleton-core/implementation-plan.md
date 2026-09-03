---
stage: plan
bolt: 085-phase-1-skeleton-core
created: 2026-09-03T20:38:46Z
---

## Implementation Plan: 085-phase-1-skeleton-core (verification plan)

### Objective

Confirm on the record, story by story, that the review loop under `reviews/` satisfies stories
001–005 of unit 001-phase-1-skeleton (guide Prompts 1–5) — or name exactly where it does not.
Nothing is built. The deliverable is a verdict table with evidence.

### Deliverables

- `implementation-plan.md` (this file) — the verification plan.
- `test-walkthrough.md` — the verdict table: one row per story, verdict ∈ {satisfied ·
  satisfied with a gap (named) · not satisfied}, each backed by `file:line` and, where the
  behaviour can be run, command output.
- One new gap story per confirmed gap under
  `memory-bank/intents/035-bug-hunter-agent-system/units/001-phase-1-skeleton/stories/`, with
  `assigned_bolt: 087-phase-2-trust`, and its id appended to bolt 087's `stories:` list.
- `**Status:**` lines on stories 001–005 updated from "claimed … to be verified" to the recorded
  verdict; `status`/`implemented` frontmatter set for stories whose verdict is satisfied.

### What this bolt does not produce

No engine code. No `bug-hunting/` tree. No change of behaviour under `reviews/lib/**`, no write
under `reviews/state/**`, no PPW id, no edit to any `memory-bank/` index file, nothing under
`src/`.

### Dependencies

- `docs/agent-systems/bug-hunter-build-guide.md` — Prompts 1–5 (the briefs being checked) and
  the "Implementation status (2026-09)" claim table.
- `memory-bank/intents/035-bug-hunter-agent-system/units/001-phase-1-skeleton/stories/00{1..5}-*.md`
  — acceptance criteria and the `**Status:**` line naming each seam.
- `reviews/README.md` — the loop's conventions, router and entry tiers.
- The seams themselves (read-only): `reviews/lib/records/ledger.mjs`,
  `reviews/lib/review/mint-id.mjs`, `reviews/lib/records/render-records.mjs`,
  `.claude/skills/reconcile-findings/SKILL.md`, `reviews/templates/review.md`,
  `reviews/templates/summary.md`, `reviews/lib/records/doc-gate.mjs`,
  `reviews/lib/drive/gates.mjs`.

### Technical approach

**The claim under test.** The guide's status table asserts an *equivalence in spirit*, not a
literal port: the bug-hunter briefs describe a standing sweep writing `bug-hunting/**`, the
review loop is the same engine in pre-merge mode writing `reviews/<target>/**`. So the pinned
paths (`bug-hunting/bug-ledger.json`, `bug-report-run-NN-*.md`), the skill names, and the
skill-creator build route are **not** what is being checked — story acceptance criteria carry
the 2026-09 amendment saying a component that extends the review loop is a script or skill edit
under `reviews/lib` / `.claude/skills`. What is checked per criterion is the **mechanism**: does
the loop have a component that does this job, and does it do it?

Each criterion is therefore judged on one of three grounds, and the walkthrough says which:

1 - **mechanism present** — the loop has the behaviour, under its own names/paths.
2 - **mechanism absent** — no component does this job → the criterion fails and the gap is named.
3 - **not applicable in pre-merge mode** — the criterion describes standing-sweep plumbing the
    pre-merge engine has no place for (a run lock over a whole-codebase sweep, an
    `application_map` section, `bug-hunting/` path pinning). Recorded as N/A **with its reason**,
    never silently counted as satisfied. Where the *purpose* behind such a criterion is served
    another way, the walkthrough names the substitute.

**Verdict rule per story.** *satisfied* = every criterion is mechanism-present or N/A-with-reason.
*satisfied with a gap* = the mechanism exists and is used, but a named sub-behaviour is missing.
*not satisfied* = the claimed seam does not do the job at all.

**Evidence rule.** Every verdict row carries `file:line` for the mechanism and, where the
behaviour is executable, the command that exercises it plus its output. Runnable here:
`node reviews/lib/tests/run-tests.mjs` (the fixture suite covering ledger, mint-id,
render-records, doc-gate, routing and the gates) and read-only/`--dry-run` entry points of the
lib scripts. Nothing that writes under `reviews/state/**` or `reviews/<target>/**` is run.

**Per-story evidence plan**

1 - **001-ledger-io** → `reviews/lib/records/ledger.mjs` (ledger read/parse),
    `reviews/lib/review/mint-id.mjs` (id minting: stable, never reused, collision-safe across
    parallel worktrees), `reviews/lib/records/render-records.mjs` (the single writer that
    publishes rows from the worklog). Check: one owner of ledger reads/writes; ids atomic and
    never reused; parallel-writer safety; no lost data; the human view regenerated rather than
    hand-edited. Run the id and records unit tests.
2 - **002-bug-documentation** → `reviews/lib/records/render-records.mjs` plus the record shape in
    `reviews/lib/records/schema.mjs` and `reviews/rules/doc-contracts.md`. The status table
    already grades this ◐ ("ledger row + fix brief, no three-audience record"). Check whether the
    required-field set is enforced anywhere, and which of the three audiences the loop's record
    actually serves. Expect a gap; name it precisely.
3 - **003-deduplication** → `.claude/skills/reconcile-findings/SKILL.md`. Check: same-problem vs
    new decision with confidence and reason, split-when-unsure, lineage (`residual-of`,
    `seed_round`, `area`), `hinted` marking, decision attachment instead of suppression, and the
    ground-truth scoring gate. Cross-check the lineage fields exist in the metrics schema.
4 - **004-report-rendering** → `reviews/templates/review.md`, `reviews/templates/summary.md`,
    `reviews/lib/records/doc-gate.mjs`. Check: a new file per pass (never overwritten), severity
    ordering, the reporting floor (what the loop's equivalent of "Low in an appendix" is), the
    60-line owner-summary cap and its refusal, the deterministic lint plus model judge, and
    "zero findings is a valid pass". Run the doc-gate tests; run the doc gate read-only against
    an archived target's records if a read-only entry point exists.
5 - **005-triage-intake** → `reviews/lib/drive/gates.mjs` plus
    `reviews/lib/drive/autonomy-policy.mjs` and the README's "Unattended runs" section. Check:
    a defined owner-decision channel; decisions recorded with provenance; parked decisions with
    the default that was taken, surfaced in the run-end report; reason-less dismissal handling;
    decisions attached to re-found findings rather than suppressing them.

### Backlog sweep (bolt-process.md stage 2)

`reviews/state/backlog.md`, 298 lines, 8 targets, no row belonging to the review engine itself.
Areas this bolt touches: none of the product areas. The only area word that could be argued is
`records` (29 rows) — every one of those rows is a documentation defect in a **product** bolt's
records (035, 042, 044-045, 038-039 ddd docs, ADRs, standards), not in intent 035's planning
docs or in `reviews/lib`.

| Rows | Area | Ruling |
|---|---|---|
| PPW-12, 131, 335, 371, 390, 393, 402, 421, 422, 433, 436, 437, 497, 539, 548, 572, 577, 601, 619, 623, 627, 630, 631, 640, 641, 643, 644, 650, 656 | `records` | re-deferred: all are record defects of product targets (035/042/044-045/038-039); none touches the files this bolt writes (intent-035 stories, bolt folders) and fixing them would edit another group's files |
| all other rows | `uploads`, `jobs`, `tests`, `observability`, `data`, `payments`, `orders`, `shipping`, `edge`, `gallery`, `auth` | re-deferred: application-source areas; this bolt is read-only on `src/` |

The re-deferral notes are written on the rows by the coordinator at merge time — this bolt does
not edit `reviews/state/backlog.md`.

### Acceptance criteria

- [ ] Stories 001–005 each carry a verdict in `test-walkthrough.md` with `file:line` evidence and,
      where runnable, command output.
- [ ] Every criterion judged N/A carries its reason and, where one exists, the substitute
      mechanism.
- [ ] Every confirmed gap exists as a new story file with `assigned_bolt: 087-phase-2-trust`, and
      its id appears in `memory-bank/bolts/087-phase-2-trust/bolt.md`'s `stories:` list.
- [ ] `**Status:**` lines on stories 001–005 state the recorded verdict.
- [ ] `git diff origin/main...HEAD --name-only` shows nothing under `src/`, `reviews/lib/`,
      `reviews/state/`, or any `memory-bank/` index file.
- [ ] No `bug-hunting/` tree, no new skill, no engine code.

### Human validation checkpoint (stage 1)

Self-validated 2026-09-03 under the wave-1 coordinator addendum (specsmd checkpoints are
validated by the executing session and the outcome recorded in the artifact).
**Outcome: approved.** The plan's one judgment call — the three-ground rule that lets a
standing-sweep-only criterion be recorded N/A rather than failed — is the whole reason this
verification is meaningful rather than a rubber stamp, so it is stated up front and every N/A
must carry its reason in the walkthrough.


### Design-check amendments (the stage-2 gate, bolt-process.md)

One adversarial agent was dispatched against this plan with the brief "attack this verification
plan — where can a satisfied verdict be reached without evidence, where is the N/A hatch too
wide, what is claimed runnable that is not". It returned ten attacks. All ten are folded in
below; none was declined.

1 - **One row per acceptance criterion, not per story.** The story verdict is a roll-up of its
    criteria rows, not a free-standing judgment. Unit 001's five stories carry 28 criteria
    (7+5+5+6+5 across 001–005 and 006–007); every one gets a row with its own ground.
2 - **Criterion 1 of every story ("the brief's test prompts pass") is run or failed, never
    waved.** Each brief's test prompts are translated into a concrete run against a throwaway
    tree or an archived target and the output recorded; where no adaptation is possible the
    criterion reads **not satisfied — untested**, never N/A.
3 - **A fourth verdict: *satisfied, seam misattributed*.** Two `**Status:**` lines name a file
    that cannot do the job: `reviews/lib/records/ledger.mjs` is a 36-line reader whose own
    header says "this file only reads", and `reviews/lib/drive/gates.mjs` is 40 lines of
    exported constants that "Reads nothing". Where the behaviour exists elsewhere, the verdict
    says so and names the real component; the story's `**Status:**` line is corrected.
4 - **N/A is refused for anything the loop's own contract documents as unimplemented.**
    `docs/agent-systems/integration-contract.md` is added to the dependencies; its §1
    (lines 106–115) states the id reservation for parallel worktrees is *not in place* and the
    duplicate-mint guard is same-target-only. That is a gap, not standing-sweep plumbing.
5 - **`--dry-run` proves nothing about write safety** — it is the branch that skips the write
    (`mint-id.mjs:53`). Write-safety criteria are exercised for real in a throwaway tree via
    the `--root` flag the scripts already support, and the fixture suite is cited only as what
    it is: argument handling and refusals, with no concurrency case
    (`grep -c 'concurren\|parallel\|race\|reuse' reviews/lib/tests/unit/mint-id.test.mjs` → 0)
    and no `ledger.test.mjs` at all.
6 - **A fifth ground: *deliberate divergence*.** Story 003's "dismissed entries drop" is
    something the loop refuses on purpose (README "never suppress"; integration contract §6.5).
    That is recorded as a divergence with the ruling cited — and it is the story text that
    needs amending, not the loop.
7 - **Story 004 is checked against its six criteria verbatim**, on the confidence axis its own
    v3.2 note fixes — not the severity axis this plan first wrote.
8 - **The reconciler's ground-truth score is checked for staleness**, not cited: `git log` on
    the skill file against the recorded score date.
9 - **Stage 4 (the review pass) runs centrally after merge**; this bolt hands back at
    `status: review-pending`, never `complete`. Running it here would append a
    `reviews/state/index.md` row and mint ids — both forbidden to this bolt.
10 - **`memory-bank/story-index.md` will go stale** the moment gap stories are added and the
    `**Status:**` lines change. This bolt may not edit any index file, so the re-roll is named
    explicitly as a coordinator hand-off item in the walkthrough and the final report.

Two smaller points, recorded rather than actioned: the gap of story 002 is filed **once** — by
re-assigning story 002 itself to bolt 087 (the route bolt 085's own stage 5 prescribes), not by
minting a duplicate gap story; and the guide's summary line "Phase 1 is done"
(`docs/agent-systems/bug-hunter-build-guide.md:128`) contradicts its own table row grading
`bug-documentation` ◐ (line 140) — recorded as a correction owed to the claim under test, for
the coordinator, since `docs/` is outside this bolt's write set.
