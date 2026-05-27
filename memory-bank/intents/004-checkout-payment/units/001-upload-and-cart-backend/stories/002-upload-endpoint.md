---
id: 002-upload-endpoint
unit: 001-upload-and-cart-backend
intent: 004-checkout-payment
status: draft
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 012-photo-upload-backend
implemented: false
---

# Story: 002-upload-endpoint

## User Story

**As a** customer (authenticated or guest)
**I want** to upload my photos to the server
**So that** they are safely stored and ready to be added to my cart

## Acceptance Criteria

- [ ] **Given** a valid JPEG/PNG/HEIC file under 50 MB, **When** `POST /api/uploads` is called with Bearer JWT or `X-Guest-Token`, **Then** a 201 response returns `[{ uploadId, widthPx, heightPx, fileSizeBytes, previewUrl }]`
- [ ] **Given** a file with a non-image magic byte sequence, **When** the upload is submitted (even renamed to `.jpg`), **Then** the server returns 415 Unsupported Media Type
- [ ] **Given** a file exceeding 50 MB, **When** the upload is submitted, **Then** the server returns 413 Content Too Large
- [ ] **Given** a session already has 30 uploads, **When** a 31st upload is attempted, **Then** the server returns 429 Too Many Requests
- [ ] **Given** a valid file, **When** dimensions are extracted via ImageSharp, **Then** `WidthPx` and `HeightPx` are accurate without fully decoding the image (metadata read only)

## Technical Notes

- `POST /api/uploads` accepts `multipart/form-data`; supports multiple files in one request
- MIME validation via `IMimeValidator` — JPEG (`FF D8 FF`), PNG (`89 50 4E 47 0D 0A 1A 0A`), HEIC (`ftyp` box at offset 4)
- ImageSharp: use `Image.IdentifyAsync` (reads header only — no full decode) to get `WidthPx`, `HeightPx`
- Session upload count: query `COUNT(Uploads WHERE UserId/GuestSessionId = current AND DeletedAt IS NULL)`
- File size check: read `IFormFile.Length` before reading stream
- `previewUrl` format: `/api/uploads/{id}/preview`

## Dependencies

### Requires
- Story 001-upload-entity-schema (Upload entity + IStorageService + IMimeValidator)
- Bolt 001 (ExceptionHandlerMiddleware for 413/415/429)
- Bolt 002 (security baselines — request size limits)

### Enables
- Story 003-upload-preview-and-cleanup (preview endpoint uses Upload records)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Zero-byte file | Rejected with 400 (validation) |
| HEIC file with .png extension | MIME check by bytes overrides extension — HEIC accepted |
| File renamed from .exe to .jpg | MIME bytes fail — rejected with 415 |
| Upload request with no auth (no JWT, no guest token) | Returns 401 |

## Out of Scope

- Client-side HEIC-to-JPEG conversion (handled in Angular, bolt 014)
- Background cleanup (story 003)
