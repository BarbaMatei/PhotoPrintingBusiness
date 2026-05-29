---
bolt: 043-cloud-storage-provider
created: 2026-05-28T08:14:00Z
status: accepted
superseded_by: null
---

# ADR-009: Cloudflare R2 as the Recommended Concrete Cloud Target

## Context

`S3StorageService` is implemented against `AWSSDK.S3`, which speaks the S3 API and works equally well against AWS S3, Cloudflare R2, MinIO, or any S3-compatible backend. *Which* concrete backend a production deployment should use is a separate decision.

The relevant workload is image serving for an EU (Romanian) photo-printing e-commerce business: thumbnails and (post-intent-024) large previews are fetched repeatedly via presigned URLs; originals are uploaded once and (after printing) deleted; storage volume is modest, **egress is the dominant cost driver**.

## Decision

**Cloudflare R2 is the recommended concrete cloud target.** AWS S3 and MinIO remain fully supported by the same code; this is a configuration recommendation, not a code lock-in.

Configuration shape for R2 (documented in `StorageSettings` + `.env.example`):

```text
Storage:Provider       = "S3"
Storage:Bucket         = <bucket>
Storage:Region         = "auto"                           # R2 quirk
Storage:EndpointUrl    = "https://<account-id>.r2.cloudflarestorage.com"
Storage:ForcePathStyle = true                             # required for R2 / MinIO
Storage:AccessKey      = <from secret store — ADR-006>
Storage:SecretKey      = <from secret store — ADR-006>
```

## Rationale

R2 is not architecturally different from S3 — it speaks the same API. The choice is **economic and geographic**.

- **Zero egress.** R2 charges **$0 egress**. AWS S3 charges ~$0.09/GB out (first tier). For an image-serving workload, egress dominates the bill; this is the decisive factor.
- **Edge proximity.** R2 sits behind Cloudflare's CDN with a Bucharest PoP. Pairs naturally with the `thumbs/` prefix layout (Option-2) and a single CDN cache rule for the hot path.
- **Storage cost.** R2 storage ~$0.015/GB-month vs. S3 ~$0.023/GB-month — small absolute win, but consistent.
- **API compatibility.** Same `AWSSDK.S3` code; only `EndpointUrl` / `Region=auto` / `ForcePathStyle` change. If R2 ever proved unsuitable, swapping to AWS S3 is a config change.

The decision is recorded as an **ADR rather than a code change** because the *recommendation* is what carries forward — the code is already vendor-neutral.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|-------------|------|------|--------------|
| Cloudflare R2 (chosen) | $0 egress (decisive for image serving); CF edge proximity; cheap storage | Newer than S3; smaller feature surface (no IA tiering, etc.); SDK quirks (`Region="auto"`, `ForcePathStyle`) | **Accepted** |
| AWS S3 | Most mature; widest ecosystem; AWS sandbox familiar to many devs | Per-GB egress dominates the bill; no native CF-edge integration | Rejected on cost alone for this workload |
| Self-hosted MinIO (prod) | No vendor; full control; cheap on a VM you already own | We operate it (durability, backup, replication, SLAs); CF edge benefits gone | Rejected — operational burden disproportionate to the savings |
| Azure Blob via a separate adapter | Azure ecosystem if we ever go there | Adds a second adapter to maintain; same egress issue as S3 | Rejected — out of scope; S3 API covers our needs |

## Consequences

### Positive

- Cost predictability for the dominant workload (image fetches) — egress doesn't appear on the bill.
- Customer-facing latency is near-CDN-edge for Romanian users without any extra CDN setup.
- Documentation (DEPLOYMENT.md, `.env.example`) can show a concrete R2 config as the worked example.

### Negative

- One vendor-specific quirk to remember: `Region="auto"` + `ForcePathStyle=true` are mandatory for R2. The `StorageSettings` validator must accept `"auto"` as a region.
- R2 lacks a few S3 features (storage classes / IA tiering, some headers). We don't use them — but it constrains future evolution if we ever need them.

### Risks

- **Risk**: presigned-URL + CDN cache churn (each signed URL is unique → poor cache-hit ratio when fronted by a generic CDN). **Mitigation**: noted as a *future* option to serve `thumbs/` via a public/custom-domain bucket with Cloudflare's own access control, dropping per-request signing on the hot path. Out of scope for bolt 043.
- **Risk**: R2 pricing changes. **Mitigation**: code is vendor-neutral; swap to S3 / MinIO is config-only.

## Related

- **Stories**: 001-s3-storage-service (R2 is the recommended target of `S3StorageService` in prod).
- **Standards**: should be reflected in `docs/DEPLOYMENT.md` (concrete R2 config example).
- **Previous ADRs**: complements ADR-006 (credentials in secret store) — the R2 API token is the secret in question.
