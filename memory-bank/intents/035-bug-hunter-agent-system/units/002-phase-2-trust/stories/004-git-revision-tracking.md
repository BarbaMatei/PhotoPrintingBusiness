---
id: 004-git-revision-tracking
unit: 002-phase-2-trust
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 087-phase-2-trust
implemented: false
---

# Story: 004-git-revision-tracking (guide Prompt 11)

## User Story

**As** the ledger's defense against code drift
**I want** every run pinned to a commit and prior bugs reconciled against the current code
**So that** the ledger doesn't rot — fixed bugs get proposed closed with evidence, moved bugs keep resolving

## Acceptance Criteria

- [ ] **Given** Prompt 11, **When** built, **Then** skill `git-revision-tracking` exists, created via skill-creator, and the brief's three test prompts pass (SHA into run metadata; deleted code → propose Fixed with evidence; moved 40 lines → location updated)
- [ ] **Given** run start, **When** opening, **Then** the current commit SHA is recorded into run metadata via `ledger-io`
- [ ] **Given** open prior bugs, **When** reconciling, **Then** each bug's file region is diffed old-vs-current commit: code gone/changed-as-described ⇒ **propose** `Fixed` (with the fixing commit); code merely moved ⇒ update location so the signature still resolves
- [ ] **Given** the safety rule, **When** acting, **Then** changes are **proposed, never auto-closed** — surfaced for a human or (P4) the Curator

## Technical Notes

- ⚠️ Build by pasting **Prompt 11** from `docs/agent-systems/bug-hunter-build-guide-v3.6.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- Also the foundation for incremental scanning (24d) and fix-verification's fallback
  (P5) — keep the diff primitives reusable.

## Dependencies

### Requires
- 001-ledger-io; git tooling on the host

### Enables
- orchestrator wiring (11b); bug-lifecycle (P4); fix-verification fallback (P5);
  incremental scanning (24d)

## Out of Scope

- Status transitions themselves (bug-lifecycle, P4).
