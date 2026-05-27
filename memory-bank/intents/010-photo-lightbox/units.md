---
intent: 010-photo-lightbox
phase: inception
status: complete
created: 2026-05-24T14:00:00Z
---

# Units: Photo Lightbox

## Decomposition

| Unit | Type | Stories | Default Bolt Type |
|------|------|---------|-------------------|
| 001-photo-lightbox-ui | frontend | US-901, US-902 | simple-construction-bolt |

## Rationale

The feature is entirely presentational. The `File` object is already in the `UploadState` held by `FormatSelectorPage` and the thumbnail already computes `URL.createObjectURL(state.file)`. No new service, no API call, and no backend change is needed. A single frontend unit covering one new component (`PhotoLightboxComponent`) and a small wiring change to the existing `PhotoThumbnailComponent` and `FormatSelectorPage` is the right granularity.
