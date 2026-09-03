---
id: 003-reachability
unit: 003-phase-3-breadth-and-scale
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 088-phase-3-map-and-reachability
implemented: false
---

# Story: 003-reachability (guide Prompt 14)

**Workbench seam:** a new tool under `reviews/lib/`, read by `reviews/lib/records/schema.mjs` for the risk weight — missing entirely today.

## User Story

**As** the single biggest false-positive filter
**I want** every candidate's location traced back to a real entry point
**So that** dead-code bugs stop outranking reachable ones — without dynamic stacks getting flattened to "unknown"

## Acceptance Criteria

- [ ] **Given** Prompt 14, **When** built, **Then** skill `reachability` exists, created via skill-creator, and the brief's three test prompts pass (is X reachable; deleted-route-only → unreachable; reflection/DI → unknown without down-ranking everything) *(2026-09: a component that extends the review loop is a script or skill edit under `reviews/lib` / `.claude/skills`, built and tested there; skill-creator applies only to a new standalone skill — FR-1 as amended)*
- [ ] **Given** a target location + the map, **When** tracing backwards through the call graph/references, **Then** the answer is `reachable` / `unreachable` / `unknown` — `unknown` is a valid honest answer (never guess `reachable`, never drop the bug), and reachable answers include the shortest entry→target path as evidence
- [ ] **Given** the v3 **framework-aware unknown weight**, **When** the stack is metaprogramming-heavy (DI, decorators, route registration, event buses, reflection, serialization-driven calls), **Then** the stack is detected and the "unknown" penalty calibrated so genuinely reachable bugs aren't systematically flattened

## Technical Notes

- ⚠️ Build by pasting **Prompt 14** from `docs/agent-systems/bug-hunter-build-guide.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- This repo IS the framework-aware case: ASP.NET Core DI + attribute routing +
  EF/SignalR mean static "unknown" is common — the calibration matters here, not as
  a corner case.

## Dependencies

### Requires
- 002-code-index, 001-app-mapping

### Enables
- 004-severity-scoring-reachability-ext (14b); Verifier wiring via 24d

## Out of Scope

- Changing the risk formula (that's 14b's seam extension).
