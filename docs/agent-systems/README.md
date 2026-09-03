# Agent Systems — AI-DLC × Bug-Hunter × Knowledge-Builder

Documentation for the three cooperating agent systems and the contract that binds them.

**New here?** Start with **[ARCHITECTURE.md](ARCHITECTURE.md)** — diagrams of the four roles, the
closed loop, the storage map, and the build order, at a glance.

## Current documents (specs of record)

| Document | Role |
|---|---|
| [ARCHITECTURE.md](ARCHITECTURE.md) | **Visual summary** — the org, the closed loop, sole-writer map, pipelines, build interleave, plugin/worker layer. The map; the guides are the territory. |
| [integration-contract.md](integration-contract.md) | **The normative cross-system interface.** Storage layout + sole-writer map, the `ledger-query` envelope, flow identity, loop-signal mailboxes, freshness/staleness, twin-name discipline, build interleave, consumer table. Wins over any brief in either guide. |
| [bug-hunter-build-guide.md](bug-hunter-build-guide.md) | **Bug-hunter spec of record.** Complete additive build guide: 6-slot pipeline (Map → Hunt → Verify → Triage → Report → Learn), all construction briefs, phases 1–5 + optional integrations. Inception ran 2026-06-10; re-scoped 2026-09 — 085–086 verify that the review loop satisfies Phase 1 (verification bolts); 087–094 build the rest. **Implementation status 2026-09: partially built as the review loop (`reviews/`); see the guide's status table.** |
| [knowledge-builder-build-guide.md](knowledge-builder-build-guide.md) | **Knowledge-builder spec of record.** Complete additive build guide: 7-stage pipeline (Ingest → … → Publish), the three-way firewall, all construction briefs, phases 1–5. Inception pending — §7 of the contract is updated when it assigns bolt numbers. **Not started (2026-09).** |
| [operating-profiles.md](operating-profiles.md) | **Operator & deployment guide (non-normative).** How to choose, switch, and wire an operating profile (`TriggerPolicy` × `CommitPolicy`); home for the deployment artifacts (hook script, CI template) once built. Defers to contract §5.5 for the rules. |
| [theory-vs-practice-2026-09.md](theory-vs-practice-2026-09.md) | **The bridge.** Cross analysis of these specs against the review machinery built on main, June–September 2026: concept map, contradictions and rulings, the 43-brief status, next steps. Read it before extending either side. |
| [reconciliation-plan-2026-09.md](reconciliation-plan-2026-09.md) | **The plan that applied the bridge.** Task by task, which document each of the owner's rulings changes, in what order, and the check that proves it landed. Read it to see why a status sentence here says what it says. |

## Future / planned systems (captured, not built)

Theory-level analysis lives on the `analysis/architect-review` branch; brainstormed systems are
captured as connected concept notes so implementation later starts from a whole design. Start with the
map.

| Document | Role |
|---|---|
| [future/](future/README.md) | **The full agentic-org map** — every system (built / specced / planned / roadmap-gated), the decide→coordinate→do→operate layers, and how the future pieces connect to what exists. (The `future/` folder's index.) |
| [future/code-review-system.md](future/code-review-system.md) | **Reviewer** — pre-merge, diff-scoped gate. Three of its five dimensions run today as lenses of the review loop; the remainder is deferred. |
| [future/conductor-system.md](future/conductor-system.md) | **Conductor** — planning "engineering manager": aggregates every system's signals → proposes a ranked next-work queue (proposes, never decides). Hosts the **coordinate-layer** pipeline (Analyst → Conductor → Planner → Wave-orchestrator) — half already built. |
| [future/analyst-system.md](future/analyst-system.md) | **Analyst** — proactive architectural review → ranked gap/improvement proposals that feed the Conductor. Exists as a proto (`architect-analyst` agent); note plans how to evolve it. |
| [future/planner-system.md](future/planner-system.md) | **Planner & Wave-orchestrator** — the execution half of the coordinate layer: decided bolts → conflict-safe waves → run a wave. Already built (`bolt-parallel-planner` / `bolt-wave-orchestrator`); note records how to make them profile- and contract-aware. |
| [future/test-quality-system.md](future/test-quality-system.md) | **Test-Quality** — builds & *judges* the safety net (coverage, mutation, e2e); maps to the roadmap's e2e/regression phase. |
| [future/observability-system.md](future/observability-system.md) | **Observability / SRE** — watches the running product, turns incidents into fix-requests. Roadmap-gated (post-deployment). |

## Relationship to the review loop (`reviews/`)

The review loop is this blueprint's Inspector engine, built first and by hand while reviewing the
bolt branches, running in the guide's "pre-merge" mode on one feature at a time. It keeps the ledger
(`reviews/<target>/ledger.md`, `reviews/state/backlog.md`), verifies fixes by reverting them
(`reviews/lib/verify/verify-fixes.mjs`), reconciles findings (`reconcile-findings` skill), and
records every pass (`reviews/state/index.md`). Its operating rules live in
[`reviews/README.md`](../../reviews/README.md); its design notes in
[`reviews/notes/self-driving-loop-design.md`](../../reviews/notes/self-driving-loop-design.md).
The two vocabularies are mapped in the bridge document's Appendix A. Rule of thumb: the guides say
what the Inspector should become; `reviews/` says what it is.

## Design history

These specs converged over four cross-system review rounds (findings **F1–F23 → G1–G16 → H1–H35 →
J1–J4**) plus a final operating-profiles pass. Once they reached final form, the review documents and
every intermediate spec version were removed — they remain fully recoverable in **git history**. The
*why* behind non-obvious decisions lives in each spec's own changelog section and in
[`memory-bank/intents/035-bug-hunter-agent-system/inception-log.md`](../../memory-bank/intents/035-bug-hunter-agent-system/inception-log.md).

## Conventions for this folder

- **One spec of record per system**, versionless filenames, **edited in place** — git is the version
  history; no archived copies, no version-suffixed names.
- **Path references** from `memory-bank/` (intents, bolts, story-index) point at
  `docs/agent-systems/<file>`.
- The integration contract is the **normative** interface: if a brief and the contract disagree, the
  contract wins; changing it requires checking every consumer in its §8.
- **Two sides, one truth.** A change to a status, a rule or a build order in these specs is mirrored
  in `reviews/README.md`'s pointer section when it affects the running loop, and vice versa.
