---
intent: 024-order-photo-archive
phase: inception
status: complete
created: 2026-05-27T13:00:00Z
updated: 2026-06-05T10:30:00Z
source: conversation 2026-05-27 (promote-on-payment lifecycle decision)
depends_on: 019-thumbnail-cache-and-cloud-storage
priority_score: 16
---

# Requirements: Order Photo Archive & Lifecycle

## Intent Overview

Intent 019 builds the cloud storage *mechanism* (`S3StorageService`, presigned URLs, location-aware preview). This intent defines the **business lifecycle** layered on top, using a **two-tier model**: photos live on the local deployment server while a customer browses/carts/checks out; **only when an order is paid** are its photos promoted to durable cloud storage and removed from the server. The full-resolution **original is purged once printing completes**; a generated **~2000 px large web preview + thumbnail are retained for 12 months** so the customer can review what they ordered in their account, after which they are cleaned up.

This minimizes third-party data footprint (abandoned/unpaid photos never reach the cloud; originals are not retained post-fulfilment) while preserving a customer-facing order-history archive.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Minimize third-party data footprint | Unpaid/abandoned photos never uploaded to cloud; originals deleted after printing | Must |
| Let customers review past orders | Order history shows thumbnail grid + ~2000 px full view for 12 months | Must |
| Free the deployment server | Promoted photos removed from local disk after confirmed cloud write | Must |
| Reliable, crash-safe promotion | No photo lost on webhook/job failure; local deleted only after confirmed cloud write | Must |

---

## Functional Requirements

### FR-1: Promote photos on order payment
- **Description**: When an order transitions to **Paid** (Stripe webhook / the legacy processor IPN), asynchronously promote each of its uploads to cloud: upload the original, generate + upload a ~2000 px large preview, ensure the thumbnail exists; set `Upload.StorageLocation = Cloud` and the cloud keys; **delete the local copies only after** the cloud writes are confirmed.
- **Acceptance Criteria**:
  - Promotion runs **off the webhook hot path** (enqueue → background worker).
  - Idempotent: re-running for an already-promoted order is a no-op.
  - Retried on transient failure; local files are never deleted before their cloud objects are confirmed.
  - Each upload row updated atomically after its objects are written.
- **Priority**: Must

### FR-2: Generate large web preview (~2000 px)
- **Description**: `ImageProcessor` produces a ~2000 px (long edge), q85 JPEG used for full-size order-history viewing — distinct from the 300 px thumbnail and the original.
- **Acceptance Criteria**: bounded by the existing decompression-bomb guard; stored under a `previews/` key prefix.
- **Priority**: Must

### FR-3: Schema additions
- **Description**: `Upload.LargePreviewPath varchar(512) NULL` and lifecycle bookkeeping (e.g. `OriginalPurgedAt`, archive-expiry derivable from order completion). `Upload.StorageLocation` (Local|Cloud) is added in **bolt 043** (intent 019) and consumed here.
- **Acceptance Criteria**: migration applies cleanly on Postgres and PostgreSQL.
- **Priority**: Must

### FR-4: Purge original after printing completes
- **Description**: When an order reaches the configurable "production complete" status (default: **Shipped**), delete the cloud **original** and null its key; retain large preview + thumbnail.
- **Acceptance Criteria**:
  - Only the original is removed; large + thumbnail remain.
  - Idempotent; safe if the original is already gone.
- **Priority**: Must

### FR-5: 12-month archive retention cleanup
- **Description**: A background job deletes the large preview + thumbnail and nulls their keys after a **configurable window (default 12 months)** measured from order completion. Order metadata is retained.
- **Acceptance Criteria**:
  - Window configurable (e.g. down to 6 months).
  - Active/recent orders within the window are never purged.
  - Per-order outcome logged; summary counters.
- **Priority**: Must

### FR-6: Order-history photo viewing
- **Description**: Registered users (and guests who registered + claimed their orders) can view, in the account order section, a thumbnail grid of each order's photos and open a full-size (~2000 px) view.
- **Acceptance Criteria**:
  - Backend endpoint lists an order's photos with **presigned** large-preview + thumbnail URLs (1 h TTL).
  - Ownership/claim authorization enforced before any URL is issued.
  - Frontend: order-detail thumbnail grid → click opens the large preview (lightbox).
  - Photos past the retention window simply no longer appear.
- **Priority**: Must

### FR-7: One-off backfill for pre-existing paid orders
- **Description**: A console command promotes photos for orders that were already Paid before this intent shipped (reuses the FR-1 promoter). **Supersedes the original "migrate all local files" scope** (bolt 050 / story 019-003), which is invalid under the two-tier model — we promote *paid orders*, not every upload.
- **Acceptance Criteria**: dry-run lists affected orders; full run promotes them; idempotent; resumable.
- **Priority**: Should

---

## Non-Functional Requirements

### Reliability
| Requirement | Metric | Target |
|-------------|--------|--------|
| Promotion durability | local deleted only after confirmed cloud write | 100% |
| Crash safety | partial promotion safe; resumes on retry | no data loss |

### Performance
| Requirement | Metric | Target |
|-------------|--------|--------|
| Large-preview generation | p95 | < 2 s |
| Webhook impact | added latency on Paid transition | ~0 (async enqueue) |

### Security & Privacy
| Requirement | Standard | Notes |
|-------------|----------|-------|
| Presigned URL TTL | ≤ 1 h | private cache only |
| Authorization | owner / claimed-guest | enforced before issuing any URL |
| Data minimization | GDPR | unpaid photos never in cloud; originals purged post-printing; archive auto-expires |

---

## Constraints

- **Triggers**: order→**Paid** (Stripe webhook / the legacy processor IPN) for promotion; order→**Shipped** (configurable) for original purge; both via the existing order status machine.
- Depends on intent **019** (`S3StorageService`, presigned URLs) and **bolt 043** (`StorageLocation`, location-aware preview).
- Pre-payment previews are served from local disk → single-node serving for the pre-payment phase (acceptable on the current single-VM deployment; revisit for multi-node).

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| Payment + order status machine exist (Paid → … → Shipped/Delivered) | Trigger hooks unavailable | Confirm status names during design |
| Fulfilment pulls the original from cloud | Printing can't access the file | Original is in cloud post-Paid until purge; presigned admin download |

## Open Questions

| Question | Owner | Resolution |
|----------|-------|------------|
| Q1: Which order status marks "printing complete → purge original"? | Ops | Pending — propose **Shipped** (configurable) |
| Q2: Retention measured from order *completion* vs *delivery* date? | Ops | Pending — propose order completion/delivery timestamp |
