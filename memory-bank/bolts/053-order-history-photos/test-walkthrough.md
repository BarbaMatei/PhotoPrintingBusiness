---
stage: test
bolt: 053-order-history-photos
created: 2026-05-29T14:55:00Z
---

## Test Report: Order History Photos

### Summary

| Surface | Passed | Failed | Skipped | Total |
|---------|-------:|-------:|--------:|------:|
| Backend (new to bolt 053) | 8 | 0 | 0 | 8 |
| Frontend (new to bolt 053) | 7 | 0 | 0 | 7 |
| **Full backend suite** | **591** | **0** | **7** (CI-gated MinIO) | **598** |
| **Full UI suite** | **402** | **0** | **0** | **402** |

`dotnet test PhotoPrint.sln --no-build -c Release` — 16 s.
`ng test --watch=false` — 10 s.

Bolt 052 left the project at 590 backend tests + 395 UI tests. Bolt 053 adds **15 new
tests** (8 backend + 7 UI) without churning any existing test — only one mock extension
was needed (the pre-existing `order-detail-page.spec.ts` learned about `getOrderPhotos`
during Stage 2).

### Test Files

**Backend:**

- [x] `src/PhotoPrint.Tests/Unit/Services/OrderServiceTests.cs` *(extended)* — 8 new tests under "Bolt 053: GetOrderPhotosAsync". A `CreateSutWithCloud` helper builds an SUT with the cloud tier enabled (the existing `_service` field has cloud off so legacy tests stay clean); a `SeedPaidOrderWithPromotedUploadAsync` helper produces a paid Cloud-located upload with both blob keys set, parameterised so individual tests can flip one field at a time.

**Frontend:**

- [x] `src/PhotoPrint.UI/src/app/features/orders/pages/order-detail-page.spec.ts` *(extended)* — 7 new tests under "Bolt 053: photo archive grid + lightbox". Drives the DOM via standard Angular component-fixture interactions.

### Acceptance Criteria Validation

**Story 001 — endpoint:**

- ✅ `GET /api/orders/{id}/photos` returns 200 with `{ photos: [...] }` for the order owner — `GetOrderPhotosAsync_HappyPath_ReturnsPresignedUrlsForEachPhoto`.
- ✅ Each photo entry has `uploadId`, `fileName`, `thumbnailUrl`, `largeUrl` — asserted explicitly in the happy-path test.
- ✅ Presigned URL TTL is configurable via `StorageSettings:PresignTtlMinutes` — `GetOrderPhotosAsync_PresignTtl_MatchesConfiguredMinutes` verifies a 90-minute config flows through to `Cloud.GetPresignedUrlAsync(_, TimeSpan.FromMinutes(90), _)`.
- ✅ Non-owner gets 403 (chose 403 over 404 — matches existing `OrderService.GetOrderDetailAsync` convention; documented in implementation walkthrough). `GetOrderPhotosAsync_NonOwner_ThrowsForbiddenException`.
- ✅ Order-not-found → 404 — `GetOrderPhotosAsync_OrderNotFound_ThrowsNotFoundException`.
- ✅ Uploads still Local omitted, not errored — `GetOrderPhotosAsync_LocalUpload_ExcludedFromResults` (also verifies zero presign calls).
- ✅ Uploads whose blobs expired omitted — `GetOrderPhotosAsync_LargePreviewPathNull_ExcludedFromResults` + `GetOrderPhotosAsync_ThumbnailPathNull_ExcludedFromResults` (both half-expired states filtered).
- ✅ Cloud-tier-off returns empty list (not 500) — `GetOrderPhotosAsync_CloudTierOff_ReturnsEmptyPhotos`.

**Story 002 — UI:**

- ✅ Thumbnail grid renders from the endpoint — `renders a thumbnail tile per photo returned by the endpoint`.
- ✅ Clicking a thumbnail opens the lightbox with the `largeUrl` — `opens the lightbox with the largeUrl when a thumbnail is clicked`.
- ✅ Lightbox dismissible — `closes the lightbox when its close event fires` (clicks the backdrop).
- ✅ Loading + empty / expired state — `shows the "no longer available" copy when the photos endpoint returns empty` checks the Romanian message renders.
- ✅ Lazy-load semantics — `uses native lazy-loading on thumbnail images` (`loading="lazy"` attr) + `does not render the lightbox until a thumbnail is clicked` (lightbox `[src]` is null until click, so the large URL isn't requested).
- ✅ Photos failure doesn't navigate away — `silently empties the photos list when the photos call fails (no navigation)`. Critical: a 500 on the photos call must NOT eject the customer from their order-detail page.

### Test Patterns Used

- **`MockBehavior.Strict` on `IStorageService` (cloud)** — any unexpected presign call fails the test. The "Local upload excluded" + "half-expired excluded" tests rely on this to assert zero presigning happened.
- **Deterministic mock URL** — `https://cdn.test/{key}?sig=test&ttl={minutes}` — lets a single test (`PresignTtl_MatchesConfiguredMinutes`) prove the TTL flowed through without reaching into `Moq.Verify(It.Is<TimeSpan>(...))` for every TTL-related assertion.
- **Component-fixture DOM probing** — query for `.photo-tile`, `.lightbox__backdrop`, `.lightbox__img`. Treats the lightbox as a black box (we don't import its internals); a future swap to a different lightbox component would only require updating the selectors.
- **Per-test `setup({ getOrderPhotos: vi.fn() })` overrides** — the default mock returns empty photos, so individual tests opt in to a populated grid by overriding.

### Issues Found

None. All paths through the new endpoint and the new UI surface are exercised by at least
one test; cross-cutting behaviour (cloud-off, half-expired states, owner mismatch) is
covered explicitly rather than left to integration assumption.

### Notes

1. **No CI-gated MinIO integration test for this endpoint** — out of scope for this bolt
   (same reasoning as 051/052 deferred follow-ups). The happy path is exercised end-to-end
   by the existing presigned-URL tests on `S3StorageServiceIntegrationTests`; the photos
   endpoint just composes them.
2. **Browser-level lazy-load isn't observable in jsdom** — the `loading="lazy"` attribute
   is verified, but actual deferred image fetching depends on the browser's intersection
   observer (jsdom doesn't simulate scrolling). The attribute presence is the meaningful
   signal — if it's there, every modern browser does the right thing.
3. **No production smoke needed for the UI** — Angular AOT compilation + the `ng test`
   suite covers template binding errors at build time. A real-browser smoke after deploy
   is still recommended but not a bolt blocker.
