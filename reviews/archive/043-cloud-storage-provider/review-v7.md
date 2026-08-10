---
type: review
target: 043-cloud-storage-provider
version: 7
supersedes: 6
commit: 2d02b13
branch: feat/bolt-043-cloud-storage-provider
pass-type: discovery
subtype: certification-pair
date: 2026-07-22
reviewer: "certification pair — two independent blinded full-manifest passes on frozen 2d02b13 (Pass A: correctness·security·race·db-parity·observability·requirements·quality; Pass B: correctness·security·race·input-validation·frontend-ux·tests-coverage·completeness-critic)"
verifies: null
verdict: request-changes
blockers: [F1]
findings: { high: 1, medium: 11, low: 17, cleanup: 5, reraised: 9 }
tests: { dotnet: "702/702 (+10 skipped MinIO)", frontend: "439/439" }
certified: false
---

# Review v7 — 043-cloud-storage-provider (CERTIFICATION PAIR)

The certification step of the loop: **two independent blinded full-manifest discovery passes**, both
against the frozen commit `2d02b13`, per the protocol in
[self-driving-loop-design.md](self-driving-loop-design.md). This is the **first time the full manifest
has ever run on this feature** — v1 was lean (5 lenses), v3/v5 were deltas, v2/v4/v6 were verifications.
The owed lenses (db-parity, observability, input-validation, whole-feature requirements) ran here for
the first time, alongside a second independent look by the core correctness/security/race lenses.

**Verdict: `request-changes`. NOT CERTIFIED.** The pair surfaced **1 High** (silent data loss on S3
retry) and a **cross-order data-loss class** independently found by BOTH passes, plus a wave of new
Mediums/Lows. Per the certification protocol and the severity-based stop rule, new serious findings
reset the counter → **back to a fix round**; the feature is **not merge-ready**. This overturns the
"merge-ready with followups" reading after v6: v6 only verified that the two v5 fixes held — it could
not certify, and the full manifest has now shown why.

- **Pass A** (`wf_8e3b5928-a15`): 33 agents, 1.83M tok, 27 raw → **25 canonical**.
- **Pass B** (`wf_9250d60a-e9f`): 40 agents, 2.13M tok, 30 raw → **28 canonical**.
- Combined ~3.96M tok / 73 agents. Two verified independently by inspection (the High and the
  shared-upload premise — see below); the rest carry the workflow's convergence-weighted skeptic
  verdicts.

## 🔴 Blocker

**F1 / D49 — S3 `SaveAsync` rewinds the stream outside the Polly retry loop → silent data loss.**
`src/PhotoPrint.API/Services/S3StorageService.cs:63-72`. `content.Position = 0` runs once at line 63,
*before* `_pipeline.ExecuteAsync` (line 66). On a transient `AmazonS3Exception` Polly re-invokes the
upload lambda (MaxRetryAttempts=3), but the stream is never rewound per-attempt — the retry reads from
end-of-stream and uploads a **truncated/empty object**. The PUT succeeds, `OrderPhotoPromoter` flips the
row to `Cloud` and deletes the local original → the customer's paid original is **permanently lost**,
silently. **Confirmed by direct inspection** (Pass B, correctness; confidence 6 → verified real).
Fix: move the rewind *inside* the `ExecuteAsync` lambda so every attempt rewinds; reject/buffer
non-seekable streams. This is the marquee catch the full-manifest certification existed to find — six
prior passes never inspected the S3 retry internals.

## 🟠 New Mediums

The headline is a **class both passes found independently** (real cross-pass convergence — the strongest
signal the pair produces):

**F2 / D50 — shared-upload cross-order data loss.** An `Upload` is shared by ID across orders
(`OrderService.cs:77` copies `ci.UploadId`; no per-order copy — **confirmed by inspection**). Destructive
operations keyed on *one* order's lifecycle then destroy a photo a *second* still-active order needs:
- *Purge* (`OriginalPurger.cs:103`, Pass A): order A ships → purge deletes the shared cloud original +
  nulls `FilePath`; order B (paid, unfulfilled) ZIPs with a silently truncated archive, original gone.
- *Retention* (`ArchiveRetentionJob.cs:95-96`, **both passes**): `Any(PaidAt < cutoff)` deletes the
  shared preview/thumbnail on the *oldest* order's age; a newer in-window order loses its viewable
  photos permanently (no regeneration once the original is purged).
- Fix is a **design decision** (deserves the rule-3 approach-check): ref-count / "all referencing orders
  past cutoff" before any destructive op on a shared upload. Flagging for owner direction, not patching.

| F# | D# | Prov | File | Finding |
|----|----|------|------|---------|
| F3 | D51 | A+B | `OrderPhotoPromotionWorker.cs:107/283` | Worker holds the concurrency slot **and DI scope** for the entire retry backoff (`Task.Delay` up to 3600s inside the semaphore) → all `MaxConcurrentOrders` slots park in backoff during a cloud blip, starving fresh promotions. Both passes. |
| F4 | D52 | A | `OrderService.cs:460` | `GetOrderPhotosAsync` has no `DeletedAt == null` filter → serves presigned URLs for uploads `UploadCleanupJob` already soft-deleted (blobs gone) → broken thumbnails/lightbox the refresh can't recover. |
| F5 | D53 | B | `WebhooksController.cs:215` | Paid-transition is an unguarded check-then-act (no concurrency token) → concurrent duplicate Stripe deliveries double-enqueue promotion **and send duplicate confirmation emails**. Extends D9 with a new user-visible consequence. |
| F6 | D54 | B(conv2) | `UploadService.cs:52` | UploadService re-advertises **HEIC** as accepted, but the MIME validator + UI still reject it — reintroduces bolt-042 **M5**. A fix-regression across features. |
| F7 | D55 | B(hinted) | `UploadService.cs:113` | Client filename not truncated to the `varchar(260)` column → InMemory/SQLite accept, **Postgres rejects (22001) → 500 in prod only**. Passes all tests, breaks in prod. |
| F8 | D56 | A | `ArchiveRetentionJob.cs:123` | `ArchiveExpired` audit event logged *before* the batched `SaveChanges` → on save failure, audit records fire for rows never persisted, and re-fire next tick. Duplicate/false audit trail. |
| F9 | D57 | A | `OriginalPurger.cs:43` | Cloud-off purge refusal logged at **Error on every ship** in the default `Provider=local` config (the ship path lacks the `CloudEnabled` gate the cancel path has) → chronic false-Error noise that masks real errors. |
| F10 | D58 | B | `OrderPhotoPromotionWorker.cs:130` | The worker retry/backoff/re-enqueue path is **entirely untested**. |
| F11 | D59 | B | `WebhooksController.cs:183` | The webhook→promotion `EnqueueAsync` wiring is never asserted by any test. |
| F12 | D60 | B | `S3StorageServiceIntegrationTests.cs:18` | The real cloud provider is exercised only by skip-gated MinIO + in-memory fakes — R2/AWS behavior (incl. the D49 retry path) never actually run. |

*(db-parity: Pass A's `AddUploadArchiveFields` "hardcodes text" medium was skeptic-**refuted** as the
systemic, already-documented DB-1 effect, not a per-column bug → folded into the deferred **D20**, not a
new finding.)*

## 🟡 New Lows (16) — compact

Retention `OrderBy/Take` starvation on persistent delete-failure (D61, `ArchiveRetentionJob.cs:98` — the
D38 class in a second job) · admin ZIP mid-loop `GetStream` truncation TOCTOU (D62, **both passes**) ·
preview-regen races retention delete → orphan blob (D63, both passes) · failed best-effort local delete
leaks local bytes (D64) · `LocalStorageService` storage-root prefix match without separator boundary
(D65) · ZIP entry extension taken from untrusted client filename (D66) · batch upload has no file-**count**
cap, only 500MB total (D67) · broken grid thumbnails have no fallback after the single refresh (D68) ·
originals of orders never reaching production-complete/Cancelled escape the retention window (D69) · 043
NFR "persistent S3 → 502" not implemented (surfaces as 500) (D70) · idempotent-skip reasons at Debug
never emit under the Information floor (D71) · transient vs permanent cloud-write failures collapsed into
one Warning (D72) · `GetPreviewAsync` dropped `AsNoTracking` on the hot cache-hit path (D73, bolt-042
QUAL-1 regression) · promotable-status set triplicated with a false "single source of truth" comment
(D74) · S3 Polly retry + presign-protocol untested (D75) · storage DI/AWS-region config foot-gun (D76) ·
unindexed full-scan sweeps every 6h (D77, plausible).

## ⚪ Cleanup (5)

Promoter `ToArray` + undisposed `MemoryStream`s (D78) · best-effort orphan-thumb delete swallows its
exception with no log (D79) · local-preview `Cache-Control` ADR-vs-code mismatch (D80) · freshly generated
local thumbnail re-read from disk (D81) · redundant dual error feedback, interceptor toast + inline (D82).

## Re-raises of decided items (10 — prior decisions attached, no re-litigation needed)

D27 (concurrent-promotion orphan → bolt-035), D17 (purge-on-cancel — but see below), D35 ×2 (periodic
sweep dedup → bolt-035), D9 (webhook race → bolt-035; **D53 sharpens it with the double-email
consequence**), D20 ×2 + the two db-parity plausibles (migration text/varchar → 3-env), D14 (cloud
regen fake-stream coverage), D42/D43/D40 (frontend backlog). All matched their prior decision correctly.
**One worth owner attention: D17/F(reraise)** — the requirements lens argues purge-on-cancel *reverses a
documented "keep cancelled originals" decision* (052 ddd-02 Q1) and was never in a story/AC. It was an
owner ruling, so it stands, but the **docs and code disagree** — reconcile 052 ddd-02 / ADR-012.

## Overlap & saturation

The two passes converged independently on the serious spine — the shared-upload data-loss class, the
worker-slot stall, the ZIP mid-loop TOCTOU, the preview-regen/retention orphan race — which is strong
evidence those are real, not single-lens noise. But they also each found large **disjoint** sets (Pass
B's High and HEIC/filename/coverage cluster were absent from A; Pass A's soft-deleted-serve and
observability cluster were absent from B). **Disjoint findings ⇒ the population is not exhausted ⇒ not
saturated ⇒ not certifiable** — exactly the bolt-035 lesson ("a review going quiet measures the reviewer,
not the code"), and here the first full pass was anything but quiet.

## Meta-note (for the loop's own record)

This is the load-bearing result of the whole exercise: six prior passes (v1 lean, v3/v5 delta, v2/v4/v6
verification) drove the *known* population to green and read as "merge-ready," yet the first full-manifest
pass found a data-loss **High** and a data-loss **class**. The delta/verification tiers are structurally
blind to whole-feature issues (a shared-upload lifecycle spanning promote+purge+retention; S3 retry
internals; cross-feature regressions like HEIC). The recalibration's severity-based stop correctly kept
certification mandatory for a full-loop-tier (storage) feature — without it, this would have merged. The
cost lesson also holds: the pair cost ~3.96M tokens and it was worth it *here*; it would not have been on
a UI-only bolt (why certification is risk-tiered).

## Next

**Fix round required** (`/fix-review` on this v7), blocker-first:
1. **D49 (High)** — move the S3 rewind inside the retry lambda + reject non-seekable; regression test that
   a retried upload re-sends full bytes. This one should be fixed regardless of anything else.
2. **D50 (shared-upload data loss)** — a **design decision** (ref-count uploads across orders / "all
   referencing orders past cutoff"); run the rule-3 adversarial approach-check before implementing.
3. The new Mediums (D51–D57) + the coverage Mediums (D58–D60); Lows/Cleanups → backlog unless cheap.
Then verify → and because certification is not yet passed, **re-freeze and re-run the certification pair**
once the serious findings are fixed (counter reset). Cluster A (D35/D37/D46/D47) remains owed to bolt-035;
D53 is another reason to prioritize that concurrency-token work.

Provenance: Pass A `wf_8e3b5928-a15`, Pass B `wf_9250d60a-e9f` (full skeptic evidence in the workflow
transcripts). Frozen tree `2d02b13`; suites **.NET 702/702 (+10 skipped MinIO) · FE 439/439** unchanged.
