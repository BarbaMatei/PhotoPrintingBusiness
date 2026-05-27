# FotoTipar — Architecture Analysis
**Date**: 2026-05-25  
**Analyst**: GitHub Copilot (architect-analyst workflow)  
**Scope**: Full codebase — `src/PhotoPrint.API`, `src/PhotoPrint.UI`, `src/PhotoPrint.Tests`, `memory-bank/`

---

## Architecture Scan Results

### 1. Project Structure

Monorepo (single deployment unit, no microservices) with three .NET projects under `PhotoPrint.sln`:

| Project | Role |
|---|---|
| `src/PhotoPrint.API/` | ASP.NET Core 8 Web API — 12 controllers, 50+ services |
| `src/PhotoPrint.UI/` | Angular 21 SPA — standalone components, lazy-loaded routes |
| `src/PhotoPrint.Tests/` | xUnit integration + unit tests (20 integration test files) |

`memory-bank/` holds 12 intents → 32 bolts → 59 stories managed via a Specs.md / AI-DLC workflow.

---

### 2. Tech Stack (verified from source)

**Backend** (`PhotoPrint.API.csproj`):
- .NET 8, C# 12, ASP.NET Core 8 Web API
- EF Core 8 (Code-First, Npgsql provider for Postgres, SQLite for dev)
- FluentValidation 11.3, Serilog 10, SignalR, RazorLight 2.3
- SixLabors.ImageSharp 3.1, Stripe.net 46.3, MailKit 4.16, SendGrid 9.29

**Frontend** (`package.json`):
- Angular 21.2, TypeScript 5.9, RxJS 7.8, Vitest (test runner)
- `@stripe/stripe-js` 9.6, `@microsoft/signalr` 10, `leaflet` 1.9, `chart.js` 4.5

**Database**: PostgreSQL 16 (prod) / SQLite (dev fallback with DateTimeOffset→Unix ms value converter)

---

### 3. Database Schema

17 entities across 13 migrations (latest: `20260524131359_AddFinishNameToCartItem`):

| Entity group | Tables |
|---|---|
| Auth | `Users`, `RefreshTokens`, `EmailConfirmationTokens`, `PasswordResetTokens`, `ExternalLogins`, `GuestSessions` |
| Catalog | `Products`, `ProductSizes`, `ProductFinishes`, `PricingTiers` |
| Commerce | `Uploads`, `CartItems`, `EasyboxLockers`, `Orders`, `OrderItems` |
| Account | `SavedAddresses` |
| Infra | `EmailQueue` |

**Key conventions**: UUID PKs, `CreatedAt`/`UpdatedAt` on all entities, `DeletedAt` soft-delete on `Uploads` only, JSONB for `ShippingAddress` and `CropData`, `decimal(18,2)` for all monetary fields in RON.

**Indexes defined**: unique on `Users.NormalizedEmail`, `Orders.OrderNumber`, `RefreshTokens.TokenHash`, `ExternalLogins(Provider,ProviderKey)`; composite on `Orders(Status,CreatedAt)`, `EmailQueue(Status,NextRetryAt)`, `CartItems(UserId,UploadId)`, `CartItems(GuestSessionId,UploadId)`.

---

### 4. API Contracts

- 12 REST controllers, unversioned (`/api/*`)
- Auth: JWT Bearer OR `X-Guest-Token` (dual-auth policy on cart/upload/payments)
- Errors: ProblemDetails RFC 7807 via `ExceptionHandlerMiddleware`
- Validation: FluentValidation → 422 with `{errors:[{field,message}]}` via `ValidationFilter` (ADR-002)
- Pagination: offset-based `{items, total, page, size}` envelope
- Files: `multipart/form-data`, 50 MB/file, 500 MB/batch, MIME magic-byte validated
- Response caching: `[ResponseCache(Duration=300)]` on `GET /api/products`

**Controller inventory**:

| Controller | Route prefix | Auth |
|---|---|---|
| `AuthController` | `/api/auth` | Public (rate-limited) |
| `ProductsController` | `/api/products` | Anonymous |
| `UploadsController` | `/api/uploads` | Dual-auth |
| `CartController` | `/api/cart` | Dual-auth |
| `ShippingController` | `/api/shipping` | Anonymous / Admin |
| `PaymentsController` | `/api/payments` | Dual-auth |
| `WebhooksController` | `/api/webhooks` | Anonymous (signature-verified) |
| `OrdersController` | `/api/orders` | JWT only |
| `AccountController` | `/api/account` | JWT only |
| `AdminOrdersController` | `/api/admin/orders` | Admin role |
| `AdminProductsController` | `/api/admin/products` | Admin role |
| `AdminStatsController` | `/api/admin/stats` | Admin role |

---

### 5. Security Model

| Control | Implementation |
|---|---|
| JWT | RS256, 15-min access token; 30-day refresh (HttpOnly, Secure, SameSite=Strict, SHA-256 hashed in DB, rotated on use) |
| Google OAuth | Server-side `id_token` validation via `GoogleTokenValidator` |
| RBAC | `Customer`, `Admin`, `Guest` roles; `[Authorize(Roles="Admin")]` on admin controllers + hub |
| Rate limiting | 100 req/min/IP (global), 10 req/min/IP (auth endpoints) via .NET 8 `RateLimiter` |
| CORS | Exact-origin whitelist — no wildcards; `AllowCredentials()` for refresh cookie |
| Security headers | HSTS (365d), CSP, X-Content-Type-Options, X-Frame-Options via `SecurityHeadersMiddleware` |
| File uploads | MIME magic-byte validation; 50 MB/file, 100 uploads/guest session |
| Stripe webhooks | SDK signature verification (`StripeSignatureVerifier`) |
| EuPlatesc webhooks | HMAC-MD5 v3 spec signature + amount validation |
| Secrets | **⚠️ Real RSA private key committed to `appsettings.Development.json`** |

---

### 6. Deployment & Infrastructure

**Status: No deployment artefacts exist on disk.**

`file_search "**/Dockerfile*"` → 0 results  
`file_search "**/docker-compose*"` → 0 results  
`file_search "**/.github/workflows/*.yml"` → 0 results

`memory-bank/standards/tech-stack.md` documents a Docker Compose + GitHub Actions topology — but none of those files have been created. Production deploys are by-hand.

**File storage**: `LocalStorageService` writes to `Storage:BasePath` (local disk, single-node only). `IStorageService` abstraction exists for S3/Blob migration but no cloud implementation has been written.

**Background jobs** (all `BackgroundService` — no Hangfire/Quartz):

| Job | Schedule | Purpose |
|---|---|---|
| `UploadCleanupJob` | Every 1 h | Soft-delete orphan uploads older than 24 h — **has a data-loss bug** |
| `GuestSessionCleanupJob` | (periodic) | Expire old guest sessions |
| `EmailRetryJob` | (periodic) | Retry failed emails from `EmailQueue` |
| `AccountDeletionJob` | (periodic) | GDPR 30-day account deletion |

---

### 7. Observability

| Component | Present |
|---|---|
| Structured logging (Serilog JSON) | ✅ Daily-rolling files, 30-day retention |
| Correlation IDs | ✅ `CorrelationIdMiddleware` (ADR-003) |
| Health endpoint | ✅ `/health` (DB + disk checks, always 200 — ADR-001) |
| OpenTelemetry / distributed tracing | ❌ |
| Metrics (Prometheus / OTLP) | ❌ |
| Error aggregation (Sentry / Bugsnag) | ❌ |
| Log shipping (Seq / Loki / Datadog) | ❌ |
| APM | ❌ |

**Observability score: 2 / 5** — file logs + correlation IDs are good for a single-node dev environment; insufficient for production incident response.

---

## Inferred Business Workflows

**Detected domain**: **E-commerce (photo printing)** — customers upload photos, choose format × finish × quantity with tiered pricing, check out via Stripe or EuPlatesc, ship via Sameday Easybox locker or home courier. Romanian market.

| Workflow | Key endpoints | Key models | Status |
|---|---|---|---|
| Authentication & account | `/api/auth/*`, `/api/account/*` | `User`, `RefreshToken`, `EmailConfirmationToken`, `ExternalLogin`, `SavedAddress` | ✅ Complete |
| Guest sessions | `/api/auth/guest`, `/api/auth/guest/init`, `/api/auth/guest/contact`, `/api/cart/merge` | `GuestSession` | ✅ Complete |
| Product catalog | `/api/products/*`, `/api/admin/products/*` | `Product`, `ProductSize`, `ProductFinish`, `PricingTier` | ✅ Complete |
| Photo upload | `/api/uploads`, `/api/uploads/batch`, `/api/uploads/{id}/preview` | `Upload` | ⚠️ Partial |
| Cart | `/api/cart`, `/api/cart/merge` | `CartItem` | ✅ Complete |
| Shipping | `/api/shipping/lockers`, `/api/shipping/cost`, `/api/shipping/awb` | `EasyboxLocker` | ❌ Stub |
| Checkout / payment | `/api/payments/stripe/intent`, `/api/payments/euplatesc/initiate` | `Order`, `OrderItem` | ⚠️ Partial |
| Payment confirmation (webhooks) | `/api/webhooks/stripe`, `/api/webhooks/euplatesc` | `Order` | ✅ Complete |
| Customer orders | `/api/orders`, `/api/orders/{id}` | `Order`, `OrderItem` | ✅ Complete |
| Admin order workflow | `/api/admin/orders/*` (status, cancel+refund, ZIP, notes) | `Order` | ✅ Complete |
| Admin stats | `/api/admin/stats` | `Order` (aggregated) | ✅ Complete |
| Email delivery | Internal (`EmailQueue` + retry) | `EmailQueue` | ✅ Complete |
| Real-time admin notifications | SignalR hub `/hubs/admin-orders` | `Order` | ⚠️ Partial |

### Flagged workflows

**❌ Shipping AWB (Stub)**  
`StaticShippingService.GenerateAwbAsync` returns `Manual: true, Message: "AWB se generează manual în portalul Sameday"`. The Sameday API integration documented in `tech-stack.md` does not exist in code. Every shipped order requires manual operator action in the Sameday portal — this does not scale.

**⚠️ Photo upload (data-loss bug)**  
`UploadCleanupJob.CleanupAsync` deletes every `Upload` older than 24 h with `DeletedAt IS NULL`, **with no check for `CartItem` or `OrderItem` references** (the inline comment claims otherwise). If a user uploads photos, leaves for a day, then pays, the source files are gone before the admin downloads them for printing.

**⚠️ Checkout / payment (fraud + duplicate orders)**  
`CreateOrderRequest.ShippingCostRon` is sent by the Angular client and added directly to `order.TotalRon` with no server-side validation — an attacker can POST `ShippingCostRon: -100` to reduce the charge. Additionally, `POST /api/payments/stripe/intent` creates a new `Order` row every call with no idempotency key, so double-clicking "Pay" produces two paid orders with two Stripe charges.

**⚠️ Real-time admin (single-instance only)**  
`AdminOrderHub` has no Redis backplane. Two API replicas → admins on different replicas miss each other's notifications.

---

## Gap Analysis

### Universal capability matrix

| Capability | Status | Notes |
|---|---|---|
| Authentication | ✅ | JWT RS256 + Google OAuth + guest tokens |
| Authorisation / RBAC | ✅ | Role checks on all admin routes + hub |
| Input validation | ⚠️ | FluentValidation on Auth/Cart/Account/Admin; **no validator for `CreateOrderRequest`** |
| Structured error responses | ✅ | ProblemDetails everywhere |
| Pagination | ✅ | Offset-based on orders + admin orders |
| Rate limiting | ✅ | Per-IP, global + auth |
| Soft deletes | ⚠️ | Only on `Uploads`; no guard on `Orders` deletion |
| Audit log | ❌ | No `created_by`/`updated_by`; no event log table. Status changes logged to file only |
| Health check | ✅ | `/health` DB + disk |
| Structured logging | ✅ | Serilog JSON + correlation IDs |
| Environment config | ⚠️ | Real dev RSA key committed to source; no `.env.example` |
| Tests | ✅ | ~20 integration test files + unit tests; Vitest on FE |

### E-commerce capability matrix

| Capability | Status | Notes |
|---|---|---|
| Cart persistence | ✅ | `CartItem` table, server-synced |
| Inventory management | N/A | Print-on-demand, no stock |
| Order state machine | ✅ | `OrderStatusMachine` with 7 valid transitions |
| Payment integration | ✅ | Stripe + EuPlatesc, both signature-verified |
| Refund flow | ✅ | `AdminOrderService.CancelOrderAsync` for both gateways |
| Tax / VAT | ❌ | **Zero references to TVA/VAT in codebase**; Romania mandates 19% VAT + invoices |
| Invoicing / e-Factura | ❌ | No `Invoice` entity; Romania's e-Factura (SPV) is legally mandatory |
| Email receipts | ✅ | `OrderEmailService.FireOrderConfirmedEmail` |
| Product search / filter | ⚠️ | All 6 products returned; no filter API (fine at this scale) |
| Product variants | ✅ | Size × finish per product |
| Discount / coupon | ❌ | No promo code model or endpoint |
| Shipping rate calculation | ⚠️ | Flat rates hardcoded in `appsettings.json` (20 RON / 25 RON) |
| Carrier integration | ❌ | Sameday API absent — AWB is manual |

### Scalability bottlenecks

- **Thumbnail regeneration on every preview** — `ImageProcessor.GenerateThumbnailAsync` decodes the full 50 MB original and re-encodes JPEG for every `GET /api/uploads/{id}/preview` call. No on-disk thumbnail cache, no CDN. CPU-intensive under any real load.
- **Local-disk file storage** — `LocalStorageService` binds the API to a single VM. `IStorageService` interface is ready for migration but no S3/Azure Blob implementation exists.
- **In-process SignalR hub** — `AdminOrderHub` with no Redis backplane. Multi-instance deploys split notification delivery.
- **In-memory cache only** — `AddMemoryCache()` per-instance; cross-instance invalidation impossible.
- **Cleanup job loads full table into memory** — `db.Uploads.Where(...).ToListAsync()` in `UploadCleanupJob`; fine at MVP, problematic with millions of uploads.
- **Rate limiter is per-instance** — `FixedWindowLimiter` in-process; clients can bypass the limit by hitting different replicas.

### Security gaps (concrete)

1. **Client-trusted shipping cost** — `CreateOrderRequest.ShippingCostRon` flows into `order.TotalRon` with no validator. POST `ShippingCostRon: -100` → discounted order.
2. **No payment idempotency** — every POST `/api/payments/stripe/intent` creates a new `Order` + Stripe PaymentIntent. Retry or double-click → duplicate orders + charges.
3. **RSA private key in source control** — real key in `appsettings.Development.json#L13`. JWT forgery risk if the key is reused for staging/prod or if the repo is/becomes public.
4. **EuPlatesc amount mismatch goes unmonitored** — on mismatch, the order is left in `AwaitingPayment` silently with no alert; the signed IPN response is still returned to EuPlatesc.
5. **PII stored in plaintext** — `User.Email`, `User.Phone`, `Order.ShippingAddress` (JSONB) unencrypted at rest.
6. **No image decompression-bomb protection** — `ImageSharp` has no `MAX_PIXELS` cap configured; a crafted image could exhaust memory.
7. **Stack trace in dev responses** — `ExceptionHandlerMiddleware` includes `stackTrace` when `IsDevelopment()`; must ensure `ASPNETCORE_ENVIRONMENT=Production` in prod.

### Observability score: **2 / 5**

JSON file logs + correlation IDs are appropriate for a single-node dev environment. Everything else (metrics, traces, error aggregation, log shipping, alerting) is absent.

### Top 5 critical gaps

| # | Gap | Risk |
|---|---|---|
| 1 | `UploadCleanupJob` deletes cart/order-referenced uploads | Silent customer data loss |
| 2 | Romanian VAT + e-Factura compliance | Legal liability / ANAF fines |
| 3 | Client-trusted shipping cost | Direct revenue leakage / fraud |
| 4 | No deployment artefacts (Dockerfile, compose, CI/CD) | Cannot deploy reproducibly |
| 5 | Sameday AWB integration absent | Every order requires manual operator action |

---

## Improvement Proposals

Scoring formula: `priority_score = (business_impact × 3) + ((6 - complexity) × 2)` — max 25.  
Complexity and business impact both rated 1–5.

---

### #1 — Fix `UploadCleanupJob` to skip uploads referenced by cart or order items

| Field | Value |
|---|---|
| Category | `fix` |
| Complexity | 2 — small EF query change + integration test |
| Business impact | 5 — silently destroys paid customer data |
| Priority score | **23** |
| Estimated effort | 2 developer-days |
| Affects | `src/PhotoPrint.API/BackgroundJobs/UploadCleanupJob.cs` |

**What and why**  
`UploadCleanupJob.CleanupAsync` deletes every `Upload` with `UploadedAt < now - 24h` and `DeletedAt IS NULL` — despite the inline comment claiming referenced uploads are excluded, **no such check exists in the query**. A customer who uploads photos, leaves for a day, then pays will have their source files deleted before the admin can print and zip them.

**Implementation steps**
1. Extend the `candidates` LINQ query with `!db.CartItems.Any(ci => ci.UploadId == u.Id)` and `!db.OrderItems.Any(oi => oi.UploadId == u.Id)`.
2. Add config keys: `UploadCleanup:OrphanRetentionHours` (default 24) and `UploadCleanup:ReferencedRetentionDays` (default 365 — keep for reprints/customer service).
3. Add an integration test: upload → add to cart + create order item → run cleanup tick → assert `Upload.DeletedAt IS NULL` and file still on disk.
4. Log skipped uploads at Debug level.

**Schema / API changes**
```csharp
// UploadCleanupJob.cs — replace candidates query
var candidates = await db.Uploads
    .Where(u => u.UploadedAt < cutoff && u.DeletedAt == null)
    .Where(u => !db.CartItems.Any(ci => ci.UploadId == u.Id))
    .Where(u => !db.OrderItems.Any(oi => oi.UploadId == u.Id))
    .ToListAsync(ct);
```

**Risks**
- Uploads already orphaned on-disk may accumulate; add a one-shot reconcile script for existing data.

---

### #2 — Server-side shipping cost resolution + payment idempotency

| Field | Value |
|---|---|
| Category | `security` |
| Complexity | 2 — DTO refactor + idempotency key |
| Business impact | 5 — direct fraud/revenue risk |
| Priority score | **23** |
| Estimated effort | 3 developer-days |
| Affects | `DTOs/Payments/CreateOrderRequest.cs`, `Services/OrderService.cs`, `Controllers/PaymentsController.cs`, new `Validators/Payments/CreateOrderRequestValidator.cs` |

**What and why**  
`ShippingCostRon` is accepted from the client and added to `order.TotalRon` with zero server-side validation ([OrderService.cs line ~74](src/PhotoPrint.API/Services/OrderService.cs)). An attacker can POST `"ShippingCostRon": -100` to get a discounted order charged via Stripe. Additionally, each call to `POST /api/payments/stripe/intent` creates a fresh `Order` row and PaymentIntent — double-clicking "Pay" generates duplicate orders with duplicate charges.

**Implementation steps**
1. Remove `ShippingCostRon` from `CreateOrderRequest`; server resolves it from `IShippingService.GetShippingCostAsync(request.DeliveryType)`.
2. Add `CreateOrderRequestValidator`: `EasyboxLockerId` required iff `DeliveryType == Easybox`; `ShippingAddress` required iff `DeliveryType == Courier`.
3. Add `Idempotency-Key` header support on both payment endpoints. First call: create `Order` + intent, persist key. Subsequent calls within 24 h: return the existing `ClientSecret` + `OrderId`.
4. Pass idempotency key through to Stripe via `RequestOptions.IdempotencyKey`.

**Schema / API changes**
```sql
ALTER TABLE "Orders" ADD COLUMN "IdempotencyKey" varchar(80) NULL;
CREATE UNIQUE INDEX "ix_orders_idempotency_key"
    ON "Orders"("IdempotencyKey") WHERE "IdempotencyKey" IS NOT NULL;
```
```csharp
// New DTO — remove ShippingCostRon
public record CreateOrderRequest(
    PaymentProcessor PaymentProcessor,
    DeliveryType DeliveryType,
    Guid? EasyboxLockerId,
    ShippingAddressSnapshot? ShippingAddress);
```

**Risks**
- Frontend currently sends `ShippingCostRon` — coordinate FE/BE deploy; accept (ignore) the field for one release as a transitional measure.

---

### #3 — Sameday API integration for AWB generation + tracking

| Field | Value |
|---|---|
| Category | `feature` |
| Complexity | 4 — external API + retry + background job |
| Business impact | 5 — eliminates manual fulfilment bottleneck |
| Priority score | **19** |
| Estimated effort | 10 developer-days |
| Affects | new `Services/SamedayShippingService.cs`, `Configuration/SamedaySettings.cs`, new background job |

**What and why**  
`StaticShippingService.GenerateAwbAsync` returns `Manual: true`. Every shipped order requires an operator to copy recipient details into the Sameday portal manually. This is the single largest operational cost driver as order volume grows.

**Implementation steps**
1. Add `SamedaySettings` (BaseUrl, Username, Password, PickupPointId) and authenticate via Sameday token endpoint.
2. Implement `SamedayShippingService : IShippingService`. Map `Order` → AWB request; parcel weight estimated as `N × 50g + 50g`.
3. Persist `Order.AwbNumber` (exists) + new `Order.AwbLabelUrl` (see schema).
4. Register `SamedayShippingService` when `Sameday:Enabled=true`; fall back to `StaticShippingService` otherwise.
5. Add `ShipmentTrackingJob` (`BackgroundService`): poll Sameday every 15 min for `Shipped` orders → auto-transition to `Delivered` + fire email.
6. Wrap in `Polly` retry with exponential backoff. Failed AWB creation must NOT block the order; queue retry.

**Schema / API changes**
```sql
ALTER TABLE "Orders" ADD COLUMN "AwbLabelUrl" varchar(500) NULL;
ALTER TABLE "Orders" ADD COLUMN "LastTrackingSyncAt" timestamptz NULL;
```

**Risks**
- Sameday sandbox credentials differ from prod; test with real shipments before go-live.
- Sameday rate-limits at ~10 req/s — use `IHttpClientFactory` with `Polly`.

---

### #4 — Romanian VAT calculation + e-Factura invoice generation

| Field | Value |
|---|---|
| Category | `security` (legal compliance) |
| Complexity | 4 — fiscal spec + ANAF SPV integration |
| Business impact | 5 — legally mandatory |
| Priority score | **19** |
| Estimated effort | 12 developer-days |
| Affects | new `Models/Invoice.cs`, `Services/IInvoiceService.cs`, `Models/Order.cs`, checkout UI, email templates |

**What and why**  
Zero references to TVA/VAT/tax anywhere in the codebase. `TotalRon` is gross with no breakdown. Romania's e-Factura system (ANAF SPV) is mandatory for B2B since 2024 and for most B2C scenarios from 2025. Failure to issue compliant invoices risks ANAF fines.

**Implementation steps**
1. Add `Order.NetTotalRon`, `Order.VatRon`, `Order.VatRate` (19% configurable). Compute in `CreateFromCartAsync`: `vat = round(subtotal * 0.19 / 1.19, 2)`.
2. Add `Invoice` entity (see schema below).
3. On `Order.Status → Paid`, fire `InvoiceGenerationJob`: build UBL 2.1 XML per the e-Factura schema, upload to ANAF SPV (OAuth), store PDF via `IStorageService`.
4. Attach PDF to order-confirmation email; expose `GET /api/orders/{id}/invoice`.
5. Admin UI: list invoices, retry failed ANAF uploads.
6. Invoice numbers must be strictly sequential per fiscal year per series — use a DB sequence per `Series`.

**Schema / API changes**
```sql
ALTER TABLE "Orders"
    ADD COLUMN "NetTotalRon" numeric(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN "VatRon"      numeric(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN "VatRate"     numeric(5,4)  NOT NULL DEFAULT 0.19;

CREATE TABLE "Invoices" (
    "Id"             uuid PRIMARY KEY,
    "OrderId"        uuid NOT NULL REFERENCES "Orders"("Id"),
    "InvoiceNumber"  varchar(50) NOT NULL UNIQUE,
    "Series"         varchar(10) NOT NULL,
    "IssuedAt"       timestamptz NOT NULL,
    "XmlPayload"     text NOT NULL,
    "PdfStoragePath" varchar(500) NULL,
    "AnafUploadId"   varchar(100) NULL,
    "AnafStatus"     varchar(30) NOT NULL,
    "CreatedAt"      timestamptz NOT NULL,
    "UpdatedAt"      timestamptz NULL
);
```

**Risks**
- ANAF SPV OAuth requires a real legal-entity digital certificate — not testable without it.
- Invoice numbering must never have gaps per fiscal year; use `CREATE SEQUENCE` per series in Postgres.

---

### #5 — Deployment artefacts: Dockerfile + docker-compose + GitHub Actions CI/CD

| Field | Value |
|---|---|
| Category | `ops` |
| Complexity | 2 — well-trodden .NET patterns |
| Business impact | 4 — blocks reproducible deploys + DR |
| Priority score | **20** |
| Estimated effort | 3 developer-days |
| Affects | new `Dockerfile`, `docker-compose.yml`, `docker-compose.prod.yml`, `.github/workflows/ci.yml`, `.github/workflows/deploy.yml` |

**What and why**  
`tech-stack.md` documents a Docker + GitHub Actions deployment topology. Zero Dockerfiles or workflow files exist on disk. Production today is by-hand on a VPS — no rollback, no preview, no disaster recovery path.

**Implementation steps**
1. Multi-stage `Dockerfile` for the API: `sdk` build stage → `runtime` image, non-root user, `HEALTHCHECK CMD curl -f http://localhost:8080/health`.
2. `docker-compose.yml` for local dev: API + Postgres 16 + MailHog + Angular dev server (`ng serve`).
3. `docker-compose.prod.yml`: API + Postgres (or point at managed DB) + Caddy reverse proxy with Let's Encrypt.
4. `ci.yml`: restore → build → `dotnet test` → `ng build --configuration production` → upload artefacts → fail on test failures.
5. `deploy.yml`: on push to `main`, build and push container to GHCR, SSH-deploy or trigger Azure/DigitalOcean webhook.
6. Replace committed secrets in `appsettings.Development.json` with env-var placeholders; document the mapping in `README.md`.

**Schema / API changes** — None.

**Risks**
- First production deploy will surface every "works on my machine" assumption — schedule a maintenance window.

---

### #6 — Rotate RSA private key out of source control

| Field | Value |
|---|---|
| Category | `security` |
| Complexity | 1 — config change + git hygiene |
| Business impact | 4 — JWT forgery if key reused or repo goes public |
| Priority score | **22** |
| Estimated effort | 1 developer-day |
| Affects | `appsettings.Development.json`, git history, README |

**What and why**  
`appsettings.Development.json` contains a real `-----BEGIN RSA PRIVATE KEY-----` block (line 13). Even if "dev only", anyone with read access to the repo can forge JWTs signed with this key — or use it as a template pattern that propagates to staging/prod configs.

**Implementation steps**
1. Generate a fresh RSA keypair. Rotate any staging/prod keys derived from the current one immediately.
2. Replace `PrivateKeyPem` value with `""` in `appsettings.Development.json`.
3. Document `dotnet user-secrets set JwtSettings:PrivateKeyPem "$(cat dev-key.pem)"` in `README.md`.
4. Add `.gitignore` entries for `appsettings.*.local.json` and `secrets/`.
5. Rewrite git history with `git filter-repo --invert-paths --path src/PhotoPrint.API/appsettings.Development.json` (or accept the leak and rely on key rotation).
6. Add a pre-commit hook or GitHub secret-scanning rule blocking `-----BEGIN RSA PRIVATE KEY-----`, `sk_live_`, `pk_live_`.

**Schema / API changes** — None.

**Risks**
- History rewrite invalidates any open PRs and forks — coordinate with the team.

---

### #7 — On-disk thumbnail cache + cloud storage backend (S3 / Azure Blob)

| Field | Value |
|---|---|
| Category | `scalability` |
| Complexity | 3 — new storage implementation + thumbnail persistence |
| Business impact | 4 — unblocks horizontal scaling, eliminates repeated CPU decodes |
| Priority score | **18** |
| Estimated effort | 5 developer-days |
| Affects | `Services/LocalStorageService.cs`, `Services/ImageProcessor.cs`, `Controllers/UploadsController.cs`, new `Services/S3StorageService.cs`, `Models/Upload.cs` |

**What and why**  
`GET /api/uploads/{id}/preview` runs `Image.LoadAsync` → resize → JPEG encode on **every request** regardless of caching. With local-disk storage the API is locked to a single VM and burns CPU on identical thumbnails. At 100 simultaneous users, each uploading 30 photos and refreshing the preview pane, this will saturate the CPU before any other bottleneck manifests.

**Implementation steps**
1. Add `Upload.ThumbnailPath` (nullable) to the schema.
2. On first preview request, generate thumbnail and persist it via `_storage.SaveAsync(thumbStream, ..., fileId: $"{uploadId}_thumb")`. Subsequent requests stream the cached blob directly.
3. Implement `S3StorageService` (AWSSDK.S3 or MinIO for self-host). Config-switch: `Storage:Provider = Local|S3|AzureBlob`.
4. For cloud storage: return a `302 redirect` to a pre-signed CDN URL instead of proxying bytes through the API.
5. Set `Cache-Control: public, max-age=2592000, immutable` on thumbnails (UUID-keyed, safe to cache forever).

**Schema / API changes**
```sql
ALTER TABLE "Uploads" ADD COLUMN "ThumbnailPath" varchar(500) NULL;
```

**Risks**
- Existing local files need a one-shot migration batch job when switching to S3.
- Files uploaded during the migration cutover need careful atomic routing logic.

---

### #8 — OpenTelemetry traces + Prometheus metrics + Sentry error tracking

| Field | Value |
|---|---|
| Category | `ops` |
| Complexity | 3 — multi-package, config-heavy |
| Business impact | 3 — invisible until first incident, then transformative |
| Priority score | **15** |
| Estimated effort | 5 developer-days |
| Affects | `Program.cs`, new `Extensions/ObservabilityExtensions.cs`, `.csproj` packages |

**What and why**  
Current observability is JSON file logs only. No RPS/latency metrics, no distributed traces, no error aggregator. The first production incident will require a manual log dive with grep.

**Implementation steps**
1. Add NuGet: `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Instrumentation.EntityFrameworkCore`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `Sentry.AspNetCore`.
2. Wire OTel tracing + metrics in `AddObservability(builder.Configuration)` — export to OTLP (Honeycomb / Grafana Tempo+Mimir / SigNoz).
3. Define custom metrics:
   - `orders_created_total{processor,status}` (counter)
   - `payment_webhook_total{processor,result}` (counter)
   - `upload_size_bytes` (histogram)
   - `order_processing_duration_seconds` (histogram)
4. Integrate Sentry for unhandled exceptions; tag with `correlation_id` and `user_id`.
5. Expose `/metrics` Prometheus endpoint (or rely on OTLP push).
6. Document SLOs: availability ≥ 99.5%, p95 checkout latency ≤ 1.5 s, payment-webhook success ≥ 99.9%.

**Schema / API changes** — None.

**Risks**
- OTel adds ~5–10% CPU overhead — tune sampling rate on high-traffic endpoints (`/api/uploads/{id}/preview`).

---

### #9 — SignalR Redis backplane + distributed rate-limiter cache

| Field | Value |
|---|---|
| Category | `scalability` |
| Complexity | 2 — NuGet package + connection string |
| Business impact | 3 — required for multi-instance; low value at 1 replica |
| Priority score | **17** |
| Estimated effort | 2 developer-days |
| Affects | `Program.cs`, `docker-compose.yml`, new `Redis` config section |

**What and why**  
`AddSignalR()` and `AddMemoryCache()` are single-process. With two API replicas behind a load balancer, admin SignalR clients only receive order notifications from the replica they connected to, and the in-memory locker-list cache loses hit-rate. Rate limiter state is also per-instance — clients can bypass the 10-req/min auth limit by alternating requests across replicas.

**Implementation steps**
1. Add `Microsoft.AspNetCore.SignalR.StackExchangeRedis` and `Microsoft.Extensions.Caching.StackExchangeRedis`.
2. `AddSignalR().AddStackExchangeRedis(connectionString)`.
3. Replace or wrap `AddMemoryCache()` with a two-level cache: L1 in-memory (fast, per-instance) + L2 Redis (shared). Use Redis for the locker list and product catalog cache.
4. Add Redis to `docker-compose.yml` and `docker-compose.prod.yml`.
5. Consider moving rate-limiter partitions to Redis via `AspNetCoreRateLimit` or custom Redis-backed partition.

**Schema / API changes** — None.

**Risks**
- Redis becomes a tier-1 dependency — deploy with persistence (`appendonly yes`) + monitoring.

---

### #10 — Coupon / promo code system

| Field | Value |
|---|---|
| Category | `feature` |
| Complexity | 3 — schema + service + validation + UI |
| Business impact | 3 — marketing lever for retention and seasonal promos |
| Priority score | **15** |
| Estimated effort | 6 developer-days |
| Affects | new `Models/Coupon.cs`, `Models/CouponRedemption.cs`, `Services/ICouponService.cs`, `Controllers/CartController.cs`, checkout UI, admin UI |

**What and why**  
No discount mechanism exists. First-order discounts, "FREESHIP" codes, and seasonal promotions (BlackFriday, Valentine's Day) are effectively table-stakes for a Romanian e-commerce site. This should be sequenced **after** VAT (#4) so discounts apply to the pre-VAT subtotal correctly.

**Implementation steps**
1. Add `Coupon` and `CouponRedemption` entities (see schema).
2. `POST /api/cart/coupon { code }` — validate and return preview discount. `DELETE /api/cart/coupon` to remove.
3. In `OrderService.CreateFromCartAsync`: look up applied code, subtract `DiscountRon` from subtotal before VAT calculation, persist `Order.DiscountRon` + `Order.CouponCode`, create `CouponRedemption` in the same transaction. Use `RowVersion` on `Coupon` to prevent over-redemption races.
4. Admin CRUD: `GET/POST/PUT/DELETE /api/admin/coupons` + redemption stats.
5. Frontend: coupon input on cart page; show discount line on review + confirmation pages.

**Schema / API changes**
```sql
CREATE TABLE "Coupons" (
    "Id"              uuid PRIMARY KEY,
    "Code"            varchar(50) NOT NULL UNIQUE,
    "Type"            varchar(20) NOT NULL,  -- Percent | Fixed | FreeShipping
    "Value"           numeric(10,2) NOT NULL,
    "MinSubtotalRon"  numeric(10,2) NOT NULL DEFAULT 0,
    "ValidFrom"       timestamptz NOT NULL,
    "ValidUntil"      timestamptz NOT NULL,
    "MaxRedemptions"  int NULL,
    "RedemptionsCount" int NOT NULL DEFAULT 0,
    "IsActive"        boolean NOT NULL DEFAULT true,
    "RowVersion"      bytea NOT NULL DEFAULT '\x00'
);
CREATE TABLE "CouponRedemptions" (
    "Id"          uuid PRIMARY KEY,
    "CouponId"    uuid NOT NULL REFERENCES "Coupons"("Id"),
    "OrderId"     uuid NOT NULL REFERENCES "Orders"("Id"),
    "UserId"      uuid NULL REFERENCES "Users"("Id"),
    "DiscountRon" numeric(10,2) NOT NULL,
    "RedeemedAt"  timestamptz NOT NULL
);
ALTER TABLE "Orders"
    ADD COLUMN "CouponCode"   varchar(50) NULL,
    ADD COLUMN "DiscountRon"  numeric(10,2) NOT NULL DEFAULT 0;
```

**Risks**
- Race condition on `MaxRedemptions` — use `RowVersion` optimistic concurrency, retry once on conflict.
- Sequence after #4 (VAT) to avoid retro-fitting the calculation order.

---

## Roadmap

### Now (< 2 weeks) — Quick wins
*Score ≥ 20 AND complexity ≤ 2*

| # | Proposal | Score | Effort |
|---|---|---|---|
| 1 | Fix `UploadCleanupJob` data-loss bug | 23 | 2 d |
| 2 | Server-side shipping cost + payment idempotency | 23 | 3 d |
| 6 | Rotate RSA private key out of repo | 22 | 1 d |
| 5 | Dockerfile + compose + GitHub Actions CI/CD | 20 | 3 d |

### Next (2–8 weeks) — Main sprint
*Score 14–19 or complexity 3*

| # | Proposal | Score | Effort | Depends on |
|---|---|---|---|---|
| 3 | Sameday API integration (AWB + tracking) | 19 | 10 d | — |
| 4 | Romanian VAT + e-Factura compliance | 19 | 12 d | — |
| 7 | Thumbnail cache + S3/Blob storage | 18 | 5 d | #5 |
| 9 | Redis backplane (SignalR + cache + rate-limit) | 17 | 2 d | #5 |

### Later (> 8 weeks) — Strategic
*Lower scores or complex, require dedicated resourcing*

| # | Proposal | Score | Effort | Depends on |
|---|---|---|---|---|
| 8 | OpenTelemetry + Sentry + metrics | 15 | 5 d | #5 |
| 10 | Coupon / promo code system | 15 | 6 d | #4 (VAT first) |

### Dependency notes
- **#5 (deploy artefacts)** must land before #7 and #9 deliver real value (multi-instance infra).
- **#4 (VAT)** must land before **#10 (coupons)** — discounts must apply to pre-VAT subtotal to avoid retro-fitting invoice calculations.
- **#2 (shipping cost server-side)** should land before **#3 (Sameday)** to prevent fraudulent orders triggering real AWB creation.
