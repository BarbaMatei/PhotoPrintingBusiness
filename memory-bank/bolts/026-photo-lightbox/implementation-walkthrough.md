---
stage: implement
bolt: 026-photo-lightbox
created: 2026-05-24T15:30:00Z
---

## Implementation Walkthrough: photo-lightbox-ui

### Summary

A new `PhotoLightboxComponent` was created as a standalone OnPush overlay that renders when its `src` input is non-null. Three dismiss paths are wired: ESC key via `@HostListener`, backdrop click, and a ✕ button. The existing `PhotoThumbnailComponent` gained a `preview` output that emits the local object-URL when the done-state image is clicked. `FormatSelectorPage` owns a `lightboxSrc` signal that opens and closes the overlay without any HTTP requests.

### Structure Overview

One new shared component plus minimal patches to two existing components. All image data flows from the in-memory `File` object already held in `UploadState` — no backend calls added.

### Completed Work

- [x] `src/app/shared/components/photo-lightbox/photo-lightbox.component.ts` — Fixed-position backdrop overlay; `@Input() src`, `@Output() close`; ESC HostListener; backdrop and ✕ dismiss; image constrained to 90vw/90vh
- [x] `src/app/features/upload/components/photo-thumbnail/photo-thumbnail.component.ts` — Added `@Output() preview`; done-state `<img>` emits `localUrl()` on click
- [x] `src/app/features/upload/pages/format-selector/format-selector-page.ts` — Imported `PhotoLightboxComponent`; added `lightboxSrc = signal<string|null>(null)`
- [x] `src/app/features/upload/pages/format-selector/format-selector-page.html` — Added `(preview)` binding on `<app-photo-thumbnail>`; added `<app-photo-lightbox>` element inside the `@else` block

### Key Decisions

- **Object-URL instead of API preview**: Avoids auth complexity; the `File` is already in memory and produces an instant preview
- **Parent-owned state**: `lightboxSrc` lives on `FormatSelectorPage` so the overlay survives thumbnail re-renders and cleans up naturally when the page is destroyed
- **`@HostListener` on `document:keydown.escape`**: Simpler than a dedicated keyboard service; guarded by `if (this.src)` so it only fires when the lightbox is actually open
