---
id: 015-hunters-contract-ext
unit: 003-phase-3-breadth-and-scale
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 091-phase-3-oracle-grounding
implemented: false
---

# Story: 015-hunters-contract-ext (guide Prompt 24b — EXTENSION across hunters)

## User Story

**As** every hunter
**I want** one added capability: check a location's governing contracts and surface contradictions
**So that** real logic bugs the model couldn't invent on its own — code violating a documented contract — become candidates with the contract as evidence

## Acceptance Criteria

- [ ] **Given** Prompt 24b, **When** applied, **Then** the hunters (`general-hunter`, `flow-tracer-agent`, `file-sweeper-agent`, `security-auditor-agent`, and the other built specialists) are **re-opened and extended at their seam** via skill-creator, and the brief's three test prompts pass (contract violation surfaced with contract cited; no-contract flow behaves exactly as before; contract-consistent flow not flagged)
- [ ] **Given** a location under examination, **When** `intent-lookup` returns a contradicted contract (e.g. "return 404 on missing" vs code returning 200+null), **Then** a candidate is surfaced with the contradicted contract as evidence
- [ ] **Given** conventions, **When** extended, **Then** still candidates-only, still dedup-first; **Given** NFR-2, **Then** every hunter's original test prompts still pass

## Technical Notes

- ⚠️ This is an **extension brief across multiple skills**: paste **Prompt 24b** from
  `docs/agent-systems/bug-hunter-build-guide-v3.6.md` into the **skill-creator** skill (`Skill`
  tool → `skill-creator:skill-creator`), re-opening each hunter in turn. Re-run each
  hunter's original tests after. STOP and report if skill-creator is unavailable.
- Apply to exactly the hunters that exist at build time (incl.
  `dependency-audit-agent`/`config-auditor-agent`/`concurrency-auditor-agent` if
  built) — the brief's "and the others".

## Dependencies

### Requires
- 014-intent-lookup; all hunters from bolts 086, 089, 090

### Enables
- Contract-grounded candidates feeding 24c's confidence rules

## Out of Scope

- Confidence weighting (24c — the Verifier's side of the oracle).
