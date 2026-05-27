# US-202 — Photo Upload — Backend

## Story
**As a** system  
**I want to** receive, validate, virus-scan and store uploaded photos; return metadata

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-2 | Upload Fotografii & Selecție Format

## Dependencies
- US-105/US-109 (Auth — accepts both JWT and guest token)
- US-801 (Error handling)
- US-803 (Background cleanup job)

## Acceptance Criteria

1. **`POST /api/uploads`** (multipart/form-data) — accepts Bearer or X-Guest-Token
2. **Validates MIME by magic bytes** (not extension); rejects non-image files with `415`
3. **Extracts width/height** using ImageSharp; stores in `Uploads` table
4. **File saved** as `/uploads/{sessionId}/{uuid}.{ext}`; original filename never used in path
5. **Returns** `UploadDto[]`: `{uploadId, widthPx, heightPx, fileSizeBytes, previewUrl}`
6. **Uploads not linked to an order within 24h** are soft-deleted by background job
7. **Max 30 uploads per session** enforced server-side (`429` if exceeded)

## Technical Notes

### Endpoint
```
POST /api/uploads
Content-Type: multipart/form-data
Authorization: Bearer {jwt} OR X-Guest-Token: {uuid}

→ 200 [
  { "uploadId": "uuid", "widthPx": 4000, "heightPx": 3000, "fileSizeBytes": 5242880, "previewUrl": "/api/uploads/uuid/preview" }
]
→ 415 Unsupported Media Type (non-image file)
→ 429 Too Many Requests (>30 uploads)
→ 413 Payload Too Large (>50MB per file)
```

### Implementation Details
- MIME validation: read first 8 bytes of file for magic number detection (JPEG: `FF D8 FF`, PNG: `89 50 4E 47`, HEIC: check for `ftyp` box)
- Image processing: use `SixLabors.ImageSharp` to read dimensions (width, height); do NOT process/resize at upload time
- Storage: local filesystem in dev (`/uploads/{userId|guestSessionId}/{uuid}.{ext}`); abstract via `IStorageService` for future S3/Azure Blob migration
- Preview endpoint: `GET /api/uploads/{id}/preview` — serves resized thumbnail (300px max dimension) with cache headers
- File path: NEVER use original filename in storage path (prevent path traversal)
- Uploads table: `Id(UUID)`, `UserId?`, `GuestSessionId?`, `FilePath`, `OriginalFileName`, `WidthPx`, `HeightPx`, `FileSizeBytes`, `UploadedAt`, `DeletedAt?`
- Cleanup job: `UploadCleanupJob` — runs hourly, soft-deletes uploads with no OrderItem reference after 24h; deletes physical files

### Security
- Validate file content, not just headers/extension
- Serve uploads with `Content-Disposition: attachment`
- Rate limit: 30 files per session (userId or guestSessionId)

## Files to Create/Modify
- `src/PhotoPrint.API/Controllers/UploadsController.cs`
- `src/PhotoPrint.API/DTOs/UploadDto.cs`
- `src/PhotoPrint.API/Models/Upload.cs`
- `src/PhotoPrint.API/Services/IStorageService.cs` + `LocalStorageService.cs`
- `src/PhotoPrint.API/Services/IUploadService.cs` + `UploadService.cs`
- `src/PhotoPrint.API/BackgroundJobs/UploadCleanupJob.cs`
- EF Core migration for Uploads

## Testing
- Unit test: MIME magic byte validation
- Unit test: dimension extraction
- Unit test: session upload count enforcement
- Unit test: file path generation (no original filename)
- Integration test: upload flow with valid image
- Integration test: reject non-image file
