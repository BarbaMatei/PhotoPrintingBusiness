---
stage: implement
bolt: 053-order-history-photos
created: 2026-05-29T14:30:00Z
---

## Implementation Walkthrough: Order History Photos

### Summary

Customer-facing read path for the intent-024 archive. The backend gains a thin
`GET /api/orders/{id}/photos` endpoint that returns presigned cloud URLs for the
owned order's viewable photos; the frontend renders a thumbnail grid on the
order-detail page and reuses the existing `PhotoLightboxComponent` for full-size
viewing. No new schema; no new ADRs.

### Structure Overview

Two distinct surfaces wired together by HTTP:

- **Backend** — one new endpoint on the existing `OrdersController`, a new service
  method on `IOrderService` / `OrderService`, and a tiny DTO file. The service
  injects `IStorageRouter` + `IOptions<StorageSettings>` to mint presigned URLs.
- **Frontend** — one new TS model, one new method on `OrderService`, and an
  extended `OrderDetailPage` with a photo-grid section + lightbox overlay. The
  page now fires two HTTP requests in parallel on init (existing detail + new
  photos).

### Completed Work

**Backend:**

- [x] `src/PhotoPrint.API/DTOs/Orders/OrderPhotoDto.cs` — `OrderPhotoDto` (uploadId, fileName, thumbnailUrl, largeUrl) + `OrderPhotosDto` envelope.
- [x] `src/PhotoPrint.API/Services/IOrderService.cs` — added `GetOrderPhotosAsync(orderId, userId, ct)` interface method with NotFound / Forbidden contract.
- [x] `src/PhotoPrint.API/Services/OrderService.cs` — added `IStorageRouter` + `IOptions<StorageSettings>` to constructor; implemented `GetOrderPhotosAsync` with the filter (Cloud + both keys non-null) + presigned URL minting + cloud-tier-off empty-list short-circuit.
- [x] `src/PhotoPrint.API/Controllers/OrdersController.cs` — added `[HttpGet("{id:guid}/photos")]` returning the DTO. Mirrors the existing `GetOrderDetail` auth pattern (`User.GetUserIdOrNull()` → 401 if null).
- [x] `src/PhotoPrint.Tests/Unit/Services/OrderServiceTests.cs` — extended constructor wiring to pass mocked `IStorageRouter` + `StorageSettings`; default cloud-tier-off so existing tests don't accidentally invoke the new path.

**Frontend:**

- [x] `src/PhotoPrint.UI/src/app/core/models/order.model.ts` — added `OrderPhotoDto` + `OrderPhotosDto` interfaces alongside the existing order types.
- [x] `src/PhotoPrint.UI/src/app/core/services/order.service.ts` — added `getOrderPhotos(id): Observable<OrderPhotosDto>` calling `/api/orders/{id}/photos`.
- [x] `src/PhotoPrint.UI/src/app/features/orders/pages/order-detail-page.ts` — imported `PhotoLightboxComponent`; added `photosLoading`, `photos`, `lightboxSrc` signals; loads photos in parallel with the existing order detail (failure → empty list, no navigation); rendered a new `<section class="order-photos">` with thumbnail grid + lightbox overlay; CSS grid + hover/focus styles.
- [x] `src/PhotoPrint.UI/src/app/features/orders/pages/order-detail-page.spec.ts` — added a default `getOrderPhotos` mock so all existing tests still pass; new photo-grid tests come in Stage 3.

### Key Decisions

- **Author the cloud-off branch up front.** `IStorageRouter.Cloud` throws if cloud is disabled — wrapping access with a `CloudEnabled` check returns an empty photos list instead of a 500. Dev runs with `Storage:Provider=Local` see the empty-state UI; prod misconfig is already covered by ADR-008's broader safeguards.
- **Filter requires BOTH preview keys non-null.** A row mid-retention with one key nulled is excluded — the lightbox would otherwise display a thumbnail whose full-size URL is broken. Story 002's "loading and empty states handled" is built on this filter, not on per-click error handling.
- **403 (not 404) for non-owner.** The existing `OrderService.GetOrderDetailAsync` uses `ForbiddenException` for ownership mismatches; I deviated from my Stage-1 plan (which proposed 404 to not leak existence) to keep the codebase consistent. One pattern across all order endpoints beats local optimisation.
- **Parallel HTTP calls on init, not sequenced.** `getOrderDetail` and `getOrderPhotos` fire together; photos can render before the rest of the order if the detail call is slower, but in practice both round-trip in ~similar time. A single combined endpoint was considered (cleaner client) but rejected — would couple bolt-053's photos surface to the existing order-detail DTO that other callers consume.
- **Per-photo `loading="lazy"` on the `<img>`.** Browser-level lazy-load lets the thumbnail grid scroll cheaply on long orders without us shipping an intersection observer.
- **Click-to-open via lightbox `[src]` binding.** When `lightboxSrc()` is null the lightbox renders nothing; when set, the browser only THEN requests the `largeUrl`. Lazy-loading the heavy 2000px asset comes free from the existing lightbox component.
- **CSS grid `auto-fill, minmax(140px, 1fr)`.** Responsive without explicit breakpoints; aspect-ratio 1:1 tiles to match the thumbnail's 300px-max square output from `ImageProcessor.GenerateThumbnailAsync`.

### Deviations from Plan

- **403 vs 404 for non-owner** (see Key Decisions above). Plan proposed 404; codebase convention is 403; I followed convention.
- Otherwise: implementation matches the plan exactly.

### Dependencies Added

None. All packages already in the project (Angular standalone components + existing shared `PhotoLightboxComponent`, `SpinnerComponent` on the FE; `IStorageRouter` from bolt 043, `IOptions<StorageSettings>` from bolt 043, on the BE).

### Developer Notes

- **`OrderItemDto.PreviewUrl` is unchanged.** The line-items section above the new photo grid still shows small thumbnails via the existing `/api/uploads/{id}/preview` endpoint (bolt 042/043 path). The new photo grid is a distinct surface for archive-viewing, not a replacement for line-item display.
- **The photos endpoint never returns the original.** Even for an order still in Paid status (before Shipped triggers original-purge), only `LargePreviewPath` + `ThumbnailPath` are exposed. The original key (`FilePath`) is intentionally not surfaced — it's only used internally for the admin ZIP download.
- **Presigned URL TTL is shared with the existing preview-redirect path** (`StorageSettings:PresignTtlMinutes`, default 60). A future tweak to one applies to both.
- **The empty-state copy is identical for "never had photos" and "expired."** Both legitimately mean "no longer available" from the customer's perspective; distinguishing them would leak retention timing.
- **Frontend tests still pass (395/395) after the spec mock-extension.** New photo-grid behavior tests come in Stage 3.
