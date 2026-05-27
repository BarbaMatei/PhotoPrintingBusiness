---
unit: 001-photo-lightbox-ui
intent: 010-photo-lightbox
phase: inception
status: ready
created: 2026-05-24T15:00:00Z
updated: 2026-05-24T15:00:00Z
default_bolt_type: simple-construction-bolt
---

# Unit Brief: 001-photo-lightbox-ui

## Purpose

Add a click-to-preview lightbox to the upload format-selector strip. Clicking any completed photo thumbnail opens a full-viewport overlay displaying the full-resolution image sourced entirely from the in-memory `File` object — no backend round-trip required.

## Scope

### In Scope
- New `PhotoLightboxComponent` — standalone `OnPush` overlay with backdrop, ESC/✕ dismiss, and `(close)` output
- `PhotoThumbnailComponent` patch — add `@Output() preview = new EventEmitter<string>()` emitted on image click (done state only)
- `FormatSelectorPage` patch — `lightboxSrc = signal<string|null>(null)`, wire `(preview)` on each thumbnail, render `<app-photo-lightbox>`
- Unit tests for the new component and updated specs

### Out of Scope
- Backend `/preview` endpoint (already exists but not needed here)
- Zoom / pan / carousel navigation
- Download button
