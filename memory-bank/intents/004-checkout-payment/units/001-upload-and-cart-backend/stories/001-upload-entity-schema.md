---
id: 001-upload-entity-schema
unit: 001-upload-and-cart-backend
intent: 004-checkout-payment
status: draft
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 012-photo-upload-backend
implemented: false
---

# Story: 001-upload-entity-schema

## User Story

**As a** developer
**I want** an `Upload` entity and `IStorageService` abstraction in place
**So that** all upload-related code has a clean, testable foundation that supports future cloud storage migration

## Acceptance Criteria

- [ ] **Given** a new photo is saved, **When** `IStorageService.SaveAsync` is called, **Then** the file is persisted at `uploads/{userId|guestSessionId}/{uuid}.{ext}` and the returned path contains no user-supplied string
- [ ] **Given** the `Upload` entity is created, **When** inspected, **Then** it contains: `Id (UUID)`, `UserId? (nullable)`, `GuestSessionId? (nullable)`, `FilePath`, `OriginalFileName`, `WidthPx`, `HeightPx`, `FileSizeBytes`, `ContentType`, `UploadedAt (DateTimeOffset)`, `DeletedAt? (DateTimeOffset)` (soft delete)
- [ ] **Given** the EF Core migration runs, **When** the database is updated, **Then** an `Uploads` table exists with composite indexes on `(UserId, DeletedAt)` and `(GuestSessionId, DeletedAt)`
- [ ] **Given** `IStorageService` is registered in DI, **When** `LocalStorageService` is the Phase 1 implementation, **Then** swapping to S3 requires only a new class implementing the same interface — no callers change

## Technical Notes

- `IStorageService` interface: `SaveAsync(Stream stream, string fileName, string contentType) → Task<string> path`, `DeleteAsync(string path) → Task`, `GetStreamAsync(string path) → Task<Stream>`
- `LocalStorageService` stores to `wwwroot/uploads/{userId|guestSessionId}/{uuid}.{ext}`
- `IMimeValidator` interface: `bool IsValid(Stream stream, string[] allowedTypes)` — reads first 12 bytes for magic number check
- Register `LocalStorageService` and `IMimeValidator` in `Program.cs` as `IStorageService` and `IMimeValidator`

## Dependencies

### Requires
- Bolt 005 (auth-core — `User` entity + `UserId` claim)
- Bolt 007 (guest-sessions — `GuestSessionId` claim)

### Enables
- Story 002-upload-endpoint (needs Upload entity + IStorageService + IMimeValidator)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| UserId and GuestSessionId both null | Rejected by DB constraint — at least one must be set |
| Concurrent writes to same directory | UUID filenames prevent collision |
| wwwroot/uploads directory missing | LocalStorageService creates it on first save |

## Out of Scope

- S3 or Azure Blob implementation (Phase 2)
- File serving / preview (story 003)
