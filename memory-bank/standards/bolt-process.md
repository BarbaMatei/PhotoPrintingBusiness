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
| DB schema / migration | `data-stack.md` (dual-provider rules) · D-o-D class 2 |
| Storage (files, keys, tiers) | ADR-007/008/009/011 · system-architecture storage section |
| Money / payments / orders | ADR-004/005 · D-o-D classes 4, 7 |
| Auth, guest sessions, interceptors | `api-conventions.md` auth headers · D-o-D class 11 |
| A background job / recovery / sweep | ADR-010/012 · D-o-D classes 6, 12 |
| Image processing / user input | D-o-D classes 3, 10 |
| A second provider behind one interface | D-o-D class 2 |
| Any new mechanism (cache/limiter/retry/event) | D-o-D rule 2 (new-mechanism bar) |

This is the same characteristics-to-attention mapping the review manifest uses to pick lenses —
used here to *prevent* what the lenses would otherwise be paid to find.

## Stage 2 — required sections in ddd-02

The historically-present sections (architecture pattern, contracts, API design, persistence,
settings, NFR/security/error handling, test plan) stay. Two sections are now **mandatory** —
their absence produced the worst findings on record:

1. **Caller-impact sweep.** For every interface, entity, key scheme, or contract this bolt
   touches: grep ALL existing consumers and list each in a table — `consumer → updated /
   unaffected because <reason>`. No blank rows. *(043's only High, F1, plus F2, were two known
   callers of `IStorageService` that the design never enumerated.)*
2. **Failure-mode table.** For every new mechanism and error path: `what can fail → what should
   happen → which test proves it (name it now) → what log line fires`. This table is copied
   into ddd-03 at stage 5 with the real test names filled in — an empty cell there is a visible
   incomplete.

### The Stage-2 gate: adversarial design check

Before any code: dispatch **one adversarial agent** (~20–50k tokens) against ddd-02 with the
brief "attack this design — races, resource bounds, missed callers, failure modes absent from
the table, second-path asymmetry." Fold what it finds into the design or record why not. Both
deep defect chains on bolt 042 were designs that entered implementation unchecked; a race lens
reading "temp file + `File.Move`" in a design doc names the move-target race before it costs a
review round.

## Stage 4 — implement

- Required reading: the routing table above + [definition-of-done.md](definition-of-done.md) in full.
- Write the failure-mode tests from the ddd-02 table **with** the feature, not after it. Mock
  only at system boundaries (D-o-D class 5).

### The Stage-4 gate: fresh-eyes micro-review

Before hand-off to stage 5: dispatch 1–2 anchored Explore agents (fresh context) over the full
bolt diff with exactly three questions — *class or instance? new surface at the new-mechanism
bar? anything adjacent broken?* (~100–300k tokens, against the ~2M a discovery pass costs to
find the same things). A self-skim does not satisfy this gate. Findings are fixed or recorded
in ddd-03 before proceeding.

## Stage 5 — test report (ddd-03)

Keeps its historical shape (summary, files added, AC validation, issues, recommendations) plus:

- the **failure-mode table from ddd-02, with actual test names** in the "which test proves it"
  column;
- a **"what this suite cannot prove"** section (provider parity, real-component gaps, CI-gated
  tests) with where each gap is covered or a pointer to the deferral.

## Stage 6 — review

Say: *"Continue the review loop for `<bolt>` per reviews/README.md."* The router there derives
the pass type. The review's requirements lens checks stage-2/5 artifacts exist and match
(the failure-mode table is filled, carried, and truthful) — the review loop enforces this
process; this process shrinks the review loop's bill.

## Measuring whether this works

The KPI is the **severity-weighted new-findings count of each bolt's review-v1 discovery pass**
(already recorded in `reviews/<target>/metrics.jsonl`). Bolts built under this process should
show it falling versus 035/042/043 baselines (28–53 raw v1 findings). If it doesn't fall, this
file is decoration — change it.
