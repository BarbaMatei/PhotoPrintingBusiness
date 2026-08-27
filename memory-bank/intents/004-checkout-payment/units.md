---
intent: 004-checkout-payment
phase: inception
status: inception-complete
created: 2026-05-21T10:00:00Z
updated: 2026-05-21T10:00:00Z
---

# Units: 004-checkout-payment

## Decomposition Summary

| # | Unit | Type | Bolt Type | FRs | Bolts |
|---|------|------|-----------|-----|-------|
| 001 | upload-and-cart-backend | Backend | ddd-construction-bolt | FR-1, FR-2, FR-3, FR-4, FR-5, FR-6 | 2 (012, 013) |
| 002 | upload-format-cart-ui | Frontend | simple-construction-bolt | FR-7, FR-8, FR-9 | 1 (014) |
| 003 | shipping-and-order-core | Backend | ddd-construction-bolt | FR-10, FR-11, FR-12, FR-13 | 1 (015) |
| 004 | payment-backends | Backend | ddd-construction-bolt | FR-14, FR-15 | 1 (016) |
| 005 | checkout-ui | Frontend | simple-construction-bolt | FR-16, FR-17, FR-18 | 1 (017) |

## Requirement-to-Unit Mapping

- **FR-1** Photo upload endpoint → `001-upload-and-cart-backend`
- **FR-2** Upload storage abstraction (IStorageService) → `001-upload-and-cart-backend`
- **FR-3** Upload preview endpoint → `001-upload-and-cart-backend`
- **FR-4** Upload cleanup background job → `001-upload-and-cart-backend`
- **FR-5** Cart API CRUD (POST/GET/DELETE) → `001-upload-and-cart-backend`
- **FR-6** Cart merge on login → `001-upload-and-cart-backend`
- **FR-7** Bulk photo upload Angular UI → `002-upload-format-cart-ui`
- **FR-8** Format & finish selector Angular UI → `002-upload-format-cart-ui`
- **FR-9** Cart page Angular UI + CartService → `002-upload-format-cart-ui`
- **FR-10** Easybox locker catalog (entity + seed) → `003-shipping-and-order-core`
- **FR-11** Shipping cost endpoint → `003-shipping-and-order-core`
- **FR-12** Order entity + order number generation → `003-shipping-and-order-core`
- **FR-13** Order status state machine → `003-shipping-and-order-core`
- **FR-14** Stripe payment integration (backend) → `004-payment-backends`
- **FR-15** the legacy processor payment integration (backend) → `004-payment-backends`
- **FR-16** Delivery step Angular UI → `005-checkout-ui`
- **FR-17** Order review step Angular UI → `005-checkout-ui`
- **FR-18** Payment step + order confirmation Angular UI → `005-checkout-ui`

---

## Unit 001: upload-and-cart-backend

**Purpose**: All backend work to receive, validate, store, and serve uploaded photos; plus the server-side cart that ties uploads to a product selection with quantities and computes totals.

**Why two bolts?**
The upload domain (file I/O, ImageSharp, IStorageService, background job) is structurally independent from the cart (relational join of Uploads + Products with quantity logic). Splitting them keeps each bolt focused and testable in isolation.

**Key technical challenges**:
- MIME magic byte validation (security boundary at upload)
- `IStorageService` abstraction must be clean enough for future S3 swap
- Cart merge atomicity (database transaction across guest and user records)

**Bolt Plan**:
- `012-photo-upload-backend` — Upload entity, IStorageService, MIME validation, ImageSharp, preview endpoint, cleanup job (FR-1, FR-2, FR-3, FR-4)
- `013-cart-api` — CartItem entity, CRUD endpoints, price lookup, merge endpoint (FR-5, FR-6)

**Stories** (6):
1. `001-upload-entity-schema.md` — Uploads table + IStorageService interface
2. `002-upload-endpoint.md` — POST /api/uploads with full validation pipeline
3. `003-upload-preview-and-cleanup.md` — Preview endpoint + UploadCleanupJob
4. `004-cart-item-entity.md` — CartItem EF Core entity + migration
5. `005-cart-crud-endpoints.md` — POST/GET/DELETE /api/cart with totals
6. `006-cart-merge-endpoint.md` — POST /api/cart/merge (transactional)

**Depends on**: Bolt 005 (auth-core — JWT + guest token), Bolt 007 (guest-sessions), Bolt 009 (product-catalog-core — Products table for price lookup)

---

## Unit 002: upload-format-cart-ui

**Purpose**: All Angular work for the customer-facing upload experience — dragging photos, watching them upload with progress, selecting a global format and finish, adjusting quantities per photo, reviewing the summary, and managing the cart page.

**Why one bolt?**
These three pages (upload, format-selector, cart) form a tightly coupled user journey where shared state (`UploadService`, `CartService`, `ProductService`) flows through all of them. Bundling them in one bolt avoids partial delivery that leaves the user stuck mid-flow.

**Key technical challenges**:
- HEIC preview via `heic2any` (browser-only, async conversion)
- Quality badge recalculation on format change must be synchronous and reactive
- `CartService` must maintain a single source of truth between localStorage (guest) and server (logged-in) with merge-on-login

**Bolt Plan**:
- `014-upload-format-cart-ui` — All Angular upload + format + cart components (FR-7, FR-8, FR-9)

**Stories** (5):
1. `001-upload-page.md` — Drag-drop zone, HEIC conversion, progress bars, thumbnail grid
2. `002-format-finish-selector.md` — Segmented format control + finish toggle + quality badge recalc
3. `003-order-summary-panel.md` — Live subtotal panel + quantity steppers + add-to-cart CTA
4. `004-cart-page.md` — `/cos` route with cart item list, edit, navigation
5. `005-cart-service.md` — CartService (observable state, localStorage, server sync, badge count)

**Depends on**: Bolt 013 (cart-api), Bolt 011 (product-catalog-ui — shared product models/service)

---

## Unit 003: shipping-and-order-core

**Purpose**: The two foundational backend schemas needed by the payment flow: (a) the Easybox locker catalog with its shipping API, and (b) the Order + OrderItem entity schema with order number generation and status machine. This unit has no payment logic itself — it establishes the domain model that bolts 016 and 017 build on.

**Why one bolt?**
Shipping API and Order entity are both needed before any payment bolt can be built (payment creates an Order, which references a locker or address). They share the same dependency (bolt 013) and can be built together.

**Key technical challenges**:
- Seeding ~200 EasyboxLocker rows in an EF Core migration without bloating migration history
- Order number generation (`FT-YYYYNNNN`) must be concurrency-safe (DB sequence or atomic counter)
- `OrderStatus` state machine must be enforced at the service layer, not the controller

**Bolt Plan**:
- `015-shipping-and-order-core` — Locker catalog + shipping endpoints + Order entity + status machine (FR-10, FR-11, FR-12, FR-13)

**Stories** (4):
1. `001-easybox-locker-catalog.md` — EasyboxLocker entity + seeded migration (~200 records)
2. `002-shipping-endpoints.md` — GET /api/shipping/lockers + /cost + IShippingService
3. `003-order-entity-schema.md` — Order + OrderItem entities + EF migration + order number generation
4. `004-order-status-machine.md` — OrderStatus enum + OrderStatusMachine transitions

**Depends on**: Bolt 013 (cart-api — CartItems needed to create OrderItems from cart)

---

## Unit 004: payment-backends

**Purpose**: The complete backend payment integration: Stripe (PaymentIntent, webhook) and the legacy processor (redirect initiate, IPN callback). Both processors share the `OrderService` which creates Orders from carts and fires post-payment side effects (email trigger, upload association).

**Why one bolt?**
Stripe and the legacy processor share the Order entity, `IOrderService`, and the webhook/IPN pattern. Shared context means they are faster to build together than independently, and the ADR decision to keep them in parallel (same Order model) is easier to enforce in one bolt.

**Key technical challenges**:
- Stripe: raw body must be read before JSON deserialization for webhook signature verification (ASP.NET Core body parsing must be disabled on webhook endpoint)
- the legacy processor: HMAC-MD5 field order is exact and documented — any field ordering mistake breaks all payments
- IPN amount validation: must cross-check IPN amount against stored order amount in RON
- Idempotency: both webhook handlers must check existing order status before applying transition

**Bolt Plan**:
- `016-payment-backends` — IOrderService + Stripe + the legacy processor backends (FR-14, FR-15)

**Stories** (5):
1. `001-order-service.md` — IOrderService: create order from cart snapshot, compute order number
2. `002-stripe-payment-intent.md` — POST /api/payments/stripe/intent
3. `003-stripe-webhook-handler.md` — POST /api/webhooks/stripe + sig + idempotency
4. `004-legacy-processor-initiate.md` — POST /api/payments/legacy-processor/initiate + HMAC-MD5
5. `005-legacy-processor-ipn-handler.md` — POST /api/webhooks/legacy-processor + IPN validation

**Depends on**: Bolt 015 (shipping-and-order-core — Order entity + status machine)

---

## Unit 005: checkout-ui

**Purpose**: The full Angular checkout experience — a 3-step stepper (Delivery → Review → Payment) plus the confirmation page. Consumes the shipping API, cart state, and payment backends. This unit makes the payment backends visible and usable to customers.

**Why one bolt?**
The checkout steps share a single `CheckoutStateService` that carries state (delivery selection, terms acceptance) across routes. Splitting steps across bolts would produce half-working states. The stepper, steps, and confirmation page are delivered together.

**Key technical challenges**:
- Leaflet.js integration in Angular standalone components (needs `ngx-leaflet` or custom wrapper)
- Stripe Elements must be initialized with `clientSecret` just-in-time (before user submits)
- the legacy processor redirect must handle browser back-button case gracefully (order in `AwaitingPayment` state)
- Checkout state must survive browser refresh (sessionStorage backup)
- Confirmation page must handle `?processor=legacy-processor` query param from the legacy processor return URL

**Bolt Plan**:
- `017-checkout-ui` — Full Angular checkout flow (FR-16, FR-17, FR-18)

**Stories** (6):
1. `001-checkout-stepper.md` — CheckoutStepper component + CheckoutStateService
2. `002-delivery-step.md` — Delivery method selection + address form + saved addresses
3. `003-locker-map-component.md` — Leaflet map + city search + locker pin selection
4. `004-order-review-step.md` — Read-only summary + terms checkbox + totals
5. `005-payment-step.md` — Dual payment tabs: Stripe Elements + the legacy processor redirect
6. `006-order-confirmation-page.md` — /comanda/{orderId}/confirmare + stepper + guest CTA
