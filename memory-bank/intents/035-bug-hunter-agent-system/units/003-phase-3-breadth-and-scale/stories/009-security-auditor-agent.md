---
id: 009-security-auditor-agent
unit: 003-phase-3-breadth-and-scale
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 089-phase-3-specialists-a
implemented: false
---

# Story: 009-security-auditor-agent (guide Prompt 19 — agent-as-skill)

## User Story

**As** the Hunt slot's security specialist
**I want** a dedicated pass over taint, authn/authz, secrets, and the common vuln classes
**So that** security bugs get a hunter whose whole attention is security

## Acceptance Criteria

- [ ] **Given** Prompt 19, **When** built, **Then** skill `security-auditor-agent` exists (agent-as-skill), created via skill-creator, and the brief's three test prompts pass (auth+payment security pass; object-level authz on every load-by-ID endpoint; hardcoded secrets / weak crypto)
- [ ] **Given** assigned flows/files, **When** auditing, **Then** `taint-analysis` runs for data-flow; authn/authz checked at each protected entry point and step (missing checks, broken object-level authorization, privilege escalation); exposed secrets and insecure config in code scanned; injection/XSS/SSRF/path traversal/insecure deserialization/weak crypto/open redirects checked
- [ ] **Given** emission, **When** done, **Then** `deduplication` first; candidates only with precise `category_guess` + data-flow evidence; read-only

## Technical Notes

- ⚠️ Build by pasting **Prompt 19** from `docs/agent-systems/bug-hunter-build-guide-v3.6.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- Repo grounding: ownership checks on order/upload/invoice load-by-ID endpoints,
  guest-session token handling, webhook signature verification (Stripe/EuPlatesc),
  JWT/refresh-cookie flows — all prime object-level-authz territory.

## Dependencies

### Requires
- 006-taint-analysis; deduplication, bug-documentation, ledger-io (bolt 085);
  002-code-index

### Enables
- orchestrator specialist dispatch (24d); hunters-contract-ext (24b)

## Out of Scope

- Dependency CVEs (P20) and config/infra (P21) — separate specialists.
