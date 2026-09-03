---
id: 011-owner-queue-age-escalation
unit: 001-phase-1-skeleton
intent: 035-bug-hunter-agent-system
status: ready
priority: should
created: 2026-09-03T21:40:00Z
assigned_bolt: 087-phase-2-trust
implemented: false
---

# Story: 011-owner-queue-age-escalation (gap from story 005, verified by bolt 085)

**Status:** gap confirmed by bolt 085-phase-1-skeleton-core
(`memory-bank/bolts/085-phase-1-skeleton-core/test-walkthrough.md`, story 005 row 4).

## User Story

**As** the owner with a queue of decisions the loop parked for me
**I want** an item that has waited to rise to the top of what I am shown next
**So that** the question nobody answers cannot quietly stay unanswered for ever

## The defect, concretely

The parking half works: every delegated decision is parked with the default that was taken
(`.claude/skills/loop-driver/SKILL.md:263`), every parked item is listed in the run-end report
(`SKILL.md:313-314`), and the count rides the `run-end` event —
`{"ev":"run-end","passes":4,"parked":5}` (`reviews/archive/038-039-invoicing/worklog.jsonl:246`).
What is missing is age. Nothing compares a parked item against the ones before it, so an item
that has waited three runs is presented exactly like one parked a minute ago. It has already
happened: "which fiscal address a parcel-locker order carries is an owner decision; **asked
twice, unanswered**" (`worklog.jsonl:291`) — and the target closed with that decision still open.

The same is true one level up: `reviews/state/backlog.md` has no notion of how long a row has
been waiting, only which target it came from.

## Acceptance Criteria

- [ ] **Given** a parked item repeated across runs, **When** the next run-end report is written,
      **Then** items are ordered oldest-first and each shows how long it has waited and how many
      runs it has survived
- [ ] **Given** an item older than a stated threshold, **When** the report renders, **Then** it
      is called out at the top rather than listed in run order — the threshold is written down,
      not implicit
- [ ] **Given** the same question parked twice, **When** it is parked again, **Then** it is
      recognised as the same question rather than appended as a new one
- [ ] **Given** a loop that closes with parked items open, **When** it closes, **Then** those
      items land somewhere with an owner — the close report or `backlog.md` — and are not lost
      with the run
- [ ] **Given** a decision recorded anywhere in the loop, **When** it is written, **Then** it
      names **who** decided it — the story's provenance clause asks for who / when /
      against-which-commit, and only when and commit exist today (there is no actor field
      anywhere; bolt 085 recorded this as absent in the same pass)
- [ ] **Given** the fixture suite, **When** the change lands, **Then** a case proves the ordering
      and the escalation, failing without them

## Technical Notes

- Every input already exists: `gate-parked` carries `{kind, default, reason}` and the stamper
  owns the timestamp (`reviews/lib/records/schema.mjs:37`), so age is a read over the worklog,
  not new data.
- Recognising "the same question" is the harder half. Keying on `kind` + the ids named in the
  reason is probably enough; splitting when unsure is the loop's house rule and applies here too.

## Dependencies

### Requires
- 005-triage-intake (verified satisfied-with-this-gap by bolt 085)

### Enables
- A parked-decision queue that cannot silently starve, which is what the brief asked for
