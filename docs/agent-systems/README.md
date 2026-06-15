# Agent Systems — AI-DLC × Bug-Hunter × Knowledge-Builder

Documentation for the three cooperating agent systems and the contract that binds them.

**New here?** Start with **[ARCHITECTURE.md](ARCHITECTURE.md)** — diagrams of the four roles, the
closed loop, the storage map, and the build order, at a glance.

## Current documents (specs of record)

| Document | Role |
|---|---|
| [ARCHITECTURE.md](ARCHITECTURE.md) | **Visual summary** — the org, the closed loop, sole-writer map, pipelines, build interleave, plugin/worker layer. The map; the guides are the territory. |
| [integration-contract-v1.5.md](integration-contract-v1.5.md) | **The normative cross-system interface.** Storage layout + sole-writer map, the `ledger-query` envelope, flow identity, loop-signal mailboxes, freshness/staleness, twin-name discipline, build interleave, consumer table. Wins over any brief in either guide. |
| [bug-hunter-build-guide-v3.6.md](bug-hunter-build-guide-v3.6.md) | **Bug-hunter spec of record.** Complete additive build guide: 6-slot pipeline (Map → Hunt → Verify → Triage → Report → Learn), all construction briefs, phases 1–5 + optional integrations. Inception has run: bolts 085–094 (intent 035). |
| [knowledge-builder-build-guide-v3.5.md](knowledge-builder-build-guide-v3.5.md) | **Knowledge-builder spec of record.** Complete additive build guide: 7-stage pipeline (Ingest → … → Publish), the three-way firewall, all construction briefs, phases 1–5. Inception pending — §7 of the contract is updated when it assigns bolt numbers. |
| [operating-profiles.md](operating-profiles.md) | **Operator & deployment guide (non-normative).** How to choose, switch, and wire an operating profile (`TriggerPolicy` × `CommitPolicy`); home for the deployment artifacts (hook script, CI template) once built. Defers to contract §5.5 for the rules. |

## Future / planned systems (captured, not built)

Theory-level analysis lives on the `analysis/architect-review` branch; brainstormed systems are
captured as connected concept notes so implementation later starts from a whole design. Start with the
map.

| Document | Role |
|---|---|
| [future/](future/README.md) | **The full agentic-org map** — every system (built / specced / planned / roadmap-gated), the decide→coordinate→do→operate layers, and how the future pieces connect to what exists. (The `future/` folder's index.) |
| [future/code-review-system.md](future/code-review-system.md) | **Reviewer** — pre-merge, diff-scoped gate composing the `pr-review-toolkit` plugins. Deferred until the other three are built. |
| [future/conductor-system.md](future/conductor-system.md) | **Conductor** — planning "engineering manager": aggregates every system's signals → proposes a ranked next-work queue (proposes, never decides). Hosts the **coordinate-layer** pipeline (Analyst → Conductor → Planner → Wave-orchestrator) — half already built. |
| [future/analyst-system.md](future/analyst-system.md) | **Analyst** — proactive architectural review → ranked gap/improvement proposals that feed the Conductor. Exists as a proto (`architect-analyst` agent); note plans how to evolve it. |
| [future/planner-system.md](future/planner-system.md) | **Planner & Wave-orchestrator** — the execution half of the coordinate layer: decided bolts → conflict-safe waves → run a wave. Already built (`bolt-parallel-planner` / `bolt-wave-orchestrator`); note records how to make them profile- and contract-aware. |
| [future/test-quality-system.md](future/test-quality-system.md) | **Test-Quality** — builds & *judges* the safety net (coverage, mutation, e2e); maps to the roadmap's e2e/regression phase. |
| [future/observability-system.md](future/observability-system.md) | **Observability / SRE** — watches the running product, turns incidents into fix-requests. Roadmap-gated (post-deployment). |

## Reviews

| Document | Scope |
|---|---|
| [reviews/cross-system-review-v1-2026-06-11.md](reviews/cross-system-review-v1-2026-06-11.md) | Findings **G1–G16**. High-severity: BH prompt-injection gap, BH secret leakage, orphaned `fix-reported` state, signature collisions. **APPLIED 2026-06-11**: BH → v3.2, contract → v1.1, KB → v3.1. |
| [reviews/cross-system-review-v2-2026-06-12.md](reviews/cross-system-review-v2-2026-06-12.md) | Findings **H1–H35** (+4 refuted, listed). Dominant pattern: G fixes that landed as policy without mechanism. High-severity: the mailbox scan no brief built (→ new BH Prompt 31b), single-history stores vs parallel worktrees. **APPLIED 2026-06-12**: BH → v3.3, contract → v1.2, KB → v3.2. |
| [reviews/cross-system-review-v3-2026-06-12.md](reviews/cross-system-review-v3-2026-06-12.md) | Findings **I1–I13** (0 refuted; several severity-trimmed in verification). Dominant root cause: runtime co-residence of the two systems in one worktree (→ cross-system mutex, scoped audits, path-scoped commits); plus two fix-loop state-machine bugs (`fix-failed` unreachable by the scan; `closed-unverified` oracle gap → owner decision B: stays out). **APPLIED 2026-06-12**: BH → v3.4, contract → v1.3, KB → v3.3. |
| [reviews/cross-system-review-v4-2026-06-15.md](reviews/cross-system-review-v4-2026-06-15.md) | Findings **J1–J4** (0 refuted). The code-index seam: the I-round's store-scoped audit blinded the "never edit app code" backstop (J1 → forbidden-ground check), and the dual-writer index at `bug-hunting/code-index/` collided with KB's sole-writer convention and path-scoped commit (J2/J3 → gitignored build artifact). J4: schedule/order agree (Phase 5 may precede Phase 4). **APPLIED 2026-06-15**: BH → v3.5, contract → v1.4, KB → v3.4. |

The KB guide's v3 changelog references an earlier external review of v2 (findings **F1–F23**), which was folded directly into v3 rather than kept as a standalone file.

## Archive (superseded versions — referenced only historically, e.g. by review headers)

| Document | Superseded by |
|---|---|
| [archive/integration-contract-v1.4.md](archive/integration-contract-v1.4.md) | v1.5 (operating profiles, 2026-06-15) |
| [archive/bug-hunter-build-guide-v3.5.md](archive/bug-hunter-build-guide-v3.5.md) | v3.6 (operating profiles, 2026-06-15) |
| [archive/knowledge-builder-build-guide-v3.4.md](archive/knowledge-builder-build-guide-v3.4.md) | v3.5 (operating profiles, 2026-06-15) |
| [archive/bug-hunter-build-guide-v3.4.md](archive/bug-hunter-build-guide-v3.4.md) | v3.5 (review J-fixes, 2026-06-15) |
| [archive/integration-contract-v1.3.md](archive/integration-contract-v1.3.md) | v1.4 (review J-fixes, 2026-06-15) |
| [archive/knowledge-builder-build-guide-v3.3.md](archive/knowledge-builder-build-guide-v3.3.md) | v3.4 (review J-fixes, 2026-06-15) |
| [archive/bug-hunter-build-guide-v3.3.md](archive/bug-hunter-build-guide-v3.3.md) | v3.4 (review I-fixes, 2026-06-12) |
| [archive/integration-contract-v1.2.md](archive/integration-contract-v1.2.md) | v1.3 (review I-fixes, 2026-06-12) |
| [archive/knowledge-builder-build-guide-v3.2.md](archive/knowledge-builder-build-guide-v3.2.md) | v3.3 (review I-fixes, 2026-06-12) |
| [archive/bug-hunter-build-guide-v3.2.md](archive/bug-hunter-build-guide-v3.2.md) | v3.3 (review H-fixes, 2026-06-12) |
| [archive/integration-contract-v1.1.md](archive/integration-contract-v1.1.md) | v1.2 (review H-fixes, 2026-06-12) |
| [archive/knowledge-builder-build-guide-v3.1.md](archive/knowledge-builder-build-guide-v3.1.md) | v3.2 (review H-fixes, 2026-06-12) |
| [archive/bug-hunter-build-guide-v3.1.md](archive/bug-hunter-build-guide-v3.1.md) | v3.2 (review G-fixes, 2026-06-11) |
| [archive/integration-contract-v1.md](archive/integration-contract-v1.md) | v1.1 (review errata, 2026-06-11) |
| [archive/knowledge-builder-build-guide-v3.md](archive/knowledge-builder-build-guide-v3.md) | v3.1 (review point fixes, 2026-06-11) |
| [archive/bug-hunter-build-guide-v3.md](archive/bug-hunter-build-guide-v3.md) | v3.1 (interface-alignment edits, 2026-06-11) |
| [archive/knowledge-builder-build-guide-v2.md](archive/knowledge-builder-build-guide-v2.md) | v3 (folded in review findings F1–F23) |
| [archive/knowledge-builder-build-guide-v1.md](archive/knowledge-builder-build-guide-v1.md) | v2 |

## Conventions for this folder

- **One spec of record per system** at the top level; superseded versions move to `archive/` and are never edited.
- **Reviews** live in `reviews/`, named `cross-system-review-v<N>-<YYYY-MM-DD>.md` — the version number is the unique round identifier (a second review on the same day is simply `v<N+1>`), the date is context. Each round numbers its findings continuing the existing letter sequence (F1–F23 → G1–G16 → H…).
- **Path references** from `memory-bank/` (intents, bolts, story-index) point at `docs/agent-systems/<file>` — when a new version becomes the spec of record, update those references in the same commit.
- **Version-bump checklist (H32)** — a bump updates, in the same commit: the memory-bank sweep (above), the contract's "Referenced by" line, the sibling guide's spec-of-record references, this README's tables, `ARCHITECTURE.md`'s specs-of-record line, `operating-profiles.md`'s contract link, and prior review files' "documents reviewed" paths (re-pointed to `archive/`). Nothing else may cite a version-bearing filename.
- The Integration Contract is versioned (v1, v2, …); changing it requires checking every consumer in its §8.
