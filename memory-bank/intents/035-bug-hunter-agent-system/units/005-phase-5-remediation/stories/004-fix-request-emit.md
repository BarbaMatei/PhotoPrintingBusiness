---
id: 004-fix-request-emit
unit: 005-phase-5-remediation
intent: 035-bug-hunter-agent-system
status: ready
priority: should
created: 2026-06-10T10:40:14Z
assigned_bolt: 093-phase-5-remediation
implemented: false
---

# Story: 004-fix-request-emit (guide Prompt 33, NEW in v3)

## User Story

**As** the bridge to AI-DLC
**I want** confirmed bugs handed over through an idempotent fix-request store keyed by `correlation_id`
**So that** the fix loop closes through a store (not a direct call) and resolves on the same bug it opened with

## Acceptance Criteria

- [ ] **Given** Prompt 33, **When** built, **Then** skill `fix-request-emit` exists, created via skill-creator, and the brief's three test prompts pass (fresh correlation_id emission; re-run updates not duplicates; never calls AI-DLC or edits source)
- [ ] **Given** a Confirmed bug selected for remediation, **When** emitting, **Then** a `correlation_id` is assigned via `ledger-io` per the v3.3 allocation rule (= the bug's ledger id or bug-id + run-scoped suffix; **never reused** — re-emission after `Reopened` gets a fresh id; Integration Contract §4), and a fix-request record lands in the store with: `plain_summary`, `developer_detail`, `reproduction`, `evidence` (**redacted only — never raw secret material**, v3.2), `location`, severity, any `fix_direction`, the contradicted contract (if any), the `correlation_id`, and the **`fix_status` lifecycle field** (`open` at creation; `fix-reported` / `verified-fixed` / `fix-failed` later written by fix-verification — Integration Contract §4)
- [ ] **Given** idempotency (keyed on **`correlation_id`**, v3.4 — review I10), **When** a record with this id exists, **Then** it updates, never duplicates; a re-emission after `Reopened` mints a fresh id and therefore writes a **new** record (never overwriting the prior cycle's terminal record), linked to the prior via `related`
- [ ] **Given** boundaries, **When** operating, **Then** AI-DLC is never called directly, the fix is never acted on, app source untouched — the loop closes later via `fix-verification`'s `verified-fixed` for the same `correlation_id`

## Technical Notes

- ⚠️ Build by pasting **Prompt 33** from `docs/agent-systems/bug-hunter-build-guide.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- Store location pinned by requirements D3: `bug-hunting/fix-requests/`; record format
  incl. the `fix_status` lifecycle per Integration Contract §4. AI-DLC's consumption
  convention is now stated in §4 (v1.1): the owner-driven inception flow reads
  `fix-requests/` with `fix_status: open` as candidate bug-bolts — this skill still
  only owns the record format + idempotency.
- AI-DLC convention (v3.1 brief, Integration Contract §4): the bug-bolt created from a
  request carries the `correlation_id` in its `bolt.md` frontmatter.

## Dependencies

### Requires
- bug-documentation, ledger-io (bolt 085); the fix-request store location (D3)

### Enables
- AI-DLC bug-bolts; the full bug→fix→verified-fixed loop

## Out of Scope

- AI-DLC's bug-bolt mechanics; closing bugs (fix-verification's gate).
