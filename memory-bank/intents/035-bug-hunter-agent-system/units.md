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
> briefs (43 total). All bolts are `simple-construction-bolt`; outputs are scripts and
> skills in the review-loop tree (`reviews/lib`, `.claude/skills`), never application code.
>
> **Re-cut 2026-09 (reconciliation).** The engine is built — it is the review loop under
> `reviews/`. Of the 43 briefs, **12 are satisfied** by it, tracked in the guide's
> "Implementation status (2026-09)" table; the units below cover the **remaining 31**
> (16 missing + 15 partial). Unit order follows the build order the owner ruled
> (integration contract §7): trust upgrades → Map slot → specialists → learn & measure →
> remediation hand-off → the oracle tier (unit 003's last bolt, gated on the knowledge-builder)
> → optional integration (after the oracle in §7's list, and adoption-gated).
> Bolts 085 and 086 are retired; story files never move, so a story's folder still names
> the phase it was decomposed under.
>
> ⚠️ **Cross-cutting (FR-1, FR-2), binding for every unit:** every component is built
> by pasting its brief (Prompt N from `docs/agent-systems/bug-hunter-build-guide.md`) into the
> **skill-creator skill** (`Skill` tool → `skill-creator:skill-creator`), then running
> the brief's three test prompts before moving on — in master-build-order. The system
> is **read-only on application source**. **Amended 2026-09:** skill-creator is for a *new
> standalone skill*; a piece that extends the review loop is built as a script or skill in that
> tree at the seam its story names, with a test under `reviews/lib/tests`, following
> `reviews/README.md`'s conventions.

## Units Overview

This intent decomposes into **6 units** (43 stories: 12 satisfied by the review loop,
31 remaining) — the same six as at inception. The 2026-09 re-cut changed what each unit covers,
not how many there are: the oracle tier is the **last bolt of unit 003** (bolt 091, gated on the
knowledge builder), not a unit of its own.

### Unit 1: 001-phase-1-skeleton — satisfied by the review loop (2026-09), no bolt

**Description**: The smallest complete end-to-end system: concurrency-safe ledger
(`ledger-io`), canonical bug records (`bug-documentation`), `deduplication`, floored
Markdown `report-rendering`, the human-decision channel (`triage-intake`), one
`general-hunter`, and the `orchestrator` defining all six permanent slots. **All of it runs
today** as the review loop's records tree, `reconcile-findings`, review/summary documents with
their doc gate, the owner gates, the core six lenses and `loop-driver` + the pass router. Its
two bolts (085, 086) are retired — the work is done, so there is nothing to schedule.

**Stories** (7, Prompts 1–7): 001-ledger-io, 002-bug-documentation,
003-deduplication, 004-report-rendering, 005-triage-intake, 006-general-hunter,
007-orchestrator-skeleton. Six carry a `**Status:** satisfied by …` line naming the file that
satisfies them. The one gap is **002-bug-documentation** (Prompt 2): the loop writes a ledger
row plus a fix brief, not the three-audience record. No bolt work is planned for it.

**Dependencies**: Depends on — none. Depended by — nothing any more (later units extend the
review loop directly, at the seam each story names).

**Estimated Complexity**: — (built) · **Assigned Requirements**: FR-3 (+ FR-1/FR-2).

### Unit 2: 002-phase-2-trust — trust upgrades (bolt 087, first in the order)

**Description**: The cheapest gaps: make a finding provable and cheap to trust. Four pieces —
`severity-scoring` gains a real risk score and the reachability weight (8, 14b; the loop has
four severity levels and a convergence weight, no risk score); `tool-ingest` reads existing
scanner output (dependency audit, static analysis) in as untrusted candidates instead of
re-deriving it by hand (9 — missing entirely); `bug-verifier` gains **execution proof** — a
high-severity finding needs a failing test written by someone who did not fix it, naming the
commit it was taken on (10; today's skeptics argue, they do not run code); and
`git-revision-tracking` gains moved/fixed detection across runs (11).

**Stories** (5, Prompts 8–11b): 001-severity-scoring, 002-tool-ingest,
003-bug-verifier, 004-git-revision-tracking — the four gaps — and
005-orchestrator-verify-wiring (11b), **satisfied** by the pass router's rows. The reachability
weight (14b) is scored here but its story, `004-severity-scoring-reachability-ext`, sits in
unit 003 next to `reachability` itself, which it needs.

**Dependencies**: Depends on — the review loop (built). Depended by — Units 3a/3b, 4, 5.
**External prerequisite**: none any more — the proof runs the repo's own test commands, so the
Phase 2 sandbox recipe (requirements D4) is no longer a gate on this unit.

**Estimated Complexity**: M · **Assigned Requirements**: FR-4.

### Unit 3: 003-phase-3-breadth-and-scale — three waves in one unit (17 stories)

This is where the hole is: the review loop has no Map slot at all. The unit keeps all 17 stories
and its three bolts, read in this order: **3a — the Map slot** (bolt 088), **3b — the
specialists** (bolts 089 ∥ 090), then **the oracle tier** (bolt 091, last and gated). Only the
oracle bolt carries the external gate (requirements D6), so it no longer holds up 3a or 3b — the
change the 2026-09 re-cut made here.

**3a — Map slot (bolt 088)**: `app-mapping` (12), `code-index` (13) and `reachability` (14),
all missing today, plus the scoring extension that consumes reachability (14b) and
`taint-analysis` (16, its story sits with bolt 089). The budget-and-incremental-scanning half of
the orchestrator scale extension (24d) belongs here too — the loop caps delta passes and picks
lenses by touched area, but has no budget unit. `code-index` is a **shared tool** with the
knowledge builder (contract §7), owned by neither system's judgment layer.
Stories: 001-app-mapping, 002-code-index, 003-reachability,
004-severity-scoring-reachability-ext, 006-taint-analysis (+ the budget half of
017-orchestrator-scale-ext). Left as-is: 005-flow-tracing (15) — the lenses trace flows by
prompt.

**3b — Specialists (bolts 089 ∥ 090, both after the Map slot)**: the two lenses the manifest
lacks — `dependency-audit-agent` (20) and `config-auditor-agent` (21), both consumers of
`tool-ingest` — and `root-cause-clustering` (23), whose gap is one record covering many
locations. Stories: 010-dependency-audit-agent, 011-config-auditor-agent,
013-root-cause-clustering. **Satisfied**: 009-security-auditor-agent (19) = the security lens,
012-concurrency-auditor-agent (22) = the race lens. Left as-is: 007-flow-tracer-agent (17) and
008-file-sweeper-agent (18) — lenses do both, without a tools-first pass.

**Last bolt of this unit — the oracle tier (bolt 091), gated**: 014-intent-lookup (24),
015-hunters-contract-ext (24b), 016-verifier-scoring-contract-ext (24c) and the oracle half of
017-orchestrator-scale-ext (24d) — all four missing. They ground a finding in written intent
instead of the model's opinion: `intent-lookup` reads the knowledge builder's `ledger-query`
interface, the lens extension surfaces contract contradictions, the verification/scoring
extension weights contract corroboration and tags a model-prior-only finding
"intent-unconfirmed". Bolt 091 stays in this unit (`unit: 003-phase-3-breadth-and-scale`), runs
after 089/090, and is **⛔ gated on the knowledge builder's `ledger-query`** (requirements D6 —
the cross-system gate; contract §7). It is the last bolt of the whole intent bar the optional
tier, and nothing waits on it — which is why the map and the specialists are no longer held up
by its gate.

**Dependencies**: Depends on — Unit 2 (3b also on 3a; the oracle bolt on 3a/3b plus the external
knowledge-builder gate). Depended by — Units 4–5, which depend on 3a/3b only, never on the
oracle bolt.

**Estimated Complexity**: L · **Assigned Requirements**: FR-5.

### Unit 4: 004-phase-4-learn-and-measure — learn & measure (bolt 092)

**Description**: Make the system measure itself. The gaps: a **standing eval corpus plus a
poison fixture** (27 — there is a seeded-run protocol, no standing corpus); **recall and escape
metrics** (28 — `metrics.jsonl` and a track record exist, recall is unproven); and **curator
automation** (29, 29b — the system self-review and the speed report are run by hand).

**Stories** (6, Prompts 25–29b): 003-eval-corpus, 004-eval-metrics, 005-curator-agent,
006-orchestrator-learn-ext — the gaps — plus 002-bug-lifecycle (26), **satisfied** by the
loop's statuses, reopen and lineage, and 001-suppression-learning (25), **superseded**: the
loop never suppresses a finding, it attaches the prior decision to it (guide Prompt 25,
contract §6.5).

**Dependencies**: Depends on — Units 2 and 3 (bolts 089/090 — the corpus scores what the map,
the proof and the specialists produce; never the oracle bolt). Depended by — Unit 5 (the
remediation gate reads the metrics).

**Estimated Complexity**: M · **Assigned Requirements**: FR-6.

### Unit 5: 005-phase-5-remediation — remediation hand-off (bolt 093)

**Description**: Hand a confirmed bug to a fixer outside the loop, and keep the proof.
Two gaps: **`regression-harvest` by a non-fixer** (30 — today the fixer writes its own red-first
test and a test-meaning audit checks it; the harvested tripwire must come from someone who did
not write the fix) and **`fix-request-emit`** (33 — missing, because today's fixer is in-loop;
an out-of-loop fixer needs a fix-request store keyed by `correlation_id`).

**Stories** (5, Prompts 30–33 + 31b): 001-regression-harvest and 004-fix-request-emit — the
gaps — plus 002-fix-verification (31), **satisfied** by the loop's verification pass, and
005-orchestrator-remediation-ext (31b), **satisfied** by the router's verification row.
003-fix-proposal (32) is left as-is: the loop's fixer applies patches directly, by design, so
the blueprint's never-apply rule does not describe this engine.

**Dependencies**: Depends on — Units 2 and 4. Depended by — whatever consumes the
fix-request store (the AI-DLC bug-fix loop).

**Estimated Complexity**: M · **Assigned Requirements**: FR-7.

### Unit 6: 006-optional-integration — optional (bolt 094, on owner adoption)

**Description**: Unchanged, all three still missing. Build only on an owner adoption decision:
`report-rendering` SARIF twin (A), idempotent `issue-sync` (B), baseline-aware `ci-gate` (C).

**Stories** (3, Prompts A–C): 001-report-rendering-sarif-ext, 002-issue-sync,
003-ci-gate.

**Dependencies**: Depends on — Units 2 and 4 (the risk score and the lifecycle they publish).
Depended by — none.

**Estimated Complexity**: S · **Assigned Requirements**: FR-8 (Could).

## Requirement-to-Unit Mapping

- **FR-1** (skill-creator build loop) → cross-cutting, every story
- **FR-2** (shared conventions) → cross-cutting, every story
- **FR-3** (Phase 1 skeleton) → `001-phase-1-skeleton` — satisfied by the review loop
- **FR-4** (Phase 2 trust) → `002-phase-2-trust`
- **FR-5** (Phase 3 breadth/scale + oracle) → `003-phase-3-breadth-and-scale` (3a map,
  3b specialists, then the oracle tier as its last bolt — 091, gated)
- **FR-6** (Phase 4 learn/measure) → `004-phase-4-learn-and-measure`
- **FR-7** (Phase 5 remediation) → `005-phase-5-remediation`
- **FR-8** (Optional integration) → `006-optional-integration`

## Unit Dependency Graph

```text
[001-skeleton: BUILT — the review loop]
        │
        ▼
[002-trust] ─► [003a-map] ─► [003b-specialists] ─► [004-learn-and-measure] ─► [005-remediation]
                                    │                       │
                                    │                       └────► [006-optional] (⏸ adoption)
                                    ▼
                       [003 oracle tier — bolt 091, LAST]
                       (⛔ waits on the knowledge builder's ledger-query; nothing waits on it)
                       (within Unit 3b, bolts 089 ∥ 090 are the one wave-parallel pair)
```

## Execution Order (bolts 087–094)

**Eight bolts remain.** The order is the one the owner ruled in 2026-09, written out in the
integration contract §7; it replaces the guide's original top-to-bottom master order. Bolts 085
and 086 are gone — the review loop satisfied them, and a bolt is never marked `complete` without
a discovery pass, so a satisfied bolt is removed rather than closed.

1. **087** (Unit 2): trust upgrades — risk score + reachability weight, `tool-ingest`,
   execution proof, moved/fixed detection. First, because it is the cheapest and every later
   finding leans on it.
2. **088** (Unit 3a): the Map slot — `app-mapping`, `code-index` (shared with the knowledge
   builder), `reachability`, the scoring extension, and the budget unit.
3. **089 ∥ 090** (Unit 3b): specialists — **089** builds `taint-analysis` (16), the one gap of
   its four stories, since the security lens (19) is satisfied and the flow-tracer (17) and
   file-sweeper (18) stories stay as they are; **090** builds `dependency-audit-agent` (20),
   `config-auditor-agent` (21) and `root-cause-clustering` (23), the concurrency story (22) being
   satisfied by the race lens. Both wait for **088**, not for 087's `tool-ingest` alone, because
   they read the map; disjoint files, so they are the one safe parallel pair.
4. **092** (Unit 4): learn & measure — standing corpus, recall/escape metrics, curator
   automation.
5. **093** (Unit 5): remediation hand-off — non-fixer regression harvest, `fix-request-emit`.
6. **091** (Unit 3, its last bolt): oracle tier — **last**, and ⛔ blocked until the knowledge
   builder's `ledger-query` exists (requirements D6, contract §7). It re-opens pieces from
   088–090, so it never runs in parallel with anything.
7. **094** (Unit 6): optional integration — listed after 091 in the §7 order and **⏸
   adoption-gated**; its only bolt dependency is 092, so it may be built any time after that
   once the owner adopts a tracker or a CI gate.

Standing-sweep mode — the same engine on a schedule over all of `main` rather than a pre-merge
pass over one branch — has no bolt of its own yet (contract §7, step 5).
