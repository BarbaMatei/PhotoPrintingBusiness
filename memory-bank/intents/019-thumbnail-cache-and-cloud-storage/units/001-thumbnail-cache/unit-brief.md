---
unit: 001-thumbnail-cache
intent: 019-thumbnail-cache-and-cloud-storage
phase: inception
status: draft
created: 2026-05-25T10:30:00Z
updated: 2026-05-25T10:30:00Z
---

# Unit Brief: Thumbnail Cache

## Purpose

Persist generated thumbnails so subsequent preview requests skip ImageSharp work; add the schema column needed to track them; harden ImageSharp against decompression bombs.

## Scope

### In Scope
- Schema: `Uploads.ThumbnailPath` nullable
- `ImageProcessor.GenerateThumbnailAsync` writes through to `IStorageService`
- `UploadsController.GetPreview` reads thumbnail when present
- ImageSharp `MaxImageWidth/Height` global cap configured

### Out of Scope
- Cloud storage backend (002)
- Multiple sizes / responsive images

---

## Story Summary

| Story ID | Title | Priority |
|----------|-------|----------|
| 001-thumbnail-path-schema | EF migration adds `Uploads.ThumbnailPath` | Must |
| 002-persist-thumbnail-on-first-request | First preview persists thumbnail; later requests stream cached file | Must |
| 003-imagesharp-max-pixels | Configure `MaxImageWidth/Height` (decomp-bomb defence) | Must |
