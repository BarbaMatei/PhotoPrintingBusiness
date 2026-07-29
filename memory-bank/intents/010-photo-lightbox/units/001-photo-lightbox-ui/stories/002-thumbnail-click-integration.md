---
id: 002-thumbnail-click-integration
unit: 001-photo-lightbox-ui
intent: 010-photo-lightbox
status: complete
priority: must
created: 2026-05-24T15:00:00Z
assigned_bolt: 026-photo-lightbox
implemented: true
---

# Story: 002-thumbnail-click-integration

## User Story

**As a** customer in the format-selector page
**I want** to click any uploaded photo thumbnail to open the lightbox
**So that** I can inspect the photo without leaving the page

## Acceptance Criteria

- [ ] **Given** a photo is in `done` state, **When** the user clicks its thumbnail image, **Then** the lightbox opens with that photo's object-URL
- [ ] **Given** the lightbox is open, **When** it emits `(close)`, **Then** the overlay disappears and the format-selector page remains intact
- [ ] **Given** a photo is removed from the strip, **Then** `lightboxSrc` is reset to null if it was showing that photo

## Technical Notes

- `PhotoThumbnailComponent`: add `@Output() preview = new EventEmitter<string>()`, emit `localUrl()` on image click (done state only)
- `FormatSelectorPage`: add `readonly lightboxSrc = signal<string|null>(null)`, bind `(preview)="lightboxSrc.set($event)"` on `<app-photo-thumbnail>`, bind `(close)="lightboxSrc.set(null)"` on `<app-photo-lightbox>`
- No changes to routing, guards, or services

## Dependencies

### Requires
- 001-photo-lightbox-component
