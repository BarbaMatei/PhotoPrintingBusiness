---
stage: plan
bolt: 045-error-tracking-and-slos
created: 2026-06-02T18:00:00Z
---

## Implementation Plan: error-tracking-and-slos

### Objective

Wire Sentry error tracking into the API so every unhandled exception lands in Sentry with correlation id, user id, environment, release SHA, and PII-scrubbed payloads — and document the SLOs the team will operate against, with a starter Grafana dashboard JSON.

### Deliverables

**Story 001 — Sentry ASP.NET integration**

- `Sentry.AspNetCore` package added to `PhotoPrint.API.csproj`.
- `SentrySettings` POCO + `IValidateOptions<SentrySettings>` (matching the `SamedaySettings` / `ArchiveSettings` precedent — see [[adr-005-fluentvalidation-vs-validateoptions]]).
- `Configuration/SentryDataScrubbers.cs` — the central allow/deny list for tags + request bodies.
- `Program.cs` wiring: `builder.WebHost.UseSentry(...)` reading from `Sentry:` section. SDK guarded by `Sentry:Enabled` master flag (off by default) so an empty DSN never crashes boot. Two-stage rollout matches the Sameday posture.
- A request scope enricher (small middleware or `ISentryUserFactory` + `SetBeforeSend` callback) that stamps every event with: `correlation_id` (from existing `CorrelationIdMiddleware`), `user_id` (from `ClaimsPrincipal` when authenticated), `environment` (from `IHostEnvironment`), `release` (from env var `GIT_COMMIT_SHA`).
- `SetBeforeSend` PII scrubber: drops `email`, `phone`, `password`, full request/response bodies, cookies, `Authorization` header. Keeps the ProblemDetails `correlationId` extension and structured exception metadata.
- Default sample rates: 100% errors, 10% transactions. Both overridable from config.
- `appsettings.json` block (commented placeholder) + extension to `docs/DEPLOYMENT.md` section 13 mirroring section 12 (Sameday) style.

**Story 002 — SLO documentation + Grafana dashboard JSON**

- `memory-bank/operations/slos.md` documenting:
  - Availability ≥ 99.5% (rolling 30 d).
  - p95 checkout latency ≤ 1.5s on `POST /api/payments/stripe/intent`.
  - Payment-webhook success ≥ 99.9% (Stripe + the legacy processor combined).
  - AWB auto-creation ≥ 98% (intent 015).
  - ANAF submission success ≥ 99% (intent 016, planned).
- `ops/dashboards/fototipar-overview.json` — Grafana dashboard JSON, 6 panels: RPS, latency p50/p95/p99, error rate, orders/day, payment-webhook success, AWB success, ANAF status.
- `README.md` — short "Operations" section linking to `memory-bank/operations/slos.md` and the dashboard JSON.

### Dependencies

- **Requires (already shipped)**: bolt 040 (deploy workflow that sets `GIT_COMMIT_SHA`), Serilog + `CorrelationIdMiddleware` (intent 001).
- **Forward dependency (NOT YET shipped)**: bolt 044 (OTel tracing + Prometheus business metrics). Story 002's dashboard references metric names that bolt 044 will create. **Handled as design choice — see below.**

### Technical Approach

**Sentry SDK choice.** `Sentry.AspNetCore` 4.x — wires into `IWebHostBuilder` directly. No separate logging-provider config needed; Serilog already does that. Sentry will receive unhandled exceptions via its built-in ASP.NET Core integration plus any explicit `SentrySdk.CaptureException` calls (none planned for this bolt).

**Two-stage rollout.** Mirroring Sameday:

- `Sentry:Enabled` (master) — false by default. Wires the SDK and starts sending events only when true.
- `Sentry:Dsn` — empty by default. With master flag off, the SDK is never constructed.

This means: zero risk of leaking events from local dev or test envs (Testing host + Postgres dev DB never flip `Enabled=true`).

**Scope enricher pattern.** Two options:

1. **`SetBeforeSend` callback** — runs on the SDK worker thread, mutates event before send.
2. **Per-request `SentrySdk.ConfigureScope(...)` from a middleware** — runs in request pipeline.

Option 2 is cleaner because it has direct access to `HttpContext` (already-stamped `CorrelationId` in `context.Items`, `HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value`). One middleware, `SentryScopeEnricherMiddleware`, registered after `CorrelationIdMiddleware`. PII scrubbing stays in `SetBeforeSend` (global, runs even for events captured outside an HTTP request — e.g. background-service exceptions).

**Release SHA.** `Sentry:Release` config key takes precedence if set; otherwise read `GIT_COMMIT_SHA` env var (set by the GitHub Actions deploy workflow per bolt 040). Fallback is `null` — Sentry then derives a release from assembly version, which is fine for dev.

**Sample rates.**

- `o.SampleRate = 1.0` (default; errors always sent).
- `o.TracesSampleRate = 0.10` (10% transactions for performance traces).
- Both overridable: `Sentry:SampleRate`, `Sentry:TracesSampleRate`.

**PII scrubber list.**

The list below is what this bolt shipped. It was a deny-list at one SDK hook and leaked; the
shipped contract is now deny-by-default across all three hooks — see `docs/DEPLOYMENT.md` §13.6.

- Request body — **always dropped**. We send ProblemDetails which already contains a sanitized detail message; the raw body is replaced with `<scrubbed:request-body>`.
- Request headers — `Authorization`, `Cookie`, `X-Guest-Token` dropped.
- Form fields — `email`, `phone`, `password`, `confirmPassword` keys dropped.
- Query string — kept (no known PII on this codebase; if added later, scrubber expands).
- Stack traces — kept.
- Extra context — kept.
- Tags — kept (correlation_id, user_id are already non-PII).

The scrubber list lives in `Configuration/SentryDataScrubbers.cs` as static readonly arrays so it's reviewable in one place.

**Test approach.** Sentry SDK ships an in-memory transport. The integration test:

1. Boots the API via `WebApplicationFactory` with `Sentry:Enabled=true` and a `MemoryTransport`.
2. Hits a synthetic 500 endpoint registered for the test only (we'll add `MapGet("/__test/throw", () => throw new Exception("synthetic"))` behind `IHostEnvironment.IsEnvironment("Testing")`).
3. Asserts the in-memory transport captured exactly one event with correlation_id and the exception type.

Plus unit tests for the scrubber (data in → scrubbed data out) and the scope enricher (claims + correlation id → event tags).

**Out of scope for this bolt** (would creep otherwise):

- Sentry Frontend SDK (Angular). Separate intent — UI errors are surfaced via the existing toast service, not Sentry today.
- Burn-rate alerts on SLOs. SLO doc enumerates targets; alerts are an ops decision.
- PagerDuty / OpsGenie pager integration. Sentry handles email/slack out of the box.
- Per-route Sentry sampling. Bolt 044 handles that for OTel; Sentry stays at the global default for now.

### Resolved design question: forward dependency on bolt 044

Bolt 044 (OTel + Prometheus business metrics) is planned but not shipped. Story 002 of this bolt references metric names that bolt 044 will create (`payment_webhook_total`, etc.).

**Recommendation:** ship story 002 **forward-looking**:

- The SLO doc enumerates the targets in plain English and references the *intended* metric names.
- The Grafana dashboard JSON references the same metric names.
- A clear `> NOTE` block at the top of `slos.md` says "metrics referenced here ship in bolt 044 — dashboard panels will show 'No Data' until then."

This means the artefacts become *the contract* that bolt 044 implements against. The dashboard turns on when bolt 044 ships; no rework, no second pass.

Alternative considered: defer story 002 to a slipstream of bolt 044. Rejected because (a) the SLO doc is independently useful as a written commitment, (b) writing the dashboard now while the metric names are fresh is cheaper than re-doing it after bolt 044.

### Acceptance Criteria

**Story 001 (Sentry):**

- [ ] `Sentry.AspNetCore` package added.
- [ ] `Sentry:Enabled` master flag wired; SDK never constructed when off.
- [ ] `Sentry:Dsn` read from config; empty value safely no-ops even when master flag is on (SDK ships this behaviour — verified by docs).
- [ ] Every captured event carries tags `correlation_id`, `user_id` (when authenticated), `environment`, `release`.
- [ ] PII scrubber drops: email/phone/password fields, full request body, Authorization/Cookie/X-Guest-Token headers.
- [ ] Configurable sample rates (default 100% errors / 10% transactions).
- [ ] Integration test passes: synthetic 500 → in-memory transport captures one event with correct tags.
- [ ] `SentrySettingsValidator` enforces: when `Enabled=true`, `Dsn` must be a parseable URI.

**Story 002 (SLO + dashboard):**

- [ ] `memory-bank/operations/slos.md` exists with 5 documented SLOs + caveat about bolt-044 metric dependency.
- [ ] `ops/dashboards/fototipar-overview.json` exists, parseable as Grafana 10.x JSON.
- [ ] `README.md` Operations section links both.

### Risk register

- **R1 — Sentry SDK boot failure with malformed DSN.** Mitigated by the master `Enabled` flag + a strict validator. Dev secrets (`appsettings.Development.Local.json`) never set `Enabled=true`.
- **R2 — Scope enricher fires for events captured before middleware runs** (e.g., during DI construction). Sentry then sends an event with no correlation_id. Acceptable — these are extremely rare boot-path exceptions and the `release` tag still tells us where to look.
- **R3 — Forward-referenced metrics in dashboard JSON.** Mitigated by the `NOTE` block in slos.md and by panel titles that match what bolt 044 will implement.
- **R4 — In-memory transport test couples to internal Sentry types.** Mitigated by using only public `Sentry.Extensions.Logging.SentryLoggerOptions.Transport` surface.

### Estimated scope

| Surface | Files | Tests |
|---|---|---|
| Settings + validator | 2 | 1 |
| Scrubber config | 1 | 1 |
| Scope-enricher middleware | 1 | 1 |
| Program.cs wiring | 0 (modified) | — |
| `SentryDataScrubbers` static class | 1 | 1 |
| Synthetic-throw test endpoint (Testing env only) | 0 (in Program.cs) | 1 (integration) |
| appsettings.json + DEPLOYMENT.md section 13 | 0 (modified) | — |
| SLO doc | 1 | — |
| Dashboard JSON | 1 | — |
| README update | 0 (modified) | — |
| **Total new files** | **~7** | **~5 tests** |

This is a small bolt — the design fits a `simple-construction-bolt` 3-stage flow comfortably.
