---
id: 003-deduplication
unit: 001-phase-1-skeleton
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 085-phase-1-skeleton-core
implemented: false
---

# Story: 003-deduplication (guide Prompt 3)

## User Story

**As** the pipeline's Triage stage (and every hunter before emitting)
**I want** a verdict on whether a candidate is genuinely NEW vs duplicate/dismissed/suppressed
**So that** sequential runs go deeper instead of re-reporting the same findings

## Acceptance Criteria

- [ ] **Given** Prompt 3, **When** built, **Then** skill `deduplication` exists, created via skill-creator, and the brief's four test prompts pass (duplicate check vs ledger; dismissed-signature drop; same-line-different-defect → NEW; same-signature-but-distinct-defect → NEW, v3.2)
- [ ] **Given** a candidate, **When** checking, **Then** the `signature` (path::symbol::bug_type) is computed **normalized so a moved line still matches**, and the verdict is one of `{new | duplicate | dismissed | suppressed}` with `matched_id_or_pattern` + rationale
- [ ] **Given** the ledger via `ledger-io`, **When** matching, **Then** `bug_index` duplicates link to the existing ID (not re-reported), `dismissed` entries drop, `suppression_patterns` matches drop with the pattern noted — and the (empty-until-Phase-4) patterns section is already honored so no change is needed later
- [ ] **Given** the brief's caution, **When** judging, **Then** "same area" is NOT collapsed into "same bug" — only true duplicates merge
- [ ] **Given** the v3.2 collision guard, **When** a signature matches, **Then** it is treated as a **candidate** duplicate only — hypotheses/lines/trigger conditions are compared before collapsing, so two distinct same-type defects in one symbol both survive as separate bugs

## Technical Notes

- ⚠️ Build by pasting **Prompt 3** from `docs/agent-systems/bug-hunter-build-guide.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's four test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.

## Dependencies

### Requires
- 001-ledger-io

### Enables
- general-hunter and every later hunter; the Triage slot

## Out of Scope

- Root-cause grouping across distinct true bugs (Prompt 23, Phase 3).
