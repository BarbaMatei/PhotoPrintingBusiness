---
id: 001-thumbnail-path-schema
unit: 001-thumbnail-cache
intent: 019-thumbnail-cache-and-cloud-storage
status: complete
priority: must
created: 2026-05-25T10:30:00Z
assigned_bolt: 042-thumbnail-cache
implemented: true
---

# Story: 001-thumbnail-path-schema

## User Story

**As** the platform
**I want** persistent storage for thumbnail paths
**So that** generated thumbnails survive restarts and are reachable by id

## Acceptance Criteria

- [ ] EF migration adds `Uploads.ThumbnailPath varchar(512) NULL`.
- [ ] `Upload` entity exposes the property.
- [ ] Existing rows have `NULL`; no backfill required.

## Technical Notes

- Same shape as `FilePath` (`varchar(512)`), also nullable to indicate "not generated yet".

## Dependencies

### Requires
- None

### Enables
- 002-persist-thumbnail-on-first-request

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Migration on running prod | Nullable column added without table rewrite |

## Out of Scope

- Multiple thumbnail variants (single 800px for now).
