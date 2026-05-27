# US-201 — Bulk Photo Upload (Frontend)

## Story
**As a** customer (registered or guest)  
**I want to** upload multiple photos at once and see a thumbnail grid before choosing options

## Type
FRONTEND — Angular

## Epic
EPIC-2 | Upload Fotografii & Selecție Format

## Dependencies
- US-202 (Backend upload endpoint)
- US-804 (Angular App Shell)
- US-108/US-104 (Authentication — either logged in or guest)

## Acceptance Criteria

1. **Drag-and-drop zone** + `Selectează fotografii` button; accepts `.jpg`, `.jpeg`, `.png`, `.heic`
2. **Max 30 files** per session; **max 50 MB** per file; both validated client-side before upload
3. **Upload starts immediately** on file selection; per-file progress bar
4. **Thumbnail grid**: photo preview, filename (truncated), resolution (WxH px), quality badge
5. **Quality badge** per thumbnail — Green: optimal resolution; Yellow: minimum met; Red: below minimum (warning, not blocked)
6. **Individual ✕ remove button**; `Șterge toate` clear-all button
7. **Total photo count** shown: `12 fotografii selectate`

## Technical Notes

### Component Location
`src/app/features/upload/photo-upload/photo-upload.component.ts`

### Implementation Details
- Drag-and-drop: use Angular CDK `DragDropModule` or custom directive
- File validation (client-side): check file extension AND MIME type; reject unsupported with toast
- Size validation: check `file.size < 50 * 1024 * 1024`; reject with specific error per file
- Count validation: prevent adding more than 30 total
- Upload: `POST /api/uploads` (multipart/form-data) per file or batched; use `HttpClient` with `reportProgress: true` for progress tracking
- Thumbnail generation: use `FileReader` + `createImageBitmap()` or canvas for client-side preview
- HEIC support: use `heic2any` library for browser preview conversion
- Quality badge logic: compare image dimensions against selected product's min/optimal resolution (from products API)
- Store upload results (uploadId, dimensions, previewUrl) in component state / NgRx or service

### UI/UX
- Drop zone: dashed border, icon, text `Trage fotografiile aici sau`
- Progress: individual progress bars per file during upload
- Grid: responsive, 3-4 columns on desktop, 2 on tablet, 1 on mobile
- Filename truncation: max 20 chars with ellipsis
- Error states: file too large, wrong format, max count reached

## Files to Create/Modify
- `src/app/features/upload/photo-upload/photo-upload.component.ts`
- `src/app/features/upload/photo-upload/photo-upload.component.html`
- `src/app/features/upload/photo-upload/photo-upload.component.scss`
- `src/app/features/upload/photo-thumbnail/photo-thumbnail.component.ts`
- `src/app/core/services/upload.service.ts`
- `src/app/core/models/upload.model.ts`

## Testing
- Unit test: file type validation
- Unit test: file size validation
- Unit test: max file count enforcement
- Unit test: quality badge calculation
- Unit test: progress tracking
- E2E: upload 3 photos, verify thumbnails appear
