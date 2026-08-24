# System Architecture

*(Rewritten 2026-07-14 from the code. Descriptive — states what IS, not what is planned.)*

## Overview

FotoTipar is a monolithic **ASP.NET Core 8 REST API** (`src/PhotoPrint.API`) with an
**Angular 21 SPA** (`src/PhotoPrint.UI`, standalone/zoneless, SPA-only — no SSR). Persistence is
**PostgreSQL 16** in every environment (see [data-stack.md](data-stack.md)); file
storage is **two-tier** (local disk + S3-compatible cloud). Real-time admin notifications via
SignalR. The API can serve the built SPA from `wwwroot` (`MapFallbackToFile`).

```text
┌────────────┐   REST/JSON + SignalR   ┌─────────────────────────────┐
│ Angular 21 │ ←─────────────────────→ │  ASP.NET Core 8 Web API     │
│ SPA        │                         │  + 9 background jobs        │
└────────────┘                         └───────────┬─────────────────┘
                     ┌────────────┬────────────────┼──────────────┬───────────────┐
               ┌─────▼─────┐ ┌────▼─────┐ ┌────────▼───────┐ ┌────▼────┐ ┌────────▼──────┐
               │ PostgreSQL│ │ Local    │ │ S3-compatible  │ │ Stripe  │ │ SMTP (dev)    │
               │ 16        │ │ disk     │ │ cloud (R2/S3/  │ │ EuPlat- │ │ SendGrid(prod)│
               │ (Npgsql)  │ │ (tier 1) │ │ MinIO, tier 2) │ │ esc     │ │ via EmailQueue│
               └───────────┘ └──────────┘ └────────────────┘ └─────────┘ └───────────────┘
```

Shipping is currently **`StaticShippingService`** (server-side static costs) + a DB-seeded
`EasyboxLocker` table; there is no live courier API integration in the code today.

## Request pipeline (order matters)

`CorrelationId` → `ExceptionHandler` → Serilog request logging → security baselines
(prod-only HSTS/HTTPS-redirect, security headers, CORS `AllowAngularApp`, rate limiter) →
response caching → static files → routing → authn → authz → controllers +
`/hubs/admin-orders` + `/health`.

## Authentication & authorization

- **JWT RS256**, access token 15 min (claims: sub, email, role, jti), issuer `fototipar`.
  Key from `JwtSettings:PrivateKeyPem` (dev key in gitignored `appsettings.*.Local.json`).
- **Refresh tokens**: 30 days, random 64 bytes, stored SHA-256-hashed, **rotated on every
  refresh**, delivered as HttpOnly Secure SameSite=Strict cookie scoped to `Path=/api/auth`.
- **Passwords**: ASP.NET Identity `PasswordHasher<User>` (PBKDF2) — *not* bcrypt, and not the
  full Identity stack. Lockout: 5 failures → 15 min. Email confirmation required to log in.
- **Google OAuth**: server-side `id_token` verification against Google's tokeninfo endpoint
  (5s timeout; unreachable → 502), `ExternalLogin` linking, then own JWT.
- **Guests**: `X-Guest-Token` header (GuestSession GUID) via a custom `GuestToken` scheme;
  policy **`DualAuth`** accepts Bearer JWT *or* guest token. Frontend stores the JWT in
  sessionStorage, the guest session in localStorage; **there is no refresh/silent-renew flow
  in the SPA** (401 → logout or clear-guest-token).
- Rate limits: global 100/min/IP; `auth` 10/min; register 5/h; resend-confirmation and
  forgot-password 3/h.

## Storage architecture (two-tier — ADR-007/008/009/011/012)

- `IStorageService` (byte persistence at **caller-supplied keys** — ADR-007; keys minted by
  `StorageKeys`: `uploads/{yyyy}/{MM}/{id}`, `thumbs/{id}.jpg`, `previews/{id}.jpg`).
- Implementations: `LocalStorageService` (disk, traversal-guarded) and `S3StorageService`
  (R2/S3/MinIO; Polly retry on transient errors; presigned URLs; 404 translated to
  `FileNotFoundException` so the contract is uniform across tiers).
- `IStorageRouter` resolves keyed services `"local"`/`"cloud"`; **every** read/write/delete
  routes by `Upload.StorageLocation` (`Local`|`Cloud`). New uploads always start Local.
- **Lifecycle** (intent 024): payment success enqueues promotion → `OrderPhotoPromotionWorker`
  (channel consumer, ≤4 concurrent orders, backoff retries) moves original+thumb+large-preview
  to cloud per-upload with **Confirmed-Write-Then-Delete** (ADR-011) and flips
  `StorageLocation`. Originals are purged from cloud at the production-complete status
  (default Shipped) and on cancel/refund (**Confirmed-Delete-Then-Update**); previews/thumbs
  are retention-deleted `RetentionMonths` (12) after `Order.PaidAt` (ADR-012).
- Boot: `S3BucketVerifier` fails the host fast if cloud is on but the bucket is unreachable.
- Serving: preview endpoint streams local files (private cache + ETag/304) or 302-redirects to
  a presigned URL for cloud files, `max-age` derived from the presign TTL.

## Background processing (all IHostedService/BackgroundService)

| Job | Trigger | Purpose |
|---|---|---|
| `UploadCleanupJob` | hourly | soft-delete + blob-delete expired orphan/referenced uploads |
| `GuestSessionCleanupJob` | hourly | purge expired unclaimed guest sessions |
| `AccountDeletionJob` | daily | hard-delete accounts 30 days after deletion request |
| `EmailRetryJob` | 10 s poll | drain `EmailQueue` (3 attempts, 1s/4s/16s backoff) |
| `OrderPhotoPromotionWorker` | channel | move paid orders' photos to cloud (drains on shutdown) |
| `PromotionRecoveryScanner` | boot only | re-derive lost promotion work from `StorageLocation` (ADR-010) |
| `OriginalPurgeRecoveryScanner` | boot + every 6 h | purge missed originals (incl. Cancelled) |
| `ArchiveRetentionJob` | every 6 h | delete cloud previews/thumbs past retention |
| `S3BucketVerifier` | boot only | fail-fast bucket probe (cloud on only) |

Queueing is **in-process** (`Channel<T>` + recovery scans, ADR-010) — no durable queue table;
this blocks multi-VM scale-out by design until the Redis work (bolt 046, deliberately parked).

## Payments

- **Stripe** is the only payment processor (Elements client-side; server creates PaymentIntents
  with gateway-side idempotency keyed by order Id). The webhook is an anonymous endpoint with
  signature verification.
- **Idempotency (bolt 035)**: `Idempotency-Key` header (≤80 chars) → `Orders.IdempotencyKey`,
  globally-unique index, 24 h replay window; replays return the cached client
  secret; divergent replays → 409 with `divergentFields` (ADR-004/005). There is
  no separate idempotency table and **no optimistic-concurrency tokens anywhere** — uniqueness
  violations + retry are the concurrency mechanism.
- Payment success: order → `Paid` (state machine), SignalR `NewOrderReceived`, confirmation
  email, promotion enqueue.

## Email

`IEmailService` → `ReliableEmailService`: try direct send; on failure enqueue an `EmailQueue`
row that `EmailRetryJob` drains. Provider is **required config** (`Email:Provider` =
`Smtp` (MailKit, dev default localhost:1025) or `SendGrid`). Templates are RazorLight
`.cshtml` under `EmailTemplates/`. Order + auth emails are fire-and-forget templated sends.

## Image pipeline

ImageSharp behind `IImageProcessor`: thumbnails max 300 px / previews max 2000 px (JPEG q85,
never upscales previews). Defenses: 100 MP pixel-area cap pre-decode (422
`DecompressionBombException`), `MaxFrames=1`, forced `Rgba32`, ImageSharp allocator backstop
512 MB, and `ImageDecodeLimiter` (concurrency = min(CPU, RAM/512 MB), overridable). Upload
contract: JPG/PNG, 50 MB/file, 500 MB/batch, 100 uploads per guest session (magic-byte MIME
validation; HEIC bytes pass the validator but no decoder exists and the FE rejects them —
JPG/PNG is the real contract).

## Order lifecycle

```text
AwaitingPayment → Paid → Printing → Shipped → Delivered
      │             │        │
      ▼             ▼        ▼
 PaymentFailed   Cancelled  Cancelled   (transitions enforced by OrderStatusMachine → 400)
```

## Error contract

RFC 7807 ProblemDetails from `ExceptionHandlerMiddleware` (typed exception → status map, always
`correlationId`, Romanian user-facing detail) — **except validation**, which returns 422
`{ errors: [{field, message}] }` via the global `ValidationFilter` (ADR-002). State conflicts
are 409, distinct from 422 (ADR-004). Full exception→status map lives in
`Middleware/ExceptionHandlerMiddleware.cs`.

## Configuration pattern

Settings POCOs in `Configuration/` bound per section with validators and **`ValidateOnStart`**
(storage, archive, payments-in-production) — misconfiguration fails the boot, not the first
request. CORS origins are required exact origins (boot-throw if missing). Health endpoint
`/health` always returns HTTP 200 with status in the body (ADR-001); checks: database
connectivity, disk space.
