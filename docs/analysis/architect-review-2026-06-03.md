# Architecture Review — FotoTipar Photo Printing Platform

> **Snapshot of June 2026.** Statements about SQLite describe the app at that date; since 2026-08-20 the application is PostgreSQL-only in every environment (see memory-bank/standards/data-stack.md).

**Date:** 2026-06-03
**Branch:** `analysis/architect-review` (stacked off the bolt-039 line)
**Reviewer:** ArchitectAnalyst (senior architect persona)
**Scope:** Backend (`PhotoPrint.API`), Frontend (`PhotoPrint.UI`), Tests (`PhotoPrint.Tests`), ops/CLI, observability/security postures, and the 24-ADR decision history.

This review is requested by the maintainer because the codebase has grown — 375 backend .cs files (~15 KLOC excluding migrations), 121 test files (~16 KLOC), 96 TypeScript files (~13 KLOC), 11 hosted services, 14 controllers — and the original "flat, type-based, monolithic" structure is showing strain. It addresses three explicit pain points (scaling pains across all three projects, dependency sprawl, and discoverability of hidden functionality for regression) plus the broader cross-cutting concerns the maintainer invited.

---

## Business summary

FotoTipar is a Romanian-market B2C e-commerce platform for photo printing. End users — guest or registered — upload photos, pick sizes/finishes, pay via Stripe or EuPlatesc, and receive their order via Sameday Easybox locker or courier. Romanian fiscal regulation (VAT, sequential invoice numbering, ANAF SPV e-Factura submission) is a first-class, deeply integrated requirement; the bolt 038/039 line just shipped it. Single-tenant, monolith, pre-deployment.

## Detected tech stack

- **Backend**: ASP.NET Core 8 Web API, C# 12, EF Core 8 (Npgsql prod / SQLite dev / InMemory tests). FluentValidation, Polly v8 (resilience + rate limiting), Serilog, MailKit/SendGrid, Stripe.net 46.3.0, QuestPDF 2024.12.3, RazorLight, SixLabors.ImageSharp, AWSSDK.S3, OpenTelemetry 1.11.x, Sentry 4.13.
- **Frontend**: **Angular 21.2** (standalone components, lazy-loaded feature routes — `app.routes.ts` confirms full lazy loading), TypeScript 5.9 strict, Vitest 4 (NOT Jasmine/Karma — `tech-stack.md` is stale). Stripe.js 9.6, SignalR 10, Chart.js 4.5, Leaflet 1.9. `heic2any` and `ng2-charts` are listed in `tech-stack.md` but NOT in `package.json` — drift.
- **Database**: PostgreSQL 16 (prod via Npgsql) + 21 migrations + 17 entity sets in `PhotoPrintDbContext`. SQLite for dev, InMemory for tests. Query-splitting enabled globally.
- **Infrastructure**: Single Docker image (Caddyfile + Dockerfile + docker-compose.prod.yml at root). No K8s, no Helm. Not yet deployed.
- **Observability**: OTel traces (OTLP) + Prometheus metrics + Sentry — all gated by master flags (`Observability:Enabled`, `Sentry:Enabled`).
- **Test counts**: 941/948 passing (7 known skips or expected failures per the user brief).

## Business workflows found

**Domain classification:** E-commerce with a regulated-fiscal sub-domain (Romanian VAT + ANAF e-Factura).

| Workflow | Endpoints | Key models | Status |
|---|---|---|---|
| Identity & auth (register, login, refresh, password reset, email confirm, Google OAuth) | `/api/auth/*` | `User`, `RefreshToken`, `EmailConfirmationToken`, `PasswordResetToken`, `ExternalLogin` | Complete |
| Guest session lifecycle (init, contact-update, claim) | `/api/auth/guest/*` | `GuestSession` | Complete |
| Product catalogue (read + admin CRUD) | `/api/products`, `/api/admin/products/*` | `Product`, `ProductSize`, `ProductFinish`, `PricingTier` | Complete |
| Photo upload (single + batch + preview) | `/api/uploads/*` | `Upload`, `StorageLocation` (Local/Cloud) | Complete |
| Cart (guest-or-auth) | `/api/cart/*` | `CartItem` | Complete |
| Order creation + idempotent re-submit | `/api/orders/*` | `Order`, `OrderItem`, `ProductSnapshot`, `ShippingAddressSnapshot` | Complete |
| Payment intent + webhook (Stripe + EuPlatesc) | `/api/payments/*`, `/api/webhooks/*` | `Order.PaymentIntentId`, `IdempotencyKey` | Complete |
| Photo promotion to cloud (post-paid) | (background only) | `Upload.StorageLocation`, `PromotionRecoveryScanner` | Complete |
| Photo archive retention (purge originals, then prune) | (background only) | `Order.PaidAt` anchor, `Archive` settings | Complete |
| Shipping — Sameday AWB + tracking | (webhook → background) | `Order.AwbNumber`, `Order.LastTrackingSyncAt`, `Order.DeliveredAt` | Complete |
| Invoicing — VAT, numbering, UBL XML, PDF, ANAF SPV upload, admin retry/XML download | `/api/invoices/*`, `/api/admin/invoices/*`, ANAF SPV | `Invoice`, `InvoiceAnafStatus` | Complete |
| Admin orders + stats + SignalR real-time hub | `/api/admin/orders/*`, `/api/admin/stats`, `/hubs/admin-orders` | `Order`, snapshots | Complete |
| Account deletion (GDPR) | `/api/account/delete-request` + background job | `User.DeletionRequestedAt` | Complete |
| Saved addresses | `/api/account/addresses` | `SavedAddress` | Complete |
| Refund flow | (None — no refund endpoint exists) | — | **Missing** |
| Inventory / stock | (None) | — | Intentionally absent (made-to-order; flagged below) |

The two genuinely missing workflows are **refund/return** and **discount/coupon**. Neither is a bug — but both are visible holes for an e-commerce site in the EU (refund is a legal right under EU consumer-protection law; coupons are a standard growth lever). They appear in the proposals.

## Findings

### Security

**The security posture is strong, with three concrete gaps.**

What's in place (don't re-propose):
- JWT RS256 with refresh-token rotation, HttpOnly cookie. JWT signing key rotated after the historical leak (ADR-006). Pre-commit + CI gitleaks scan.
- FluentValidation everywhere (validators in `Validators/{Account,Admin,Auth,Cart,Invoices,Payments}`); `[ApiController]`'s default 400 suppressed in favour of a 422 ProblemDetails contract (ADR-002).
- ProblemDetails RFC 7807 globally via `ExceptionHandlerMiddleware`.
- Webhook signature verification (Stripe + EuPlatesc HMAC-MD5).
- Rate limiting via Polly.RateLimiting on auth endpoints (`AuthRateLimitPolicy`, `RegisterRateLimitPolicy`, `ResendConfirmationRateLimitPolicy`, `ForgotPasswordRateLimitPolicy`).
- Security headers middleware (HSTS, X-Content-Type-Options, X-Frame-Options, CSP in appsettings).
- Idempotency key on payment intent creation (ADR-005); state-conflict 409 vs validation 422 split (ADR-004).
- `/metrics` IP allow-list (ADR-018) — correct call.
- MIME-magic-byte validation (`MimeValidator.cs`) on uploads, not just Content-Type sniffing.
- Sentry data scrubber to strip PII (`SentryDataScrubbers.cs`).
- Production-environment-gated boot validation for Stripe/EuPlatesc secrets (Program.cs:241–253).

**Concrete gaps:**

1. **No global rate limit on the non-auth API surface.** Auth endpoints are protected; everything else relies on Caddy/Nginx limits which aren't configured. `/api/uploads/batch` accepts 500 MB and `/api/admin/invoices/{id}/xml` returns the full XML — both are amplification-attractive.
2. **Admin role check is string-based** (`[Authorize(Roles = "Admin")]`) with no centralised policy. There is no `AdminPolicy` constant — six controllers literal-string the role. Trivial to typo into a no-op. Not a vulnerability today but a footgun.
3. **CORS is single-origin** (`Cors:AllowedOrigins = "https://fototipar.ro"` — a string, not a list). Multi-environment deployments (staging.fototipar.ro) will need this to become a list. Minor.
4. **No CSP nonce/hash strategy for inline styles.** The CSP in appsettings forbids inline scripts (good) but Angular emits inline styles for components. If a strict CSP review is done pre-launch, `style-src 'self'` will break Angular's runtime styles. Currently the CSP is not strict (`default-src 'self'` + `script-src 'self' ...`) and inline styles fall back to `default-src` which won't allow `'unsafe-inline'`. Worth verifying on the rendered page before launch.
5. **`GuestAuthenticationHandler` accepts `X-Guest-Token` over plaintext if HSTS isn't yet primed for first-visit users.** This is industry-standard but flag it — guest tokens are bearer credentials with 7-day TTL. If a guest cart can include PII (shipping address), session-fixation via stolen token is a real threat vector. The SHA-256 hashing in DB only mitigates DB-leak risk, not in-transit theft.

**OWASP top-10 surface check (high-level):**
- A01 Broken access control — Authorize attributes present; admin-only string-based (minor).
- A02 Cryptographic failures — JWT RS256, bcrypt password hash, SHA-256 token hash. Strong.
- A03 Injection — EF Core parameterised queries; no `FromSqlRaw` in the codebase. Strong.
- A04 Insecure design — idempotency and webhook duplicate handling explicitly designed. Strong.
- A05 Security misconfig — CSP present, HSTS yes, security headers middleware present. Strong.
- A06 Vulnerable components — `OpenTelemetry.Exporter.OpenTelemetryProtocol 1.11.2` has a Moderate CVE (GHSA-4625-4j76-fww9). Fix is a 1.15.x bump. Flagged in Dependencies.
- A07 Auth failures — refresh-token rotation, rate-limited login. Strong.
- A08 Software/data integrity — UBL XML written and read; signature verification on webhooks. Strong.
- A09 Logging — structured Serilog + Sentry + OTel. Strong.
- A10 SSRF — outbound HTTP only to allowlisted partners (Sameday, ANAF, Stripe, Google). No user-controlled URL fetching. Strong.

### Scalability

**Designed for a pre-deployment system. Most "scale" concerns are deferred correctly (ADR-010, ADR-013, ADR-023, the bolt 046 deprioritization).** The real near-term bottlenecks are different:

1. **Single-replica binding.** Three subsystems are explicitly in-process: promotion queue (`Channel<T>`, ADR-010), Sameday token cache (ADR-013), AWB jobs (ADR-015 accepts dupe). All correct calls. Bolt 046 is deprioritised — fine. But the implications need to be **documented in one place** for any future operator (today they're scattered across ADRs 010/013/015/016/023). See proposal #12.
2. **N+1 risk in `OrderService.GetOrderPhotosAsync` (lines 350–357 of `OrderService.cs`).** Inside a foreach over `viewable`, the code calls `GetPresignedUrlAsync` twice per upload (thumbnail + large). For an order with 30 uploads that's 60 sequential awaits against S3. Cloudflare R2 presign is local-cpu and fast, but at 30+ photos with TLS round-trip overhead it adds up. **Fix:** `await Task.WhenAll(...)` the presign calls (presigning is stateless and the SDK is thread-safe).
3. **Cart-load join cost (`OrderService.CreateFromCartAsync`).** Lines 53–65 do `Include(ci => ci.Product).ThenInclude(p => p.Sizes).ThenInclude(s => s.PricingTiers)` plus `Include(ci => ci.Product).ThenInclude(p => p.Finishes)` plus `Include(ci => ci.Upload)`. With query-splitting on (good) this becomes 4 round-trips; without it, a cartesian explosion. The pattern repeats in `CartService` (296 LOC). Worth caching `Product` (`AddMemoryCache` is registered but only used for the locker list per the system-arch doc); a Product entity rarely changes.
4. **`AdminInvoicesController.ListAsync`** uses a client-side EF `Join` (lines 59–70). This expands to a SQL JOIN, which is fine, but `.AsNoTracking()` + `OrderByDescending(i.CreatedAt) + Skip + Take` — without an index on `Invoices(CreatedAt)` — will full-scan as the table grows. Need a confirmation that the migration added an index.
5. **`/api/uploads/{id}/preview` Local-tier returns a `FileStream` that holds an OS handle for the duration of the response.** Under concurrent load that's bounded by `kernel ulimit -n`. Cloud tier (302 to presigned URL) sidesteps it. This is well-known and acceptable, but as the user said "scale-out path matters" — the Local tier is the bottleneck, and the Confirmed-Write-Then-Delete promotion (ADR-011) means pre-payment serving stays Local. A CDN in front would help but isn't trivial because previews are private.
6. **Background jobs all use `IServiceScopeFactory.CreateScope` + DbContext per tick.** Correct. But each tick does a SELECT-then-N-UPDATEs sequence — `InvoiceUploadJob` (267 LOC) does this for every Pending+Submitted row in the batch. At scale (10K invoices) the per-row scope creation cost adds up; for now it's fine because batch size caps at 50.
7. **No connection-pool ceiling configured.** EF's Npgsql connection pool defaults are used. Once deployed, `MaxPoolSize=200` (or some value tied to expected replica count × DB connection budget) wants to be in the connection string. Bolt 046 deprioritization makes this a Now-rather-than-Later concern; capacity planning must happen at launch.

### Observability

**Score: 4/5.** Solid — bolts 044 and 045 just shipped. Real gaps:

1. **No business-metric labels for invoices.** `FotoMetrics.OrdersCreated` and `FotoMetrics.PaymentWebhook` exist; nothing parallel for `invoices_uploaded_total{status}` despite the entire bolt 039 ANAF lifecycle being SLO-relevant (5-business-day legal SLA per ADR-024). Without this, SRE has to derive ANAF lag from logs.
2. **No SLO for upload throughput or batch upload latency.** `slos.md` covers webhooks but uploads — the highest-volume request type — are unmeasured. The route-level sampling override (`Observability:Sampling:Routes`) for `GET /api/uploads/{id}/preview` is 5%, which is appropriate for cost — but the trace data is the only thing that would catch a slow-S3-presign regression.
3. **Sentry release tagging depends on `GIT_COMMIT_SHA` env var.** If the deploy workflow forgets to set it, Sentry events have no release linkage and you can't bisect. Worth adding a CI assertion.
4. **No health-check endpoint dedicated to background-job liveness.** `DbHealthCheck` and `DiskHealthCheck` exist; if `InvoiceUploadJob` crashes silently (caught its own exception, never threw it out, `BackgroundService` swallows), the only signal is a missing metric increment. Operators wouldn't notice for hours.
5. **No structured `request_started_at` on the SignalR hub** — `AdminOrderHub` broadcasts with no trace correlation. Admin debugging "did the customer order come in?" has to manually correlate.
6. **`/metrics` endpoint has no end-to-end test for the IP allow-list behaviour with `X-Forwarded-For`.** `MetricsEndpointIpAllowListMiddleware` reads `Connection.RemoteIpAddress`; behind Caddy/Nginx that's the proxy's IP unless `ForwardedHeadersMiddleware` is registered. Looking at `Program.cs`: it isn't. So the allow-list will be wrong on day-1 of deployment.

### Missing capabilities

Universal capability matrix:

| Capability | Status | Note |
|---|---|---|
| Authentication | Present | JWT RS256, dual-auth (JWT or X-Guest-Token) |
| Authorisation / RBAC | Partial | String-based "Admin" role; no policy constant |
| Input validation | Present | FluentValidation everywhere; ADR-002 enforces it |
| Structured error responses | Present | ProblemDetails RFC 7807 globally |
| Pagination | Partial | Present on admin/orders, invoices, my-orders; **inconsistent defaults** (10 vs 20 vs 50) |
| Rate limiting | Partial | Auth endpoints only; no global default |
| Soft deletes | Partial | `Upload.DeletedAt` yes; `User`/`Order`/`Invoice` no (hard-delete via cascades) |
| Audit log | Partial | Logged via Serilog with admin actor; **no immutable AuditLog table** |
| Health check endpoint | Present | `/health` per ADR-001; DB + disk checks |
| Structured logging | Present | Serilog JSON with correlation IDs |
| Environment config | Strong | Secrets out of source (ADR-006); validators on boot |
| Tests | Present | 941/948 passing; ~16 KLOC of test code |

E-commerce specific:

| Capability | Status | Note |
|---|---|---|
| Cart persistence | Present | DB-backed, guest-or-user |
| Inventory management | N/A | Made-to-order; intentionally absent. But flag if dropshipping/stock changes |
| Order state machine | Present | Enforced via `OrderStatusMachine` |
| Payment integration | Present | Stripe + EuPlatesc + idempotency |
| **Refund / return flow** | **Missing** | No refund endpoint; no `Order.RefundedAt`/`RefundAmount` columns; no Stripe refund webhook handler. Legal requirement under EU consumer law (14-day cooling-off). |
| Tax calculation | Present | Romanian VAT, bolt 038 — gold-standard implementation |
| Email receipts | Present | `OrderEmailService` + Sameday delivery email |
| Search / filtering | Partial | Admin orders has search, customer catalog has filter, no search-as-you-type |
| Product variants | Present | Size + Finish snapshot model |
| **Discount / coupon system** | **Missing** | Common e-commerce capability; absent entirely |

### Code health

**Diagnosed pain points, with file:line specificity.**

1. **`Services/` is a 49-file flat directory of God objects.** Concrete sizes:
   - `OrderService.cs` — 381 LOC, 3 concerns (cart→order, idempotency, query). The `CreateFromCartAsync` method is 145 LOC and mixes: cart load with 4 includes, idempotency resolution, guest email lookup, order-number generation, VAT calculation, order persist, metrics emission. **Refactor target:** extract `CartCheckoutHandler` (handler-pattern) at `src/PhotoPrint.API/Services/Orders/Checkout/CartCheckoutHandler.cs`.
   - `AuthService.cs` — 424 LOC, the single largest service. Likely 6+ concerns (register, login, refresh, confirm-email, password-reset, social claims). **Refactor target:** split into `AuthService` (login/refresh), `AccountRegistrationService` (register + confirm), `PasswordResetService`.
   - `AdminOrderService.cs` — 320 LOC, 296 in `CartService.cs`, 296 in `WebhooksController.cs`. Each is a single-class folder begging to be subdivided.

2. **`Controllers/WebhooksController.cs` is 345 LOC and binds 10 dependencies.** It mixes Stripe and EuPlatesc handlers, calls 4 services (`OrderService`, `OrderEmailService`, `OrderPhotoPromoter`, `AwbCreationNotifier`, `InvoiceCreationService`), broadcasts SignalR, increments metrics, and parses raw JSON. The duplicate "after Paid" logic block (BroadcastNewOrder → FireOrderConfirmedEmail → EnqueueCloudPromotion → NotifyPaidAsync) appears once in `EuPlatescIpnAsync` and once in `HandleStripePaymentSucceededAsync` — perfect duplication. **Extract `OrderPaidEventDispatcher`** that fires the side-effect fan-out.

3. **`Program.cs` is 534 LOC and is the de-facto composition root for everything.** Five distinct sub-DI graphs (Sameday, ANAF, Invoicing, Sentry, Observability) live inline with conditional `if (xEnabled)` blocks. The extensions in `Extensions/*.cs` already exist for some subsystems (`AddSocialAuth`, `AddGuestSessions`, `AddEmailInfrastructure`, `AddPhotoStorage`, `AddPhotoArchive`, `AddSecurityBaselines`, `AddObservability`) — but Sameday, ANAF, Invoicing, Payments, and Sentry are NOT extracted. **Refactor target:** finish what the existing pattern started — extract `AddSameday`, `AddAnaf`, `AddInvoicing`, `AddPayments`, `AddSentry` so Program.cs becomes ~120 LOC of "compose these subsystems."

4. **`OrderService.GetOrderPhotosAsync` (lines 318–360) is in the wrong class.** It's pure cloud-presign logic, never touches Order state, and only uses `_db`, `_storageRouter`, `_storageSettings`. Belongs in a new `OrderPhotoQueryService` or in `IOrderPhotoPromoter`. Symptom of the flat folder.

5. **No Domain layer.** `OrderStatusMachine.cs` (45 LOC, pure static) is the only piece of "domain logic" that lives as a free function. Status transitions, idempotency-key equality (`DivergentFields`), VAT calculation (`VatCalculator`), invoice numbering, and storage-key generation are all single-purpose helpers that drift between Services/ and inline private methods. **Pattern proposed:** introduce a `Domain/` namespace; move purely-functional pieces (`OrderStatusMachine`, `VatCalculator`, `StorageKeys`, `InvoiceNumber`, `PromotionOutcome`, `PurgeOutcome`) there. No new project — just a folder + namespace rename. The boundary becomes "if this class needs `DbContext` or `HttpClient`, it stays in Services/; otherwise Domain/."

6. **`PhotoPrintDbContext.cs` is 437 LOC of inline `modelBuilder.Entity<X>(...)` lambdas in `OnModelCreating`.** A `Data/Configurations/` folder exists (with one file in it — interesting); the entity configurations should all move out to `IEntityTypeConfiguration<T>` classes. This will halve the file and make per-entity diff reviews readable.

7. **Test count vs assertion brittleness.** 941/948 passing means 7 fail consistently — looking at the suite that's expected (S3-integration-on-CI skips, etc.) but the brief said "7 known," and that gap is not documented in a `KNOWN_FAILURES.md`. Test discovery and stability is currently tribal knowledge.

8. **Test folder is type-clustered (`Unit/Services/`, `Unit/Validators/`).** As the test count grew, you got `Unit/Services/` with 40+ files. A future refactor that splits `AuthService` into 3 services will need to split `AuthServiceTests.cs` (probably 500+ LOC) too. A feature-clustered test folder (`Unit/Auth/`, `Unit/Orders/`, `Unit/Invoicing/`, `Unit/Sameday/`) mirrors the proposed feature-folder backend refactor and gives one-pull-request changes for an entire feature.

9. **Documentation rot.** `memory-bank/standards/tech-stack.md` says "Angular 17+", "Jasmine/Karma", lists `heic2any` and `ng2-charts`. Reality is Angular 21, Vitest, neither library installed. Same file claims "MailKit (dev) / SendGrid (prod)" — `SmtpEmailService` and `SendGridEmailService` both exist; the actual provider selection is via `Email:Provider` config, not env. Worth a quarterly standards-doc audit.

### Architectural layering — the deep dive

> Added 2026-06-03 (second pass) in response to maintainer feedback that the first pass didn't go deep enough on layering. Every claim below cites file paths and line numbers.

The project is **a single ASP.NET Core assembly** (`src/PhotoPrint.API/PhotoPrint.API.csproj`) — controllers, services, DbContext, models, validators, DTOs, middleware, background jobs, options classes, observability code, hosted services. There is no `PhotoPrint.Domain`, no `PhotoPrint.Application`, no `PhotoPrint.Infrastructure`. That's a deliberate choice (`memory-bank/standards/system-architecture.md` line 8: *"Monolithic REST API + SPA frontend"*) and it's correct for the scale — but it has been allowed to drift in ways that make it harder than it has to be.

#### A. Layer separation (Presentation / Business / Data Access)

The folder names hint at conventional layers — `Controllers/`, `Services/`, `Data/`, `DTOs/`, `Models/`, `Validators/` — but in practice the layers leak in three concrete ways:

**A.1 — Four controllers inject `PhotoPrintDbContext` directly.** Grep confirms:
- `Controllers/InvoicesController.cs:19,22` — `private readonly PhotoPrintDbContext _db;` + `public InvoicesController(PhotoPrintDbContext db, IStorageService storage)`. The controller then runs its OWN authorization query against `_db.Orders` (lines 44–48), reads the invoice metadata directly (lines 53–57), and even sets the `Retry-After` header inline. This is a Presentation→Data Access shortcut around the Service layer.
- `Controllers/AdminInvoicesController.cs:22,27,45–71` — composes a `Join` between `_db.Invoices` and `_db.Orders` *inside the controller action* to build the admin list. Pagination logic, projection logic, filtering logic — all in the controller.
- `Controllers/PaymentsController.cs:22,31,73–75,110–111` — mutates `order.PaymentIntentId` then calls `_db.SaveChangesAsync` directly. The "create an order" call went through `IOrderService` (good), but the follow-up write that links the payment intent back to the order skips the service layer entirely.
- `Controllers/WebhooksController.cs:26,40,206,227,272,298,336–337` — six direct `_db.SaveChangesAsync` calls and explicit lazy-load via `_db.Entry(order).Collection(...).LoadAsync()`. The webhook handler is doing data-access orchestration that belongs to an order-paid command handler.

**A.2 — Two services depend on `IConfiguration` directly instead of typed options.** `Services/SamedayShippingService.cs:31` and `Services/StaticShippingService.cs:11,13` take `IConfiguration` in the constructor. Every other shipping/payment/storage component takes `IOptions<XSettings>` (the right pattern — `Configuration/SamedaySettings.cs` exists and is validated by `Validators/SamedaySettingsValidator.cs`). These two are the **only two services in 49** that still untyped-sniff config. Quick-fix targets.

**A.3 — `IQueryable` does NOT leak from services.** A positive finding: `grep IQueryable src/PhotoPrint.API/Services` returns zero matches. Every service materialises via `ToListAsync` / `FirstOrDefaultAsync` before returning. This means a future "introduce repositories" refactor is NOT blocked by leaked query trees.

**A.4 — `Models/` is correctly POCO-free of presentation attributes.** Spot-check of `Models/Order.cs` — no `[ApiController]`, no `[FromBody]`, no `[JsonPropertyName]`. The 17 entities are clean POCOs. The `Data/Configurations/` folder exists with one file (`UploadConfiguration.cs`) but the other 16 entity configurations are inline in `Data/PhotoPrintDbContext.cs.OnModelCreating` — a half-finished refactor flagged in the first pass (P15).

**A.5 — There is no `Application/` boundary at all.** Use cases (`CreateFromCartAsync`, `PromoteOrderPhotosAsync`, `RetryInvoiceUploadAsync`) live as methods on coarse-grained `*Service.cs` classes. There is no command/handler abstraction. The duplicated post-Paid fan-out in `WebhooksController.cs` (EuPlatesc lines 199–216 ≈ Stripe lines 267–283) is the canonical symptom: when two callers need the same sequence of side effects, there is nowhere natural to put the sequence.

**Verdict:** Layers exist by folder name but leak by access path. The pre-deployment, single-tenant, single-team context does NOT justify a four-project split (P22 below evaluates that and recommends against), but it DOES justify codifying the layer rules + enforcing them (P21 + P23 + P24 below).

#### B. Interface ↔ implementation co-location

The maintainer's exact complaint. Concrete evidence:

- `Services/` (top level) has **32 `I*.cs` files** alphabetically interleaved with **40 implementation files** — `IAuthService.cs` immediately above `IAccountService.cs`; `OrderService.cs` immediately above `OrderStatusMachine.cs`. The folder listing is unreadable.
- The Sub-folders (`Services/Invoicing/`, `Services/Sameday/`) are **also** interleaved — `Services/Invoicing/IInvoiceCreationService.cs` next to `InvoiceCreationService.cs`. The team adopted feature folders but kept the type-mixed-with-interface convention inside them.
- **Positive:** no interface is declared *inside* an implementation file. Each `IFoo` has its own file. This makes the eventual refactor purely mechanical (move + namespace).
- **Positive:** there is no `Abstractions/` folder anywhere, but there is also no inconsistency — the convention is uniformly "interface and class side-by-side, both at the same flat level." That's a coherent (if cluttered) convention.

**Verdict:** This is the cleanest single intervention available. A 1-day refactor that introduces an `Abstractions/` subfolder per feature folder would halve the folder noise. See P23.

#### C. Data access layer

There is **no repository pattern**. Services inject `PhotoPrintDbContext` directly and write LINQ-to-Entities inline. Evidence:

- `OrderService.cs:53–65` — 12 lines of `Include(...).ThenInclude(...).ThenInclude(...)` chain inside the service method. This shape repeats in `CartService.cs` (which loads the same cart-include tree, by definition).
- `AdminOrderService.cs` (320 LOC), `AdminProductService.cs` (272 LOC), `OrderEmailService.cs` (228 LOC) all do their own EF Core queries. The query-logic-per-service is genuinely repetitive.
- `OrderService.GetByPaymentIntentIdAsync` and `OrderService.GetByOrderNumberAsync` and `OrderService.GetByIdAsync` are obvious "by-key" queries that could move to an `IOrderRepository` if we wanted that abstraction.

**There is also no Unit-of-Work abstraction.** Each service calls `_db.SaveChangesAsync` at its own discretion. `WebhooksController.cs:206` is an example of cross-service coordination — `IInvoiceCreationService.CreateForOrderAsync` is called and then the controller's own `_db.SaveChangesAsync` commits the transaction. This works because the controller and service share the same scoped DbContext, but it's load-bearing implicit state.

**Verdict:** For a pre-deployment single-DB monolith, the "no repositories, direct DbContext, document the rules" posture is correct. But the rules need to be written down (today they aren't), and one architectural rule must be enforced: **`IQueryable` may not leak from a service** (current truth — preserve it). See P24.

#### D. Application layer / use-case orchestration

There is no handler-per-use-case pattern. The 145-LOC `OrderService.CreateFromCartAsync` (lines 45–195 of `OrderService.cs`) mixes 8 concerns: cart load (4-level Include), idempotency window check, guest-vs-user email resolution, order-number generation, VAT extraction call, order persistence, idempotency-key persistence, metrics emission. The duplicated EuPlatesc/Stripe post-Paid fan-out is the same anti-pattern viewed from the controller side. See P25.

#### E. UI layer scaling

The Angular code is the project's healthiest layer overall — `core/services/` are tight (largest is `auth.service.ts` at 179 LOC; mean ≈ 75 LOC). The structural problems are in **components** and **routes**, not services:

- `features/home/home-page.ts` is **951 LOC** — by far the largest TypeScript file in the project. Inline template + inline `OnInit` data fetching + computed signals + DecimalPipe. Should be at least four smaller components (hero, features, pricing-teaser, trust-strip) under `features/home/components/`.
- `features/account/pages/saved-addresses/saved-addresses-page.ts` (498 LOC) and `features/account/pages/profile/profile-page.ts` (473 LOC) are both **page components mixing form state, validation, API calls, and UI state**. No smart-vs-dumb split — these pages own their forms inline.
- `features/checkout/pages/delivery-step.ts` (382 LOC), `features/orders/pages/order-detail-page.ts` (381 LOC), `features/admin/pages/products/admin-products-page.ts` (381 LOC), `features/cart/pages/cart-page.ts` (380 LOC), `features/upload/pages/format-selector/format-selector-page.ts` (362 LOC) — five more 350+ LOC pages, each mixing presentation with data orchestration.
- **There is no `BaseApiService`** — all 14 services in `core/services/` hand-roll `HttpClient` calls. Repeated error-translation, repeated `withCredentials: true` flags, repeated idempotency-key threading. A 50-line shared base would dedupe meaningfully.

See P26.

#### F. Test layer scaling

The maintainer asked: are unit tests actually unit tests? The answer is **only partly**:

- **25 tests under `tests/Unit/` construct a `PhotoPrintDbContext` directly** (`grep "new PhotoPrintDbContext" Unit/ -r | wc -l = 16`, plus inheritance via mocks raises it to ≥25 once `Unit/Invoicing/` and `Unit/Sameday/` are included). These are integration tests pretending to be unit tests — they exercise real EF Core LINQ translation against the InMemory provider. Slow, brittle when EF behaviour changes, and they couple service tests to schema.
- **`tests/Unit/Services/OrderServiceTests.cs` is 645 LOC** — the largest single test file. `AuthServiceTests.cs` is 636 LOC. Both grew in lockstep with the services they cover. The P06 backend feature-folder refactor needs a companion test-folder refactor or these monoliths will block the service split.
- **There is no `IntegrationTestBase` / `TestApplicationFactory`.** 11 distinct `WebApplicationFactory<Program>` subclasses (`AccountFactory`, `AuthFactory`, `CartFactory`, `GuestSessionFactory`, `OrdersFactory`, `PaymentFactory`, `ProductCatalogFactory`, `SecurityBaselineFactory`, `SentryIntegrationFactory`, `ShippingFactory`, `SocialAuthFactory`, `UploadFactory` + the inline `CloudUploadFactory`). **Every one of them duplicates the same 30–80 lines of InMemory-DB / JWT-keys / Cors / RateLimit / Email config.** The bolt 044 author started the right refactor — `MetricsEndpointIntegrationTests.cs:88` declares `internal abstract class ObservabilityFactoryBase` — but stopped there. That base needs to be promoted and shared across all 11 factories.
- **There are no `TestBuilders` / `TestDataFactories`.** Each test file inlines its own user/order/cart seeding. `AuthFactory.SeedConfirmedUserAsync` is the closest thing but it's confined to one factory.

See P27.

#### G. Cross-cutting concerns

**G.1 — `TimeProvider` is half-adopted.** This is the most surprising finding. The newer code (`Services/Invoicing/InvoiceCreationService.cs:20,27`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs`, `Services/Sameday/AwbCreator.cs`, all of bolt 037+039+044) injects `TimeProvider _clock` and tests use `FakeTimeProvider`. The older code (`Services/AuthService.cs:79,109,127,151,179,187,210,229,269,299,317,339,357` — **13 raw `DateTimeOffset.UtcNow` calls in a single file**, `Services/OrderService.cs:193`, `Services/AccountService.cs`, `Services/CartService.cs`, plus all `Models/*.cs` defaults, plus `BackgroundJobs/UploadCleanupJob.cs`) hard-codes the system clock. Grep reports **63 raw `DateTime(.Offset)?.UtcNow` calls across 35 files**, alongside 12 files that use `TimeProvider`. This is the canonical "we learned, but didn't go back" pattern. Critically, it means unit tests of `AuthService` can't deterministically test token expiry without freezing the system clock.

**G.2 — Admin role string is duplicated 6+ times.** `[Authorize(Roles = "Admin")]` appears as a literal in `AdminInvoicesController.cs:19`, `AdminOrdersController.cs`, `AdminProductsController.cs`, `AdminStatsController.cs`, `AccountController.cs` (admin endpoints), and at least one place inside `WebhooksController.cs`'s admin retry. One typo (`"admin"`, `"Admin "`) silently opens the endpoint. Already flagged in P08, but adding it here as the canonical "shotgun surgery" pattern.

**G.3 — Inline `OrderStatus` strings in metrics emission.** `MetricNames.cs` constants exist; some webhook code uses them, others use inline strings. Not a critical bug but a consistency drift.

**G.4 — Validators are feature-folder-organised, but their *settings-validator* siblings are NOT.** `Validators/` has `Auth/`, `Cart/`, `Invoices/`, etc. — feature folders. But the **settings validators** (`AnafSettingsValidator.cs`, `InvoicingSettingsValidator.cs`, `ObservabilitySettingsValidator.cs`, `SamedaySettingsValidator.cs`, `SellerSettingsValidator.cs`, `SentrySettingsValidator.cs`, `VatSettingsValidator.cs`) live FLAT at the `Validators/` root. They validate strongly-typed options classes from `Configuration/` and belong next to them — or at least next to each other in a `Validators/Settings/` subfolder. Same drift as the `Services/` folder: a half-applied convention.

**G.5 — 11 `BackgroundJobs/` files live flat, not feature-grouped.** `AwbDispatcher.cs`, `AwbRetryJob.cs`, `ShipmentTrackingJob.cs` are all Sameday-feature; they live alongside `EmailRetryJob.cs`, `UploadCleanupJob.cs`, `OrderPhotoPromotionWorker.cs`. The `Sameday/` folder under `Services/` doesn't contain its jobs.

### Frontend

The Angular shape is **better than the backend.** Full lazy-loading of every feature module (`app.routes.ts` is 70 lines, every non-trivial route is `loadChildren` or `loadComponent`). Standalone components throughout. State management is `BehaviorSubject` in core services — appropriate for the scale; ADR/standards explicitly rejected NgRx.

Real concerns:

1. **No bundle-size budget enforcement in CI.** Angular's `angular.json` default budgets exist; nothing pins them in PR gating. As more features ship (admin charts, leaflet, signalr), main.bundle.js bloats. A budget assertion in CI ("main < 500KB, lazy chunks < 200KB") is a 1-line ESLint-equivalent.
2. **14 core services in `core/services/`, none above ~200 LOC by quick spot-check, but they share an HTTP-client pattern that isn't DRY.** Each service hand-rolls `HttpClient` calls. A `BaseApiService` (or RxJS operator wrapper) could centralise error translation, retry, and idempotency-key threading.
3. **No e2e tests.** Standards doc mentions "Cypress or Playwright" but the codebase has neither. For a pre-launch e-commerce site processing real money, at least 3 e2e scenarios should exist: guest checkout → Stripe → confirmation; logged-in checkout → EuPlatesc → confirmation; admin sees real-time SignalR notification.
4. **`vitest` 4 with no shared test setup file visible.** With 46 spec files, the test bootstrap (HttpClientTestingModule, mock providers, etc.) is likely duplicated. Worth a shared `test.setup.ts` audit.
5. **No service worker / offline experience.** A photo printing site where users upload large files would benefit from upload-resume support (chunked uploads with resumable state). Today, a failed upload at 49.99 MB of 50 MB starts over. Not a launch blocker; future feature.
6. **Mixed Romanian/English in code.** Route segments are Romanian (`/tipareste`, `/comenzile-mele`, `/contul-meu`) — good for SEO. But user-facing strings are inline in templates with no `i18n` layer. If FotoTipar ever opens an English landing page (or Hungarian — common Romania practice), retro-fitting `@angular/localize` will touch every component.

### Dependencies

**Direct answer to the user's "I want to keep track of all of them" concern.**

NuGet — `PhotoPrint.API` and `PhotoPrint.Tests` `.csproj` audit:

| Issue | Detail | Severity |
|---|---|---|
| Stripe.net version drift | API.csproj says `46.3.0`; Tests.csproj says `46.3.0` BUT `dotnet list package --outdated` reports Tests *resolved* `47.0.0` (transitive override). API resolved 46.3.0. **Two Stripe.net versions co-existing across the solution.** | High |
| OpenTelemetry CVE | `OpenTelemetry.Exporter.OpenTelemetryProtocol 1.11.2` has Moderate advisory (GHSA-4625-4j76-fww9). Fix: 1.15.x. | High |
| OpenTelemetry pre-release | Two `1.11.2-beta.1` packages (`Prometheus.AspNetCore`, `EntityFrameworkCore`). NU1902 warnings likely suppressed. Stable EF instrumentation now exists. | Medium |
| Sentry.AspNetCore stale | API.csproj is `4.13.0`; latest is `6.6.0`. Two majors behind. | Medium |
| AWS SDK stale | `AWSSDK.S3 3.7.406`; latest is `4.0.24`. Major version skip; behaviour-compat unclear. | Medium |
| EF Core / Npgsql at 8.x | All `8.0.11`; latest is 10.0.x. .NET 8 LTS is supported until Nov 2026 — fine for now but planning required. | Low |
| QuestPDF stale | `2024.12.3` vs `2026.5.0`. ADR-021 records the choice but not the upgrade cadence. | Low |
| Multiple Microsoft.Extensions.Configuration | Tests pulls `10.0.8`; API uses 8.x transitively. Different versions side-by-side. | Low |

NPM — `npm audit` reports **0 vulnerabilities** in `PhotoPrint.UI/package.json` (clean as of 2026-06-03). `npm outdated` shows everything at most one patch behind (Angular 21.2.11 → 21.2.15; Stripe.js 9.6 → 9.7) — well-maintained.

**No Renovate / Dependabot config.** The repo has no `.github/dependabot.yml` and no `renovate.json`. Every upgrade is manual.

**No centralised version variables.** Both .csproj files hard-code Stripe.net version — there's no `Directory.Packages.props` enabling Central Package Management. CPM would catch the Stripe.net 46/47 drift at restore time (it'd warn or fail).

## 20 Improvement proposals

Scored `priority_score = business_impact*3 + (6-complexity)*2`. Maximum 25.

| Rank | ID | Title | Cat | Cx | Imp | Score |
|---|---|---|---|---|---|---|
| 1 | P01 | Patch OpenTelemetry CVE (GHSA-4625-4j76-fww9) | security | 1 | 4 | **22** |
| 2 | P02 | Unify Stripe.net version + adopt Central Package Management (`Directory.Packages.props`) | ops | 2 | 4 | **20** |
| 3 | P03 | Add `Renovate` config with quarterly grouped upgrade PRs | ops | 1 | 3 | **19** |
| 4 | P04 | Build `/api/admin/system-info` feature manifest endpoint (regression-discoverability) | ops | 2 | 4 | **20** |
| 5 | P05 | Register `ForwardedHeadersMiddleware` so `/metrics` allow-list works behind Caddy | fix | 1 | 3 | **19** |
| 6 | P06 | Refactor `Services/` into feature folders (`Orders/`, `Auth/`, `Invoicing/`, `Sameday/`, `Storage/`) | refactor | 3 | 5 | **21** |
| 7 | P07 | Extract `Program.cs` subsystem composition into 5 new extension methods | refactor | 2 | 3 | **17** |
| 8 | P08 | Add global rate limit + per-endpoint admin role policy constant | security | 2 | 3 | **17** |
| 9 | P09 | Refund / return endpoint (legal-compliance EU 14-day cooling off) | feature | 4 | 5 | **19** |
| 10 | P10 | Centralise feature-flag layer via `IFeatureGate` (typed, testable, single source) | refactor | 3 | 4 | **18** |
| 11 | P11 | Extract `OrderPaidEventDispatcher` to dedupe webhook-side-effect fan-out | refactor | 2 | 3 | **17** |
| 12 | P12 | Write `docs/architecture/multi-replica-readiness.md` consolidating ADRs 010/013/015/016/023 | ops | 1 | 2 | **14** |
| 13 | P13 | Decompose `AuthService` (424 LOC) into 3 services | refactor | 3 | 3 | **15** |
| 14 | P14 | Decompose `WebhooksController` + `OrderService` god-methods | refactor | 3 | 3 | **15** |
| 15 | P15 | Add `IEntityTypeConfiguration<T>` per-entity files; shrink DbContext to <100 LOC | refactor | 2 | 2 | **14** |
| 16 | P16 | Domain layer extraction (`Domain/` namespace; no new project) | refactor | 3 | 2 | **12** |
| 17 | P17 | Background-job liveness health check + ANAF invoice metrics | ops | 2 | 3 | **17** |
| 18 | P18 | Bundle-size CI budget + 3 e2e smoke tests (guest checkout, admin login, real-time SignalR) | ops | 2 | 3 | **17** |
| 19 | P19 | Refresh `tech-stack.md`, add `KNOWN_FAILURES.md`, ADR-quarterly-audit ritual | ops | 1 | 2 | **14** |
| 20 | P20 | Discount / coupon engine (`Coupons` table + redemption flow) | feature | 4 | 4 | **16** |
| 21 | P21 | Codify Presentation / Application / Domain / Infrastructure folder layering inside `PhotoPrint.API` (NO new csproj) | refactor | 3 | 5 | **21** |
| 22 | P22 | Evaluate (and REJECT) the 4-project clean-arch split — write the ADR-NoSplit record | refactor | 1 | 2 | **14** |
| 23 | P23 | Interface ↔ implementation convention — introduce `Abstractions/` subfolder per feature | refactor | 2 | 4 | **20** |
| 24 | P24 | Explicit "no repositories" policy — but DOCUMENT it + add Roslyn-rule that `IQueryable` may not appear in any service public signature | refactor | 1 | 3 | **19** |
| 25 | P25 | Handler-per-use-case pattern — without MediatR; just `IXHandler` interfaces with one handler per controller action | refactor | 3 | 4 | **18** |
| 26 | P26 | UI scaling refactor — break up `home-page.ts` (951 LOC), `saved-addresses-page.ts` (498 LOC), `profile-page.ts` (473 LOC) + introduce `BaseApiService` | refactor | 3 | 3 | **15** |
| 27 | P27 | Shared `IntegrationTestBase` / `TestApplicationFactory` + `TestBuilders/` + audit "unit tests" that secretly use InMemory DB | refactor | 2 | 4 | **20** |
| 28 | P28 | Adopt `TimeProvider` consistently — kill the 63 raw `DateTimeOffset.UtcNow` calls in 35 files | refactor | 2 | 3 | **17** |

---

### #1 — Patch OpenTelemetry CVE (GHSA-4625-4j76-fww9)

| Field | Value |
|---|---|
| Category | `security` |
| Complexity | 1 — version bump + restore |
| Business impact | 4 — known moderate CVE in a deployed observability pipeline is a P1 audit finding |
| Priority score | 22 |
| Estimated effort | 0.5 dev-day |
| Affects | `src/PhotoPrint.API/PhotoPrint.API.csproj` |

**What and why**
`dotnet list package --vulnerable` reports `OpenTelemetry.Exporter.OpenTelemetryProtocol 1.11.2` has a Moderate severity advisory. The fix is in 1.15.x. The whole OTel suite (`AspNetCore`, `EntityFrameworkCore`, `Http`, `Runtime`, `Exporter.Console`, `Extensions.Hosting`) should be bumped in lockstep to 1.15.x — version skew across OTel sub-packages causes initialization failures.

**Implementation steps**
1. Update all six `OpenTelemetry.*` PackageReferences in `PhotoPrint.API.csproj` from `1.11.x` to the matching `1.15.x` line. `EntityFrameworkCore` and `Prometheus.AspNetCore` may still be beta — accept the pre-release if there's no stable peer.
2. `dotnet restore && dotnet build && dotnet test`. Re-run the bolt 044 integration tests (`MetricsEndpointIntegrationTests`).
3. `dotnet list package --vulnerable` should return clean.

**Schema / API changes**
```xml
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol"   Version="1.15.3" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting"               Version="1.15.3" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore"       Version="1.15.2" />
<PackageReference Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="1.11.0-beta.1" /> <!-- pinned, see note -->
<PackageReference Include="OpenTelemetry.Instrumentation.Http"             Version="1.15.1" />
<PackageReference Include="OpenTelemetry.Instrumentation.Runtime"          Version="1.15.1" />
<PackageReference Include="OpenTelemetry.Exporter.Console"                 Version="1.15.3" />
```

**Risks**
- `EntityFrameworkCore` instrumentation is still beta; check release notes for breaking changes.
- Prometheus exporter API surface may have moved (1.11.2-beta.1 → newer beta). Smoke-test the `/metrics` endpoint integration test.

---

### #2 — Unify Stripe.net version + adopt Central Package Management

| Field | Value |
|---|---|
| Category | `ops` |
| Complexity | 2 — add Directory.Packages.props, refactor 2 csproj files |
| Business impact | 4 — eliminates a class of "works in tests, breaks in prod" bugs |
| Priority score | 20 |
| Estimated effort | 1 dev-day |
| Affects | `PhotoPrint.API.csproj`, `PhotoPrint.Tests.csproj`, new `Directory.Packages.props` at solution root |

**What and why**
`PhotoPrint.API.csproj` and `PhotoPrint.Tests.csproj` both declare `Stripe.net 46.3.0`, but `dotnet list package --outdated` shows the Tests project's *resolved* version is `47.0.0` (transitive override or NuGet's restore heuristic). This is a silent two-versions-loaded scenario. Central Package Management (`Directory.Packages.props` at the solution root, `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`) catches this at restore time — and gives the user the single file they wanted to "keep track of all of them."

**Implementation steps**
1. Create `Directory.Packages.props` at solution root with a `<PackageVersion>` for every package across both csproj files.
2. Set `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` in `Directory.Build.props` (create if absent).
3. Strip `Version=` attributes from every `<PackageReference>` in both csproj files.
4. `dotnet restore` — if Stripe.net 47 transient override surfaces, CPM will fail the restore until pinned.
5. Pin Stripe.net to the version the Tests project actually needs (likely 47.0.0 — verify against test code) and bump the API to match.

**Schema / API changes**
```xml
<!-- Directory.Packages.props (new file at solution root) -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Stripe.net"                                        Version="47.0.0" />
    <PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol"      Version="1.15.3" />
    <!-- ... all other packages ... -->
  </ItemGroup>
</Project>
```

**Risks**
- Stripe.net 46 → 47 may have breaking API changes (event-type renames, deserialization tweaks). Run the full webhook integration test suite (`PaymentControllerIntegrationTests`).
- Some test packages may resist CPM (Moq sometimes complains about pre-release pinning).

---

### #3 — Add Renovate config with grouped quarterly upgrade PRs

| Field | Value |
|---|---|
| Category | `ops` |
| Complexity | 1 — single file, no code change |
| Business impact | 3 — durable mitigation of the dependency-sprawl pain |
| Priority score | 19 |
| Estimated effort | 0.5 dev-day |
| Affects | new `.github/renovate.json` |

**What and why**
Today every dependency upgrade is manual — the OpenTelemetry CVE wasn't picked up until this review. Renovate (free, well-supported, runs as a GitHub App) groups upgrades by ecosystem and minor-version cadence, opens one PR per group, and runs CI against it. Configure conservatively: monthly minor/patch updates, quarterly major roll-up.

**Implementation steps**
1. Create `.github/renovate.json` with three package groups: `dotnet-core` (EF + Npgsql + AspNetCore stay in lockstep), `observability` (OTel suite), `frontend-angular` (Angular packages move together).
2. Pin schedule to "before 6am on the first Monday of the month" so PRs land before the workweek.
3. Add `dependencyDashboard: true` so a single open issue shows all pending upgrades.
4. Install the Renovate GitHub App (one-time, repo admin action).

**Schema / API changes**
```json
{
  "$schema": "https://docs.renovatebot.com/renovate-schema.json",
  "extends": ["config:recommended", ":dependencyDashboard"],
  "schedule": ["before 6am on the first day of the month"],
  "packageRules": [
    { "matchPackagePatterns": ["^OpenTelemetry\\."], "groupName": "OpenTelemetry" },
    { "matchPackagePatterns": ["^Microsoft\\.EntityFrameworkCore", "^Npgsql"], "groupName": "EF Core + Npgsql" },
    { "matchPackagePatterns": ["^@angular/"], "groupName": "Angular" },
    { "matchUpdateTypes": ["major"], "schedule": ["before 6am on the first day of January, April, July, October"] }
  ],
  "vulnerabilityAlerts": { "labels": ["security"], "automerge": false }
}
```

**Risks**
- Renovate PRs without a maintainer triage become noise. Mitigate via `dependencyDashboard` + a quarterly review ritual (P19).

---

### #4 — `GET /api/admin/system-info` feature manifest endpoint (Concern 3)

| Field | Value |
|---|---|
| Category | `ops` |
| Complexity | 2 — one new admin endpoint + a registration extension |
| Business impact | 4 — directly addresses the "hidden functionality regression" pain point |
| Priority score | 20 |
| Estimated effort | 2 dev-days |
| Affects | new `Controllers/AdminSystemInfoController.cs`, new `Services/SystemInfo/`, populated by extensions |

**What and why**
The user's third concern is that ~11 background jobs, 7 feature flags, multiple CLI verbs, and dozens of "off-by-default" code paths are invisible — a regression in `Anaf:Enabled=false` boot won't be caught unless someone manually tests with the flag flipped. Today there is no single source of truth for "what is wired right now." The fix is a `/api/admin/system-info` endpoint that introspects DI + config and returns a JSON manifest the admin UI can render. Bonus: the integration test suite can assert "with `Anaf:Enabled=true`, the system-info reports `InvoiceUploadJob: Running`." A regression where someone removes `AddHostedService<InvoiceUploadJob>()` is caught at PR time.

**Implementation steps**
1. Create `Services/SystemInfo/ISystemInfoService.cs` with one method `Task<SystemManifest> GetAsync()` returning hosted services, feature flags, CLI verbs, webhook routes, and admin routes.
2. Implement by querying `IServiceProvider` for `IEnumerable<IHostedService>`, scraping `IConfiguration` for the known feature-flag keys, and using reflection on `IEndpointRouteBuilder` for the route list. Cache for 30 s.
3. Add `[Authorize(Roles = "Admin")]` controller `GET /api/admin/system-info`.
4. In the Admin Angular shell, render a System tab that consumes this — searchable, clickable.
5. Pair with P10: when `IFeatureGate` ships, the manifest becomes 100% derived from `IFeatureGate.GetAll()`.

**Schema / API changes**
```csharp
public sealed record SystemManifest(
    string Version,                                // commit SHA
    DateTimeOffset BuiltAt,
    IReadOnlyList<HostedServiceInfo> HostedServices,
    IReadOnlyList<FeatureFlagInfo> FeatureFlags,
    IReadOnlyList<RouteInfo> AdminRoutes,
    IReadOnlyList<RouteInfo> WebhookRoutes,
    IReadOnlyList<CliVerb> CliVerbs);

public sealed record HostedServiceInfo(string Name, string Status, string? GatedBy /* flag key */);
public sealed record FeatureFlagInfo(string Key, bool Enabled, string Description, string DefinedAt);
public sealed record RouteInfo(string Method, string Pattern, string AuthPolicy);
public sealed record CliVerb(string Verb, string Description, string Source /* "Program.cs:442" */);

// GET /api/admin/system-info -> 200 SystemManifest (Admin only)
```

**Risks**
- Reflection on the endpoint table can be slow; cache for 30s. Don't open new attack surface — the endpoint is `[Authorize(Roles="Admin")]`-gated and exposes no secrets.

---

### #5 — Register `ForwardedHeadersMiddleware` so `/metrics` allow-list works in production

| Field | Value |
|---|---|
| Category | `fix` |
| Complexity | 1 |
| Business impact | 3 — silent observability failure on day-1 of prod |
| Priority score | 19 |
| Estimated effort | 0.5 dev-day |
| Affects | `Program.cs`, `Middleware/MetricsEndpointIpAllowListMiddleware.cs` |

**What and why**
ADR-018 mandates the `/metrics` IP allow-list. The middleware reads `HttpContext.Connection.RemoteIpAddress`. Behind Caddy/Nginx (which is the deployment model per the Caddyfile at repo root), that field is the reverse-proxy's IP, NOT the scraper's. There's no `ForwardedHeadersMiddleware` registration in Program.cs. Result: in production, the allow-list will either accept everything (proxy IP is in the list) or reject everything (proxy IP isn't) — and a useful test is impossible without prod-like network.

**Implementation steps**
1. Add `app.UseForwardedHeaders()` early in the pipeline (before `UseCorrelationId`).
2. Configure `ForwardedHeadersOptions` to trust the reverse-proxy network range (`Caddyfile`'s upstream).
3. Update the integration test `MetricsEndpointIntegrationTests` with an `X-Forwarded-For` case.
4. Update DEPLOYMENT.md §14 with the proxy-trust note.

**Schema / API changes**
```csharp
// Program.cs — add before app.UseCorrelationId()
builder.Services.Configure<ForwardedHeadersOptions>(opts =>
{
    opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Trust the reverse proxy on the local docker bridge network.
    opts.KnownNetworks.Clear();
    opts.KnownProxies.Clear();
    opts.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("172.17.0.0"), 16)); // adjust per Caddyfile
});

// after var app = builder.Build();
app.UseForwardedHeaders();
```

**Risks**
- Misconfigured `KnownNetworks` enables IP spoofing. Anchor to the actual reverse-proxy CIDR.

---

### #6 — Refactor `Services/` into feature folders (Concern 1)

| Field | Value |
|---|---|
| Category | `refactor` |
| Complexity | 3 — large rename, namespace shuffle, no behaviour change |
| Business impact | 5 — directly addresses Concern 1 (scaling pains across API + UI + Tests) |
| Priority score | 21 |
| Estimated effort | 3-4 dev-days |
| Affects | All 49 files in `src/PhotoPrint.API/Services/*.cs` plus their callers and test files |

**What and why**
This is the biggest single intervention. `Services/` is flat — 49 files dropped at the top level (`AuthService.cs`, `OrderService.cs`, `CartService.cs`, `OrderPhotoPromoter.cs`, `S3StorageService.cs`, `StripePaymentGateway.cs`, etc.) — with sub-folders only for `Sameday/` and `Invoicing/` (the most recent bolts), an inconsistency that reveals the original folder strategy doesn't scale. Mirror the Angular `features/` structure and the recent `Sameday/`/`Invoicing/` precedent: every service goes into a feature folder. Three benefits: (a) cognitive load — `OrderService` is 4 clicks from `OrderItem`, `OrderStatus`, `OrderEmailService`, `OrderPaidEventDispatcher`; (b) namespace boundary — `PhotoPrint.API.Orders.Services` vs `PhotoPrint.API.Auth.Services` enables future module-boundary tooling; (c) tests mirror it (P-tests-folder companion below).

**Implementation steps**
1. Create feature folders: `Services/Auth/`, `Services/Account/`, `Services/Catalog/`, `Services/Cart/`, `Services/Orders/`, `Services/Payments/`, `Services/Uploads/`, `Services/Storage/`, `Services/Email/`, `Services/Shipping/` (already has `Sameday/`), `Services/Invoicing/` (already exists), `Services/Admin/`.
2. Move each service + its interface into the matching folder. Update namespace from `PhotoPrint.API.Services` → `PhotoPrint.API.Services.Orders` etc.
3. **Critical**: do this in one mechanical PR per feature folder — small batches, so git history is bisectable.
4. Update Test folder in lockstep: `Tests/Unit/Orders/`, `Tests/Unit/Auth/`, etc.
5. The DI registrations in `Program.cs` need a namespace update only.
6. Validate via `dotnet build` after each batch.

**Schema / API changes** — namespaces only. No public-surface change.
```
src/PhotoPrint.API/Services/
├── Auth/              IAuthService, AuthService, ISocialAuthService, SocialAuthService, IGoogleTokenValidator, GoogleTokenValidator, ITokenService, TokenService, IEmailTokenService, EmailTokenService, IGuestSessionService, GuestSessionService
├── Account/           IAccountService, AccountService
├── Catalog/           IProductService, ProductService, IAdminProductService, AdminProductService, PricingService
├── Cart/              ICartService, CartService
├── Orders/            IOrderService, OrderService, OrderStatusMachine, IOrderNumberService, OrderNumberService, IOrderEmailService, OrderEmailService, IAdminOrderService, AdminOrderService, IAdminStatsService, AdminStatsService, IOrderPhotoPromoter, OrderPhotoPromoter, IPromotionQueue, PromotionQueue, PromotionJob, PromotionOutcome, IOriginalPurger, OriginalPurger, PurgeOutcome
├── Payments/          IStripePaymentGateway, StripePaymentGateway, IStripeSignatureVerifier, StripeSignatureVerifier, IEuPlatescService, EuPlatescService
├── Uploads/           IUploadService, UploadService, IImageProcessor, ImageProcessor, IMimeValidator, MimeValidator
├── Storage/           IStorageService, IStorageRouter, StorageRouter, LocalStorageService, S3StorageService, S3BucketVerifier, StorageKeys
├── Email/             IEmailService, IEmailSender, ReliableEmailService, SmtpEmailService, SendGridEmailService, IRazorTemplateService, RazorTemplateService
├── Shipping/          IShippingService, StaticShippingService, SamedayShippingService, Sameday/
├── Invoicing/         (already organised)
├── Admin/             (cross-cutting admin orchestrators that don't belong elsewhere)
└── VAT/               VatCalculator
```

**Risks**
- Merge conflicts with any in-flight bolt. Plan the refactor as the FIRST merged thing after the current branch ships.
- Test refactor must happen in same PR sequence to keep CI green.

---

### #7 — Extract `Program.cs` subsystem composition into 5 extension methods

| Field | Value |
|---|---|
| Category | `refactor` |
| Complexity | 2 |
| Business impact | 3 — pure code-health, but directly addresses "scaling pains" by making boot reviewable |
| Priority score | 17 |
| Estimated effort | 1.5 dev-days |
| Affects | `Program.cs`, new `Extensions/SamedayExtensions.cs`, `AnafExtensions.cs`, `InvoicingExtensions.cs`, `PaymentsExtensions.cs`, `SentryExtensions.cs` |

**What and why**
`Program.cs` is 534 LOC. The existing pattern (`AddSocialAuth`, `AddGuestSessions`, `AddEmailInfrastructure`, `AddPhotoStorage`, `AddPhotoArchive`, `AddSecurityBaselines`, `AddObservability`) is already established — but the Sameday block (lines 162–229), the ANAF block (lines 297–361), the Invoicing block (lines 264–331), the Payments block (lines 237–262), and the Sentry block (lines 29–61) are all inline. Each is exactly the kind of conditional DI tree that begs to be an extension. Result: Program.cs becomes ~120 LOC of "wire these subsystems," and each subsystem's DI graph is unit-testable in isolation.

**Implementation steps**
1. For each subsystem, create `Extensions/<Subsystem>Extensions.cs` with an `AddX` method that takes `IServiceCollection` and `IConfiguration`.
2. The conditional `if (xEnabled)` stays inside the extension — the extension knows about the flag.
3. Move `QuestPDF.Settings.License = ...` into `AddInvoicing` so the license declaration is co-located with the only consumer.
4. Update `Program.cs` to call the new extensions in order.
5. Add a unit test per extension: "with Enabled=false the extension registers nothing background-y" — this is exactly the test that catches the regression of accidentally always-registering `InvoiceUploadJob`.

**Schema / API changes**
```csharp
// Extensions/SamedayExtensions.cs
public static IServiceCollection AddSameday(this IServiceCollection services, IConfiguration cfg)
{
    services.Configure<SamedaySettings>(cfg.GetSection(SamedaySettings.SectionName));
    services.AddSingleton<IValidateOptions<SamedaySettings>, SamedaySettingsValidator>();
    services.AddOptions<SamedaySettings>().ValidateOnStart();

    services.AddSingleton<IAwbCreationNotifier, NullAwbCreationNotifier>(); // default

    var enabled = cfg.GetSection(SamedaySettings.SectionName).GetValue<bool>("Enabled");
    if (!enabled) return services;

    // ... move lines 184–228 of Program.cs here ...
    return services;
}

// Program.cs becomes:
builder.Services
    .AddSameday(builder.Configuration)
    .AddAnaf(builder.Configuration)
    .AddInvoicing(builder.Configuration)
    .AddPayments(builder.Configuration, builder.Environment)
    .AddSentry(builder.Configuration, builder.WebHost);
```

**Risks**
- Order matters — `AddInvoicing` must run before `AddAnaf` (Anaf depends on Invoicing services). The fluent chain enforces order, but a test that boots the host catches regressions.

---

### #8 — Add global rate limit + per-endpoint admin role policy constant

| Field | Value |
|---|---|
| Category | `security` |
| Complexity | 2 |
| Business impact | 3 |
| Priority score | 17 |
| Estimated effort | 1 dev-day |
| Affects | `Program.cs`, `Extensions/SecurityExtensions.cs`, all `[Authorize(Roles="Admin")]` controllers |

**What and why**
Two distinct hardening moves. (a) Today only auth endpoints are rate-limited; an unauthenticated visitor can hit `/api/products` 1000×/sec. Add a global fallback rate limit (e.g. 200 req/min/IP for `/api/*`) that auth-endpoint-specific policies override. (b) `[Authorize(Roles = "Admin")]` appears as a string literal in 6 controllers; one typo (`"Admin "` with trailing space, or `"admin"` lowercase) and the endpoint is open. Introduce a `Policies.Admin` constant and a corresponding policy registered via `AddAuthorization`.

**Implementation steps**
1. In `SecurityExtensions.cs` add a `GlobalRateLimitPolicy` with `PartitionedRateLimiter.Create<HttpContext, string>` keyed on `X-Forwarded-For` (after P5 lands) → 200 req/min sliding window.
2. Add a `public static class Policies { public const string Admin = "AdminRole"; }`.
3. Register `options.AddPolicy(Policies.Admin, p => p.RequireRole("Admin"))`.
4. Find/replace `[Authorize(Roles = "Admin")]` → `[Authorize(Policy = Policies.Admin)]` across 6 controllers.
5. Add an integration test asserting that an anonymous request to `/api/admin/*` returns 401, not 403.

**Schema / API changes**
```csharp
public static class Policies
{
    public const string Admin       = "AdminRole";
    public const string DualAuth    = "GuestOrUser"; // existing, just centralise
}

services.AddAuthorization(opts =>
{
    opts.AddPolicy(Policies.Admin, p => p.RequireRole("Admin"));
    // existing DualAuth policy moves here
});

services.AddRateLimiter(opts =>
{
    opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetSlidingWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 200, Window = TimeSpan.FromMinutes(1), SegmentsPerWindow = 6
            }));
});
```

**Risks**
- Global rate limit may surprise legitimate burst users (admin uploading 30 photos in 10 seconds). Tune limit during pre-launch load test.

---

### #9 — Refund / return endpoint (EU consumer law)

| Field | Value |
|---|---|
| Category | `feature` |
| Complexity | 4 — Stripe refund API, EuPlatesc refund flow, accounting cascade |
| Business impact | 5 — legal requirement, customer-facing |
| Priority score | 19 |
| Estimated effort | 7-10 dev-days |
| Affects | new `Controllers/AdminRefundsController.cs`, new `Services/Refunds/`, `Order` model gets `RefundedAt`/`RefundAmountRon`/`RefundReason`; new `Invoice` credit-note row |

**What and why**
EU Directive 2011/83/EU gives consumers a 14-day right of withdrawal. There is no refund endpoint, no `Order.RefundedAt`, no `RefundReason`, no credit-note invoice in the `Invoices` table (a refund requires a credit-note per Romanian fiscal law — accountancy will flag this). Currently the only path is admin manual via Stripe Dashboard, which leaves the FotoTipar DB out of sync — orders show `Status=Delivered` while Stripe shows refunded, and ANAF never sees the credit-note. This is a launch blocker for compliance.

**Implementation steps**
1. Add migration: `Order` gets `RefundedAt`, `RefundAmountRon`, `RefundReason`. Add `OrderStatus.Refunded` (terminal). Update `OrderStatusMachine`.
2. `Invoice` already has `InvoiceType` (proforma vs final) implicitly via the bolt 038 model — verify; add an explicit `InvoiceType` enum with `Final` and `CreditNote`. The credit-note row references the original via `OriginalInvoiceId` FK and has negative amounts.
3. Add `Services/Refunds/IRefundService.cs` with `Task<RefundResult> RefundOrderAsync(Guid orderId, decimal? amount, string reason, CancellationToken ct)`. Full + partial refunds.
4. Stripe path: call `stripe refund create` against the PaymentIntent. EuPlatesc: documented refund endpoint (or manual Z-report in the admin UI for now — flag).
5. Bolt-039 ANAF path: credit-note is a separate UBL invoice with `cbc:InvoiceTypeCode` 381; the existing `InvoiceUploadJob` picks it up because it filters on `Pending`+`Submitted` regardless of type.
6. Admin endpoint: `POST /api/admin/orders/{id}/refund` with `{amount?, reason}`. Customer-facing: no endpoint; refund is admin-initiated.

**Schema / API changes**
```sql
ALTER TABLE "Orders"
  ADD COLUMN "RefundedAt"      timestamptz NULL,
  ADD COLUMN "RefundAmountRon" numeric(10,2) NULL,
  ADD COLUMN "RefundReason"    text NULL;

-- OrderStatus enum: add 'Refunded' (terminal state)

ALTER TABLE "Invoices"
  ADD COLUMN "InvoiceType"        text NOT NULL DEFAULT 'Final',  -- 'Final' | 'CreditNote'
  ADD COLUMN "OriginalInvoiceId"  uuid NULL REFERENCES "Invoices"("Id");

CREATE INDEX "ix_invoices_original" ON "Invoices"("OriginalInvoiceId") WHERE "OriginalInvoiceId" IS NOT NULL;
```

**Risks**
- Refunds intersect with the photo-archive retention (bolt 052). Refunded orders should NOT auto-purge originals at the same Shipped trigger — flag.
- Partial refunds raise an accounting edge case (which line items get the credit-note?). Pick the simplest customer-side: refund proportionally across all line items, document.

---

### #10 — Centralise feature-flag layer via `IFeatureGate`

| Field | Value |
|---|---|
| Category | `refactor` |
| Complexity | 3 — new abstraction, retro-fit every flag |
| Business impact | 4 — operates as the foundation for P4 (system-info) and the regression discoverability concern |
| Priority score | 18 |
| Estimated effort | 2 dev-days |
| Affects | new `Services/FeatureFlags/IFeatureGate.cs`, `Program.cs`, every site that reads `GetValue<bool>("Enabled")` |

**What and why**
Seven feature flags exist today — `Sameday:Enabled`, `Sameday:Jobs:Enabled`, `Sentry:Enabled`, `Observability:Enabled`, `Anaf:Enabled`, `Invoicing:CustomerEmailAttachments:Enabled`, `OrderPhotoArchive:Enabled`, plus `Archive:Enabled`, `Storage:Provider` (tier-style). Each is read via `Configuration.GetSection(...).GetValue<bool>("Enabled")` — string-typed, no compile-time check, no centralised registry, no test seam. A typo (`"Enabeld"`) is silent. Solution: a typed `IFeatureGate` with a flag enum and a registry, populated from config at boot. The registry doubles as the data source for the P4 system-info endpoint.

**Implementation steps**
1. Define `enum FeatureFlag { Sameday, SamedayJobs, Sentry, Observability, Anaf, InvoiceEmailAttachments, PhotoArchive, Archive }`.
2. `IFeatureGate.IsEnabled(FeatureFlag flag)` + `IReadOnlyDictionary<FeatureFlag, FeatureFlagInfo> GetAll()`.
3. Implementation `ConfigFeatureGate` binds at boot from `IConfiguration` and caches.
4. Refactor each call site — Program.cs reads `gate.IsEnabled(FeatureFlag.Sameday)`. The flag-mapping (enum → config key + default + description) is one static table.
5. Wire `IFeatureGate.GetAll()` into the system-info endpoint (P4).
6. Unit-test the gate against missing/malformed config.

**Schema / API changes**
```csharp
public enum FeatureFlag
{
    Sameday, SamedayJobs, Sentry, Observability, Anaf,
    InvoiceEmailAttachments, PhotoArchive, OldOriginalArchive
}

public sealed record FeatureFlagInfo(
    FeatureFlag Flag, string ConfigKey, bool Enabled, bool Default, string Description);

public interface IFeatureGate
{
    bool IsEnabled(FeatureFlag flag);
    IReadOnlyDictionary<FeatureFlag, FeatureFlagInfo> GetAll();
}
```

**Risks**
- Don't try to make it dynamic-reloadable in this round; config is read at boot. Document this. (A `Microsoft.FeatureManagement` upgrade is out of scope for the bolt 046-deprioritized phase.)

---

### #11 — Extract `OrderPaidEventDispatcher` to dedupe webhook side-effect fan-out

| Field | Value |
|---|---|
| Category | `refactor` |
| Complexity | 2 |
| Business impact | 3 |
| Priority score | 17 |
| Estimated effort | 1 dev-day |
| Affects | `Controllers/WebhooksController.cs`, new `Services/Orders/OrderPaidEventDispatcher.cs` |

**What and why**
`WebhooksController` has the same 5-line block duplicated for Stripe (lines 271–282) and EuPlatesc (lines 205–216) once an order transitions to `Paid`: create invoice, save, record metric, broadcast SignalR, fire confirmation email, enqueue photo promotion, notify AWB. Duplication = "two places must change in lockstep." Extract a dispatcher that the controller calls once after transitioning the order; the controller becomes routing + signature verification only.

**Implementation steps**
1. Create `OrderPaidEventDispatcher` in `Services/Orders/`.
2. Move the post-Paid side-effect fan-out (lines 205–216 and 271–282) into a single `DispatchAsync(Order order, CancellationToken ct)` method.
3. Both webhook handlers become: verify signature → transition order → `await _dispatcher.DispatchAsync(order, ct)`.
4. Unit-test the dispatcher with mocked dependencies — assert all 5 side effects fire in the right order.

**Schema / API changes** — none, internal refactor.

**Risks**
- Order of side effects matters (invoice INSERT before SignalR broadcast — ADR-020). Document the order as a load-bearing contract in the dispatcher's XML doc.

---

### #12 — Multi-replica readiness doc consolidating ADRs 010/013/015/016/023

| Field | Value |
|---|---|
| Category | `ops` |
| Complexity | 1 |
| Business impact | 2 |
| Priority score | 14 |
| Estimated effort | 0.5 dev-day |
| Affects | new `docs/architecture/multi-replica-readiness.md` |

**What and why**
Today the "what stays in-process / what needs Redis" reasoning lives in 5 different ADRs (010 promotion `Channel<T>`, 013 Sameday token cache, 015 accept-duplicate AWB, 016 CAS via ExecuteUpdate, 023 ANAF polling not channel). Anyone planning multi-replica scale must read all five. Consolidate into one architecture doc.

**Implementation steps**
1. Write `docs/architecture/multi-replica-readiness.md` with one section per concern (promotion queue, token caches, AWB dedupe, status CAS, ANAF dispatch). Each section cites the ADR and states "today: X / future bolt 046: Y."
2. Link from `memory-bank/standards/system-architecture.md`.

**Schema / API changes** — none.

**Risks** — none.

---

### #13 — Decompose `AuthService` (424 LOC)

| Field | Value |
|---|---|
| Category | `refactor` |
| Complexity | 3 |
| Business impact | 3 |
| Priority score | 15 |
| Estimated effort | 2 dev-days |
| Affects | `Services/AuthService.cs`, callers in `AuthController`, test file `Unit/Services/AuthServiceTests.cs` |

**What and why**
424 LOC, 6 concerns: register, login, refresh, confirm-email, forgot-password, reset-password. Split into three services with clean boundaries.

**Implementation steps**
1. Create `IAccountRegistrationService` (Register, ConfirmEmail, ResendConfirmation).
2. Create `IPasswordResetService` (Forgot, Reset).
3. `IAuthService` retains Login, Refresh, RevokeRefreshToken.
4. Each service has its own test file.

**Schema / API changes** — none, internal refactor.

**Risks** — touching auth requires the full integration test pass. Plan in a quiet PR.

---

### #14 — Decompose `WebhooksController` + `OrderService` god-methods

| Field | Value |
|---|---|
| Category | `refactor` |
| Complexity | 3 |
| Business impact | 3 |
| Priority score | 15 |
| Estimated effort | 2 dev-days |
| Affects | `Controllers/WebhooksController.cs` (345 LOC), `Services/OrderService.cs` (381 LOC) |

**What and why**
Pairs with P11. `WebhooksController` becomes thin (sig verification + routing). `OrderService.CreateFromCartAsync` (145 LOC, 8 concerns) becomes a `CartCheckoutHandler` (handler pattern). `GetOrderPhotosAsync` moves out to `OrderPhotoQueryService` per the code-health finding.

**Implementation steps**
1. Create `Services/Orders/Checkout/CartCheckoutHandler.cs` with a single `HandleAsync(CheckoutCommand cmd, CancellationToken ct)` method. Move cart-load, idempotency, order-build, VAT, metrics fan-out into it.
2. Move `GetOrderPhotosAsync` to `Services/Orders/OrderPhotoQueryService.cs`.
3. Both `IOrderService.CreateFromCartAsync` and `IOrderService.GetOrderPhotosAsync` delegate one-liners to the new classes.
4. Test files mirror the split.

**Schema / API changes** — none.

**Risks** — order-creation is the highest-traffic write path. Plan for the full payment-integration test suite to run.

---

### #15 — Per-entity `IEntityTypeConfiguration<T>` files; shrink DbContext to <100 LOC

| Field | Value |
|---|---|
| Category | `refactor` |
| Complexity | 2 |
| Business impact | 2 |
| Priority score | 14 |
| Estimated effort | 1.5 dev-days |
| Affects | `Data/PhotoPrintDbContext.cs` (437 LOC), new files under `Data/Configurations/<Entity>Configuration.cs` |

**What and why**
The `Data/Configurations/` folder exists but only has one file (likely a recent test). The DbContext's `OnModelCreating` is 400 lines of inline lambdas — hard to diff, hard to spot a missing index. Move each entity's config into its own file implementing `IEntityTypeConfiguration<T>` and call `modelBuilder.ApplyConfigurationsFromAssembly(typeof(PhotoPrintDbContext).Assembly)` once.

**Implementation steps**
1. For each of the 17 entities, create `Data/Configurations/<Entity>Configuration.cs`.
2. Move the inline `modelBuilder.Entity<X>(e => { ... })` block into `Configure(EntityTypeBuilder<X>)`.
3. Replace the whole `OnModelCreating` body with `modelBuilder.ApplyConfigurationsFromAssembly(...)` plus the SQLite-DateTimeOffset value-converter loop (which stays — it's cross-cutting).
4. EF Core migration generation should produce zero diff after this — if it doesn't, a configuration was mis-translated.

**Schema / API changes** — none, pure refactor; verify migration `Add-Migration NoOpRefactorVerify` produces empty up/down.

**Risks**
- Easy to drop a `HasIndex` line. Run `Add-Migration` after and visually inspect that no schema change is produced.

---

### #16 — Domain layer extraction (no new project)

| Field | Value |
|---|---|
| Category | `refactor` |
| Complexity | 3 |
| Business impact | 2 |
| Priority score | 12 |
| Estimated effort | 1.5 dev-days |
| Affects | `OrderStatusMachine`, `VatCalculator`, `StorageKeys`, `InvoiceNumber`, `PromotionOutcome`, `PurgeOutcome` |

**What and why**
Pure-functional helpers (no DbContext, no HttpClient) drift between `Services/` and inline statics. Create a `Domain/` namespace and move them. Boundary: "no infrastructure dependencies in `Domain/`." No new csproj — the architecture standards doc explicitly endorses the monolith, so introducing a `Domain.csproj` would over-shoot. Folder + namespace is enough.

**Implementation steps**
1. Create `Domain/Orders/`, `Domain/Invoicing/`, `Domain/Uploads/`, `Domain/Storage/`.
2. Move the listed types. Add a Roslyn analyzer rule (or just a CONTRIBUTING.md note) that `Domain/` may not reference `Microsoft.EntityFrameworkCore` or `System.Net.Http`.
3. Update namespaces.

**Schema / API changes** — namespaces only.

**Risks** — none material.

---

### #17 — Background-job liveness health check + ANAF invoice metrics

| Field | Value |
|---|---|
| Category | `ops` |
| Complexity | 2 |
| Business impact | 3 |
| Priority score | 17 |
| Estimated effort | 1.5 dev-days |
| Affects | new `HealthChecks/BackgroundJobLivenessCheck.cs`, `Observability/FotoMetrics.cs`, `InvoiceUploadJob.cs`, `slos.md` |

**What and why**
Two distinct observability gaps. (a) A `BackgroundService` that throws inside its loop is caught by the framework and the job stops silently. Today only `DbHealthCheck` and `DiskHealthCheck` exist; an `InvoiceUploadJob` death goes unnoticed for hours. Add a `BackgroundJobLivenessCheck` that tracks a heartbeat per known hosted service (timestamp updated each tick) and returns Degraded if any heartbeat is older than 3× its scheduled interval. (b) Add `invoice_upload_total{result}` and `invoice_upload_lag_seconds` metrics, mirroring the existing `payment_webhook_total` pattern. SLO entry in `slos.md` for the 5-business-day ANAF SLA from ADR-024.

**Implementation steps**
1. Create `Observability/IHeartbeat.cs` — `void Beat(string jobName)`.
2. Inject into each `BackgroundService`; call `_heartbeat.Beat(nameof(InvoiceUploadJob))` per tick.
3. `BackgroundJobLivenessCheck` consults the heartbeat registry; reports Degraded for stale heartbeats.
4. Add `FotoMetrics.InvoiceUpload = meter.CreateCounter<long>("invoice_upload_total", description: "...")` + `InvoiceUploadLagSeconds = meter.CreateHistogram<double>(...)`.
5. Stamp metrics at the end of `InvoiceUploadJob.ProcessOneAsync` with `result: accepted | rejected | failed | retried`.

**Schema / API changes**
```csharp
public interface IHeartbeat
{
    void Beat(string jobName);
    IReadOnlyDictionary<string, DateTimeOffset> Snapshot();
}

// FotoMetrics additions
public static readonly Counter<long>  InvoiceUpload      = ...;
public static readonly Histogram<double> InvoiceUploadLagSeconds = ...;
```

**Risks** — none material.

---

### #18 — Bundle-size CI budget + 3 e2e smoke tests

| Field | Value |
|---|---|
| Category | `ops` |
| Complexity | 2 |
| Business impact | 3 |
| Priority score | 17 |
| Estimated effort | 2 dev-days |
| Affects | `angular.json`, `package.json`, new `e2e/` folder, CI workflow |

**What and why**
No bundle budget. No e2e. Both are launch blockers for a payment-processing site. Bundle budget is one config line. E2e is Playwright + 3 scenarios (guest checkout, admin login, SignalR real-time broadcast).

**Implementation steps**
1. In `angular.json` set `budgets: [{ type: 'initial', maximumWarning: '500kB', maximumError: '750kB' }, { type: 'anyComponentStyle', maximumError: '4kB' }]`.
2. Add `@playwright/test` as a dev dep.
3. Write three e2e: `guest-checkout.spec.ts`, `admin-login.spec.ts`, `realtime-order.spec.ts`.
4. GH Actions workflow: `playwright-e2e.yml` that boots the API + UI (docker-compose) and runs e2e.

**Schema / API changes** — none.

**Risks** — Playwright requires browsers in CI (~200MB cache). Use the official action; budget for ~3 min per e2e run.

---

### #19 — Refresh `tech-stack.md`, add `KNOWN_FAILURES.md`, ADR-quarterly-audit ritual

| Field | Value |
|---|---|
| Category | `ops` |
| Complexity | 1 |
| Business impact | 2 |
| Priority score | 14 |
| Estimated effort | 0.5 dev-day |
| Affects | `memory-bank/standards/tech-stack.md`, new `docs/KNOWN_FAILURES.md`, new `docs/ARCHITECTURE_AUDIT_CHECKLIST.md` |

**What and why**
`tech-stack.md` says Angular 17+ / Jasmine — reality is Angular 21 / Vitest. `heic2any` and `ng2-charts` are listed but uninstalled. `MailKit (dev) / SendGrid (prod)` is wrong — provider is config-driven, both code paths exist. Standards docs lose trust when they lie. Refresh, add a `KNOWN_FAILURES.md` for the 7 failing tests, and a quarterly audit checklist anchored in this review.

**Implementation steps**
1. Rewrite `tech-stack.md` against current `package.json` and `.csproj`.
2. Enumerate the 7 failing tests in `docs/KNOWN_FAILURES.md` — each with a reason and a tracking issue.
3. Add `docs/ARCHITECTURE_AUDIT_CHECKLIST.md` — a 1-page quarterly review checklist (vulnerabilities, outdated, LOC growth, ADR additions, doc rot).

**Schema / API changes** — none.

**Risks** — none.

---

### #20 — Discount / coupon engine

| Field | Value |
|---|---|
| Category | `feature` |
| Complexity | 4 |
| Business impact | 4 |
| Priority score | 16 |
| Estimated effort | 5-8 dev-days |
| Affects | new `Coupons` table + redemption tracking, `OrderService.CreateFromCartAsync` integrates, Stripe metadata, admin UI |

**What and why**
Standard e-commerce capability — entirely absent. Marketing levers (welcome10, BLACKFRIDAY, referral) need a coupon engine. Affects pricing (CartService), checkout (OrderService), and ANAF invoicing (the credit applies before VAT extraction, which means `VatCalculator` consumes a post-discount total).

**Implementation steps**
1. `Coupons` table: `Id`, `Code` (unique, case-insensitive), `Type` (`PercentageOff` | `FixedRon` | `FreeShipping`), `Value`, `MinOrderRon`, `ValidFrom`, `ValidUntil`, `MaxRedemptions`, `RedemptionCount`, `IsActive`.
2. `CouponRedemptions` table: `Id`, `CouponId`, `OrderId`, `UserId`, `GuestSessionId`, `RedeemedAt` (uniqueness on `(CouponId, UserId)` for one-per-user).
3. `Order` gets `CouponId`, `DiscountRon`. VAT calculation uses `Subtotal - DiscountRon`.
4. Endpoint `POST /api/cart/coupon { code }` — validates + attaches; idempotent.
5. Admin endpoints to CRUD coupons.
6. ANAF/invoice: discount is a UBL `AllowanceCharge` line (cbc:ChargeIndicator=false).

**Schema / API changes**
```sql
CREATE TABLE "Coupons" (
    "Id"               uuid PRIMARY KEY,
    "Code"             text NOT NULL,
    "Type"             text NOT NULL,
    "Value"            numeric(10,2) NOT NULL,
    "MinOrderRon"      numeric(10,2) NULL,
    "ValidFrom"        timestamptz NOT NULL,
    "ValidUntil"       timestamptz NULL,
    "MaxRedemptions"   int NULL,
    "RedemptionCount"  int NOT NULL DEFAULT 0,
    "IsActive"         bool NOT NULL DEFAULT true
);
CREATE UNIQUE INDEX "ix_coupons_code" ON "Coupons" (UPPER("Code"));

ALTER TABLE "Orders" ADD COLUMN "CouponId" uuid NULL REFERENCES "Coupons"("Id");
ALTER TABLE "Orders" ADD COLUMN "DiscountRon" numeric(10,2) NOT NULL DEFAULT 0;
```

**Risks**
- Cross-cuts the VAT calculator (which is bolt 038's gold-standard implementation). Must engage with ADR-019 (rounding) — discount before VAT extraction, not after.
- One-per-user enforcement opens a guest-bypass surface (new email = new coupon). Document and accept; rate-limit redemption attempts per IP.

---

### #21 — Codify Presentation / Application / Domain / Infrastructure folder layering inside `PhotoPrint.API` (NO new csproj)

| Field | Value |
|---|---|
| Category | `refactor` |
| Complexity | 3 — large folder reshuffle, no behaviour change |
| Business impact | 5 — directly addresses the maintainer's core complaint about layer separation |
| Priority score | 21 |
| Estimated effort | 4–5 dev-days (lands after P06 — they're the same physical refactor coordinated) |
| Affects | every folder under `src/PhotoPrint.API/`; namespace changes touch ~200 files |

**What and why**

The first-pass review proposed P06 (feature folders inside `Services/`) — necessary but insufficient. The maintainer's deeper complaint is that there is no clear **Presentation / Application / Domain / Infrastructure** layering anywhere — controllers, services, data, models, validators, middleware, background jobs, and configuration all sit at the top level of one project. Two coherent paths exist; this proposal picks one. The other is P22.

**Recommended target tree** (folder + namespace inside the single `PhotoPrint.API` csproj — no new projects):

```
src/PhotoPrint.API/
├── Web/                          # PRESENTATION
│   ├── Controllers/              # ← from /Controllers
│   ├── Hubs/                     # ← from /Hubs
│   ├── Middleware/               # ← from /Middleware
│   ├── Filters/                  # ← from /Filters
│   ├── Authentication/           # ← from /Authentication
│   └── Validators/               # ← from /Validators (request-shape validators only)
│
├── Application/                  # APPLICATION (use cases + DTOs)
│   ├── Orders/
│   │   ├── Abstractions/         # IOrderService, ICreateOrderHandler ...
│   │   ├── Handlers/             # CreateOrderHandler, RetryAwbHandler ...
│   │   ├── Services/             # OrderService (thin coordinator)
│   │   └── Dtos/                 # ← from /DTOs/Orders
│   ├── Auth/ Cart/ Invoicing/ Sameday/ Storage/ Uploads/ Catalog/ Account/ Payments/
│   └── Shared/                   # cross-feature DTOs (paging, problem-details extras)
│
├── Domain/                       # DOMAIN (pure functions, no infra deps)
│   ├── Orders/                   # OrderStatusMachine, OrderNumber, PromotionOutcome, PurgeOutcome
│   ├── Invoicing/                # InvoiceNumber, VatCalculator (move out of Services/)
│   ├── Storage/                  # StorageKeys
│   ├── Uploads/                  # MimeValidator (pure byte-pattern logic)
│   └── Models/                   # the 24 POCO entities currently in /Models
│
├── Infrastructure/               # INFRASTRUCTURE (EF Core, HttpClient, third-party SDKs)
│   ├── Data/                     # PhotoPrintDbContext + Configurations/
│   ├── Email/                    # SmtpEmailService, SendGridEmailService, MailKit wrappers
│   ├── Storage/                  # LocalStorageService, S3StorageService, S3BucketVerifier
│   ├── Payments/                 # StripePaymentGateway, StripeSignatureVerifier, EuPlatescService
│   ├── Sameday/                  # SamedayClient, SamedayAuthHandler, AnafSpvClient, AnafAuthHandler
│   ├── Pdf/                      # InvoicePdfRenderer, InvoicePdfDocument
│   ├── Templates/                # RazorTemplateService + /EmailTemplates
│   ├── BackgroundJobs/           # ← from /BackgroundJobs
│   └── Observability/            # ← from /Observability
│
├── Configuration/                # OPTIONS (kept flat — they're just option classes)
│   └── Validators/               # the 7 *SettingsValidator.cs (move out of /Validators)
│
├── Cli/                          # ← unchanged
├── Extensions/                   # composition root helpers
└── Program.cs
```

**Layering rules to codify** (one CONTRIBUTING.md page + one Roslyn-analyzer rule each, or just a code-review checklist if analyzers feel like overkill):

| From | May reference | May NOT reference |
|---|---|---|
| `Web/` | `Application/`, `Domain/`, `Configuration/` | `Infrastructure/`, `PhotoPrintDbContext`, EF Core |
| `Application/` | `Domain/`, `Configuration/`, `Application/.../Abstractions/` (other features' interfaces) | `Web/`, `Infrastructure/` (except via interface DI) |
| `Domain/` | nothing in the project | EVERYTHING — pure C# only |
| `Infrastructure/` | `Domain/`, `Configuration/`, third-party SDKs | `Web/`, `Application/.../Services/` (uses interfaces only) |

**Why this and not the four-project split (P22):**

1. The project is pre-deployment, single-tenant, single-team, single-deployable. The four-project split has costs (build complexity, separate package dependency graphs, harder migration generation, contributor onboarding overhead) and zero corresponding benefit at this scale.
2. The same logical separation is achievable with folders + namespaces + a Roslyn analyzer. .NET 8's `BannedSymbolsAnalyzer` + `NoPesticide` patterns make folder-based layer rules enforceable.
3. If the project ever does need to extract a microservice (say, the bolt 052 photo-archive pipeline becomes its own service), the existing namespace boundary is already the cut line — extraction becomes "move `Infrastructure/Storage/` + `Application/Uploads/` + relevant `Domain/Uploads/` types to a new csproj." The boundary work is done.

**Implementation steps**

1. **Sequence after P06** — P06 introduces feature folders inside the existing `Services/`. P21 then PROMOTES those feature folders one level up under `Application/`. Doing P06 first means P21's mechanical move is `Services/Orders/` → `Application/Orders/`, not "untangle the flat folder AND reshape it."
2. PR-1: introduce `Domain/` folder + namespace; move the 6 pure-functional types (`OrderStatusMachine`, `VatCalculator`, `StorageKeys`, `InvoiceNumber`, `PromotionOutcome`, `PurgeOutcome`) into it. **This is P16 from the first pass — folded in.**
3. PR-2: introduce `Infrastructure/` folder; move `Data/`, `BackgroundJobs/`, `Observability/`, `EmailTemplates/`, and the **implementation-only** halves of feature folders (`S3StorageService.cs` moves to `Infrastructure/Storage/`, leaving `IStorageService.cs` in `Application/Storage/Abstractions/`).
4. PR-3: introduce `Web/` folder; move `Controllers/`, `Hubs/`, `Middleware/`, `Filters/`, `Authentication/`, and the request-shape validators from `Validators/`.
5. PR-4: introduce `Application/` folder; promote the existing `Services/<Feature>/` to `Application/<Feature>/Services/` and add `Application/<Feature>/Abstractions/` for the interfaces.
6. PR-5: move the 7 `*SettingsValidator.cs` files from `Validators/` to `Configuration/Validators/` (or just delete the flat `Validators/` folder once all its content is sorted into Web vs Configuration).
7. After each PR: `dotnet build && dotnet test` must be green. **No behaviour change** — pure folder + namespace.

**Schema / API changes** — none. Pure refactor. EF Core migration drift = zero.

**Risks**

- **Merge-conflict risk is the dominant cost.** Plan for ~2 weeks of frozen feature work, OR coordinate with current bolts so P21 lands in a quiet window.
- One Roslyn rule (`Domain/` may not reference EF Core) needs a NuGet `BannedApiAnalyzer` config. If we skip the analyzer, the rule lives in CONTRIBUTING.md and code review.
- Some test files use `using static PhotoPrint.API.Services.OrderStatusMachine` — after this change, that becomes `using static PhotoPrint.API.Domain.Orders.OrderStatusMachine`. Mechanical find-replace.
- The `Sameday/` background jobs currently live in `BackgroundJobs/` (flat), not `Services/Sameday/`. They should move to `Infrastructure/Sameday/` during this refactor — not stay in a separate flat folder.

---

### #22 — Evaluate (and explicitly REJECT) the four-project clean-arch split

| Field | Value |
|---|---|
| Category | `refactor` |
| Complexity | 1 — single ADR write-up |
| Business impact | 2 — protects against future "let's just split it" attempts |
| Priority score | 14 |
| Estimated effort | 0.5 dev-day |
| Affects | new `bolts/architect-review/adr-NN-no-clean-arch-split.md`, link from `memory-bank/standards/system-architecture.md` |

**What and why**

The architectural-elegance temptation is to split into `PhotoPrint.Domain`, `PhotoPrint.Application`, `PhotoPrint.Infrastructure`, `PhotoPrint.Web`. The decision NOT to do that is currently an implicit one (`system-architecture.md` line 8 says "Monolithic"). For a project with 24 ADRs and an explicit decision-index discipline, an implicit choice this load-bearing should be documented — otherwise the next contributor reading P21's layer rules will reasonably ask "why aren't these projects?"

**Implementation steps**

1. Write an ADR titled "Folder-based layering inside one assembly, not separate csproj projects." Include the rejected alternative (4-project split) and the load-bearing reasons:
   - Single deployable; single ops surface; single CI matrix.
   - Migrations need `Microsoft.EntityFrameworkCore.Design` reachable from the project that owns the DbContext — splitting `Infrastructure` adds a `dotnet ef --project` ceremony for every migration.
   - Test project would need to reference 4 csproj instead of 1 — increases coupling, not decreases it.
   - The team is small (1–2 devs). 4-project navigation is a tax, not a feature, at this scale.
2. State the trigger conditions for revisiting: (a) team grows beyond 4 devs; (b) a domain wants to ship as a separate service; (c) a domain's dependencies (e.g. ANAF integration) genuinely don't belong in the same package.
3. Link from `system-architecture.md` so the next reviewer finds it.

**Schema / API changes** — none.

**Risks** — none.

---

### #23 — Interface ↔ implementation convention: introduce `Abstractions/` subfolder per feature

| Field | Value |
|---|---|
| Category | `refactor` |
| Complexity | 2 |
| Business impact | 4 — directly resolves the maintainer's "interfaces and classes in the same place" complaint |
| Priority score | 20 |
| Estimated effort | 1.5 dev-days |
| Affects | every folder that has `IFoo.cs` next to `Foo.cs`; namespace shifts only |

**What and why**

Today `Services/IAuthService.cs` is alphabetically interleaved with `Services/AuthService.cs`. `Services/Invoicing/` has 5 `I*.cs` mixed with 8 implementations. The maintainer called this out as scaling pain. Three options exist; this proposal picks one and argues against the other two.

**Option A (REJECTED) — keep them side-by-side.** Status quo. The folder listing becomes more useful when the number of files is small (≤6 per feature). It becomes increasingly noisy at 8+ per feature, and at the top-level `Services/` (72 files) it's already unreadable.

**Option B (REJECTED) — interfaces live with the CONSUMER per Dependency Inversion.** Pure-DIP architecture has `IOrderRepository` declared in `Application/Orders/` and `OrderRepository` declared in `Infrastructure/Persistence/`. Theoretically clean. For this codebase: the consumer is *also* the implementation's only consumer — there's no second-implementation-of-IOrderService anywhere. Forcing the indirection adds noise without unlocking testability or alternative implementations.

**Option C (RECOMMENDED) — `Abstractions/` subfolder per feature.** Inside each `Services/<Feature>/` folder (or after P21, inside each `Application/<Feature>/` folder), introduce an `Abstractions/` subfolder. All `I*.cs` files move there. Implementations stay at the feature root.

Concrete target structure after P21 + P23:

```
Application/Orders/
├── Abstractions/
│   ├── IOrderService.cs
│   ├── IOrderNumberService.cs
│   ├── IOrderPhotoPromoter.cs
│   ├── IOrderEmailService.cs
│   ├── IAdminOrderService.cs
│   ├── IAdminStatsService.cs
│   ├── IPromotionQueue.cs
│   ├── IOriginalPurger.cs
│   ├── ICreateOrderHandler.cs        ← from P25
│   └── IOrderPaidEventDispatcher.cs   ← from P11
├── Services/
│   ├── OrderService.cs
│   ├── OrderNumberService.cs
│   ├── OrderEmailService.cs
│   └── ...
├── Handlers/
│   ├── CreateOrderHandler.cs          ← from P25
│   └── OrderPaidEventDispatcher.cs    ← from P11
└── Dtos/
    └── ...
```

**Implementation steps**

1. Coordinate with P21. If P21 ships first, this becomes "add `Abstractions/` subfolder per `Application/<Feature>/`."
2. Move `I*.cs` files into `Abstractions/` per feature. Update namespaces from `PhotoPrint.API.Application.Orders` to `PhotoPrint.API.Application.Orders.Abstractions`.
3. Consumers in other features should reference `Abstractions/` namespace — explicit DIP.
4. Update DI registrations in `Program.cs` / extension methods — these only need a `using` directive change.
5. `dotnet build && dotnet test` after each batch.

**Schema / API changes** — none.

**Risks** — minor; namespace churn touches every `using` block, but it's mechanical.

---

### #24 — Explicit "no repositories" policy + analyzer rule: `IQueryable` may not appear in any service public signature

| Field | Value |
|---|---|
| Category | `refactor` |
| Complexity | 1 — write the rule + one analyzer config |
| Business impact | 3 — locks in a property we already have but isn't enforced |
| Priority score | 19 |
| Estimated effort | 0.5 dev-day |
| Affects | new `memory-bank/standards/data-access-conventions.md`, possibly `Directory.Build.props` for the analyzer |

**What and why**

Today services inject `PhotoPrintDbContext` directly and write LINQ inline. There is no repository pattern. The first-pass review deferred this decision — this proposal takes a position: **keep the no-repository posture, but document it and enforce the one property that protects it (`IQueryable` may not leak from a service).** Grep confirms IQueryable currently doesn't leak — make sure it stays that way.

Two reasons to NOT introduce repositories now:

1. **The pre-deployment + single-DB + single-tenant scale doesn't justify it.** Repositories are a useful abstraction when (a) you have multiple data stores (you don't), (b) you swap implementations between test and prod (you don't — InMemory is the swap and it's good enough), or (c) you want to enforce query reuse (today, you don't — each service has its own queries).
2. **The 25 "unit" tests that construct an InMemory DbContext already prove the test seam works without repositories.** Adding `IOrderRepository` between `OrderService` and the DbContext would force every test to mock the repository, which means every service test rewrites itself. That's a 100-test rewrite for no behaviour gain.

**One rule that locks the posture:** no service public method may return `IQueryable<T>`. Services materialise (`ToListAsync` / `FirstOrDefaultAsync`) before returning. This protects:
- Test isolation (no lazy enumeration crossing the test boundary).
- Provider portability (a query tree pinned to EF Core leaks the persistence concern up).
- Connection lifetime (no DataReader open beyond the service method).

**Implementation steps**

1. Write `memory-bank/standards/data-access-conventions.md` covering:
   - "Services inject `PhotoPrintDbContext` directly. No repositories."
   - "`IQueryable<T>` may not appear in any public method signature of a service. Materialise inside the service."
   - "Each service owns its queries. Duplicated query shape across services is a smell — extract to a private static helper or a shared `XQueryHelpers` class only when 3+ services share the same shape."
   - "Cross-service `SaveChangesAsync` coordination is implicit via the scoped DbContext. Document on a per-handler basis when this is load-bearing."
2. Add a `RS1024` / banned-API analyzer rule (via the existing `Microsoft.CodeAnalysis.BannedApiAnalyzers` package) that flags any `IQueryable<T>` return type in `Application/.../Services/*.cs` and `Application/.../Abstractions/I*.cs`. If the rule fails on existing code, we have a leak we missed — fix it.
3. Link the new convention doc from `system-architecture.md`.

**Schema / API changes** — none.

**Risks** — adding the analyzer may surface a real leak we haven't found. That's a *good* outcome; it just becomes a tracking ticket.

---

### #25 — Handler-per-use-case pattern (without MediatR; just `IXHandler` interfaces)

| Field | Value |
|---|---|
| Category | `refactor` |
| Complexity | 3 |
| Business impact | 4 — makes use cases discoverable + testable + composable |
| Priority score | 18 |
| Estimated effort | 3–4 dev-days |
| Affects | `Application/<Feature>/Handlers/` per feature; controllers become thin |

**What and why**

`OrderService.CreateFromCartAsync` is 145 LOC mixing 8 concerns. `WebhooksController.HandleStripePaymentSucceededAsync` and the EuPlatesc IPN handler share an identical 5-step post-Paid fan-out. There is no place to put a multi-step use case. Introduce a handler-per-use-case pattern: one class per controller action (or per webhook event, or per background-job tick) implementing `IXHandler.HandleAsync(XCommand, CancellationToken)`.

**Should we add MediatR (the NuGet package)?**

| For MediatR | Against MediatR |
|---|---|
| Mature, well-known, IServiceCollection-friendly. | One more dependency to track (the user's Concern 2). |
| Built-in pipeline-behavior support (logging, validation, transaction). | The pipeline-behavior story is half a reason to adopt MediatR — and we already have FluentValidation as a pipeline (FluentValidation.AspNetCore) + transaction is implicit per-request scope. |
| Notifications (publish/subscribe) are useful for OrderPaid → 5 side effects fan-out. | We can write 30 lines of `IRequestHandler` interfaces ourselves and skip the dependency. |
| 50K+ GitHub stars, low bus factor risk. | The author (Jimmy Bogard) recently relicensed MediatR — commercial use is now paid above a usage threshold. Don't want to assume that surface. |

**Recommendation: NO MediatR.** Roll our own `IHandler` interface. The pattern is 30 LOC. We retain control of the dependency tree.

```csharp
public interface ICommandHandler<TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct = default);
}

public interface IEventDispatcher<TEvent>
{
    Task DispatchAsync(TEvent evt, CancellationToken ct = default);
}
```

**Concrete migration targets (priority order):**

1. `CreateOrderCommand` + `CreateOrderHandler` — extracts the 145-LOC `OrderService.CreateFromCartAsync`. The result type is the existing `OrderCreationResult`.
2. `OrderPaidEvent` + `OrderPaidEventDispatcher` — this IS P11; it's the canonical use of the new pattern. Both webhook paths construct the event and call the dispatcher.
3. `RetryInvoiceUploadCommand` + handler — pulls the admin-retry CAS logic out of `AdminInvoicesController.RetryAsync`.
4. `PromoteOrderPhotosCommand` + handler — pulls the cloud-promotion sequence out of `OrderPhotoPromoter.cs`.

Stop there. The point is to make multi-step use cases legible, not to convert every CRUD endpoint into a handler. Single-statement controller actions (`GET /api/products`) stay as-is.

**Implementation steps**

1. Define `ICommandHandler<TCommand, TResult>` + `IEventDispatcher<TEvent>` in `Application/Shared/Abstractions/`.
2. Implement `CreateOrderHandler` — receives the cart-load deps, idempotency logic, VAT calc, order-number generation. The handler is what `OrderService.CreateFromCartAsync` becomes; the service method becomes a delegating one-liner.
3. Implement `OrderPaidEventDispatcher` — folds in P11.
4. Both controllers' `_orderService.CreateFromCartAsync` calls go through the handler instead.
5. Tests: each handler gets its own test file; `OrderServiceTests.cs` shrinks proportionally.

**Schema / API changes** — none.

**Risks**

- Easy to over-apply. Set the bar: handler when 3+ concerns or 50+ LOC. Otherwise keep it as a service method.
- The `OrderPaidEventDispatcher` ordering (invoice INSERT before SignalR broadcast — ADR-020) is load-bearing. Document in the XML doc.

---

### #26 — UI scaling refactor — break up the four largest pages + introduce `BaseApiService`

| Field | Value |
|---|---|
| Category | `refactor` |
| Complexity | 3 |
| Business impact | 3 |
| Priority score | 15 |
| Estimated effort | 3 dev-days |
| Affects | `features/home/`, `features/account/pages/saved-addresses/`, `features/account/pages/profile/`, `features/checkout/pages/delivery-step.ts`, `core/services/api/base-api.service.ts` (new) |

**What and why**

The Angular code is the project's healthiest layer **at the service layer** (largest service is 179 LOC). The structural problem lives in components — `home-page.ts` is **951 LOC**, three other pages exceed 380 LOC, and the inline-template + inline-fetch + inline-state pattern is repeated. Plus all 14 services hand-roll HttpClient calls with no shared base.

**Concrete extractions:**

1. `home-page.ts` (951 LOC) → split into:
   - `home/page/home-page.ts` (thin container, ~100 LOC)
   - `home/components/hero-section.component.ts`
   - `home/components/value-props.component.ts`
   - `home/components/pricing-teaser.component.ts`
   - `home/components/trust-strip.component.ts`
   - `home/components/cta-banner.component.ts`
2. `saved-addresses-page.ts` (498 LOC) → split smart container + `address-form.component.ts` (dumb) + `address-list-item.component.ts` (dumb).
3. `profile-page.ts` (473 LOC) → split smart container + `personal-info-form.component.ts` + `email-change-form.component.ts` + `password-change-form.component.ts` (it already calls all three flows).
4. `delivery-step.ts` (382 LOC) → extract `locker-selector.component.ts` (it already imports `locker-map.ts`).

**`BaseApiService` extraction:**

The 14 services in `core/services/` each hand-roll:
- `HttpClient.get/post/put/delete<T>(url, { withCredentials: true })`
- Error translation (catch HttpErrorResponse → user-friendly toast)
- Idempotency-key header threading on POST endpoints
- `BehaviorSubject` cache invalidation patterns

A `BaseApiService` (or RxJS operator wrapper) centralises this. Concrete API:

```typescript
@Injectable({ providedIn: 'root' })
export class BaseApiService {
  constructor(private http: HttpClient) {}

  protected get<T>(url: string, opts?: { params?: HttpParams }): Observable<T> {
    return this.http.get<T>(this.url(url), { ...opts, withCredentials: true })
      .pipe(catchError(this.translateError));
  }

  protected post<T>(url: string, body: unknown, opts?: { idempotencyKey?: string }): Observable<T> {
    const headers = opts?.idempotencyKey
      ? new HttpHeaders({ 'Idempotency-Key': opts.idempotencyKey })
      : undefined;
    return this.http.post<T>(this.url(url), body, { headers, withCredentials: true })
      .pipe(catchError(this.translateError));
  }
  // ...
}
```

**Implementation steps**

1. Create `core/services/api/base-api.service.ts`. Migrate one service at a time (`order.service.ts` first — it's small and has clear API verbs).
2. Component breakups land as separate PRs per page. Each follows the smart-container + dumb-child pattern.
3. Verify Vitest test suite still passes after each migration.

**Schema / API changes** — none.

**Risks** — visual regression on the home page. Take screenshots before/after.

---

### #27 — Shared `IntegrationTestBase` / `TestApplicationFactory` + `TestBuilders/` + unit-vs-integration audit

| Field | Value |
|---|---|
| Category | `refactor` |
| Complexity | 2 |
| Business impact | 4 — fixes the 11-factory duplication AND surfaces the misclassified "unit" tests |
| Priority score | 20 |
| Estimated effort | 2.5 dev-days |
| Affects | `src/PhotoPrint.Tests/Integration/_Base/` (new), 11 existing `*Factory.cs` files, `src/PhotoPrint.Tests/Builders/` (new), reclassification of 25 misnamed tests |

**What and why**

Three test-architecture problems compound:

1. **11 `WebApplicationFactory<Program>` subclasses each duplicate 30–80 lines of config.** Confirmed by file inspection — every factory hand-codes the same `Cors:AllowedOrigins`, `RateLimit:*`, `JwtSettings:*`, `Email:Provider`, `HealthCheck:UploadsPath`, `App:BaseUrl`. A change to the standard test config requires 11 file edits.
2. **There is no `TestBuilders/`.** Each test file inlines its own user/order/cart seeding. `AuthFactory.SeedConfirmedUserAsync` is the only seed-helper that exists; it's confined to one factory.
3. **25 tests under `Unit/` construct a `PhotoPrintDbContext` directly** — they're integration tests pretending to be unit tests. This couples service tests to schema and EF Core LINQ translation, makes them slow, and means the test pyramid is misleading.

**Concrete intervention:**

```
src/PhotoPrint.Tests/
├── _Base/
│   ├── PhotoPrintTestApplicationFactory.cs    # abstract base — current MetricsEndpointIntegrationTests.ObservabilityFactoryBase promoted
│   ├── TestConfigurationDefaults.cs            # the 25 standard config keys
│   ├── InMemoryDatabaseTrait.cs                # standard InMemory swap
│   └── NoOpEmailService.cs                     # moved from AuthFactory
├── Builders/
│   ├── UserBuilder.cs                          # fluent: new UserBuilder().Confirmed().WithEmail("x@y.com").Build()
│   ├── OrderBuilder.cs
│   ├── CartItemBuilder.cs
│   ├── InvoiceBuilder.cs
│   ├── UploadBuilder.cs
│   └── ...
├── Integration/                                # 11 factories shrink to ~30 LOC each (only override what differs)
│   ├── AccountFactory.cs
│   ├── AuthFactory.cs
│   └── ...
└── Unit/
    ├── Domain/                                  # genuine unit tests of pure logic
    │   ├── OrderStatusMachineTests.cs           # already exists, already correct
    │   ├── VatCalculatorTests.cs                # already exists
    │   └── ...
    └── Application/                             # service-level tests with mocked deps
        └── ...
```

**Reclassification of the 25 misnamed tests:**

The 25 tests that construct a real `PhotoPrintDbContext` are NOT unit tests. They're micro-integration tests of service-against-EF-Core. Two paths:

- **Option A (preferred):** rename the folder. Move them to `tests/Integration/ServiceLevel/` and keep the `new PhotoPrintDbContext(...)` pattern (using the new shared in-memory helper). The tests are correct; their naming was wrong.
- **Option B (rejected):** introduce repositories so the service tests can mock `IOrderRepository`. P24 explicitly says no.

**Implementation steps**

1. Promote `MetricsEndpointIntegrationTests.ObservabilityFactoryBase` (lines 88–138) to `src/PhotoPrint.Tests/_Base/PhotoPrintTestApplicationFactory.cs`. Make it `public abstract`.
2. Refactor each of the 11 factories to inherit from the new base. Each factory keeps only its feature-specific overrides (e.g. `AuthFactory` adds `NoOpEmailService` and seeds confirmed users; `PaymentFactory` adds Stripe keys).
3. Create `Builders/` with fluent builders for the 6 most-used entities. Use the existing `OrdersFactory.SeedConfirmedUserAsync` pattern as a template.
4. Reclassify the 25 misnamed tests: move them under `tests/Integration/ServiceLevel/` (keep the folder mirror of the new feature folders from P06). Their `[Fact]` content is unchanged.
5. Update test discovery in CI — the `dotnet test --filter` patterns may need to reflect the new folder structure.

**Schema / API changes** — none.

**Risks** — Vitest-like xUnit fixture sharing has subtle ordering gotchas. The base factory uses `IClassFixture<T>` for share-per-class; verify nothing depends on per-test isolation that the share would break.

---

### #28 — Adopt `TimeProvider` consistently — kill the 63 raw `DateTimeOffset.UtcNow` calls in 35 files

| Field | Value |
|---|---|
| Category | `refactor` |
| Complexity | 2 |
| Business impact | 3 — testability + consistency + sets the stage for any future time-zone work |
| Priority score | 17 |
| Estimated effort | 2 dev-days |
| Affects | 35 files, 63 call sites; concentrate on `Services/AuthService.cs` (13 calls) and `Services/OrderService.cs`, `Services/AccountService.cs`, all `BackgroundJobs/*.cs`, all `Models/*.cs` default property assignments |

**What and why**

Half the codebase uses `TimeProvider` (the 2026-era code: bolts 037+039+044) — the other half hard-codes `DateTimeOffset.UtcNow`. The split runs cleanly along bolt-vintage lines. The older code can't be deterministically tested without freezing the system clock; the newer code uses `FakeTimeProvider` and is fully time-deterministic.

Concrete: `AuthService.cs` has **13 raw `DateTimeOffset.UtcNow` calls** at lines 79, 109, 127, 151, 179, 187, 210, 229, 269, 299, 317, 339, 357 — covering refresh-token expiry, lockout-end, email-confirmation expiry, password-reset expiry. Every one of these is a deterministic-test-blocking call.

Tests today work around this by either (a) accepting wall-clock-dependent assertions, (b) using `Thread.Sleep` in test (slow + flaky), or (c) calling `DateTimeOffset.UtcNow` from the test code and asserting "approximately within 5 seconds." Adopting `TimeProvider` everywhere fixes all three.

**Implementation steps**

1. Add a banned-API analyzer rule: `DateTimeOffset.UtcNow` is forbidden in `Application/` and `Infrastructure/` (the only exception is `Domain/` if a static needs a `now` fallback, but document the exception).
2. Refactor the offenders in priority order:
   - `Services/AuthService.cs` — 13 calls. Inject `TimeProvider _clock`; replace `DateTimeOffset.UtcNow` → `_clock.GetUtcNow()`. Tests get `FakeTimeProvider`.
   - `Services/AccountService.cs` (4 calls), `Services/AdminOrderService.cs` (3 calls), `Services/OrderService.cs` (1 call), `Services/EuPlatescService.cs` (3 calls).
   - All `BackgroundJobs/*.cs` (current count: 6 files using raw clock).
   - `Models/*.cs` default property assignments (`public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;`) — these are the trickiest. Options: (i) leave them; defaults run only when an entity is constructed in test, and tests should construct via builders that set the clock explicitly (synergy with P27 Builders). (ii) Replace with `default` + set in the handler. Recommend (i) — model default is a write-time fallback, not a service concern.
3. Add unit tests using `Microsoft.Extensions.TimeProvider.Testing.FakeTimeProvider` (already a referenced package in the newer test files) for at least one time-sensitive scenario per refactored service.

**Schema / API changes** — none.

**Risks**

- A `TimeProvider _clock` constructor parameter on `AuthService` is a public-interface change to the constructor — every test that constructs `AuthService` directly needs an update. P27's Builders fold this cost in: the `UserBuilder` / `AuthServiceBuilder` (test helper) hides the constructor signature behind a fluent API.

---

## Roadmap

### Now (< 2 weeks) — Quick wins

These are score >= 18 AND complexity <= 2:

- **P01** — Patch OpenTelemetry CVE (0.5 day)
- **P02** — Stripe.net unification + Central Package Management (1 day)
- **P03** — Renovate config (0.5 day)
- **P05** — `ForwardedHeadersMiddleware` (0.5 day)
- **P07** — Program.cs subsystem extension methods (1.5 days)
- **P04** — `/api/admin/system-info` feature manifest (2 days)
- **P12** — Multi-replica-readiness doc (0.5 day)
- **P19** — Refresh standards docs (0.5 day)

Total: ~7 dev-days. Lands the security/dependency-hygiene story and ships the visibility tooling that directly answers Concern 3.

### Next (2-8 weeks) — Main sprint

Score 14-17 OR complexity 3, dependency-ordered:

- **P10** — `IFeatureGate` (2 days; feeds P04)
- **P06** — `Services/` feature-folder refactor (3-4 days; biggest single intervention against Concern 1)
- **P08** — Global rate limit + admin policy constant (1 day)
- **P11** — `OrderPaidEventDispatcher` (1 day)
- **P13** — `AuthService` decomposition (2 days)
- **P14** — Webhook + OrderService god-method decomposition (2 days)
- **P15** — Per-entity DbContext configurations (1.5 days)
- **P17** — Background-job liveness + ANAF metrics (1.5 days)
- **P18** — Bundle budget + e2e smoke tests (2 days)

Total: ~17 dev-days. Lands the bulk of the structural fix the user is asking for.

### Later (> 8 weeks) — Strategic

- **P09** — Refund / return endpoint (7-10 days; legal-compliance, plan for launch)
- **P16** — Domain layer extraction (1.5 days; but value goes up after P06 lands)
- **P20** — Discount / coupon engine (5-8 days; growth lever, not pre-launch)

---

## Mapping to user's three concerns

The user explicitly asked for 1-3 proposals per concern. The mapping:

**Concern 1 — Scaling pains across API + UI + Tests projects:**
- **P06** (Services feature folders — the central fix at the Services level)
- **P21** (Presentation/Application/Domain/Infrastructure folder layering — the broader frame; P06 is one PR inside it) — **the new central fix**
- **P23** (interface ↔ implementation `Abstractions/` subfolder — directly answers the maintainer's "interfaces and classes in the same place" complaint)
- **P25** (handler-per-use-case — gives ad-hoc 145-LOC service methods a home)
- **P26** (UI scaling — break up the four largest pages + `BaseApiService`)
- **P27** (Test layer — shared factory base + Builders + reclassify misnamed unit tests)
- **P28** (TimeProvider consistency)
- **P07** (Program.cs subsystem extraction)
- **P14** (WebhooksController + OrderService god-methods)
- Supporting: P11, P13, P15, P16, P22 (the "no clean-arch split" ADR)

**Concern 2 — Dependency sprawl:**
- **P01** (OpenTelemetry CVE patch)
- **P02** (Stripe.net unification + Central Package Management)
- **P03** (Renovate config — the sustainable answer)
- **P24** (data-access policy doc + IQueryable analyzer — locks in the no-repository posture so a future contributor doesn't introduce MediatR + AutoMapper + Repositories + Specifications "to be safe")

**Concern 3 — Hidden functionality discoverability for regression:**
- **P04** (`/api/admin/system-info` feature manifest — directly addresses the user's framing)
- **P10** (`IFeatureGate` — typed source of truth for flags)
- **P17** (Background-job liveness — catches the silent-death case)
- **P25** (handlers-per-use-case make the use-case inventory grep-able — `find Application -name '*Handler.cs'` returns the full list)
- Supporting: P12 (multi-replica doc)

---

## Risks called out

1. **Refactor sequencing risk.** P06 (the big feature-folder move) and P13/P14/P15 are all touching the same files in different ways. **Plan P06 to merge first**, then the decompositions land in the new folders. If they ship in the wrong order, conflict-hell.
2. **Stripe.net 46→47 break risk.** P02 may surface real API changes. Run the full webhook integration test pass; have a rollback PR ready.
3. **OpenTelemetry 1.11.x→1.15.x risk.** The `Prometheus.AspNetCore` and `EntityFrameworkCore` instrumentation packages are still on a `-beta` track. Their API may have moved. Smoke-test `/metrics` and EF span emission.
4. **`/metrics` is broken-on-day-1 of production without P05.** Allow-list reads `Connection.RemoteIpAddress` which is the reverse-proxy IP without `ForwardedHeadersMiddleware`. This is the most ship-blocking finding in the review.
5. **Refund flow (P09) intersects archive retention (bolt 052) and ANAF (bolt 039).** A refunded order should NOT auto-purge originals on the Shipped trigger, and SHOULD push a credit-note UBL to ANAF. Both touch live, regulated paths. Plan dedicated review.
6. **The `Anaf:Enabled=false` boot is currently the only test of "off-by-default fully no-ops."** A future bolt that accidentally moves an ANAF DI registration outside the `if (anafEnabled)` guard would only surface in production. P04's system-info endpoint + P10's `IFeatureGate` are the durable mitigation.
7. **Feature-folder refactor (P06) is heavy on git churn.** Touches ~80 files. Plan it for a quiet week and merge in 5-6 PRs by feature. Reviewers should look at each PR as a single namespace shuffle, not a behaviour change.
8. **Test brittleness on namespace rename.** Some test files use `using static PhotoPrint.API.Services.OrderStatusMachine`. After P16, this becomes `Domain.Orders.OrderStatusMachine`. Mechanical find-replace.

---

## Architectural surprises the maintainer may not be tracking

1. **Two Stripe.net versions are loaded into the same solution.** `dotnet list package --outdated` reports the Tests project resolves to 47.0.0 even though both csproj declare 46.3.0. This is the silent-most kind of "tests pass / prod breaks" risk.
2. **`/metrics` IP allow-list is broken behind the reverse proxy as currently coded.** No `ForwardedHeadersMiddleware`. Day-1 of deployment, the allow-list is wrong.
3. **Angular is 21.x, not 17+.** The `tech-stack.md` standards doc is **three majors behind reality**. Same doc lists `heic2any` and `ng2-charts` — both uninstalled. Vitest is the runner, not Jasmine/Karma.
4. **`OpenTelemetry.Exporter.OpenTelemetryProtocol 1.11.2` has an unpatched Moderate CVE.** GHSA-4625-4j76-fww9. Fix is a 1.15.x bump.
5. **Sentry SDK is two majors behind** (4.13 vs 6.6). Not a CVE today, but a long-term deferral.
6. **There is no refund endpoint.** EU consumer law (Directive 2011/83/EU) requires a 14-day cooling-off period with refund. The architecture has all the pieces — `Invoices` table, ANAF credit-note via UBL invoice type 381, Stripe refund API, `OrderStatus.Refunded` would be a trivial enum addition — but none of them are wired. Launch-blocker for legal compliance unless explicitly accepted in writing.
7. **The `Data/Configurations/` folder exists with ~1 file in it,** while `PhotoPrintDbContext.OnModelCreating` is 400 lines of inline lambdas. The team started the right refactor and stopped.
8. **`SemaphoreSlim(MaxConcurrentSamedayCalls)` is constructed in `ShipmentTrackingJob`'s constructor and disposed in `Dispose`** — but `BackgroundService.Dispose` is rarely called gracefully if the host shuts down abnormally. Minor; flagging because the same pattern is in `OrderPhotoPromotionWorker`.
9. **There is no `Directory.Build.props` or `Directory.Packages.props`.** No central place for "this is what we depend on." For a maintainer who said "I want to keep track of all of them," this is the answer.
10. **The CSP doesn't allow Angular's runtime inline styles** because there's no `style-src` directive — it falls back to `default-src 'self'` which doesn't include `'unsafe-inline'`. The browser may be silently dropping styles in production. Test on a built bundle before launch.
11. **24 ADRs in 30 days** (ADR-001 dated 2026-05-05, ADR-024 dated 2026-06-03). The ADR cadence is excellent. Whatever ritual is producing them, keep it. But: there's no index of which ADRs are still load-bearing vs which have been superseded. ADR-008 (storage tier) and ADR-009 (R2 recommendation) are both relevant; ADR-010's reference to "polling-table DB load" is partially superseded by ADR-023's later analysis. Add a `Status: superseded-by-ADR-XYZ` field next time one ADR overrides another.
12. **Test code is bigger than production code.** Tests: 16,178 LOC. API (ex-migrations): 14,997 LOC. UI TS: 12,962 LOC. This is healthy by most measures but means the test refactor (mirror of P06) is itself a significant chunk of work. Budget accordingly.
13. **`TimeProvider` is half-adopted across exactly the bolt-vintage cleavage.** 12 files use `TimeProvider` (bolts 037, 039, 044 — the 2026 code); 35 files use raw `DateTimeOffset.UtcNow` (everything older). `AuthService.cs` alone has 13 raw calls; tests of token expiry can't be deterministic without `FakeTimeProvider`. P28 fixes it.
14. **11 `WebApplicationFactory<Program>` test factories duplicate the same 50 lines of config.** No shared base. The bolt 044 author started the right refactor (`internal abstract ObservabilityFactoryBase` at `MetricsEndpointIntegrationTests.cs:88`) and stopped. P27 promotes it.
15. **25 "unit" tests construct an InMemory DbContext directly** — they're integration tests pretending to be unit tests. `tests/Unit/Services/OrderServiceTests.cs` (645 LOC) is the largest example. The test pyramid is misleading; P27 reclassifies.
16. **Four controllers inject `PhotoPrintDbContext` directly** (`InvoicesController`, `AdminInvoicesController`, `PaymentsController`, `WebhooksController`). Presentation-to-data-access shortcut around the Service layer. P21 + P25 unwind it.
17. **`Validators/` has feature-folder subfolders (`Auth/`, `Cart/`, ...) — but the 7 `*SettingsValidator.cs` files live FLAT at the root** alongside those feature folders. Same half-applied-convention pattern as `Services/`. P21 sweeps them into `Configuration/Validators/`.
18. **`home-page.ts` is 951 LOC.** Eight hundred lines longer than the largest TypeScript file should ever be. P26 breaks it up.

---

## Implementation Plan

> Added 2026-06-03 (second pass). The maintainer explicitly asked for a plan that groups proposals by similarity (touch the same files), importance (ranked by score), and impact + dependency ordering. The plan groups the 28 proposals into 7 work streams, sequences them, and identifies the pre-launch must-haves.

### Grouping rationale

Proposals are grouped by **which files they touch**, not by category. Two proposals that both rewrite `Program.cs` MUST ship together or in strict sequence to avoid merge hell. Two proposals on unrelated files can ship in parallel even if they have different priority scores. The groups are sized to be one-PR-ish — between 0.5 and 5 dev-days each.

**Dependency rules used:**

1. **P21 (folder layering) is a prerequisite for everything structural.** Doing P11/P13/P14/P15/P16/P23/P24/P25/P28 first means doing them twice — once now, once after the folder shuffle. Sequence P21 EARLY.
2. **P06 was the first-pass version of P21's Services-folder piece.** They MERGE into P21; P06 alone is no longer the right ask.
3. **P02 (Central Package Management) is a prerequisite for P03 (Renovate config).** Renovate needs `Directory.Packages.props` to do meaningful grouping.
4. **P27 (test refactor) MUST track P21 (structure refactor) in lockstep**, or every PR breaks the build twice.
5. **P05 (ForwardedHeadersMiddleware) is a strict pre-launch must-have** — `/metrics` is broken on day-1 of production otherwise.
6. **P25 (handler-per-use-case) consumes P11 (OrderPaidEventDispatcher).** P11 IS the canonical first handler — fold P11 into P25's first PR.

### Group 1 — Security & dependency hygiene (parallel to everything; ship FIRST)

- **Theme:** Patch the known CVE; lock down the dependency tree; turn on automated upgrades.
- **Proposals:** P01 (OTel CVE), P02 (Central Package Management), P03 (Renovate), P05 (ForwardedHeadersMiddleware).
- **Total effort:** 2.5 dev-days.
- **Ship order:** P01 → P02 → P03 → P05 (sequential because they touch the same csproj / Program.cs files).
- **Blocks/unblocks:** Unblocks all other work because a tree of known CVEs is a security audit problem; nothing here blocks subsequent groups.
- **Pre-launch must-have:** ALL FOUR. P01 (CVE) is a legal-audit blocker; P05 (forwarded-headers) is a day-1 ops blocker.

### Group 2 — Observability + boot composition + system manifest (parallel to Group 1)

- **Theme:** Make the system inspectable in production. Make the boot script readable. Catch the silent-death-of-background-jobs scenario.
- **Proposals:** P07 (Program.cs subsystem extension methods), P04 (`/api/admin/system-info`), P10 (`IFeatureGate`), P17 (background-job liveness + ANAF metrics), P12 (multi-replica-readiness doc), P19 (refresh standards docs).
- **Total effort:** 7.5 dev-days.
- **Ship order:** P07 → P10 → P04 (P04 consumes P10's `IFeatureGate.GetAll()`); P17 + P12 + P19 in parallel.
- **Blocks/unblocks:** P07 makes the Program.cs refactor diff small enough to review during Group 4. P10 powers P04. Nothing here is blocked by Group 1.
- **Pre-launch must-have:** P17 (liveness check), P19 (docs accuracy). P04 + P10 are strong nice-to-haves.

### Group 3 — Structural refactor: the BIG one (sequential, frozen feature work for ~1.5 weeks)

- **Theme:** Establish the Presentation / Application / Domain / Infrastructure layering, the interface ↔ implementation convention, the no-repository policy, and the handler pattern. This is the maintainer's core complaint.
- **Proposals:** P21 (layering — folds P06 + P16), P23 (`Abstractions/` subfolders), P24 (no-repository policy + analyzer), P25 (handler-per-use-case — folds P11), P22 (the "no clean-arch split" ADR).
- **Total effort:** 11 dev-days.
- **Ship order:**
  1. P22 first (0.5 day) — write the "we chose folders, not csproj projects" ADR so the layering PRs reference it.
  2. P21-PR1: introduce `Domain/` + move pure-functional types (1 day).
  3. P21-PR2: introduce `Infrastructure/` + move `Data/`, `BackgroundJobs/`, `Observability/`, EF Core implementations (1.5 days).
  4. P21-PR3: introduce `Web/` + move `Controllers/`, `Middleware/`, etc. (1 day).
  5. P21-PR4: promote `Services/<Feature>/` → `Application/<Feature>/Services/` (the OLD P06 — but now scoped inside the layered tree) (2 days).
  6. P23: add `Abstractions/` per feature folder (1.5 days).
  7. P24: document policy + add analyzer (0.5 day).
  8. P25: handler-per-use-case for the four target commands (3 days).
- **Blocks/unblocks:** This group EATS P06, P11, P13, P14, P16. It UNBLOCKS Group 5 (the decompositions land in the new shape, not the old shape).
- **Pre-launch must-have:** No. None of these change behaviour; they change code shape. But not doing them before launch means doing them under deploy-pressure, which is strictly worse.

### Group 4 — Test layer refactor (track Group 3 in lockstep)

- **Theme:** Promote the shared factory base, introduce Builders, reclassify the misnamed unit tests.
- **Proposals:** P27, P28 (TimeProvider — sets up FakeTimeProvider use across the suite).
- **Total effort:** 4.5 dev-days.
- **Ship order:** P28 first (adopting `TimeProvider` in services creates new constructor params; Builders in P27 hide that complexity from test code), then P27.
- **Blocks/unblocks:** Strict pair with Group 3 — every Group-3 PR breaks tests, so P27's IntegrationTestBase + Builders make Group-3 PRs reviewable. **In practice, ship the Group-3 + Group-4 PRs interleaved**, not sequentially.
- **Pre-launch must-have:** No, but a healthy test suite is what gives you the confidence to deploy.

### Group 5 — Decomposition of god-methods + DbContext config split (after Group 3 lands)

- **Theme:** Now that the folder structure is right, decompose the god-methods INTO the new structure.
- **Proposals:** P13 (AuthService decomposition into 3 services), P14 (WebhooksController + OrderService god-methods), P15 (per-entity `IEntityTypeConfiguration<T>`), P08 (global rate limit + admin policy constant).
- **Total effort:** 6.5 dev-days.
- **Ship order:** P08 first (smallest, unrelated to the others). P15 in parallel (touches only `Data/`). P14 then P13 (both touch overlapping service surfaces).
- **Blocks/unblocks:** Doing these after Group 3 means the new files land in `Application/Auth/Services/`, `Application/Orders/Handlers/`, etc. — the right place from the start.
- **Pre-launch must-have:** P08 (admin policy constant) is a soft must-have because the string-based "Admin" role is a footgun. The decompositions are not.

### Group 6 — UI scaling refactor (parallel to backend Groups 3–5)

- **Theme:** Break up the four 380+ LOC pages. Introduce `BaseApiService`. Add bundle budget + e2e smoke tests.
- **Proposals:** P26 (UI page breakups + `BaseApiService`), P18 (bundle budget + e2e).
- **Total effort:** 5 dev-days.
- **Ship order:** P18 first (1-line angular.json change + the e2e foundation); P26 by page (home → saved-addresses → profile → delivery-step).
- **Blocks/unblocks:** Completely independent of backend groups. Can run on a second developer in parallel.
- **Pre-launch must-have:** P18 (e2e smoke tests) — yes, three real-money paths need automation before launch.

### Group 7 — Feature work (after launch unless legally required)

- **Theme:** New customer-facing capabilities.
- **Proposals:** P09 (refund / return — legal), P20 (discount / coupon — growth).
- **Total effort:** 12–18 dev-days.
- **Ship order:** P09 first IF launching to EU customers (the 14-day cooling-off right is non-optional). P20 strictly after launch.
- **Blocks/unblocks:** Both are best done after Group 3 ships so they land in the layered shape.
- **Pre-launch must-have:** P09 IF the launch market includes EU consumers. Otherwise post-launch.

### Sequencing diagram — 6-week plan for 1 developer (FT)

```
Week 1 (Jun 03-09):
  Mon: P01 (OTel CVE)            ½d   ← strict launch blocker
  Mon: P02 (CPM + Stripe.net)    1d
  Tue: P03 (Renovate)            ½d
  Tue: P05 (ForwardedHeaders)    ½d   ← strict launch blocker
  Wed: P07 (Program.cs extns)    1½d
  Thu/Fri: P10 (IFeatureGate)    2d
  ----- Group 1 + half of Group 2 SHIPPED -----

Week 2 (Jun 10-16):
  Mon/Tue: P04 (system-info)     2d
  Wed: P17 (job liveness)        1½d
  Thu: P12 (multi-replica doc)   ½d
  Thu: P19 (docs refresh)        ½d
  Fri: P22 (no-split ADR)        ½d
  Fri: P28 (TimeProvider audit)  start ←
  ----- Group 2 SHIPPED; Group 4 started -----

Week 3 (Jun 17-23):
  Mon: P28 (TimeProvider)        finish 2d
  Wed: P21-PR1 (Domain/)         1d
  Thu: P21-PR2 (Infrastructure/) 1½d
  ----- Group 4 partial; Group 3 started -----

Week 4 (Jun 24-30):
  Mon: P21-PR3 (Web/)            1d
  Tue/Wed: P21-PR4 (Application/) 2d
  Thu/Fri: P27 (IntegrationTestBase + Builders) 2½d ← interleaved with P21 PRs
  ----- Group 3 mostly landed; Group 4 landed -----

Week 5 (Jul 01-07):
  Mon: P23 (Abstractions/ subfolders) 1½d
  Tue: P24 (no-repo policy)      ½d
  Wed/Thu/Fri: P25 (handlers — includes P11) 3d
  ----- Group 3 SHIPPED -----

Week 6 (Jul 08-14):
  Mon: P08 (global rate limit + admin policy) 1d
  Tue/Wed: P15 (per-entity EF config) 1½d
  Thu/Fri + Wk7 Mon/Tue: P13 (AuthService decomposition) 2d
  Mid Wk7: P14 (Webhook + Order god-methods) 2d
  ----- Group 5 SHIPPED -----

Week 7-8: Group 6 (UI) — handed to a second developer in parallel from Week 4.
Week 8+: Group 7 (P09 refund if EU launch) before customer launch.
```

If a second developer is available, **Group 6 (UI) and Group 1 (security hygiene) parallelise** — the second dev can ship Group 6 in weeks 1–3 while the first dev grinds the backend layering.

### Pre-launch must-haves vs post-launch nice-to-haves

**Pre-launch (must ship before first real-money transaction):**

| ID | Why blocking |
|---|---|
| P01 | Moderate-severity CVE in deployed observability dependency. Audit blocker. |
| P02 | Two Stripe.net versions in the same solution is a silent prod-breaks risk. |
| P05 | `/metrics` IP allow-list is broken on day-1 behind Caddy. Ops blocker. |
| P17 | Background jobs (invoice upload, AWB creation) failing silently means missed ANAF SLA. Compliance blocker. |
| P19 | Standards docs lying about Angular version / test runner is contributor-onboarding poison. |
| P09 (conditional) | If EU customers, 14-day-cooling-off refund is legally required. |

**Strong nice-to-have pre-launch (ship if time allows):**

| ID | Why |
|---|---|
| P03 | Renovate is the durable answer to dependency hygiene. |
| P04 + P10 | The user's regression-discoverability concern. |
| P08 | Admin policy constant + global rate limit are pre-launch hardening. |
| P18 | E2e smoke tests on the three payment paths. |

**Post-launch (no behaviour change OR no immediate user value):**

| ID | Why deferred |
|---|---|
| P21, P22, P23, P24, P25, P27, P28 | Pure refactors; no behaviour change. High value as the codebase grows but no launch-blocker. |
| P06, P07, P11, P12, P13, P14, P15, P16 | Same — all refactors. |
| P26 | UI refactor; no behaviour change. |
| P20 | Coupon engine — growth lever, not launch requirement. |

### Risk-adjusted recommended path (single-developer, 8-week target)

The big risk is **Group 3 mid-flight**: a partial layering migration leaves the codebase in a worse state than starting. Mitigation: each P21 PR must be reviewable and mergeable in isolation, and `dotnet build && dotnet test` must be green after EVERY PR.

Recommended weekly milestone schedule (1 FT dev, no parallel work):

- **Week 1:** Group 1 (Security & dependency hygiene). 4 PRs land. Production-readiness milestone.
- **Week 2:** Group 2 (Observability + manifest + IFeatureGate). 5 PRs land. Visibility milestone.
- **Week 3:** Group 4 first half (TimeProvider audit) + Group 3 first PR (Domain/). 2 PRs land. Layering foundation.
- **Week 4:** Group 3 PRs 2–4 (Infrastructure/, Web/, Application/) + Group 4 second half (IntegrationTestBase + Builders) interleaved. 6 PRs land. **Structural fix landed.**
- **Week 5:** Group 3 final PRs (Abstractions/, no-repo policy, handlers). 3 PRs land. **Maintainer's core complaint resolved.**
- **Week 6:** Group 5 (decompositions). 4 PRs land. God-methods gone.
- **Week 7:** Group 6 (UI refactor + e2e). 4 PRs land. Pre-launch UI hardening.
- **Week 8:** Group 7 conditional (P09 refund flow if EU launch). 1 PR lands. **Launch-ready.**

Total: 29 PRs over 8 weeks. ~3.6 PRs/week — sustainable for a single developer with code review.

**Risk callouts on the path:**

1. **Week 4 is the hardest week.** 6 PRs, all touching the folder structure, all needing test-suite green at each step. If anything slips, this is where it slips. Mitigation: pre-write the namespace find/replace scripts for each PR.
2. **Group 3 + Group 4 lockstep is non-negotiable.** Skipping Group 4 means Group 3 PRs each break ~25 test files. Doing Group 4 first means writing the IntegrationTestBase against the OLD folder shape and rewriting it for the NEW shape.
3. **If launch date moves earlier**, drop the schedule below at the Week 4 line. Ship Group 1 (must-haves) + Group 2 (visibility) + the conditional P09. Defer ALL structural work to post-launch. The codebase is launch-ready without the refactors; the refactors prevent FUTURE pain, not present pain.
4. **If launch date moves later**, do NOT add new features into the deferred slots. Add P03 (Renovate), P18 (e2e), P26 (UI refactor) instead — they pay back the most per dev-day before launch.
