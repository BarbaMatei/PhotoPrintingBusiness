---
stage: implement
bolt: 036-sameday-api-client
created: 2026-06-02T14:30:00Z
---

# Stage 4 — Implementation Walkthrough: Sameday API Client

## Summary

Bolt 036's foundations are in place — HTTP transport, token cache, schema,
DI wiring. With `Sameday:Enabled = false` (the shipped default), system
behaviour is byte-identical to the pre-bolt baseline.

Three small Stage-2 corrections were made during implementation (recorded
in the `Stage 4 Correction` footer on `ddd-02-technical-design.md`): Polly
v8 `ResiliencePipeline` instead of the v7 API, rate-limit deferred to bolt
037, and the options validator follows the project's
`IValidateOptions<T>` pattern instead of FluentValidation.

Solution build: **green** (`dotnet build PhotoPrint.sln` — 0 errors, 5
warnings, all pre-existing).

---

## Files Created

### Exceptions (`src/PhotoPrint.API/Exceptions/`)

- `SamedayException.cs` — abstract base; carries `Endpoint` + optional `HttpStatus`.
- `SamedayAuthException.cs` — second 401 after token refresh; caller stops retrying.
- `SamedayUnreachableException.cs` — transport-exhausted retries; caller schedules.
- `SamedayProtocolException.cs` — 2xx with bad payload; vendor contract drift.
- `SamedayValidationException.cs` — 4xx (≠ 401/408/429); our request is malformed.

### Configuration + validator

- `Configuration/SamedaySettings.cs` — POCO, `Enabled` defaulting to `false`.
- `Validators/SamedaySettingsValidator.cs` — `IValidateOptions<SamedaySettings>`,
  every rule guarded by `options.Enabled`.

### Domain value objects + DTOs (`src/PhotoPrint.API/Services/Sameday/`)

- `SamedayToken.cs` — record with `IsValid(now, safetyWindow)`; overrides
  `ToString()` to never expose `Value`.
- `SamedayCredentials.cs` — overrides `ToString()` to `Username=***, Password=***`.
- `AwbCreationRequest.cs` + `AwbCreationResult.cs` — declared for bolt 037.
- `TrackingState.cs` + `TrackingSnapshot.cs` + `TrackingEvent.cs` — declared
  for bolt 037.
- `SamedayWireDtos.cs` — internal vendor JSON shapes; only
  `AuthenticateResponse` is wired in this bolt.
- `LogRedactor.cs` — single chokepoint for log-string formatting of headers.

### Domain + transport services

- `ISamedayTokenProvider.cs` + `SamedayTokenProvider.cs` — singleton,
  `SemaphoreSlim(1,1)` against thundering-herd, `TimeProvider` injected.
- `ISamedayAuthenticator.cs` — narrow ctor dep for the token provider; lets it
  avoid taking the full `ISamedayClient`.
- `ISamedayClient.cs` + `SamedayClient.cs` — typed HttpClient.
  Only `AuthenticateAsync` is implemented; AWB/label/tracking methods throw
  `NotImplementedException("Implemented in bolt 037-awb-and-tracking-jobs.")`.
- `SamedayAuthHandler.cs` — bearer attach, 401-retry-once, clones the
  request body via `ReadAsByteArrayAsync` so the retried call has its own
  readable stream.
- `SamedayPolicies.cs` — static factory for the Polly v8
  `ResiliencePipeline<HttpResponseMessage>` (retry only).
- `SamedayResilienceHandler.cs` — wraps `base.SendAsync` in the pipeline.

### Shipping service

- `Services/SamedayShippingService.cs` — registered only when
  `Sameday:Enabled=true`. Reuses `StaticShippingService` by composition for
  locker + cost lookups (same DB + config sources); AWB method returns the
  manual-fallback DTO until bolt 037.

---

## Files Modified

- `Models/Order.cs` — added `AwbLabelUrl: string?` and
  `LastTrackingSyncAt: DateTimeOffset?` properties.
- `Data/PhotoPrintDbContext.cs` — added `HasMaxLength(500)` + `IsRequired(false)`
  for `AwbLabelUrl`; `IsRequired(false)` for `LastTrackingSyncAt` (the PostgreSQL
  Unix-ms converter from the top of `OnModelCreating` applies automatically
  via the loop).
- `Program.cs` — registered `TimeProvider.System` as singleton; added the
  conditional Sameday DI block (validator + typed client + auth handler +
  resilience handler + conditional `IShippingService` selection).
- `appsettings.json` — added `Sameday` section with `Enabled = false` and
  empty credential strings.
- `appsettings.Development.json` — overrides `BaseUrl` to the Sameday
  sandbox URL; flag still `false`.

### Migration

- `Migrations/20260602141429_AddSamedayOrderFields.cs` — scaffolded against
  PostgreSQL, hand-edited to Postgres column types (`text` for the URL with
  `maxLength: 500`, `timestamp with time zone` for the timestamp). PostgreSQL
  never executes this — it uses `EnsureCreated` (Program.cs lines 152-184).
- `Migrations/20260602141429_AddSamedayOrderFields.Designer.cs` — generated
  by `dotnet ef migrations add` (snapshot capture).
- `Migrations/PhotoPrintDbContextModelSnapshot.cs` — updated by the same
  command.

---

## DI Wiring Detail

When `Sameday:Enabled = false`:

- `SamedaySettings` is bound + validated (validator is a no-op when disabled).
- `TimeProvider.System` is registered (singleton).
- `IShippingService → StaticShippingService` (unchanged).
- No Sameday services are registered. Boot is identical to the pre-bolt
  baseline.

When `Sameday:Enabled = true`:

- `SamedaySettings` is bound + validated (boot fails fast on missing
  credentials, bad URL, or `RequestTimeoutSeconds` out of `[1, 60]`).
- `ISamedayTokenProvider → SamedayTokenProvider` (singleton).
- `ISamedayAuthenticator` resolves to `ISamedayClient` via a factory
  (avoids the ctor cycle where the client wants the provider wants the
  authenticator wants the client).
- `SamedayAuthHandler` + `SamedayResilienceHandler` registered transient.
- `AddHttpClient<ISamedayClient, SamedayClient>` configures the typed
  client with `BaseAddress` + `Timeout` from settings; chain:
  `SamedayAuthHandler → SamedayResilienceHandler → primary`.
- `IShippingService → SamedayShippingService` (scoped).

---

## Notable Decisions Made During Implementation

1. **`ISamedayAuthenticator` extracted as a narrow surface.** Avoids the
   `TokenProvider → Client → TokenProvider` ctor cycle. The DI registration
   resolves it from the same singleton `ISamedayClient` instance.

2. **`SamedayShippingService` composes `StaticShippingService` rather than
   inheriting.** Locker + cost paths read the same data regardless of
   which courier is configured; composition keeps the two services
   decoupled and lets bolt 037 swap behaviours independently.

3. **Migration types fixed for Postgres post-scaffold.** EF scaffolds
   against the *configured* provider, which is PostgreSQL in dev. Migrations
   only run on Postgres (Program.cs guards on `IsNpgsql()`). The existing
   `AddUploadArchiveFields.cs` documents the same pattern; this migration
   follows it.

4. **`AuthenticatePath` short-circuit in `SamedayAuthHandler`.** The path
   match uses `EndsWith` on `AbsolutePath` so it works whether the request
   URI is absolute (test scenarios with a mocked handler) or relative
   (production, where `BaseAddress` provides the host).

5. **`SamedayClient.AuthenticateAsync` throws the exception taxonomy
   directly** rather than relying on Polly to surface it. The resilience
   pipeline only converts retries; the 401 / 5xx / 4xx / protocol-violation
   branches are explicit so the test surface is deterministic.

---

## Build + Compile Verification

```text
dotnet build PhotoPrint.sln
  → Build succeeded.  0 Error(s), 5 Warning(s)
  (all warnings are pre-existing: NU1603 Stripe.net version, EF1002 in
   OrderNumberService, CS1998 in RazorTemplateServiceTests)
```

`dotnet ef migrations add AddSamedayOrderFields` succeeded; the resulting
migration was hand-edited for Postgres column types and the design-time
build passed.

---

## What Bolt 036 Does NOT Do (by design)

These items live in bolt 037 (`037-awb-and-tracking-jobs`):

- `SamedayClient.CreateAwbAsync` real implementation + DTO mapping.
- `SamedayClient.GetLabelPdfAsync` real implementation.
- `SamedayClient.GetTrackingAsync` real implementation + state mapping.
- `SamedayShippingService.GenerateAwbAsync` workflow (Order → request).
- `Channel<AwbJob>` queue + `AwbCreationJob : BackgroundService`.
- `ShipmentTrackingJob : BackgroundService` with 15-minute period.
- 5 req/s rate-limit policy (will land alongside the tracking job).
- Admin notifications for repeated AWB failures.

---

## ⛔ Human Checkpoint

Stage 4 (Implement) is complete and the solution builds. Please review
and approve before I move to Stage 5 (Test).

**Ready to proceed?**

- **1** — Approve and continue to Stage 5.
- **2** — Need changes (specify which file or behaviour).
