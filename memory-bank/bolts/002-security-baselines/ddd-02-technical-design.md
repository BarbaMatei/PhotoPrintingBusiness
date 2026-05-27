---
unit: 002-security-baselines
bolt: 002-security-baselines
stage: design
status: complete
updated: 2026-05-19T00:00:00Z
---

# Technical Design - Security Baselines

## Architecture Pattern

**Pattern**: Pipeline Configuration + Middleware Decorator

All four concerns are implemented as ASP.NET Core middleware / pipeline configuration. No controllers, no services with business logic, no database access. The single custom component is `SecurityHeadersMiddleware`; the other three (HTTPS/HSTS, CORS, rate limiting) use built-in ASP.NET Core middleware configured via options.

A `SecurityExtensions.cs` extension class centralises registration and pipeline wiring, keeping `Program.cs` a clean one-liner per concern.

---

## Layer Structure

```text
Program.cs            → IServiceCollection registration + IApplicationBuilder pipeline (call sites only)
Extensions/           → SecurityExtensions.cs (all AddX / UseX wiring)
Configuration/        → CorsSettings, RateLimitSettings, SecurityHeadersOptions (options POCOs)
Middleware/           → SecurityHeadersMiddleware.cs (custom header injection)
```

No changes to Domain, Data, or any other layer.

---

## File Structure

```text
src/PhotoPrint.API/
├── Configuration/
│   ├── CorsSettings.cs              (new)
│   ├── RateLimitSettings.cs         (new)
│   └── SecurityHeadersOptions.cs    (new)
├── Middleware/
│   └── SecurityHeadersMiddleware.cs (new)
└── Extensions/
    └── SecurityExtensions.cs        (new)
```

**`Program.cs` changes** (additions only, no removals):
- Replace bare `app.UseHttpsRedirection()` with `app.UseSecurityBaselines()` (which includes it)
- Add `builder.Services.AddSecurityBaselines(builder.Configuration)`

---

## Configuration Schema

### `appsettings.json` additions

```json
"Cors": {
  "AllowedOrigins": "http://localhost:4200"
},
"RateLimit": {
  "WindowSeconds": 60,
  "Public": { "PermitLimit": 100 },
  "Auth":   { "PermitLimit": 10 }
},
"SecurityHeaders": {
  "ContentSecurityPolicy": "default-src 'self'; script-src 'self' https://js.stripe.com https://accounts.google.com; frame-src https://js.stripe.com; connect-src 'self' https://api.stripe.com; frame-ancestors 'none'; object-src 'none'"
}
```

### `appsettings.Development.json` additions

```json
"Cors": {
  "AllowedOrigins": "http://localhost:4200"
}
```

*(HSTS is suppressed in Development automatically by the environment guard — no config key needed.)*

---

## Options POCOs

### `CorsSettings`
```csharp
public sealed class CorsSettings
{
    public string AllowedOrigins { get; init; } = string.Empty;

    // Splits comma-separated string; filters empty entries
    public string[] GetOrigins() =>
        AllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
```

### `RateLimitSettings`
```csharp
public sealed class RateLimitSettings
{
    public int WindowSeconds { get; init; } = 60;
    public RateLimitWindow Public { get; init; } = new() { PermitLimit = 100 };
    public RateLimitWindow Auth   { get; init; } = new() { PermitLimit = 10 };
}

public sealed class RateLimitWindow
{
    public int PermitLimit { get; init; }
}
```

### `SecurityHeadersOptions`
```csharp
public sealed class SecurityHeadersOptions
{
    public string ContentSecurityPolicy { get; init; } =
        "default-src 'self'; frame-ancestors 'none'; object-src 'none'";
}
```

---

## SecurityHeadersMiddleware Design

```text
Request arrives
      |
[SecurityHeadersMiddleware.InvokeAsync]
      |
      ├── context.Response.OnStarting(callback)  ← Register header write callback
      |      (headers written just before response flushes — preserves status code)
      |
next(context) → rest of pipeline runs
      |
[OnStarting fires]
      ├── X-Content-Type-Options: nosniff
      ├── X-Frame-Options: DENY
      ├── Referrer-Policy: strict-origin-when-cross-origin
      └── Content-Security-Policy: {from options}
```

**Why `OnStarting` callback?** Writing to `Response.Headers` after the pipeline has started would throw if the response has already begun streaming. `OnStarting` ensures headers are added just before the first byte is written, regardless of where in the pipeline the response originates.

**Signature**:
```csharp
public class SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityHeadersOptions> options)
{
    public async Task InvokeAsync(HttpContext context) { ... }
}
```

*Uses primary constructor (C# 12). Not registered as `IMiddleware` — uses convention-based `UseMiddleware<T>()` internally via extension method.*

---

## Rate Limiting Design

**Two-layer approach** — GlobalLimiter + named "auth" limiter:

| Layer | Policy | Scope | Mechanism |
|-------|--------|-------|-----------|
| Global | 100 req/min per IP | All endpoints | `options.GlobalLimiter` via `PartitionedRateLimiter` |
| Named "auth" | 10 req/min per IP | Auth endpoints only | `options.AddFixedWindowLimiter("auth", ...)` |

Auth endpoints are double-limited: GlobalLimiter (100/min) AND named "auth" (10/min). Both limiters must permit the request. In practice, auth endpoints are limited to 10/min (the stricter bound). Non-auth endpoints are limited to 100/min only.

**429 Response**:
```csharp
options.OnRejected = async (ctx, token) =>
{
    ctx.HttpContext.Response.StatusCode = 429;
    ctx.HttpContext.Response.Headers.RetryAfter =
        ((int)ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? retryAfter.TotalSeconds : 60).ToString();
    await ctx.HttpContext.Response.WriteAsync("Too many requests.", token);
};
```

**IP partition key**:
```csharp
httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
```
*(X-Forwarded-For is NOT used here directly — the reverse proxy (nginx/Caddy) is responsible for setting `RemoteIpAddress` via `UseForwardedHeaders`. This is a separate concern added by the reverse proxy configuration, not this bolt.)*

---

## CORS Design

```csharp
services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy
            .WithOrigins(corsSettings.GetOrigins())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();          // Required for HttpOnly refresh-token cookie
    });
});
```

**Invariant enforced at startup**: If `AllowedOrigins` is empty after parsing, throw `InvalidOperationException` — misconfigured CORS is a security risk that should prevent startup.

---

## HSTS Design

```csharp
// Production only — guarded by environment check in UseSecurityBaselines()
services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubdomains = true;
    options.Preload = false;                 // Preload not appropriate for initial deployment
});
```

**Environment guard** in `UseSecurityBaselines`:
```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();
```

---

## Middleware Pipeline (updated `Program.cs` section)

```csharp
// ── Security ──────────────────────────────────────────────────────────────────
builder.Services.AddSecurityBaselines(builder.Configuration);

// ... existing registrations ...

var app = builder.Build();

// ── Middleware Pipeline (ORDER MATTERS) ───────────────────────────────────────
app.UseCorrelationId();           // 1: stamp correlation ID
app.UseGlobalExceptionHandler();  // 2: catch unhandled exceptions
app.UseSerilogRequestLogging();   // 3: structured request log

app.UseSecurityBaselines();       // 4-8: HSTS, HTTPS, SecurityHeaders, CORS, RateLimit

app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapHealthEndpoint();
```

`UseSecurityBaselines()` encapsulates:
```text
4. UseHsts()              (production only)
5. UseHttpsRedirection()  (moves here from previous bare call)
6. UseMiddleware<SecurityHeadersMiddleware>()
7. UseCors(CorsPolicyName)
8. UseRateLimiter()
```

---

## Security Design

| Concern | Approach |
|---------|----------|
| CORS wildcard prevention | Startup throws if origin list is empty or contains `"*"` |
| API key / secret exposure | None — no secrets in this bolt |
| CSP injection | CSP value comes from `IOptions<SecurityHeadersOptions>` bound from config, not user input |
| Rate limit bypass via IP spoofing | Out of scope for MVP; `X-Forwarded-For` trusted only after `UseForwardedHeaders` (reverse proxy layer) |
| HSTS in dev | Hard-guarded by `IWebHostEnvironment.IsDevelopment()` — never mistakenly applied |

---

## NFR Implementation

| Requirement | Approach |
|-------------|---------|
| Performance | `SecurityHeadersMiddleware` uses `OnStarting` callback (zero allocation on happy path); rate limiting is in-memory (no DB) |
| Observability | `OnRejected` callback logs rate limit events via Serilog; CORS failures logged by framework at Debug level |
| Configurability | All limits and origins are config-driven — no recompile needed |
| Testability | Middleware unit-testable via `DefaultHttpContext`; rate limits testable via `WebApplicationFactory` |

---

## Integration Test Plan (Stage 5)

| Test | Method |
|------|--------|
| CORS allowed origin → 200 with `Access-Control-Allow-Origin` | `WebApplicationFactory` + preflight OPTIONS |
| CORS disallowed origin → no CORS headers | Same factory, different origin header |
| Security headers present on any response | Assert headers on `/health` response |
| Rate limit exceeded (auth) → 429 + Retry-After | Send 11 requests to auth endpoint in test |
| HSTS header present in non-Development | Override environment in test factory |
