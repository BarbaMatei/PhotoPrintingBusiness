---
id: 001-suppression-learning
unit: 004-phase-4-learn-and-measure
intent: 035-bug-hunter-agent-system
status: ready
priority: should
created: 2026-06-10T10:40:14Z
assigned_bolt: 092-phase-4-learn-and-measure
implemented: false
---

# Story: 001-suppression-learning (guide Prompt 25)

## User Story

**As** the learning loop
**I want** one-off dismissals generalized into reusable, validated suppression patterns
**So that** whole classes of false positives stop recurring — without ever hiding a real bug

## Acceptance Criteria

- [ ] **Given** Prompt 25, **When** built, **Then** skill `suppression-learning` exists, created via skill-creator, and the brief's three test prompts pass (five @NonNull dismissals → pattern; wouldn't-have-hidden-a-Confirmed-bug check; blast radius shown)
- [ ] **Given** dismissed findings (with reasons, via `triage-intake` → `ledger-io`), **When** generalizing, **Then** shared traits (rule_id, category-within-package, sanitizer/annotation presence, framework idiom — dismissal reasons are the strong signal) become candidate patterns: human-readable description + a precise match rule `deduplication` can apply
- [ ] **Given** the safety rule, **When** proposing, **Then** every pattern is **validated against the Confirmed set** (would it have suppressed a genuine bug? → reject or narrow); blast radius reported; patterns are **proposed, never auto-activated** — approval flows through `triage-intake`, storage via `ledger-io`

## Technical Notes

- ⚠️ Build by pasting **Prompt 25** from `docs/agent-systems/bug-hunter-build-guide.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- The guide's warning is the design center: an over-broad suppression silently hides
  real bugs — worse than a false positive.

## Dependencies

### Requires
- ledger-io + triage-intake (bolt 085) — dismissals-with-reasons are its input

### Enables
- curator-agent step (1)

## Out of Scope

- Applying patterns during dedup (Prompt 3 already honors them).
