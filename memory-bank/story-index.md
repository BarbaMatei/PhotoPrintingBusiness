# Global Story Index

## Overview
- **Total stories**: 114 (59 prior + 51 from arch-analysis + 4 from intent 023)
- **Generated**: 103
- **Implemented**: 17 (bolts 033 + 034 + 035 + 049 + 041)
- **Intents complete**: 013, 014, 018, 023
- **Planned (not yet story-filed)**: 11
- **Last updated**: 2026-05-27T00:00:00Z

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

### 001-upload-entity-schema.md ⬜ NOT STARTED
**Title**: Upload entity, IStorageService, and storage path convention
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/001-upload-and-cart-backend/stories/001-upload-entity-schema.md`
**Bolt**: 012 | **Epic story**: US-202

### 002-upload-endpoint.md ⬜ NOT STARTED
**Title**: POST /api/uploads with MIME validation, ImageSharp, and rate limiting
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/001-upload-and-cart-backend/stories/002-upload-endpoint.md`
**Bolt**: 012 | **Epic story**: US-202

### 003-upload-preview-and-cleanup.md ⬜ NOT STARTED
**Title**: Upload preview endpoint and hourly cleanup background job
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/001-upload-and-cart-backend/stories/003-upload-preview-and-cleanup.md`
**Bolt**: 012 | **Epic story**: US-202

### 004-cart-item-entity.md ⬜ NOT STARTED
**Title**: CartItem EF Core entity and migration
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/001-upload-and-cart-backend/stories/004-cart-item-entity.md`
**Bolt**: 013 | **Epic story**: US-206

### 005-cart-crud-endpoints.md ⬜ NOT STARTED
**Title**: POST/GET/DELETE /api/cart with computed totals
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/001-upload-and-cart-backend/stories/005-cart-crud-endpoints.md`
**Bolt**: 013 | **Epic story**: US-206

### 006-cart-merge-endpoint.md ⬜ NOT STARTED
**Title**: POST /api/cart/merge — transactional guest-to-user cart merge
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/001-upload-and-cart-backend/stories/006-cart-merge-endpoint.md`
**Bolt**: 013 | **Epic story**: US-206

---

#### Unit: 002-upload-format-cart-ui (5 stories)

### 001-upload-page.md ⬜ NOT STARTED
**Title**: Drag-and-drop upload page with progress bars and thumbnail grid
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/002-upload-format-cart-ui/stories/001-upload-page.md`
**Bolt**: 014 | **Epic story**: US-201

### 002-format-finish-selector.md ⬜ NOT STARTED
**Title**: Global format/finish selector with reactive quality badge recalculation
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/002-upload-format-cart-ui/stories/002-format-finish-selector.md`
**Bolt**: 014 | **Epic story**: US-203

### 003-order-summary-panel.md ⬜ NOT STARTED
**Title**: Sticky live order summary panel with quantity steppers and add-to-cart CTA
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/002-upload-format-cart-ui/stories/003-order-summary-panel.md`
**Bolt**: 014 | **Epic story**: US-203

### 004-cart-page.md ⬜ NOT STARTED
**Title**: Cart page /cos with item list, edit controls, and navigation
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/002-upload-format-cart-ui/stories/004-cart-page.md`
**Bolt**: 014 | **Epic story**: US-205

### 005-cart-service.md ⬜ NOT STARTED
**Title**: CartService with localStorage/server sync, merge on login, and item count badge
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/002-upload-format-cart-ui/stories/005-cart-service.md`
**Bolt**: 014 | **Epic story**: US-205

---

#### Unit: 003-shipping-and-order-core (4 stories)

### 001-easybox-locker-catalog.md ⬜ NOT STARTED
**Title**: EasyboxLocker entity and seeded migration with ~200 Romanian lockers
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/003-shipping-and-order-core/stories/001-easybox-locker-catalog.md`
**Bolt**: 015 | **Epic story**: US-302

### 002-shipping-endpoints.md ⬜ NOT STARTED
**Title**: GET /api/shipping/lockers and /cost endpoints with IShippingService abstraction
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/003-shipping-and-order-core/stories/002-shipping-endpoints.md`
**Bolt**: 015 | **Epic story**: US-302

### 003-order-entity-schema.md ⬜ NOT STARTED
**Title**: Order and OrderItem entities with JSONB fields, enums, and FT-YYYYNNNN order number
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/003-shipping-and-order-core/stories/003-order-entity-schema.md`
**Bolt**: 015 | **Epic story**: US-305

### 004-order-status-machine.md ⬜ NOT STARTED
**Title**: OrderStatus enum and OrderStatusMachine valid transition enforcement
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/003-shipping-and-order-core/stories/004-order-status-machine.md`
**Bolt**: 015 | **Epic story**: US-305

---

#### Unit: 004-payment-backends (5 stories)

### 001-order-service.md ⬜ NOT STARTED
**Title**: IOrderService — create order from cart with pricing snapshot and order number generation
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/004-payment-backends/stories/001-order-service.md`
**Bolt**: 016 | **Epic story**: US-305

### 002-stripe-payment-intent.md ⬜ NOT STARTED
**Title**: POST /api/payments/stripe/intent — PaymentIntent creation and pending order
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/004-payment-backends/stories/002-stripe-payment-intent.md`
**Bolt**: 016 | **Epic story**: US-305

### 003-stripe-webhook-handler.md ⬜ NOT STARTED
**Title**: POST /api/webhooks/stripe — signature verification, idempotency, order status transitions
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/004-payment-backends/stories/003-stripe-webhook-handler.md`
**Bolt**: 016 | **Epic story**: US-305

### 004-euplatesc-initiate.md ⬜ NOT STARTED
**Title**: POST /api/payments/euplatesc/initiate — HMAC-MD5 signed redirect URL generation
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/004-payment-backends/stories/004-euplatesc-initiate.md`
**Bolt**: 016 | **Epic story**: US-306

### 005-euplatesc-ipn-handler.md ⬜ NOT STARTED
**Title**: POST /api/webhooks/euplatesc — IPN validation, amount check, EuPlatesc spec response
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/004-payment-backends/stories/005-euplatesc-ipn-handler.md`
**Bolt**: 016 | **Epic story**: US-306

---

#### Unit: 005-checkout-ui (6 stories)

### 001-checkout-stepper.md ⬜ NOT STARTED
**Title**: Checkout stepper component and CheckoutStateService with sessionStorage persistence
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/005-checkout-ui/stories/001-checkout-stepper.md`
**Bolt**: 017 | **Epic story**: US-301

### 002-delivery-step.md ⬜ NOT STARTED
**Title**: Delivery method selection step — Easybox cards and home delivery address form
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/005-checkout-ui/stories/002-delivery-step.md`
**Bolt**: 017 | **Epic story**: US-301

### 003-locker-map-component.md ⬜ NOT STARTED
**Title**: Leaflet.js locker map with city search, pin rendering, and locker selection
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/005-checkout-ui/stories/003-locker-map-component.md`
**Bolt**: 017 | **Epic story**: US-301

### 004-order-review-step.md ⬜ NOT STARTED
**Title**: Order review step with read-only summary, grand total, and terms acceptance gate
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/005-checkout-ui/stories/004-order-review-step.md`
**Bolt**: 017 | **Epic story**: US-303

### 005-payment-step.md ⬜ NOT STARTED
**Title**: Payment step with Stripe Elements tab and EuPlatesc redirect tab
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/005-checkout-ui/stories/005-payment-step.md`
**Bolt**: 017 | **Epic story**: US-304

### 006-order-confirmation-page.md ⬜ NOT STARTED
**Title**: Order confirmation page with status stepper, guest CTA, and cart state reset
**Priority**: Must
**Path**: `intents/004-checkout-payment/units/005-checkout-ui/stories/006-order-confirmation-page.md`
**Bolt**: 017 | **Epic story**: US-307

---

## Stories by Status

- **Generated**: 85
- **In Progress**: 0
- **Completed**: 17 (all of 001-foundation-infrastructure)

---

### 010-photo-lightbox

#### Unit: 001-photo-lightbox-ui (2 stories)

### 001-photo-lightbox-component.md ⬜ NOT STARTED
**Title**: Photo lightbox overlay component
**Priority**: Must
**Path**: `intents/010-photo-lightbox/units/001-photo-lightbox-ui/stories/001-photo-lightbox-component.md`
**Bolt**: 026

### 002-thumbnail-click-integration.md ⬜ NOT STARTED
**Title**: Wire thumbnail click to open lightbox in format-selector
**Priority**: Must
**Path**: `intents/010-photo-lightbox/units/001-photo-lightbox-ui/stories/002-thumbnail-click-integration.md`
**Bolt**: 026

---

### 012-ui-polish

> Source: May 2026 live web design review. All issues are P2/P3 Angular/SCSS frontend — no backend changes.

#### Unit: 001-auth-scss-refactor (2 stories) — Bolt: 027

### 001-extract-auth-shared-styles.md ⬜ NOT STARTED
**Title**: Extract shared auth layout styles into `_auth-forms.scss` partial
**Priority**: Must
**Path**: `intents/012-ui-polish/units/001-auth-scss-refactor/stories/001-extract-auth-shared-styles.md`
**Bolt**: 027

### 002-remove-local-spinner-animation.md ⬜ NOT STARTED
**Title**: Remove local `.spinner` CSS from register page; confirm `<app-spinner>` usage
**Priority**: Must
**Path**: `intents/012-ui-polish/units/001-auth-scss-refactor/stories/002-remove-local-spinner-animation.md`
**Bolt**: 027

---

#### Unit: 002-shared-components-adoption (4 stories) — Bolt: 028

### 001-audit-pages-for-inline-loading.md ⬜ NOT STARTED
**Title**: Audit all feature pages for inline loading/empty-state patterns
**Priority**: Must
**Path**: `intents/012-ui-polish/units/002-shared-components-adoption/stories/001-audit-pages-for-inline-loading.md`
**Bolt**: 028

### 002-replace-inline-patterns-admin.md ⬜ NOT STARTED
**Title**: Replace inline loading/empty patterns in admin pages with shared components
**Priority**: Must
**Path**: `intents/012-ui-polish/units/002-shared-components-adoption/stories/002-replace-inline-patterns-admin.md`
**Bolt**: 028

### 003-replace-inline-patterns-catalog.md ⬜ NOT STARTED
**Title**: Replace inline loading/empty patterns in product catalog pages
**Priority**: Must
**Path**: `intents/012-ui-polish/units/002-shared-components-adoption/stories/003-replace-inline-patterns-catalog.md`
**Bolt**: 028

### 004-replace-inline-patterns-profile-cart.md ⬜ NOT STARTED
**Title**: Replace inline loading/empty patterns in profile and cart pages
**Priority**: Must
**Path**: `intents/012-ui-polish/units/002-shared-components-adoption/stories/004-replace-inline-patterns-profile-cart.md`
**Bolt**: 028

---

#### Unit: 003-global-ui-primitives (4 stories) — Bolts: 029, 030

### 001-create-buttons-partial.md ⬜ NOT STARTED
**Title**: Create `_buttons.scss` global partial with all button variants
**Priority**: Should
**Path**: `intents/012-ui-polish/units/003-global-ui-primitives/stories/001-create-buttons-partial.md`
**Bolt**: 029

### 002-remove-local-btn-definitions.md ⬜ NOT STARTED
**Title**: Remove duplicate `.btn` definitions from all feature SCSS files
**Priority**: Should
**Path**: `intents/012-ui-polish/units/003-global-ui-primitives/stories/002-remove-local-btn-definitions.md`
**Bolt**: 029

### 003-breadcrumb-standalone-component.md ⬜ NOT STARTED
**Title**: Create reusable `BreadcrumbComponent` with `title` and `backLink` inputs
**Priority**: Could
**Path**: `intents/012-ui-polish/units/003-global-ui-primitives/stories/003-breadcrumb-standalone-component.md`
**Bolt**: 030

### 004-wire-breadcrumb-admin-order-detail.md ⬜ NOT STARTED
**Title**: Replace inline breadcrumb in admin-order-detail-page with `<app-breadcrumb>`
**Priority**: Could
**Path**: `intents/012-ui-polish/units/003-global-ui-primitives/stories/004-wire-breadcrumb-admin-order-detail.md`
**Bolt**: 030

---

#### Unit: 004-responsive-ux-fixes (3 stories) — Bolts: 031, 032

### 001-show-hamburger-at-md-breakpoint.md ⬜ NOT STARTED
**Title**: Show header hamburger at 768px so tablet users have navigation
**Priority**: Should
**Path**: `intents/012-ui-polish/units/004-responsive-ux-fixes/stories/001-show-hamburger-at-md-breakpoint.md`
**Bolt**: 031

### 002-extract-password-checklist-component.md ⬜ NOT STARTED
**Title**: Extract register page password checklist into shared `PasswordChecklistComponent`
**Priority**: Could
**Path**: `intents/012-ui-polish/units/004-responsive-ux-fixes/stories/002-extract-password-checklist-component.md`
**Bolt**: 032

### 003-wire-checklist-profile-page.md ⬜ NOT STARTED
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

### 003-euplatesc-initiate-idempotency.md ✅ IMPLEMENTED
**Title**: Reuse persisted EuPlatesc redirect URL on repeat calls
**Priority**: Must
**Path**: `intents/014-payment-hardening/units/002-payment-idempotency/stories/003-euplatesc-initiate-idempotency.md`
**Bolt**: 035

---

### 015-sameday-shipping-integration

#### Unit: 001-sameday-api-client (3 stories) — Bolt: 036

### 001-sameday-settings-and-typed-client.md ⬜ NOT STARTED
**Title**: SamedaySettings, typed HTTP client, Polly retry + rate-limit policies
**Priority**: Must
**Path**: `intents/015-sameday-shipping-integration/units/001-sameday-api-client/stories/001-sameday-settings-and-typed-client.md`
**Bolt**: 036

### 002-token-auth-and-refresh.md ⬜ NOT STARTED
**Title**: Token endpoint authentication + 401-retry-once refresh
**Priority**: Must
**Path**: `intents/015-sameday-shipping-integration/units/001-sameday-api-client/stories/002-token-auth-and-refresh.md`
**Bolt**: 036

### 003-sameday-schema-additions.md ⬜ NOT STARTED
**Title**: EF migration adding `AwbLabelUrl` + `LastTrackingSyncAt` to Orders
**Priority**: Must
**Path**: `intents/015-sameday-shipping-integration/units/001-sameday-api-client/stories/003-sameday-schema-additions.md`
**Bolt**: 036

#### Unit: 002-awb-and-tracking-jobs (3 stories) — Bolt: 037

### 001-awb-creation-on-paid.md ⬜ NOT STARTED
**Title**: Auto-create AWB when order transitions to Paid
**Priority**: Must
**Path**: `intents/015-sameday-shipping-integration/units/002-awb-and-tracking-jobs/stories/001-awb-creation-on-paid.md`
**Bolt**: 037

### 002-awb-retry-job.md ⬜ NOT STARTED
**Title**: BackgroundService retries failed AWB creations hourly with cap
**Priority**: Must
**Path**: `intents/015-sameday-shipping-integration/units/002-awb-and-tracking-jobs/stories/002-awb-retry-job.md`
**Bolt**: 037

### 003-shipment-tracking-job.md ⬜ NOT STARTED
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

### 001-api-dockerfile.md ⬜ NOT STARTED
**Title**: Multi-stage Dockerfile with non-root user and HEALTHCHECK
**Priority**: Must
**Path**: `intents/017-deployment-cicd/units/001-containers-and-pipelines/stories/001-api-dockerfile.md`
**Bolt**: 040

### 002-docker-compose-dev.md ⬜ NOT STARTED
**Title**: Compose for API + Postgres + MailHog
**Priority**: Must
**Path**: `intents/017-deployment-cicd/units/001-containers-and-pipelines/stories/002-docker-compose-dev.md`
**Bolt**: 040

### 003-docker-compose-prod-caddy.md ⬜ NOT STARTED
**Title**: Production compose + Caddy reverse proxy with Let's Encrypt
**Priority**: Must
**Path**: `intents/017-deployment-cicd/units/001-containers-and-pipelines/stories/003-docker-compose-prod-caddy.md`
**Bolt**: 040

### 004-github-actions-ci.md ⬜ NOT STARTED
**Title**: CI workflow — restore, build, test, artefacts
**Priority**: Must
**Path**: `intents/017-deployment-cicd/units/001-containers-and-pipelines/stories/004-github-actions-ci.md`
**Bolt**: 040

### 005-github-actions-deploy.md ⬜ NOT STARTED
**Title**: CD workflow — tag image, push GHCR, deploy
**Priority**: Must
**Path**: `intents/017-deployment-cicd/units/001-containers-and-pipelines/stories/005-github-actions-deploy.md`
**Bolt**: 040

### 006-env-vars-matrix.md ⬜ NOT STARTED
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

### 001-thumbnail-path-schema.md ⬜ NOT STARTED
**Title**: EF migration adds `Uploads.ThumbnailPath`
**Priority**: Must
**Path**: `intents/019-thumbnail-cache-and-cloud-storage/units/001-thumbnail-cache/stories/001-thumbnail-path-schema.md`
**Bolt**: 042

### 002-persist-thumbnail-on-first-request.md ⬜ NOT STARTED
**Title**: First preview persists thumbnail; later requests stream cached file
**Priority**: Must
**Path**: `intents/019-thumbnail-cache-and-cloud-storage/units/001-thumbnail-cache/stories/002-persist-thumbnail-on-first-request.md`
**Bolt**: 042

### 003-imagesharp-max-pixels.md ⬜ NOT STARTED
**Title**: Configure ImageSharp `MaxImageWidth/Height` (decomp-bomb defence)
**Priority**: Must
**Path**: `intents/019-thumbnail-cache-and-cloud-storage/units/001-thumbnail-cache/stories/003-imagesharp-max-pixels.md`
**Bolt**: 042

#### Unit: 002-cloud-storage-provider (3 stories) — Bolt: 043

### 001-s3-storage-service.md ⬜ NOT STARTED
**Title**: `S3StorageService : IStorageService` against AWS SDK
**Priority**: Must
**Path**: `intents/019-thumbnail-cache-and-cloud-storage/units/002-cloud-storage-provider/stories/001-s3-storage-service.md`
**Bolt**: 043

### 002-preview-redirect-presigned-url.md ⬜ NOT STARTED
**Title**: 302 redirect to pre-signed URL on cloud provider
**Priority**: Must
**Path**: `intents/019-thumbnail-cache-and-cloud-storage/units/002-cloud-storage-provider/stories/002-preview-redirect-presigned-url.md`
**Bolt**: 043

### 003-local-to-cloud-migration-tool.md ⬜ NOT STARTED
**Title**: Resumable `migrate-storage` console command
**Priority**: Should
**Path**: `intents/019-thumbnail-cache-and-cloud-storage/units/002-cloud-storage-provider/stories/003-local-to-cloud-migration-tool.md`
**Bolt**: 043

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

#### Unit: 001-redis-backplane (4 stories) — Bolt: 046

### 001-signalr-redis-backplane.md ⬜ NOT STARTED
**Title**: SignalR `.AddStackExchangeRedis(...)` + multi-replica fan-out test
**Priority**: Must
**Path**: `intents/021-distributed-state-redis/units/001-redis-backplane/stories/001-signalr-redis-backplane.md`
**Bolt**: 046

### 002-two-level-cache.md ⬜ NOT STARTED
**Title**: `ITwoLevelCache` with L1 memory + L2 Redis + pub/sub invalidation
**Priority**: Must
**Path**: `intents/021-distributed-state-redis/units/001-redis-backplane/stories/002-two-level-cache.md`
**Bolt**: 046

### 003-distributed-rate-limiter.md ⬜ NOT STARTED
**Title**: Redis-backed rate-limit partition with fallback
**Priority**: Must
**Path**: `intents/021-distributed-state-redis/units/001-redis-backplane/stories/003-distributed-rate-limiter.md`
**Bolt**: 046

### 004-redis-health-and-compose.md ⬜ NOT STARTED
**Title**: `/health` Redis probe + Compose service entries
**Priority**: Must
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

### 001-uploadservicetests-mock-fileid.md ⬜ NOT STARTED
**Title**: Update Moq setup for the new `IStorageService.SaveAsync` 5-arg overload
**Priority**: Must
**Path**: `intents/023-test-project-drift-repair/units/001-test-project-drift-repair/stories/001-uploadservicetests-mock-fileid.md`
**Bolt**: 049 ✅ IMPLEMENTED

### 002-cartservicetests-grouped-dto.md ⬜ NOT STARTED
**Title**: Adapt `CartServiceTests` assertions to grouped `CartResponseDto.Groups[].Items` shape
**Priority**: Must
**Path**: `intents/023-test-project-drift-repair/units/001-test-project-drift-repair/stories/002-cartservicetests-grouped-dto.md`
**Bolt**: 049 ✅ IMPLEMENTED

### 003-cart-controller-tests-grouped-dto.md ⬜ NOT STARTED
**Title**: Adapt `CartControllerIntegrationTests` + pass `FinishName` on every request
**Priority**: Must
**Path**: `intents/023-test-project-drift-repair/units/001-test-project-drift-repair/stories/003-cart-controller-tests-grouped-dto.md`
**Bolt**: 049 ✅ IMPLEMENTED

### 004-suite-green-verification.md ⬜ NOT STARTED
**Title**: Final `dotnet test` runs end-to-end with no file exclusions
**Priority**: Must
**Path**: `intents/023-test-project-drift-repair/units/001-test-project-drift-repair/stories/004-suite-green-verification.md`
**Bolt**: 049 ✅ IMPLEMENTED
