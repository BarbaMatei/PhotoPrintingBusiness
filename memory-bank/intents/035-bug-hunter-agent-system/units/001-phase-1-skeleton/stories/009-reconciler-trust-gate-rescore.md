---
id: 009-reconciler-trust-gate-rescore
unit: 001-phase-1-skeleton
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-09-03T21:40:00Z
assigned_bolt: 087-phase-2-trust
implemented: false
---

# Story: 009-reconciler-trust-gate-rescore (gap from story 003, verified by bolt 085)

**Status:** gap confirmed by bolt 085-phase-1-skeleton-core
(`memory-bank/bolts/085-phase-1-skeleton-core/test-walkthrough.md`, story 003 row 1).

## User Story

**As** the loop that decides whether a finding is new or already known
**I want** the reconciler's ground-truth score to be no older than its own matching rules
**So that** "it passed the gate" describes the skill that is running, not one two revisions back

## The defect, concretely

`.claude/skills/reconcile-findings/SKILL.md:109-110` sets the rule itself: re-score blind
against the eval set "after any material change to the rules above". The only recorded score is
**2026-07-27** (`SKILL.md:118-126`). `git log` on the file shows two later changes:

- `c09675d` (2026-08-13) — the id scheme the matching rules key on (`F#`/`D#` → `PPW-<n>`),
  rewriting the description and the canonical-id rule;
- `0c6938c` (2026-09-02) — a new required lineage rule inside "Fix-residual ≠ re-find":
  `seed_round` and `area`, with a "write `null` when the round cannot be named, never guess"
  clause that feeds the convergence rule.

The second is unambiguously a material change to the matching rules. Everything downstream that
cites "scored against ground truth" — including the discovery runbook at
`reviews/runbooks/runbook-discovery.md:115-118` and the guide's own ✓ for `deduplication` — is
citing a stale gate.

## Acceptance Criteria

- [ ] **Given** the current SKILL.md, **When** the gate is re-run, **Then** a blind run against
      `.claude/skills/reconcile-findings/overlap-ground-truth.md` is scored and recorded in the
      skill's **Scores** section with its date, and any over-merge of hard cases 1–7 is a fail
- [ ] **Given** the new lineage rule, **When** the eval set is used, **Then** it contains at
      least one case exercising `residual-of` + `seed_round` + `area` — the rule added after the
      last score — or the eval set is extended in the same change
- [ ] **Given** staleness recurs, **When** the loop next routes, **Then** something mechanical
      compares the recorded score date against the file's last change date, so the next drift is
      caught by a check rather than by a verification bolt
- [ ] **Given** the descriptive-standards rule, **When** the score is recorded, **Then** any
      document claiming the reconciler is ground-truth-scored is updated in the same change

## Technical Notes

- The eval set and its scoring guide already exist at
  `.claude/skills/reconcile-findings/overlap-ground-truth.md`; the runner must not read it.
- The mechanical staleness check is cheap: last-commit date of `SKILL.md` versus the newest date
  in its Scores section. It belongs with the records auditor, which the driver already runs first.

## Dependencies

### Requires
- 003-deduplication (verified satisfied-with-this-gap by bolt 085)

### Enables
- Any claim that the loop's dedup is trustworthy, including the guide's ✓ row for `deduplication`
