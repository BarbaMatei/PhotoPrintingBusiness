# Future System — Conductor (planning / "engineering manager") concept note

> **Status: PROPOSED / DEFERRED (captured 2026-06-15).** Idea record, not a spec. Build only once ≥2
> of the systems it conducts exist (it has nothing to coordinate before then). Part of the
> [future-systems map](README.md).

---

## The gap

You have specialist systems that each emit signals — the **Inspector** emits confirmed bugs, the
**Librarian** flags intent drift, the loop emits fix-requests, and there's a feature backlog, tech
debt, and dependency upgrades. **Nothing aggregates and prioritises across all of them.** Today *the
human* is the integration point: you decide, in your head, what to work on next. That is the single
largest pool of remaining human intervention — not the *doing*, the **coordinating**.

## The role

A **planning conductor** that, on demand or on a cadence:
1. reads every system's output (bug ledger, oracle drift flags, fix-request mailbox, feature backlog,
   tech-debt/dependency signals),
2. de-duplicates and **prioritises** them into a single ranked next-work queue,
3. **proposes** that queue to the owner for approval, and
4. on approval, hands the chosen items to **AI-DLC inception** (which turns them into bolts).

## Where it sits

*Above* the doing-systems, and *above* the existing **execution** planners
(`bolt-parallel-planner` / `bolt-wave-orchestrator`) — those run work already decided; the conductor
decides *which* work becomes bolts in the first place. So: Conductor (what to do) → AI-DLC inception
(turn into bolts) → execution planners (how to run the bolts) → Builder.

## The coordinate layer — and what already exists

The Conductor isn't alone; it's the centrepiece of a **coordinate-layer pipeline** that sits between
"the human decides" and "the systems do." Two of its four roles **already exist as agents** — this
layer is half-built:

```
Analyst ──→ Conductor ──→ (human ratifies) ──→ AI-DLC inception ──→ Planner ──→ Wave-orchestrator ──→ Builder
(propose      (aggregate                          (turn into bolts)   (group into   (run one wave)
 gaps)         + prioritise)                                           safe waves)
```

| Role | Job | Status |
|---|---|---|
| **Analyst** | proactive architectural review → gap/improvement candidates | **partial** — `architect-analyst` agent exists ([note](analyst-system.md)) |
| **Conductor** | aggregate *all* candidates + prioritise → ranked queue for ratification | **gap** (this note) |
| **Planner** | decided/incepted bolts → conflict-safe wave schedule + kickoff prompts | **built** — `bolt-parallel-planner` ([note](planner-system.md)) |
| **Wave-orchestrator** | execute one wave (spawn instances, verify, open PRs) | **built** — `bolt-wave-orchestrator` ([note](planner-system.md)) |

So the **only true gap is the Conductor** (the prioritising aggregator); the Analyst needs evolving;
the execution half works today.

The Planner and Wave-orchestrator already exist; folding them cleanly into this layer (profile-aware
commit policy, contract-aware serialization, Conductor-fed input) is covered in their own note —
[planner-system.md](planner-system.md).

## The hard rule (non-negotiable)

**It proposes; it never decides.** *Deciding what matters* is the human's irreducible role (the goal
is *minimal* intervention, not zero). The conductor's output is a ranked plan for ratification, with
its reasoning shown — never an autonomous commitment of work.

## Reads / writes

- **Reads:** `bug-hunting/**` (open bugs), `knowledge/**` (drift, contested contracts),
  `bug-hunting/fix-requests/`, the feature backlog (location TBD), `memory-bank/**` (in-flight work).
- **Writes:** a proposed work-queue artifact for the owner; on approval, feeds AI-DLC inception. No
  cross-store writes (sole-writer discipline holds).

## Open questions (resolve when picked up)

- Where does the **feature backlog** live as a first-class store the conductor can read?
- How is **priority scored** (severity × reachability × business value × staleness…)? Who tunes it?
- Exact handoff to **AI-DLC inception** — does the conductor emit intents, or annotated candidates?
- Does it participate in the operating-profile triggering (e.g. propose after each loop run), or stay
  on-demand?

## Connections

Consumes: [bug-hunter](../bug-hunter-build-guide.md), [knowledge-builder](../knowledge-builder-build-guide.md),
the fix loop ([contract §4](../integration-contract.md)), and (later) the
[Reviewer](code-review-system.md) and [Test-Quality](test-quality-system.md) outputs.
Sits above the owner's execution planners. The human checkpoint is on its proposed plan.
