---
unit: 003-phase-3-breadth-and-scale
intent: 035-bug-hunter-agent-system
phase: inception
status: ready
unit_type: tooling
default_bolt_type: simple-construction-bolt
created: 2026-06-10T10:40:14Z
updated: 2026-06-10T10:40:14Z
---

# Unit Brief: Phase 3 — Breadth & Scale (+ Oracle)

## Purpose

See more, scan smarter, control cost, and ground findings in real intent. Adds the
application map and searchable code index, splits hunting into specialists, adds two
whole new bug classes (dependency + config/infra), adds reachability as the third
risk factor, groups symptoms by root cause, makes runs cheap (budget, incremental,
cheap-first), and reads the **knowledge ledger as an oracle** so a spec violation is
distinguishable from the model's opinion. All via extensions at the seams Phase 1–2
planned — never rebuilds.

## Scope

### In Scope — 17 briefs (guide Prompts 12–24d)
| Component | Brief | Role |
|-----------|-------|------|
| `app-mapping` | 12 | Entry points, modules, flows + risk classes (diff on refresh) |
| `code-index` | 13 | Symbol/reference index; slice retrieval; incremental |
| `reachability` | 14 | reachable/unreachable/unknown + framework-aware unknown weight |
| severity-scoring ext | 14b | risk = severity × confidence × **reachability** |
| `flow-tracing` | 15 | Walk one flow, check every handoff (shared procedure) |
| `taint-analysis` | 16 | Source→sink tracking with sanitizer awareness |
| `flow-tracer-agent` | 17 | Top-down hunt over priority flows |
| `file-sweeper-agent` | 18 | Bottom-up local-defect sweep (tools first) |
| `security-auditor-agent` | 19 | Taint + authn/authz + secrets + vuln classes |
| `dependency-audit-agent` | 20 | Manifests/lockfiles vs live advisories (CVEs) |
| `config-auditor-agent` | 21 | Config/infra bugs (compose, CI, env, IaC) |
| `concurrency-auditor-agent` | 22 | Races/deadlocks/TOCTOU — **Should** (async-heavy stack) |
| `root-cause-clustering` | 23 | N symptoms → 1 multi-location bug (conservative) |
| `intent-lookup` | 24 | Oracle read: contracts by location/flow/symbol |
| hunters ext | 24b | Surface contract-contradiction candidates |
| verifier+scoring ext | 24c | Contract-corroborated confidence; "intent-unconfirmed" tag |
| orchestrator ext | 24d | Map refresh; specialist dispatch; cost control; oracle wiring |

### Out of Scope
- The knowledge builder system itself (only its `ledger-query` read interface is
  consumed — requirements D6).

---

## ⚠️ Construction Method (owner mandate + guide Part I — MUST follow)

**Each component MUST be created with the `skill-creator` skill** (`Skill` tool →
`skill-creator:skill-creator`): paste Prompt N from
`docs/agent-systems/bug-hunter-build-guide.md`, build, **run the brief's three test prompts**,
fix, then move on — in order. Briefs 14b/24b/24c/24d **re-open existing skills**
(seam extensions; prior tests must still pass). If skill-creator is unavailable,
**STOP and report**.

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-5 | Phase 3 breadth & scale + oracle (Prompts 12–24d) | Must (story 012: Should) |
| FR-1, FR-2 | Cross-cutting | Must |

## Story Summary

- **Total Stories**: 17 — **Must**: 16 — **Should**: 1 (concurrency-auditor)

### Stories
- [ ] **001-app-mapping** — Must · [ ] **002-code-index** — Must
- [ ] **003-reachability** — Must · [ ] **004-severity-scoring-reachability-ext** — Must
- [ ] **005-flow-tracing** — Must · [ ] **006-taint-analysis** — Must
- [ ] **007-flow-tracer-agent** — Must · [ ] **008-file-sweeper-agent** — Must
- [ ] **009-security-auditor-agent** — Must · [ ] **010-dependency-audit-agent** — Must
- [ ] **011-config-auditor-agent** — Must · [ ] **012-concurrency-auditor-agent** — Should
- [ ] **013-root-cause-clustering** — Must · [ ] **014-intent-lookup** — Must ⛔ext-dep
- [ ] **015-hunters-contract-ext** — Must ⛔ext-dep · [ ] **016-verifier-scoring-contract-ext** — Must ⛔ext-dep
- [ ] **017-orchestrator-scale-ext** — Must ⛔ext-dep (oracle part)

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 002-phase-2-trust | Scoring formula to extend; Verifier to extend; tool-ingest for specialists |

### Depended By
| Unit | Reason |
|------|--------|
| 004, 005 | Learning/remediation operate on the full-breadth pipeline |

## Technical Context

- **Stack grounding**: .NET 8 API (controllers, BackgroundServices, EF Core,
  SignalR) + Angular frontend. DI + attribute routing make static reachability
  frequently "unknown" — exactly the framework-aware unknown weight case (Prompt 14).
  High-risk flows for `app-mapping` risk classes: auth, checkout/payment (Stripe,
  EuPlatesc), order state machine, uploads, invoicing (ANAF), shipping (Sameday).
- **Dependency manifests** for Prompt 20: `*.csproj` / NuGet lockfiles + frontend
  `package.json`/lockfile; live sources: OSV / GitHub Advisory / `npm audit` /
  `dotnet list package --vulnerable` via `tool-ingest` — query at run time.
- **Oracle (⛔ cross-system gate)**: `intent-lookup` queries the knowledge builder's
  `ledger-query` interface per the normative envelope in
  `docs/agent-systems/integration-contract.md` §2 (flow identity per §3); only `intent_contracts`
  are authority; superseded / not-yet-`done` contracts returned tagged; `contested`
  contracts never raise confidence. Bolt 091 runs after the knowledge builder's
  Phases 1–2 (contract §7), unless the owner descopes the oracle.

## Constraints

- Read-only on app source. Extensions only at the briefs' named seams (NFR-2).
- Bolts 089 ∥ 090 are the only parallel pair (disjoint new skills); 091 re-opens
  skills from 086–090 and must run alone.

## Success Criteria

- [ ] All 17 briefs built via skill-creator; three test prompts each, passing.
- [ ] A run dispatches specialists by risk class, scans incrementally under a budget,
      and (oracle available) reports a contract-contradiction with the contract cited.

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 088-phase-3-map-and-reachability | simple-construction-bolt | 001–005 | Map, index, reachability (+14b), flow-tracing |
| 089-phase-3-specialists-a | simple-construction-bolt | 006–009 | Taint + flow/file/security hunters |
| 090-phase-3-specialists-b | simple-construction-bolt | 010–013 | Dependency/config/concurrency hunters + clustering |
| 091-phase-3-oracle-grounding | simple-construction-bolt | 014–017 | intent-lookup + the three oracle/scale extensions |
