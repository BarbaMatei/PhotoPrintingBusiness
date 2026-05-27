---
id: 001-photo-lightbox-component
unit: 001-photo-lightbox-ui
intent: 010-photo-lightbox
status: draft
priority: must
created: 2026-05-24T15:00:00Z
assigned_bolt: 026-photo-lightbox
implemented: false
---

# Story: 001-photo-lightbox-component

## User Story

**As a** customer uploading photos
**I want** to click a thumbnail and see the full-resolution image in an overlay
**So that** I can check photo quality and composition before selecting a print format

## Acceptance Criteria

- [ ] **Given** the lightbox is open, **When** the user presses Escape, **Then** the overlay closes
- [ ] **Given** the lightbox is open, **When** the user clicks the backdrop, **Then** the overlay closes
- [ ] **Given** the lightbox is open, **When** the user clicks the ✕ button, **Then** the overlay closes
- [ ] **Given** the lightbox `src` is null, **Then** the overlay is not rendered
- [ ] **Given** the lightbox is open, **Then** the image is constrained to max 90vw / 90vh

## Technical Notes

- New standalone `OnPush` component at `src/app/shared/components/photo-lightbox/photo-lightbox.component.ts`
- `@Input() src: string | null` — null means closed
- `@Output() close = new EventEmitter<void>()`
- `@HostListener('document:keydown.escape')` to dismiss
- Fixed backdrop covers full viewport; click on backdrop emits close
- Image uses `object-fit: contain`

## Dependencies

### Requires
- None (pure UI component)
