---
type: resolution
target: 042-thumbnail-cache
answers_review: review-v2.md
version: 2
branch: feat/bolt-042-thumbnail-cache
status: resolved
verified_in: review-v3.md
fixed_commit: e3a77d9
opened: 2026-07-14
closed: 2026-07-14
findings:
  NEW-1: { status: fixed, commit: 656c2fd, note: "raised decode cap 50 MP -> 100 MP per owner decision (accept large-format prints/A1@300DPI + high-res originals); 100 MP decode ~400 MB stays under the unchanged 512 MB allocator; 625 MP+ bombs still rejected. Updated ExceedsDecodeLimits boundary test (100 MP) + real-image oversized test (110 MP) + story 003 AC." }
  NEW-2: { status: fixed, commit: 5712aad, note: "fetchPreviewWithRetry now drops a restored upload ONLY on a definitive 404; 5xx/network (and a still-401 after the one retry) keep it visible so a later refresh retries. Added a transient-error regression test." }
  NEW-3: { status: deferred, commit: null, note: "Latent cleanup-vs-preview TOCTOU on a non-transactional file+DB model; complete fix is an orphan-sweep job (or a storage transaction), not a partial narrowing. Sub-second window, one small file. Tracked for the bolt-043 storage-lifecycle work. See decisions." }
  NEW-4: { status: fixed, commit: e3a77d9, note: "LocalStorageService returns OS-independent '/'-separated keys (ToFullPath maps to OS paths for filesystem ops); a Windows-written key now reads on Linux and maps to a cloud object key. Added LocalStorageServiceTests (real service, none existed): key format + save/exists/get/delete round-trip." }
---

# Resolution — Bolt 042: Thumbnail Cache (answers review-v2)

Fixer's response to the four **new follow-ups** raised by the verification pass
[review-v2.md](review-v2.md). (The v1 findings are verified in review-v2 and recorded in
[resolution-v1.md](resolution-v1.md).) **3 fixed, 1 deferred.** Both suites green:

- **.NET:** `dotnet test` → **515 passed / 0 failed** (was 511; +4 `LocalStorageServiceTests`).
- **Frontend:** `ng test` → **403 passed / 0 failed** (was 402; +1 transient-error test).

Each behavioral fix is proven **fail-before / pass-after** (revert → red):

| Finding | Reverted to | Test | Result |
|---------|-------------|------|--------|
| NEW-1 | 50 MP cap | `ImageProcessorTests.ExceedsDecodeLimits_AtCapAllowed_…` (100 MP boundary) | **RED** ✓ |
| NEW-2 | drop-on-any-error | `format-selector-page.spec … keeps a restored entry on a transient error` | **RED** ✓ |
| NEW-4 | `Path.Combine` key | `LocalStorageServiceTests.SaveAsync_WithPrefix_ReturnsForwardSlashKey` | **RED** ✓ |

| ID | Sev | Status | Summary | Fix commit |
|----|-----|--------|---------|-----------|
| NEW-1 | 🟠 | fixed | Decode cap 50 → 100 MP (accept large-format prints); allocator unchanged | 656c2fd |
| NEW-2 | 🟡 | fixed | Restored upload dropped only on 404; transient errors keep it | 5712aad |
| NEW-4 | 🟡 | fixed | OS-independent `/`-separated storage keys + real-service tests | e3a77d9 |
| NEW-3 | 🟡 | deferred | Latent cleanup/preview race → bolt-043 orphan sweep | — |

## Decisions / rationale

- **NEW-1 — cap set to 100 MP (owner decision).** The 50 MP cap rejected legitimate
  large-format prints (A1 @ 300 DPI ≈ 70 MP) and high-res camera/phone originals that the
  old per-axis check accepted. Owner chose ~100 MP: it accepts those while a 100 MP decode
  (~400 MB RGBA) stays under the unchanged 512 MB allocator backstop, and 625 MP+ bombs are
  still rejected at Identify. Tunable via `ImageProcessor.MaxDecodePixels` (raise the
  allocator too if going materially higher).
- **NEW-3 — deferred (not partially patched).** This is a genuine TOCTOU between the
  preview (writes a thumbnail file + `ThumbnailPath`) and the cleanup job (reads candidates,
  deletes files, soft-deletes) over a **non-transactional file+DB** boundary. Any
  read-then-delete narrowing (reload, re-query) shrinks the window but cannot close it,
  because the preview may write *after* cleanup's delete attempt; and a partial fix isn't
  cleanly regression-testable (the race can't be interleaved inside the single `CleanupAsync`
  call in a unit test). The complete, honest fix is a **periodic orphan sweep** (scan storage
  for keys with no live DB reference) — a feature that belongs with the bolt-043 cloud-storage
  work, where the storage lifecycle is redesigned and the same sweep covers the cloud
  provider. Impact today is a single small orphaned file in a sub-second window for a 24h+-old
  unreferenced upload previewed at the instant cleanup runs. Re-affirm or reprioritise in v3.

## Notes for the re-reviewer

- **Self-reviewed the diff:** NEW-1 only changes a constant + its tests (no logic change; the
  `long`-multiply overflow guard is unchanged). NEW-2 narrows a drop condition (404-only) and
  keeps the FE-4 401-retry path intact. NEW-4 changes only the key *separator* (Guid/owner
  format preserved), and the integration suite (real UploadService + FakeStorage, which was
  already `/`-based) stays green, confirming the round-trip.
## Verification (review-v3)

Verified in [review-v3.md](review-v3.md) — revert→red on NEW-1/NEW-2/NEW-4 + an independent
verifier. **NEW-1/NEW-2/NEW-4 → verified; NEW-3 deferral accepted (sound); 0 reopened.** The
`verified` verdicts live in review-v3 (this resolution keeps the fixer's `fixed`/`deferred`).
Two comment drifts the fixes introduced were caught by v3 and fixed in-pass (`f8b1325`). A
deploy-time note was recorded (decode concurrency limit for concurrent large previews).
