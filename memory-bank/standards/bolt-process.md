# Bolt Process

**What this is.** The canonical construction-bolt lifecycle. Until now the DDD stage structure
existed only by convention, copied from bolt to bolt; this file is the source. It extends the
observed 5-stage process with a review stage and the design-time checks that the review data
(bolts 035/042/043) showed were missing. Companion checklist: [definition-of-done.md](definition-of-done.md).

## The stages

| # | Stage | Artifact | Gate to next stage |
|---|-------|----------|--------------------|
| 1 | Domain model | `ddd-01-domain-model.md` | — |
| 2 | Technical design | `ddd-02-technical-design.md` | **Adversarial design check** (below) |
| 3 | ADR analysis *(optional)* | `adr-NNN-*.md` + entry in `standards/decision-index.md` | — |
| 4 | Implement | code + tests | **Fresh-eyes micro-review** (below) |
| 5 | Test report | `ddd-03-test-report.md` | failure-mode table carried over, filled |
| 6 | **Review** | `reviews/<bolt>/review-v1.md` (the review loop takes over — `reviews/README.md`) | — |

A bolt's frontmatter `status` may be set to **`complete` only after stage 6's first discovery
pass has run**. Bolts 044–052 were declared complete unreviewed and the defects sat for weeks
while dependent bolts consumed them; a dependent bolt building on unreviewed surface must say so
in its own `bolt.md`.

## Required reading before Stage 2 (the routing table)

Scan `standards/decision-index.md` "Read when" lines first — then, by what the bolt touches:

| If the bolt touches… | Read before designing |
|---|---|
| DB schema / migration | `data-stack.md` (PostgreSQL + migration rules) · D-o-D class 2 |
| Storage (files, keys, tiers) | ADR-007/008/009/011 · system-architecture storage section |
| Money / payments / orders | ADR-004/005 · D-o-D classes 4, 7 |
| Auth, guest sessions, interceptors | `api-conventions.md` auth headers · D-o-D class 11 |
| A background job / recovery / sweep | ADR-010/012 · D-o-D classes 6, 12 |
| Image processing / user input | D-o-D classes 3, 10 |
| A second provider behind one interface | D-o-D class 2 |
| Any new mechanism (cache/limiter/retry/event) | D-o-D rule 2 (new-mechanism bar) |

This is the same characteristics-to-attention mapping the review manifest uses to pick lenses —
used here to *prevent* what the lenses would otherwise be paid to find.

**Backlog sweep (mandatory, same sitting):** read `reviews/state/backlog.md` and filter for rows
whose Area the bolt touches. Each match is either pulled into the bolt's scope or explicitly
re-deferred with a one-line note on the row naming this bolt — never silently skipped. A row
pulled in is fixed under the normal rules (regression test, ledger write-back to its home
target, then the row is removed).

## Stage 2 — required sections in ddd-02

The historically-present sections (architecture pattern, contracts, API design, persistence,
settings, NFR/security/error handling, test plan) stay. Two sections are now **mandatory** —
their absence produced the worst findings on record:

1. **Caller-impact sweep.** For every interface, entity, key scheme, or contract this bolt
   touches: grep ALL existing consumers and list each in a table — `consumer → updated /
   unaffected because <reason>`. No blank rows. *(043's only High, F1, plus F2, were two known
   callers of `IStorageService` that the design never enumerated.)*
2. **Failure-mode list.** For every new mechanism and error path: `what can fail → what should
   happen → which test proves it (name it now) → what log line fires`. It lives as data in
   `memory-bank/bolts/<id>/failure-modes.jsonl` (row shape under "Stage exit conditions" below);
   the table in ddd-02 is rendered from it, and ddd-03 renders it again at stage 5 with the real
   test names — an empty cell there is a visible incomplete. Rows come from three sources: the
   bolt's own mechanisms (`source: bolt`), the definition-of-done classes the routing table above
   maps to what the bolt touches (`source: class`), and the shared invariants the bolt touches
   (`source: invariant`).

### The Stage-2 gate: adversarial design check

Before any code: dispatch **one adversarial agent** (~20–50k tokens) against ddd-02 *and*
`failure-modes.jsonl` with the brief "attack this design — races, resource bounds, missed callers,
failure modes absent from the list, tests that would pass for the wrong reason, second-path
asymmetry — and return every attack as a row in the list's own shape (`source: attack`)." The
author appends every returned row with a `disposition`: `accepted` (and a named test) or
`rejected` (and a reason); a check that found nothing to add is recorded as
`{"source":"attack","none":true,"reason":"..."}`. No attack row means the check did not run and
the stage cannot exit. Both deep defect chains on bolt 042 were designs that entered
implementation unchecked; a race lens reading "temp file + `File.Move`" in a design doc names the
move-target race before it costs a review round.

## Stage 4 — implement

- Required reading: the routing table above + [definition-of-done.md](definition-of-done.md) in full.
- Write the failure-mode tests from `failure-modes.jsonl` **before** the feature, and prove each
  one through the wrapper with `--log memory-bank/bolts/<id>/test-stamps.jsonl`: `--kind red`
  first (runner exit non-zero — a test that does not compile yet counts), `--kind green` after the
  code, `--kind revert-and-rerun --mutate <production file>:<line>` (the wrapper breaks that line,
  runs, restores it and records what it did; the run must be red), then `--kind green` again. Mock
  only at system boundaries (D-o-D class 5).

### The Stage-4 gate: fresh-eyes micro-review

Before hand-off to stage 5: dispatch 1–2 anchored Explore agents (fresh context) over the full
bolt diff with exactly three questions — *class or instance? new surface at the new-mechanism
bar? anything adjacent broken?* (~100–300k tokens, against the ~2M a discovery pass costs to
find the same things). A self-skim does not satisfy this gate. Findings are fixed or recorded
in ddd-03 before proceeding.

## Stage 5 — test report (ddd-03)

Keeps its historical shape (summary, files added, AC validation, issues, recommendations) plus:

- the **failure-mode table rendered from `failure-modes.jsonl`, with actual test names** in the
  "which test proves it" column;
- an **"Adversarial design check"** heading and a **"Fresh-eyes micro-review"** heading recording
  what each gate ran and found (the stage-exit check looks for both);
- a **"what this suite cannot prove"** section (provider parity, real-component gaps, CI-gated
  tests) with where each gap is covered or a pointer to the deferral;
- every bullet under its Summary, Acceptance Criteria Validation, Test Files and Issues Found
  headings names a repository path, a `file:line` or the command that shows it
  (`node .specsmd/aidlc/scripts/lint-claims.mjs <report>`; the pre-commit hook runs it).

## Stage 6 — review

Say: *"Continue the review loop for `<bolt>` per reviews/README.md."* The router there derives
the pass type. The review's requirements lens checks stage-2/5 artifacts exist and match
(the failure-mode table is filled, carried, and truthful) — the review loop enforces this
process; this process shrinks the review loop's bill.

## Stage exit conditions

**The rule.** A bolt cannot leave its design stage (ddd `technical-design`, simple `plan`) without
a complete list of failure tests, and cannot leave its `implement` stage until every one of those
tests exists, was seen red before the code, and was seen red once more with its mechanism broken
by the tool after the code. Spike bolts are exempt. The design and implementation record of this
rule is `docs/planning/prevention-first-construction-2026-09-04.md`.

**The list** is `memory-bank/bolts/<id>/failure-modes.jsonl`, one JSON object per line:

| Field | Meaning |
|---|---|
| `id` | `FM-<n>`, unique in the file |
| `source` | `bolt` · `class` · `invariant` · `attack` (the adversarial check's rows) |
| `fails` / `expected` | what can fail / what must happen instead |
| `test` | the proving test, as a fragment usable **verbatim** as the wrapper's `--filter` (API) or `--include` (UI) |
| `ui` | `true` when the test is a Vitest spec |
| `log` | the log line that fires |
| `disposition` | `accepted` (default) or `rejected`; a rejected row needs a `reason` and no test |
| `reason` | why a row is rejected, or why the bolt has no failure modes |

`{"none":true,"reason":"..."}` says the bolt has no failure modes (a docs-only bolt); it waives
the test rules, not the adversarial check. `{"source":"attack","none":true,"reason":"..."}` says
the check ran and found nothing to add.

**The proof** is `memory-bank/bolts/<id>/test-stamps.jsonl`, written only by
`reviews/lib/run-scoped-tests.mjs --log <that file>`: one line per run with the time, `kind`,
`filter`/`include`, counts, the runner's `exit` code and, for `revert-and-rerun --mutate`, the
`mutate` record (`file`, `line`, `original`, `mutated`). Red means non-zero exit. The wrapper
refuses to mutate a test file. A stamp matches a row when either name contains the other, so one
class-level red run covers the class's rows.

**The check** is `node .specsmd/aidlc/scripts/check-stage-exit.mjs <bolt> <stage>`: design/plan
— every accepted row names a test, every rejected row has a reason, every attack row has a
disposition, at least one attack row exists; implement — for every accepted test a red stamp
earlier than its first green, a red `revert-and-rerun` stamp with a non-test `mutate` record, a
green stamp after it; test — the implement conditions plus both gate headings in the test
artifact. It exits 0 with one line, or 1 with one line per unmet condition.

**Where it bites.** The commit that adds a stage to `bolt.md`'s `stages_completed` runs the check
(`.githooks/pre-commit`, through `.specsmd/aidlc/scripts/check-changed-bolts.mjs --staged`),
together with `lint-claims.mjs` over every staged stage artifact; exit 1 refuses the commit.
`STAGE_GATE_OK=1` exists for a genuine false positive and is logged like the hook's other
overrides. CI (`.github/workflows/ci.yml`, job `construction-gates`) runs the same driver against
the pull request's base with no override. The stage launcher prints the conditions into every
stage prompt and carries a failed attempt's list into the next launch of the same stage.

## Measuring whether this works

The KPI is the **severity-weighted new-findings count of each bolt's review-v1 discovery pass**
(already recorded in `reviews/<target>/metrics.jsonl`; weights 🔴 5 · 🟠 3 · 🟡 1 · ⚪ 0.5, the
same the prevention sweep uses), normalised **per thousand lines added** (`git diff --stat` of the
bolt's branch against its base) and read next to the pass's **`lenses`** count. The control group
is the wave-1 bolts in flight on 2026-09-04 (054, 057, 047-048, 066-067, 085-086), built without
the stage exit conditions and reviewed by the same reviewer in the same weeks; their review-v1
lines are the baseline, and 035/042/043 (28–53 raw v1 findings) a secondary one. Per bolt, the
construction cost (the session-cost rows) and the review cost (`cost` in the metrics lines) are
added up too: prevention that works moves findings from the review column into the construction
gates and then shrinks the total. Decision rule: of the first four bolts built under the
conditions, at least three score below the control group's median on the weighted per-KLOC
measure and their total cost is not above the control group's median. Otherwise this file is
decoration — change it.
