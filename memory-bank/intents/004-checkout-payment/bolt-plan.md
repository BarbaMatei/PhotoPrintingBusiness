---
intent: 004-checkout-payment
phase: inception
status: inception-complete
created: 2026-05-21T10:00:00Z
updated: 2026-05-21T10:00:00Z
---

# Bolt Plan: 004-checkout-payment

## Overview

This intent delivers 6 bolts (012–017) across 5 units. The bolts follow a layered dependency order: upload backend → cart backend → upload UI → shipping + order model → payment backends → checkout UI. This order guarantees each bolt can be developed and tested independently before the next layer begins.

```
012-photo-upload-backend
    └→ 013-cart-api
            └→ 014-upload-format-cart-ui
            └→ 015-shipping-and-order-core
                    └→ 016-payment-backends
                                └→ 017-checkout-ui
                    └→ (014 also feeds 017)
```

---

## Bolt 012 — photo-upload-backend

```yaml
bolt: "012"
name: photo-upload-backend
intent: 004-checkout-payment
unit: 001-upload-and-cart-backend
type: ddd
status: not-started
stories:
  - 001-upload-entity-schema
  - 002-upload-endpoint
  - 003-upload-preview-and-cleanup
epic_stories:
  - US-202
depends_on:
  - "005"  # auth-core (JWT + guest token)
  - "007"  # guest-sessions
enables:
  - "013"  # cart-api
```

### What This Bolt Builds

**Domain model**:
- `Upload` entity: `Id (UUID)`, `UserId?`, `GuestSessionId?`, `FilePath`, `OriginalFileName`, `WidthPx`, `HeightPx`, `FileSizeBytes`, `ContentType`, `UploadedAt (DateTimeOffset)`, `DeletedAt? (DateTimeOffset)` (soft delete)
- EF Core migration: `Uploads` table + index on `(UserId, DeletedAt)` and `(GuestSessionId, DeletedAt)`

**Services & abstractions**:
- `IStorageService` — interface: `SaveAsync(stream, fileName, contentType) → string path`, `DeleteAsync(path)`, `GetStreamAsync(path) → Stream`
- `LocalStorageService` — saves to `wwwroot/uploads/{userId|guestId}/{uuid}.{ext}`; path returned for DB storage
- `IMimeValidator` — reads first 12 bytes; validates JPEG (`FF D8 FF`), PNG (`89 50 4E 47 0D 0A 1A 0A`), HEIC (`ftyp` box at offset 4)

**Endpoints**:
- `POST /api/uploads` (multipart/form-data; Bearer JWT or X-Guest-Token)
  - Validates MIME by magic bytes → 415 if not image
  - Validates file size ≤ 50 MB → 413 if exceeded
  - Validates session upload count ≤ 30 → 429 if exceeded
  - Uses `SixLabors.ImageSharp` to extract `WidthPx`, `HeightPx` without full decode
  - Saves file via `IStorageService`
  - Persists `Upload` record
  - Returns `UploadDto[]`: `{ uploadId, widthPx, heightPx, fileSizeBytes, previewUrl }`
- `GET /api/uploads/{id}/preview` (public with upload ownership check)
  - Serves resized JPEG (max 300px dimension) via `ImageSharp.Resize`
  - Cache-Control: max-age=3600 + ETag
  - Returns 404 for unknown or soft-deleted uploads

**Background job**:
- `UploadCleanupJob` (IHostedService + timer, runs hourly)
  - Soft-deletes `Upload` records where `UploadedAt < UtcNow - 24h` AND `Id NOT IN (SELECT UploadId FROM OrderItems)`
  - Deletes physical files via `IStorageService.DeleteAsync` for soft-deleted records
  - Logs count of cleaned-up files with Serilog

**Security hardening**:
- Original filename stored in DB (`OriginalFileName`) for audit but NEVER used in storage path
- Uploads served via API controller, never directly from `wwwroot` (avoids directory listing)
- `Content-Disposition: inline` on preview; `Content-Disposition: attachment` on full download

**Tests**:
- Unit: MIME validation — valid JPEG/PNG/HEIC pass; EXE/PDF reject with 415
- Unit: Size enforcement — 49.9 MB passes; 50.1 MB rejects with 413
- Unit: Session count enforcement — 29th upload passes; 31st rejects with 429
- Unit: Cleanup job skips uploads with OrderItem reference
- Unit: Cleanup job deletes uploads older than 24h without OrderItem
- Integration: POST /api/uploads with valid 1×1 JPEG → 200 + UploadDto
- Integration: POST /api/uploads with renamed EXE → 415

---

## Bolt 013 — cart-api

```yaml
bolt: "013"
name: cart-api
intent: 004-checkout-payment
unit: 001-upload-and-cart-backend
type: ddd
status: not-started
stories:
  - 004-cart-item-entity
  - 005-cart-crud-endpoints
  - 006-cart-merge-endpoint
epic_stories:
  - US-206
depends_on:
  - "012"  # photo-upload-backend (Uploads table must exist)
  - "009"  # product-catalog-core (Products table for price lookup)
  - "005"  # auth-core
  - "007"  # guest-sessions
enables:
  - "014"  # upload-format-cart-ui
  - "015"  # shipping-and-order-core
```

### What This Bolt Builds

**Domain model**:
- `CartItem` entity: `Id (UUID)`, `UserId?`, `GuestSessionId?`, `UploadId → Uploads`, `ProductId → Products`, `Quantity (int, 1–100)`, `AddedAt (DateTimeOffset)`
- EF Core migration: `CartItems` table + unique composite index on `(UserId, UploadId)` and `(GuestSessionId, UploadId)` (prevent duplicate cart entries)
- Navigation: `CartItem.Upload`, `CartItem.Product` (with price tier data)

**Service**:
- `ICartService` — `GetCartAsync(userId|guestId) → CartResponseDto`, `SetCartAsync(userId|guestId, request) → CartResponseDto`, `ClearCartAsync(userId|guestId)`, `MergeCartsAsync(userId, guestCart) → CartResponseDto`
- `CartService` implementation — replace strategy on `SetCart`: delete-all + insert-new within a `DbContext.Database.BeginTransactionAsync()` block

**Endpoints**:
- `POST /api/cart` (Bearer JWT or X-Guest-Token)
  - Body: `{ productId, items: [{ uploadId, quantity }] }`
  - Validates: `productId` is active; each `uploadId` belongs to the calling user/session; `quantity` 1–100
  - Atomically replaces all cart items
  - Returns `CartResponseDto` with computed totals
- `GET /api/cart` (Bearer JWT or X-Guest-Token)
  - Returns `{ productId, productName, items: [...], subtotal, itemCount }`
  - Joins `CartItems → Uploads` (for `previewUrl`) and `CartItems → Products` (for `unitPrice` at quantity)
  - Returns `{ items: [], subtotal: 0, itemCount: 0 }` for empty cart (never 404)
- `DELETE /api/cart` (Bearer JWT or X-Guest-Token)
  - Removes all cart items for user/session
  - Returns `204 No Content`
- `POST /api/cart/merge` (Bearer JWT required)
  - Body: `{ guestCart: { productId, items: [...] } }`
  - Merges guest items: if `uploadId` conflict, keep user's existing item; transfer guest uploads to user
  - Entire operation is transactional
  - Returns merged `CartResponseDto`

**DTOs**:
- `CartRequest`: `{ productId: Guid, items: CartItemRequest[] }`
- `CartItemRequest`: `{ uploadId: Guid, quantity: int }`
- `CartResponseDto`: `{ productId, productName, finishName, items: CartItemDto[], subtotal: decimal, itemCount: int }`
- `CartItemDto`: `{ uploadId, quantity, previewUrl, unitPrice, lineTotal, widthPx, heightPx }`

**Tests**:
- Unit: SetCart replaces all existing items (verify delete + insert count)
- Unit: GetCart computes `lineTotal = unitPrice × quantity` correctly
- Unit: Merge — conflict resolution (user item wins)
- Unit: Validation rejects `uploadId` belonging to a different user (403)
- Unit: Validation rejects `quantity` of 0 or 101 (422)
- Integration: Full cart lifecycle — POST → GET → DELETE → GET (empty)
- Integration: Merge guest cart into user cart

---

## Bolt 014 — upload-format-cart-ui

```yaml
bolt: "014"
name: upload-format-cart-ui
intent: 004-checkout-payment
unit: 002-upload-format-cart-ui
type: simple
status: not-started
stories:
  - 001-upload-page
  - 002-format-finish-selector
  - 003-order-summary-panel
  - 004-cart-page
  - 005-cart-service
epic_stories:
  - US-201
  - US-203
  - US-205
depends_on:
  - "013"  # cart-api (POST /api/cart, GET /api/cart)
  - "011"  # product-catalog-ui (product models, ProductService)
  - "012"  # photo-upload-backend (POST /api/uploads)
  - "004"  # angular-app-shell (route guards, interceptors)
enables:
  - "017"  # checkout-ui (cart page feeds checkout)
```

### What This Bolt Builds

**Feature module**: `src/app/features/upload/`

**Upload page** (`/upload`):
- `PhotoUploadComponent` — drag-and-drop zone using `@angular/cdk/drag-drop` or custom `HostListener('drop')`; accepts `.jpg/.jpeg/.png/.heic`; client-side validation (MIME ext + file.size); rejects with toast on failure
- `PhotoThumbnailComponent` — displays preview, filename (≤20 chars), resolution badge, quality badge (Green/Yellow/Red vs. selected product's `minWidthPx`/`minHeightPx`), remove button
- HEIC support: `heic2any` library for browser-side HEIC → JPEG conversion for thumbnail display (original file still uploaded as HEIC)
- Per-file `HttpClient` with `reportProgress: true`; `upload$: Observable<UploadProgress>` exposed in `UploadService`
- Progress bars via `HttpEventType.UploadProgress`
- State: `uploads: UploadState[]` where `UploadState = { file, progress, uploadDto?, error? }`

**Format & finish selector**:
- `FormatSelectorComponent` — segmented button group: `10×15`, `13×18`, `15×21`; on change → calls `ProductService.getActive()` → finds matching product → emits `selectedProduct$`
- `FinishToggleComponent` — toggle between `Lucios` / `Mat`
- Quality badge recalculation: subscribes to `selectedProduct$`; for each uploaded photo, compares `widthPx × heightPx` against product's `minWidthPx`/`minHeightPx` and `optWidthPx`/`optHeightPx`; badges update synchronously
- Per-photo quantity stepper: `QuantityStepperComponent` (min 1, max 100, debounceTime 300ms on value changes)
- Resolution guide: shown below selector `10×15 → min 1200×1800px`

**Order summary panel**:
- `OrderSummaryComponent` — sticky panel (right sidebar on ≥992px, bottom drawer on mobile)
- Live subtotal: `totalPrice$ = combineLatest([uploads$, quantities$, selectedProduct$]).pipe(map(calcTotal))`
- `Adaugă în coș` button: disabled when `uploads$.length === 0`; on click → `CartService.setCart({ productId, items })` → navigate to `/cos`

**Cart page** (`/cos`):
- `CartComponent` — lists items with thumbnail (from `previewUrl`), format name, finish, `QuantityStepperComponent`, `unitPrice | currency:'RON'`, `lineTotal`, remove button
- Read-only format/finish banner at top
- `Subtotal`, shipping placeholder (`Calculat la pasul următor`), grand total
- Empty cart state: illustration + `Adaugă fotografii` button → `/upload`
- `Continuă cumpărăturile` → `/upload`; `Finalizează comanda` → `/checkout` (triggers auth guard)
- Cart nav badge: `CartService.itemCount$` subscribed in `HeaderComponent`

**CartService**:
- `BehaviorSubject<CartState>` as source of truth
- Guest: persist to localStorage (`'fotoTipar_cart'` key) + sync to `POST /api/cart` on change (debounce 1s)
- Logged-in: read from `GET /api/cart` on init; write via `POST /api/cart` on change
- `mergeOnLogin(guestCart)` → calls `POST /api/cart/merge`; clears localStorage
- `itemCount$: Observable<number>` = `cart$.pipe(map(c => c.itemCount))`
- On 401: clear cart state without error (user logged out)

**Models**:
- `src/app/core/models/upload.model.ts` — `UploadDto`, `UploadState`, `UploadProgress`
- `src/app/core/models/cart.model.ts` — `CartState`, `CartItemDto`, `CartRequest`

**Tests**:
- Unit: `PhotoUploadComponent` — rejects `.pdf` file; rejects 51 MB file; rejects 31st file
- Unit: Quality badge calculation — image below min → Red; between min/opt → Yellow; above opt → Green
- Unit: Quality badge updates when format changes
- Unit: `CartService.setCart` updates `itemCount$`
- Unit: `CartService` persists to localStorage for guest session
- Unit: `CartService.mergeOnLogin` calls `/api/cart/merge` and clears localStorage
- E2E: drag 3 photos → format selected → add to cart → navigate to `/cos` → quantity changed → proceed to checkout

---

## Bolt 015 — shipping-and-order-core

```yaml
bolt: "015"
name: shipping-and-order-core
intent: 004-checkout-payment
unit: 003-shipping-and-order-core
type: ddd
status: not-started
stories:
  - 001-easybox-locker-catalog
  - 002-shipping-endpoints
  - 003-order-entity-schema
  - 004-order-status-machine
epic_stories:
  - US-302
depends_on:
  - "013"  # cart-api (CartItems needed for OrderItems derivation)
  - "005"  # auth-core
enables:
  - "016"  # payment-backends (Order entity required)
```

### What This Bolt Builds

**EasyboxLocker domain**:
- `EasyboxLocker` entity: `Id (UUID)`, `SamedayId (string)`, `Name`, `Address`, `City`, `County`, `Lat (double)`, `Lng (double)`, `IsActive (bool)`
- EF Core migration: `EasyboxLockers` table + index on `City` (for city-filter query)
- Seed data migration: ~200 representative Romanian Sameday Easybox locations (major cities: București, Cluj-Napoca, Timișoara, Iași, Brașov, Constanța, Galați, Craiova, Ploiești, Oradea + others); data as hard-coded C# list in migration `Up()` method
- Note: Phase 2 will replace seed with live Sameday API sync; no seed data removal needed (Phase 2 merges)

**IShippingService abstraction**:
- `IShippingService`: `GetLockersAsync(city) → Task<IEnumerable<LockerDto>>`, `GetShippingCostAsync(type) → Task<ShippingCostDto>`, `GenerateAwbAsync(orderId) → Task<AwbResultDto>`
- `StaticShippingService` (Phase 1): `GetLockers` → DB query; `GetCost` → reads from `IConfiguration["Shipping:EasyboxCostRon"]`; `GenerateAwb` → returns `{ manual: true }`
- Register as `services.AddScoped<IShippingService, StaticShippingService>()`

**Shipping endpoints**:
- `GET /api/shipping/lockers?city={query}` (public, no auth)
  - Case-insensitive `EF.Functions.ILike` query on `City` column
  - Returns `LockerDto[]`: `{ id, samedayId, name, address, city, lat, lng }`
  - Returns empty array (not 404) if no lockers found for city
- `GET /api/shipping/cost?type=Easybox|Courier` (public, no auth)
  - Returns `{ costRon: decimal }`
  - 400 if `type` is not `Easybox` or `Courier`
- `POST /api/shipping/awb` (Admin JWT required)
  - Body: `{ orderId }`
  - Phase 1: returns `{ manual: true, message: "AWB se generează manual în portalul Sameday" }`
  - Phase 2: returns `{ awbNumber, trackingUrl }`

**Order entity domain**:
- `Order` entity:
  - `Id (UUID)`, `OrderNumber (string, unique)`, `UserId?`, `GuestSessionId?`
  - `Status (OrderStatus enum)`, `PaymentProcessor (PaymentProcessor enum)`, `PaymentIntentId? (string)`, `the legacy processorTransactionId? (string)`
  - `ShippingAddress (JSONB)` — `ShippingAddressDto` serialized: `{ street, number, block?, city, county, postalCode, recipientName, phone }`
  - `DeliveryType (DeliveryType enum: Easybox | Courier)`, `EasyboxLockerId? → EasyboxLockers`
  - `ShippingCostRon (decimal)`, `SubtotalRon (decimal)`, `TotalRon (decimal)`
  - `AwbNumber?`, `TrackingUrl?`
  - `CreatedAt`, `UpdatedAt?`, `PaidAt?`
- `OrderItem` entity:
  - `Id (UUID)`, `OrderId → Orders`, `UploadId → Uploads`, `ProductId → Products`
  - `Quantity (int)`, `UnitPriceRon (decimal)` (snapshot at order time), `LineTotalRon (decimal)`
  - `ProductSnapshot (JSONB)` — `{ productName, size, finish }` at order time
- Enums: `OrderStatus { AwaitingPayment, Paid, Printing, Shipped, Delivered, PaymentFailed, Cancelled }`, `PaymentProcessor { Stripe, the legacy processor }`, `DeliveryType { Easybox, Courier }`
- EF Core migration: `Orders` table + `OrderItems` table; unique index on `OrderNumber`; composite index on `(Status, CreatedAt)`

**Order number generation**:
- `IOrderNumberService` — `GenerateAsync() → string`
- `OrderNumberService` — uses a PostgreSQL sequence `order_number_seq_{year}` (created if not exists); formats as `FT-{YYYY}{seq:D4}` (e.g. `FT-20260001`)
- Sequence is reset per calendar year via a check on `CreatedAt.Year != currentYear` (handled in service)

**OrderStatus state machine**:
- `OrderStatusMachine` (static class):
  - Valid transitions: `AwaitingPayment → Paid`, `AwaitingPayment → PaymentFailed`, `Paid → Printing`, `Printing → Shipped`, `Printing → Cancelled`, `Shipped → Delivered`
  - `Transition(OrderStatus from, OrderStatus to)` — throws `InvalidOrderTransitionException` if invalid
  - `InvalidOrderTransitionException` caught by global exception handler → 400 ProblemDetails
- `CanTransition(from, to) → bool` exposed for guard usage

**Configuration** (`appsettings.json`):
```json
{
  "Shipping": {
    "EasyboxCostRon": 20.00,
    "CourierCostRon": 25.00
  }
}
```

**Tests**:
- Unit: Locker search — `Cluj` returns Cluj lockers; empty string returns empty array
- Unit: Shipping cost — Easybox returns 20.00; Courier returns 25.00; unknown type 400
- Unit: OrderStatusMachine — all valid transitions succeed; invalid transition throws
- Unit: Order number format — `FT-20260001` format enforced; zero-padding correct
- Integration: GET /api/shipping/lockers?city=București returns seeded data
- Integration: GET /api/shipping/cost?type=Easybox returns configured value

---

## Bolt 016 — payment-backends

```yaml
bolt: "016"
name: payment-backends
intent: 004-checkout-payment
unit: 004-payment-backends
type: ddd
status: not-started
stories:
  - 001-order-service
  - 002-stripe-payment-intent
  - 003-stripe-webhook-handler
  - 004-legacy-processor-initiate
  - 005-legacy-processor-ipn-handler
epic_stories:
  - US-305
  - US-306
depends_on:
  - "015"  # shipping-and-order-core (Order entity, OrderStatusMachine)
  - "013"  # cart-api (CartService for building OrderItems)
  - "003"  # email-infrastructure (IEmailService for order confirmed)
enables:
  - "017"  # checkout-ui
```

### What This Bolt Builds

**IOrderService**:
- `IOrderService`: `CreateFromCartAsync(userId|guestId, shippingDetails, processor) → Order`, `GetByPaymentIntentIdAsync(paymentIntentId) → Order?`, `GetByIdAsync(orderId) → Order?`
- `OrderService` — `CreateFromCart`: fetches cart, validates non-empty, creates `Order` + `OrderItems` (quantity × unitPrice snapshot from Products pricing tiers), generates `OrderNumber`, sets `Status = AwaitingPayment`

**Stripe integration** (NuGet: `Stripe.net`):
- `IStripePaymentService` → `StripePaymentService`
- `POST /api/payments/stripe/intent` (Bearer JWT or X-Guest-Token)
  - Calls `OrderService.CreateFromCartAsync`
  - Creates Stripe `PaymentIntent(amount = totalRon × 100 [bani], currency = "ron")`
  - Stores `PaymentIntentId` on Order
  - Returns `{ clientSecret, orderId }`
  - 400 if cart is empty
- `POST /api/webhooks/stripe` (NO auth — Stripe webhook)
  - ASP.NET Core: read the raw body via `Request.Body`, bounded — the endpoint is anonymous, so it caps the body in the action and carries `[RequestSizeLimit]` as the byte backstop
  - `EventUtility.ConstructEvent(rawBody, Stripe-Signature header, STRIPE_WEBHOOK_SECRET)`
  - `payment_intent.succeeded`: find Order by `PaymentIntentId`; if `Status == AwaitingPayment` → `Transition(AwaitingPayment, Paid)` → set `PaidAt = UtcNow` → `IEmailService.SendOrderConfirmedEmailAsync` (fire-and-forget)
  - `payment_intent.payment_failed`: find Order → `Transition(AwaitingPayment, PaymentFailed)`
  - Idempotency: if Order already in `Paid` state, return `200 OK` silently
  - Unknown `PaymentIntentId`: log warning, return `200 OK` (Stripe recommends not returning 4xx for unknown events)
  - Stripe signature invalid: return `400 Bad Request`
- Config: `Stripe:SecretKey`, `Stripe:WebhookSecret`, `Stripe:PublishableKey` (returned to frontend via config endpoint or environment)

**the legacy processor integration** (custom service — no official NuGet):
- `ILegacyProcessorService` → `the legacy processorService`
- `POST /api/payments/legacy-processor/initiate` (Bearer JWT or X-Guest-Token)
  - Calls `OrderService.CreateFromCartAsync` (sets `PaymentProcessor = the legacy processor`)
  - Builds the legacy processor payment parameters (exact field names per the legacy processor v3 spec):
    - `amount` (decimal, 2dp), `curr = "RON"`, `invoice_id = orderId.ToString()`, `order_desc = "FotoTipar comanda {orderNumber}"`, `merch_id`, `timestamp (yyyyMMddHHmmss UTC)`, `nonce (random 32-hex)`
  - HMAC-MD5 generation: concatenate fields in exact the legacy processor spec order → HMAC-MD5 with merchant secret key (hex digest)
  - Returns `{ redirectUrl: "https://secure.legacy-processor.ro/tdsprocess/tranzactd.php?{params}", orderId }`
  - `return_url = {frontendUrl}/comanda/{orderId}/confirmare?processor=legacy-processor`
  - `cancel_url = {frontendUrl}/checkout?cancelled=true`
  - `ipn_url = {backendUrl}/api/webhooks/legacy-processor`
- `POST /api/webhooks/legacy-processor` (NO auth — the legacy processor IPN; public endpoint)
  - Content-Type: `application/x-www-form-urlencoded`
  - Read all form fields; validate HMAC signature using `the legacy processorService.ValidateIpnSignature(fields, key)`
  - Amount validation: `fields["amount"] == order.TotalRon.ToString("F2")` → reject if mismatch (log + 200 with error response per spec)
  - `fields["action"] == "0"` → success: find Order by `invoice_id` → Transition to Paid → store `the legacy processorTransactionId` → fire email
  - Any other action value → PaymentFailed
  - Response: `<epayment>{date}|{hmac}</epayment>` plain text as per the legacy processor IPN spec
  - Invalid signature: return `<epayment>error</epayment>` (per spec; do NOT return 4xx or the legacy processor will retry indefinitely)
- Config: `the legacy processor:MerchantId`, `the legacy processor:SecretKey`, `the legacy processor:GatewayUrl`

**Configuration** (environment variables only — never in appsettings.json for secrets):
- `STRIPE__SECRETKEY`, `STRIPE__WEBHOOKSECRET`, `STRIPE__PUBLISHABLEKEY`
- `legacy-processor__MERCHANTID`, `legacy-processor__SECRETKEY`

**Tests**:
- Unit: `OrderService.CreateFromCart` — builds OrderItems with correct unit price from tier; sets OrderNumber
- Unit: Stripe PaymentIntent amount = TotalRon × 100 (bani)
- Unit: Stripe webhook — valid signature + `payment_intent.succeeded` → Order status = Paid
- Unit: Stripe webhook — tampered signature → 400
- Unit: Stripe webhook — duplicate event for already-paid order → 200, no email fired
- Unit: the legacy processor HMAC generation — verified against known test vector from the legacy processor docs
- Unit: the legacy processor IPN — valid signature + `action=0` → Order Paid
- Unit: the legacy processor IPN — amount mismatch → no status change, warning logged
- Unit: the legacy processor IPN — invalid signature → `<epayment>error</epayment>` response
- Integration: Stripe intent creation with mocked `StripeClient`
- Integration: Full the legacy processor initiate + IPN flow

---

## Bolt 017 — checkout-ui

```yaml
bolt: "017"
name: checkout-ui
intent: 004-checkout-payment
unit: 005-checkout-ui
type: simple
status: not-started
stories:
  - 001-checkout-stepper
  - 002-delivery-step
  - 003-locker-map-component
  - 004-order-review-step
  - 005-payment-step
  - 006-order-confirmation-page
epic_stories:
  - US-301
  - US-303
  - US-304
  - US-307
depends_on:
  - "014"  # upload-format-cart-ui (cart state + CartService)
  - "016"  # payment-backends (payment endpoints)
  - "015"  # shipping-and-order-core (shipping endpoints)
  - "004"  # angular-app-shell (guards, interceptors)
enables: []  # terminal bolt for this intent
```

### What This Bolt Builds

**Feature module**: `src/app/features/checkout/`

**CheckoutStateService** (singleton):
- Holds entire checkout state: `cart$`, `deliveryMethod$`, `shippingAddress$`, `selectedLocker$`, `shippingCostRon$`
- Persists to `sessionStorage` (survives refresh, cleared on tab close)
- `reset()` — called after successful order confirmation (clears all state + calls `CartService.clear()`)
- Guards: if `deliveryMethod$` is null at Step 2/3, redirect to Step 1

**Checkout stepper** (`/checkout`):
- `CheckoutComponent` — parent route with `<router-outlet>`; shows horizontal stepper (Step 1: Livrare, Step 2: Recapitulare, Step 3: Plată)
- Routes: `/checkout` → redirect to `/checkout/livrare`; `/checkout/livrare`, `/checkout/recapitulare`, `/checkout/plata`
- Step navigation via `CheckoutStateService`; back/forward buttons
- `GuestOrAuthGuard` on `/checkout` route (guest or logged-in required; if neither, show login/guest prompt modal)

**Delivery step** (`/checkout/livrare`):
- `DeliveryStepComponent` — two option cards with radio selection: `Easybox Sameday` (20 RON) and `Livrare la ușă` (25 RON)
- Calls `GET /api/shipping/cost` on init to populate prices on each card
- **Easybox flow**: city search `FormControl` + `debounceTime(300)` + `switchMap(city => ShippingService.getLockers(city))`; results shown in list AND on `LockerMapComponent`
- **Locker map** (`LockerMapComponent`): wraps `Leaflet.js` (lazy-loaded to avoid SSR issues); center on Romania on init; on locker list load, fit map bounds to locker pins; pin click → select locker → emit `lockerSelected` event; selected pin highlighted (green icon)
- **Home delivery flow**: `ReactiveFormsModule` form — `Stradă`, `Număr`, `Bloc/Ap (optional)`, `Oraș`, `Județ (Select)`, `Cod poștal`, `Nume destinatar`, `Telefon`; Județ dropdown: hardcoded 41 counties + `București`
- Saved addresses (logged-in users): `GET /api/account/addresses` → show radio list; `+ Adresă nouă` expands form
- `Continuă` button: disabled until delivery type selected AND (locker selected OR address form valid)
- On continue: saves to `CheckoutStateService` + navigates to `/checkout/recapitulare`

**Order review step** (`/checkout/recapitulare`):
- `ReviewStepComponent` — reads from `CartService.cart$` and `CheckoutStateService`
- Displays: list of cart items (thumbnail, format, finish, quantity, unit price, line total), delivery method, address/locker name, subtotal, shipping cost, **grand total in RON**
- Estimated delivery: `Livrare estimată: 2–4 zile lucrătoare`
- `Modifică coșul` → navigate to `/cos`; `Modifică adresa` → navigate back to `/checkout/livrare`
- Terms checkbox — links to `/termeni-si-conditii` (`target="_blank"`)
- `Plătește acum` button: disabled until `termsAccepted === true`
- On click → navigate to `/checkout/plata`

**Payment step** (`/checkout/plata`):
- `PaymentStepComponent` — on init: calls `POST /api/payments/stripe/intent` to get `clientSecret` (eagerly to reduce latency when user reaches step)
- Two option tabs: `Card internațional (Stripe)` | `Card românesc (the legacy processor)`
- **Stripe tab** (`StripeFormComponent`):
  - Loads `@stripe/stripe-js` with `loadStripe(environment.stripePublishableKey)`
  - Creates `Elements` instance with `clientSecret`; mounts `CardElement` in div
  - On submit: `stripe.confirmCardPayment(clientSecret, { payment_method: { card } })`
  - Success: navigate to `/comanda/{orderId}/confirmare`
  - Error: display inline Romanian error message; Stripe Elements remain mounted for retry
- **the legacy processor tab**:
  - `Plătește cu the legacy processor` button
  - On click: calls `POST /api/payments/legacy-processor/initiate` → `window.location.href = redirectUrl`
  - Loading spinner during API call
- Back button → `/checkout/recapitulare` (Stripe intent already created; order remains in `AwaitingPayment`)

**Order confirmation** (`/comanda/:orderId/confirmare`):
- `ConfirmationComponent` — route: `/comanda/:orderId/confirmare`
- On init: `GET /api/orders/{orderId}` — if `status != Paid`, redirect to `/`
- Handles `?processor=legacy-processor` query param (same page, different entry path — no behavioral difference)
- Displays: success animation (CSS checkmark), order number, photo count, format, total paid, delivery method + address/locker
- `OrderStatusStepperComponent` (shared, reusable): shows 4 stages with current step highlighted — `Comandă primită ✓`, `În pregătire`, `Expediată`, `Livrată`
- Estimated delivery date range
- Guest users: `Vrei să-ți salvezi comanda? Creează un cont gratuit` CTA → pre-fills email from `GuestAuthService.email`
- Logged-in users: `Vezi istoricul comenzilor` link → `/contul-meu/comenzi`
- On display: `CheckoutStateService.reset()` (clears cart + checkout state)
- `ShippingService` — `src/app/core/services/shipping.service.ts`
  - `getLockers(city) → Observable<LockerDto[]>`
  - `getShippingCost(type) → Observable<ShippingCostDto>`
- `PaymentService` — `src/app/core/services/payment.service.ts`
  - `createStripeIntent() → Observable<StripeIntentResponse>`
  - `initiateLegacyProcessor() → Observable<the legacy processorInitiateResponse>`

**NPM packages to install**:
- `leaflet` + `@types/leaflet`
- `@stripe/stripe-js`
- `heic2any` (already needed by bolt 014 but listed here for completeness of this bolt's `npm install`)

**Tests**:
- Unit: `DeliveryStepComponent` — Easybox selection enables locker map; form invalid until locker selected
- Unit: `DeliveryStepComponent` — home delivery requires all required fields
- Unit: Locker search — `debounceTime` filter prevents rapid API calls
- Unit: `ReviewStepComponent` — grand total = subtotal + shippingCost
- Unit: `ReviewStepComponent` — `Plătește acum` disabled until terms checked
- Unit: `PaymentStepComponent` — Stripe Elements initialized on component init
- Unit: `ConfirmationComponent` — redirects to `/` if order not Paid
- Unit: `ConfirmationComponent` — shows guest CTA for guest session, `Comenzi` link for auth user
- E2E: Full checkout flow — upload → format → cart → delivery (Easybox) → review → Stripe payment → confirmation
- E2E: the legacy processor redirect flow (mock redirect)

---

## Build Order Summary

The bolts must be delivered in the following dependency-safe sequence:

```
Phase A: [012] photo-upload-backend
Phase B: [013] cart-api         (requires 012)
Phase C: [014] upload-format-cart-ui    (requires 013)
         [015] shipping-and-order-core  (requires 013) ← parallel with 014
Phase D: [016] payment-backends         (requires 015)
Phase E: [017] checkout-ui              (requires 014 + 016)
```

Phases C bolts (014 and 015) can run in **parallel** as they share only Bolt 013 as a dependency.
