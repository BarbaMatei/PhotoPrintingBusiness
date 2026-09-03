# Future Systems — the full agentic-org map

> **Status: living map (2026-06-15).** The complete vision of the AI software organization — what
> exists, what's specced, and what's captured-for-later — so no brainstormed piece is lost and each
> one knows its neighbours. The deferred entries are **concept notes, not specs**: enough to start
> clean when their time comes, no more. Build in roadmap order; capture out of order.

---

## The layers (and where the human stays)

As the org automates, the human role compresses toward two irreducible things — **deciding what
matters** (intent + priorities) and **ratifying** at checkpoints. Everything else is a layer of
automation:

| Layer | Job | Systems |
|---|---|---|
| **Decide** | what to build, what matters | **the human (owner)** — irreducible |
| **Coordinate** | analyse → prioritise → schedule → run the work | **Analyst** → **Conductor** → **Planner** → **Wave-orchestrator** (half already built — see below) |
| **Do** | build · review · inspect · know · test | Builder · Reviewer (partial) · Inspector (partial) · Librarian · Test-Quality\* |
| **Operate** | watch the running product, feed incidents back | **Observability** *(roadmap-gated)* |

\* planned/deferred. Partial = exists inside the review loop, see the roster.

The **coordinate layer is a pipeline, and it's half-built today**:
`Analyst → Conductor → (human ratifies) → AI-DLC inception → Planner → Wave-orchestrator → Builder`.
The **Planner** (`bolt-parallel-planner`) and **Wave-orchestrator** (`bolt-wave-orchestrator`) already
exist; the **Analyst** exists as a proto (`architect-analyst`); the only true gap is the **Conductor**.
See [conductor-system.md](conductor-system.md) for the pipeline and the planner-refinement plan.

## The roster

| System | Role | Status | Note |
|---|---|---|---|
| **AI-DLC / specsmd** | Builder — specs + code from intent | **built / installed** | — |
| **bug-hunter** | Inspector — finds defects | **partially built** — the review loop under `reviews/` is this engine running in pre-merge mode: Phase 1 complete, Phases 2/4/5 half built, Phase 3 (map, breadth) missing; 12 of 43 briefs built, 15 partial, 16 missing | [guide](../bug-hunter-build-guide.md) · [status table](../bug-hunter-build-guide.md#implementation-status-2026-09) · [cross analysis](../theory-vs-practice-2026-09.md) |
| **knowledge-builder** | Librarian / oracle — distils intent → contracts | **specced, ready to build** | [guide](../knowledge-builder-build-guide.md) |
| **code-review** | Reviewer — pre-merge, diff-scoped gate | **partial, unplanned** — three of five dimensions run as lenses of the review loop (`requirements`, `quality`, `tests-coverage`); verdict synthesis and contract fidelity not built | [concept](code-review-system.md) |
| **analyst** | Architectural review — scans the system, detects gaps, proposes ranked improvements (feeds the Conductor) | **partial** — `architect-analyst` agent exists | [concept](analyst-system.md) |
| **conductor** | Planning conductor — aggregates all signals → proposes ranked next-work queue | **planned (deferred)** | [concept](conductor-system.md) |
| **planner** | Execution planner — decided bolts → conflict-safe wave schedule + kickoff prompts | **built** — `bolt-parallel-planner` agent | [concept](planner-system.md) |
| **wave-orchestrator** | Executes one wave — spawns instances, verifies, opens PRs | **built** — `bolt-wave-orchestrator` agent | [concept](planner-system.md) |
| **test-quality** | QA-author — builds & *judges* the safety net (coverage, mutation, e2e) | **planned (deferred)** | [concept](test-quality-system.md) |
| **observability** | SRE — watches the running product, turns incidents into fix-requests | **roadmap-gated (post-deploy)** | [concept](observability-system.md) |

## How the future pieces connect to what exists

- **The review loop is the Inspector's engine in pre-merge mode.** It gates every bolt today (all
  eleven lenses) and holds the ledger, the fix verification and the certification record. The
  standing-sweep mode the guide describes — a scheduled pass over the whole codebase on `main` — is
  the same engine's second mode, not built yet: the owner's ruling of September 2026 that the two
  postures are one engine (see [the reconciliation](../theory-vs-practice-2026-09.md)).
- **Conductor** sits *above* all the doing-systems: it reads their outputs (the bug ledger, the
  oracle's drift flags, the fix-request mailbox, the feature backlog, tech debt) and *proposes* what
  AI-DLC should work on next. It sits above the existing **execution** planners
  (`bolt-parallel-planner` / `bolt-wave-orchestrator`), which run work that's already been decided.
- **Reviewer** gates AI-DLC's bolts *and* the bug-hunter's `fix-proposal` patches before they land;
  reads the oracle for intent-fidelity.
- **Test-Quality** judges the suite the Builder and `regression-harvest` produce; maps to the
  roadmap's "full e2e/regression" phase.
- **Observability** feeds the loop exactly like the inspector does — an incident becomes a
  fix-request keyed by `correlation_id` — but sourced from the *running* product, post-deployment.

## Sequencing (capture now, build in order)

Per the pre-deployment roadmap (bolts → AI infra → e2e/regression → 3-env → EU readiness → deploy)
and the 2026-09 reconciliation: first the cheapest Inspector gaps (a run budget with metered fix
rounds, the proof rule for high-severity findings, deterministic scanner ingest), then the Map slot
and the standing-sweep mode, then the knowledge-builder only if intent-drift findings appear; the
Reviewer's remaining dimensions and the Conductor follow; Test-Quality aligns with the
e2e/regression phase; Observability is strictly post-deployment. These notes exist so that when each
phase arrives, the design is already on paper — not re-derived from memory.
