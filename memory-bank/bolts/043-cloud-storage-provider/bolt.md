---
id: 043-cloud-storage-provider
unit: 002-cloud-storage-provider
intent: 019-thumbnail-cache-and-cloud-storage
type: ddd-construction-bolt
status: complete
stories:
  - 001-s3-storage-service
  - 002-preview-redirect-presigned-url
created: 2026-05-25T10:30:00Z
started: 2026-05-27T12:00:00Z
completed: 2026-05-29T08:30:00Z
current_stage: null
stages_completed:
  - name: model
    completed: 2026-05-27T12:10:00Z
    artifact: ddd-01-domain-model.md
  - name: design
    completed: 2026-05-28T08:00:00Z
    artifact: ddd-02-technical-design.md
  - name: adr
    completed: 2026-05-28T08:20:00Z
    artifact: adr-007, adr-008, adr-009
  - name: implement
    completed: 2026-05-29T08:00:00Z
    artifact: src/PhotoPrint.API + Tests (462/462 passing)
  - name: test
    completed: 2026-05-29T08:30:00Z
    artifact: ddd-03-test-report.md (504 tests; 497 passed, 7 CI-gated)

requires_bolts: [042-thumbnail-cache]
enables_bolts: [051-order-photo-promotion, 046-distributed-state-redis]
requires_units: []
blocks: false

complexity:
  avg_complexity: 3
  avg_uncertainty: 3
  max_dependencies: 1
  testing_scope: 3
---

# Bolt: 043-cloud-storage-provider

## Overview

S3-compatible storage provider (recommended target: **Cloudflare R2**) plus a per-upload, location-aware preview (`302` to a pre-signed URL when an upload lives in the cloud; stream when it's local).

> **Re-scoped for the two-tier model (intent 024).** This bolt no longer picks a single provider per deployment. It registers **both** a local and a cloud store, adds `Upload.StorageLocation (Local|Cloud)`, and the preview serves each upload *from wherever it lives*. The promotion that actually moves files to the cloud is intent 024 (bolt 051). The migration tool (old story 003 / bolt 050) is **retired** — superseded by intent 024's paid-order backfill. `ddd-02-technical-design.md` will be revised accordingly before implementation.

## Stage Plan (DDD — 5 stages)

| Stage | Name | Output |
|-------|------|--------|
| 1 | Domain Model | `IStorageService` (+ `GetPresignedUrlAsync`); `StorageKey`/`PresignedUrl`/`Provider` semantics; `uploads/`+`thumbs/` key conventions |
| 2 | Technical Design | Provider switch wiring; `StorageSettings` (R2-first); fail-fast bucket verification; Polly; 302-redirect controller change |
| 3 | ADR Analysis *(optional)* | Key-scheme change (Option 2); provider-abstraction decisions |
| 4 | Implement | `S3StorageService`, `StorageSettings`, `Program.cs` switch, controller redirect, MinIO service in `ci.yml` |
| 5 | Test | Real MinIO integration tests (Save/Get/Delete/Exists/presign); redirect test |

## Dependencies

- **Requires**: 042-thumbnail-cache (cache lives at portable keys).
- **Enables**: 051-order-photo-promotion (intent 024 lifecycle); intent 021 / bolt 046 (distributed state).

## Key Technical Notes

- Recommended cloud target: **Cloudflare R2** — zero egress, CF edge near the Romanian audience (`EndpointUrl` + `Region=auto` + `ForcePathStyle`). AWS S3 / MinIO supported via the same code.
- Key scheme (**Option 2**): `uploads/{yyyy}/{mm}/{uploadId}{ext}` for originals, `thumbs/{uploadId}.jpg` for thumbnails.
- Bucket bootstrap is one-shot ops; app boot only **verifies existence (fail-fast)**, never creates the bucket.
- Tests run against a **real MinIO service in CI**; skip-gated locally where Docker is absent.
- Credentials (`AccessKey`/`SecretKey` / R2 API token) follow **ADR-006** — secret store, never committed.
