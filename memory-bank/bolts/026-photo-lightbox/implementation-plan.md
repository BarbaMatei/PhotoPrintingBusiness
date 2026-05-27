---
stage: plan
bolt: 026-photo-lightbox
created: 2026-05-24T15:30:00Z
---

## Implementation Plan: photo-lightbox-ui

### Objective
Add a click-to-open full-resolution lightbox to the upload format-selector strip. Clicking any done-state photo thumbnail opens a full-viewport overlay. Image data comes from the already-computed `URL.createObjectURL(state.file)` — no API call needed.

### Deliverables
- `src/app/shared/components/photo-lightbox/photo-lightbox.component.ts` — new standalone OnPush overlay component
- `PhotoThumbnailComponent` patch — `@Output() preview = new EventEmitter<string>()` emitted on image click (done state)
- `FormatSelectorPage` patch — `lightboxSrc = signal<string|null>(null)`, bind `(preview)` and `(close)`, render `<app-photo-lightbox>`

### Dependencies
- `localUrl()` method on `PhotoThumbnailComponent` — already exists ✅
- Angular `HostListener`, `signal`, `EventEmitter`, `ChangeDetectionStrategy.OnPush` — already in use ✅
- No new packages required

### Technical Approach
`PhotoLightboxComponent` accepts `@Input() src: string | null`. When non-null it renders a fixed full-viewport backdrop div + a centred `<img>`. Three dismiss paths: backdrop click, ✕ button, ESC key — all emit `@Output() close`. Parent keeps a `lightboxSrc` signal; setting it to null hides the component via `@if`.

### Acceptance Criteria
- [ ] Clicking a done-state thumbnail opens the lightbox
- [ ] ESC key closes the lightbox
- [ ] Clicking the dark backdrop closes the lightbox
- [ ] Clicking the ✕ button closes the lightbox
- [ ] Lightbox is not rendered when no thumbnail has been clicked
- [ ] No HTTP request is made for the image
