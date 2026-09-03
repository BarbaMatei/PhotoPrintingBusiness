---
id: 012-atomic-record-publish
unit: 001-phase-1-skeleton
intent: 035-bug-hunter-agent-system
status: ready
priority: should
created: 2026-09-04T00:20:00Z
assigned_bolt: 087-phase-2-trust
implemented: false
---

# Story: 012-atomic-record-publish (gap from story 001, verified by bolt 085)

**Status:** gap confirmed by bolt 085-phase-1-skeleton-core
(`memory-bank/bolts/085-phase-1-skeleton-core/test-walkthrough.md`, story 001 rows 3a and 5c).

## User Story

**As** the loop writing its records on a Windows machine
**I want** a publish that either lands whole or does not land at all
**So that** a crash, a held file handle, or an editor with the ledger open cannot leave a
half-written record behind

## The defect, concretely

Every record write is a plain `writeFileSync` — `reviews/lib/review/mint-id.mjs:116`,
`reviews/lib/records/render-records.mjs:174` and `:178`. There is no temp-file-then-rename, and
so no rename-retry for the Windows case the brief names: a rename over a file a reader holds
open fails, and the brief's answer is retry-with-backoff, never an in-place partial write.

What stands in its place is real but narrower: the renderer runs every check before its first
write, so a *refusal* leaves `metrics.jsonl`, `index.md` and `ledger.md` untouched
(`render-records.mjs:12-14`), and git is the restore point. Neither helps once writing has
begun.

Two smaller holes in the same surface, found in the same pass:

- **No restore-from-git instruction on a refusal.** The brief requires a corrupt store to be
  refused *and* the operator told to restore from git history. The refusals name the problem
  (`render-records.mjs:89` on an unparseable worklog; the doc gate lists violations) and never
  name the remedy.
- **No version refusal on load.** The brief wants a loader that refuses a record format newer
  than it knows rather than guessing. The loop versions by date instead — `reviews/rules/metrics-schema.md`
  v2/v3/v4 with grandfathering cut-offs at `reviews/lib/records/schema.mjs:78,82` — and no reader
  refuses anything for being too new. This was first graded a deliberate divergence and regraded
  absent at the stage-4 gate, because no ruling exists that chose it.

## Acceptance Criteria

- [ ] **Given** any record publish, **When** it writes, **Then** it writes a temp file and
      renames, and a failed rename retries with backoff before giving up — never a partial
      in-place write
- [ ] **Given** a reader holding the file open (the Windows case), **When** the publish retries,
      **Then** it either succeeds or fails loudly with the file still intact
- [ ] **Given** a refusal on a corrupt or unreadable record, **When** it prints, **Then** it
      names the restore path — which file, and that git history holds the last good copy
- [ ] **Given** a records file whose schema is newer than the reader knows, **When** it loads,
      **Then** the reader refuses rather than guessing — or a written ruling says why dated
      grandfathering is the answer instead, and `reviews/rules/doc-contracts.md` carries it
- [ ] **Given** the fixture suite, **When** the change lands, **Then** a case proves the retry
      path, failing without it

## Technical Notes

- Three call sites only; the change is small. The care is in the Windows retry, which the
  fixture suite has no case for today (there is no `ledger.test.mjs` at all).
- Sequence this with story 008: both touch how records reach disk, and doing them together costs
  one round of fixture work instead of two.

## Dependencies

### Requires
- 001-ledger-io (verified satisfied-with-these-gaps by bolt 085)

### Enables
- A records store whose worst case is a refusal rather than a half-written file
