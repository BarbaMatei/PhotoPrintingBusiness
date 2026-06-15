---
id: 001-app-mapping
unit: 003-phase-3-breadth-and-scale
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 088-phase-3-map-and-reachability
implemented: false
---

# Story: 001-app-mapping (guide Prompt 12)

## User Story

**As** the Map slot
**I want** a maintained application map (entry points, modules, flows, risk classes)
**So that** the Orchestrator plans hunts against the real structure and prioritizes high-risk flows

## Acceptance Criteria

- [ ] **Given** Prompt 12, **When** built, **Then** skill `app-mapping` exists, created via skill-creator, and the brief's three test prompts pass (map this repo; risk-class tagging; new-route refresh shows a diff)
- [ ] **Given** mapping, **When** recording via `ledger-io`, **Then** the `application_map` holds entry points (routes/controllers, UI actions, CLI, jobs, event handlers, public APIs), modules + dependency edges, end-to-end flows (ordered component lists entry→data layer), and external dependencies
- [ ] **Given** prioritization, **When** tagging, **Then** each flow carries a **risk class** (auth, money, data-write = high; read-only = lower)
- [ ] **Given** a refresh, **When** the map exists, **Then** it diffs and updates rather than overwriting
- [ ] **Given** the v3.2 freshness stamp, **When** publishing, **Then** the map carries **`built_at_commit`** — the stamp the contract's §3 evaluates flow-map staleness against (same threshold mechanism as §5)

## Technical Notes

- ⚠️ Build by pasting **Prompt 12** from `docs/agent-systems/bug-hunter-build-guide-v3.6.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- Repo grounding: high-risk flows here are auth, checkout/payment (Stripe/EuPlatesc),
  order state machine, uploads, invoicing (ANAF), shipping (Sameday); jobs =
  BackgroundServices; events = SignalR hubs + webhooks.
- Per the v3.1 brief: the published flow IDs are the **cross-system flow identity**
  (Integration Contract §3) — the knowledge builder's `ledger-query` resolves flow
  queries through this map; keep flow IDs stable across refreshes.

## Dependencies

### Requires
- ledger-io (bolt 085)

### Enables
- reachability, flow-tracing, orchestrator scale extension (24d)

## Out of Scope

- Symbol-level indexing (code-index).
