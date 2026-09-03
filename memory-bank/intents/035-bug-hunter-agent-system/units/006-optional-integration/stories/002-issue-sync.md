---
id: 002-issue-sync
unit: 006-optional-integration
intent: 035-bug-hunter-agent-system
status: ready
priority: could
created: 2026-06-10T10:40:14Z
assigned_bolt: 094-optional-integration
implemented: false
---

# Story: 002-issue-sync (guide Optional B)

**Workbench seam:** a new standalone tool, reading records from `reviews/lib/records/ledger.mjs`.

## User Story

**As** the owner working from a tracker
**I want** confirmed bugs pushed as tickets and closed/reopened in lockstep with the bug lifecycle
**So that** the tracker mirrors the ledger without manual copying or duplicates

## Acceptance Criteria

- [ ] **Given** Optional B, **When** built, **Then** skill `issue-sync` exists, created via skill-creator, and the brief's three test prompts pass (3 tickets created; re-run updates not duplicates; Fixed bug → ticket closed) *(2026-09: a component that extends the review loop is a script or skill edit under `reviews/lib` / `.claude/skills`, built and tested there; skill-creator applies only to a new standalone skill — FR-1 as amended)*
- [ ] **Given** a NEW Confirmed bug, **When** syncing (tool-agnostic: Jira/Linear/GitHub Issues), **Then** a ticket is created (title = plain_summary; body from the record; priority from severity; labels from category) and the bug-id ↔ ticket-id link recorded in the ledger
- [ ] **Given** idempotency, **When** re-running, **Then** linked tickets update, never duplicate; **Given** lifecycle transitions, **Then** `Fixed` closes the ticket, `Reopened` reopens it
- [ ] **Given** the v3.2 secret-safety rule (doubly so — tickets leave the machine), **When** building ticket bodies, **Then** only the record's **redacted** evidence appears; never raw secret material

## Technical Notes

- ⚠️ Build by pasting **Optional B** from `docs/agent-systems/bug-hunter-build-guide.md` into
  the **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- Owner adoption decision pending; GitHub Issues via the `gh` CLI is the zero-cost
  default if adopted.

## Dependencies

### Requires
- report-rendering (built — the review loop), bug-lifecycle (bolt 092), ledger-io; a tracker connector

### Enables
- Tracker-driven workflow

## Out of Scope

- Choosing/configuring the tracker; two-way sync of human edits made in the tracker.
