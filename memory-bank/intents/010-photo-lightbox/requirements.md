---
intent: 010-photo-lightbox
phase: inception
status: complete
created: 2026-05-24T14:00:00Z
updated: 2026-05-24T14:00:00Z
---

# Requirements: Photo Lightbox

## Intent Overview

Add a lightbox/modal to the upload format-selector strip so that clicking a photo thumbnail opens the full-resolution image in an overlay. No backend changes are required — the image data is already available in memory as a `File` object via `URL.createObjectURL(state.file)`.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Customers can inspect photo quality before confirming format | Lightbox opens on thumbnail click showing full image | Must |
| Lightbox does not block upload or format-selection workflow | Modal is dismissible via backdrop click, Escape, or ✕ button | Must |

---

## Functional Requirements

### FR-1: Photo Lightbox Component
- **Description**: A standalone Angular overlay component that renders a full-resolution image. Accepts a `src` input (object-URL string or null); null means closed.
- **Acceptance Criteria**: Opens when `src` is non-null; image fills available viewport space (max-width/max-height 90vw/90vh); backdrop click dismisses; ✕ button dismisses; Escape key dismisses; emits `(close)` output.
- **Priority**: Must
- **Related Stories**: US-901

### FR-2: Thumbnail Click Integration
- **Description**: `PhotoThumbnailComponent` emits a `(preview)` event carrying the object-URL when the thumbnail image is clicked (only when `status === 'done'`). `FormatSelectorPage` listens to this event and drives the lightbox.
- **Acceptance Criteria**: Click on a `done` thumbnail image opens the lightbox; click on an `uploading` or `error` thumbnail does nothing; removing a photo while lightbox is open closes the lightbox.
- **Priority**: Must
- **Related Stories**: US-902

---

## Non-Functional Requirements

| Category | Requirement |
|----------|-------------|
| Performance | Object-URL already exists; no additional fetch or blob creation |
| Accessibility | Overlay traps focus; role="dialog" aria-modal="true"; Escape dismiss |
| Architecture | OnPush everywhere; no new services; purely presentational |
| Angular version | Angular 21 conventions: `signal()`, `@if`, standalone components |
