---
stage: design
bolt: 036-sameday-api-client
created: 2026-06-02T09:35:00Z
---

# Stage 2 — Technical Design: Sameday API Client

## Architecture Pattern

**Pattern**: Anti-corruption layer over a typed `HttpClient`, fronted by a
single-token caching domain service, wired via `IHttpClientFactory` +
Polly delegating handlers.

**Rationale**:

- `IHttpClientFactory` is the project standard for outbound HTTP
  (tech-stack: ASP.NET Core 8). It gives socket-pool management and
  per-client policy registration for free.
- A *typed* client (`SamedayClient`) lets the rest of the system depend
  on a domain-shaped interface (`ISamedayClient`) without ever seeing
  `HttpResponseMessage`. This is the anti-corruption seam.
- Token lifecycle (cache, refresh, 401-retry) is the kind of
  cross-cutting concern that belongs in *one* domain service
  (`SamedayTokenProvider`), not sprinkled inside Polly retry callbacks.
  Keeping it explicit makes the `SemaphoreSlim` thundering-herd guard
  testable.
- Conditional registration (`AddSingleton<IShippingService,
  SamedayShippingService>()` only when `Sameday:Enabled=true`)
  preserves the today-behaviour escape hatch demanded by FR-1 — flip
  the flag back to `false` and the system reverts byte-for-byte.

**Alternatives considered (will recur in Stage 3 ADR Analysis)**:

- A monolithic `SamedayClient` that owns its own `HttpClient`: rejected
  — bypasses the project's `IHttpClientFactory` standard, complicates
  socket reuse.
- A `DelegatingHandler` that attaches the token via
  `IHttpContextAccessor`-style ambient context: rejected — couples the
  auth lifecycle to the request pipeline and makes background-job
  callers (bolt 037) awkward to wire.
- A separate `Refit` client interface: rejected — adds a dependency
  for the sake of three endpoints; vendor JSON is irregular enough
  that hand-rolled DTOs are clearer.

---

## Layer Structure

```text
┌──────────────────────────────────────────────────────────────────┐
│  Presentation                                                    │
│   (no new controllers — feature-flag visibility via /health)     │
├──────────────────────────────────────────────────────────────────┤
│  Application                                                     │
│   SamedayShippingService : IShippingService                      │
│     → delegates AWB creation to ISamedayClient                   │
│     → registered only when Sameday:Enabled = true                │
├──────────────────────────────────────────────────────────────────┤
│  Domain                                                          │
│   SamedayToken, SamedayCredentials, AwbCreationResult,           │
│   TrackingSnapshot, TrackingState, SamedayException family       │
├──────────────────────────────────────────────────────────────────┤
│  Infrastructure                                                  │
│   ISamedayTokenProvider / SamedayTokenProvider                   │
│   ISamedayClient / SamedayClient (typed HttpClient)              │
│   SamedayAuthHandler (DelegatingHandler — bearer token)          │
│   SamedayPolicies (Polly: rate-limit + retry)                    │
│   PhotoPrintDbContext + Order entity (extended — 2 new columns)  │
│   EF migration: 20260602_AddSamedayOrderFields                   │
└──────────────────────────────────────────────────────────────────┘
```

**Responsibility split**:

- **Application** — picks which `IShippingService` to register based on
  config. `SamedayShippingService` in this bolt is thin: it forwards
  `GenerateAwbAsync` to `ISamedayClient.CreateAwbAsync`. The
  outer "Paid → enqueue AWB job" workflow lands in bolt 037.
- **Domain** — pure data + invariants (no I/O). Reusable from tests
  without any DI container.
- **Infrastructure** — owns every line that touches the network or
  the database.

---

## Component Design

### `SamedayTokenProvider` (singleton)

```text
+-------------------------------------------------------------+
|  ISamedayTokenProvider                                      |
|  - Task<SamedayToken> GetTokenAsync(CancellationToken ct)   |
|  - void Invalidate()                                        |
+-------------------------------------------------------------+
                            ▲
                            │ implements
                            │
+-------------------------------------------------------------+
|  SamedayTokenProvider                                       |
|  - SamedayToken? _current                                   |
|  - SemaphoreSlim _gate = new(1, 1)                          |
|  - ISamedayAuthenticator _authenticator (= SamedayClient)   |
|  - IOptions<SamedaySettings> _settings                      |
|  - ILogger<SamedayTokenProvider> _logger                    |
|  - TimeProvider _clock (System.TimeProvider)                |
|                                                             |
|  + GetTokenAsync                                            |
|      if _current is non-null AND _current.IsValid(now,60s)  |
|         return _current                                     |
|      else await _gate.WaitAsync                             |
|         re-check inside lock                                |
|         _current = await _authenticator.AuthenticateAsync   |
|         return _current                                     |
|      finally _gate.Release                                  |
|                                                             |
|  + Invalidate                                               |
|      _current = null  (called by SamedayAuthHandler on 401) |
+-------------------------------------------------------------+
```

**Why `TimeProvider`**: lets the token-expiry tests freeze the clock
without `Thread.Sleep`. Project standard since .NET 8.

### `SamedayAuthHandler : DelegatingHandler`

Attaches the bearer token to every outbound request *except*
`/api/authenticate` itself, and implements the
"401 → invalidate → retry once" rule:

```text
SendAsync(req, ct):
  if req.Path == "/api/authenticate":
    return await base.SendAsync(req, ct)        // no bearer

  token = await _tokenProvider.GetTokenAsync(ct)
  req.Headers.Authorization = Bearer(token.Value)
  res = await base.SendAsync(req, ct)

  if res.StatusCode == 401:
    res.Dispose()
    _tokenProvider.Invalidate()
    freshToken = await _tokenProvider.GetTokenAsync(ct)
    // clone the request — original is already consumed
    retryReq = await CloneAsync(req)
    retryReq.Headers.Authorization = Bearer(freshToken.Value)
    retryRes = await base.SendAsync(retryReq, ct)
    if retryRes.StatusCode == 401:
      throw new SamedayAuthException(endpoint: req.RequestUri.AbsolutePath)
    return retryRes

  return res
```

Notes:

- `CloneAsync` only deep-copies what we need: method, URI, headers,
  and the buffered body (we use `JsonContent` on the way out, so a
  body buffer always exists for retry).
- A retry-once is a separate concern from Polly's 5xx retry policy;
  401 is *not* in Polly's retryable set. This handler runs *inside*
  Polly's retry — i.e. Polly retries the *whole* clone-and-retry
  sequence if the second leg returns a `5xx` (within its 3-attempt
  budget).

### `SamedayClient : ISamedayClient`

Owns the `HttpClient` (injected by `IHttpClientFactory`'s typed-client
machinery). Wire shape:

```text
+------------------------+   IHttpClientFactory   +-----------+
| ISamedayClient         |◀───────────────────────|  Caller   |
| (typed-client iface)   |                        |  (DI)     |
+------------------------+                        +-----------+
            │
            │ HttpClient configured with:
            │   - BaseAddress = SamedaySettings.BaseUrl
            │   - Timeout     = RequestTimeoutSeconds
            │   - Default JSON serializer (camelCase)
            │
            ▼
   ┌───────────────────────────────────────────┐
   │  Pipeline (outer → inner):                │
   │    SamedayAuthHandler                     │
   │    Polly RateLimitPolicy (5 req/s)        │
   │    Polly RetryPolicy (3x exp 1/4/16s)     │
   │    PrimaryHandler (HttpClientHandler)     │
   └───────────────────────────────────────────┘
```

Operation implementations declared in 036 (with `AuthenticateAsync`
the only one fully implemented in this bolt):

- `AuthenticateAsync(credentials, ct)`
  → POST `/api/authenticate` with header
  `Authorization: Basic base64(user:pass)` (per Sameday docs) *or*
  body `{ "remember_me": false }` — exact shape pinned in the test
  fixture (Stage 5).
  → Parse `{ token, expire_at_utc }` → `new SamedayToken(...)`.
  → On 4xx → `SamedayAuthException`; on 5xx → Polly handles it; on
  invalid payload → `SamedayProtocolException`.

- `CreateAwbAsync`, `GetLabelPdfAsync`, `GetTrackingAsync` — stubs
  that compile but throw `NotImplementedException("Implemented in
  bolt 037-awb-and-tracking-jobs.")`. This keeps the *interface*
  stable so bolt 037 can graft the workflow on without touching
  the DI graph.

### `SamedayShippingService : IShippingService` (Application layer)

```text
class SamedayShippingService : IShippingService
{
    ctor: ISamedayClient client, ILogger logger;

    Task<AwbResult> GenerateAwbAsync(Order order, CancellationToken ct)
    {
        // Bolt 037 fills in the real Order → AwbCreationRequest mapping.
        // In bolt 036, this method exists so DI compiles when the flag
        // is on, but it delegates and surfaces NotImplementedException
        // until bolt 037 ships.
        throw new NotImplementedException(
            "AWB workflow is implemented in bolt 037.");
    }
}
```

Conditional DI registration (in `Program.cs`):

```csharp
services.AddOptions<SamedaySettings>()
    .Bind(config.GetSection(SamedaySettings.SectionName))
    .ValidateDataAnnotations()     // for the simple non-empty rules
    .ValidateOnStart();            // boot fails fast on bad config

services.AddSingleton<TimeProvider>(_ => TimeProvider.System);
services.AddSingleton<ISamedayTokenProvider, SamedayTokenProvider>();
services.AddTransient<SamedayAuthHandler>();

services.AddHttpClient<ISamedayClient, SamedayClient>((sp, http) =>
{
    var s = sp.GetRequiredService<IOptions<SamedaySettings>>().Value;
    http.BaseAddress = new Uri(s.BaseUrl);
    http.Timeout     = TimeSpan.FromSeconds(s.RequestTimeoutSeconds);
})
.AddHttpMessageHandler<SamedayAuthHandler>()
.AddPolicyHandler(SamedayPolicies.RateLimit(maxPerSecond: 5))
.AddPolicyHandler(SamedayPolicies.Retry(attempts: 3));

var samedaySection = config.GetSection(SamedaySettings.SectionName);
if (samedaySection.GetValue<bool>("Enabled"))
{
    services.AddSingleton<IShippingService, SamedayShippingService>();
}
else
{
    // existing line — left untouched:
    // services.AddSingleton<IShippingService, StaticShippingService>();
}
```

Last-registration-wins is a known footgun. We use the
`if/else` branch above rather than registering both and trying to
override; this keeps DI behavior identical regardless of `appsettings`
load order.

---

## API Design

### Outbound (we → Sameday)

This bolt only *fully implements* one outbound endpoint. The others
are declared on the interface so bolt 037 can wire workflows without
re-touching DI.

| Endpoint | Method | Implemented in 036? | Notes |
|---|---|---|---|
| `/api/authenticate` | POST | ✅ Yes | Body or Basic header per fixture; returns `{token, expire_at_utc}`. |
| `/api/awb` | POST | ⚠ Stub | Returns `AwbCreationResult`; called by bolt 037's job. |
| `/api/awb/{number}/label` | GET | ⚠ Stub | Returns PDF bytes; used by admin download in 037. |
| `/api/awb/{number}/tracking` | GET | ⚠ Stub | Returns `TrackingSnapshot`; used by tracking job in 037. |

"Stub" = interface present, method throws `NotImplementedException`
until 037. This avoids a churn-causing follow-up where bolt 037 would
otherwise have to re-touch `ISamedayClient`.

### Inbound (clients → us)

**No new HTTP endpoints in this bolt.** The only externally visible
surface change is `/health`:

- The existing `/health` endpoint already returns `200 OK` with a
  `status` body (per ADR-001).
- NOT IMPLEMENTED: an earlier design added a `"sameday": "enabled"` field to the
  response when `Sameday:Enabled == true`. The generic health-response writer never
  carried it and it was dropped as out of scope (the flag is observable from config /
  the resolved `IShippingService`). No active Sameday probing happens here either way.

---

## Data Model

### Schema additions

Two columns added to `Orders`:

| Column | Type (Postgres) | Type (SQLite — dev) | Nullable | Notes |
|---|---|---|---|---|
| `AwbLabelUrl` | `varchar(500)` | `TEXT` (max-length 500 enforced by EF) | yes | URL to Sameday-hosted PDF label. |
| `LastTrackingSyncAt` | `timestamptz` | `TEXT` (ISO-8601 UTC, EF default) | yes | UTC timestamp of last successful tracking poll. |

EF Core configuration (`OrderConfiguration`):

```csharp
builder.Property(o => o.AwbLabelUrl).HasMaxLength(500).IsRequired(false);
builder.Property(o => o.LastTrackingSyncAt).IsRequired(false);
```

### Migration

`Migrations/20260602_AddSamedayOrderFields.cs`:

```csharp
migrationBuilder.AddColumn<string>(
    name: "AwbLabelUrl",
    table: "Orders",
    type: "varchar(500)",
    maxLength: 500,
    nullable: true);

migrationBuilder.AddColumn<DateTimeOffset>(
    name: "LastTrackingSyncAt",
    table: "Orders",
    type: "timestamp with time zone",
    nullable: true);
```

**Down** is a `DropColumn` for both — no data loss concern: both
columns are nullable from day one and the AWB workflow is gated
behind a flag.

**Cross-provider compatibility**:

- Npgsql: `varchar(500)` and `timestamp with time zone` map cleanly.
- SQLite (dev/test): EF translates both to `TEXT`. Lossless for our
  read/write patterns.

### `Order` entity (extension)

```csharp
public class Order
{
    // ... existing properties ...

    public string?         AwbLabelUrl        { get; set; }
    public DateTimeOffset? LastTrackingSyncAt { get; set; }
}
```

Setters intentionally `public` (matches the rest of the entity). The
"writes only via domain services" rule is preserved by *convention*
plus tests; we don't introduce a fresh private-setter pattern just
for these two fields.

### Persistence rules

- `AwbLabelUrl` written exactly once (when Sameday returns the AWB
  number alongside the label URL — both fields persist in the same
  `SaveChangesAsync` call in bolt 037).
- `LastTrackingSyncAt` overwritten on each successful poll. The
  monotonic-non-decreasing invariant from Stage 1 is enforced by the
  tracking job (bolt 037) treating "this poll's `ObservedAt` < the
  stored value" as a programming error, not by a DB constraint.

---

## Configuration

`SamedaySettings.cs` (Configuration/):

```csharp
public sealed class SamedaySettings
{
    public const string SectionName = "Sameday";

    public bool   Enabled               { get; set; }
    public string BaseUrl               { get; set; } = "https://api.sameday.ro";
    public string Username              { get; set; } = string.Empty;
    public string Password              { get; set; } = string.Empty;
    public string PickupPointId         { get; set; } = string.Empty;
    public int    RequestTimeoutSeconds { get; set; } = 10;
}
```

Validator (FluentValidation, per ADR-002 — no data annotations for
"real" rules; the `ValidateDataAnnotations` call above is a belt for
non-empty-when-enabled):

```csharp
public sealed class SamedaySettingsValidator : AbstractValidator<SamedaySettings>
{
    public SamedaySettingsValidator()
    {
        When(s => s.Enabled, () =>
        {
            RuleFor(s => s.BaseUrl).NotEmpty().Must(BeAnAbsoluteHttpUri);
            RuleFor(s => s.Username).NotEmpty();
            RuleFor(s => s.Password).NotEmpty();
            RuleFor(s => s.PickupPointId).NotEmpty();
            RuleFor(s => s.RequestTimeoutSeconds).InclusiveBetween(1, 60);
        });
    }

    private static bool BeAnAbsoluteHttpUri(string raw)
        => Uri.TryCreate(raw, UriKind.Absolute, out var u)
        && (u.Scheme == "https" || u.Scheme == "http");
}
```

`ValidateOnStart()` wires the validator so the host **fails to boot**
if `Sameday:Enabled=true` and any credential is missing — exactly the
story's "MUST fail at startup" criterion.

### `appsettings.json` defaults (default OFF)

```json
{
  "Sameday": {
    "Enabled":               false,
    "BaseUrl":               "https://api.sameday.ro",
    "Username":              "",
    "Password":              "",
    "PickupPointId":         "",
    "RequestTimeoutSeconds": 10
  }
}
```

### `appsettings.Development.json` (sandbox URL)

```json
{
  "Sameday": {
    "Enabled":               false,
    "BaseUrl":               "https://sameday-api.demo.sameday.ro",
    "RequestTimeoutSeconds": 10
  }
}
```

(Credentials in dev live in `dotnet user-secrets`, not in source.)

---

## Polly Policies

`SamedayPolicies.cs` (Services/Sameday/):

```csharp
public static class SamedayPolicies
{
    // Rate-limit: 5 req/s ceiling — well below Sameday's documented
    // ~10 req/s. Uses the System.Threading.RateLimiting bridge so the
    // semantic is "burst then throttle", not "evenly spaced".
    public static IAsyncPolicy<HttpResponseMessage> RateLimit(int maxPerSecond) =>
        Policy.RateLimitAsync<HttpResponseMessage>(
            numberOfExecutions: maxPerSecond,
            perTimeSpan: TimeSpan.FromSeconds(1));

    // Retry: 3 attempts, exponential 1 / 4 / 16 s.
    // Retry only:
    //   - HttpRequestException (DNS/TCP)
    //   - 5xx
    //   - 408 RequestTimeout
    //   - 429 TooManyRequests
    // Do NOT retry on 401 — SamedayAuthHandler owns that.
    public static IAsyncPolicy<HttpResponseMessage> Retry(int attempts) =>
        HttpPolicyExtensions
            .HandleTransientHttpError()          // 5xx + 408
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: attempts,
                sleepDurationProvider: i => TimeSpan.FromSeconds(Math.Pow(4, i - 1)));
            // i=1 → 1 s, i=2 → 4 s, i=3 → 16 s
}
```

Order of handlers (outer-most first): `SamedayAuthHandler` →
`RateLimit` → `Retry` → primary handler. Rationale: the auth handler
must see the *first* 401 (before retry/backoff turns it into a
sleep), and rate-limiting should clamp our *attempts*, not just our
successes.

---

## Security Design

Three concerns. All three are *enforced* in code, not just documented.

1. **Credential persistence.**
   - Dev: `dotnet user-secrets` (already the project convention since
     intent 018 / ADR-006).
   - Staging / prod: environment variables (`Sameday__Username`,
     `Sameday__Password`). Bound by the standard
     `AddEnvironmentVariables()` line in `Program.cs`.
   - **Forbidden**: `appsettings.json` for non-empty credentials.
     CI's gitleaks scan (per ADR-006) covers accidental commits.

2. **Bearer-token & credential leakage in logs.**
   - `SamedayAuthHandler` calls a small `LogRedactor` helper before
     emitting `LogDebug("Sameday {Method} {Path} → {Status}", …)`.
     `Authorization` header value is replaced with the literal
     string `"Bearer ***"`.
   - `SamedayCredentials` overrides `ToString()` to return
     `"SamedayCredentials(Username=***, Password=***)"`. Defence in
     depth in case a stray `_logger.LogX("creds={Creds}", creds)`
     slips into a future PR.
   - `SamedayToken.Value` is **never** logged. The `SamedayToken`
     record overrides `ToString()` to return only
     `"SamedayToken(ExpiresAt={ExpiresAt:o})"`.
   - The `appsettings.json` template never holds real values.

3. **Replay / cross-tenant.**
   - Out of scope here — Sameday's API is single-tenant per
     `PickupPointId`; there is no cross-tenant surface to defend.

---

## Non-Functional Design

| NFR (from `requirements.md`) | Design choice |
|---|---|
| Sameday call latency p95 < 5 s | `RequestTimeoutSeconds = 10` ceiling; Polly retry budget is independent of this and adds up to 21 s wall-clock in the worst (1 + 4 + 16) case — caller is a background job, not an inbound request, so this is acceptable. |
| Rate-limit ceiling ≤ 5 req/s | Polly `RateLimit(5)` (above). Bolt 037's tracking job will additionally schedule itself so concurrent calls stay under this. |
| No credential plaintext in logs | `LogRedactor` + record `ToString` overrides (above). |
| Token cache singleton, no cross-instance sharing | `AddSingleton<ISamedayTokenProvider, …>()`. Cross-instance share deferred to intent 021. |
| AWB success rate ≥ 98% (intent goal) | Achieved by 037's retry job; this bolt's contribution is the retry-once on 401 and the Polly retry on 5xx. |
| Tests must be hermetic | `RichardSzalay.MockHttp` (or equivalent) for `SamedayClient`; recorded fixtures captured by hand during initial sandbox calls. |
| Token expiry tests must be deterministic | `TimeProvider` injection into `SamedayTokenProvider` lets tests run with `FakeTimeProvider`. |

---

## Integration Points

- **Health check** — passive flag only (see API Design above). No
  cron-style probe.
- **Correlation ID** — `SamedayAuthHandler` reads
  `X-Correlation-Id` from `IHttpContextAccessor` *when present* and
  attaches it to outbound requests as `X-Client-Correlation-Id`.
  When invoked from a background job (no HTTP context), it generates
  a fresh GUID. Per ADR-003.
- **Exception → ProblemDetails mapping** — done by the existing
  `ExceptionHandlerMiddleware`. We register `SamedayAuthException`,
  `SamedayProtocolException`, `SamedayValidationException` as `500`
  (server-side fault from the customer's perspective) with the
  existing redaction; `SamedayUnreachableException` from background
  callers never reaches the middleware (it's caught by 037's job
  loop). No middleware changes in this bolt.

---

## Project Structure

```text
src/PhotoPrint.API/
  Configuration/
    SamedaySettings.cs
  Validators/
    SamedaySettingsValidator.cs
  Services/
    Sameday/
      ISamedayClient.cs
      SamedayClient.cs
      ISamedayTokenProvider.cs
      SamedayTokenProvider.cs
      SamedayAuthHandler.cs
      SamedayPolicies.cs
      SamedayWireDtos.cs                  ← internal; vendor JSON shapes
      LogRedactor.cs                      ← internal helper
    SamedayShippingService.cs             ← lives next to existing
                                            StaticShippingService.cs
  Exceptions/
    SamedayException.cs                   ← abstract base
    SamedayAuthException.cs
    SamedayUnreachableException.cs
    SamedayProtocolException.cs
    SamedayValidationException.cs
  Models/
    Order.cs                              ← two new properties
  Data/Configurations/
    OrderConfiguration.cs                 ← two new EF mappings
  Migrations/
    20260602_AddSamedayOrderFields.cs
  Program.cs                              ← conditional DI

src/PhotoPrint.Tests/
  Unit/Services/Sameday/
    SamedayTokenProviderTests.cs
    SamedayAuthHandlerTests.cs
    SamedayClientTests.cs                 ← AuthenticateAsync only
    SamedayPoliciesTests.cs
    SamedaySettingsValidatorTests.cs
  Integration/Sameday/
    SamedaySchemaMigrationTests.cs        ← migration applies cleanly
```

---

## Completion Criteria

- [x] Architecture pattern selected and rationale recorded (typed
      `HttpClient` + ACL + `SemaphoreSlim`-guarded token cache).
- [x] All layers designed with responsibilities.
- [x] Outbound API contracts mapped (`AuthenticateAsync` fully; the
      rest declared for bolt 037).
- [x] Schema additions designed (two nullable columns on `Orders`)
      with cross-provider notes (Postgres / SQLite).
- [x] NFRs addressed (latency, rate-limit, log redaction, token
      cache lifetime, hermetic tests via `TimeProvider` +
      MockHttp).
- [x] Security patterns applied (secrets via env / user-secrets,
      `LogRedactor` + record `ToString` overrides, never log
      bearer or password).

---

## ⛔ Human Checkpoint

Stage 2 (Technical Design) is drafted. Please review and approve
before I move to Stage 3 (ADR Analysis).

**Ready to proceed?**

- **1** — Approve and continue to Stage 3.
- **2** — Need changes (specify which section).

---

## Stage 4 Correction (2026-06-02)

Two design choices were adjusted during implementation. Captured here
so the design doc remains the source of truth for what was actually
built.

### Correction 1 — Polly v8 ResiliencePipeline instead of v7 `Policy`-style API

Stage 2 specified Polly v7-style policy syntax
(`Policy.RateLimitAsync<HttpResponseMessage>`, `WaitAndRetryAsync`,
`HttpPolicyExtensions.HandleTransientHttpError()`,
`AddPolicyHandler`). This conflicts with the project's existing
pattern — `S3StorageService.cs` (bolt 043) already uses Polly v8's
`ResiliencePipeline` + `ResiliencePipelineBuilder` + `RetryStrategyOptions`,
which is the only Polly API surface installed (Polly 8.5.0,
no `Microsoft.Extensions.Http.Polly`).

**As built**:

- `SamedayPolicies.BuildRetryPipeline()` returns a
  `ResiliencePipeline<HttpResponseMessage>` configured with
  `RetryStrategyOptions<HttpResponseMessage>` (3 attempts, exponential
  backoff with `Delay = 1s` and `BackoffType.Exponential` → 1s/4s/16s).
- `SamedayResilienceHandler : DelegatingHandler` owns one
  pipeline-per-handler-instance and executes
  `base.SendAsync(request, ct)` inside `_pipeline.ExecuteAsync(...)`.
- Wired via `AddHttpMessageHandler<SamedayResilienceHandler>()` on
  the typed-client registration — no new NuGet dependency needed.

Pipeline order is unchanged from the design: `SamedayAuthHandler` →
`SamedayResilienceHandler` → primary. 401 stays out of the retry
strategy (ADR-014).

### Correction 2 — Rate-limit (5 req/s) deferred to bolt 037

Stage 2 specified `RateLimit(5)` as part of the resilience pipeline.
The implementing rationale was "well below Sameday's 10 req/s
ceiling, protects against concurrent jobs collectively exceeding it."

In bolt 036, the only outbound call is
`SamedayClient.AuthenticateAsync`, and that call is *already*
serialized by `SamedayTokenProvider`'s `SemaphoreSlim(1, 1)`. Adding
a rate-limit policy on top would have no behavioural effect for any
call site that exists in this bolt — and the Polly v8 rate-limit
strategy lives in a separate `Polly.RateLimiting` NuGet package that
the project does not yet take.

**As built**: only retry. The 5 req/s ceiling will be wired in bolt
037 when high-frequency callers (the tracking-poll job, parallel AWB
creation) actually exist. At that point `Polly.RateLimiting` is the
right package to add — the chokepoint is `SamedayPolicies` and the
wire point is a single line in `SamedayResilienceHandler`'s pipeline
construction.

This is a scope reduction, not a regression: nothing in this bolt
issues enough Sameday calls to need rate-limiting yet.

### Correction 3 — Options validator pattern (`IValidateOptions<T>`, not FluentValidation)

Stage 2 specified a `SamedaySettingsValidator : AbstractValidator<SamedaySettings>`
(FluentValidation). On review, the project's convention for
configuration validation is the built-in
`Microsoft.Extensions.Options.IValidateOptions<T>` pattern, used by
both `ArchiveSettingsValidator` and `OrderPhotoArchiveSettingsValidator`.
ADR-002's "use FluentValidation" rule applies to controller DTOs;
options validation is a different surface.

**As built**:
`SamedaySettingsValidator : IValidateOptions<SamedaySettings>`,
wired with `services.AddSingleton<IValidateOptions<SamedaySettings>, SamedaySettingsValidator>()`
+ `services.AddOptions<SamedaySettings>().ValidateOnStart()` — mirrors
the existing pattern exactly. Validation rules and error messages are
identical to what the FluentValidation version would have produced.

### Other notes

- `SamedayShippingService.GenerateAwbAsync` returns the
  manual-fallback `AwbResultDto` until bolt 037 lands the real
  workflow. The static fallback service is wrapped (composition, not
  inheritance) for the locker + cost paths because the data those
  paths read (`EasyboxLockers` table, `Shipping:*` config) is
  identical regardless of which courier is registered.
- `SamedayAuthHandler.CloneAsync` reads request bodies via
  `ReadAsByteArrayAsync` and wraps the bytes in a fresh
  `ByteArrayContent`. Slightly heavier than required for `JsonContent`
  (which has a known buffer) but works for any future content type.
- `appsettings.json` ships with `Sameday:Enabled = false` and empty
  credentials. `appsettings.Development.json` overrides `BaseUrl` to
  the Sameday sandbox.
- Migration `20260602141429_AddSamedayOrderFields` was scaffolded
  against the SQLite dev provider and hand-edited to use Postgres
  column types (`text` + `timestamp with time zone`), per the
  established pattern in `AddUploadArchiveFields.cs`.
