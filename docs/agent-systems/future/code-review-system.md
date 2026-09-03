# Future System — Code-Review Agentic System (concept note)

> **Status: PARTIALLY BUILT, UNPLANNED (2026-09).** Three of the five dimensions below run today as
> lenses of the review loop (`reviews/`): intent fidelity as `requirements`, design quality as
> `quality` (report-only), test adequacy as `tests-coverage`. The other two — standards adherence
> and comment/doc accuracy — are not built. The `requirements` lens reads the bolt's own documents
> rather than a distilled contract, so contract fidelity (the same intent-fidelity dimension,
> checked against an oracle instead of against the bolt's own text) is still on paper. The verdict
> exists (`request-changes` / `approve-with-followups` / `approved`) but is a loop outcome, not the
> accept / block / revise synthesis described here. The rest of this note stands as the design for
> the remainder.

---

## The gap it fills

The three current systems map to roles in a software organization:

| Role | System | When it acts | On what |
|------|--------|--------------|---------|
| Builder | AI-DLC / specsmd | writes specs + code | new work |
| Inspector / QA | bug-hunter | periodic runs | the **whole standing codebase** (read-only) |
| Librarian / oracle | knowledge-builder | distillation runs | intent contracts |
| **Reviewer** | **partial** — three dimensions run as lenses of the review loop (`reviews/`); the rest deferred | **at the moment of change** | **one diff / bolt** |

The review loop under `reviews/` gates every bolt *at merge time* today, but with only three of
these dimensions (intent fidelity, design quality, test adequacy); standards adherence, comment/doc
accuracy and the accept / block / revise synthesis are still missing. The bug-hunter is post-hoc and
**defect-focused**; it runs over standing state and finds bugs. A reviewer is **pre-merge** and
**change-focused**: it judges whether *this specific diff* should be accepted at all. The classes it
catches — intent drift, design degradation, standards violations, missing test coverage, comment rot
— are exactly the things the bug-hunter structurally does not look for, and catching them before
they land is the cheapest quality gate in the pipeline.

## The make-or-break constraint: dimension disjointness

A reviewer that "looks for bugs in the diff" just duplicates the bug-hunter and breaks the
separation-of-powers discipline the whole architecture depends on (twin-name rules, "judgment agents
never shared"). The reviewer must be **dimension-disjoint** from the bug-hunter:

- **Reviewer owns:** intent fidelity (does the change match its bolt/story contract?), design quality
  (type design, encapsulation, API surface, simplification, naming, pattern consistency), standards
  adherence (`memory-bank/standards/`), test adequacy, comment/doc accuracy.
- **Reviewer defers to the bug-hunter:** finding latent defects anywhere in the corpus.
- **Resolved (2026-09): error-handling / silent-failure review belongs to the Inspector.** The
  review loop's `observability` lens (swallowed exceptions, indistinguishable incident types,
  partial-state failures) already owns it, and it is defect-hunting by nature. The Reviewer does not
  look for it.

## What exists today (2026-09)

On the two features whose records carry per-finding lens data (152 serious findings), the three
reviewer lenses alone caught 21%, the defect lenses alone 56%, both kinds 12.5%. The reviewer
dimensions earn their place pre-merge; they do not need a separate system to run. What this note
still adds: the two missing dimensions, the accept / block / revise synthesis on top of the loop's
verdict, and reading intent from a contract rather than from the bolt's own text. Source:
[theory-vs-practice-2026-09.md §3](../theory-vs-practice-2026-09.md).

## Why it's the cheapest system to add: compose, don't build

Unlike the other two systems (built ground-up via skill-creator), the reviewer's **worker layer is
already installed as plugins**:

- `pr-review-toolkit` → `code-reviewer`, `type-design-analyzer`, `comment-analyzer`,
  `pr-test-analyzer`, `silent-failure-hunter`, `code-simplifier` — the dimension specialists.
- `code-review` (diff correctness + `--comment` / `--fix`) and the `review-pr` orchestration skill.
- `code-simplifier` (standalone).

So the reviewer ≈ **orchestration + integration + verdict synthesis** wrapped around existing plugins.
The governance split (consistent with the skill-creator mandate):

- **Compose plugins** for generic, project-agnostic dimensions (type smells, comment accuracy, test
  coverage shape).
- **Build with skill-creator** the integration-sensitive parts: the orchestrator, the **oracle
  consumer** that checks intent-fidelity against *this* project's knowledge ledger, the contract-aware
  glue, and the **verdict synthesizer**.

**Critical adaptation:** the `pr-review-toolkit` agents *advise* (they comment/suggest) — built for a
human in the loop. A gate in a closed loop needs a **decision** (accept / block / revise-and-resubmit).
The synthesis layer that turns advisory prose into a verdict is the part you build; the human
checkpoint sits on that verdict.

## Closed-loop integration sketch (for later)

- **Gates two kinds of diff:** AI-DLC's bolts (pre-merge) **and** the bug-hunter's `fix-proposal`
  patches (BH Prompt 32) before they're applied — the fix loop currently has no review step on its
  own proposals.
- **Reads:** the knowledge ledger (oracle, for intent fidelity), the bug ledger (to avoid re-flagging
  known defects — that's the bug-hunter's job), `memory-bank/standards/`.
- **Writes:** a review verdict, keyed by the bolt's `correlation_id`.
- **Human checkpoint:** on the verdict (the only human in the org, per the standing design).

## Integration-contract delta (when built)

- A new store (e.g. `code-review/`) + a sole-writer-map row.
- A **third co-resident system** in the integration worktree — the cross-system mutex, store-scoped
  audit + forbidden-ground check, and path-scoped publish-commit (contract §1/§5) all extend from a
  two-system to a three-system model. *(Or: the reviewer runs as a pre-merge CI gate, read-only on the
  stores, with a lighter footprint — decide which.)*
- A twin-name entry if any skill name collides with an existing one.

## When picked up — two ways in (pick one; don't over-spec)

1. **Lean spec** — one orchestrator brief, the dimension→plugin mapping, the contract delta, the
   verdict-synthesis design. Stop there.
2. **Thin slice (preferred)** — wire `review-pr` + the oracle into a single "review this bolt against
   its contract" pass, run it on one real bolt, and let the result tell you what the spec should say.

The disjointness question was settled in 2026-09 (error-handling review belongs to the Inspector);
start from that.

## Non-goals for now

No inception, no bolts, no skill-creator build, no contract edits. This file exists only so the idea
survives until the other three systems are real.
