---
id: 012-photo-upload-backend
unit: 001-upload-and-cart-backend
intent: 004-checkout-payment
type: ddd-construction-bolt
status: complete
started: 2026-05-21T12:30:00Z
completed: 2026-05-21T18:00:00Z
current_stage: done
stages_completed: [domain-model, technical-design, implement, test]
stories:
  - 001-upload-entity-schema
  - 002-upload-endpoint
  - 003-upload-preview-and-cleanup
created: 2026-05-21T10:00:00Z

requires_bolts: ["005-auth-core", "007-guest-sessions"]
enables_bolts: ["013-cart-api"]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 2
---

## Bolt: 012-photo-upload-backend

### Objective

Build the photo upload subsystem: `Upload` entity with EF Core migration, `IStorageService` abstraction (Phase 1: local filesystem), MIME magic-byte validation, ImageSharp dimension extraction, upload endpoint with rate/size/count guards, preview thumbnail endpoint, and hourly upload cleanup background job.

### Stories Included

- [ ] **001-upload-entity-schema**: Upload entity + EF Core migration + IStorageService + IMimeValidator - Priority: Must
- [ ] **002-upload-endpoint**: POST /api/uploads — MIME check, size/count limits, ImageSharp, storage, response DTO - Priority: Must
- [ ] **003-upload-preview-and-cleanup**: GET /api/uploads/{id}/preview + UploadCleanupJob background service - Priority: Must

### Expected Outputs

- EF Core migration: `Uploads` table + indexes on `(UserId, DeletedAt)` and `(GuestSessionId, DeletedAt)`
- `IStorageService` interface + `LocalStorageService` Phase 1 implementation
- `IMimeValidator` interface + implementation (JPEG / PNG / HEIC magic bytes)
- `POST /api/uploads` endpoint (multipart/form-data, JWT + guest token auth)
- `GET /api/uploads/{id}/preview` endpoint (resized JPEG thumbnail, ETag, Cache-Control)
- `UploadCleanupJob` background service (hourly, soft-delete + physical delete)
- Unit tests: MIME validation, size/count guards, cleanup logic
- Integration tests: upload happy path + security rejections

### Dependencies

#### Bolt Dependencies
- **005-auth-core** (required): JWT auth middleware, User entity, UserId claim
- **007-guest-sessions** (required): X-Guest-Token middleware, GuestSessionId claim

#### Cross-bolt Enables
- **013-cart-api**: Uploads table must exist for CartItem.UploadId FK
