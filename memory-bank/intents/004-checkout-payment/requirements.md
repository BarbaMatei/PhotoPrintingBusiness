---
intent: 004-checkout-payment
phase: inception
status: complete
created: 2026-05-21T10:00:00Z
updated: 2026-05-21T12:00:00Z
---

# Requirements: Checkout & Payment

## Intent Overview

Delivers the entire customer purchase journey for FotoTipar — from uploading photos through delivery selection, dual-processor payment (Stripe + the legacy processor), and order confirmation. This is the **core revenue-generating flow** of the platform.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Customers can complete a purchase end-to-end | Order created with Paid status after payment | Must |
| Romanian market payment support | the legacy processor IPN confirms orders for Romanian cards | Must |
| Guests can checkout without registering | Guest token accepted across upload/cart/checkout | Must |
| Photos preserved with orders indefinitely | Cleanup job never deletes ordered uploads | Must |

---

## Functional Requirements

### FR-1: Photo upload endpoint
- **Description**: Backend accepts multipart/form-data photo uploads (JPEG, PNG, HEIC) for authenticated users and guests. Validates MIME by magic bytes. Extracts dimensions via ImageSharp. Returns metadata.
- **Acceptance Criteria**: `POST /api/uploads` returns `[{ uploadId, widthPx, heightPx, fileSizeBytes, previewUrl }]`; rejects non-image with 415; enforces 30-file session limit with 429; rejects >50 MB files with 413.
- **Priority**: Must

### FR-2: Upload storage abstraction
- **Description**: Uploaded files are saved using `IStorageService`. Phase 1 uses local filesystem (`LocalStorageService`). Storage path uses UUID, never original filename.
- **Acceptance Criteria**: Files stored at `uploads/{userId|guestSessionId}/{uuid}.{ext}`. `IStorageService` interface allows future S3/Azure Blob swap without changing callers.
- **Priority**: Must

### FR-3: Upload preview endpoint
- **Description**: A resized thumbnail (300px max dimension) is served from the backend with cache headers.
- **Acceptance Criteria**: `GET /api/uploads/{id}/preview` returns a JPEG thumbnail with `Content-Disposition: inline`. Unknown or soft-deleted IDs return 404.
- **Priority**: Must

### FR-4: Upload cleanup job
- **Description**: Background job removes uploads not associated with any order after 24 hours, freeing storage.
- **Acceptance Criteria**: `UploadCleanupJob` runs hourly; soft-deletes uploads with no `OrderItem` reference after 24 h; deletes physical files; idempotent.
- **Priority**: Must

### FR-5: Cart API (server-side)
- **Description**: Authenticated users and guests manage a server-side cart with selected product, finish, and per-upload quantities.
- **Acceptance Criteria**: `POST /api/cart` replaces cart atomically; `GET /api/cart` returns items with computed totals; `DELETE /api/cart` clears cart; all endpoints accept JWT or `X-Guest-Token`.
- **Priority**: Must

### FR-6: Cart merge on login
- **Description**: When a guest logs in, their guest cart is merged into their user cart.
- **Acceptance Criteria**: `POST /api/cart/merge` accepts `{ guestCart }` with Bearer token; server-side cart takes precedence for conflicts; guest uploads transferred to user.
- **Priority**: Must

### FR-7: Bulk photo upload UI
- **Description**: Angular upload page with drag-and-drop zone, per-file progress bars, thumbnail grid, quality badges, HEIC preview, and per-file remove.
- **Acceptance Criteria**: Accepts JPEG/PNG/HEIC; client-side validation (type, size ≤ 50 MB, count ≤ 30); progress bar per file; quality badge per thumbnail (Green/Yellow/Red vs. selected format's resolution); `Șterge toate` button.
- **Priority**: Must

### FR-8: Format & finish selector UI
- **Description**: Segmented control for format (10×15 / 13×18 / 15×21) and toggle for finish (Lucios / Mat) above the thumbnail grid, applied globally. Per-photo quantity stepper. Live order summary panel.
- **Acceptance Criteria**: Changing format/finish recalculates quality badges immediately; quantity stepper min 1 / max 100; order summary shows per-line subtotal and grand total (excl. shipping); `Adaugă în coș` CTA disabled if no photos.
- **Priority**: Must

### FR-9: Cart page UI
- **Description**: Angular `/cos` page showing cart items with edit capabilities and persistent state.
- **Acceptance Criteria**: Each item shows thumbnail, format, finish, quantity stepper, unit price, line total, remove button; shipping shown as `Calculat la pasul următor`; nav cart icon badge updates reactively; localStorage persistence for guests with server sync on change.
- **Priority**: Must

### FR-10: Easybox locker catalog (server-side)
- **Description**: Database table of ~200 Romanian Sameday Easybox locker locations with coordinates, seeded at migration.
- **Acceptance Criteria**: `GET /api/shipping/lockers?city=` returns lockers for a city (case-insensitive); indexed `City` column. Phase 1: seeded static data.
- **Priority**: Must

### FR-11: Shipping cost endpoint
- **Description**: Returns shipping cost per delivery type from configuration.
- **Acceptance Criteria**: `GET /api/shipping/cost?type=Easybox` returns `{ costRon: 20.00 }`; `type=Courier` returns `{ costRon: 25.00 }`. Config-driven, no hardcoded values in code.
- **Priority**: Must

### FR-12: Order entity and order number generation
- **Description**: Orders are created when payment is initiated. Each order has a human-readable number in `FT-YYYYNNNN` format.
- **Acceptance Criteria**: `OrderNumber` is auto-generated, unique per calendar year, zero-padded to 4 digits. Counter resets per year via service layer.
- **Priority**: Must

### FR-13: Order status state machine
- **Description**: `OrderStatus` enum enforces valid transitions: `AwaitingPayment → Paid → Printing → Shipped → Delivered`; side branches: `AwaitingPayment → PaymentFailed`, `Printing → Cancelled`.
- **Acceptance Criteria**: Invalid transitions return 400 Bad Request. `OrderStatusMachine.Transition(from, to)` throws on invalid transition.
- **Priority**: Must

### FR-14: Stripe payment integration
- **Description**: Backend creates a Stripe PaymentIntent, builds the pending Order, and confirms it via webhook.
- **Acceptance Criteria**: `POST /api/payments/stripe/intent` returns `{ clientSecret, orderId }`; `POST /api/webhooks/stripe` validates `Stripe-Signature`; `payment_intent.succeeded` → Order Paid + email; `payment_intent.payment_failed` → PaymentFailed; idempotent on duplicate events.
- **Priority**: Must

### FR-15: the legacy processor payment integration
- **Description**: Backend generates a legacy-processor redirect URL with HMAC-MD5 signature and confirms orders via IPN callback.
- **Acceptance Criteria**: `POST /api/payments/legacy-processor/initiate` returns `{ redirectUrl, orderId }`; `POST /api/webhooks/legacy-processor` validates HMAC, sets Order Paid on `action=0`; IPN amount cross-checked against order amount.
- **Priority**: Must

### FR-16: Delivery step UI (checkout Step 1)
- **Description**: Angular step for delivery method selection — Sameday Easybox (Leaflet map + locker search) or home delivery (address form with Romanian county dropdown).
- **Acceptance Criteria**: City search debounced 300ms calls `/api/shipping/lockers`; lockers on Leaflet/OpenStreetMap map with clickable pins; home delivery form validates all required fields; saved addresses shown for authenticated users; `Continuă` disabled until selection complete.
- **Priority**: Must

### FR-17: Order review step UI (checkout Step 2)
- **Description**: Read-only summary of cart contents, delivery details, and grand total before payment.
- **Acceptance Criteria**: Shows photo count, format, finish, line totals, subtotal, shipping cost, grand total in RON; `Plătește acum` disabled until T&C checkbox checked; `Modifică coșul` / `Modifică adresa` edit links present.
- **Priority**: Must

### FR-18: Payment step & order confirmation UI (checkout Steps 3 + Confirmation)
- **Description**: Step 3 offers Stripe Elements (embedded card form) and the legacy processor (redirect button) tabs. Confirmation page at `/comanda/{orderId}/confirmare` shows order number, status stepper, and delivery details.
- **Acceptance Criteria**: Stripe tab calls `POST /api/payments/stripe/intent`, initializes Elements, calls `stripe.confirmCardPayment()` on submit; the legacy processor tab calls `POST /api/payments/legacy-processor/initiate` then `window.location.href` redirect; confirmation page fetches order, redirects home if not Paid; guest CTA shown.
- **Priority**: Must

---

## Non-Functional Requirements

### Security

| Requirement | Standard | Measurement |
|-------------|----------|-------------|
| NFR-1: No card data on server | Stripe Elements client-side only; the legacy processor redirect | Security audit — zero card fields in any API log |
| NFR-3: MIME validation at byte level | Read first 8 magic bytes, not file extension | Integration test: renamed .exe rejected with 415 |
| NFR-4: Upload path traversal prevention | UUID-generated filenames only | File path in DB contains no user-supplied string |
| NFR-5: Stripe webhook signature verification | `EventUtility.ConstructEvent` before processing | Unit test: tampered signature → 400 |
| NFR-6: the legacy processor IPN amount validation | Cross-check IPN amount vs. stored order amount | Unit test: mismatched amount → warning + error response |

### Reliability

| Requirement | Metric | Measurement |
|-------------|--------|-------------|
| NFR-2: Webhook idempotency | Duplicate webhook → no side effects | Unit test: 200 OK, no duplicate email/transition |
| NFR-7: Cart merge atomicity | Single DB transaction | Integration test: mid-merge failure leaves cart unchanged |
| NFR-8: Upload cleanup correctness | Never delete uploads linked to completed orders | Unit test: upload with OrderItem reference not deleted |
| NFR-9: Checkout state resilience | Angular checkout state survives page refresh | E2E test: refresh at Step 2 → cart redirect or state restored |

---

## Technical Constraints

Intent-specific constraints (project standards loaded separately by Construction Agent):

- `IStorageService` abstraction required — Phase 1 filesystem, Phase 2 S3/Azure Blob swappable without callers changing
- the legacy processor IPN uses HMAC-MD5 as required by the legacy processor specification; cannot be upgraded
- Sameday locker list Phase 1: static seeded data; `IShippingService` stub for Phase 2 API swap
- Order must be created before payment initiates (required by both Stripe and the legacy processor flows)
- Pricing snapshot stored as JSON column on Order at creation time (historical accuracy)
- HEIC client-side preview via `heic2any` — no server-side HEIC conversion
