---
stage: domain-model
bolt: 012-photo-upload-backend
created: 2026-05-21T12:30:00Z
status: approved
---

## Static Domain Model: photo-upload-backend

### Entities

- **Upload**: `Id (Guid)`, `UserId? (Guid)`, `GuestSessionId? (Guid)`, `FilePath (string)`, `OriginalFileName (string)`, `ContentType (string)`, `WidthPx (int)`, `HeightPx (int)`, `FileSizeBytes (long)`, `UploadedAt (DateTimeOffset)`, `DeletedAt? (DateTimeOffset)` — Business rules: exactly one owner (UserId XOR GuestSessionId); FilePath must never contain OriginalFileName substring; DeletedAt set only by cleanup job; uploads linked to an OrderItem must never be soft-deleted

### Value Objects

- **MimeType**: `image/jpeg` | `image/png` | `image/heic` — determined by magic bytes, not client Content-Type; equality by string value
- **StoragePath**: `uploads/{ownerId}/{uuid}.{ext}` — immutable once assigned; UUID-only filename
- **UploadDimensions**: `WidthPx (int)`, `HeightPx (int)` — both > 0; extracted from image header without full decode

### Aggregates

- **Upload** (root): members: Upload only; invariants: (1) exactly one owner, (2) FilePath never from OriginalFileName, (3) soft-delete is permanent, (4) ordered uploads are never deleted

### Domain Events

- **UploadCreated**: Trigger: successful POST /api/uploads — Payload: `{ UploadId, OwnerId, ContentType, WidthPx, HeightPx, FileSizeBytes, UploadedAt }`
- **UploadSoftDeleted**: Trigger: UploadCleanupJob runs — Payload: `{ UploadId, DeletedAt, CleanupReason: "expired" | "orphaned" }`

### Domain Services

- **IMimeValidator**: `bool IsValid(Stream, out string detectedMimeType)` — reads first 12 bytes; validates JPEG (FF D8 FF), PNG (89 50 4E 47 0D 0A 1A 0A), HEIC (66 74 79 70 at offset 4); resets stream position to 0
- **IStorageService**: `SaveAsync(Stream, Guid ownerId, string ext) → string path`, `DeleteAsync(string path)`, `GetStreamAsync(string path) → Stream` — generates UUID filename internally; caller never provides filename
- **IImageProcessor**: `ExtractDimensionsAsync(Stream) → UploadDimensions` (Image.IdentifyAsync — header only), `GenerateThumbnailAsync(Stream, int maxDimension) → Stream` (ResizeMode.Max, JPEG output)
- **IUploadSessionGuard**: `GetSessionUploadCountAsync(Guid? userId, Guid? guestSessionId) → int` — used to enforce 30-file cap before accepting upload

### Repository Interfaces

- **IUploadRepository**: `SaveAsync(Upload)`, `GetByIdAsync(Guid)`, `GetByIdForOwnerAsync(Guid, Guid? userId, Guid? guestSessionId)`, `CountActiveByOwnerAsync(Guid? userId, Guid? guestSessionId)`, `GetExpiredOrphanedAsync(DateTimeOffset olderThan)`, `SoftDeleteAsync(IEnumerable<Guid> ids, DateTimeOffset deletedAt)`

### Ubiquitous Language

- **Upload**: A photo file submitted by a user or guest, stored on the server, and linked to a future order
- **Magic bytes**: The first 8–12 bytes of a file used to identify its true type, independent of file extension
- **Soft delete**: Setting `DeletedAt` on an Upload record; physical file deleted separately by the cleanup job
- **Orphaned upload**: An Upload not referenced by any OrderItem that has exceeded its 24-hour retention window
- **Preview thumbnail**: A resized JPEG (max 300px) served from the backend for display in the Angular UI
- **Session upload count**: The number of active (non-deleted) uploads owned by a single user or guest session
- **Storage path**: The UUID-based filesystem path where a file is saved; never derived from user-supplied data
- **HEIC**: High Efficiency Image Coding — Apple's photo format; accepted on upload, previewed client-side via heic2any
