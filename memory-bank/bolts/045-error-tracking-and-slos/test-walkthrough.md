---
stage: test
bolt: 045-error-tracking-and-slos
created: 2026-06-03T00:45:00Z
---

## Test Report: error-tracking-and-slos

### Summary

- **Sentry-scoped tests**: 32/32 passed (1.7s)
- **Full suite**: 766/766 passed, 7 skipped (S3 cloud tests — require AWS credentials, expected skip), 0 failed (5s)
- **New test count delta**: +19 tests vs. pre-bolt baseline

### Test Files

- [x] `src/PhotoPrint.Tests/Unit/Configuration/SentrySettingsValidatorTests.cs` (8 tests) — pins the `IValidateOptions<SentrySettings>` contract: disabled is a no-op even with garbage values, enabled enforces DSN + sample-rate constraints with aggregated failure messages.
- [x] `src/PhotoPrint.Tests/Unit/Configuration/SentryDataScrubbersTests.cs` — the PII contract. This bolt shipped 15 tests over a deny-list of sensitive keys; the file was rewritten when that model was found to leak, and now pins the deny-by-default allow-list described in `docs/DEPLOYMENT.md` §13.6 across all three SDK hooks, including an SDK-populated event and a real-transport end-to-end.
- [x] `src/PhotoPrint.Tests/Unit/Middleware/SentryScopeEnricherMiddlewareTests.cs` (3 tests) — smoke tests: absent IHub no-ops, missing correlation id no-ops, unauthenticated user no-ops. The positive scope-tag-stamping path is exercised in the integration test.
- [x] `src/PhotoPrint.Tests/Integration/SentryIntegrationFactory.cs` (test support) — boots the API with `Sentry:Enabled=true` (via env vars set in static ctor — required because Program.cs reads the flag before WAF's `ConfigureAppConfiguration` callback fires), then replaces the DI `IHub` registration with a Moq fake.
- [x] `src/PhotoPrint.Tests/Integration/SentryIntegrationTests.cs` (1 test) — end-to-end: a real HTTP request to `/__test/throw` (the Testing-only synthetic-500 endpoint) flows through the full middleware pipeline, the mock `IHub` captures the exception via `ExceptionHandlerMiddleware`, and `correlation_id` lands on the scope from `SentryScopeEnricherMiddleware`.

### Acceptance Criteria Validation

**Story 001 — Sentry ASP.NET integration**

- ✅ **`Sentry.AspNetCore` package added** — version `4.13.0` in `PhotoPrint.API.csproj`.
- ✅ **`builder.WebHost.UseSentry(o => …)` wired; DSN from `Sentry:Dsn`** — see `Program.cs` lines ~25–55; conditional on `Sentry:Enabled`.
- ✅ **Every Sentry event has tags `correlation_id`, `user_id` (when authenticated), `environment`, `release`** — `correlation_id` + `user_id` stamped per-request by `SentryScopeEnricherMiddleware`; `environment` + `release` stamped once at SDK init from `SentryOptions`. The integration test verifies `correlation_id` reaches the captured scope.
- ✅ **PII scrubbing: email, phone, full request body redacted; only structured metadata sent** — `SentryDataScrubbers.Register` wires the scrubber on `SetBeforeSend`, `SetBeforeSendTransaction` and `SetBeforeBreadcrumb`. (At bolt time only `SetBeforeSend` was wired, which left transactions unscrubbed.)
- ✅ **Sample rate configurable; default 100% errors / 10% transactions** — `Sentry:SampleRate` defaults to `1.0`, `Sentry:TracesSampleRate` defaults to `0.1`. Both validated to be in [0.0, 1.0] when `Enabled=true`.
- ✅ **Integration test: synthetic 500 endpoint produces a Sentry event in the in-memory transport** — `SentryIntegrationTests.Synthetic_500_captures_exception_through_sentry_hub` passes. (Implementation deviation: a Moq `IHub` is used instead of a custom `ITransport`. Achieves the same observable behaviour while sidestepping the process-global static-SDK contamination across the full test suite. See [implementation-walkthrough.md](implementation-walkthrough.md) "Deviations from Plan".)

**Story 002 — SLO documentation + Grafana dashboard**

- ✅ **`memory-bank/operations/slos.md` documents 5 SLOs** — availability 99.5%, p95 checkout latency 1.5s, payment-webhook 99.9%, AWB 98%, ANAF 99%. Each with rationale, source metric expression, breach action, owner.
- ✅ **`ops/dashboards/fototipar-overview.json` is a Grafana dashboard JSON with the required panels** — schema 38 (Grafana 10.x), 8 panels: availability, RPS, latency p50/p95/p99, error rate, orders/day, payment-webhook success, AWB success, ANAF success. Datasource templated via `${DS_PROMETHEUS}`.
- ✅ **README link added under Operations section** — [README.md](../../../README.md) now has an Operations section linking SLOs, dashboard, and DEPLOYMENT.md §13.

### Issues Found

None during testing. One iteration was required during Stage 2 to fix the integration test's cross-suite contamination — captured in detail in [implementation-walkthrough.md](implementation-walkthrough.md). The fix (resolving `IHub` from per-request DI instead of the static `SentrySdk`) is now the production code path, which is also slightly safer outside tests.

### Notes

- **Bolt-044 forward dependency is explicit and accepted.** The dashboard panels reference metrics that bolt 044 will create. *(Superseded by review 044-045-v1 F14: two of the four names assumed here never existed — `http_request_total` and `anaf_submission_total`. The emitted names are `http_server_request_duration_seconds_*` and `invoice_anaf_status_total`, and `DashboardMetricNamesTests` now holds every dashboard and SLO query against a real exposition. Only the ANAF panel is still "No Data", pending intent 016's increments.)*
- **Two-stage rollout posture pinned.** With `Sentry:Enabled=false` (the shipped default), the SDK is never constructed, no middleware is registered, boot is byte-identical to the pre-bolt baseline. Reverse-proof: the suite was run with `Sentry__Enabled` unset and the full 766 passed, including a non-Sentry-related Exception test that exercises `ExceptionHandlerMiddleware`'s unhandled-exception branch (`hub` resolves to null, the no-op path runs).
- **Scope of Sentry capture is correct.** *Superseded by review 044-045-v1 F15* — it was not: a mapped 5xx (`BadGatewayException` → 502) reached Sentry not at all. Capture is now keyed on the mapped status code being ≥ 500, and both legs have integration tests (`MappedServerErrorSentryTests`), including the negative case this note argued was not worth writing.
- **PII scrubbing is enforced.** Every event, transaction and breadcrumb leaving the SDK passes through `SentryDataScrubbers`. It is deny-by-default: the allow-lists live in one static class, and widening one is a 1-line change + a test addition.
