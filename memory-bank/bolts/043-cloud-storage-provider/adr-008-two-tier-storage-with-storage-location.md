---
bolt: 043-cloud-storage-provider
created: 2026-05-28T08:12:00Z
status: accepted
superseded_by: null
---

# ADR-008: Two-Tier Storage with Per-Upload `StorageLocation` and `IStorageRouter`

## Context

Intent 019 originally framed `IStorageService` as **one provider per deployment**: `Storage:Provider=Local` registers `LocalStorageService`, `Storage:Provider=S3` registers `S3StorageService`, end of story. The preview endpoint branched on this single config value.

Mid-construction of bolt 043 we chose a different lifecycle (intent 024 — **promote-on-payment**): photos stay on the local deployment server during browse/cart/checkout and are promoted to durable cloud storage **only when the order is paid**. After printing completes the original is purged; the ~2000 px large preview + thumbnail remain in cloud for ~12 months as the customer-facing order archive.

This requires **both stores to be active at once**: pre-payment bytes live locally; promoted bytes live in cloud. The preview endpoint can no longer decide stream-vs-`302` from a single per-deployment flag — it must decide *per upload*. The decision was deliberate (data minimization + GDPR), but it inverts the original architecture.

## Decision

**Run the storage layer as two tiers — local (always available) and cloud (optional) — and resolve which tier owns any given upload via `Upload.StorageLocation` and a new `IStorageRouter`.**

- `Upload.StorageLocation` enum column (`Local` default | `Cloud`) — added by this bolt's migration.
- `IStorageRouter` injects both adapters (via .NET 8 keyed DI: `"local"`/`"cloud"`) and exposes `For(StorageLocation)`, `Local`, `Cloud`, and `CloudEnabled`.
- **`Storage:Provider` is repurposed**: it no longer selects a single store. `Provider=Local` (dev default) means "cloud tier off — every upload stays Local; promotion is a no-op." `Provider=S3` (prod) wires the cloud adapter, the `IAmazonS3` factory, and the `S3BucketVerifier` (fail-fast at boot).
- The preview endpoint branches on the upload's own `StorageLocation`: `Local` → stream + `Cache-Control: public, max-age=2592000, immutable` (unchanged from bolt 042); `Cloud` → presigned `302` + `Cache-Control: private, max-age=3600`.
- Promotion of `Local → Cloud` per upload lives in **intent 024 (bolt 051)** — this bolt only builds the mechanism.

## Rationale

Three architectural goals were in tension:

1. **Data minimization (chosen driver).** Most uploads are abandoned. A single-tier "everything to cloud on upload" model ships every guest's abandoned photos to a third-party cloud — bad for GDPR posture in an EU business. Two-tier keeps abandoned bytes off the cloud entirely.
2. **Multi-replica scalability** (the original intent-019 goal). Two-tier sacrifices this *for the pre-payment phase*: pre-payment bytes are on one VM's disk. Acceptable now (single-VM deployment per bolt-040 recommendation); revisited when we scale (shared staging volume or short-lived cloud staging prefix).
3. **Operational simplicity.** A per-upload router is more moving parts than a per-deployment switch. We accept that complexity because the lifecycle benefit (smaller cloud footprint + cleaner privacy story) is concrete and the router itself is small (~one interface + keyed DI).

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|-------------|------|------|--------------|
| Two-tier with `StorageLocation` + router (chosen) | Best GDPR/data-minimization; minimal cloud footprint; aligns with single-VM reality | Pre-payment serving = single-node; per-upload location tracking + promotion job complexity | **Accepted** |
| Cloud-from-upload (original) | Fully multi-replica; simplest; matches stories 001/002 verbatim | Every abandoned photo briefly in cloud; weaker privacy story; relies on cloud-side lifecycle rules to bound cost | Rejected for an EU photo business handling personal images |
| Hybrid: cloud-from-upload to `staging/` prefix with auto-expiry, promote on paid | Scalable + retention-controlled | Still puts abandoned photos in cloud briefly; most moving parts | Rejected — gets the worst of both unless really needed |

## Consequences

### Positive

- Unpaid/abandoned photos **never reach** the cloud — strong, concrete data-minimization story.
- Pre-payment serving is free of any cloud SDK or credential dependency (dev can run with `Provider=Local` indefinitely).
- The router cleanly accommodates intent 024's lifecycle (promote, purge-original, retention cleanup) without further re-wiring.
- Bolt 046 (distributed state / Redis) still gets its prize — promoted (paid-order) photos *are* shared across replicas.

### Negative

- Pre-payment uploads are bound to a single VM. Scaling out the API now requires either a shared staging volume (NFS, EFS, etc.) or moving to a short-lived cloud staging prefix.
- Two adapter registrations + a router + a per-upload column = more surface area than a single switch.
- Story 001's original acceptance criterion ("`Provider == S3` registers `S3StorageService` over `LocalStorageService`") is reinterpreted; the construction-log records the scope change.

### Risks

- **Risk**: a programmer adds a code path that calls `_router.Cloud` when `CloudEnabled` is false. **Mitigation**: `Cloud` throws `InvalidOperationException` on access; the preview path goes through `For(StorageLocation)` (always safe).
- **Risk**: an upload's `StorageLocation` and its actual byte location drift (e.g. flipped to Cloud but cloud write failed). **Mitigation**: intent-024 promoter updates the row **only after** confirmed cloud writes; local is deleted only after the row is updated.

## Related

- **Stories**: 001-s3-storage-service, 002-preview-redirect-presigned-url (intent 019); the entire intent 024 lifecycle.
- **Standards**: superseding part of intent-019 FR-2 / FR-3 (Provider-as-switch interpretation); the construction log captures the scope-change.
- **Previous ADRs**: complements ADR-007 (caller-supplied keys make the per-tier handoff trivial — same key, two adapters).
