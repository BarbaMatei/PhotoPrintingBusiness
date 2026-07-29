# System — Execution Planner (& Wave-Orchestrator) concept note

> **Status: BUILT — already exists as two agents** (`.claude/agents/bolt-parallel-planner.md` and
> `.claude/agents/bolt-wave-orchestrator.md`). This note folds them into the agentic-org architecture
> and records the refinements to make them first-class members of it. Part of the
> [future-systems map](README.md). (Filed under `future/` because the *integration* into the systems
> vision is what's pending — the agents themselves work today.)

---

## The role

The **execution half** of the coordinate layer — it does not decide *what* to build, it decides *how
to run* already-decided work safely in parallel:

- **Planner** (`bolt-parallel-planner`) — takes the planned/unimplemented bolts, groups them into
  branch-sized, conflict-safe batches, and produces a **wave schedule**: dependency-ordered, with a
  conflict matrix, EF-migration isolation, merge order, and ready-to-paste **kickoff prompts** per
  instance. Example output: [bolt-parallel-plan-2026-06-05.md](../../planning/bolt-parallel-plan-2026-06-05.md).
- **Wave-Orchestrator** (`bolt-wave-orchestrator`) — executes **one wave** of that plan: pre-flight
  checks, creates the worktrees/branches, launches one implementation instance per group, verifies
  their work, pushes branches, opens PRs, and reports the merge order. Its execution partner.

Command reference for both: [agent-commands.md](../../planning/agent-commands.md).

## Where it sits

The coordinate-layer pipeline (see [conductor-system.md](conductor-system.md)):

```
Analyst → Conductor → (human ratifies) → AI-DLC inception → Planner → Wave-Orchestrator → Builder
                                          (decision → bolts)  (group     (run one wave)
                                                                into waves)
```

So the Planner sits **below** the Conductor and AI-DLC inception: it schedules bolts that already
exist as decided work; it never originates work. The Wave-Orchestrator sits below the Planner: it runs
what the Planner scheduled.

## Refining them ("redo it better") — fold them into the architecture

Both agents predate the systems framing, so the work is *integration*, not rebuild:

1. **Profile-aware (the big one).** The current plan hard-codes a commit/merge policy — its §0 says
   *"PR-based — per user decision, no direct push to main."* That is exactly a **CommitPolicy**
   decision (Integration Contract §5.5), not a planning fact. It should come from the **active
   operating profile**, so the same Planner serves `solo-local` (`direct-to-main`) and `team-ci`
   (`pr-auto-merge`) without rewriting the plan, and the Wave-Orchestrator commits/PRs per that policy.
2. **Contract-aware.** Wave/worktree planning should honour the single-history / serialization rules
   and the migration-collision logic **by reference to the contract (§1, §5.5)** rather than
   re-deriving them in each plan.
3. **Conductor-fed.** Today the Planner takes *every planned bolt*. Once the [Conductor](conductor-system.md)
   exists, it should take the Conductor's **prioritised, ratified queue** — so wave order reflects
   priority, not just dependency/conflict topology.

## Disjointness (vs its neighbours)

- vs **Conductor:** the Conductor decides *what* (and in what priority); the Planner decides *how to
  run* it in parallel. Different verbs.
- vs **AI-DLC inception:** inception turns a decision into bolts; the Planner schedules bolts that
  already exist.
- vs **Wave-Orchestrator:** the Planner *plans*; the Orchestrator *executes* one wave.
- **Note — drift detection overlap:** the Planner currently also reports drift (e.g. bolts marked
  `planned` that actually shipped, stale `story-index` lines). That's *git/bolt-status* consistency,
  narrower than the Librarian's *intent* drift — keep it, or later delegate it to the Librarian; flag
  so the two don't silently diverge.

## Open questions (resolve when integrated)

- Where the profile is read from at plan time (env / a repo config / the contract's active-profile
  line).
- Whether the Wave-Orchestrator warrants its own note once it grows profile-specific commit logic, or
  stays documented here as the Planner's partner.
- How the Planner's bolt-status drift findings reconcile with the Librarian's intent-drift once both
  exist.

## Connections

Below the [Conductor](conductor-system.md) and AI-DLC inception; feeds the Builder. Honours the
operating profile and single-history rules ([contract §1/§5.5](../integration-contract.md)).
Existing implementation: `.claude/agents/bolt-parallel-planner.md`,
`.claude/agents/bolt-wave-orchestrator.md`; reference [agent-commands.md](../../planning/agent-commands.md).
