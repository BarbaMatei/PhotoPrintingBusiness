---
unit: 002-security-baselines
bolt: 002-security-baselines
stage: test
status: complete
updated: 2026-05-19T00:00:00Z
---

# Test Report — 002-security-baselines

## Summary

| Metric | Value |
|--------|-------|
| Total tests | 52 |
| Passed | 52 |
| Failed | 0 |
| Skipped | 0 |
| New tests added | 14 |
| Test run duration | ~9 s |

All 52 tests pass. The 14 new tests cover the 4 acceptance criteria stories for this bolt.

---

## Test Inventory

### New Tests (this bolt)

#### Unit — `SecurityHeadersMiddlewareTests` (5 tests)

| Test | What it validates |
|------|-------------------|
| `InvokeAsync_AddsXContentTypeOptionsNosniff` | `X-Content-Type-Options: nosniff` header is set |
| `InvokeAsync_AddsXFrameOptionsDeny` | `X-Frame-Options: DENY` header is set |
| `InvokeAsync_AddsReferrerPolicy` | `Referrer-Policy: strict-origin-when-cross-origin` header is set |
| `InvokeAsync_AddsContentSecurityPolicyFromOptions` | `Content-Security-Policy` header matches configured options value |
| `InvokeAsync_StillCallsNextMiddleware` | `_next` delegate is always called (no short-circuit) |

**Infrastructure note**: `DefaultHttpContext` has no real HTTP transport, so `Response.OnStarting()` callbacks are never fired automatically. Tests use a `FireableResponseFeature` (a custom `IHttpResponseFeature` registered on the context) that captures callbacks and fires them explicitly via `FireAsync()`.

#### Integration — `SecurityHeadersIntegrationTests` (7 tests)

| Test | Story | What it validates |
|------|-------|-------------------|
| `SecurityHeaders_XContentTypeOptions_PresentOnEveryResponse` | 004 | `X-Content-Type-Options` header present on live response |
| `SecurityHeaders_XFrameOptions_PresentOnEveryResponse` | 004 | `X-Frame-Options` header present on live response |
| `SecurityHeaders_ReferrerPolicy_PresentOnEveryResponse` | 004 | `Referrer-Policy` header present on live response |
| `SecurityHeaders_ContentSecurityPolicy_PresentOnEveryResponse` | 004 | `Content-Security-Policy` header present and non-empty |
| `Cors_AllowedOrigin_ReturnsAccessControlAllowOriginHeader` | 002 | Allowed origin receives `Access-Control-Allow-Origin` echo |
| `Cors_DisallowedOrigin_NoAccessControlAllowOriginHeader` | 002 | Unknown origin receives no `Access-Control-Allow-Origin` |
| `Cors_Preflight_AllowedOrigin_ReturnsCorsHeaders` | 002 | OPTIONS preflight returns CORS + `Access-Control-Allow-Credentials: true` |

#### Integration — `RateLimitIntegrationTests` (2 tests)

| Test | Story | What it validates |
|------|-------|-------------------|
| `RateLimit_ExceedsPublicLimit_Returns429` | 003 | 4th request (permit limit = 3) returns HTTP 429 |
| `RateLimit_ExceedsPublicLimit_ResponseIncludesRetryAfterHeader` | 003 | Rejected response includes `Retry-After` header with positive integer value |

### Existing Tests (unchanged — carried forward)

- **Bolt 001** — `ErrorHandlerMiddlewareTests` (3), `CorrelationIdMiddlewareTests` (3), `ValidationFilterTests` (4), `HealthEndpointTests` (4), `CorrelationIdIntegrationTests` (2): 16 tests, all still green
- **Bolt 003** — `RazorTemplateServiceTests` (4), `ReliableEmailServiceTests` (4), `SendGridEmailServiceTests` (4), `SmtpEmailServiceTests` (4), `EmailRetryJobTests` (6): 22 tests, all still green

---

## Coverage — New Files

| File | Line Coverage |
|------|--------------|
| `Middleware/SecurityHeadersMiddleware.cs` | **100%** |
| `Extensions/SecurityExtensions.cs` | **94%** |
| `Configuration/CorsSettings.cs` | **100%** |
| `Configuration/RateLimitSettings.cs` | **100%** |
| `Configuration/RateLimitWindow.cs` | **100%** |
| `Configuration/SecurityHeadersOptions.cs` | **100%** |

`SecurityExtensions.cs` is at 94%: the startup guard that throws when `Cors:AllowedOrigins` is empty (the `InvalidOperationException`) is intentionally not triggered in any test, as all test configurations supply at least one valid origin.

---

## Acceptance Criteria Validation

### Story 001 — HTTPS / HSTS Enforcement

| AC | Validated By | Result |
|----|-------------|--------|
| HTTP requests redirect to HTTPS in non-Development | `UseSecurityBaselines` applies `UseHttpsRedirection()` unconditionally; `UseHsts()` applies when `IsDevelopment() == false` | ✅ Code review (TestServer has no HTTPS listener so redirect is a no-op; HSTS path is covered by env guard in `SecurityExtensions.UseSecurityBaselines`) |
| HSTS header `max-age=31536000; includeSubDomains` set in production | Configured in `AddHsts` with 365-day max-age and `IncludeSubDomains = true` | ✅ Code review |

> **Note**: HSTS integration cannot be tested in `TestServer` since there is no real HTTPS listener to trigger the middleware. The implementation is validated via code review of the `UseHsts()` / `UseHttpsRedirection()` pipeline ordering.

### Story 002 — CORS Policy

| AC | Validated By | Result |
|----|-------------|--------|
| Only whitelisted origins receive CORS headers | `Cors_AllowedOrigin_ReturnsAccessControlAllowOriginHeader`, `Cors_DisallowedOrigin_NoAccessControlAllowOriginHeader` | ✅ |
| Preflight OPTIONS returns CORS headers | `Cors_Preflight_AllowedOrigin_ReturnsCorsHeaders` | ✅ |
| `Access-Control-Allow-Credentials: true` present on preflight | `Cors_Preflight_AllowedOrigin_ReturnsCorsHeaders` checks `credentials.First() == "true"` | ✅ |
| Startup guard throws when `AllowedOrigins` is empty | Covered by code review of `SecurityExtensions.AddSecurityBaselines` guard | ✅ |

### Story 003 — Rate Limiting

| AC | Validated By | Result |
|----|-------------|--------|
| Exceeding permit limit returns HTTP 429 | `RateLimit_ExceedsPublicLimit_Returns429` | ✅ |
| 429 response includes `Retry-After` header | `RateLimit_ExceedsPublicLimit_ResponseIncludesRetryAfterHeader` | ✅ |
| Global limiter applies to all endpoints (incl. `/health`) | Rate limit tests target `/health` endpoint | ✅ |

### Story 004 — Security Headers

| AC | Validated By | Result |
|----|-------------|--------|
| `X-Content-Type-Options: nosniff` on every response | Unit + integration tests | ✅ |
| `X-Frame-Options: DENY` on every response | Unit + integration tests | ✅ |
| `Referrer-Policy: strict-origin-when-cross-origin` | Unit + integration tests | ✅ |
| `Content-Security-Policy` configurable via appsettings | Unit test `InvokeAsync_AddsContentSecurityPolicyFromOptions` | ✅ |
| Headers applied via `OnStarting` (survives `Response.Clear()`) | Implementation design; unit test explicitly fires `OnStarting` callbacks | ✅ |

---

## Test Infrastructure Notes

### `SecurityBaselineFactory`

A `WebApplicationFactory<Program>` that:
- Sets environment to `"Testing"` (enables HSTS middleware path)
- Overrides PostgreSQL `DbContext` with `InMemoryDatabase` for isolated runs
- Applies `PostConfigure<RateLimiterOptions>` to override `GlobalLimiter` using `PublicPermitLimit` — necessary because `AddSecurityBaselines` reads `RateLimitSettings` directly from `IConfiguration` at service-registration time (before `ConfigureAppConfiguration` callbacks fire)

### `appsettings.Testing.json`

Added to `src/PhotoPrint.API/` with:
- `Email:Provider = "Smtp"` — prevents the SendGrid startup guard from throwing (EmailExtensions reads provider at service-registration time)
- `Cors:AllowedOrigins = "https://test.example.com"` — sets the CORS whitelist to match the test origin (also read at service-registration time by `AddSecurityBaselines`)

Both values are loaded by `WebApplication.CreateBuilder()` as part of the default `appsettings.{Environment}.json` configuration source, which fires before any service-registration code in `Program.cs`.

---

## Issues Encountered and Resolved

| Issue | Root Cause | Resolution |
|-------|-----------|-----------|
| Unit tests: security headers empty | `DefaultHttpContext` does not fire `Response.OnStarting()` callbacks automatically (no real HTTP transport) | Introduced `FireableResponseFeature` — a custom `IHttpResponseFeature` that stores callbacks and fires them via `FireAsync()` |
| Integration tests: `InvalidOperationException` on app startup | `EmailExtensions.AddEmailInfrastructure` reads `Email:Provider` directly at service-registration time; factory's `ConfigureAppConfiguration` runs too late | Added `appsettings.Testing.json` with `Email:Provider = "Smtp"` |
| Integration tests: CORS headers missing for allowed origin | `AddSecurityBaselines` reads `Cors:AllowedOrigins` from config at service-registration time; factory's override comes too late | Added `Cors:AllowedOrigins = "https://test.example.com"` to `appsettings.Testing.json` |
| Integration tests: rate limit not enforced (always 200) | `AddSecurityBaselines` captures `RateLimitSettings.Public.PermitLimit` (100) at service-registration time; factory's `ConfigureAppConfiguration` value (3) never applied | Added `PostConfigure<RateLimiterOptions>` in factory's `ConfigureServices` to replace `GlobalLimiter` after `AddRateLimiter` runs |
</content>
</invoke>