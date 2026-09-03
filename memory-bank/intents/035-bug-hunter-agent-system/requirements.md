---
intent: 035-bug-hunter-agent-system
phase: inception
status: inception-complete
created: 2026-06-10T10:40:14Z
updated: 2026-06-10T10:40:14Z
---

# Requirements: Bug-Hunting Agent System (Tooling Intent)

> **Tooling-only intent.** Builds the multi-agent bug-hunting system described in
> `docs/agent-systems/bug-hunter-build-guide.md` — **43 numbered briefs** across 5 additive phases
> plus an optional integration tier. Components are Claude Code skills (agents are
> built as skills defining their procedure, per the guide's shared conventions).
> **The system is read-only on application source** — this intent ships no production
> code changes.
>
> **Re-scoped 2026-09 (reconciliation).** The system's engine exists as the review loop
> (`reviews/`), built in pre-merge mode. This intent now covers only what that engine lacks:
> the **16 missing** briefs and the gaps of the **15 partial** ones, in the order the owner
> ruled (integration contract §7). The status of every brief is the guide's
> "Implementation status (2026-09)" table — 43 briefs: 12 satisfied, 31 remaining. Three owner
> rulings shape this re-scope: **one engine, two modes** (the pre-merge review that exists plus
> a standing sweep over `main` that does not); **the contract §7 build order**; and **rewrite
> this intent in place** — the two Phase 1 skeleton bolts (085, 086) are **verification bolts**:
> they confirm, story by story, that the review loop really satisfies Phase 1, record the verdict,
> and complete through the normal process. They build nothing.
>
> ⚠️ **CONSTRUCTION METHOD (owner mandate + guide Part I):** every component **MUST be
> created with the `skill-creator` skill** (invoke via the `Skill` tool, name
> `skill-creator:skill-creator`). The guide is written for exactly this: each brief
> ("Prompt N") is a self-contained construction prompt to paste into skill-creator.
> Build loop per component: paste brief → build → run its three test prompts → fix →
> only then move on. If skill-creator is unavailable in the construction context,
> STOP and report — do not hand-roll skills. **Amended 2026-09:** that mandate holds for a
> *new standalone skill*; a piece that extends the review loop is built as a script or skill in
> that tree (`reviews/lib`, `.claude/skills`) at the seam its story names, with a test under
> `reviews/lib/tests`, following `reviews/README.md`'s conventions.
>
> Source feed (the spec of record): `docs/agent-systems/bug-hunter-build-guide.md`. Stories do
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

**Where it stands (2026-09).** That engine runs today as the review loop under `reviews/`,
in one of its two modes: the pre-merge pass over a branch. The standing sweep over `main` is
not built. Inside the pipeline, the Map slot is empty (no application map, no code index, no
reachability), findings are argued rather than proven by running code, scanner output is not
ingested, and the system does not measure its own recall. Those gaps — not the pipeline — are
this intent's remaining work.

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
The gaps the review loop leaves, in the ruled build order (integration contract §7). Each item
names its brief (Prompt N) and its **workbench seam** — the `reviews/lib` file it extends.

- **Trust upgrades** (bolt 087)
  - `tool-ingest` (9) — dependency-audit and static-analysis output read in as untrusted
    candidates instead of re-derived by hand · seam: `reviews/lib/discovery-review.wf.js`
  - `severity-scoring` risk score + reachability weight (8, 14b) ·
    seam: `reviews/lib/records/schema.mjs`
  - `bug-verifier` execution proof (10) — a high-severity finding needs a failing test written
    by someone who did not fix it, naming the commit it was taken on ·
    seam: `reviews/lib/verify/verify-fixes.mjs`
  - `git-revision-tracking` moved/fixed detection across runs (11) ·
    seam: `reviews/lib/verify/git.mjs`
- **Map & breadth** (bolts 088 → 089 ∥ 090; both specialists wait for the Map slot)
  - `app-mapping` (12), `code-index` (13), `reachability` (14) — the empty Map slot; new shared
    tools · seam: new files under `reviews/lib/`, wired into `reviews/lib/discovery-review.wf.js`
  - `taint-analysis` (16) · seam: the security lens in `reviews/lib/records/schema.mjs`
  - `dependency-audit-agent` (20), `config-auditor-agent` (21) — two lenses the manifest lacks ·
    seam: `reviews/lib/records/schema.mjs`
  - `root-cause-clustering` (23) — one record covering many locations ·
    seam: `reviews/lib/records/ledger.mjs`
  - budget unit + incremental scanning (24d) · seam: `reviews/lib/discovery-review.wf.js`
- **Learn & measure** (bolt 092)
  - standing corpus + poison fixture (27) · seam: `reviews/lib/tests/fixture-builder.mjs`
  - recall + escape metrics (28) — recall is unproven today · seam: `reviews/lib/measure/`
  - curator automation (29, 29b) — the system self-review and speed report run by hand ·
    seam: `reviews/lib/measure/speed-report.mjs`
- **Remediation hand-off** (bolt 093)
  - `regression-harvest` by a non-fixer (30) · seam: `reviews/lib/fix/handback-gates.mjs`
  - `fix-request-emit` (33) — a fix-request store for an out-of-loop fixer ·
    seam: `reviews/lib/records/ledger.mjs`
- **Oracle tier** (bolt 091, **last** — gated on the knowledge-builder's `ledger-query`,
  contract §7): `intent-lookup` (24) plus the hunter (24b) and verifier/scoring (24c)
  extensions · seam: `reviews/lib/records/schema.mjs` + the lens prompts they ground
- **Standing-sweep mode** (no bolt of its own yet — contract §7 step 5): the same engine on a
  schedule over all of `main`, as against today's pre-merge pass over one branch.
- **Optional** (bolt 094, on owner adoption): SARIF output (A), `issue-sync` (B), `ci-gate` (C) ·
  seam: `reviews/lib/records/render-records.mjs`
- Pinned repo conventions for system outputs (see Decisions below).

**Arithmetic.** 43 briefs = 12 satisfied + 31 remaining (16 missing + 15 partial). The lines
above schedule 25 of the 31. The other six get no bolt work — five partials the loop covers
another way: `bug-documentation` (2), `flow-tracing` (15), `flow-tracer-agent` (17) and
`file-sweeper-agent` (18), which the lenses do by prompt, and `fix-proposal` (32), because the
loop's fixer applies patches directly, by design; plus one missing brief the design replaces:
`suppression-learning` (25) → decision attachment (guide Prompt 25, contract §6.5). Nothing
satisfied is rebuilt, and every satisfied brief is verified before something is built on it:
bolts 085/086 verify the seven Phase 1 stories — six of the twelve satisfied briefs; the other
six (orchestrator wiring 11b, security-auditor, concurrency-auditor, bug-lifecycle,
fix-verification, mailbox scan 31b) are verified in the plan stage of the construction bolt that
carries them.

### Out of Scope (hard constraints)
- ❌ **No application code changes — ever.** The system is read-only on app source
  (guide convention). Allowed writes only: the ledger, code index, sandbox, run
  reports, fix-request store, and — Phase 5, with owner approval — new test files.
- ❌ **The knowledge builder / knowledge ledger system itself.** `intent-lookup`
  *reads* its `ledger-query` interface; building that system is a separate intent.
  Its absence blocks only the oracle stories (see bolt 091 `blocks` flag).
- ❌ **Auto-applied patches.** `fix-proposal` proposes diffs; it never applies them.
- ❌ **Modifying the guide.** `docs/agent-systems/bug-hunter-build-guide.md` is the spec of
  record; construction follows it verbatim (deviations are owner decisions).
- ❌ **No deployment implications.** This is tooling; roadmap Phase 6 (deployment)
  is untouched.
- ❌ **Re-building what the review loop already does.** The 12 satisfied briefs —
  Prompts 1, 3–7 (Phase 1 bar the three-audience record of Prompt 2), 11b, 19, 22, 26, 31,
  31b — are not re-implemented; the guide's status table is the record of who satisfies them.

---

## Functional Requirements

### FR-1: skill-creator build loop (cross-cutting)
- **Description**: **Amended 2026-09:** components that extend the review loop are scripts or
  skill edits under `reviews/lib` and `.claude/skills`, built and tested in that tree (see the
  header note and units.md); skill-creator is mandatory only for a new standalone skill (the
  oracle tier's `intent-lookup`). Every component is built by pasting its brief (Prompt N) from the
  guide into the **skill-creator skill** and following the guide's build loop: build →
  run the brief's three test prompts → confirm/fix → only then take the next component
  in the master build order (dependency-ordered, top to bottom).
- **Acceptance Criteria**: Each component lands where its brief places it — a script or skill edit
  under `reviews/lib` / `.claude/skills` for a loop extension, a skill under
  `.claude/skills/{name}/` with the guide's exact component name for a new standalone skill;
  construction log records the build route + the three test-prompt results per component; build
  order respected (a component's dependencies were built first); for a new standalone skill only,
  if skill-creator is unavailable, the bolt STOPS.
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

### FR-3: Phase 1 — Skeleton (Prompts 1–7) — satisfied 2026-09
> Met by the review loop under `reviews/`, bar Prompt 2's three-audience record; kept for the
> record, not for building (see Out of Scope).
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
- **Description**: **Rewritten 2026-09 around decision attachment** (guide Prompt 25,
  integration contract §6.5), which replaces suppression learning. A dismissal never
  becomes a filter: when a finding is found again, the owner's prior decision is
  **attached to it** and the finding is **re-argued** on the new evidence — the loop
  never suppresses a hunter, so nothing can silently stop being looked for. What is
  still missing is the measurement around it: a standing eval corpus (labeled real +
  seeded synthetic bugs, plus a **poison fixture** a pass must not "find"), recall and
  escape metrics, and the curator work — the system self-review and the speed report —
  run automatically instead of by hand. The bug lifecycle (evidence-based self-closing,
  regression flagging) already exists.
- **Acceptance Criteria**: A re-found finding arrives carrying its prior decision and a
  fresh argument, never as a hidden suppression; the **overturn rate** — how often an
  attached decision is reversed on re-argument — is measured and trended; recall is
  measured against the standing corpus rather than asserted; escapes (a bug that
  reached `main` unfound) are counted; each eval run records the model it ran on.
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
| D3 | **Amended 2026-09.** Outputs of the **pre-merge mode** live under `reviews/**`, in the review loop's own layout (records tree, per-pass review/summary documents, `metrics.jsonl`, state). `bug-hunting/**` stays **reserved for the standing-sweep mode** — the ledger, run reports, eval corpus and fix-request store of a scheduled whole-repo pass, when that mode is built | Two modes, one engine: the mode that exists already has a write-root, and inventing a second one for it would fork the records. Reserving `bug-hunting/**` keeps the sweep's store out of both app source and `memory-bank/` (AI-DLC's tree), and keeps path knowledge behind the same few files |
| D4 | **Amended 2026-09: the sandbox recipe is not a gate.** The execution proof runs the repo's own test commands, so bolt 087 can start without one. A recipe (the repo's `docker-compose` assets, API + Postgres, adapted once by the owner) is needed only if the owner picks the **containerised variant of Prompt 10's proof** | The gap the re-scope asks for is "a failing test written by a non-fixer, naming the commit it was taken on" — satisfiable on the host. Container isolation is an option on top, not a precondition; keeping it as a gate would have stalled the cheapest bolt |
| D5 | **Amended 2026-09: satisfied.** `concurrency-auditor-agent` (Prompt 22) is the review loop's **`race` lens** — ✓ in the guide's status table; no bolt work | The decision to have it at all was right for this async/await + BackgroundServices + EF stack; it simply already exists |
| D6 | Oracle stories (Prompts 24–24d) carry a cross-system gate | `intent-lookup` reads the knowledge builder's `ledger-query` interface — now specified in `docs/agent-systems/integration-contract.md` (§2/§3) and built per `docs/agent-systems/knowledge-builder-build-guide.md`; bolt 091 runs after the knowledge builder's Phases 1–2 (contract §7), unless the owner descopes the oracle |
| D7 | Story files do not duplicate brief content | The guide is the spec of record; stories carry tracking + acceptance criteria + the skill-creator mandate, and point at Prompt N |

## Traceability

| FR | Unit | Stories |
|----|------|---------|
| FR-1, FR-2 | all (cross-cutting) | every story |
| FR-3 | 001-phase-1-skeleton | 7 (Prompts 1–7) |
| FR-4 | 002-phase-2-trust | 5 (Prompts 8–11b) |
| FR-5 | 003-phase-3-breadth-and-scale | 17 (Prompts 12–24d) |
| FR-6 | 004-phase-4-learn-and-measure | 6 (Prompts 25–29b) |
| FR-7 | 005-phase-5-remediation | 5 (Prompts 30–33 + 31b) |
| FR-8 | 006-optional-integration | 3 (Prompts A–C) |

Story files stay where they are, and so do the six units. The oracle stories (014–017 of unit
003) are built by that unit's **last bolt** — 091, gated on the knowledge builder — not by a
unit of their own.
