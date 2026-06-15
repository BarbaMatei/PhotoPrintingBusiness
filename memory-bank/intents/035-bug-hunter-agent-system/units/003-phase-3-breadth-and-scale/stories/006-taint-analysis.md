---
id: 006-taint-analysis
unit: 003-phase-3-breadth-and-scale
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 089-phase-3-specialists-a
implemented: false
---

# Story: 006-taint-analysis (guide Prompt 16)

## User Story

**As** the security hunter's core procedure
**I want** untrusted data tracked from sources to dangerous sinks with sanitizer awareness
**So that** injection-class vulnerabilities are found with path evidence — and safe flows aren't flagged

## Acceptance Criteria

- [ ] **Given** Prompt 16, **When** built, **Then** skill `taint-analysis` exists, created via skill-creator, and the brief's three test prompts pass (param→SQL unsanitized; upload handler path traversal; escaped-before-output NOT flagged)
- [ ] **Given** sources (request params, headers, uploads, external responses) and sinks (SQL/command execution, file paths, HTML output, deserialization, redirects), **When** tracing via `code-index`, **Then** unsanitized source→sink paths emit candidates with the path as evidence and a precise `category_guess` (SQL injection, command injection, XSS, path traversal, SSRF, insecure deserialization, open redirect)
- [ ] **Given** sanitizers, **When** data is correctly handled for the sink type, **Then** it is marked clean (no false flag); **Given** unresolvable paths, **Then** `unknown` is surfaced rather than guessed

## Technical Notes

- ⚠️ Build by pasting **Prompt 16** from `docs/agent-systems/bug-hunter-build-guide.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- Repo grounding: EF Core parameterization is the dominant SQL sanitizer (raw-SQL
  spots are the exceptions to hunt); upload paths and webhook payloads
  (Stripe/EuPlatesc/Sameday/ANAF) are first-class sources.

## Dependencies

### Requires
- 002-code-index

### Enables
- security-auditor-agent (P19)

## Out of Scope

- Authn/authz checks (security-auditor handles those separately).
