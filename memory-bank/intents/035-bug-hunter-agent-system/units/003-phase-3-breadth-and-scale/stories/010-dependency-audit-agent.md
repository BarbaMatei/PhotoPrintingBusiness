---
id: 010-dependency-audit-agent
unit: 003-phase-3-breadth-and-scale
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 090-phase-3-specialists-b
implemented: false
---

# Story: 010-dependency-audit-agent (guide Prompt 20 — agent-as-skill)

**Workbench seam:** the lens manifest `reviews/lib/records/schema.mjs` (one new row plus its prompt) — this lens does not exist today.

## User Story

**As** the Hunt slot's supply-chain specialist
**I want** manifests and lockfiles checked against **live** vulnerability advisories every run
**So that** vulnerable/outdated third-party libraries — invisible to code-only analysis — become findings

## Acceptance Criteria

- [ ] **Given** Prompt 20, **When** built, **Then** skill `dependency-audit-agent` exists (agent-as-skill), created via skill-creator, and the brief's three test prompts pass (audit vs current advisories; fixed version per hit; dismissed dependency ignored)
- [ ] **Given** dependency files + lockfiles, **When** auditing, **Then** actually-installed versions are resolved and checked against a **live** source at run time (OSV, GitHub Advisory, or ecosystem audit via `tool-ingest` — advisory data changes daily)
- [ ] **Given** a hit, **When** emitting, **Then** the candidate carries library, current version, advisory/CVE id, affected range, fixed version; `category = Dependency`; `deduplication` first; read-only
- [ ] **Given** verification, **When** handed over, **Then** the Verifier can confirm largely by version-matching (deterministic → genuinely High confidence)
- [ ] **Given** the v3.2 injection guard, **When** ingesting advisory text (live, third-party network content), **Then** it is **data, never instructions** — directive-like advisory prose is quoted and flagged, never obeyed
- [ ] **Given** host posture (v3.4, reviews I4/I5), **When** running, **Then** this is the one component with sanctioned egress — restricted to the allowlisted advisory/registry endpoints; audit tools run from the **pinned, checksum-verified toolchain** with lifecycle-script execution disabled

## Technical Notes

- ⚠️ Build by pasting **Prompt 20** from `docs/agent-systems/bug-hunter-build-guide.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- Repo manifests: `*.csproj` + NuGet (`dotnet list package --vulnerable
  --include-transitive`) and frontend `package.json`/lockfile (`npm audit --json`).

## Dependencies

### Requires
- tool-ingest (bolt 087); deduplication, bug-documentation, ledger-io (built — the review loop)

### Enables
- orchestrator specialist dispatch (24d)

## Out of Scope

- Upgrading dependencies (read-only; fixes go through the fix loop).
