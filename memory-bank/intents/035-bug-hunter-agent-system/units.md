---
intent: 035-bug-hunter-agent-system
phase: inception
status: units-decomposed
updated: 2026-06-10T10:40:14Z
---

# Bug-Hunting Agent System - Unit Decomposition

> **Decomposition note.** Tooling-only intent — the standard `full-stack-web`
> backend/frontend DDD decomposition does **not** apply. Units mirror the guide's
> **additive phases** (Part I, master build order): each unit is a phase after which
> the whole system still runs end-to-end. Stories map 1:1 to the guide's numbered
> briefs (42 total). All bolts are `simple-construction-bolt`; outputs are Claude Code
> skills, not application code.
>
> ⚠️ **Cross-cutting (FR-1, FR-2), binding for every unit:** every component is built
> by pasting its brief (Prompt N from `docs/agent-systems/bug-hunter-build-guide.md`) into the
> **skill-creator skill** (`Skill` tool → `skill-creator:skill-creator`), then running
> the brief's three test prompts before moving on — in master-build-order. The system
> is **read-only on application source**.

## Units Overview

This intent decomposes into **6 units** (42 stories):

### Unit 1: 001-phase-1-skeleton

**Description**: The smallest complete end-to-end system: concurrency-safe ledger
(`ledger-io`), canonical bug records (`bug-documentation`), `deduplication`, floored
Markdown `report-rendering`, the human-decision channel (`triage-intake`), one
`general-hunter`, and the `orchestrator` defining all six permanent slots (Verify and
Learn as placeholders; report labeled "unverified candidates").

**Stories** (7, Prompts 1–7): 001-ledger-io, 002-bug-documentation,
003-deduplication, 004-report-rendering, 005-triage-intake, 006-general-hunter,
007-orchestrator-skeleton.

**Dependencies**: Depends on — none. Depended by — every later unit.

**Estimated Complexity**: M · **Assigned Requirements**: FR-3 (+ FR-1/FR-2).

### Unit 2: 002-phase-2-trust

**Description**: Fill the Verify slot. `severity-scoring` (severity × confidence),
`tool-ingest` (deterministic findings → candidates), the hardened `bug-verifier`
(disprove-first; sandbox dynamic confirmation; commit-match + flaky-test guards),
`git-revision-tracking` (commit pinning; fixed/moved reconciliation), and the
orchestrator wiring extension (11b).

**Stories** (5, Prompts 8–11b): 001-severity-scoring, 002-tool-ingest,
003-bug-verifier, 004-git-revision-tracking, 005-orchestrator-verify-wiring.

**Dependencies**: Depends on — Unit 1. Depended by — Units 3–5.
**External prerequisite**: the sandbox recipe (owner adapts the repo's compose assets
once — requirements D4).

**Estimated Complexity**: M–L (Verifier + sandbox) · **Assigned Requirements**: FR-4.

### Unit 3: 003-phase-3-breadth-and-scale

**Description**: See more, scan smarter, control cost, ground in real intent.
Map/index (`app-mapping`, `code-index`), `reachability` (+ scoring extension 14b),
shared procedures (`flow-tracing`, `taint-analysis`), five specialist hunters + one
conditional (`concurrency-auditor-agent`), `root-cause-clustering`, and the oracle:
`intent-lookup` + extensions 24b (hunters), 24c (verifier+scoring), 24d
(orchestrator: specialists dispatch, cost control, incremental scanning).

**Stories** (17, Prompts 12–24d): 001-app-mapping, 002-code-index, 003-reachability,
004-severity-scoring-reachability-ext, 005-flow-tracing, 006-taint-analysis,
007-flow-tracer-agent, 008-file-sweeper-agent, 009-security-auditor-agent,
010-dependency-audit-agent, 011-config-auditor-agent, 012-concurrency-auditor-agent
(Should), 013-root-cause-clustering, 014-intent-lookup, 015-hunters-contract-ext,
016-verifier-scoring-contract-ext, 017-orchestrator-scale-ext.

**Dependencies**: Depends on — Unit 2. Depended by — Units 4–5.
**External prerequisite (oracle stories 014–017)**: the knowledge ledger's
`ledger-query` interface (requirements D6) — gates bolt 091.

**Estimated Complexity**: L · **Assigned Requirements**: FR-5.

### Unit 4: 004-phase-4-learn-and-measure

**Description**: The Curator fills the Learn slot: `suppression-learning` (dismissal
reasons → validated, proposed-not-auto-activated patterns), `bug-lifecycle`
(evidence-based transitions, regression flagging), `eval-corpus` + `eval-metrics`
(recall vs seeded corpus; precision via dismissal rate; pinned eval model/temp),
`curator-agent`, and the orchestrator Learn-slot extension (29b).

**Stories** (6, Prompts 25–29b): 001-suppression-learning, 002-bug-lifecycle,
003-eval-corpus, 004-eval-metrics, 005-curator-agent, 006-orchestrator-learn-ext.

**Dependencies**: Depends on — Units 1–3 (consumes triage-intake dismissals,
git-revision-tracking, the full pipeline). Depended by — Unit 5 (fix-verification
extends bug-lifecycle).

**Estimated Complexity**: M · **Assigned Requirements**: FR-6.

### Unit 5: 005-phase-5-remediation

**Description**: Close the fix loop. `regression-harvest` (keep the proving test —
the one owner-approved new-file write), `fix-verification` (the closure **gate**:
re-run the proving test; emit `verified-fixed` by `correlation_id`; never close on
AI-DLC's word alone), `fix-proposal` (diffs validated against the surrounding suite,
never applied), `fix-request-emit` (idempotent hand-off store for AI-DLC).

**Stories** (4, Prompts 30–33): 001-regression-harvest, 002-fix-verification,
003-fix-proposal, 004-fix-request-emit.

**Dependencies**: Depends on — Units 2 and 4. Depended by — the AI-DLC bug-fix loop.

**Estimated Complexity**: M · **Assigned Requirements**: FR-7.

### Unit 6: 006-optional-integration

**Description**: Build only on owner adoption decision: `report-rendering` SARIF
twin (A), idempotent `issue-sync` (B), baseline-aware `ci-gate` (C).

**Stories** (3, Prompts A–C): 001-report-rendering-sarif-ext, 002-issue-sync,
003-ci-gate.

**Dependencies**: Depends on — Units 1, 2, 4 (report-rendering, severity-scoring,
bug-lifecycle). Depended by — none.

**Estimated Complexity**: S · **Assigned Requirements**: FR-8 (Could).

## Requirement-to-Unit Mapping

- **FR-1** (skill-creator build loop) → cross-cutting, every story
- **FR-2** (shared conventions) → cross-cutting, every story
- **FR-3** (Phase 1 skeleton) → `001-phase-1-skeleton`
- **FR-4** (Phase 2 trust) → `002-phase-2-trust`
- **FR-5** (Phase 3 breadth/scale/oracle) → `003-phase-3-breadth-and-scale`
- **FR-6** (Phase 4 learn/measure) → `004-phase-4-learn-and-measure`
- **FR-7** (Phase 5 remediation) → `005-phase-5-remediation`
- **FR-8** (Optional integration) → `006-optional-integration`

## Unit Dependency Graph

```text
[001-skeleton] ─► [002-trust] ─► [003-breadth-and-scale] ─► [004-learn-and-measure] ─► [005-remediation]
                                                                      │
                                                                      └────► [006-optional-integration]
                       (within Unit 3, bolts 089 ∥ 090 are the one wave-parallel pair)
```

## Execution Order (bolts 085–094)

The guide's master build order is **binding** (dependency-ordered, top to bottom;
each brief's dependencies must already exist). Bolt grouping:

1. **085** (Unit 1, Prompts 1–5): foundation skills.
2. **086** (Unit 1, Prompts 6–7): general-hunter + orchestrator skeleton.
3. **087** (Unit 2, Prompts 8–11b): trust layer + sandbox.
4. **088** (Unit 3, Prompts 12–14b, 15): map, index, reachability, flow-tracing.
5. **089 ∥ 090** (Unit 3): specialists A (16–19) ∥ specialists B + triage (20–23) —
   disjoint skill directories, both depend only on 088; the one safe parallel pair.
6. **091** (Unit 3, Prompts 24–24d): oracle grounding — ⛔ **blocked** until the
   knowledge ledger's `ledger-query` interface is available (owner decision D6).
   24b/24c/24d re-open skills from 086–090, so 091 must never run in parallel with
   anything.
7. **092** (Unit 4, Prompts 25–29b): learn & measure.
8. **093** (Unit 5, Prompts 30–33): remediation loop.
9. **094** (Unit 6, Prompts A–C): optional integration — on owner adoption.
