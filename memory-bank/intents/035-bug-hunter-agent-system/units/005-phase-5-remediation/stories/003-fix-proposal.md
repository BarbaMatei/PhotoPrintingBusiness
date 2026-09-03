---
id: 003-fix-proposal
unit: 005-phase-5-remediation
intent: 035-bug-hunter-agent-system
status: ready
priority: should
created: 2026-06-10T10:40:14Z
assigned_bolt: 093-phase-5-remediation
implemented: false
---

# Story: 003-fix-proposal (guide Prompt 32)

**Workbench seam:** `.claude/skills/fix-review/SKILL.md` — partial by design: the loop’s fixer applies patches directly, so the never-apply rule does not describe this engine. No bolt work planned.

## User Story

**As** the owner deciding how to fix a confirmed bug
**I want** a minimal draft patch, validated in the sandbox against the surrounding suite
**So that** the first-draft effort is saved while every decision stays mine — and "validated" actually means validated

## Acceptance Criteria

- [ ] **Given** Prompt 32, **When** built, **Then** skill `fix-proposal` exists, created via skill-creator, and the brief's three test prompts pass (off-by-one fix validated vs harvested test + module suite; unvalidated proposal when no test; own-test-passes-but-sibling-breaks → NOT labeled validated)
- [ ] **Given** a confirmed bug, **When** drafting, **Then** a minimal diff addressing the **root cause** is produced with a short rationale
- [ ] **Given** a harvested test, **When** validating, **Then** the patch is applied **in the sandbox** and not just the bug's own test but the relevant surrounding suite runs (at minimum the same module's tests) — "validated" only if the bug's test passes AND nothing else newly breaks; if the wider set isn't feasible, the label downgrades to "passes its own test, broader impact unchecked"
- [ ] **Given** the hard boundary, **When** done, **Then** the patch is **never applied to the real repository** — attached to the bug record/report as a proposal only

## Technical Notes

- ⚠️ Build by pasting **Prompt 32** from `docs/agent-systems/bug-hunter-build-guide.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- In the AI-DLC loop, these proposals ride along in the fix-request record
  (`fix_direction` + proposed diff) as input to the fix-bolt — advisory, not binding.

## Dependencies

### Requires
- bug-verifier (bolt 087), 001-regression-harvest; the sandbox

### Enables
- Richer fix-requests (story 004)

## Out of Scope

- Applying patches anywhere outside the sandbox; multi-file refactors.
