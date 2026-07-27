---
id: 002-preview-redirect-presigned-url
unit: 002-cloud-storage-provider
intent: 019-thumbnail-cache-and-cloud-storage
status: complete
priority: must
created: 2026-05-25T10:30:00Z
completed: 2026-05-29T08:30:00Z
assigned_bolt: 043-cloud-storage-provider
implemented: true
---

# Story: 002-preview-redirect-presigned-url

## User Story

**As** the platform
**I want** preview requests on cloud-storage deployments to redirect to a pre-signed URL
**So that** bytes are served directly from object storage / CDN, not proxied through the API

## Acceptance Criteria

- [ ] **Given** `Storage:Provider != "Local"` and a valid `Upload.ThumbnailPath`, **When** `GET /api/uploads/{id}/preview` is called, **Then** the response is `302 Found` with `Location: <pre-signed URL valid 1 h>` and `Cache-Control: private, max-age=3600`.
- [ ] **Given** `Storage:Provider == "Local"`, behaviour is unchanged (controller proxies bytes).
- [ ] Authorization check (owner / admin) runs before the redirect; unauthorized callers see 403, never the URL.
- [ ] Integration test asserts the redirect URL points at the configured S3 endpoint and includes a valid signature query string.

## Technical Notes

- Use `GetPresignedUrlAsync(path, ttl: TimeSpan.FromHours(1))`.
- `Cache-Control: private` so intermediate caches don't share the signed URL across users.

## Dependencies

### Requires
- 001-s3-storage-service

### Enables
- 003-local-to-cloud-migration-tool

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Pre-signed URL expired in client cache | Browser will refetch and receive a fresh redirect from the API |
| Customer with CDN in front (e.g. Cloudflare) | Pre-signed URLs are unique per request, so CDN caches by URL — acceptable churn at low volume; revisit at scale |

## Out of Scope

- Per-tenant signing keys.
