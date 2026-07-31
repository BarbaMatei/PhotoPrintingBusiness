---
stage: implement
bolt: 045-error-tracking-and-slos
created: 2026-06-03T00:30:00Z
---

## Implementation Walkthrough: error-tracking-and-slos

### Summary

Wired the Sentry .NET SDK into the API behind a two-stage feature flag, added a
per-request scope enricher that stamps every event with `correlation_id` +
`user_id`, and centralised PII scrubbing in a single static class. Captured
synthetic 500s via an in-memory mocked `IHub` in an integration test. Also
shipped an internal SLO document and a starter Grafana dashboard JSON.

### Structure Overview

The integration follows the project's existing two-stage rollout pattern
(see Sameday §12 of `DEPLOYMENT.md`): a master `Sentry:Enabled` flag wraps the
SDK wiring. With the flag off, no SDK is constructed and the API boots
byte-identically to the pre-bolt baseline.

Production code touches four surfaces: settings/validator (configuration),
the data scrubber (privacy contract), the scope enricher middleware (per-request
tagging), and a small change to `ExceptionHandlerMiddleware` (capture unhandled
exceptions via the per-request DI'd `IHub`, sidestepping the static-SDK
process-global state that bites tests).

The SLO doc + dashboard are forward-looking: the panels reference metrics that
bolt 044 will create. A NOTE block at the top of `slos.md` makes the dependency
explicit; the SLO targets themselves are committed regardless.

### Completed Work

- [x] `src/PhotoPrint.API/PhotoPrint.API.csproj` — added `Sentry.AspNetCore 4.13.0`.
- [x] `src/PhotoPrint.API/Configuration/SentrySettings.cs` — the POCO; `Enabled` master flag, DSN, optional `Release` / `Environment` overrides, sample rates, debug toggle.
- [x] `src/PhotoPrint.API/Validators/SentrySettingsValidator.cs` — `IValidateOptions<SentrySettings>`; all rules guarded by `if (!Enabled) return Success`, matching the `SamedaySettingsValidator` pattern.
- [x] `src/PhotoPrint.API/Configuration/SentryDataScrubbers.cs` — `SetBeforeSend` hook; clears request body, scrubs sensitive headers, scrubs extras whose keys contain sensitive substrings. The single place to change to add a new sensitive key.
- [x] `src/PhotoPrint.API/Middleware/SentryScopeEnricherMiddleware.cs` — resolves `IHub` from `context.RequestServices` (NOT static `SentrySdk`) so each WebApplicationFactory in tests uses its own hub. Stamps `correlation_id` + `SentryUser.Id` on the request scope.
- [x] `src/PhotoPrint.API/Middleware/ExceptionHandlerMiddleware.cs` — modified: the unhandled-exception branch now resolves `IHub` from per-request DI and calls `CaptureException`. Domain exceptions (the `_exceptionMappings` branch) are NOT captured.
- [x] `src/PhotoPrint.API/Extensions/MiddlewareExtensions.cs` — added `UseSentryScopeEnricher` extension.
- [x] `src/PhotoPrint.API/Program.cs` — registers settings + validator, reads `Sentry:Enabled` once at boot, conditionally calls `builder.WebHost.UseSentry(...)` with the scrubber + scope-enricher middleware. Registers a Testing-only synthetic-throw endpoint at `/__test/throw`.
- [x] `src/PhotoPrint.API/appsettings.json` — added a `Sentry` section with `Enabled: false` and an empty DSN, plus a comment pointing at `DEPLOYMENT.md` §13 + ADR-006.
- [x] `src/PhotoPrint.Tests/Unit/Configuration/SentrySettingsValidatorTests.cs` — 8 tests covering disabled-no-op, valid-enabled, invalid DSN, out-of-range sample rates, aggregate failures.
- [x] `src/PhotoPrint.Tests/Unit/Configuration/SentryDataScrubbersTests.cs` — 7 tests covering body redaction, sensitive headers, sensitive-key substring matching, request-less events.
- [x] `src/PhotoPrint.Tests/Unit/Middleware/SentryScopeEnricherMiddlewareTests.cs` — 3 smoke tests (absent IHub, missing correlation id, unauthenticated user).
- [x] `src/PhotoPrint.Tests/Integration/SentryIntegrationFactory.cs` — boots with `Sentry:Enabled=true` (via env vars set in static ctor; required because Program.cs reads the flag before WAF's `ConfigureAppConfiguration` callback fires), then replaces the DI `IHub` with a Moq fake.
- [x] `src/PhotoPrint.Tests/Integration/SentryIntegrationTests.cs` — hits `/__test/throw` with a known correlation id; asserts the fake hub captured the exception and the scope enricher recorded the `correlation_id` tag.
- [x] `memory-bank/operations/slos.md` — five SLOs documented with rationale, source metric, and on-breach action.
- [x] `ops/dashboards/fototipar-overview.json` — Grafana 10.x dashboard, 8 panels (availability, latency, RPS, error rate, orders/day, payment-webhook success, AWB success, ANAF success).
- [x] `README.md` — added Operations section linking SLOs, dashboard, and DEPLOYMENT.md sections 12/13.
- [x] `docs/DEPLOYMENT.md` — appended §13 (11 sub-sections covering what / flag / provisioning / secrets / rollout / scrubber / playbook / alerts / cost / SLO link / future).

### Key Decisions

- **Two-stage flag posture mirrors Sameday.** `Sentry:Enabled` is a master, not a config-validity bit. The DSN can be wired but Enabled left off — useful during dry-runs.
- **Resolve IHub from per-request DI, not from `SentrySdk`.** The static `SentrySdk` shares one hub across all WebApplicationFactories in a test run; that polluted the integration test until both the scope enricher and the exception handler switched to `context.RequestServices.GetService<IHub>()`. In production this is also slightly safer — clearer ownership, no chance of a stale process-global state.
- **Explicit `IHub.CaptureException` over Sentry's MEL log integration.** The project uses `UseSerilog()` which replaces all other logging providers (including Sentry's). Rather than reconfigure Serilog (which would have downstream impact on Sameday's structured logs etc.), the explicit one-line capture call in `ExceptionHandlerMiddleware` is the smallest correct change.
- **Capture only in the `else` branch.** Domain exceptions (`NotFoundException` etc.) are mapped to `LogWarning` — they're business outcomes, not server errors. Only the LogError branch (genuine 500s) reaches Sentry.
- **Scrubber is a static class, not configurable.** A static list of sensitive substrings is more reviewable than a configurable list; adding a key is a 1-line change that goes through code review. There is no scenario where ops-time configuration of "what to scrub" is desirable.
- **Synthetic-throw endpoint exists only in `Testing` env.** Guarded by `IsEnvironment("Testing")` so neither Development nor Production can reach it.
- **Moq fake for `IHub` in integration test.** A real SDK + custom transport approach fought the process-global static SDK state and was flaky in the full suite. Mocking `IHub` and replacing the DI registration is per-factory clean.
- **SLO doc references not-yet-shipped bolt-044 metrics on purpose.** Shipping the doc + dashboard now (with a clear `NOTE`) becomes the contract bolt 044 implements against. Defers no work; gives the team something to point at.

### Deviations from Plan

- **Acceptance criterion "in-memory transport"** was met via a fake `IHub` instead of a real-SDK + custom `ITransport`. The fake achieves the same observable behaviour (captured events inspectable from test code) with materially less complexity and no static-state contamination across the full suite run.
- Plan estimated ~7 new files + ~5 tests; actual: 9 new production files (settings, validator, scrubber, middleware, dashboard json, slos.md + 3 in tests project) + 4 modified files (Program.cs, MiddlewareExtensions.cs, ExceptionHandlerMiddleware.cs, appsettings.json) + 1 test factory + 4 test files (32 tests across them). Reasonable scope match.

### Dependencies Added

- [x] `Sentry.AspNetCore 4.13.0` — error tracking SDK + ASP.NET integration. Pulls in `Sentry.Extensions.Logging` and `Sentry` transitive packages.

### Developer Notes

- **`SentrySettings` env vars use double underscore.** `Sentry__Enabled=true`, `Sentry__Dsn=...`. Same convention as everywhere else.
- **`SentryRequest` not `Request`.** Sentry SDK 4.x renamed `Sentry.Request` → `Sentry.SentryRequest` (avoids collision with `HttpRequest`). Tests that construct events should use `SentryRequest`.
- **Test order can affect the static SDK.** The integration test factory's static constructor sets `Sentry__Enabled=true` env var, which persists for the test process. Other factories booting after this point ALSO see the flag. That's why we resolve `IHub` from per-request DI — each factory's own DI registration of `IHub` is what's used, isolating the test.
- **The `BeforeSend` family runs on a worker thread.** The scrubber must be allocation-light. It must also never throw: Sentry 4.13 sends the **original, unscrubbed** payload when the callback throws (verified against the real SDK — the note here previously claimed the event is dropped, which is wrong). The scrubber therefore catches everything and returns null to drop the payload.
- **Widening what Sentry sees**: the scrubber is deny-by-default; add the key to the relevant allow-list in `SentryDataScrubbers.cs` (`AllowedHeaders`, `AllowedEnvKeys`, `AllowedExtraKeys`, `AllowedDiagnosticKeys`, `UrlValuedKeys`) plus a test case. The shipped contract is documented in `docs/DEPLOYMENT.md` §13.6.
- **The dashboard's `${DS_PROMETHEUS}` template variable** needs the Grafana operator (or a manual datasource binding) to resolve to your real Prometheus instance before any panel renders data.
