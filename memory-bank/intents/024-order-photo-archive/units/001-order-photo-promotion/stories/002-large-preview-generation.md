---
id: 002-large-preview-generation
unit: 001-order-photo-promotion
intent: 024-order-photo-archive
status: draft
priority: must
created: 2026-05-27T13:05:00Z
assigned_bolt: 051-order-photo-promotion
implemented: false
---

# Story: 002-large-preview-generation

## User Story

**As** the platform
**I want** to generate a ~2000 px web preview of an uploaded photo
**So that** customers can view a full-screen version of what they ordered without serving the multi-MB original

## Acceptance Criteria

- [ ] `ImageProcessor.GenerateLargePreviewAsync(storageKey, ct)` returns a ~2000 px (long edge), q85 JPEG stream.
- [ ] Aspect ratio preserved; never upscale (images already < 2000 px pass through at native size).
- [ ] Subject to the existing decompression-bomb guard (`MaxDecodeDimension`, bolt 042).
- [ ] Stored under the `previews/{uploadId}.jpg` key (via `StorageKeys`).

## Technical Notes

- Reuse the bolt-042 `GenerateThumbnailAsync` structure; only the target dimension + key prefix differ.
- Long-edge resize: `ResizeMode.Max` at 2000 px.

## Dependencies

### Requires
- 001-archive-schema; bolt 043 (`StorageKeys`, `previews/` prefix convention).

### Enables
- 003-promote-on-paid

## Out of Scope
- Multiple responsive sizes (single large size for now).
