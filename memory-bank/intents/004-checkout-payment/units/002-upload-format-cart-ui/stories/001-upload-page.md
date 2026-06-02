---
id: 001-upload-page
unit: 002-upload-format-cart-ui
intent: 004-checkout-payment
status: complete
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 014-upload-format-cart-ui
implemented: true
---

# Story: 001-upload-page

## User Story

**As a** customer
**I want** to drag and drop my photos onto the upload page and see them appear as thumbnails with upload progress
**So that** I can quickly add all my photos before choosing a print format

## Acceptance Criteria

- [ ] **Given** the `/upload` route is loaded, **When** the page renders, **Then** a drag-and-drop zone is visible with the text `Trage fotografiile aici sau apasă pentru a alege`
- [ ] **Given** files are dropped or selected, **When** they include only JPEG/PNG/HEIC files under 50 MB and the total is ≤ 30, **Then** each file shows an individual progress bar during upload to `POST /api/uploads`
- [ ] **Given** a HEIC file is selected, **When** shown as thumbnail, **Then** `heic2any` converts it to JPEG in the browser for preview display (original HEIC is uploaded to server)
- [ ] **Given** a file fails client-side validation (wrong type, >50 MB, or would exceed 30-file limit), **When** dropped or selected, **Then** it is rejected immediately with a Romanian error message — no upload request is made
- [ ] **Given** all uploads complete, **When** the thumbnail grid is shown, **Then** each thumbnail displays a quality badge (Green/Yellow/Red) based on the photo's pixel dimensions vs. the currently selected format's required resolution
- [ ] **Given** the `Șterge toate` button is clicked, **When** confirmed, **Then** all uploads are removed from the UI and `DELETE /api/uploads` (or equivalent) is called for each

## Technical Notes

- Use Angular `standalone` component with `NgxDropzoneModule` or custom drag-drop directive
- Upload via `HttpClient` with `reportProgress: true` to drive individual `BehaviorSubject<number>` progress streams
- `heic2any`: `npm install heic2any` — call `heic2any({ blob, toType: 'image/jpeg', quality: 0.8 })` for preview only
- Quality badge thresholds per format: 10×15 = 1200×900px (min); 13×18 = 1535×1063px; 15×21 = 1772×1240px
- Badge colours: Green = ≥ required; Yellow = 80–99% of required; Red = < 80%
- `UploadService` holds uploads as `Signal<UploadItem[]>` for reactive updates

## Dependencies

### Requires
- Bolt 012 (photo-upload-backend — `POST /api/uploads` endpoint)
- Bolt 004 (angular-app-shell — route registration, interceptors)

### Enables
- Story 002-format-finish-selector (quality badge thresholds depend on selected format)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Server returns 415 for a file | Per-file error state shown below thumbnail |
| Server returns 429 (session limit) | Error banner: `"Ai atins limita de 30 de fotografii"` |
| HEIC conversion fails in browser | Show placeholder thumbnail; upload still proceeds |
| Upload interrupted (network error) | Per-file retry button shown |

## Out of Scope

- Server-side HEIC conversion
- Multi-session upload recovery (page refresh loses in-progress uploads)
