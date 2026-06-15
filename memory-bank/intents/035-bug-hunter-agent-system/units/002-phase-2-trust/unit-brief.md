---
unit: 002-phase-2-trust
intent: 035-bug-hunter-agent-system
phase: inception
status: ready
unit_type: tooling
default_bolt_type: simple-construction-bolt
created: 2026-06-10T10:40:14Z
updated: 2026-06-10T10:40:14Z
---

# Unit Brief: Phase 2 — Trust

## Purpose

Stop trusting hunches. Fill the Verify slot with the hardened `bug-verifier`
(disprove-first; dynamic confirmation in a sandbox; sandbox-vs-commit check;
flaky-test double-run), add real risk scoring (severity × confidence), ingest
deterministic-tool output as candidates, pin runs to commits, and wire the
orchestrator's Verify/Triage slots — all without rewriting Phase 1.

## Scope

### In Scope — 5 briefs (guide Prompts 8–11b)
| Component | Brief | Role |
|-----------|-------|------|
| `severity-scoring` | Prompt 8 | severity × confidence → 0–100 risk score (extensible) |
| `tool-ingest` | Prompt 9 | Normalize linter/type-checker/SAST/test output to candidates |
| `bug-verifier` | Prompt 10 | The quality gate (agent-as-skill) — fills Verify |
| `git-revision-tracking` | Prompt 11 | Commit pinning; fixed/moved reconciliation (propose, don't auto-close) |
| orchestrator extension | Prompt 11b | Point Verify at the Verifier; Triage uses scoring; SHA at open/close |

### Out of Scope
- Reachability (P3 extends the formula at its planned seam), oracle confidence
  (P3), suppression learning (P4).

---

## ⚠️ Construction Method (owner mandate + guide Part I — MUST follow)

**Each component MUST be created with the `skill-creator` skill** (`Skill` tool →
`skill-creator:skill-creator`): paste Prompt N from
`docs/agent-systems/bug-hunter-build-guide-v3.6.md`, build, **run the brief's three test prompts**,
fix, then move on — in order. Prompt 11b **re-opens** the existing `orchestrator`
skill (extension at its seam — no restructuring; Phase 1 tests must still pass).
If skill-creator is unavailable, **STOP and report**.

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-4 | Phase 2 trust (Prompts 8–11b) | Must |
| FR-1, FR-2 | Cross-cutting | Must |

## Story Summary

- **Total Stories**: 5 — **Must Have**: 5

### Stories
- [ ] **001-severity-scoring** — Must — Planned
- [ ] **002-tool-ingest** — Must — Planned
- [ ] **003-bug-verifier** — Must — Planned
- [ ] **004-git-revision-tracking** — Must — Planned
- [ ] **005-orchestrator-verify-wiring** — Must — Planned

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 001-phase-1-skeleton | Verifier writes via bug-documentation/ledger-io; orchestrator extension re-opens the P1 skill |

### Depended By
| Unit | Reason |
|------|--------|
| 003–005 | Reachability extends scoring; harvest/fix loop reuse the Verifier's proving tests |

## Technical Context

- **Sandbox (external prerequisite, requirements D4)**: owner provides the recipe
  once — adapt the repo's compose assets (API + Postgres; .NET test runner). The
  Verifier builds/destroys a fresh container per run; outbound network locked;
  time/CPU/memory capped; never real production data (NFR-3).
- **v3 hardening (binding)**: before trusting any sandbox result, confirm the
  container builds the **commit under analysis** — stale recipe ⇒ "could not verify
  in sandbox" + report the broken environment (never silent static fallback); proof
  tests run **more than once** — flickering ⇒ "confirmation unreliable (flaky test)".
- **Repo tools for `tool-ingest`**: `dotnet build`/analyzers, `dotnet test` logs,
  ESLint/tsc (frontend), SARIF input; dedupe across tools; all tool findings are
  candidates, not confirmed bugs.
- Confidence ladder: High = dynamically confirmed or tool-corroborated; Medium =
  strong static reasoning; Low = plausible-unconfirmed — **all reported** (floor
  handles prominence).

## Constraints

- Read-only on app source — the Verifier may run code in the sandbox only; its
  writes are sandbox, ledger, report (NFR-1).

## Success Criteria

- [ ] All 5 briefs built via skill-creator; three test prompts each, passing.
- [ ] A run now emits per-finding confidence + risk score; blanket "unverified" label
      gone; commit SHA recorded at open, reconciliation proposals at close.

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 087-phase-2-trust | simple-construction-bolt | all 5 | Fill the Verify slot; wire scoring + commit tracking |
