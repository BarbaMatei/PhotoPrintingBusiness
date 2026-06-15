# Global Story Index

## Overview
- **Total story listings**: 233 (+ 6 intents documented inline without per-story listings)
- **Story files on disk**: 220
- **Implemented**: 80 listings (across 44 shipped bolts) + 8 COMPLETE (intent 024) + 46 GENERATED-but-actually-shipped (early intents 001–003, 005)
- **Intents complete (shipped)**: 001–010, 012–020, 023, 024 (016 VAT/e-Factura + 020 observability shipped via bolts 038/039/044/045; 011 is a non-standard design-review intent)
- **Intents parked**: 021 (Redis multi-replica — deprioritized until deployed + real scaling pressure; bolt 046)
- **Intents planned (inception done, not built)**: 022 (coupons → bolts 047/048) + architect-review 2026-06-03 intents 025–031 + roadmap Phase 3–4 intents 032–033 + research intent 034 (EU expansion study) + tooling intent 035 (bug-hunter agent system) (see below)
- **Bolts planned (not built)**: 046 (parked), 047, 048 (coupons), 054–069 (architect-review 2026-06-03), 070–075 (roadmap Phase 3–4), 076–084 (intent 034 EU-expansion research), 085–094 (intent 035 bug-hunter agent system; 091 ⛔ knowledge-ledger gate, 094 ⏸ adoption-gated). Bolt 050 remains unallocated by design (no directory exists).
- **Last updated**: 2026-06-10
- **Last index change**: 2026-06-10 (added tooling intent 035 bug-hunter-agent-system → 6 units, 42 stories, bolts 085–094 — all status PLANNED / ✅ GENERATED. Builds the bug-hunting agent system from `docs/agent-systems/bug-hunter-build-guide.md`: 5 additive phases + optional tier, 6-slot pipeline (Map→Hunt→Verify→Triage→Report→Learn), read-only on app source. ⚠️ Construction mandate: every component built with the **skill-creator** skill per the guide's build loop. Order: 085→086→087→088→(089 ∥ 090)→091 ⛔(knowledge-ledger gate)→092→093; 094 ⏸ adoption-gated. Note: an earlier same-day inception misread the subject (specsmd-skills port, bolts 085–087) — deleted and replaced by this intent.)
- **Last index change (prior)**: 2026-06-05 (added research-only intent 034 EU-expansion-architecture-study → 3 units, 10 stories, bolts 076–084 — all status PLANNED / ✅ GENERATED. Spike-bolts; zero production code. Source: eu-expansion-research-brief-2026-06-05. Owner Checkpoint-1 decisions: compare both tiers · one brand EU-wide · ship from Romania · local currencies.)
- **Last index change (prior 2)**: 2026-06-05 (added roadmap Phase 3–4 intents 032–033 → 6 units, 25 stories, bolts 070–075 — all status PLANNED / ✅ GENERATED. Source: ai-workflow-review-2026-06-05 §6. 032 builds on bolts 066+062; 033 is infrastructure-readiness only, NOT deployment.)
- **Roadmap Phase 3–4 intents planned**: 032 (regression + comprehensive e2e — Phase 3 stabilize; builds on bolts 066 + 062), 033 (environment triad — Phase 4 infrastructure readiness only, NOT deployment).
- **Roadmap Phase 3–4 bolts planned**: 070–075 (6 bolts → 25 stories).
- **Architect-review (2026-06-03) intents planned**: 025 (security/dependency hygiene), 026 (observability/manifest), 027 (architectural layering), 028 (test architecture), 029 (decomposition/hardening), 030 (UI scaling/e2e), 031 (refund/return). P20 coupon → existing 022 (not re-added).
- **Architect-review bolts planned**: 054–069 (16 bolts → 44 stories)
- **Last index change (prior 3)**: 2026-06-05 (added architect-review-2026-06-03 intents 025–031 → 16 units, 44 stories, bolts 054–069 — all status PLANNED / ✅ GENERATED)
- **Last index change (prior 4)**: 2026-06-02 (drift repair — see notes below)
- **Note**: intent 024 (order-photo archive) shipped 2026-05-30 → 2026-06-01 (bolts 051, 052, 053). Intent 015 (Sameday shipping integration) shipped 2026-06-02 (bolts 036, 037). Story 019-003 superseded → backfill done via intent 024 bolt 051. Bolt 050 is unallocated (no directory exists).
- **Drift-repair note (2026-06-02)**: prior to this edit, the index undercounted ~51 stories. Flipped from `⬜ NOT STARTED` to `✅ IMPLEMENTED`: all stories under intents 004, 010, 012, 015, 023. Added sections for intents 005–009 + 011 which were missing from the index entirely.

---

## Stories by Intent

### 001-foundation-infrastructure

#### Unit: 001-error-handling-logging (5 stories)

### 001-exception-handler-middleware.md ✅ GENERATED
**Title**: Exception handler returns ProblemDetails
**Priority**: Must
**Path**: `intents/001-foundation-infrastructure/units/001-error-handling-logging/stories/001-exception-handler-middleware.md`

### 002-correlation-id-middleware.md ✅ GENERATED
**Title**: Correlation ID tracking per request
**Priority**: Must
**Path**: `intents/001-foundation-infrastructure/units/001-error-handling-logging/stories/002-correlation-id-middleware.md`

### 003-serilog-configuration.md ✅ GENERATED
**Title**: Structured logging with Serilog
**Priority**: Must
**Path**: `intents/001-foundation-infrastructure/units/001-error-handling-logging/stories/003-serilog-configuration.md`

### 004-health-check-endpoint.md ✅ GENERATED
**Title**: Health check endpoint with DB and disk checks
**Priority**: Must
**Path**: `intents/001-foundation-infrastructure/units/001-error-handling-logging/stories/004-health-check-endpoint.md`

### 005-fluentvalidation-integration.md ✅ GENERATED
**Title**: FluentValidation auto-validation with 422 responses
**Priority**: Must
**Path**: `intents/001-foundation-infrastructure/units/001-error-handling-logging/stories/005-fluentvalidation-integration.md`

---

#### Unit: 002-security-baselines (4 stories)

### 001-https-hsts-enforcement.md ✅ GENERATED
**Title**: HTTPS redirect and HSTS header
**Priority**: Must
**Path**: `intents/001-foundation-infrastructure/units/002-security-baselines/stories/001-https-hsts-enforcement.md`

### 002-cors-policy.md ✅ GENERATED
**Title**: CORS exact origin whitelist
**Priority**: Must
**Path**: `intents/001-foundation-infrastructure/units/002-security-baselines/stories/002-cors-policy.md`

### 003-rate-limiting.md ✅ GENERATED
**Title**: Rate limiting middleware
**Priority**: Must
**Path**: `intents/001-foundation-infrastructure/units/002-security-baselines/stories/003-rate-limiting.md`

### 004-security-headers.md ✅ GENERATED
**Title**: Security headers including CSP
**Priority**: Must
**Path**: `intents/001-foundation-infrastructure/units/002-security-baselines/stories/004-security-headers.md`

---

#### Unit: 003-email-infrastructure (3 stories)

### 001-email-service-abstraction.md ✅ GENERATED
**Title**: IEmailService with MailKit and SendGrid implementations
**Priority**: Must
**Path**: `intents/001-foundation-infrastructure/units/003-email-infrastructure/stories/001-email-service-abstraction.md`

### 002-razor-template-rendering.md ✅ GENERATED
**Title**: Razor template engine with shared layout
**Priority**: Must
**Path**: `intents/001-foundation-infrastructure/units/003-email-infrastructure/stories/002-razor-template-rendering.md`

### 003-email-retry-queue.md ✅ GENERATED
**Title**: Database-backed retry queue with exponential backoff
**Priority**: Must
**Path**: `intents/001-foundation-infrastructure/units/003-email-infrastructure/stories/003-email-retry-queue.md`

---

#### Unit: 004-angular-app-shell (5 stories)

### 001-app-shell-layout.md ✅ GENERATED
**Title**: App shell with header, footer, router-outlet
**Priority**: Must
**Path**: `intents/001-foundation-infrastructure/units/004-angular-app-shell/stories/001-app-shell-layout.md`

### 002-lazy-loaded-routes.md ✅ GENERATED
**Title**: Lazy-loaded feature routes with standalone components
**Priority**: Must
**Path**: `intents/001-foundation-infrastructure/units/004-angular-app-shell/stories/002-lazy-loaded-routes.md`

### 003-route-guards.md ✅ GENERATED
**Title**: Auth, Admin, and GuestOrAuth functional guards
**Priority**: Must
**Path**: `intents/001-foundation-infrastructure/units/004-angular-app-shell/stories/003-route-guards.md`

### 004-http-interceptors.md ✅ GENERATED
**Title**: JWT, Guest, and Error interceptors
**Priority**: Must
**Path**: `intents/001-foundation-infrastructure/units/004-angular-app-shell/stories/004-http-interceptors.md`

### 005-environment-config.md ✅ GENERATED
**Title**: Environment files for dev and prod
**Priority**: Must
**Path**: `intents/001-foundation-infrastructure/units/004-angular-app-shell/stories/005-environment-config.md`

---

### 002-authentication

#### Unit: 001-auth-core (7 stories)

### 001-user-registration.md ✅ GENERATED
**Title**: User registration with password hashing and email token
**Priority**: Must
**Path**: `intents/002-authentication/units/001-auth-core/stories/001-user-registration.md`

### 002-email-verification.md ✅ GENERATED
**Title**: Email confirmation token validation and resend
**Priority**: Must
**Path**: `intents/002-authentication/units/001-auth-core/stories/002-email-verification.md`

### 003-jwt-login.md ✅ GENERATED
**Title**: JWT RS256 login with HttpOnly refresh cookie
**Priority**: Must
**Path**: `intents/002-authentication/units/001-auth-core/stories/003-jwt-login.md`

### 004-refresh-token.md ✅ GENERATED
**Title**: Sliding-window refresh token rotation
**Priority**: Must
**Path**: `intents/002-authentication/units/001-auth-core/stories/004-refresh-token.md`

### 005-logout.md ✅ GENERATED
**Title**: Revoke refresh token and clear cookie
**Priority**: Must
**Path**: `intents/002-authentication/units/001-auth-core/stories/005-logout.md`

### 006-account-lockout.md ✅ GENERATED
**Title**: Account lockout after 5 failed login attempts
**Priority**: Must
**Path**: `intents/002-authentication/units/001-auth-core/stories/006-account-lockout.md`

### 007-password-reset.md ✅ GENERATED
**Title**: Forgot password and reset via email token
**Priority**: Must
**Path**: `intents/002-authentication/units/001-auth-core/stories/007-password-reset.md`

---

#### Unit: 002-social-auth (2 stories)

### 001-google-token-validation.md ✅ GENERATED
**Title**: Google id_token server-side validation
**Priority**: Must
**Path**: `intents/002-authentication/units/002-social-auth/stories/001-google-token-validation.md`

### 002-account-upsert-linking.md ✅ GENERATED
**Title**: Google OAuth user upsert and account auto-linking
**Priority**: Must
**Path**: `intents/002-authentication/units/002-social-auth/stories/002-account-upsert-linking.md`

---

#### Unit: 003-guest-sessions (3 stories)

### 001-guest-session-create.md ✅ GENERATED
**Title**: Guest session creation with X-Guest-Token
**Priority**: Must
**Path**: `intents/002-authentication/units/003-guest-sessions/stories/001-guest-session-create.md`

### 002-guest-session-claim.md ✅ GENERATED
**Title**: Transfer guest orders to registered account
**Priority**: Must
**Path**: `intents/002-authentication/units/003-guest-sessions/stories/002-guest-session-claim.md`

### 003-guest-session-cleanup.md ✅ GENERATED
**Title**: Background job to clean up expired orphaned sessions
**Priority**: Must
**Path**: `intents/002-authentication/units/003-guest-sessions/stories/003-guest-session-cleanup.md`

---

#### Unit: 004-authentication-ui (7 stories)

### 001-register-page.md ✅ GENERATED
**Title**: Registration page with password strength and GDPR consent
**Priority**: Must
**Path**: `intents/002-authentication/units/004-authentication-ui/stories/001-register-page.md`

### 002-email-verification-pending.md ✅ GENERATED
**Title**: Email verification pending page with resend
**Priority**: Must
**Path**: `intents/002-authentication/units/004-authentication-ui/stories/002-email-verification-pending.md`

### 003-login-page.md ✅ GENERATED
**Title**: Login page with redirect-back and error mapping
**Priority**: Must
**Path**: `intents/002-authentication/units/004-authentication-ui/stories/003-login-page.md`

### 004-google-auth-button.md ✅ GENERATED
**Title**: Google Identity Services button component
**Priority**: Must
**Path**: `intents/002-authentication/units/004-authentication-ui/stories/004-google-auth-button.md`

### 005-guest-checkout-prompt.md ✅ GENERATED
**Title**: Guest checkout prompt modal with guest form
**Priority**: Must
**Path**: `intents/002-authentication/units/004-authentication-ui/stories/005-guest-checkout-prompt.md`

### 006-forgot-password-page.md ✅ GENERATED
**Title**: Forgot password page (anti-enumeration)
**Priority**: Must
**Path**: `intents/002-authentication/units/004-authentication-ui/stories/006-forgot-password-page.md`

### 007-reset-password-page.md ✅ GENERATED
**Title**: Reset password page (reads token from query params)
**Priority**: Must
**Path**: `intents/002-authentication/units/004-authentication-ui/stories/007-reset-password-page.md`

---

### 003-product-catalog

#### Unit: 001-product-catalog-core (7 stories)

### 001-product-entity-schema.md ✅ GENERATED
**Title**: Product, size, finish, and pricing tier DB schema
**Priority**: Must
**Path**: `intents/003-product-catalog/units/001-product-catalog-core/stories/001-product-entity-schema.md`

### 002-quantity-tiered-pricing.md ✅ GENERATED
**Title**: Quantity-tiered pricing storage and lookup
**Priority**: Must
**Path**: `intents/003-product-catalog/units/001-product-catalog-core/stories/002-quantity-tiered-pricing.md`

### 003-public-catalog-endpoint.md ✅ GENERATED
**Title**: Public catalog endpoint GET /api/products
**Priority**: Must
**Path**: `intents/003-product-catalog/units/001-product-catalog-core/stories/003-public-catalog-endpoint.md`

### 004-product-detail-endpoint.md ✅ GENERATED
**Title**: Public product detail endpoint GET /api/products/{id}
**Priority**: Must
**Path**: `intents/003-product-catalog/units/001-product-catalog-core/stories/004-product-detail-endpoint.md`

### 005-price-calculation-endpoint.md ✅ GENERATED
**Title**: Server-side price calculation endpoint
**Priority**: Must
**Path**: `intents/003-product-catalog/units/001-product-catalog-core/stories/005-price-calculation-endpoint.md`

### 006-admin-product-management.md ✅ GENERATED
**Title**: Admin CRUD endpoints for products and size variants
**Priority**: Must
**Path**: `intents/003-product-catalog/units/001-product-catalog-core/stories/006-admin-product-management.md`

### 007-admin-pricing-management.md ✅ GENERATED
**Title**: Admin atomic pricing tier replace with validation
**Priority**: Must
**Path**: `intents/003-product-catalog/units/001-product-catalog-core/stories/007-admin-pricing-management.md`

---

#### Unit: 002-product-catalog-ui (3 stories)

### 001-product-catalog-page.md ✅ GENERATED
**Title**: Angular /tipareste catalog grid with product cards
**Priority**: Must
**Path**: `intents/003-product-catalog/units/002-product-catalog-ui/stories/001-product-catalog-page.md`

### 002-format-selection-price-calculator.md ✅ GENERATED
**Title**: Format selector with client-side tier price calculation
**Priority**: Must
**Path**: `intents/003-product-catalog/units/002-product-catalog-ui/stories/002-format-selection-price-calculator.md`

### 003-admin-product-management-ui.md ✅ GENERATED
**Title**: Admin product catalog management dashboard
**Priority**: Must
**Path**: `intents/003-product-catalog/units/002-product-catalog-ui/stories/003-admin-product-management-ui.md`

---

---

### 004-checkout-payment

#### Unit: 001-upload-and-cart-backend (6 stories)

### 001-upload-entity-schema.md ✅ IMPLEMENTED
**Title**: Upload entity, IStorageService, and storage path convention
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/001-upload-and-cart-backend/stories/001-upload-entity-schema.md`
**Bolt**: 012 | **Epic story**: US-202

### 002-upload-endpoint.md ✅ IMPLEMENTED
**Title**: POST /api/uploads with MIME validation, ImageSharp, and rate limiting
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/001-upload-and-cart-backend/stories/002-upload-endpoint.md`
**Bolt**: 012 | **Epic story**: US-202

### 003-upload-preview-and-cleanup.md ✅ IMPLEMENTED
**Title**: Upload preview endpoint and hourly cleanup background job
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/001-upload-and-cart-backend/stories/003-upload-preview-and-cleanup.md`
**Bolt**: 012 | **Epic story**: US-202

### 004-cart-item-entity.md ✅ IMPLEMENTED
**Title**: CartItem EF Core entity and migration
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/001-upload-and-cart-backend/stories/004-cart-item-entity.md`
**Bolt**: 013 | **Epic story**: US-206

### 005-cart-crud-endpoints.md ✅ IMPLEMENTED
**Title**: POST/GET/DELETE /api/cart with computed totals
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/001-upload-and-cart-backend/stories/005-cart-crud-endpoints.md`
**Bolt**: 013 | **Epic story**: US-206

### 006-cart-merge-endpoint.md ✅ IMPLEMENTED
**Title**: POST /api/cart/merge — transactional guest-to-user cart merge
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/001-upload-and-cart-backend/stories/006-cart-merge-endpoint.md`
**Bolt**: 013 | **Epic story**: US-206

---

#### Unit: 002-upload-format-cart-ui (5 stories)

### 001-upload-page.md ✅ IMPLEMENTED
**Title**: Drag-and-drop upload page with progress bars and thumbnail grid
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/002-upload-format-cart-ui/stories/001-upload-page.md`
**Bolt**: 014 | **Epic story**: US-201

### 002-format-finish-selector.md ✅ IMPLEMENTED
**Title**: Global format/finish selector with reactive quality badge recalculation
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/002-upload-format-cart-ui/stories/002-format-finish-selector.md`
**Bolt**: 014 | **Epic story**: US-203

### 003-order-summary-panel.md ✅ IMPLEMENTED
**Title**: Sticky live order summary panel with quantity steppers and add-to-cart CTA
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/002-upload-format-cart-ui/stories/003-order-summary-panel.md`
**Bolt**: 014 | **Epic story**: US-203

### 004-cart-page.md ✅ IMPLEMENTED
**Title**: Cart page /cos with item list, edit controls, and navigation
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/002-upload-format-cart-ui/stories/004-cart-page.md`
**Bolt**: 014 | **Epic story**: US-205

### 005-cart-service.md ✅ IMPLEMENTED
**Title**: CartService with localStorage/server sync, merge on login, and item count badge
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/002-upload-format-cart-ui/stories/005-cart-service.md`
**Bolt**: 014 | **Epic story**: US-205

---

#### Unit: 003-shipping-and-order-core (4 stories)

### 001-easybox-locker-catalog.md ✅ IMPLEMENTED
**Title**: EasyboxLocker entity and seeded migration with ~200 Romanian lockers
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/003-shipping-and-order-core/stories/001-easybox-locker-catalog.md`
**Bolt**: 015 | **Epic story**: US-302

### 002-shipping-endpoints.md ✅ IMPLEMENTED
**Title**: GET /api/shipping/lockers and /cost endpoints with IShippingService abstraction
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/003-shipping-and-order-core/stories/002-shipping-endpoints.md`
**Bolt**: 015 | **Epic story**: US-302

### 003-order-entity-schema.md ✅ IMPLEMENTED
**Title**: Order and OrderItem entities with JSONB fields, enums, and FT-YYYYNNNN order number
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/003-shipping-and-order-core/stories/003-order-entity-schema.md`
**Bolt**: 015 | **Epic story**: US-305

### 004-order-status-machine.md ✅ IMPLEMENTED
**Title**: OrderStatus enum and OrderStatusMachine valid transition enforcement
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/003-shipping-and-order-core/stories/004-order-status-machine.md`
**Bolt**: 015 | **Epic story**: US-305

---

#### Unit: 004-payment-backends (5 stories)

### 001-order-service.md ✅ IMPLEMENTED
**Title**: IOrderService — create order from cart with pricing snapshot and order number generation
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/004-payment-backends/stories/001-order-service.md`
**Bolt**: 016 | **Epic story**: US-305

### 002-stripe-payment-intent.md ✅ IMPLEMENTED
**Title**: POST /api/payments/stripe/intent — PaymentIntent creation and pending order
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/004-payment-backends/stories/002-stripe-payment-intent.md`
**Bolt**: 016 | **Epic story**: US-305

### 003-stripe-webhook-handler.md ✅ IMPLEMENTED
**Title**: POST /api/webhooks/stripe — signature verification, idempotency, order status transitions
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/004-payment-backends/stories/003-stripe-webhook-handler.md`
**Bolt**: 016 | **Epic story**: US-305

### 004-legacy-processor-initiate.md ✅ IMPLEMENTED
**Title**: POST /api/payments/legacy-processor/initiate — HMAC-MD5 signed redirect URL generation
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/004-payment-backends/stories/004-legacy-processor-initiate.md`
**Bolt**: 016 | **Epic story**: US-306

### 005-legacy-processor-ipn-handler.md ✅ IMPLEMENTED
**Title**: POST /api/webhooks/legacy-processor — IPN validation, amount check, the legacy processor spec response
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/004-payment-backends/stories/005-legacy-processor-ipn-handler.md`
**Bolt**: 016 | **Epic story**: US-306

---

#### Unit: 005-checkout-ui (6 stories)

### 001-checkout-stepper.md ✅ IMPLEMENTED
**Title**: Checkout stepper component and CheckoutStateService with sessionStorage persistence
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/005-checkout-ui/stories/001-checkout-stepper.md`
**Bolt**: 017 | **Epic story**: US-301

### 002-delivery-step.md ✅ IMPLEMENTED
**Title**: Delivery method selection step — Easybox cards and home delivery address form
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/005-checkout-ui/stories/002-delivery-step.md`
**Bolt**: 017 | **Epic story**: US-301

### 003-locker-map-component.md ✅ IMPLEMENTED
**Title**: Leaflet.js locker map with city search, pin rendering, and locker selection
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/005-checkout-ui/stories/003-locker-map-component.md`
**Bolt**: 017 | **Epic story**: US-301

### 004-order-review-step.md ✅ IMPLEMENTED
**Title**: Order review step with read-only summary, grand total, and terms acceptance gate
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/005-checkout-ui/stories/004-order-review-step.md`
**Bolt**: 017 | **Epic story**: US-303

### 005-payment-step.md ✅ IMPLEMENTED
**Title**: Payment step with Stripe Elements tab and the legacy processor redirect tab
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/005-checkout-ui/stories/005-payment-step.md`
**Bolt**: 017 | **Epic story**: US-304

### 006-order-confirmation-page.md ✅ IMPLEMENTED
**Title**: Order confirmation page with status stepper, guest CTA, and cart state reset
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/005-checkout-ui/stories/006-order-confirmation-page.md`
**Bolt**: 017 | **Epic story**: US-307

---

### 005-order-management

> Bolts: 018 (orders-api) ✅ IMPLEMENTED · 019 (orders-ui) ✅ IMPLEMENTED. Five stories on disk.

#### Unit: 001-orders-api (2 stories) — Bolt: 018

### 001-orders-list-endpoint.md ✅ IMPLEMENTED
**Title**: Paginated `GET /api/orders` with `X-Total-Count`
**Priority**: Must
**Path**: `intents/005-order-management/units/001-orders-api/stories/001-orders-list-endpoint.md`
**Bolt**: 018

### 002-order-detail-endpoint.md ✅ IMPLEMENTED
**Title**: Ownership-checked `GET /api/orders/{id}` with full DTOs
**Priority**: Must
**Path**: `intents/005-order-management/units/001-orders-api/stories/002-order-detail-endpoint.md`
**Bolt**: 018

#### Unit: 002-orders-ui (3 stories) — Bolt: 019

### 003-order-status-pipe.md ✅ IMPLEMENTED
**Title**: `OrderStatusPipe` + `STATUS_ORDER` constants
**Priority**: Must
**Path**: `intents/005-order-management/units/002-orders-ui/stories/003-order-status-pipe.md`
**Bolt**: 019

### 001-order-history-page.md ✅ IMPLEMENTED
**Title**: Paginated order list at `/comenzi`
**Priority**: Must
**Path**: `intents/005-order-management/units/002-orders-ui/stories/001-order-history-page.md`
**Bolt**: 019

### 002-order-detail-page.md ✅ IMPLEMENTED
**Title**: Full order detail at `/comenzi/:id`
**Priority**: Must
**Path**: `intents/005-order-management/units/002-orders-ui/stories/002-order-detail-page.md`
**Bolt**: 019

---

### 006-email-notifications

> Bolt: 020 (transactional-emails) ✅ IMPLEMENTED. No story files on disk — bolt was built directly from the intent brief (`intents/006-email-notifications/`). Records 4 transactional flows: registration confirmation, password reset, order confirmation, shipping notification.

---

### 007-admin-panel

> Bolts: 021 (admin-api) ✅ IMPLEMENTED · 022 (admin-ui) ✅ IMPLEMENTED. No story files on disk — bolts built directly from the intent brief (`intents/007-admin-panel/`). Covers admin order management, product CRUD, dashboard metrics.

---

### 008-user-account

> Bolts: 023 (account-api) ✅ IMPLEMENTED · 024 (account-ui) ✅ IMPLEMENTED. No story files on disk — bolts built directly from the intent brief (`intents/008-user-account/`). Covers profile editing, password change, address book.

---

### 009-background-jobs

> Bolt: 025 (background-jobs) ✅ IMPLEMENTED. No story files on disk — bolt built directly from the intent brief (`intents/009-background-jobs/`). Covers guest-session cleanup, expired-upload cleanup, retention sweeps.

---

### 011-web-design-review

> No bolts directly attached — this intent captured findings that fed into intent 012 (ui-polish, bolts 027–032 ✅ all IMPLEMENTED). No story files on disk; the artefact is the review document itself.

---

## Stories by Status

(counts from the per-story listing lines)

- **✅ IMPLEMENTED**: 80
- **✅ COMPLETE** (intent 024 specifically): 8
- **✅ GENERATED**: 46 — these are intent 001/002/003 stories that *are* implemented but the index was authored before that and never reclassified; treat as IMPLEMENTED
- **⬜ NOT STARTED**: 20 (intents 016, 020, 021, 022 — 7 planned bolts)
- **♻️ SUPERSEDED**: 1 (story 019-003 → folded into intent 024 bolt 051)

---

### 010-photo-lightbox

#### Unit: 001-photo-lightbox-ui (2 stories)

### 001-photo-lightbox-component.md ✅ IMPLEMENTED
**Title**: Photo lightbox overlay component
**Priority**: Must
**Path**: `intents/010-photo-lightbox/units/001-photo-lightbox-ui/stories/001-photo-lightbox-component.md`
**Bolt**: 026

### 002-thumbnail-click-integration.md ✅ IMPLEMENTED
**Title**: Wire thumbnail click to open lightbox in format-selector
**Priority**: Must
**Path**: `intents/010-photo-lightbox/units/001-photo-lightbox-ui/stories/002-thumbnail-click-integration.md`
**Bolt**: 026

---

### 012-ui-polish

> Source: May 2026 live web design review. All issues are P2/P3 Angular/SCSS frontend — no backend changes.

#### Unit: 001-auth-scss-refactor (2 stories) — Bolt: 027

### 001-extract-auth-shared-styles.md ✅ IMPLEMENTED
**Title**: Extract shared auth layout styles into `_auth-forms.scss` partial
**Priority**: Must
**Path**: `intents/012-ui-polish/units/001-auth-scss-refactor/stories/001-extract-auth-shared-styles.md`
**Bolt**: 027

### 002-remove-local-spinner-animation.md ✅ IMPLEMENTED
**Title**: Remove local `.spinner` CSS from register page; confirm `<app-spinner>` usage
**Priority**: Must
**Path**: `intents/012-ui-polish/units/001-auth-scss-refactor/stories/002-remove-local-spinner-animation.md`
**Bolt**: 027

---

#### Unit: 002-shared-components-adoption (4 stories) — Bolt: 028

### 001-audit-pages-for-inline-loading.md ✅ IMPLEMENTED
**Title**: Audit all feature pages for inline loading/empty-state patterns
**Priority**: Must
**Path**: `intents/012-ui-polish/units/002-shared-components-adoption/stories/001-audit-pages-for-inline-loading.md`
**Bolt**: 028

### 002-replace-inline-patterns-admin.md ✅ IMPLEMENTED
**Title**: Replace inline loading/empty patterns in admin pages with shared components
**Priority**: Must
**Path**: `intents/012-ui-polish/units/002-shared-components-adoption/stories/002-replace-inline-patterns-admin.md`
**Bolt**: 028

### 003-replace-inline-patterns-catalog.md ✅ IMPLEMENTED
**Title**: Replace inline loading/empty patterns in product catalog pages
**Priority**: Must
**Path**: `intents/012-ui-polish/units/002-shared-components-adoption/stories/003-replace-inline-patterns-catalog.md`
**Bolt**: 028

### 004-replace-inline-patterns-profile-cart.md ✅ IMPLEMENTED
**Title**: Replace inline loading/empty patterns in profile and cart pages
**Priority**: Must
**Path**: `intents/012-ui-polish/units/002-shared-components-adoption/stories/004-replace-inline-patterns-profile-cart.md`
**Bolt**: 028

---

#### Unit: 003-global-ui-primitives (4 stories) — Bolts: 029, 030

### 001-create-buttons-partial.md ✅ IMPLEMENTED
**Title**: Create `_buttons.scss` global partial with all button variants
**Priority**: Should
**Path**: `intents/012-ui-polish/units/003-global-ui-primitives/stories/001-create-buttons-partial.md`
**Bolt**: 029

### 002-remove-local-btn-definitions.md ✅ IMPLEMENTED
**Title**: Remove duplicate `.btn` definitions from all feature SCSS files
**Priority**: Should
**Path**: `intents/012-ui-polish/units/003-global-ui-primitives/stories/002-remove-local-btn-definitions.md`
**Bolt**: 029

### 003-breadcrumb-standalone-component.md ✅ IMPLEMENTED
**Title**: Create reusable `BreadcrumbComponent` with `title` and `backLink` inputs
**Priority**: Could
**Path**: `intents/012-ui-polish/units/003-global-ui-primitives/stories/003-breadcrumb-standalone-component.md`
**Bolt**: 030

### 004-wire-breadcrumb-admin-order-detail.md ✅ IMPLEMENTED
**Title**: Replace inline breadcrumb in admin-order-detail-page with `<app-breadcrumb>`
**Priority**: Could
**Path**: `intents/012-ui-polish/units/003-global-ui-primitives/stories/004-wire-breadcrumb-admin-order-detail.md`
**Bolt**: 030

---

#### Unit: 004-responsive-ux-fixes (3 stories) — Bolts: 031, 032

### 001-show-hamburger-at-md-breakpoint.md ✅ IMPLEMENTED
**Title**: Show header hamburger at 768px so tablet users have navigation
**Priority**: Should
**Path**: `intents/012-ui-polish/units/004-responsive-ux-fixes/stories/001-show-hamburger-at-md-breakpoint.md`
**Bolt**: 031

### 002-extract-password-checklist-component.md ✅ IMPLEMENTED
**Title**: Extract register page password checklist into shared `PasswordChecklistComponent`
**Priority**: Could
**Path**: `intents/012-ui-polish/units/004-responsive-ux-fixes/stories/002-extract-password-checklist-component.md`
**Bolt**: 032

### 003-wire-checklist-profile-page.md ✅ IMPLEMENTED
**Title**: Add `<app-password-checklist>` to profile change-password form
**Priority**: Could
**Path**: `intents/012-ui-polish/units/004-responsive-ux-fixes/stories/003-wire-checklist-profile-page.md`
**Bolt**: 032

---

> Intents 013–022 generated from `docs/architecture-analysis-2026-05-25.md`.
> Total new stories: 51 · New bolts: 033–048 · Last updated: 2026-05-25T10:50:00Z

### 013-upload-cleanup-fix

#### Unit: 001-upload-cleanup-job-fix (3 stories) — Bolt: 033

### 001-skip-referenced-uploads.md ✅ IMPLEMENTED
**Title**: Cleanup query excludes cart/order-referenced uploads
**Priority**: Must
**Path**: `intents/013-upload-cleanup-fix/units/001-upload-cleanup-job-fix/stories/001-skip-referenced-uploads.md`
**Bolt**: 033

### 002-retention-config.md ✅ IMPLEMENTED
**Title**: UploadCleanupSettings options class with orphan + referenced retention windows
**Priority**: Must
**Path**: `intents/013-upload-cleanup-fix/units/001-upload-cleanup-job-fix/stories/002-retention-config.md`
**Bolt**: 033

### 003-cleanup-regression-test.md ✅ IMPLEMENTED
**Title**: Unit test — referenced upload survives cleanup tick (real DbContext, mocked storage)
**Priority**: Must
**Path**: `intents/013-upload-cleanup-fix/units/001-upload-cleanup-job-fix/stories/003-cleanup-regression-test.md`
**Bolt**: 033

---

### 014-payment-hardening

#### Unit: 001-shipping-cost-server-side (2 stories) — Bolt: 034

### 001-remove-client-shipping-cost.md ✅ IMPLEMENTED
**Title**: Drop `ShippingCostRon` from DTO; resolve server-side
**Priority**: Must
**Path**: `intents/014-payment-hardening/units/001-shipping-cost-server-side/stories/001-remove-client-shipping-cost.md`
**Bolt**: 034

### 002-create-order-validator.md ✅ IMPLEMENTED
**Title**: FluentValidation rules for delivery-type conditional fields
**Priority**: Must
**Path**: `intents/014-payment-hardening/units/001-shipping-cost-server-side/stories/002-create-order-validator.md`
**Bolt**: 034

#### Unit: 002-payment-idempotency (3 stories) — Bolt: 035

### 001-idempotency-key-migration.md ✅ IMPLEMENTED
**Title**: Add nullable `Orders.IdempotencyKey` + partial unique index
**Priority**: Must
**Path**: `intents/014-payment-hardening/units/002-payment-idempotency/stories/001-idempotency-key-migration.md`
**Bolt**: 035

### 002-stripe-intent-idempotency.md ✅ IMPLEMENTED
**Title**: Wire idempotency to Stripe intent endpoint + SDK request options
**Priority**: Must
**Path**: `intents/014-payment-hardening/units/002-payment-idempotency/stories/002-stripe-intent-idempotency.md`
**Bolt**: 035

### 003-legacy-processor-initiate-idempotency.md ✅ IMPLEMENTED
**Title**: Reuse persisted the legacy processor redirect URL on repeat calls
**Priority**: Must
**Path**: `intents/014-payment-hardening/units/002-payment-idempotency/stories/003-legacy-processor-initiate-idempotency.md`
**Bolt**: 035

---

### 015-sameday-shipping-integration

#### Unit: 001-sameday-api-client (3 stories) — Bolt: 036

### 001-sameday-settings-and-typed-client.md ✅ IMPLEMENTED
**Title**: SamedaySettings, typed HTTP client, Polly retry + rate-limit policies
**Priority**: Must
**Path**: `intents/015-sameday-shipping-integration/units/001-sameday-api-client/stories/001-sameday-settings-and-typed-client.md`
**Bolt**: 036

### 002-token-auth-and-refresh.md ✅ IMPLEMENTED
**Title**: Token endpoint authentication + 401-retry-once refresh
**Priority**: Must
**Path**: `intents/015-sameday-shipping-integration/units/001-sameday-api-client/stories/002-token-auth-and-refresh.md`
**Bolt**: 036

### 003-sameday-schema-additions.md ✅ IMPLEMENTED
**Title**: EF migration adding `AwbLabelUrl` + `LastTrackingSyncAt` to Orders
**Priority**: Must
**Path**: `intents/015-sameday-shipping-integration/units/001-sameday-api-client/stories/003-sameday-schema-additions.md`
**Bolt**: 036

#### Unit: 002-awb-and-tracking-jobs (3 stories) — Bolt: 037

### 001-awb-creation-on-paid.md ✅ IMPLEMENTED
**Title**: Auto-create AWB when order transitions to Paid
**Priority**: Must
**Path**: `intents/015-sameday-shipping-integration/units/002-awb-and-tracking-jobs/stories/001-awb-creation-on-paid.md`
**Bolt**: 037

### 002-awb-retry-job.md ✅ IMPLEMENTED
**Title**: BackgroundService retries failed AWB creations hourly with cap
**Priority**: Must
**Path**: `intents/015-sameday-shipping-integration/units/002-awb-and-tracking-jobs/stories/002-awb-retry-job.md`
**Bolt**: 037

### 003-shipment-tracking-job.md ✅ IMPLEMENTED
**Title**: Background polling auto-transitions Shipped → Delivered
**Priority**: Should
**Path**: `intents/015-sameday-shipping-integration/units/002-awb-and-tracking-jobs/stories/003-shipment-tracking-job.md`
**Bolt**: 037

---

### 016-romanian-vat-efactura

#### Unit: 001-vat-calculation (2 stories) — Bolt: 038

### 001-vat-fields-and-computation.md ⬜ NOT STARTED
**Title**: Schema additions + VAT computed on order creation
**Priority**: Must
**Path**: `intents/016-romanian-vat-efactura/units/001-vat-calculation/stories/001-vat-fields-and-computation.md`
**Bolt**: 038

### 002-invoice-entity-and-numbering.md ⬜ NOT STARTED
**Title**: Invoice entity + Postgres sequence per series per year
**Priority**: Must
**Path**: `intents/016-romanian-vat-efactura/units/001-vat-calculation/stories/002-invoice-entity-and-numbering.md`
**Bolt**: 038

#### Unit: 002-efactura-generation-and-anaf (4 stories) — Bolt: 039

### 001-ubl-xml-builder.md ⬜ NOT STARTED
**Title**: UBL 2.1 + CIUS-RO compliant XML builder
**Priority**: Must
**Path**: `intents/016-romanian-vat-efactura/units/002-efactura-generation-and-anaf/stories/001-ubl-xml-builder.md`
**Bolt**: 039

### 002-anaf-spv-client.md ⬜ NOT STARTED
**Title**: ANAF SPV OAuth + upload + status-check client + retry job
**Priority**: Must
**Path**: `intents/016-romanian-vat-efactura/units/002-efactura-generation-and-anaf/stories/002-anaf-spv-client.md`
**Bolt**: 039

### 003-invoice-pdf-renderer-and-endpoint.md ⬜ NOT STARTED
**Title**: PDF rendering + customer endpoint + email attachment
**Priority**: Must
**Path**: `intents/016-romanian-vat-efactura/units/002-efactura-generation-and-anaf/stories/003-invoice-pdf-renderer-and-endpoint.md`
**Bolt**: 039

### 004-admin-invoice-list-and-retry.md ⬜ NOT STARTED
**Title**: Admin invoice list + retry failed ANAF uploads + XML download
**Priority**: Should
**Path**: `intents/016-romanian-vat-efactura/units/002-efactura-generation-and-anaf/stories/004-admin-invoice-list-and-retry.md`
**Bolt**: 039

---

### 017-deployment-cicd

#### Unit: 001-containers-and-pipelines (6 stories) — Bolt: 040

### 001-api-dockerfile.md ✅ IMPLEMENTED
**Title**: Multi-stage Dockerfile with non-root user and HEALTHCHECK
**Priority**: Must
**Path**: `intents/017-deployment-cicd/units/001-containers-and-pipelines/stories/001-api-dockerfile.md`
**Bolt**: 040

### 002-docker-compose-dev.md ✅ IMPLEMENTED
**Title**: Compose for API + Postgres + MailHog
**Priority**: Must
**Path**: `intents/017-deployment-cicd/units/001-containers-and-pipelines/stories/002-docker-compose-dev.md`
**Bolt**: 040

### 003-docker-compose-prod-caddy.md ✅ IMPLEMENTED
**Title**: Production compose + Caddy reverse proxy with Let's Encrypt
**Priority**: Must
**Path**: `intents/017-deployment-cicd/units/001-containers-and-pipelines/stories/003-docker-compose-prod-caddy.md`
**Bolt**: 040

### 004-github-actions-ci.md ✅ IMPLEMENTED
**Title**: CI workflow — restore, build, test, artefacts
**Priority**: Must
**Path**: `intents/017-deployment-cicd/units/001-containers-and-pipelines/stories/004-github-actions-ci.md`
**Bolt**: 040

### 005-github-actions-deploy.md ✅ IMPLEMENTED
**Title**: CD workflow — tag image, push GHCR, deploy
**Priority**: Must
**Path**: `intents/017-deployment-cicd/units/001-containers-and-pipelines/stories/005-github-actions-deploy.md`
**Bolt**: 040

### 006-env-vars-matrix.md ✅ IMPLEMENTED
**Title**: `.env.example` + README env-matrix + ValidateOnStart wiring
**Priority**: Must
**Path**: `intents/017-deployment-cicd/units/001-containers-and-pipelines/stories/006-env-vars-matrix.md`
**Bolt**: 040

---

### 018-secrets-management

#### Unit: 001-secrets-rotation-and-guardrails (5 stories) — Bolt: 041

### 001-rotate-jwt-keypair.md ✅ IMPLEMENTED
**Title**: Generate + rotate keys across environments
**Priority**: Must
**Path**: `intents/018-secrets-management/units/001-secrets-rotation-and-guardrails/stories/001-rotate-jwt-keypair.md`
**Bolt**: 041

### 002-remove-key-from-repo.md ✅ IMPLEMENTED
**Title**: Empty `appsettings.Development.json` key value + user-secrets workflow
**Priority**: Must
**Path**: `intents/018-secrets-management/units/001-secrets-rotation-and-guardrails/stories/002-remove-key-from-repo.md`
**Bolt**: 041

### 003-gitignore-and-secrets-dir.md ✅ IMPLEMENTED
**Title**: `.gitignore` discipline + `secrets/.gitkeep`
**Priority**: Must
**Path**: `intents/018-secrets-management/units/001-secrets-rotation-and-guardrails/stories/003-gitignore-and-secrets-dir.md`
**Bolt**: 041

### 004-precommit-and-ci-scan.md ✅ IMPLEMENTED
**Title**: Pre-commit hook + Gitleaks CI job
**Priority**: Must
**Path**: `intents/018-secrets-management/units/001-secrets-rotation-and-guardrails/stories/004-precommit-and-ci-scan.md`
**Bolt**: 041

### 005-history-rewrite-decision.md ✅ IMPLEMENTED
**Title**: Decide and record history-rewrite vs. accept-leak in `decision-index.md`
**Priority**: Must
**Path**: `intents/018-secrets-management/units/001-secrets-rotation-and-guardrails/stories/005-history-rewrite-decision.md`
**Bolt**: 041

---

### 019-thumbnail-cache-and-cloud-storage

#### Unit: 001-thumbnail-cache (3 stories) — Bolt: 042

### 001-thumbnail-path-schema.md ✅ IMPLEMENTED
**Title**: EF migration adds `Uploads.ThumbnailPath`
**Priority**: Must
**Path**: `intents/019-thumbnail-cache-and-cloud-storage/units/001-thumbnail-cache/stories/001-thumbnail-path-schema.md`
**Bolt**: 042

### 002-persist-thumbnail-on-first-request.md ✅ IMPLEMENTED
**Title**: First preview persists thumbnail; later requests stream cached file
**Priority**: Must
**Path**: `intents/019-thumbnail-cache-and-cloud-storage/units/001-thumbnail-cache/stories/002-persist-thumbnail-on-first-request.md`
**Bolt**: 042

### 003-imagesharp-max-pixels.md ✅ IMPLEMENTED
**Title**: Configure ImageSharp `MaxImageWidth/Height` (decomp-bomb defence)
**Priority**: Must
**Path**: `intents/019-thumbnail-cache-and-cloud-storage/units/001-thumbnail-cache/stories/003-imagesharp-max-pixels.md`
**Bolt**: 042

#### Unit: 002-cloud-storage-provider (3 stories) — Bolt: 043 (001+002); story 003 superseded → intent 024

### 001-s3-storage-service.md ✅ IMPLEMENTED
**Title**: `S3StorageService : IStorageService` against AWS SDK
**Priority**: Must
**Path**: `intents/019-thumbnail-cache-and-cloud-storage/units/002-cloud-storage-provider/stories/001-s3-storage-service.md`
**Bolt**: 043 ✅ IMPLEMENTED

### 002-preview-redirect-presigned-url.md ✅ IMPLEMENTED
**Title**: 302 redirect to pre-signed URL on cloud provider
**Priority**: Must
**Path**: `intents/019-thumbnail-cache-and-cloud-storage/units/002-cloud-storage-provider/stories/002-preview-redirect-presigned-url.md`
**Bolt**: 043 ✅ IMPLEMENTED

### 003-local-to-cloud-migration-tool.md ♻️ SUPERSEDED
**Title**: Resumable `migrate-storage` console command
**Priority**: Should
**Path**: `intents/019-thumbnail-cache-and-cloud-storage/units/002-cloud-storage-provider/stories/003-local-to-cloud-migration-tool.md`
**Bolt**: — *(retired; superseded by intent 024 story 004-backfill-paid-orders)*

---

### 020-observability-stack

#### Unit: 001-tracing-and-metrics (3 stories) — Bolt: 044

### 001-otel-tracing-instrumentation.md ⬜ NOT STARTED
**Title**: OpenTelemetry SDK + ASP.NET / HttpClient / EF Core instrumentation
**Priority**: Should
**Path**: `intents/020-observability-stack/units/001-tracing-and-metrics/stories/001-otel-tracing-instrumentation.md`
**Bolt**: 044

### 002-business-metrics-and-prometheus.md ⬜ NOT STARTED
**Title**: Define business counters/histograms and expose `/metrics`
**Priority**: Should
**Path**: `intents/020-observability-stack/units/001-tracing-and-metrics/stories/002-business-metrics-and-prometheus.md`
**Bolt**: 044

### 003-per-route-sampling.md ⬜ NOT STARTED
**Title**: Per-route OTel sampler (5 % for hot read endpoints)
**Priority**: Should
**Path**: `intents/020-observability-stack/units/001-tracing-and-metrics/stories/003-per-route-sampling.md`
**Bolt**: 044

#### Unit: 002-error-tracking-and-slos (2 stories) — Bolt: 045

### 001-sentry-aspnet-integration.md ⬜ NOT STARTED
**Title**: Sentry SDK with correlation + release tagging + PII scrubbing
**Priority**: Must
**Path**: `intents/020-observability-stack/units/002-error-tracking-and-slos/stories/001-sentry-aspnet-integration.md`
**Bolt**: 045

### 002-slo-documentation-and-dashboard.md ⬜ NOT STARTED
**Title**: SLO doc + sample Grafana dashboard JSON
**Priority**: Should
**Path**: `intents/020-observability-stack/units/002-error-tracking-and-slos/stories/002-slo-documentation-and-dashboard.md`
**Bolt**: 045

---

### 021-distributed-state-redis

> ⏸ **Deprioritized 2026-06-03** — scaling infrastructure; only pays off with multiple API instances. App not yet deployed, single-server fits current/foreseeable traffic. Revisit only on real scaling pressure / zero-downtime-deploy / multi-region need. ADRs 010 / 013 / 015 explicitly accept the single-server trade-offs.

#### Unit: 001-redis-backplane (4 stories) — Bolt: 046

### 001-signalr-redis-backplane.md ⬜ NOT STARTED
**Title**: SignalR `.AddStackExchangeRedis(...)` + multi-replica fan-out test
**Priority**: Could (was Must — deprioritized)
**Path**: `intents/021-distributed-state-redis/units/001-redis-backplane/stories/001-signalr-redis-backplane.md`
**Bolt**: 046

### 002-two-level-cache.md ⬜ NOT STARTED
**Title**: `ITwoLevelCache` with L1 memory + L2 Redis + pub/sub invalidation
**Priority**: Could (was Must — deprioritized)
**Path**: `intents/021-distributed-state-redis/units/001-redis-backplane/stories/002-two-level-cache.md`
**Bolt**: 046

### 003-distributed-rate-limiter.md ⬜ NOT STARTED
**Title**: Redis-backed rate-limit partition with fallback
**Priority**: Could (was Must — deprioritized)
**Path**: `intents/021-distributed-state-redis/units/001-redis-backplane/stories/003-distributed-rate-limiter.md`
**Bolt**: 046

### 004-redis-health-and-compose.md ⬜ NOT STARTED
**Title**: `/health` Redis probe + Compose service entries
**Priority**: Could (was Must — deprioritized)
**Path**: `intents/021-distributed-state-redis/units/001-redis-backplane/stories/004-redis-health-and-compose.md`
**Bolt**: 046

---

### 022-coupon-promo-codes

#### Unit: 001-coupon-domain-and-api (4 stories) — Bolt: 047

### 001-coupon-schema.md ⬜ NOT STARTED
**Title**: `Coupons` + `CouponRedemptions` tables + `Orders` additions
**Priority**: Must
**Path**: `intents/022-coupon-promo-codes/units/001-coupon-domain-and-api/stories/001-coupon-schema.md`
**Bolt**: 047

### 002-cart-coupon-endpoints.md ⬜ NOT STARTED
**Title**: `POST/DELETE /api/cart/coupon` with validation + preview
**Priority**: Must
**Path**: `intents/022-coupon-promo-codes/units/001-coupon-domain-and-api/stories/002-cart-coupon-endpoints.md`
**Bolt**: 047

### 003-redemption-on-order-create.md ⬜ NOT STARTED
**Title**: Atomic redemption with `RowVersion` + discount-then-VAT order
**Priority**: Must
**Path**: `intents/022-coupon-promo-codes/units/001-coupon-domain-and-api/stories/003-redemption-on-order-create.md`
**Bolt**: 047

### 004-admin-coupon-crud.md ⬜ NOT STARTED
**Title**: Admin CRUD + redemption stats endpoints
**Priority**: Should
**Path**: `intents/022-coupon-promo-codes/units/001-coupon-domain-and-api/stories/004-admin-coupon-crud.md`
**Bolt**: 047

#### Unit: 002-coupon-frontend (1 story) — Bolt: 048

### 001-cart-coupon-ux.md ⬜ NOT STARTED
**Title**: Cart input + Romanian copy mapping + discount line + invoice PDF
**Priority**: Should
**Path**: `intents/022-coupon-promo-codes/units/002-coupon-frontend/stories/001-cart-coupon-ux.md`
**Bolt**: 048

---

### 023-test-project-drift-repair

> Carry-over from bolt-033 Stage 2 review. Repairs three test files broken by pre-existing production-code drift so the full `dotnet test` runs without exclusions.

#### Unit: 001-test-project-drift-repair (4 stories) — Bolt: 049

### 001-uploadservicetests-mock-fileid.md ✅ IMPLEMENTED
**Title**: Update Moq setup for the new `IStorageService.SaveAsync` 5-arg overload
**Priority**: Must
**Path**: `intents/023-test-project-drift-repair/units/001-test-project-drift-repair/stories/001-uploadservicetests-mock-fileid.md`
**Bolt**: 049 ✅ IMPLEMENTED

### 002-cartservicetests-grouped-dto.md ✅ IMPLEMENTED
**Title**: Adapt `CartServiceTests` assertions to grouped `CartResponseDto.Groups[].Items` shape
**Priority**: Must
**Path**: `intents/023-test-project-drift-repair/units/001-test-project-drift-repair/stories/002-cartservicetests-grouped-dto.md`
**Bolt**: 049 ✅ IMPLEMENTED

### 003-cart-controller-tests-grouped-dto.md ✅ IMPLEMENTED
**Title**: Adapt `CartControllerIntegrationTests` + pass `FinishName` on every request
**Priority**: Must
**Path**: `intents/023-test-project-drift-repair/units/001-test-project-drift-repair/stories/003-cart-controller-tests-grouped-dto.md`
**Bolt**: 049 ✅ IMPLEMENTED

### 004-suite-green-verification.md ✅ IMPLEMENTED
**Title**: Final `dotnet test` runs end-to-end with no file exclusions
**Priority**: Must
**Path**: `intents/023-test-project-drift-repair/units/001-test-project-drift-repair/stories/004-suite-green-verification.md`
**Bolt**: 049 ✅ IMPLEMENTED

---

> Intent 024 created mid-construction of bolt 043 — the two-tier "promote-on-payment" model. Depends on intent 019. New bolts: 051–053. Last updated: 2026-05-27T13:10:00Z.

### 024-order-photo-archive

#### Unit: 001-order-photo-promotion (4 stories) — Bolt: 051

### 001-archive-schema.md ✅ COMPLETE
**Title**: `Upload.LargePreviewPath` + `OriginalPurgedAt` migration
**Priority**: Must
**Path**: `intents/024-order-photo-archive/units/001-order-photo-promotion/stories/001-archive-schema.md`
**Bolt**: 051

### 002-large-preview-generation.md ✅ COMPLETE
**Title**: Generate ~2000 px large web preview
**Priority**: Must
**Path**: `intents/024-order-photo-archive/units/001-order-photo-promotion/stories/002-large-preview-generation.md`
**Bolt**: 051

### 003-promote-on-paid.md ✅ COMPLETE
**Title**: Async promote-on-Paid worker (+ delete local after confirmed write)
**Priority**: Must
**Path**: `intents/024-order-photo-archive/units/001-order-photo-promotion/stories/003-promote-on-paid.md`
**Bolt**: 051

### 004-backfill-paid-orders.md ✅ COMPLETE
**Title**: One-off backfill of pre-existing paid orders (supersedes 019-003)
**Priority**: Should
**Path**: `intents/024-order-photo-archive/units/001-order-photo-promotion/stories/004-backfill-paid-orders.md`
**Bolt**: 051

#### Unit: 002-archive-retention (2 stories) — Bolt: 052

### 001-purge-original-on-shipped.md ✅ COMPLETE
**Title**: Delete cloud original when order ships (configurable status)
**Priority**: Must
**Path**: `intents/024-order-photo-archive/units/002-archive-retention/stories/001-purge-original-on-shipped.md`
**Bolt**: 052

### 002-retention-cleanup-job.md ✅ COMPLETE
**Title**: 12-month configurable archive cleanup (large + thumbnail)
**Priority**: Must
**Path**: `intents/024-order-photo-archive/units/002-archive-retention/stories/002-retention-cleanup-job.md`
**Bolt**: 052

#### Unit: 003-order-history-photos (2 stories) — Bolt: 053

### 001-order-photos-endpoint.md ✅ COMPLETE
**Title**: `GET /api/orders/{id}/photos` → presigned large + thumbnail URLs
**Priority**: Must
**Path**: `intents/024-order-photo-archive/units/003-order-history-photos/stories/001-order-photos-endpoint.md`
**Bolt**: 053

### 002-order-detail-photo-grid.md ✅ COMPLETE
**Title**: Order-detail thumbnail grid + large-preview lightbox
**Priority**: Must
**Path**: `intents/024-order-photo-archive/units/003-order-history-photos/stories/002-order-detail-photo-grid.md`
**Bolt**: 053

---

## Architect Review 2026-06-03 — Improvement Intents (025–031)

> Source: `docs/analysis/architect-review-2026-06-03.md`. All PLANNED / ✅ GENERATED (inception artifacts created; not yet implemented). P20 (coupon) → existing intent 022.

### 025-security-dependency-hygiene

#### Unit: 001-dependency-and-boot-hardening (4 stories) — Bolt: 054

### 001-patch-otel-cve.md ✅ GENERATED
**Title**: Patch OpenTelemetry CVE (GHSA-4625-4j76-fww9) — P01
**Priority**: Must
**Path**: `intents/025-security-dependency-hygiene/units/001-dependency-and-boot-hardening/stories/001-patch-otel-cve.md`
**Bolt**: 054

### 002-central-package-management.md ✅ GENERATED
**Title**: Stripe.net unify + Central Package Management — P02
**Priority**: Must
**Path**: `intents/025-security-dependency-hygiene/units/001-dependency-and-boot-hardening/stories/002-central-package-management.md`
**Bolt**: 054

### 003-renovate-config.md ✅ GENERATED
**Title**: Renovate grouped upgrade PRs — P03
**Priority**: Should
**Path**: `intents/025-security-dependency-hygiene/units/001-dependency-and-boot-hardening/stories/003-renovate-config.md`
**Bolt**: 054

### 004-forwarded-headers-metrics.md ✅ GENERATED
**Title**: ForwardedHeadersMiddleware for /metrics allow-list — P05
**Priority**: Must
**Path**: `intents/025-security-dependency-hygiene/units/001-dependency-and-boot-hardening/stories/004-forwarded-headers-metrics.md`
**Bolt**: 054

---

### 026-observability-boot-manifest

#### Unit: 001-boot-composition-and-flags (2 stories) — Bolt: 055

### 001-program-subsystem-extensions.md ✅ GENERATED
**Title**: Program.cs subsystem extension methods — P07
**Priority**: Should
**Path**: `intents/026-observability-boot-manifest/units/001-boot-composition-and-flags/stories/001-program-subsystem-extensions.md`
**Bolt**: 055

### 002-typed-feature-gate.md ✅ GENERATED
**Title**: Typed IFeatureGate registry — P10
**Priority**: Should
**Path**: `intents/026-observability-boot-manifest/units/001-boot-composition-and-flags/stories/002-typed-feature-gate.md`
**Bolt**: 055

#### Unit: 002-system-manifest-and-liveness (3 stories) — Bolt: 056

### 001-system-info-endpoint.md ✅ GENERATED
**Title**: /api/admin/system-info manifest — P04
**Priority**: Should
**Path**: `intents/026-observability-boot-manifest/units/002-system-manifest-and-liveness/stories/001-system-info-endpoint.md`
**Bolt**: 056

### 002-background-job-liveness-check.md ✅ GENERATED
**Title**: Heartbeat + background-job liveness health check — P17
**Priority**: Must
**Path**: `intents/026-observability-boot-manifest/units/002-system-manifest-and-liveness/stories/002-background-job-liveness-check.md`
**Bolt**: 056

### 003-anaf-invoice-metrics-and-slo.md ✅ GENERATED
**Title**: invoice_upload metrics + ANAF SLO — P17
**Priority**: Must
**Path**: `intents/026-observability-boot-manifest/units/002-system-manifest-and-liveness/stories/003-anaf-invoice-metrics-and-slo.md`
**Bolt**: 056

#### Unit: 003-architecture-and-standards-docs (3 stories) — Bolt: 057

### 001-multi-replica-readiness-doc.md ✅ GENERATED
**Title**: Multi-replica-readiness consolidation doc — P12
**Priority**: Could
**Path**: `intents/026-observability-boot-manifest/units/003-architecture-and-standards-docs/stories/001-multi-replica-readiness-doc.md`
**Bolt**: 057

### 002-refresh-tech-stack-and-known-failures.md ✅ GENERATED
**Title**: Refresh tech-stack.md + KNOWN_FAILURES.md — P19
**Priority**: Must
**Path**: `intents/026-observability-boot-manifest/units/003-architecture-and-standards-docs/stories/002-refresh-tech-stack-and-known-failures.md`
**Bolt**: 057

### 003-architecture-audit-checklist.md ✅ GENERATED
**Title**: Quarterly architecture audit checklist — P19
**Priority**: Must
**Path**: `intents/026-observability-boot-manifest/units/003-architecture-and-standards-docs/stories/003-architecture-audit-checklist.md`
**Bolt**: 057

#### Unit: 004-observability-boot-manifest-ui (1 story) — Bolt: 058

### 001-admin-system-info-tab.md ✅ GENERATED
**Title**: Admin System tab rendering the manifest — P04 (UI)
**Priority**: Should
**Path**: `intents/026-observability-boot-manifest/units/004-observability-boot-manifest-ui/stories/001-admin-system-info-tab.md`
**Bolt**: 058

---

### 027-architectural-layering

#### Unit: 001-layering-foundation (5 stories) — Bolt: 059

### 001-no-split-adr.md ✅ GENERATED
**Title**: No four-project clean-arch split ADR — P22
**Priority**: Could
**Path**: `intents/027-architectural-layering/units/001-layering-foundation/stories/001-no-split-adr.md`
**Bolt**: 059

### 002-domain-layer-extraction.md ✅ GENERATED
**Title**: Domain/ layer extraction — P21 (folds P16)
**Priority**: Could
**Path**: `intents/027-architectural-layering/units/001-layering-foundation/stories/002-domain-layer-extraction.md`
**Bolt**: 059

### 003-infrastructure-layer.md ✅ GENERATED
**Title**: Infrastructure/ layer — P21
**Priority**: Should
**Path**: `intents/027-architectural-layering/units/001-layering-foundation/stories/003-infrastructure-layer.md`
**Bolt**: 059

### 004-web-layer.md ✅ GENERATED
**Title**: Web/ presentation layer — P21
**Priority**: Should
**Path**: `intents/027-architectural-layering/units/001-layering-foundation/stories/004-web-layer.md`
**Bolt**: 059

### 005-application-feature-promotion.md ✅ GENERATED
**Title**: Application/<Feature>/ promotion — P21 (folds P06)
**Priority**: Should
**Path**: `intents/027-architectural-layering/units/001-layering-foundation/stories/005-application-feature-promotion.md`
**Bolt**: 059

#### Unit: 002-conventions-and-policy (2 stories) — Bolt: 060

### 001-abstractions-subfolders.md ✅ GENERATED
**Title**: Abstractions/ subfolder per feature — P23
**Priority**: Should
**Path**: `intents/027-architectural-layering/units/002-conventions-and-policy/stories/001-abstractions-subfolders.md`
**Bolt**: 060

### 002-no-repository-policy-and-analyzer.md ✅ GENERATED
**Title**: No-repository policy + IQueryable analyzer — P24
**Priority**: Should
**Path**: `intents/027-architectural-layering/units/002-conventions-and-policy/stories/002-no-repository-policy-and-analyzer.md`
**Bolt**: 060

#### Unit: 003-handler-pattern (4 stories) — Bolt: 061

### 001-command-handler-abstractions.md ✅ GENERATED
**Title**: ICommandHandler/IEventDispatcher abstractions — P25
**Priority**: Should
**Path**: `intents/027-architectural-layering/units/003-handler-pattern/stories/001-command-handler-abstractions.md`
**Bolt**: 061

### 002-create-order-handler.md ✅ GENERATED
**Title**: CreateOrderHandler (extract CreateFromCartAsync) — P25
**Priority**: Should
**Path**: `intents/027-architectural-layering/units/003-handler-pattern/stories/002-create-order-handler.md`
**Bolt**: 061

### 003-order-paid-event-dispatcher.md ✅ GENERATED
**Title**: OrderPaidEventDispatcher — P25 (folds P11)
**Priority**: Should
**Path**: `intents/027-architectural-layering/units/003-handler-pattern/stories/003-order-paid-event-dispatcher.md`
**Bolt**: 061

### 004-retry-and-promote-handlers.md ✅ GENERATED
**Title**: Retry-invoice + promote-photos handlers — P25
**Priority**: Should
**Path**: `intents/027-architectural-layering/units/003-handler-pattern/stories/004-retry-and-promote-handlers.md`
**Bolt**: 061

---

### 028-test-architecture

#### Unit: 001-test-infrastructure (4 stories) — Bolt: 062

### 001-timeprovider-adoption.md ✅ GENERATED
**Title**: Adopt TimeProvider across older services — P28
**Priority**: Should
**Path**: `intents/028-test-architecture/units/001-test-infrastructure/stories/001-timeprovider-adoption.md`
**Bolt**: 062

### 002-shared-test-application-factory.md ✅ GENERATED
**Title**: Shared PhotoPrintTestApplicationFactory base — P27
**Priority**: Should
**Path**: `intents/028-test-architecture/units/001-test-infrastructure/stories/002-shared-test-application-factory.md`
**Bolt**: 062

### 003-test-builders.md ✅ GENERATED
**Title**: Fluent test data Builders — P27
**Priority**: Should
**Path**: `intents/028-test-architecture/units/001-test-infrastructure/stories/003-test-builders.md`
**Bolt**: 062

### 004-reclassify-misnamed-unit-tests.md ✅ GENERATED
**Title**: Reclassify DbContext "unit" tests to Integration — P27
**Priority**: Should
**Path**: `intents/028-test-architecture/units/001-test-infrastructure/stories/004-reclassify-misnamed-unit-tests.md`
**Bolt**: 062

---

### 029-decomposition-and-hardening

#### Unit: 001-access-hardening (2 stories) — Bolt: 063

### 001-global-rate-limit.md ✅ GENERATED
**Title**: Global per-IP rate limit — P08
**Priority**: Should
**Path**: `intents/029-decomposition-and-hardening/units/001-access-hardening/stories/001-global-rate-limit.md`
**Bolt**: 063

### 002-admin-policy-constant.md ✅ GENERATED
**Title**: Policies.Admin constant — P08
**Priority**: Should
**Path**: `intents/029-decomposition-and-hardening/units/001-access-hardening/stories/002-admin-policy-constant.md`
**Bolt**: 063

#### Unit: 002-service-decomposition (2 stories) — Bolt: 064

### 001-decompose-auth-service.md ✅ GENERATED
**Title**: Split AuthService into 3 services — P13
**Priority**: Should
**Path**: `intents/029-decomposition-and-hardening/units/002-service-decomposition/stories/001-decompose-auth-service.md`
**Bolt**: 064

### 002-thin-webhooks-and-order-photo-query.md ✅ GENERATED
**Title**: OrderPhotoQueryService + thin WebhooksController — P14
**Priority**: Should
**Path**: `intents/029-decomposition-and-hardening/units/002-service-decomposition/stories/002-thin-webhooks-and-order-photo-query.md`
**Bolt**: 064

#### Unit: 003-persistence-config (1 story) — Bolt: 065

### 001-per-entity-configurations.md ✅ GENERATED
**Title**: Per-entity IEntityTypeConfiguration<T> — P15
**Priority**: Could
**Path**: `intents/029-decomposition-and-hardening/units/003-persistence-config/stories/001-per-entity-configurations.md`
**Bolt**: 065

---

### 030-ui-scaling-and-e2e

#### Unit: 001-ci-quality-gates (2 stories) — Bolt: 066

### 001-bundle-size-budget.md ✅ GENERATED
**Title**: CI bundle-size budget — P18
**Priority**: Should
**Path**: `intents/030-ui-scaling-and-e2e/units/001-ci-quality-gates/stories/001-bundle-size-budget.md`
**Bolt**: 066

### 002-playwright-e2e-smoke-tests.md ✅ GENERATED
**Title**: 3 Playwright e2e smoke tests — P18
**Priority**: Must
**Path**: `intents/030-ui-scaling-and-e2e/units/001-ci-quality-gates/stories/002-playwright-e2e-smoke-tests.md`
**Bolt**: 066

#### Unit: 002-ui-scaling-and-e2e-ui (4 stories) — Bolt: 067

### 001-base-api-service.md ✅ GENERATED
**Title**: Shared BaseApiService — P26
**Priority**: Should
**Path**: `intents/030-ui-scaling-and-e2e/units/002-ui-scaling-and-e2e-ui/stories/001-base-api-service.md`
**Bolt**: 067

### 002-home-page-breakup.md ✅ GENERATED
**Title**: Break up home-page.ts (951 LOC) — P26
**Priority**: Should
**Path**: `intents/030-ui-scaling-and-e2e/units/002-ui-scaling-and-e2e-ui/stories/002-home-page-breakup.md`
**Bolt**: 067

### 003-account-pages-breakup.md ✅ GENERATED
**Title**: Break up saved-addresses + profile pages — P26
**Priority**: Should
**Path**: `intents/030-ui-scaling-and-e2e/units/002-ui-scaling-and-e2e-ui/stories/003-account-pages-breakup.md`
**Bolt**: 067

### 004-delivery-step-locker-selector.md ✅ GENERATED
**Title**: Extract locker-selector from delivery-step — P26
**Priority**: Should
**Path**: `intents/030-ui-scaling-and-e2e/units/002-ui-scaling-and-e2e-ui/stories/004-delivery-step-locker-selector.md`
**Bolt**: 067

---

### 031-refund-return-flow

#### Unit: 001-refund-domain-and-api (4 stories) — Bolt: 068

### 001-refund-schema-and-status.md ✅ GENERATED
**Title**: Refund schema + OrderStatus.Refunded — P09
**Priority**: Must
**Path**: `intents/031-refund-return-flow/units/001-refund-domain-and-api/stories/001-refund-schema-and-status.md`
**Bolt**: 068

### 002-refund-service-stripe-euplatesc.md ✅ GENERATED
**Title**: Refund service (full/partial, Stripe + EuPlatesc) — P09
**Priority**: Must
**Path**: `intents/031-refund-return-flow/units/001-refund-domain-and-api/stories/002-refund-service-stripe-euplatesc.md`
**Bolt**: 068

### 003-anaf-credit-note.md ✅ GENERATED
**Title**: ANAF credit-note (UBL type 381) — P09
**Priority**: Must
**Path**: `intents/031-refund-return-flow/units/001-refund-domain-and-api/stories/003-anaf-credit-note.md`
**Bolt**: 068

### 004-admin-refund-endpoint.md ✅ GENERATED
**Title**: Admin refund endpoint — P09
**Priority**: Must
**Path**: `intents/031-refund-return-flow/units/001-refund-domain-and-api/stories/004-admin-refund-endpoint.md`
**Bolt**: 068

#### Unit: 002-refund-return-flow-ui (1 story) — Bolt: 069

### 001-admin-refund-action.md ✅ GENERATED
**Title**: Admin refund action + modal — P09 (UI)
**Priority**: Must
**Path**: `intents/031-refund-return-flow/units/002-refund-return-flow-ui/stories/001-admin-refund-action.md`
**Bolt**: 069

---

## Roadmap Phase 3–4 — Stabilization & Environment Triad Intents (032–033)

> Source: `docs/analysis/ai-workflow-review-2026-06-05.md` §6 (owner's roadmap). All PLANNED / ✅ GENERATED (inception artifacts created; not yet implemented). New bolts: 070–075 (bolt 050 remains unallocated by design). Intent 032 = Phase 3 (stabilize); intent 033 = Phase 4 (environment triad — infrastructure readiness only, NOT deployment).

### 032-regression-and-e2e-stabilization

> Phase 3. Builds on bolt 066 (Playwright foundation) + bolt 062 (Builders) — extends, does not re-plan. Coupon (047/048) and refund (068/069) e2e journeys authored but gated (`test.fixme`) until those features ship.

#### Unit: 001-e2e-data-strategy (4 stories) — Bolt: 070

### 001-e2e-data-contract.md ✅ GENERATED
**Title**: Documented deterministic e2e data contract
**Priority**: Must
**Path**: `intents/032-regression-and-e2e-stabilization/units/001-e2e-data-strategy/stories/001-e2e-data-contract.md`
**Bolt**: 070

### 002-builder-backed-fixtures.md ✅ GENERATED
**Title**: Builder-backed Playwright fixtures (guest/user/admin)
**Priority**: Must
**Path**: `intents/032-regression-and-e2e-stabilization/units/001-e2e-data-strategy/stories/002-builder-backed-fixtures.md`
**Bolt**: 070

### 003-payment-testmode-fixtures.md ✅ GENERATED
**Title**: Stripe + EuPlatesc test-mode fixtures
**Priority**: Should
**Path**: `intents/032-regression-and-e2e-stabilization/units/001-e2e-data-strategy/stories/003-payment-testmode-fixtures.md`
**Bolt**: 070

### 004-real-postgres-e2e-boot.md ✅ GENERATED
**Title**: Real-Postgres docker-compose e2e boot
**Priority**: Should
**Path**: `intents/032-regression-and-e2e-stabilization/units/001-e2e-data-strategy/stories/004-real-postgres-e2e-boot.md`
**Bolt**: 070

#### Unit: 002-e2e-journey-coverage (8 stories) — Bolt: 071

### 001-guest-and-registered-checkout.md ✅ GENERATED
**Title**: Guest + registered checkout journeys (+ decline branch)
**Priority**: Must
**Path**: `intents/032-regression-and-e2e-stabilization/units/002-e2e-journey-coverage/stories/001-guest-and-registered-checkout.md`
**Bolt**: 071

### 002-authentication-journeys.md ✅ GENERATED
**Title**: Email / Google (mocked) / guest-claim auth journeys
**Priority**: Must
**Path**: `intents/032-regression-and-e2e-stabilization/units/002-e2e-journey-coverage/stories/002-authentication-journeys.md`
**Bolt**: 071

### 003-uploads-cart-and-merge.md ✅ GENERATED
**Title**: Uploads, cart edits, guest→user cart merge
**Priority**: Must
**Path**: `intents/032-regression-and-e2e-stabilization/units/002-e2e-journey-coverage/stories/003-uploads-cart-and-merge.md`
**Bolt**: 071

### 004-payments-journeys.md ✅ GENERATED
**Title**: Stripe + EuPlatesc test-mode payment journeys
**Priority**: Must
**Path**: `intents/032-regression-and-e2e-stabilization/units/002-e2e-journey-coverage/stories/004-payments-journeys.md`
**Bolt**: 071

### 005-orders-and-account-journeys.md ✅ GENERATED
**Title**: Order history/detail (+ ownership) + account management
**Priority**: Must
**Path**: `intents/032-regression-and-e2e-stabilization/units/002-e2e-journey-coverage/stories/005-orders-and-account-journeys.md`
**Bolt**: 071

### 006-admin-journeys.md ✅ GENERATED
**Title**: Admin order/product/invoice journeys
**Priority**: Must
**Path**: `intents/032-regression-and-e2e-stabilization/units/002-e2e-journey-coverage/stories/006-admin-journeys.md`
**Bolt**: 071

### 007-gated-coupon-refund-journeys.md ✅ GENERATED
**Title**: Gated coupon + refund journeys (requires 047/048 + 068/069)
**Priority**: Should
**Path**: `intents/032-regression-and-e2e-stabilization/units/002-e2e-journey-coverage/stories/007-gated-coupon-refund-journeys.md`
**Bolt**: 071

### 008-e2e-ci-tiers-and-stability.md ✅ GENERATED
**Title**: CI fast/full tiers, retries, artifacts, flake controls
**Priority**: Must
**Path**: `intents/032-regression-and-e2e-stabilization/units/002-e2e-journey-coverage/stories/008-e2e-ci-tiers-and-stability.md`
**Bolt**: 071

#### Unit: 003-regression-methodology (3 stories) — Bolt: 072

### 001-regression-checklist.md ✅ GENERATED
**Title**: Regression checklist mapped to shipped intents
**Priority**: Should
**Path**: `intents/032-regression-and-e2e-stabilization/units/003-regression-methodology/stories/001-regression-checklist.md`
**Bolt**: 072

### 002-execute-regression-baseline.md ✅ GENERATED
**Title**: Execute + record one dated regression baseline
**Priority**: Must
**Path**: `intents/032-regression-and-e2e-stabilization/units/003-regression-methodology/stories/002-execute-regression-baseline.md`
**Bolt**: 072

### 003-triage-findings-to-backlog.md ✅ GENERATED
**Title**: Triage findings into backlog / KNOWN_FAILURES
**Priority**: Should
**Path**: `intents/032-regression-and-e2e-stabilization/units/003-regression-methodology/stories/003-triage-findings-to-backlog.md`
**Bolt**: 072

---

### 033-environment-triad

> Phase 4 — **infrastructure readiness only, NOT deployment** (deployment is roadmap Phase 6). Adds a third deployable-dev tier alongside the existing local + prod compose/appsettings assets; builds from them, leaving prod behaviour unchanged. No external bolt dependencies.

#### Unit: 001-config-tiers-and-compose (4 stories) — Bolt: 073

### 001-define-dev-env-tier.md ✅ GENERATED
**Title**: Define the dev-env ASPNETCORE_ENVIRONMENT + layered appsettings
**Priority**: Must
**Path**: `intents/033-environment-triad/units/001-config-tiers-and-compose/stories/001-define-dev-env-tier.md`
**Bolt**: 073

### 002-dev-env-compose-file.md ✅ GENERATED
**Title**: docker-compose.dev-env.yml (standalone; prod untouched)
**Priority**: Must
**Path**: `intents/033-environment-triad/units/001-config-tiers-and-compose/stories/002-dev-env-compose-file.md`
**Bolt**: 073

### 003-three-tier-config-map.md ✅ GENERATED
**Title**: Per-setting config map across local / dev-env / prod
**Priority**: Should
**Path**: `intents/033-environment-triad/units/001-config-tiers-and-compose/stories/003-three-tier-config-map.md`
**Bolt**: 073

### 004-boot-validation-parity.md ✅ GENERATED
**Title**: ValidateOnStart parity for the dev-env tier (loud-fail, no fallback)
**Priority**: Must
**Path**: `intents/033-environment-triad/units/001-config-tiers-and-compose/stories/004-boot-validation-parity.md`
**Bolt**: 073

#### Unit: 002-secrets-and-seeding (4 stories) — Bolt: 074

### 001-secrets-tier-matrix.md ✅ GENERATED
**Title**: Secrets × tier matrix (test vs live, storage location)
**Priority**: Must
**Path**: `intents/033-environment-triad/units/002-secrets-and-seeding/stories/001-secrets-tier-matrix.md`
**Bolt**: 074

### 002-dev-env-secrets-template.md ✅ GENERATED
**Title**: .env.dev-env.example template (test-mode placeholders)
**Priority**: Must
**Path**: `intents/033-environment-triad/units/002-secrets-and-seeding/stories/002-dev-env-secrets-template.md`
**Bolt**: 074

### 003-seeding-policy-and-selector.md ✅ GENERATED
**Title**: Per-environment seeding policy + selector (reuse existing seeders)
**Priority**: Should
**Path**: `intents/033-environment-triad/units/002-secrets-and-seeding/stories/003-seeding-policy-and-selector.md`
**Bolt**: 074

### 004-prod-demo-data-guard.md ✅ GENERATED
**Title**: Guard — DevDataSeed cannot run in Production
**Priority**: Should
**Path**: `intents/033-environment-triad/units/002-secrets-and-seeding/stories/004-prod-demo-data-guard.md`
**Bolt**: 074

#### Unit: 003-promotion-readiness (2 stories) — Bolt: 075

### 001-promotion-path-runbook.md ✅ GENERATED
**Title**: dev→prod promotion runbook (readiness documentation)
**Priority**: Should
**Path**: `intents/033-environment-triad/units/003-promotion-readiness/stories/001-promotion-path-runbook.md`
**Bolt**: 075

### 002-deployment-deferral-note.md ✅ GENERATED
**Title**: Explicit Phase-6 deployment-deferral note
**Priority**: Should
**Path**: `intents/033-environment-triad/units/003-promotion-readiness/stories/002-deployment-deferral-note.md`
**Bolt**: 075

---

### 034-eu-expansion-architecture-study

> **Research-only intent (roadmap Phase 5 prep).** Spike-bolts; zero production code, no
> translations, no deployment. Source feed: `docs/planning/eu-expansion-research-brief-2026-06-05.md`.
> Owner decisions (Checkpoint 1, 2026-06-05): compare both market tiers · one brand EU-wide ·
> ship from Romania · local currencies.

#### Unit: 001-research-tracks (7 stories) — Bolts: 076–082

### 001-t1-fulfillment-logistics.md ✅ GENERATED
**Title**: T1 — Fulfillment & logistics (RO-ship validation, per-corridor cost/time)
**Priority**: Must
**Path**: `intents/034-eu-expansion-architecture-study/units/001-research-tracks/stories/001-t1-fulfillment-logistics.md`
**Bolt**: 076

### 002-t2-site-url-architecture.md ✅ GENERATED
**Title**: T2 — Site & URL architecture (one brand; tied to intent 033 triad)
**Priority**: Must
**Path**: `intents/034-eu-expansion-architecture-study/units/001-research-tracks/stories/002-t2-site-url-architecture.md`
**Bolt**: 077

### 003-t3-frontend-i18n.md ✅ GENERATED
**Title**: T3 — Frontend i18n (Angular 21: built-in vs runtime)
**Priority**: Must
**Path**: `intents/034-eu-expansion-architecture-study/units/001-research-tracks/stories/003-t3-frontend-i18n.md`
**Bolt**: 078

### 004-t4-backend-localization.md ✅ GENERATED
**Title**: T4 — Backend localization (.NET; deferred-culture trap)
**Priority**: Must
**Path**: `intents/034-eu-expansion-architecture-study/units/001-research-tracks/stories/004-t4-backend-localization.md`
**Bolt**: 079

### 005-t5-tax-invoicing-compliance.md ✅ GENERATED
**Title**: T5 — Tax, invoicing & compliance (OSS VAT, multi-currency, both tiers)
**Priority**: Must
**Path**: `intents/034-eu-expansion-architecture-study/units/001-research-tracks/stories/005-t5-tax-invoicing-compliance.md`
**Bolt**: 080

### 006-t6-payments-checkout.md ✅ GENERATED
**Title**: T6 — Payments & checkout (Stripe local methods, multi-currency)
**Priority**: Must
**Path**: `intents/034-eu-expansion-architecture-study/units/001-research-tracks/stories/006-t6-payments-checkout.md`
**Bolt**: 081

### 007-t7-codebase-seam-audit.md ✅ GENERATED
**Title**: T7 — Codebase seam audit (repo-bound; counts + top-10)
**Priority**: Must
**Path**: `intents/034-eu-expansion-architecture-study/units/001-research-tracks/stories/007-t7-codebase-seam-audit.md`
**Bolt**: 082

#### Unit: 002-synthesis-and-decision (2 stories) — Bolt: 083

### 001-synthesis-options-paper.md ✅ GENERATED
**Title**: Synthesize findings → options paper (D2)
**Priority**: Must
**Path**: `intents/034-eu-expansion-architecture-study/units/002-synthesis-and-decision/stories/001-synthesis-options-paper.md`
**Bolt**: 083

### 002-owner-decision-adr.md ✅ GENERATED
**Title**: ⛔ Owner decision → ADR (D3)
**Priority**: Must
**Path**: `intents/034-eu-expansion-architecture-study/units/002-synthesis-and-decision/stories/002-owner-decision-adr.md`
**Bolt**: 083

#### Unit: 003-implementation-briefs (1 story) — Bolt: 084

### 001-author-implementation-briefs.md ✅ GENERATED
**Title**: Author implementation brief(s) from the ADR (D4)
**Priority**: Must
**Path**: `intents/034-eu-expansion-architecture-study/units/003-implementation-briefs/stories/001-author-implementation-briefs.md`
**Bolt**: 084

---

### 035-bug-hunter-agent-system

> **Tooling-only intent.** Builds the bug-hunting agent system from
> `docs/agent-systems/bug-hunter-build-guide.md` (spec of record): 43 briefs across 5 additive
> phases + an optional tier (31b added in v3.3, review H1) — a permanent 6-slot pipeline
> (Map→Hunt→Verify→Triage→
> Report→Learn), agents-as-skills, **read-only on application source**, outputs under
> `bug-hunting/`. ⚠️ **Construction mandate: every component MUST be built with the
> skill-creator skill** (`Skill` tool → `skill-creator:skill-creator`) — paste the
> story's Prompt N, build, run its test prompts, fix, then next, in master order.
> Order: 085→086→087→088→(089 ∥ 090)→091 ⛔(needs knowledge-ledger `ledger-query`)→092→093; 094 ⏸ on adoption.

#### Unit: 001-phase-1-skeleton (7 stories) — Bolts: 085, 086

### 001-ledger-io.md ✅ GENERATED
**Title**: `ledger-io` — concurrency-safe shared ledger R/W, correlation_id (Prompt 1)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/001-phase-1-skeleton/stories/001-ledger-io.md`
**Bolt**: 085

### 002-bug-documentation.md ✅ GENERATED
**Title**: `bug-documentation` — canonical 3-audience bug record, contract-sourced expected_behavior (Prompt 2)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/001-phase-1-skeleton/stories/002-bug-documentation.md`
**Bolt**: 085

### 003-deduplication.md ✅ GENERATED
**Title**: `deduplication` — new/duplicate/dismissed/suppressed verdicts (Prompt 3)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/001-phase-1-skeleton/stories/003-deduplication.md`
**Bolt**: 085

### 004-report-rendering.md ✅ GENERATED
**Title**: `report-rendering` — per-run Markdown report with reporting floor (Prompt 4)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/001-phase-1-skeleton/stories/004-report-rendering.md`
**Bolt**: 085

### 005-triage-intake.md ✅ GENERATED
**Title**: `triage-intake` — human decisions front door, reason-required dismissals (Prompt 5)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/001-phase-1-skeleton/stories/005-triage-intake.md`
**Bolt**: 085

### 006-general-hunter.md ✅ GENERATED
**Title**: `general-hunter` — combined top-down + file-sweep hunting agent (Prompt 6)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/001-phase-1-skeleton/stories/006-general-hunter.md`
**Bolt**: 086

### 007-orchestrator-skeleton.md ✅ GENERATED
**Title**: `orchestrator` — 6-slot coordinator; Phase-1 output labeled unverified (Prompt 7)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/001-phase-1-skeleton/stories/007-orchestrator-skeleton.md`
**Bolt**: 086

#### Unit: 002-phase-2-trust (5 stories) — Bolt: 087

### 001-severity-scoring.md ✅ GENERATED
**Title**: `severity-scoring` — severity × confidence → 0–100 risk (Prompt 8)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/002-phase-2-trust/stories/001-severity-scoring.md`
**Bolt**: 087

### 002-tool-ingest.md ✅ GENERATED
**Title**: `tool-ingest` — deterministic tool findings → normalized candidates (Prompt 9)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/002-phase-2-trust/stories/002-tool-ingest.md`
**Bolt**: 087

### 003-bug-verifier.md ✅ GENERATED
**Title**: `bug-verifier` — hardened Verify gate: disprove-first, sandbox+commit check, flaky double-run (Prompt 10)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/002-phase-2-trust/stories/003-bug-verifier.md`
**Bolt**: 087

### 004-git-revision-tracking.md ✅ GENERATED
**Title**: `git-revision-tracking` — commit pinning + fixed/moved reconciliation (Prompt 11)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/002-phase-2-trust/stories/004-git-revision-tracking.md`
**Bolt**: 087

### 005-orchestrator-verify-wiring.md ✅ GENERATED
**Title**: orchestrator extension — Verify→Verifier, Triage→scoring, SHA open/close (Prompt 11b)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/002-phase-2-trust/stories/005-orchestrator-verify-wiring.md`
**Bolt**: 087

#### Unit: 003-phase-3-breadth-and-scale (17 stories) — Bolts: 088, 089 ∥ 090, 091 ⛔

### 001-app-mapping.md ✅ GENERATED
**Title**: `app-mapping` — entry points, flows, risk classes; diff on refresh (Prompt 12)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/003-phase-3-breadth-and-scale/stories/001-app-mapping.md`
**Bolt**: 088

### 002-code-index.md ✅ GENERATED
**Title**: `code-index` — symbol/reference index + slice retrieval, incremental (Prompt 13)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/003-phase-3-breadth-and-scale/stories/002-code-index.md`
**Bolt**: 088

### 003-reachability.md ✅ GENERATED
**Title**: `reachability` — entry-point tracing; framework-aware unknown weight (Prompt 14)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/003-phase-3-breadth-and-scale/stories/003-reachability.md`
**Bolt**: 088

### 004-severity-scoring-reachability-ext.md ✅ GENERATED
**Title**: severity-scoring extension — risk = severity × confidence × reachability (Prompt 14b)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/003-phase-3-breadth-and-scale/stories/004-severity-scoring-reachability-ext.md`
**Bolt**: 088

### 005-flow-tracing.md ✅ GENERATED
**Title**: `flow-tracing` — walk one flow, inspect every handoff (Prompt 15)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/003-phase-3-breadth-and-scale/stories/005-flow-tracing.md`
**Bolt**: 088

### 006-taint-analysis.md ✅ GENERATED
**Title**: `taint-analysis` — source→sink tracking with sanitizer awareness (Prompt 16)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/003-phase-3-breadth-and-scale/stories/006-taint-analysis.md`
**Bolt**: 089

### 007-flow-tracer-agent.md ✅ GENERATED
**Title**: `flow-tracer-agent` — top-down hunt over priority flows (Prompt 17)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/003-phase-3-breadth-and-scale/stories/007-flow-tracer-agent.md`
**Bolt**: 089

### 008-file-sweeper-agent.md ✅ GENERATED
**Title**: `file-sweeper-agent` — bottom-up sweep, deterministic tools first (Prompt 18)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/003-phase-3-breadth-and-scale/stories/008-file-sweeper-agent.md`
**Bolt**: 089

### 009-security-auditor-agent.md ✅ GENERATED
**Title**: `security-auditor-agent` — taint + authn/authz + secrets + vuln classes (Prompt 19)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/003-phase-3-breadth-and-scale/stories/009-security-auditor-agent.md`
**Bolt**: 089

### 010-dependency-audit-agent.md ✅ GENERATED
**Title**: `dependency-audit-agent` — manifests/lockfiles vs live advisories, CVEs (Prompt 20)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/003-phase-3-breadth-and-scale/stories/010-dependency-audit-agent.md`
**Bolt**: 090

### 011-config-auditor-agent.md ✅ GENERATED
**Title**: `config-auditor-agent` — config/infra bug class (compose, CI, env, IaC) (Prompt 21)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/003-phase-3-breadth-and-scale/stories/011-config-auditor-agent.md`
**Bolt**: 090

### 012-concurrency-auditor-agent.md ✅ GENERATED
**Title**: `concurrency-auditor-agent` — races/deadlocks/TOCTOU (conditional; async-heavy stack) (Prompt 22)
**Priority**: Should
**Path**: `intents/035-bug-hunter-agent-system/units/003-phase-3-breadth-and-scale/stories/012-concurrency-auditor-agent.md`
**Bolt**: 090

### 013-root-cause-clustering.md ✅ GENERATED
**Title**: `root-cause-clustering` — N symptoms → 1 multi-location bug, conservative (Prompt 23)
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/003-phase-3-breadth-and-scale/stories/013-root-cause-clustering.md`
**Bolt**: 090

### 014-intent-lookup.md ✅ GENERATED
**Title**: `intent-lookup` — oracle read of knowledge-ledger contracts (Prompt 24) ⛔ ext-dep
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/003-phase-3-breadth-and-scale/stories/014-intent-lookup.md`
**Bolt**: 091

### 015-hunters-contract-ext.md ✅ GENERATED
**Title**: hunters extension — surface contract-contradiction candidates (Prompt 24b) ⛔ ext-dep
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/003-phase-3-breadth-and-scale/stories/015-hunters-contract-ext.md`
**Bolt**: 091

### 016-verifier-scoring-contract-ext.md ✅ GENERATED
**Title**: verifier+scoring extension — contract-corroborated confidence, intent-unconfirmed tag (Prompt 24c) ⛔ ext-dep
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/003-phase-3-breadth-and-scale/stories/016-verifier-scoring-contract-ext.md`
**Bolt**: 091

### 017-orchestrator-scale-ext.md ✅ GENERATED
**Title**: orchestrator extension — specialists dispatch, budget, incremental, cheap-first, oracle (Prompt 24d) ⛔ ext-dep
**Priority**: Must
**Path**: `intents/035-bug-hunter-agent-system/units/003-phase-3-breadth-and-scale/stories/017-orchestrator-scale-ext.md`
**Bolt**: 091

#### Unit: 004-phase-4-learn-and-measure (6 stories) — Bolt: 092

### 001-suppression-learning.md ✅ GENERATED
**Title**: `suppression-learning` — dismissal reasons → validated, proposed-only patterns (Prompt 25)
**Priority**: Should
**Path**: `intents/035-bug-hunter-agent-system/units/004-phase-4-learn-and-measure/stories/001-suppression-learning.md`
**Bolt**: 092

### 002-bug-lifecycle.md ✅ GENERATED
**Title**: `bug-lifecycle` — status machine, evidence-based self-close, regression flags (Prompt 26)
**Priority**: Should
**Path**: `intents/035-bug-hunter-agent-system/units/004-phase-4-learn-and-measure/stories/002-bug-lifecycle.md`
**Bolt**: 092

### 003-eval-corpus.md ✅ GENERATED
**Title**: `eval-corpus` — labeled real + seeded synthetic ground truth, hit matcher (Prompt 27)
**Priority**: Should
**Path**: `intents/035-bug-hunter-agent-system/units/004-phase-4-learn-and-measure/stories/003-eval-corpus.md`
**Bolt**: 092

### 004-eval-metrics.md ✅ GENERATED
**Title**: `eval-metrics` — recall vs seeded corpus, precision via dismissals, trends (Prompt 28)
**Priority**: Should
**Path**: `intents/035-bug-hunter-agent-system/units/004-phase-4-learn-and-measure/stories/004-eval-metrics.md`
**Bolt**: 092

### 005-curator-agent.md ✅ GENERATED
**Title**: `curator-agent` — Learn/Reconcile/Measure/Summarize, fills the Learn slot (Prompt 29)
**Priority**: Should
**Path**: `intents/035-bug-hunter-agent-system/units/004-phase-4-learn-and-measure/stories/005-curator-agent.md`
**Bolt**: 092

### 006-orchestrator-learn-ext.md ✅ GENERATED
**Title**: orchestrator extension — Learn slot → Curator at run close (Prompt 29b)
**Priority**: Should
**Path**: `intents/035-bug-hunter-agent-system/units/004-phase-4-learn-and-measure/stories/006-orchestrator-learn-ext.md`
**Bolt**: 092

#### Unit: 005-phase-5-remediation (5 stories) — Bolt: 093

### 001-regression-harvest.md ✅ GENERATED
**Title**: `regression-harvest` — keep the proving test as a tripwire (owner-approved write) (Prompt 30)
**Priority**: Should
**Path**: `intents/035-bug-hunter-agent-system/units/005-phase-5-remediation/stories/001-regression-harvest.md`
**Bolt**: 093

### 002-fix-verification.md ✅ GENERATED
**Title**: `fix-verification` — the closure GATE; verified-fixed by correlation_id (extends bug-lifecycle) (Prompt 31)
**Priority**: Should
**Path**: `intents/035-bug-hunter-agent-system/units/005-phase-5-remediation/stories/002-fix-verification.md`
**Bolt**: 093

### 003-fix-proposal.md ✅ GENERATED
**Title**: `fix-proposal` — suite-validated draft diffs, never applied (Prompt 32)
**Priority**: Should
**Path**: `intents/035-bug-hunter-agent-system/units/005-phase-5-remediation/stories/003-fix-proposal.md`
**Bolt**: 093

### 004-fix-request-emit.md ✅ GENERATED
**Title**: `fix-request-emit` — idempotent AI-DLC hand-off store, correlation_id-keyed (Prompt 33)
**Priority**: Should
**Path**: `intents/035-bug-hunter-agent-system/units/005-phase-5-remediation/stories/004-fix-request-emit.md`
**Bolt**: 093

### 005-orchestrator-remediation-ext.md ✅ GENERATED
**Title**: orchestrator extension — run-open fix-request mailbox scan (Prompt 31b, NEW in guide v3.3 / review H1)
**Priority**: Should
**Path**: `intents/035-bug-hunter-agent-system/units/005-phase-5-remediation/stories/005-orchestrator-remediation-ext.md`
**Bolt**: 093

#### Unit: 006-optional-integration (3 stories) — Bolt: 094 ⏸ adoption-gated

### 001-report-rendering-sarif-ext.md ✅ GENERATED
**Title**: report-rendering extension — SARIF twin with count parity (Optional A)
**Priority**: Could
**Path**: `intents/035-bug-hunter-agent-system/units/006-optional-integration/stories/001-report-rendering-sarif-ext.md`
**Bolt**: 094

### 002-issue-sync.md ✅ GENERATED
**Title**: `issue-sync` — idempotent tracker tickets following the bug lifecycle (Optional B)
**Priority**: Could
**Path**: `intents/035-bug-hunter-agent-system/units/006-optional-integration/stories/002-issue-sync.md`
**Bolt**: 094

### 003-ci-gate.md ✅ GENERATED
**Title**: `ci-gate` — baseline-aware pass/fail policy, fails only NEW Critical/High (Optional C)
**Priority**: Could
**Path**: `intents/035-bug-hunter-agent-system/units/006-optional-integration/stories/003-ci-gate.md`
**Bolt**: 094
