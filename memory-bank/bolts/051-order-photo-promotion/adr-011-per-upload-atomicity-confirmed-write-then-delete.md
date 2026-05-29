---
bolt: 051-order-photo-promotion
created: 2026-05-29T10:18:00Z
status: accepted
superseded_by: null
---

# ADR-011: Per-Upload Atomicity with Confirmed-Write-Then-Delete

## Context

Bolt 051 promotes a paid order's photos from local disk to cloud storage. Each upload involves multiple side effects — cloud writes (original, large preview, thumbnail), a DB row update flipping `StorageLocation` to `Cloud` and setting three key columns, and local-file deletes. These side effects can fail independently, and the process can crash between any of them.

Two related questions had to be answered before implementation could begin:

1. **What is the unit of atomicity?** The order (all photos succeed or none) or the upload (each photo is its own commit point)?
2. **In what order are side effects applied?** Specifically: when do local files get deleted relative to cloud writes and the DB row update?

These questions matter beyond bolt 051: unit 002 (purge after printing) and unit 003 (order-history viewing) both depend on a clear invariant for "where do an upload's bytes live?" — without a pinned contract here, those units will derive their own (likely inconsistent) answers.

This ADR pins the contract.

## Decision

**Atomicity is per-upload, not per-order. Side effects are applied in the order: cloud writes → DB row update → local file deletes ("Confirmed-Write-Then-Delete"). The DB row is the single source of truth for an upload's storage location.**

### Per-Upload Atomicity

- An order's promotion is a *loop* over its uploads. Each upload's promotion is independent.
- If upload A succeeds and upload B fails inside the same order, A stays promoted (`StorageLocation = Cloud`, row updated, cloud bytes durable, local files gone) and B stays Local (untouched). The order is re-enqueued; the next pass skips A (idempotent) and retries B.
- There is **no all-or-nothing transaction** wrapping the order. `Order.IsArchived` is **derived** (`order.Uploads.All(u => u.StorageLocation == Cloud)`), not a stored flag.

### Confirmed-Write-Then-Delete

Per `PromoteUploadAsync`, side effects strictly in this order:

1. **Cloud writes** — original, thumbnail, large preview, each via `IStorageRouter.Cloud.SaveAsync`. Polly retries handle transient S3 errors at this layer.
2. **DB row update** — single `SaveChangesAsync` flipping `StorageLocation` to `Cloud`, setting `FilePath`, `ThumbnailPath`, `LargePreviewPath` to the new cloud keys. **This is the durability boundary.** Once it succeeds, the cloud bytes are the canonical location.
3. **Local file deletes** — `IStorageRouter.Local.DeleteAsync` for the old original key and the old thumbnail key. **Best-effort:** wrapped in try/catch, failures are logged at Warning, the upload as a whole still counts as Promoted.

### The Single Source of Truth Rule

> If `Upload.StorageLocation = Cloud`, the cloud bytes exist.
> If `Upload.StorageLocation = Local`, the local files exist.
> No other state is observable from outside the promoter.

The promoter's three-step ordering is the only thing that maintains this rule. Future code must not reorder it.

## Rationale

### Why per-upload, not per-order

Wrapping the order in a transaction (or in a logical retry-everything-on-any-failure block) creates two bad failure modes:

- **The "one bad photo blocks all" failure**: a single corrupt file or a single 4xx from S3 (e.g. content-type rejection) means the entire order's photos sit Local indefinitely, even though 11 of 12 would have promoted fine. We don't want one bad upload to defer all the others.
- **The "atomic rollback cost" failure**: rolling back already-uploaded cloud objects on a partial failure means a delete pass per upload + potential rate-limit exposure. The cloud writes are not transactional — pretending they are is a lie that requires real cleanup code.

Per-upload atomicity sidesteps both. The order-level concept of "archived" becomes a derived check (`all uploads Cloud`) rather than a stored flag; partial promotion is a real, observable, normal state.

The cost is observability: there's no single "this order is fully promoted" instant. We accept that — it's reconstructible from `Upload.StorageLocation` whenever wanted, and no downstream code (unit 002 purge, unit 003 viewing) needs it as a stored flag.

### Why cloud writes before DB update

If we updated the DB row first and *then* uploaded:

- A crash between row-update and cloud-write would leave `StorageLocation = Cloud` but no cloud bytes. The preview endpoint would generate a presigned URL pointing at a 404. The recovery scan wouldn't find this case (the row says Cloud). **Silent data loss.**
- Sequencing the other way (cloud first, row update second), the worst-case crash leaves cloud bytes orphaned with the row still Local. The next sweep redoes the cloud writes (S3 `PUT` is idempotent — same key, same bytes, no consequence). **Recoverable.**

So: cloud writes go first.

### Why DB update before local deletes

If we deleted local files first and *then* updated the row:

- A crash between local-delete and row-update would leave the row pointing at local keys that no longer have files. The next attempt to serve a preview would 404. The recovery scan would see `StorageLocation = Local` and try to re-read source bytes that are gone. **Permanent corruption.**
- The chosen order: a crash after the row update but before local deletes leaves Cloud row + lingering local files. The next sweep observes `StorageLocation = Cloud` and just deletes the local files (idempotent — `File.Delete` on a non-existent file is also handled). **Recoverable, no data loss.**

So: row update goes before deletes.

### Why local deletes are best-effort

Once the row says Cloud, the local files are *irrelevant* — no code path reads from them anymore (the preview endpoint branches on `StorageLocation`). A failure to delete them is litter, not data loss. Treating delete failures as upload failures would gate Promoted on a side concern that doesn't matter.

The litter is bounded: each undeleted local file is a few MB; the recovery scan and any subsequent successful sweep will re-attempt delete; a periodic disk-usage check (out of scope for this bolt) can finish the cleanup.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|-------------|------|------|--------------|
| **Per-upload atomicity + Confirmed-Write-Then-Delete (chosen)** | Crashes always recoverable; no all-or-nothing trap; clear contract for units 002/003 | Partial promotion states are normal; "order archived" is derived not stored | **Accepted** |
| Per-order atomicity wrapped in DB transaction + best-effort cloud cleanup on rollback | Stronger "order-archived" semantics; single SaveChanges | Cloud writes can't actually be rolled back; rollback code is itself failure-prone; one bad photo blocks all | Rejected — false atomicity over a non-transactional medium |
| Cloud writes after DB update, local-delete before DB update | None really | Both directions of swap create unrecoverable silent-loss states | Rejected — provably worse on crash analysis |
| Local-delete tied to cloud-write success (delete each local file the instant its cloud sibling is confirmed) | "Cleanest" intermediate state | Crash mid-loop: original cloud + original local both gone, thumbnail local gone, no row update → unrecoverable | Rejected — interleaves writes and deletes in an unsafe way |
| Two-phase commit (cloud writes, DB row "pending", then commit DB row + delete local) | Theoretically safer | Adds a `StorageLocation = Promoting` enum value, more complex queries, no real win over chosen order | Rejected — complexity without payoff |

## Consequences

### Positive

- **Crash recovery is trivial.** Every possible crash leaves a recoverable state observable from `StorageLocation`.
- **Future units 002 and 003 inherit a clear contract.** Purge-after-printing (unit 002) trusts "row says Cloud ⇒ cloud bytes exist." Order-history viewing (unit 003) trusts the same.
- **The promoter and the recovery scan share the same idempotency check** (`StorageLocation == Cloud → skip`). Three different code paths (live worker, recovery scan, backfill CLI) converge at the same per-upload work.
- **No transaction coordination across services.** Cloud SDK calls don't participate in DB transactions; we don't pretend they can.

### Negative

- **No single "order is archived" timestamp** — derived from upload rows. Observability tooling needs to know to look at the uploads, not just `Order.PromotedAt` (which doesn't exist).
- **Partial-promotion states are normal, not exceptional.** Operators watching `StorageLocation = Local` counts on a paid order should not assume "failure" — it might be in-progress, or in retry-backoff. The `UploadPromotionFailed` log event is the actual failure signal.
- **Best-effort deletes can leave litter.** A reliable disk-fill-up alarm is the safety net; a periodic cleanup pass (post-bolt) can finish the job if it becomes a problem.

### Risks

- **Risk**: a future contributor wraps the order loop in `using var tx = await db.Database.BeginTransactionAsync()` thinking they're "tightening atomicity." This would re-introduce the all-or-nothing trap. **Mitigation**: this ADR; a comment in `OrderPhotoPromoter.PromoteOrderAsync` pointing to it.
- **Risk**: a future contributor moves the local-delete step before the DB SaveChanges to "free disk faster." This would re-introduce silent corruption. **Mitigation**: this ADR; the per-upload pseudocode in `ddd-02-technical-design.md`.
- **Risk**: someone reads `StorageLocation` *during* a promotion (between cloud writes and row update) and gets `Local`, then tries to use local paths that are about to be deleted. **Mitigation**: not actually a race — the row is updated *before* deletes, so a reader who saw `Local` will continue to find local files because the delete hasn't happened. The reader will see stale data, not broken data.

## Related

- **Stories**: 003-promote-on-paid (intent 024). The other three stories in the bolt inherit the contract but don't surface it.
- **Standards**: complements ADR-008 (two-tier storage — defines *that* `StorageLocation` exists; this ADR defines *how it is flipped*).
- **Previous ADRs**: ADR-007 (caller-supplied keys — the promoter writes to caller-supplied keys, same as `UploadService`); ADR-008 (two-tier `IStorageRouter`).
- **Future units**: unit 002 (`OriginalPurgedAt` becomes the analogous lifecycle write for the *purge* operation — same single-source-of-truth philosophy); unit 003 (read path trusts the invariant set here).
