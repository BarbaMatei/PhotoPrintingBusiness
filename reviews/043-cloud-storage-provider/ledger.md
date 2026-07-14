---
type: review-ledger
target: 043-cloud-storage-provider
updated: 2026-07-14
---

# Canonical finding ledger — 043-cloud-storage-provider

Stable `D#` identities for this target, per the README's persistent-ledger standard. Each real defect
gets a `D#` that lives forever; each pass's pass-local `F#` maps onto a `D#` **after** the blinded pass
completes (blinding is preserved during the search — finders never see `D#`).

**v1 is the first pass**, so `F#` ↔ `D#` is 1:1 and there is no cross-pass overlap to compute yet. The
overlap / capture–recapture saturation signal only becomes meaningful once a **second blinded discovery
pass against the same frozen commit** runs (per the README) — this lean 5-lens pass is a single draw.

Status column reflects the cumulative outcome through **v2 (verification)**: `verified` = fix held
(revert-and-rerun / construction / inspection); `wont-fix` / `deferred` = decision upheld by v2.

| D# | First seen | Sev (v1) | Status (through v2) | Title |
|----|-----------|----------|--------|-------|
| D1 | v1 · F1 | 🔴 High | **verified** *(was blocker)* | Admin ZIP fulfilment download reads promoted originals from local tier only |
| D2 | v1 · F2 | 🟠 Med | **verified** | UploadCleanupJob deletes Cloud uploads against local tier; never deletes LargePreviewPath |
| D3 | v1 · F3 | 🟠 Med | **verified** | Cloud missing-original throws AmazonS3Exception not FileNotFoundException → preview 500 |
| D4 | v1 · F4 | 🟠 Med | **verified** | Purge on Shipped one-shot; skips in-flight promotion → original never purged until reboot |
| D5 | v1 · F5 | 🟠 Med | **verified** *(part b → frontend-ux)* | Presigned-URL TTL vs hardcoded Cache-Control max-age divergence → expired/broken images |
| D6 | v1 · F6 | 🟠 Med | **verified** | Promotion worker disposes concurrency semaphore under in-flight tasks on shutdown |
| D7 | v1 · F7 | 🟠 Med | **verified** | Migration DDL (FilePath NOT-NULL drop) unverified by tests/CI |
| D8 | v1 · F8 | 🟡 Low | **verified** | Preview GET TOCTOU: promotion deletes local thumb between service read and stream-open → 500 |
| D9 | v1 · F9 | 🟡 Low | **deferred** → bolt-035 | Concurrent duplicate payment webhooks race Order.Status (no concurrency token) |
| D10 | v1 · F10 | 🟡 Low | **wont-fix** (403 convention) | 403-vs-404 order-existence oracle on /photos and /detail |
| D11 | v1 · F11 | 🟡 Low | **verified** | /photos returns presigned URLs without Cache-Control: private |
| D12 | v1 · F12 | 🟡 Low | **wont-fix** (owner: user-only) | Guest-placed orders unreachable from the new /photos endpoint |
| D13 | v1 · F13 | 🟡 Low | **deferred** → frontend-ux | FE empty-state copy "no longer available" misfires for not-yet-promoted orders |
| D14 | v1 · F14 | 🟡 Low | **verified** | Cloud preview regen branch never exercised (fake presets ThumbnailPath) |
| D15 | v1 · F15 | 🟡 Low | **verified** | GET /orders/{id}/photos has no integration test (auth pipeline untested) |
| D16 | v1 · F16 | 🟡 Low | **verified** | Promoter row-update-failure and preview-generation-failure branches untested |
| D17 | v1 · F17 | 🟡 Low | **verified** (owner: purge-on-cancel) | Paid-then-cancelled original-purge lifecycle |
| D18 | v1 · F18 | 🟡 Low | **verified** | BackfillCommand and S3BucketVerifier have zero tests |
| D19 | v2 · carry-fwd | 🟡 Low | **open** *(next discovery pass)* | PromotionRecoveryScanner still boot-only (F4-class, promotion subsystem) — surfaced during v1 fix, not fixed |

## Refuted (recorded, no D# assigned)

| Was | Sev | Claim | Disposition |
|-----|-----|-------|-------------|
| v1 R1 | 🟡 Low | S3StorageService coverage hinges on the MinIO gate | **refuted** — CI runs MinIO on every PR/non-main push and sets the `STORAGE_TEST_*` vars, so the SkippableFacts execute. Do not re-raise; the residual `IsTransient`/multipart gap is tracked under D3/D16. |

## Accepted decisions / deferrals (through v2)

- **D12 / F12 — guest order-history photos: wont-fix (owner ruling).** `/photos` stays `[Authorize]`
  user-only; guests out of scope for bolt-053. Forward path if revisited: DualAuth + guest branch.
- **D17 / F17 — paid-then-cancelled originals: fixed (owner ruling = purge on cancel).** Fast-path
  purge in `CancelOrderAsync` + `Cancelled` in the recovery-sweep status set.
- **D10 / F10 — 403-vs-404 oracle: wont-fix.** 403-for-non-owner is the codebase convention;
  negligible GUID-enumeration risk. Re-open only as a codebase-wide 404 standardisation.
- **D9 / F9 — duplicate-webhook Order.Status race: deferred → bolt-035** (needs an `Order`
  concurrency token / event-dedup — payment-idempotency remit; no data loss today).
- **D13 / F13 + D5b / F5(b) — deferred → frontend-ux** (four-way empty-state signal; lightbox
  fetch-URL-on-open). The lean v1 pass skipped the frontend-ux lens. **Scope note for the next
  pass:** frontend-ux is paying this skipped-lens debt, so it covers the full preview/lightbox +
  order-photos frontend surface — a deliberate exception to the delta fix-diff scope.

All decisions above were re-checked and **upheld by v2 — last affirmed @ `1e7b9d3`** (the commit
v2 audited). Per the README's verification-runbook deferral gate, the next verification pass
re-judges a decision by agent only if `git diff 1e7b9d3..HEAD -- <cited file(s)>` shows the cited
code moved; otherwise it records "unchanged, stands" with no agent.
