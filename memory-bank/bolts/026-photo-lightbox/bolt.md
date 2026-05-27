---
id: 026-photo-lightbox
unit: 001-photo-lightbox-ui
intent: 010-photo-lightbox
type: simple-construction-bolt
status: complete
stories:
  - 001-photo-lightbox-component
  - 002-thumbnail-click-integration
created: 2026-05-24T14:00:00Z
started: 2026-05-24T15:00:00Z
completed: 2026-05-24T15:30:00Z
current_stage: null
stages_completed: [1, 2, 3]

requires_bolts: [014-upload-format-cart-ui]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 1
---

# Bolt: 026-photo-lightbox

## Overview

Add a click-to-preview lightbox to the upload format-selector strip. Clicking a completed photo thumbnail opens `PhotoLightboxComponent` — a full-viewport modal showing the full-resolution image sourced from the existing `URL.createObjectURL(state.file)` object-URL. No backend changes.

## Objective

By the end of this bolt a customer can click any uploaded thumbnail in the format-selector strip and immediately inspect the full-resolution photo in an overlay, then dismiss it and continue selecting formats.

## Stories Included

- **001-photo-lightbox-component**: New standalone `PhotoLightboxComponent` — full-viewport overlay, max 90vw/90vh image, backdrop/Escape/✕ dismiss, `(close)` output (Must)
- **002-thumbnail-click-integration**: Add `@Output() preview` to `PhotoThumbnailComponent`; wire `lightboxSrc` signal + `<app-photo-lightbox>` into `FormatSelectorPage` (Must)

## Bolt Type

`simple-construction-bolt` — one new Angular standalone component plus targeted wiring changes to two existing components.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Plan | `implementation-plan.md` — component API, template structure, SCSS overlay approach, signal wiring in `FormatSelectorPage` |
| 2 | Implement | `photo-lightbox.component.ts`; patch `photo-thumbnail.component.ts`; patch `format-selector-page.ts` and its template/HTML |
| 3 | Test | Spec files for `PhotoLightboxComponent`; updated spec for `PhotoThumbnailComponent` (new output); updated spec for `FormatSelectorPage` (lightbox open/close) |

## Dependencies

- **Requires**: bolt `014-upload-format-cart-ui` (`PhotoThumbnailComponent` and `FormatSelectorPage` must exist — ✅ complete)
- **Enables**: nothing

## Key Technical Notes

### PhotoLightboxComponent

```
src/PhotoPrint.UI/src/app/shared/components/photo-lightbox/photo-lightbox.component.ts
```

- `@Input() src: string | null` — null = closed, any string = open (the object-URL)
- `@Output() close = new EventEmitter<void>()`
- Standalone, `ChangeDetectionStrategy.OnPush`
- Template: `@if (src)` → fixed overlay div + `<img [src]="src">` + ✕ button
- Escape key handled via `@HostListener('document:keydown.escape')`
- SCSS: `position: fixed; inset: 0; z-index: 1000; background: rgba(0,0,0,.85)` backdrop; `max-width: 90vw; max-height: 90vh; object-fit: contain` image

### PhotoThumbnailComponent patch

```
src/PhotoPrint.UI/src/app/features/upload/components/photo-thumbnail/photo-thumbnail.component.ts
```

- Add `@Output() preview = new EventEmitter<string>()`
- In the `status === 'done'` `<img>` element add `(click)="preview.emit(localUrl())"` and `style="cursor:pointer"`

### FormatSelectorPage patch

```
src/PhotoPrint.UI/src/app/features/upload/pages/format-selector/format-selector-page.ts
```

- Add `readonly lightboxSrc = signal<string | null>(null)`
- Import `PhotoLightboxComponent` in `imports[]`
- In HTML template: add `(preview)="lightboxSrc.set($event)"` to each `<app-photo-thumbnail>`
- Add `(removed)` handler to also call `lightboxSrc.set(null)` when the previewed photo is removed
- Add `<app-photo-lightbox [src]="lightboxSrc()" (close)="lightboxSrc.set(null)" />` at the bottom of the template
