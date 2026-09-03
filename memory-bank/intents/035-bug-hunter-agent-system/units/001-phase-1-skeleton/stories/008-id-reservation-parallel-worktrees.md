---
id: 008-id-reservation-parallel-worktrees
unit: 001-phase-1-skeleton
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-09-03T21:40:00Z
assigned_bolt: 087-phase-2-trust
implemented: false
---

# Story: 008-id-reservation-parallel-worktrees (gap from story 001, verified by bolt 085)

**Status:** gap confirmed by bolt 085-phase-1-skeleton-core and reproduced on a throwaway tree
(`memory-bank/bolts/085-phase-1-skeleton-core/test-walkthrough.md`, run A5). Already documented
as owed by `docs/agent-systems/integration-contract.md:106-112`.

## User Story

**As** the review loop running in more than one worktree at once
**I want** each target to hold a reserved range of `PPW-<n>` before it mints
**So that** two branches cannot give the same id to two different defects and lose one of them
when they merge

## The defect, concretely

`reviews/lib/review/mint-id.mjs:48-53` reads `reviews/state/id-counter`, adds the count, and
writes the number back — no lock, no reservation. Each worktree carries its own working copy of
that file, so two worktrees minting on the same day both start from the same number. The only
guard is in `scaffold-ledger` (`mint-id.mjs:91`) and it compares a new id against **the same
target's** ledger only, so two worktrees minting into two *different* targets are not caught at
all. Reproduced: two roots each seeded `9000`, both printed `PPW-9000..PPW-9000`, and both
scaffolded `PPW-9000` into a different target with exit 0. Wave 1 runs four worktrees at once,
so this is live, not theoretical.

## Acceptance Criteria

- [ ] **Given** a target that opens in a worktree, **When** it opens, **Then** it reserves a
      range in `reviews/state/id-counter` and every mint for that target comes out of its own
      range — two worktrees never draw from the same numbers
- [ ] **Given** the counter file holds one number today, **When** the range format is chosen,
      **Then** the format is written into `reviews/rules/doc-contracts.md` (rule 3) in the same
      change — the descriptive-standards rule
- [ ] **Given** an id already used anywhere, **When** `scaffold-ledger` runs, **Then** the
      duplicate guard covers **every** target's ledger, not just the one being written
- [ ] **Given** a merge that brings two branches' records together, **When** the same id appears
      twice, **Then** something fails loudly — a check the records auditor runs, not a human
      noticing
- [ ] **Given** the fixture suite, **When** the change lands, **Then**
      `reviews/lib/tests/unit/mint-id.test.mjs` gains a case that fails without the reservation:
      two roots, one id, two targets

## Technical Notes

- The contract already states the intended shape: "a target reserves a range of `PPW` finding
  numbers in `reviews/state/id-counter` when it opens; two worktrees never mint from the same
  range" (`docs/agent-systems/integration-contract.md:106-108`).
- Sequencing: this touches `reviews/state/id-counter`, which is owned centrally. Land it in a
  bolt that runs alone, not beside a wave of parallel worktrees.

## Dependencies

### Requires
- 001-ledger-io (verified satisfied-with-this-gap by bolt 085)

### Enables
- Any future run of the loop in more than one worktree without hand-checking ids
