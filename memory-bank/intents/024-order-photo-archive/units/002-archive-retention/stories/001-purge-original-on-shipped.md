---
id: 001-purge-original-on-shipped
unit: 002-archive-retention
intent: 024-order-photo-archive
status: complete
priority: must
created: 2026-05-27T13:05:00Z
assigned_bolt: 052-archive-retention
implemented: true
---

# Story: 001-purge-original-on-shipped

# User Story

**As** the platform
**I want** the full-resolution original deleted once printing is complete
**So that** we retain only what the customer needs to review (large preview + thumbnail), minimizing stored personal data

## Acceptance Criteria

- [ ] When an order transitions to the configurable "production complete" status (default **Shipped**), each of its uploads has its cloud **original** deleted and `FilePath` nulled, with `OriginalPurgedAt` set.
- [ ] The **large preview and thumbnail are retained**.
- [ ] Idempotent: safe if the original was already purged or never existed.
- [ ] Configurable via `ArchiveSettings:PurgeOriginalAtStatus` (default `Shipped`).

## Technical Notes

- Hook the order-status transition (status machine) — same surface as the Paid promotion trigger.
- Deletion uses `IStorageService.DeleteAsync` (cloud); never touches large/thumb keys.

## Dependencies

### Requires
- bolt 051 (cloud-located originals to purge).

### Enables
- 002-retention-cleanup-job

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Order skips straight to a later status | Purge still fires on first transition at/after the configured status |
| Original already purged | No-op |

## Out of Scope
- Deleting large preview / thumbnail (that's the 12-month retention job).
