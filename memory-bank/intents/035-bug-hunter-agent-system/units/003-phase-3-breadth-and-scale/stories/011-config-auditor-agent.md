---
id: 011-config-auditor-agent
unit: 003-phase-3-breadth-and-scale
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 090-phase-3-specialists-b
implemented: false
---

# Story: 011-config-auditor-agent (guide Prompt 21 — agent-as-skill)

**Workbench seam:** the lens manifest `reviews/lib/records/schema.mjs` (one new row plus its prompt) — this lens does not exist today.

## User Story

**As** the Hunt slot's configuration/infrastructure specialist
**I want** the non-code files audited (env, Docker, compose, CI, IaC)
**So that** the bug class living outside code — committed secrets, permissive settings, exposed ports — stops being skipped

## Acceptance Criteria

- [ ] **Given** Prompt 21, **When** built, **Then** skill `config-auditor-agent` exists (agent-as-skill), created via skill-creator, and the brief's three test prompts pass (Dockerfile+compose audit; committed secret / debug-on found; wildcard CORS flagged)
- [ ] **Given** env/config files, Dockerfiles, compose files, CI configs, IaC, **When** auditing, **Then** checks cover: committed secrets, overly-permissive settings (0.0.0.0 binding, debug mode, wildcard CORS, world-readable permissions), default/weak credentials, exposed ports, missing security headers
- [ ] **Given** tooling, **When** available, **Then** deterministic scanners (gitleaks, hadolint, checkov, tfsec) feed in via `tool-ingest`, plus reasoning on top; `category = Configuration`; `deduplication` first; read-only

## Technical Notes

- ⚠️ Build by pasting **Prompt 21** from `docs/agent-systems/bug-hunter-build-guide.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- Repo surface: Dockerfile + docker-compose (dev/prod + Caddy), GitHub Actions
  workflows, appsettings tiers, `.env.example` files — note the repo already runs
  gitleaks in CI (bolt 041); reuse, don't duplicate, via `tool-ingest`.

## Dependencies

### Requires
- tool-ingest (bolt 087); deduplication, bug-documentation, ledger-io (built — the review loop)

### Enables
- orchestrator specialist dispatch (24d)

## Out of Scope

- Fixing configs (read-only); secrets rotation (separate concern, intent 018 did it).
