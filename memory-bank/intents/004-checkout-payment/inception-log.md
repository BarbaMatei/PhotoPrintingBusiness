---
intent: 004-checkout-payment
title: "Checkout & Payment"
type: green-field
status: complete
created: 2026-05-21T10:00:00Z
completed: 2026-05-21T12:00:00Z
---

# Inception Log: 004-checkout-payment

## Overview

**Intent**: Photo upload, cart management, and full checkout flow — from bulk photo upload through delivery selection, dual-processor payment (Stripe + the legacy processor), and order confirmation.
**Type**: green-field
**Created**: 2026-05-21T10:00:00Z
**Completed**: 2026-05-21T10:00:00Z

## Artifacts Created

| Artifact | Status | File |
|----------|--------|------|
| Inception Log | ✅ | inception-log.md |
| Requirements | ✅ | requirements.md |
| System Context | ✅ | system-context.md |
| Units | ✅ | units.md |
| Unit Brief 001 | ✅ | units/001-upload-and-cart-backend/unit-brief.md |
| Unit Brief 002 | ✅ | units/002-upload-format-cart-ui/unit-brief.md |
| Unit Brief 003 | ✅ | units/003-shipping-and-order-core/unit-brief.md |
| Unit Brief 004 | ✅ | units/004-payment-backends/unit-brief.md |
| Unit Brief 005 | ✅ | units/005-checkout-ui/unit-brief.md |
| Stories (unit 001) | ✅ | units/001-upload-and-cart-backend/stories/ (6 stories) |
| Stories (unit 002) | ✅ | units/002-upload-format-cart-ui/stories/ (5 stories) |
| Stories (unit 003) | ✅ | units/003-shipping-and-order-core/stories/ (4 stories) |
| Stories (unit 004) | ✅ | units/004-payment-backends/stories/ (5 stories) |
| Stories (unit 005) | ✅ | units/005-checkout-ui/stories/ (6 stories) |
| Bolt Plan | ✅ | bolt-plan.md |
| Bolt 012 stub | ✅ | memory-bank/bolts/012-photo-upload-backend/bolt.md |
| Bolt 013 stub | ✅ | memory-bank/bolts/013-cart-api/bolt.md |
| Bolt 014 stub | ✅ | memory-bank/bolts/014-upload-format-cart-ui/bolt.md |
| Bolt 015 stub | ✅ | memory-bank/bolts/015-shipping-and-order-core/bolt.md |
| Bolt 016 stub | ✅ | memory-bank/bolts/016-payment-backends/bolt.md |
| Bolt 017 stub | ✅ | memory-bank/bolts/017-checkout-ui/bolt.md |
| Story Index | ✅ | memory-bank/story-index.md (updated) |

## Summary

| Metric | Count |
|--------|-------|
| Functional Requirements | 18 |
| Non-Functional Requirements | 9 |
| Units | 5 |
| Stories | 26 |
| Bolts Planned | 6 (012–017) |
| Epic-2 stories covered | US-201, US-202, US-203, US-205, US-206 |
| Epic-3 stories covered | US-301, US-302, US-303, US-304, US-305, US-306, US-307 |

---

## Scope & Objectives

This intent delivers the entire customer purchase journey for FotoTipar — from uploading photos through receiving an order confirmation. It is the **core revenue-generating flow** of the platform.

### In Scope
- **Bulk photo upload**: drag-and-drop, MIME validation, dimension extraction, preview thumbnails, quality badges, upload cleanup
- **Format & finish selection**: global format (10×15 / 13×18 / 15×21) and finish (Lucios / Mat) applied across all photos; per-photo quantity steppers
- **Cart management**: server-side cart for authenticated users, localStorage fallback for guests, cart merge on login
- **Delivery selection**: Sameday Easybox locker (Phase 1: seeded static list with Leaflet map) or home delivery (address form); Sameday AWB manual in Phase 1
- **Checkout review**: read-only order summary with terms acceptance gate
- **Dual payment**: Stripe (embedded Elements, webhook-confirmed) and the legacy processor (redirect + IPN, HMAC-MD5 — Romanian market)
- **Order creation**: FT-YYYYNNNN order numbers, OrderStatus state machine, pricing snapshot at order time
- **Order confirmation page**: status stepper, guest-to-registered CTA

### Out of Scope
- Product catalog management (covered in intent 003-product-catalog; US-204 already complete)
- Order history / admin order queue (intent 005-order-management, planned)
- Transactional emails (intent 006-email-notifications, planned — email hooks are called but templates delivered separately)
- Sameday AWB API integration Phase 2 (stub `IShippingService` with manual fallback only)
- Refunds and cancellations (future intent)
- Saved payment methods / recurring orders (future intent)

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
- **Acceptance Criteria**: `GET /api/uploads/{id}/preview` returns a JPEG thumbnail. Served with `Content-Disposition: inline`. Unknown IDs return 404.
- **Priority**: Must

### FR-4: Upload cleanup job
- **Description**: Background job removes uploads not associated with any order after 24 hours, freeing storage.
- **Acceptance Criteria**: `UploadCleanupJob` runs hourly; soft-deletes uploads with no `OrderItem` reference after 24 h; deletes physical files; idempotent.
- **Priority**: Must

### FR-5: Cart API (server-side)
- **Description**: Authenticated users and guests can manage a server-side cart with the selected product, finish, and per-upload quantities.
- **Acceptance Criteria**: `POST /api/cart` replaces cart atomically; `GET /api/cart` returns items with computed totals; `DELETE /api/cart` clears cart; all endpoints accept JWT or X-Guest-Token.
- **Priority**: Must

### FR-6: Cart merge on login
- **Description**: When a guest logs in, their guest cart is merged into their user cart.
- **Acceptance Criteria**: `POST /api/cart/merge` accepts `{ guestCart }` with Bearer token; server-side cart takes precedence for conflicting items; guest uploads transferred to user.
- **Priority**: Must

### FR-7: Bulk photo upload UI
- **Description**: Angular upload page with drag-and-drop zone, per-file progress bars, thumbnail grid, quality badges, HEIC preview, and per-file remove.
- **Acceptance Criteria**: Accepts JPEG/PNG/HEIC; client-side validation (type, size ≤ 50 MB, count ≤ 30); progress bar per file; quality badge per thumbnail (Green/Yellow/Red vs. selected format's resolution); `Șterge toate` button.
- **Priority**: Must

### FR-8: Format & finish selector UI
- **Description**: Above the thumbnail grid: a segmented control for format selection and a toggle for finish selection. Both apply globally to all photos. Per-photo quantity stepper. Live order summary panel.
- **Acceptance Criteria**: Changing format/finish immediately recalculates quality badges for all photos; quantity stepper min 1 / max 100; order summary shows per-line subtotal and grand total (excl. shipping); `Adaugă în coș` CTA disabled if no photos.
- **Priority**: Must

### FR-9: Cart page UI
- **Description**: Angular `/cos` page showing cart items with edit capabilities and persistent state.
- **Acceptance Criteria**: Each item shows thumbnail, format, finish, quantity stepper, unit price, line total, remove button; shipping shown as `Calculat la pasul următor`; nav cart icon badge shows item count reactively; localStorage persistence for guests with server sync on change.
- **Priority**: Must

### FR-10: Easybox locker catalog (server-side)
- **Description**: Database table of ~200 Romanian Sameday Easybox locker locations with coordinates, seeded at migration.
- **Acceptance Criteria**: `GET /api/shipping/lockers?city=` returns lockers for a city (case-insensitive); indexed City column. Phase 1: seeded static data; Phase 2: proxied from Sameday API.
- **Priority**: Must

### FR-11: Shipping cost endpoint
- **Description**: Returns shipping cost per delivery type from configuration.
- **Acceptance Criteria**: `GET /api/shipping/cost?type=Easybox` returns `{ costRon: 20.00 }`; `type=Courier` returns `{ costRon: 25.00 }`. Config-driven, no hardcoded values in code.
- **Priority**: Must

### FR-12: Order entity and order number generation
- **Description**: Orders are created when a payment is initiated. Each order has a human-readable number in `FT-YYYYNNNN` format.
- **Acceptance Criteria**: `OrderNumber` is auto-generated, unique per calendar year, zero-padded to 4 digits. Resetting the counter per year is handled by the service layer.
- **Priority**: Must

### FR-13: Order status state machine
- **Description**: The `OrderStatus` enum enforces valid transitions: `AwaitingPayment → Paid → Printing → Shipped → Delivered`; side branches: `AwaitingPayment → PaymentFailed`, `Printing → Cancelled`.
- **Acceptance Criteria**: Invalid transitions return 400 Bad Request. `OrderStatusMachine.Transition(from, to)` throws on invalid transition.
- **Priority**: Must

### FR-14: Stripe payment integration
- **Description**: Backend creates a Stripe PaymentIntent, builds the pending Order, and confirms it via webhook.
- **Acceptance Criteria**: `POST /api/payments/stripe/intent` returns `{ clientSecret, orderId }`; `POST /api/webhooks/stripe` validates `Stripe-Signature` header; `payment_intent.succeeded` → Order Paid + OrderConfirmedEmail; `payment_intent.payment_failed` → PaymentFailed; idempotent on duplicate events.
- **Priority**: Must

### FR-15: the legacy processor payment integration
- **Description**: Backend generates a legacy-processor redirect URL with HMAC-MD5 signature and confirms orders via IPN callback.
- **Acceptance Criteria**: `POST /api/payments/legacy-processor/initiate` returns `{ redirectUrl, orderId }`; `POST /api/webhooks/legacy-processor` validates HMAC, sets Order Paid on `action=0`; amount in IPN must match order amount; response format as per the legacy processor spec.
- **Priority**: Must

### FR-16: Delivery step UI (checkout Step 1)
- **Description**: Angular step allowing delivery method selection — Sameday Easybox (with Leaflet map + locker search) or home delivery (address form with Romanian county dropdown).
- **Acceptance Criteria**: City search debounced 300ms calls `/api/shipping/lockers`; lockers rendered on Leaflet + OpenStreetMap map with clickable pins; home delivery form validates all required fields; saved addresses shown for authenticated users; `Continuă` button disabled until selection complete.
- **Priority**: Must

### FR-17: Order review step UI (checkout Step 2)
- **Description**: Read-only summary of cart contents, delivery details, and grand total before payment.
- **Acceptance Criteria**: Shows photo count, format, finish, line totals, subtotal, shipping cost, grand total in RON; `Plătește acum` disabled until Terms & Conditions checkbox checked; `Modifică coșul` / `Modifică adresa` edit links.
- **Priority**: Must

### FR-18: Payment step & order confirmation UI (checkout Steps 3 + Confirmation)
- **Description**: Step 3 offers two tabs — Stripe Elements (embedded card form) and the legacy processor (redirect button). On success, `/comanda/{orderId}/confirmare` shows the order number, status stepper, and delivery details.
- **Acceptance Criteria**: Stripe tab: calls `POST /api/payments/stripe/intent`, initializes Stripe Elements with clientSecret, `stripe.confirmCardPayment()` on submit; the legacy processor tab: calls `POST /api/payments/legacy-processor/initiate`, `window.location.href` redirect; confirmation page fetches `GET /api/orders/{orderId}`, redirects home if order not Paid; guest CTA on confirmation page.
- **Priority**: Must

---

## Non-Functional Requirements

### NFR-1: Security — no card data on server
- Card details must never touch the FotoTipar server. Stripe Elements is client-side only. the legacy processor uses a redirect to their hosted page.
- **Measurement**: Security audit — zero card fields in any API request/response logs.

### NFR-2: Payment webhook idempotency
- Webhook handlers for both Stripe and the legacy processor must be idempotent. Replaying a webhook for an already-paid order must not fire duplicate emails or create duplicate order transitions.
- **Measurement**: Unit test verifies 200 OK and no side effects on duplicate delivery.

### NFR-3: MIME validation at byte level
- Upload endpoint validates file type by reading the first 8 bytes (magic numbers), not by file extension or Content-Type header.
- **Measurement**: Integration test: rename `malware.exe` as `photo.jpg`; server must reject with 415.

### NFR-4: Upload path traversal prevention
- Stored file paths must use UUID-generated names, never the original filename.
- **Measurement**: File path in DB contains no user-supplied string.

### NFR-5: Stripe webhook signature verification
- The `Stripe-Signature` header must be verified using `EventUtility.ConstructEvent` before processing any webhook event.
- **Measurement**: Unit test: tampered signature returns 400.

### NFR-6: the legacy processor IPN amount validation
- Amount in the IPN callback must be cross-checked against the stored order amount. Mismatches must be rejected.
- **Measurement**: Unit test: IPN with different amount logs warning and returns error.

### NFR-7: Cart merge atomicity
- Cart merge on login (`POST /api/cart/merge`) must execute within a single database transaction.
- **Measurement**: Integration test: connection failure mid-merge leaves cart unchanged.

### NFR-8: Upload cleanup correctness
- Background cleanup job must never delete uploads that are linked to a completed order, even if the upload is older than 24 hours.
- **Measurement**: Unit test: upload with OrderItem reference is not deleted.

### NFR-9: Checkout state resilience
- The Angular checkout state must survive a page refresh during the checkout flow (Step 1 → Step 2 → Step 3). If lost, the user is returned to the cart page.
- **Measurement**: E2E test: refresh at Step 2 → cart page redirect (or state restored from sessionStorage).

---

## Key Design Decisions

| # | Decision | Alternatives Considered | Rationale | Status |
|---|----------|------------------------|-----------|--------|
| ADR-1 | Stripe card payment via Stripe Elements (no redirect) | Stripe Checkout hosted page | Seamless embedded UX; better brand consistency; easier error handling inline | Approved |
| ADR-2 | the legacy processor via redirect to hosted payment page | Self-hosted PCI form | PCI compliance — never handle card data; the legacy processor only supports redirect model for IPN | Approved |
| ADR-3 | Phase 1 Sameday: seeded static locker list; Phase 2: live Sameday API | Live API only | Sameday API credentials not available at build time; static seed unblocks UI delivery; `IShippingService` abstraction allows swap | Approved |
| ADR-4 | Cart replace strategy (POST /api/cart replaces all) | Delta add/remove endpoints | Simpler server state; Angular CartService holds source-of-truth; reduces conflict resolution complexity | Approved |
| ADR-5 | Product pricing snapshot at order time (JSON column) | Live join to Products table | Historical accuracy: price at order time is preserved even if admin changes prices later | Approved |
| ADR-6 | Order created before payment completes (AwaitingPayment) | Create order only on webhook | Required by both Stripe and the legacy processor: orderId needed in payment request before payment is confirmed | Approved |
| ADR-7 | IStorageService abstraction for file storage | Direct disk access in service | Enables future S3/Azure Blob migration; testable via in-memory mock | Approved |
| ADR-8 | LocalStorage cart for guests synced to server on change | Server-only or LocalStorage-only | Works offline; server sync provides resilience; merge on login preserves guest work | Approved |
| ADR-9 | HEIC preview via `heic2any` (client-side) | Server-side HEIC conversion | No server processing overhead; browser-only concern; `heic2any` is well-tested for this use case | Approved |

---

## System Context

### Upstream Dependencies (what this intent consumes)

| Dependency | Bolt | What We Need |
|------------|------|-------------|
| Auth core — JWT + Guest Sessions | 005, 007 | All upload/cart/payment endpoints accept Bearer JWT or X-Guest-Token |
| Product catalog | 009 | Products, sizes, finishes, pricing tiers for cart line items and quality badge thresholds |
| Angular App Shell | 004 | Route guards, interceptors, lazy-loaded route registration |
| Email infrastructure | 003 | `IEmailService` — called on `payment_intent.succeeded` and the legacy processor IPN success |

### Downstream Dependents (what depends on this intent)

| Dependent | Intent | What They Need |
|-----------|--------|----------------|
| Order management | 005-order-management | `Orders` + `OrderItems` tables, `GET /api/orders`, `OrderStatus` enum |
| Email notifications | 006-email-notifications | `OrderConfirmedEmail` event, order data shape |
| Admin panel | 007-admin | `PATCH /api/orders/{id}/status`, SignalR hub for new-order events |

---

## Units Decomposition

| # | Unit | Type | Stories | Bolts |
|---|------|------|---------|-------|
| 001 | upload-and-cart-backend | Backend DDD | 6 | 012, 013 |
| 002 | upload-format-cart-ui | Frontend Simple | 5 | 014 |
| 003 | shipping-and-order-core | Backend DDD | 4 | 015 |
| 004 | payment-backends | Backend DDD | 5 | 016 |
| 005 | checkout-ui | Frontend Simple | 6 | 017 |

---

## Bolt Plan

| Bolt | Name | Type | Unit | Stories Covered | Depends On |
|------|------|------|------|-----------------|------------|
| 012 | photo-upload-backend | DDD | 001 | US-202 (full) | 005, 007 |
| 013 | cart-api | DDD | 001 | US-206 (full) | 012, 009 |
| 014 | upload-format-cart-ui | Simple | 002 | US-201, US-203, US-205 | 013, 011 |
| 015 | shipping-and-order-core | DDD | 003 | US-302 (full) + Order entity | 013 |
| 016 | payment-backends | DDD | 004 | US-305, US-306 (full) | 015 |
| 017 | checkout-ui | Simple | 005 | US-301, US-303, US-304, US-307 | 014, 016 |

---

## Stories List

### Unit 001 — upload-and-cart-backend (6 stories)
1. `001-upload-entity-schema.md` — Uploads table, IStorageService, storage path logic
2. `002-upload-endpoint.md` — POST /api/uploads: MIME, ImageSharp, rate limit, response DTO
3. `003-upload-preview-and-cleanup.md` — GET /api/uploads/{id}/preview + UploadCleanupJob
4. `004-cart-item-entity.md` — CartItem entity + EF Core migration
5. `005-cart-crud-endpoints.md` — POST/GET/DELETE /api/cart with computed totals
6. `006-cart-merge-endpoint.md` — POST /api/cart/merge (guest → user, transactional)

### Unit 002 — upload-format-cart-ui (5 stories)
1. `001-upload-page.md` — Drag-drop zone, progress bars, HEIC preview, thumbnail grid (US-201)
2. `002-format-finish-selector.md` — Format/finish controls + quality badge recalc (US-203)
3. `003-order-summary-panel.md` — Live subtotal summary + add-to-cart CTA (US-203)
4. `004-cart-page.md` — Cart list with quantity steppers, persistent state (US-205)
5. `005-cart-service.md` — CartService: localStorage/server sync, merge on login, badge count (US-205)

### Unit 003 — shipping-and-order-core (4 stories)
1. `001-easybox-locker-catalog.md` — EasyboxLocker entity + ~200 seeded lockers migration
2. `002-shipping-endpoints.md` — GET /api/shipping/lockers + /api/shipping/cost + IShippingService
3. `003-order-entity-schema.md` — Order + OrderItem entities, FT-YYYYNNNN, EF migration
4. `004-order-status-machine.md` — OrderStatus enum + OrderStatusMachine transitions

### Unit 004 — payment-backends (5 stories)
1. `001-order-service.md` — IOrderService: create order from cart, snapshot pricing, order number
2. `002-stripe-payment-intent.md` — POST /api/payments/stripe/intent + PaymentIntent creation
3. `003-stripe-webhook-handler.md` — POST /api/webhooks/stripe + sig verification + idempotency
4. `004-legacy-processor-initiate.md` — POST /api/payments/legacy-processor/initiate + HMAC-MD5 signing
5. `005-legacy-processor-ipn-handler.md` — POST /api/webhooks/legacy-processor + IPN validation + amount check

### Unit 005 — checkout-ui (6 stories)
1. `001-checkout-stepper.md` — CheckoutStepper component + CheckoutStateService (US-301 scaffold)
2. `002-delivery-step.md` — Delivery method cards + shipping cost display + address form (US-301)
3. `003-locker-map-component.md` — Leaflet.js map + locker search + pin selection (US-301)
4. `004-order-review-step.md` — Read-only summary + terms checkbox + totals display (US-303)
5. `005-payment-step.md` — Stripe Elements tab + the legacy processor redirect tab (US-304)
6. `006-order-confirmation-page.md` — /comanda/{orderId}/confirmare + status stepper + guest CTA (US-307)

---

## Ready for Construction

**Checklist**:
- [x] All requirements documented (18 FR + 9 NFR)
- [x] System context defined (actors, external systems, upstream/downstream deps)
- [x] Units decomposed (5 units with unit-briefs)
- [x] Stories created for all units (26 stories across 5 units)
- [x] Bolts planned (6 bolts: 012–017)
- [x] Human review complete (2026-05-21T12:00:00Z)

## Next Steps

Start Construction with the first bolt in dependency order:

→ `/specsmd-construction-agent --bolt="012-photo-upload-backend"`

**Build order**: `012 → 013 → 014 → 015 → 016 → 017`
