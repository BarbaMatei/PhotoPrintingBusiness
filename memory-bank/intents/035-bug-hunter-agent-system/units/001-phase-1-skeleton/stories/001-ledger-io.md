---
id: 001-ledger-io
unit: 001-phase-1-skeleton
intent: 035-bug-hunter-agent-system
status: complete
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 085-phase-1-skeleton-core
implemented: true
---

# Story: 001-ledger-io (guide Prompt 1)

**Status:** **satisfied with a gap** — verified by bolt 085-phase-1-skeleton-core (2026-09-03). The seam is wider than this line claimed: `reviews/lib/records/ledger.mjs` only reads; the ledger's writers are `reviews/lib/review/mint-id.mjs`, `reviews/lib/records/render-records.mjs` and the `reconcile-findings` agent. Two gaps carried forward as stories [008-id-reservation-parallel-worktrees](008-id-reservation-parallel-worktrees.md) and [012-atomic-record-publish](012-atomic-record-publish.md), both assigned to bolt 087. Evidence: `memory-bank/bolts/085-phase-1-skeleton-core/test-walkthrough.md`.

## User Story

**As** every other component in the bug-hunting system
**I want** one concurrency-safe skill owning all reads/writes of the shared ledger
**So that** the system's memory persists across runs in one consistent format and parallel hunters never clobber each other

## Acceptance Criteria

- [ ] **Given** Prompt 1, **When** built, **Then** skill `ledger-io` exists under `.claude/skills/ledger-io/`, created via skill-creator, and the brief's three test prompts pass (fresh-ledger init + Markdown view; overlapping staging-file merge with no lost data/duplicate IDs; list never-examined files) *(2026-09: a component that extends the review loop is a script or skill edit under `reviews/lib` / `.claude/skills`, built and tested there; skill-creator applies only to a new standalone skill — FR-1 as amended)*
- [ ] **Given** the pinned location (requirements D3), **When** operating, **Then** the ledger lives at `bug-hunting/bug-ledger.json` with a generated `bug-hunting/bug-ledger.md` human view, with a top-level **`schema_version`** (loaders refuse a newer major — v3.2) and all sections from the brief (`application_map`, `bug_index` incl. **`correlation_id`**, `dismissed`, `suppression_patterns`, `coverage`, `runs`)
- [ ] **Given** the v3.2 platform/growth notes, **When** publishing, **Then** writes go temp-file-then-rename with **retry-with-backoff on Windows** (rename over an open file fails — never in-place partial writes), and growth is handled by explicit versioned archival, never silent pruning
- [ ] **Given** parallel hunters, **When** writing, **Then** workers write staging files and a **single coordinator merges at run close**; `next_bug_id` is atomic and assigned during the merge; writes never drop existing data
- [ ] **Given** the operations list, **When** exercised, **Then** all brief operations work: `load` (tolerates first-run empty; **verifies the content hash, warns on out-of-band edits, refuses corrupt files with restore-from-git instructions — v3.3**), `next_bug_id`, `upsert_bug`, `set_status`, `record_dismissal`, `add_suppression_pattern`, `update_coverage`, `append_run_summary`, `regenerate_markdown_view`
- [ ] **Given** the v3.3 schema fixes, **When** storing, **Then** the `status` enum includes **`Reopened`**, each `bug_index` entry **embeds the full bug-documentation record** (listed fields = index columns), and `runs` carries `oracle_as_of_commit` (P3) + per-run eval metrics/model (P4)
- [ ] **Given** the single-history rule (v3.3, Integration Contract §1), **When** operating, **Then** runs write only in the integration worktree on `main`; ledger-JSON merge conflicts are never resolved textually

## Technical Notes

- ⚠️ Build by pasting **Prompt 1** from `docs/agent-systems/bug-hunter-build-guide.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- This is the system's foundation — every later component depends on it; its format
  IS the contract. Keep the "pushy" trigger description per the brief.

## Dependencies

### Requires
- (none — first component in the master build order)

### Enables
- Everything (deduplication, triage-intake, hunters, orchestrator, …)

## Out of Scope

- Populating `suppression_patterns` (Phase 4) — the section exists from day one.
