---
id: 002-code-index
unit: 003-phase-3-breadth-and-scale
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 088-phase-3-map-and-reachability
implemented: false
---

# Story: 002-code-index (guide Prompt 13)

**Workbench seam:** a new tool under `reviews/lib/`, shared with the knowledge builder (contract §7) — missing entirely today.

## User Story

**As** every agent working on a large codebase
**I want** a searchable symbol/reference inventory with slice retrieval
**So that** agents pull just the relevant code instead of holding everything in context

## Acceptance Criteria

- [ ] **Given** Prompt 13, **When** built, **Then** skill `code-index` exists, created via skill-creator, and the brief's three test prompts pass (find definition + callers; 20-line slice around a location; re-index only the latest commit's changes) *(2026-09: a component that extends the review loop is a script or skill edit under `reviews/lib` / `.claude/skills`, built and tested there; skill-creator applies only to a new standalone skill — FR-1 as amended)*
- [ ] **Given** the operations, **When** queried, **Then** `find_symbol`, `find_callers`, `find_callees`, `definition_of`, `search_text`, `slice_around(location, context_lines)` all work (ctags-style map + grep-backed search is an acceptable baseline)
- [ ] **Given** incrementality, **When** given a SHA, **Then** only changed files re-index
- [ ] **Given** the v3.3 store location, **When** persisting, **Then** the index lives under `bug-hunting/code-index/` (Integration Contract §1 — derived, regenerable; the KB may refresh it as a shared tool)
- [ ] **Given** concurrent readers (v3.4, review I6), **When** refreshing, **Then** regeneration goes to a temp location and publishes by **atomic pointer swap** — never in-place — so a reader always resolves a complete index; the published index is stamped **`built_at_commit`**
- [ ] **Given** the v3.5 artifact policy (review J3), **When** persisting, **Then** the index is **gitignored** — an untracked, regenerable build artifact: never committed by either system, never part of a publish-commit, outside the close audit; regenerated on demand (and against the restored commit on rollback)
- [ ] **Given** honesty (NFR-5), **When** resolution fails (dynamic dispatch, reflection, DI), **Then** limits are stated rather than guessed around

## Technical Notes

- ⚠️ Build by pasting **Prompt 13** from `docs/agent-systems/bug-hunter-build-guide.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- Index storage belongs under `bug-hunting/` (allowed write set). Both languages
  matter: C# (API) and TypeScript (Angular).

## Dependencies

### Requires
- (none per the brief)

### Enables
- reachability, flow-tracing, taint-analysis, file-sweeper, intent-lookup

## Out of Scope

- Full semantic call-graph guarantees (be honest about limits instead).
