---
id: 001-archive-schema
unit: 001-order-photo-promotion
intent: 024-order-photo-archive
status: draft
priority: must
created: 2026-05-27T13:05:00Z
assigned_bolt: 051-order-photo-promotion
implemented: false
---

# Story: 001-archive-schema

## User Story

**As** the platform
**I want** schema fields to track the large preview and the original's purge state
**So that** the promotion + retention lifecycle has somewhere to record its progress

## Acceptance Criteria

- [ ] `Upload.LargePreviewPath varchar(512) NULL` added.
- [ ] `Upload.OriginalPurgedAt timestamptz NULL` added (set when the original is deleted post-printing).
- [ ] EF Core configuration via Fluent API (ADR-002 — no data annotations).
- [ ] Migration applies cleanly on **Postgres and SQLite**.
- [ ] `Upload.StorageLocation` is assumed present (added in bolt 043); this story does not re-add it.

## Technical Notes

- Mirror the bolt-042 `ThumbnailPath` pattern (`HasMaxLength(512)`).
- Archive expiry is **derived** from the order's completion timestamp + configured window — not stored per upload (avoids drift).

## Dependencies

### Requires
- bolt 043 (`StorageLocation` column).

### Enables
- 002-large-preview-generation, 003-promote-on-paid

## Out of Scope
- Any cleanup/purge logic (unit 002).
