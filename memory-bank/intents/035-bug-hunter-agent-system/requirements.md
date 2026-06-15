---
intent: 035-bug-hunter-agent-system
phase: inception
status: inception-complete
created: 2026-06-10T10:40:14Z
updated: 2026-06-10T10:40:14Z
---

# Requirements: Bug-Hunting Agent System (Tooling Intent)

> **Tooling-only intent.** Builds the multi-agent bug-hunting system described in
> `docs/agent-systems/bug-hunter-build-guide-v3.6.md` — **42 numbered briefs** across 5 additive phases
> plus an optional integration tier. Components are Claude Code skills (agents are
> built as skills defining their procedure, per the guide's shared conventions).
> **The system is read-only on application source** — this intent ships no production
> code changes.
>
> ⚠️ **CONSTRUCTION METHOD (owner mandate + guide Part I):** every component **MUST be
> created with the `skill-creator` skill** (invoke via the `Skill` tool, name
> `skill-creator:skill-creator`). The guide is written for exactly this: each brief
> ("Prompt N") is a self-contained construction prompt to paste into skill-creator.
> Build loop per component: paste brief → build → run its three test prompts → fix →
> only then move on. If skill-creator is unavailable in the construction context,
> STOP and report — do not hand-roll skills.
>
> Source feed (the spec of record): `docs/agent-systems/bug-hunter-build-guide-v3.6.md`. Stories do
> NOT duplicate the briefs — each story points at its Prompt N and adds memory-bank
> tracking + acceptance criteria.

## Intent Overview

A system of cooperating agents and skills that inspects this application, finds real
bugs, documents them for non-technical stakeholder / developer / tester audiences,
confirms them by execution in a sandbox, learns from owner feedback, and (Phase 5)
feeds confirmed bugs into the AI-DLC flow as fix-requests keyed by `correlation_id`.

The architecture is a **stable six-slot pipeline** — Map → Hunt → Verify → Triage →
Report → Learn — coordinated by an Orchestrator that exists from Phase 1. Growth is
**additive**: later phases fill or extend slots at planned seams; nothing built
earlier is rewritten or thrown away. After every phase the system runs end-to-end.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Find & document real bugs end-to-end | Phase 1 runs produce a per-run Markdown report from a persistent ledger; output explicitly labeled "unverified candidates" | Must |
| Make findings trustworthy | Phase 2: bugs confirmed by execution in a sandbox (commit-matched, flake-guarded); risk = severity × confidence | Must |
| Scale to the whole codebase affordably | Phase 3: map/index, specialist hunters, reachability, clustering, incremental + cheap-first scanning under a run budget | Must |
| Ground findings in real intent, not model opinion | Phase 3 oracle: contract-contradiction findings cite the knowledge ledger; model-prior-only findings tagged "intent-unconfirmed" | Must |
| System improves from feedback | Phase 4: dismissals (with reasons) become validated suppression patterns; precision/recall trends measured against an eval corpus | Should |
| Close the fix loop with AI-DLC | Phase 5: confirmed bugs → fix-request store keyed by `correlation_id`; closure only via the fix-verification gate re-running the proving test | Should |
| CI / tracker integration | Optional tier: SARIF twin, idempotent issue-sync, baseline-aware ci-gate | Could |

## Scope

### In Scope
- All 42 briefs from the guide's master build order, one story each:
  - **Phase 1 — Skeleton** (Prompts 1–7): `ledger-io`, `bug-documentation`,
    `deduplication`, `report-rendering`, `triage-intake`, `general-hunter`,
    `orchestrator` [skeleton].
  - **Phase 2 — Trust** (Prompts 8–11b): `severity-scoring`, `tool-ingest`,
    `bug-verifier`, `git-revision-tracking`, orchestrator wiring extension.
  - **Phase 3 — Breadth & Scale** (Prompts 12–24d): `app-mapping`, `code-index`,
    `reachability` (+ scoring extension), `flow-tracing`, `taint-analysis`,
    `flow-tracer-agent`, `file-sweeper-agent`, `security-auditor-agent`,
    `dependency-audit-agent`, `config-auditor-agent`, `concurrency-auditor-agent`
    (conditional), `root-cause-clustering`, `intent-lookup` + the three oracle/scale
    extensions (hunters, verifier+scoring, orchestrator).
  - **Phase 4 — Learn & Measure** (Prompts 25–29b): `suppression-learning`,
    `bug-lifecycle`, `eval-corpus`, `eval-metrics`, `curator-agent`, orchestrator
    Learn-slot extension.
  - **Phase 5 — Remediation** (Prompts 30–33): `regression-harvest`,
    `fix-verification`, `fix-proposal`, `fix-request-emit`.
  - **Optional — Integration** (A–C): `report-rendering` SARIF extension,
    `issue-sync`, `ci-gate`.
- Pinned repo conventions for system outputs (see Decisions below).

### Out of Scope (hard constraints)
- ❌ **No application code changes — ever.** The system is read-only on app source
  (guide convention). Allowed writes only: the ledger, code index, sandbox, run
  reports, fix-request store, and — Phase 5, with owner approval — new test files.
- ❌ **The knowledge builder / knowledge ledger system itself.** `intent-lookup`
  *reads* its `ledger-query` interface; building that system is a separate intent.
  Its absence blocks only the oracle stories (see bolt 091 `blocks` flag).
- ❌ **Auto-applied patches.** `fix-proposal` proposes diffs; it never applies them.
- ❌ **Modifying the guide.** `docs/agent-systems/bug-hunter-build-guide-v3.6.md` is the spec of
  record; construction follows it verbatim (deviations are owner decisions).
- ❌ **No deployment implications.** This is tooling; roadmap Phase 6 (deployment)
  is untouched.

---

## Functional Requirements

### FR-1: skill-creator build loop (cross-cutting)
- **Description**: Every component is built by pasting its brief (Prompt N) from the
  guide into the **skill-creator skill** and following the guide's build loop: build →
  run the brief's three test prompts → confirm/fix → only then take the next component
  in the master build order (dependency-ordered, top to bottom).
- **Acceptance Criteria**: Each skill exists under `.claude/skills/{name}/` with the
  guide's exact component name; construction log records skill-creator invocation +
  the three test-prompt results per component; build order respected (a component's
  dependencies were built first); if skill-creator is unavailable, the bolt STOPS.
- **Priority**: Must (applies to every story)

### FR-2: Shared conventions (cross-cutting)
- **Description**: The guide's shared conventions bind every component: agents are
  skills defining their procedure; the six slots are permanent (fill/extend, never
  restructure); hunters emit lightweight candidates in the shared shape and never
  self-censor (Verify gates); report at every confidence level with the reporting
  floor (Low → appendix); dedup before emitting; **read-only on application source**;
  concurrency-safe ledger writes (single-writer merge); oracle grounding from Phase 3;
  human feedback only through `triage-intake`.
- **Acceptance Criteria**: Each story's skill complies with the conventions its brief
  invokes; extensions are top-ups at the brief's named seam (prior behavior + tests
  still pass).
- **Priority**: Must (applies to every story)

### FR-3: Phase 1 — Skeleton (Prompts 1–7)
- **Description**: The smallest complete end-to-end system: concurrency-safe ledger,
  canonical bug records, dedup, floored Markdown reports, the human-decision channel,
  one general hunter, and the Orchestrator defining all six slots (Verify/Learn as
  placeholders).
- **Acceptance Criteria**: A full run on this repo produces `bug-ledger.json` + `.md`,
  a new per-run report labeled **"unverified candidates — high false-positive rate
  until Phase 2"**, with dedup against prior runs and human decisions applied via
  `triage-intake`.
- **Priority**: Must

### FR-4: Phase 2 — Trust (Prompts 8–11b)
- **Description**: Fill the Verify slot: the hardened `bug-verifier` (disprove-first;
  dynamic confirmation in a sandbox; sandbox-vs-commit check; flaky-test double-run),
  `severity-scoring` (severity × confidence), `tool-ingest` (deterministic findings as
  candidates), `git-revision-tracking` (commit pinning + fixed/moved reconciliation),
  and the orchestrator wiring extension.
- **Acceptance Criteria**: Candidates carry confidence + risk score; the blanket
  "unverified" label is dropped for per-finding confidence; a stale sandbox recipe
  yields "could not verify in sandbox" (a reported problem), never silent static
  fallback; runs are pinned to a commit SHA.
- **Priority**: Must

### FR-5: Phase 3 — Breadth & Scale + Oracle (Prompts 12–24d)
- **Description**: Map/index the app, split hunting into specialists (flow, file,
  security, dependency, config, optionally concurrency), add reachability as a third
  risk factor (framework-aware "unknown" weight), cluster findings by root cause,
  control cost (run budget, incremental scanning, cheap-first ordering), and ground
  findings in the knowledge ledger via `intent-lookup` (+ the hunter/verifier/
  orchestrator oracle extensions).
- **Acceptance Criteria**: Specialists dispatch by risk class; reachable High can
  outrank unreachable Critical; contract-contradiction findings cite the contract;
  model-prior-only findings tagged "intent-unconfirmed"; a run can scan only the
  latest diff and says so.
- **Priority**: Must (concurrency-auditor: Should — async-heavy .NET stack makes it
  relevant, but the guide marks it optional)

### FR-6: Phase 4 — Learn & Measure (Prompts 25–29b)
- **Description**: The Curator fills the Learn slot: dismissal reasons →
  validated suppression patterns (proposed, never auto-activated; checked against the
  Confirmed set); bug lifecycle with evidence-based self-closing and regression
  flagging; eval corpus (labeled real + seeded synthetic bugs) and metrics
  (recall vs seeded corpus; precision proxied by dismissal rate; pinned model/temp
  for eval runs).
- **Acceptance Criteria**: After a run with dismissals, proposed patterns arrive with
  blast radius + no-true-bug-suppressed confirmation; metrics trend over runs; a
  fixed signature reappearing is flagged as a regression.
- **Priority**: Should

### FR-7: Phase 5 — Remediation & Regression Safety (Prompts 30–33)
- **Description**: Keep the Verifier's proving test as a permanent regression test
  (owner-approved write); `fix-verification` is the **closure gate** (re-run the
  proving test in the sandbox; emit `verified-fixed` carrying the `correlation_id` —
  never close on AI-DLC's word alone); `fix-proposal` drafts diffs validated against
  the surrounding suite (never applied); `fix-request-emit` hands confirmed bugs to
  AI-DLC through an idempotent store keyed by `correlation_id`.
- **Acceptance Criteria**: The brief's tests pass for all four; a "fix" that doesn't
  make the test pass keeps the bug Confirmed with no signal; fix-requests update
  rather than duplicate.
- **Priority**: Should

### FR-8: Optional — Integration (Prompts A–C)
- **Description**: SARIF twin of the Markdown report; idempotent issue-tracker sync;
  baseline-aware CI gate (fail only on newly-introduced Critical/High by default).
- **Acceptance Criteria**: Per the briefs' tests; build only on owner adoption
  decision (repo already has GitHub Actions CI; tracker choice pending).
- **Priority**: Could

---

## Non-Functional Requirements

### NFR-1: Read-only safety
- **Metric**: Zero writes outside the allowed set (ledger, index, sandbox, reports,
  fix-request store, approved test files). Any component editing app source is a
  critical defect.

### NFR-2: Additive growth
- **Metric**: Extension briefs modify only their named seam; all prior briefs' test
  prompts still pass after every extension (no-regression rule of the build loop).

### NFR-3: Sandbox safety
- **Metric**: Sandbox containers are throwaway, build the commit under analysis,
  outbound network locked down, time/CPU/memory capped, never loaded with real
  production data (guide Part I sandbox rules).

### NFR-4: Cost control
- **Metric**: From Phase 3: per-run budget honored; incremental scanning default;
  deterministic tools run before LLM hunters; sandbox only for survivors.

### NFR-5: Honesty rules
- **Metric**: "Zero new bugs" is a valid run; `unknown` is a valid reachability
  answer (never guessed); bugs are never invented to fill a report; Low findings are
  reported (appendix), only proven-non-bugs are dropped.

### NFR-6: Environment
- **Metric**: Runs on this Windows 11 host (PowerShell; Docker Desktop for the
  sandbox; .NET + Angular toolchains for tool-ingest). Skills are markdown
  procedures; anything they shell out to must exist on the host or in the sandbox.

---

## Pinned Decisions (inception — owner-reviewable)

| # | Decision | Rationale |
|---|----------|-----------|
| D1 | Component names = the guide's names, verbatim (`ledger-io` … `ci-gate`) | Briefs cross-reference each other by these names; renaming breaks the dependency web |
| D2 | Skills live at `.claude/skills/{name}/` (skill-creator default, project level) | Native Claude Code loading; consistent with the construction mandate |
| D3 | System outputs root: `bug-hunting/` — `bug-hunting/bug-ledger.json` + `bug-ledger.md`, `bug-hunting/reports/bug-report-run-NN-<ts>.md`, `bug-hunting/eval/` (corpus + fixtures), `bug-hunting/fix-requests/` (the AI-DLC store) | One audited write-root outside app source and outside `memory-bank/` (AI-DLC's tree); single seam — only `ledger-io`/`report-rendering`/`fix-request-emit` know paths, so relocating later is cheap |
| D4 | Sandbox recipe = the repo's existing `docker-compose` assets (API + Postgres), adapted once by the owner as the fixed asset the Verifier reads | Guide: "you provide the recipe once"; the repo already ships compose files (bolt 040) |
| D5 | `concurrency-auditor-agent` is in scope at Should priority | Guide-optional, but this stack is async/await + BackgroundServices + EF transactions — exactly its territory |
| D6 | Oracle stories (Prompts 24–24d) carry a cross-system gate | `intent-lookup` reads the knowledge builder's `ledger-query` interface — now specified in `docs/agent-systems/integration-contract-v1.5.md` (§2/§3) and built per `docs/agent-systems/knowledge-builder-build-guide-v3.5.md`; bolt 091 runs after the knowledge builder's Phases 1–2 (contract §7), unless the owner descopes the oracle |
| D7 | Story files do not duplicate brief content | The guide is the spec of record; stories carry tracking + acceptance criteria + the skill-creator mandate, and point at Prompt N |

## Traceability

| FR | Unit | Stories |
|----|------|---------|
| FR-1, FR-2 | all (cross-cutting) | every story |
| FR-3 | 001-phase-1-skeleton | 7 (Prompts 1–7) |
| FR-4 | 002-phase-2-trust | 5 (Prompts 8–11b) |
| FR-5 | 003-phase-3-breadth-and-scale | 17 (Prompts 12–24d) |
| FR-6 | 004-phase-4-learn-and-measure | 6 (Prompts 25–29b) |
| FR-7 | 005-phase-5-remediation | 4 (Prompts 30–33) |
| FR-8 | 006-optional-integration | 3 (Prompts A–C) |
