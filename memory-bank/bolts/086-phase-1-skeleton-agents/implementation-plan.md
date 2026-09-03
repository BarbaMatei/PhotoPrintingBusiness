---
stage: plan
bolt: 086-phase-1-skeleton-agents
created: 2026-09-03T21:30:00Z
---

## Implementation Plan: 086-phase-1-skeleton-agents (verification plan)

### Objective

Confirm on the record whether the review loop satisfies stories 006-general-hunter and
007-orchestrator-skeleton (guide Prompts 6–7) — the hunter and the six-slot coordinator — or
name exactly where it does not. Nothing is built.

### Deliverables

- `implementation-plan.md` (this file).
- `test-walkthrough.md` — the verdict, one row per acceptance criterion, story verdict as the
  roll-up; `file:line` evidence and command output where the behaviour can be run.
- A gap story per confirmed gap under
  `memory-bank/intents/035-bug-hunter-agent-system/units/001-phase-1-skeleton/stories/`, with
  `assigned_bolt: 087-phase-2-trust`, its id appended to bolt 087's `stories:` list.
- `**Status:**` lines on stories 006 and 007 updated to the recorded verdict.

### Method — inherited whole from bolt 085

The verification method, its five grounds (present · substitute · absent · N/A · divergence),
the roll-up rule, the four verdicts including *seam misattributed*, and all ten amendments the
stage-2 adversarial check forced onto it are stated in
`memory-bank/bolts/085-phase-1-skeleton-core/implementation-plan.md` and are adopted here
verbatim. The ones that bite hardest in this bolt:

- **One row per acceptance criterion** — stories 006 and 007 carry 4 + 8 = **12 criteria**.
- **Criterion 1 of each story ("the brief's three test prompts pass") is run in adapted form or
  recorded `absent — untested`**, never waved.
- **N/A is refused for anything the loop's own contract or README documents as unimplemented.**
  This matters here: `reviews/README.md:198` says discovery blinding is "best-effort: enforced
  by prompts, unverified until the blinding auditor exists" — so Prompt 7's v3.7 blinding-auditor
  extension is a gap by the loop's own admission, not standing-sweep plumbing.
- **The seam named in the `**Status:**` line is what gets checked**, and is corrected when the
  real component is elsewhere.

### What this bolt does not produce

No engine code, no `bug-hunting/` tree, no new hunter or orchestrator skill, no change under
`reviews/lib/**`, no write under `reviews/state/**`, no PPW id, no edit to a `memory-bank/`
index file, nothing under `src/`.

### Dependencies

- `docs/agent-systems/bug-hunter-build-guide.md` — Prompts 6–7 and their v3.7 extensions, plus
  the status-table rows grading both ✓.
- The two story files and their acceptance criteria.
- `docs/agent-systems/integration-contract.md` §6 (blinding at discovery; the verifier is never
  the fixer), §6.5 (never suppress).
- The seams (read-only): `reviews/lib/records/schema.mjs` (`CORE_LENSES`),
  `reviews/lib/discovery-review.wf.js` (the lens prompts), `.claude/skills/loop-driver/SKILL.md`,
  `reviews/lib/drive/route-next-pass.mjs`, `reviews/README.md`.

### Per-story evidence plan

1 - **006-general-hunter** → the core six lenses. Check that one dispatch covers both halves of
    the brief's hunter: the file sweep for local defects (null paths, boundaries, wrong
    operators, resource lifetime) and the top-down flow trace (validation, auth, error handling,
    state/transaction handling per hop). Check the candidate shape, whether dedup runs before
    emission, whether coverage is recorded, whether every plausible lead survives to the report,
    and whether the hunter is read-only on source. Adapted test prompts: the manifest-lens
    coverage read (what has and has not been hunted) run through the router; the "only new
    findings" behaviour through the reconciler's use sites in a real ledger.
2 - **007-orchestrator-skeleton** → `loop-driver` + the router + `discovery-review.wf.js`. Check
    that all six permanent slots have an occupant, that the honest-labelling requirement is met
    some way, that a per-run scope and stopping condition exist, that the trigger is pushy, and
    then the run mechanics the brief pins (run lock, stale reclaim, two-part close audit,
    path-scoped commit, single-history rule, profile-agnosticism) — each judged on whether it is
    absent, or N/A because pre-merge mode has no place for it. Then the three v3.7 extensions:
    the blinding auditor at dispatch, the records gate before close, and the system as a target
    of its own hunters. Runnable: the router end-to-end on a fixture target; the fixture suite
    scoped to routing and the policy.

### Backlog sweep (bolt-process.md stage 2)

Unchanged from bolt 085's sweep, same sitting, same files touched: `reviews/state/backlog.md`
holds 298 lines across 8 product targets and no row belonging to the review engine. Every
`records`-area row is a documentation defect of a product bolt (035/042/044-045/038-039); all
other rows are application-source areas. **All re-deferred**: none touches the files this bolt
writes. The re-deferral notes go on the rows by the coordinator at merge time — this bolt does
not edit `backlog.md`.

### Acceptance criteria

- [ ] Both stories carry a verdict rolled up from per-criterion rows, with `file:line` evidence
      and command output where runnable.
- [ ] Criterion 1 of each story is run in adapted form or recorded `absent — untested`.
- [ ] Every confirmed gap is a new story with `assigned_bolt: 087-phase-2-trust`, its id in bolt
      087's `stories:` list.
- [ ] Nothing built; the diff stays inside `memory-bank/`.

### Human validation checkpoint (stage 1)

Self-validated 2026-09-03 under the wave-1 coordinator addendum. **Outcome: approved.** The one
judgment call is inheriting bolt 085's method instead of re-deriving it — deliberate, because
the method was already attacked and amended once and a second full attack on the same method
would buy nothing. The stage-2 gate for this bolt is therefore scoped to what is genuinely new:
the two seams and whether the inherited method transfers to an agent-shaped story.
