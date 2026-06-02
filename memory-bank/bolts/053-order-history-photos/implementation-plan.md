---
stage: plan
bolt: 053-order-history-photos
created: 2026-05-29T14:00:00Z
---

## Implementation Plan: Order History Photos

### Objective

Let a logged-in customer review the photos they ordered. Ship the customer-facing payoff
of intent 024:

- A thin backend read endpoint that returns presigned cloud URLs for an owned order's photos.
- An order-detail UI section that renders a thumbnail grid and opens a full-size lightbox
  on click.

This is the read path that consumes everything bolts 043/051/052 built. **No new schema,
no new domain primitives, no new background jobs.**

### Deliverables

**Backend (story 001):**

- `GET /api/orders/{id}/photos` endpoint on the existing `OrdersController` (already
  `[Authorize]`-gated).
- New `OrderPhotosDto` record: `{ photos: OrderPhotoDto[] }` where `OrderPhotoDto =
  { uploadId, fileName, thumbnailUrl, largeUrl }`.
- New `IOrderService.GetOrderPhotosAsync(orderId, userId, ct)` method.
- DTO file under `DTOs/Orders/`.

**Frontend (story 002):**

- `OrderService.getOrderPhotos(orderId): Observable<{ photos: OrderPhotoDto[] }>`
  + matching TS model in `core/models/order.model.ts`.
- A new "Fotografiile tale" section in `OrderDetailPage` rendering a CSS grid of
  thumbnails. Reuses the existing `PhotoLightboxComponent` for the full-size view.
- Loading state (`<app-spinner>` — reuses the one already on the page).
- Empty / expired state: Romanian copy "Fotografiile pentru această comandă nu mai
  sunt disponibile."
- Click handler: clicking a thumbnail sets `lightboxSrc` signal → opens lightbox; lightbox
  `(close)` clears it.

### Dependencies

- **Bolt 051**: cloud large-preview + thumbnail keys exist on `Upload`. We read
  `LargePreviewPath` + `ThumbnailPath` directly.
- **Bolt 043** (`IStorageRouter` + `IStorageService.GetPresignedUrlAsync`): we call
  `_router.Cloud.GetPresignedUrlAsync(key, ttl, ct)` to mint URLs.
- **Bolt 052** (retention): no direct API dep — but the endpoint's null-key filter is what
  surfaces "no longer available" once retention has expired a photo.
- **Existing infrastructure** (no new code):
  - `OrdersController` — already `[Authorize]`, uses `User.GetUserIdOrNull()`.
  - `IOrderService` — extended with one new method.
  - `PhotoLightboxComponent` (`shared/components/photo-lightbox/`) — `[src]` input,
    `(close)` output, Escape-key dismiss. Drop-in.
  - `SpinnerComponent` — already imported on `OrderDetailPage`.

### Technical Approach

**Endpoint algorithm** (`IOrderService.GetOrderPhotosAsync`):

```text
1. Load order with .Include(o => o.Items).ThenInclude(i => i.Upload).
   If null OR order.UserId != userId → throw NotFoundException
   (404 rather than 403 — don't leak existence to non-owners).

2. Resolve TTL from StorageSettings.PresignTtlMinutes (default 60).

3. For each item in order.Items (.Select(i => i.Upload).Distinct()):
   - Skip if u.StorageLocation != Cloud (still Local — pre-promotion).
   - Skip if u.LargePreviewPath is null OR u.ThumbnailPath is null
     (expired by retention OR never had a thumb — both = "not viewable").
   - Generate presigned URLs:
       thumbUrl   = _router.Cloud.GetPresignedUrlAsync(u.ThumbnailPath, ttl, ct)
       largeUrl   = _router.Cloud.GetPresignedUrlAsync(u.LargePreviewPath, ttl, ct)

4. Return OrderPhotosDto { Photos = [...] }.

Edge cases:
- Cloud tier off (Storage:Provider=Local in dev): IStorageRouter.Cloud throws on access.
  Wrap with a CloudEnabled check; if false, return empty photos list. Dev runs show empty
  archive; prod misconfig with this state is already covered by other ADR-008 safeguards.
- Order exists but has zero promoted uploads (still pre-Paid): empty photos list.
  The UI's empty-state copy covers it.
```

**Frontend integration:**

- The `OrderDetailPage` already loads `OrderDetailDto` in `ngOnInit`. After it loads, also
  call `getOrderPhotos(id)` and store the result in a `photos` signal. Two parallel HTTP
  calls (the existing one + the new one) — both fire as the page initialises.
- The photo grid renders inside a new `<section class="order-photos">` placed AFTER the
  cost summary and BEFORE the delivery info. Order: most-relevant-to-user-history first.
- Grid: CSS grid, `auto-fill, minmax(140px, 1fr)`. Each cell is a clickable thumbnail
  loaded directly from the presigned `thumbnailUrl` (no API roundtrip per thumbnail).
- Click → `lightboxSrc.set(photo.largeUrl)`; component renders `<app-photo-lightbox
  [src]="lightboxSrc()" (close)="lightboxSrc.set(null)" />`.
- Lazy-load semantics (per story 002): thumbnails load eagerly (the page shows them);
  large previews fetch ON DEMAND because the browser only requests the `largeUrl` when
  the lightbox `[src]` is set. The 1 h TTL covers a typical viewing session.
- Empty state: when `photos().length === 0`, render the "no longer available" message.
  Distinguished from the loading state via a separate signal.
- No new shared components needed. Lightbox + spinner are reused.

**Authorization shape:**

- Mirror the existing `OrdersController.GetOrderDetail`: `User.GetUserIdOrNull()` → if
  null, 401; otherwise pass the user id into the service. The service does the ownership
  check and throws `NotFoundException` on mismatch (translated to 404 by the global
  exception handler). 403 vs 404: we choose 404 to avoid leaking order existence to
  non-owners, matching `OrderService.GetOrderDetailAsync`'s established pattern.
- **No guest tokenized access** in this bolt (explicitly deferred per the unit brief).
  The `[Authorize]` attribute on the controller blocks unauthenticated calls; guest
  session users — who don't have a `UserId` — get 401. This matches the unit-brief
  "Registered + claimed guest orders only" with the **claimed** part deferred to a
  future bolt (guest claim flow would set `Order.UserId` on claim, at which point this
  endpoint naturally starts working for them).

### Acceptance Criteria

**Story 001 — endpoint:**

- [ ] `GET /api/orders/{id}/photos` returns 200 with `{ photos: [...] }` for the order owner.
- [ ] Each photo entry has `uploadId`, `fileName`, `thumbnailUrl`, `largeUrl` — the URLs
      are presigned (contain `Signature=` or `X-Amz-Signature=` query parameters).
- [ ] Presigned URL TTL is 1 hour (configurable via `StorageSettings:PresignTtlMinutes`).
- [ ] Non-owner gets 404 (existence not leaked); unauthenticated gets 401.
- [ ] Uploads still Local (pre-promotion) are omitted, not errored.
- [ ] Uploads whose blobs expired (LargePreviewPath / ThumbnailPath null) are omitted.
- [ ] Cloud-tier-off deployment returns empty photos list, not 500.

**Story 002 — UI:**

- [ ] `OrderDetailPage` renders a thumbnail grid populated from the new endpoint.
- [ ] Clicking a thumbnail opens the existing `PhotoLightboxComponent` showing the
      `largeUrl`. Escape key + backdrop click both close it.
- [ ] Loading state shows the existing spinner while the photos fetch is in flight.
- [ ] Empty / expired state: Romanian copy "Fotografiile pentru această comandă nu mai
      sunt disponibile." Distinguished from loading.
- [ ] Lazy-load: the large preview for each photo is fetched by the browser only when
      the user clicks (lightbox `[src]` mounted on demand).
- [ ] No regressions in existing `OrderDetailPage` behavior (line items, status stepper,
      cost summary, delivery info all unchanged).

### Out of Scope (Confirmed)

- Backend schema changes (none — all columns already exist from bolts 051/052).
- Guest tokenized access (explicitly deferred per unit brief).
- Re-ordering / re-printing from history (out of scope per story 002).
- Replacing the existing `OrderItemDto.PreviewUrl` (the line-items section keeps its
  small thumbnail; the new section is dedicated to full-archive viewing).

### Risks

- **Cloud-tier-off in dev** would 500 if we naively call `_router.Cloud`. Mitigation:
  check `_router.CloudEnabled` first; return empty list otherwise.
- **`LargePreviewPath` null but `ThumbnailPath` non-null** (or vice versa — half-expired
  state during a retention sweep mid-tick). Filter requires BOTH non-null to avoid
  rendering a thumbnail whose lightbox URL is broken.
- **Two HTTP calls on the order-detail page** (existing detail + new photos). Acceptable —
  they run in parallel, the page already has a loading state. If perceived sluggishness
  emerges in testing, we can fold both into a single detail-with-photos endpoint; not
  worth the coupling pre-emptively.
