---
id: 005-orchestrator-remediation-ext
unit: 005-phase-5-remediation
intent: 035-bug-hunter-agent-system
status: ready
priority: should
created: 2026-06-12T00:00:00Z
assigned_bolt: 093-phase-5-remediation
implemented: false
---

# Story: 005-orchestrator-remediation-ext (guide Prompt 31b, NEW in v3.3 — review H1)

**Status:** satisfied by `reviews/lib/drive/rows.mjs` — the router’s verification row (2026-09)

## User Story

**As** the fix loop's discovery mechanism
**I want** the orchestrator's Open step to scan the fix-request mailbox at every run open
**So that** AI-DLC's "fix done" is actually noticed — without this scan no brief ever checks the
mailbox, `fix_status` parks at `open` forever, and the entire bug→fix→re-distil loop silently stalls

## Acceptance Criteria

- [ ] **Given** Prompt 31b, **When** built, **Then** the `orchestrator` skill is re-opened at its
      Open seam (via skill-creator) and the brief's four test prompts pass (completed bug-bolt
      discovered at run open → record moves `open` → `fix-reported` → `verified-fixed`;
      in-progress bolt → record untouched at `open`; terminal record → not rescanned; re-completed
      `fix-failed` bolt → re-discovered, moves `fix-failed` → `fix-reported` → `verified-fixed` —
      v3.4, review I2)
- [ ] **Given** run open, **When** scanning, **Then** `bug-hunting/fix-requests/` is scanned for
      records with `fix_status: open | fix-reported | fix-failed` (the inclusion predicate —
      `fix-failed` included so a re-fix is actually re-checked, v3.4); each correlated bug-bolt's
      `bolt.md` in `memory-bank/bolts/` (matched by `correlation_id` frontmatter) is checked; any
      at `status: complete` dispatches `fix-verification` for that `correlation_id`
- [ ] **Given** the genuinely terminal records (`verified-fixed`, `closed-unverified`), **When**
      scanning, **Then** they — and only they — are skipped
- [ ] **Given** the Integration Contract §4, **When** wired, **Then** this scan IS the stated
      checking mechanism for the "fix done" mailbox (contract v1.2 names Prompt 31b its implementer)

## Technical Notes

- ⚠️ Build by pasting **Prompt 31b** from `docs/agent-systems/bug-hunter-build-guide.md` into
  the **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- Extension at the orchestrator's **Open** seam — no restructuring; build AFTER Prompt 31
  (`fix-verification` must exist to dispatch).
- Review provenance: cross-system review v2 finding **H1** (the G3 mechanism had no builder).

## Dependencies

### Requires
- 002-fix-verification (this story dispatches it); orchestrator (built — the review loop); fix-request store (D3)

### Enables
- The fix loop actually firing; KB Phase 4 re-distillation (both signals observable)

## Out of Scope

- Writing any `fix_status` value itself (fix-verification owns all post-`open` transitions).
