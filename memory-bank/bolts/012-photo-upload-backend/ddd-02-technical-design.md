---
stage: technical-design
bolt: 012-photo-upload-backend
created: 2026-05-21T12:45:00Z
status: approved
---

## Technical Design: photo-upload-backend

### Architecture Pattern

Clean Architecture with vertical slicing per feature — consistent with existing auth and product catalog pattern in the project.

### Layer Structure

```
┌────────────────────────────────────────────────────┐
│  Presentation: UploadsController                   │
│  POST /api/uploads · GET /api/uploads/{id}/preview │
├────────────────────────────────────────────────────┤
│  Application: UploadService                        │
│  Orchestrates: validation → storage → DB persist   │
├────────────────────────────────────────────────────┤
│  Domain: Upload entity · IMimeValidator            │
│          IStorageService · IImageProcessor         │
│          IUploadSessionGuard · IUploadRepository   │
├────────────────────────────────────────────────────┤
│  Infrastructure: AppDbContext (EF Core)            │
│  LocalStorageService · MimeValidator               │
│  ImageProcessor · UploadCleanupJob                 │
└────────────────────────────────────────────────────┘
```

### API Design

- **POST /api/uploads**: multipart/form-data — Request: IFormFileCollection — Response: `[{ uploadId, widthPx, heightPx, fileSizeBytes, previewUrl }]` — Errors: 400, 401, 413, 415, 429
- **GET /api/uploads/{id}/preview**: — Response: image/jpeg with Cache-Control + ETag — Errors: 304, 401, 404

### Data Model

- **Uploads**: `Id (uuid PK)`, `UserId? (FK Users)`, `GuestSessionId? (uuid)`, `FilePath (varchar 500)`, `OriginalFileName (varchar 260)`, `ContentType (varchar 50)`, `WidthPx (int)`, `HeightPx (int)`, `FileSizeBytes (bigint)`, `UploadedAt (timestamptz)`, `DeletedAt? (timestamptz)`
- Indexes: `(UserId, DeletedAt) WHERE UserId IS NOT NULL`, `(GuestSessionId, DeletedAt) WHERE GuestSessionId IS NOT NULL`
- Constraint: `CHECK (UserId IS NOT NULL XOR GuestSessionId IS NOT NULL)`

### Security Design

- MIME spoofing: MimeValidator reads first 12 magic bytes; client Content-Type ignored
- Path traversal: LocalStorageService generates Guid.NewGuid() filename; OriginalFileName never reaches filesystem
- Ownership: GetByIdForOwnerAsync returns 404 for unowned uploads (not 403 — prevents enumeration)
- Session flood: count check before stream read → 429 if ≥ 30
- File size: IFormFile.Length checked before stream open → 413 immediately
- Raw file serving: files never accessible directly from wwwroot

### NFR Implementation

- NFR-3 MIME at byte level: MimeValidator reads exactly 12 bytes → UnsupportedMediaTypeException → 415
- NFR-4 No path traversal: Path.Combine(_basePath, ownerId, $"{Guid.NewGuid():N}.{ext}")
- NFR-8 Cleanup correctness: WHERE Id NOT IN (SELECT UploadId FROM OrderItems) — in-DB orphan check
- Preview cache: ETag = "{id}-{fileSizeBytes}" stable; Cache-Control: public, max-age=3600

### ADR Analysis

Skipped — no ADR-worthy decisions beyond what is already documented in project ADRs (IStorageService abstraction: ADR-7; HEIC client-side: ADR-9).
