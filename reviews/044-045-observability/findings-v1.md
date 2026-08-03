---
type: findings
target: 044-045-observability
version: 1
commit: 5cac465
date: 2026-07-31
---

# Findings v1 — 044-045-observability

Per-finding detail behind [review-v1.md](review-v1.md). Each entry: the concrete failure, the
suggested fix, and the evidence the pass actually produced. "Trace" = an adversarial skeptic
built the failing path (often by running code); "convergence" = N lenses raised it
independently and the script accepted agreement as the precision signal; "main-agent recheck" =
the synthesizer re-verified it by hand against the source.

---

## 🔴 F1 / D1 — `/metrics` allow-list checks the TCP peer, which is always the Caddy proxy

**File** `src/PhotoPrint.API/Middleware/MetricsEndpointIpAllowListMiddleware.cs:41`
**Convergence** 3 (security, input-validation, completeness-critic) · **Verdict** confirmed

**Failure.** `docker-compose.prod.yml` runs Caddy as the TLS edge in front of `api:8080`, and
the `Caddyfile` does a bare `reverse_proxy api:8080` for every path — there is no `/metrics`
block. The API is `expose`d, not published, so *every* request reaches Kestrel from the Caddy
container. `context.Connection.RemoteIpAddress` is therefore the proxy's IP for all traffic,
internal and external alike. To make Prometheus scraping work at all, ops must add that IP to
`AllowedScrapeIps` — at which point `GET https://<site>/metrics` returns the full metric store
to any anonymous caller on the internet.

**Fix.** Block `/metrics` at the Caddy edge, or bind the Prometheus exporter to a separate
internal port that is not proxied. Do **not** simply switch to `X-Forwarded-For` without also
making the edge strip and rewrite it — a spoofable header is weaker than what exists now.

**Evidence.** Main-agent recheck: `Caddyfile:13` is `reverse_proxy api:8080` with no path
matcher; `docker-compose.prod.yml:33` sets `ASPNETCORE_URLS: http://+:8080` and `:36` exposes
8080 internally only; `grep -rn "UseForwardedHeaders|ForwardedHeaders" src/ --include=*.cs`
returns nothing. ADR-018 argues network identity is the right primitive but does not address
the proxy hop.

---

## 🔴 F2 / D2 — Sentry transactions bypass the scrubber entirely

**File** `src/PhotoPrint.API/Program.cs:57` · **Convergence** 1 (security) · **Verdict** confirmed

**Failure.** The scrubber is installed with `o.SetBeforeSend(...)` only. That hook does not run
for performance **transactions**, and `SetBeforeSendTransaction` is never called. `appsettings.json`
ships `TracesSampleRate=0.1`, so Sentry.AspNetCore auto-creates a transaction on roughly one
request in ten, and that transaction carries `scope.Request` — headers, query string, URL.
Guest sessions authenticate with a 7-day `X-Guest-Token` GUID, so ~10% of guest requests send a
live session credential to a third party.

**Fix.** Add `o.SetBeforeSendTransaction` applying the same scrub to `txn.Request`, or set
`TracesSampleRate=0` until it exists. Add a test asserting the transaction hook is non-null.

**Evidence.** Trace (reproduced live). A probe app mirroring `Program.cs:48-58` against real
Sentry.AspNetCore 4.13.0 issued `GET /api/cart?email=...` with `X-Guest-Token: <guid>`. The
resulting transaction envelope contained `"headers":{"X-Guest-Token":"1111...5555"}` and
`"query_string":"?email=victim%40example.com"` with no `<scrubbed>` marker. The SDK drops
`Authorization`/`Cookie` on its own; the guest token it does not know about.

---

## 🔴 F3 / D3 — Scrubber never touches the query string, which carries emails and tokens

**File** `src/PhotoPrint.API/Configuration/SentryDataScrubbers.cs:44`
**Convergence** 2 (security, input-validation) · **Verdict** confirmed

**Failure.** `Scrub` rewrites `Request.Data`, `Request.Headers` and `Extra` — never
`Request.QueryString` or `Request.Url`. On this app the query string carries customer PII and
at least one credential: admin order search matches `NormalizedEmail`/`GuestEmail`
(`AdminOrderService.cs:75`), and email confirmation takes its token via query
(`AuthController.cs:98`). Any 500 on such a request ships the value to Sentry. The class's own
docstring lists "query string" under *Keep* — so this is intentional, and the intent is wrong.

**Fix.** Scrub `req.QueryString` and `req.Url`: drop them outright, or parse and redact values
whose parameter name is sensitive plus anything matching an email pattern. Add a test.

**Evidence.** Trace (reproduced live). Probe app with `SendDefaultPii=false` and the real
`SetBeforeSend`, throwing inside `GET /boom?search=ion.popescu@gmail.com&token=abc123`:
`BeforeSend` observed `Request.QueryString = "?search=ion.popescu@gmail.com&token=abc123"`, and
`Scrub` passed it through untouched.

---

## 🔴 F4 / D4 — Header scrubbing is case-sensitive, so HTTP/2 lowercase names survive

**File** `src/PhotoPrint.API/Configuration/SentryDataScrubbers.cs:46`
**Convergence** 2 (correctness, quality) · **Verdict** confirmed

**Failure.** The scrubber uses two different matching strategies: an exact-cased
`req.Headers.ContainsKey("X-Guest-Token")` pass, and a substring pass over
`SensitiveFieldNames` that does not include `token` or `cookie`. HTTP/2 mandates lowercase
field names, so Kestrel stores the header as `x-guest-token`; `SentryRequest.Headers` is an
ordinal `Dictionary`, so the exact lookup misses and the substring pass has no matching term.
A live guest credential ships in cleartext on any 500 during guest checkout.

**Fix.** Match header names case-insensitively (iterate keys with
`StringComparison.OrdinalIgnoreCase`) rather than exact-cased `ContainsKey`. Add an
HTTP/2-lowercase test case.

**Evidence.** Trace (executed). Real Sentry 4.13.0 with a `DefaultHttpContext` carrying
`x-guest-token: 5f0c-live-guest-guid`: `ScopeExtensions.Populate` copies the key verbatim,
`ContainsKey("X-Guest-Token")` returns false, and the scrub output was
`sent hdr: [x-guest-token] = 5f0c-live-guest-guid`. The same run showed lowercase `cookie`
leaking, because the SDK's own PII filter is ordinal too.

---

## 🔴 F5 / D5 — Per-route sample rates can never match a configured route

**File** `src/PhotoPrint.API/Observability/Sampling/RouteAwareSampler.cs:63`
**Convergence** 5 (correctness, requirements, input-validation, tests-coverage, completeness-critic) · **Verdict** confirmed

**Failure.** `ResolveRoute` looks for an `http.route` tag and otherwise returns
`parameters.Name`. The sampler runs at span **start**, before ASP.NET Core routing has resolved
the endpoint, so no `http.route` tag exists and the lookup key is the activity name
`"Microsoft.AspNetCore.Hosting.HttpRequestIn"`. Even when the tag is present it reads
`api/products` — no method, no leading slash — while config keys are written
`"GET /api/products"`. Every lookup misses, every route falls through to `Default`, and with
the shipped `Default = 1.0` the hottest routes are traced at 100%: maximal cost, and the
feature's headline promise silently absent.

**Fix.** Drop route matching from the sampler (it cannot see the route) and do route-rate
filtering in a tail processor or in the collector; or key on `url.path` prefixes, which *are*
available at span start.

**Evidence.** Main-agent recheck of `RouteAwareSampler.cs:63-78` confirms the fallback to
`parameters.Name` and the key-format mismatch. Accepted on 5-lens convergence without a
skeptic. `RouteAwareSamplerTests` pass because they fabricate an `http.route` tag in the
sampling parameters — production never supplies one.

---

## 🔴 F6 / D6 — "Errors are always sampled" is dead code

**File** `src/PhotoPrint.API/Observability/ErrorOverrideProcessor.cs:18`
**Convergence** 4 (correctness, requirements, observability, tests-coverage) · **Verdict** confirmed

**Failure.** The processor forces the `Recorded` flag in `OnEnd`. But when the sampler returns
`SamplingDecision.Drop`, the activity is created with `IsAllDataRequested = false`, and the
OTel SDK's `ActivityStopped` handler returns early for such activities — processors' `OnEnd` is
never invoked. So exactly the spans the override exists to rescue are the ones it never sees. A
500 on a sampled-out request is never exported. The processor has no test file at all.

**Fix.** Sample `RecordOnly` instead of `Drop` and drop non-error spans in a processor. Prove it
with an `InMemoryExporter` test at rate `0.0` asserting an errored span is still exported.

**Evidence.** Main-agent recheck of `ErrorOverrideProcessor.cs:18-22` and
`ObservabilityExtensions.cs:59-60` (the processor is registered, the sampler wraps
`RouteAwareSampler` in `ParentBasedSampler`); the observability lens verified the
`IsAllDataRequested` early-return empirically. Accepted on 4-lens convergence.

---

## 🔴 F7 / D7 — Webhook branches record neither log nor metric

**File** `src/PhotoPrint.API/Controllers/WebhooksController.cs:216`
**Convergence** 4 (correctness, requirements, observability, race) · **Verdict** confirmed

**Failure.** The EuPlatesc IPN handler is a three-branch `if/else-if` chain with no terminal
`else`. A realistic sequence — customer abandons checkout, order is cancelled, 3DS completes
late, `action=0` arrives — matches no branch: the success IPN response is returned, nothing is
logged, `payment_webhook_total` is not incremented. The customer is charged, the order is never
`Paid`, and SLO 3 (`ok/total`) still reads 100% because the event never entered the
denominator. `HandleStripePaymentFailedAsync` has the same shape with two silent `return`s
(no PaymentIntent ID, order not found) and no `else` on its final `if`.

**Fix.** Add a terminal `else` that logs at Warning and records
`payment_webhook_total{result="failed"}`; give the payment-failed early returns the same
log + metric treatment the succeeded handler already has.

**Evidence.** Main-agent recheck of `WebhooksController.cs:195-290`: the EuPlatesc chain ends
at the `action != "0" && Status == AwaitingPayment` branch and falls straight to
`return Content(...)`; `HandleStripePaymentFailedAsync` returns bare at both guard clauses.
Accepted on 4-lens convergence.

---

## 🔴 F8 / D8 — The Sentry end-to-end test mocks `IHub`, so the scrubber never runs

**File** `src/PhotoPrint.Tests/Integration/SentryIntegrationFactory.cs:85`
**Convergence** 2 (requirements, tests-coverage) · **Verdict** confirmed

**Failure.** The factory replaces the DI registration of `IHub` with a Moq fake, which bypasses
the real Sentry client — and `BeforeSend` runs inside the real client. So `SetBeforeSend` (the
entire PII scrubber) and `SendDefaultPii=false` are never executed by any test.
`SentryDataScrubbersTests` calls `Scrub()` directly, which proves the function but not the
wiring. Nothing anywhere asserts the options are configured.

**Fix.** Resolve `SentryAspNetCoreOptions` from the test host, assert `BeforeSend` is non-null
and `SendDefaultPii` is false, then invoke the configured delegate against an SDK-shaped event.

**Evidence.** Trace (executed). The skeptic deleted `o.SendDefaultPii = false;` and
`o.SetBeforeSend(...)` from `Program.cs:56-57` and ran
`dotnet test src/PhotoPrint.Tests --filter FullyQualifiedName~Sentry`: **32/32 passed**.
Mutation reverted, tree clean.

---

## 🔴 F9 / D9 — No test observes any business metric being emitted

**File** `src/PhotoPrint.Tests/Integration/MetricsEndpointIntegrationTests.cs:50`
**Convergence** 2 (requirements, tests-coverage) · **Verdict** confirmed

**Failure.** `FotoMetricsTests` only reflects over the static instrument fields; the `/metrics`
integration test asserts only that `# HELP` and `# TYPE` appear — lines the runtime and
ASP.NET Core instrumentation emit on their own, with zero business traffic. So every
`FotoMetrics` call site could be deleted and the suite stays green, and the dashboards would
silently show No Data.

**Fix.** Add `MeterListener`-based tests around `OrderService`/`UploadService`/
`WebhooksController`/`AwbCreator` asserting instrument name and tag values, and assert
`orders_created_total` actually appears in the `/metrics` body.

**Evidence.** Trace (executed). No test uses `MeterListener` or `MetricCollector` anywhere in
`src/PhotoPrint.Tests`. Deleting `FotoMetrics.OrdersCreated.Add` (`OrderService.cs:186`) and
deleting `m.AddMeter(MetricNames.Meter)` (`ObservabilityExtensions.cs:83`) both compile and
leave the suite green.

---

## 🟠 F10 / D10 — Unparseable allow-list entries are silently dropped

**File** `src/PhotoPrint.API/Middleware/MetricsEndpointIpAllowListMiddleware.cs:33`
**Convergence** 5 (correctness, security, requirements, input-validation, observability) · **Verdict** confirmed

**Failure.** The constructor does `IPAddress.TryParse(s, out var ip) ? ip : null` then filters
nulls — so anything that does not parse vanishes without a word. `IPAddress.TryParse` returns
false for `"10.0.0.0/8"` and for `" 127.0.0.1"`. CIDR is exactly the format the settings
documentation advertises. The validator only checks the array is non-empty, so a
correct-looking config boots clean with an **empty** allow-set: every scrape 403s and the
dashboards go dark with no startup error and no log line.

**Fix.** Validate that every entry parses at startup and fail boot on any that does not; trim
entries; add real CIDR support, since container peers get dynamic addresses.

**Evidence.** Main-agent recheck: `MetricsEndpointIpAllowListMiddleware.cs:32-36` is the silent
filter; `ObservabilitySettingsValidator.cs:42-44` checks only
`AllowedScrapeIps is null || .Length == 0`. Accepted on 5-lens convergence.

---

## 🟠 F11 / D11 — Middleware registered `Scoped`, defeating its own log-flood guard

**File** `src/PhotoPrint.API/Extensions/ObservabilityExtensions.cs:50`
**Convergence** 6 (correctness, security, quality, input-validation, observability, race) · **Verdict** confirmed

**Failure.** The middleware implements `IMiddleware`, so `UseMiddleware` resolves it per request
through `IMiddlewareFactory`. Registered `AddScoped`, every request gets a fresh instance — so
`_loggedDenies` is always empty, `TryAdd` always succeeds, and the "one Info entry per distinct
denied IP per process" contract written in the class docstring never holds. A scraper deployed
with the wrong IP logs a line every scrape (4/min at a 15s interval) forever. The allow-list
`HashSet` is also re-parsed on every request.

**Fix.** Register as `AddSingleton` — the middleware reads `IOptions` once and holds no
per-request state — or make `_allowed`/`_loggedDenies` static with a bounded dictionary.

**Evidence.** Main-agent recheck: `ObservabilityExtensions.cs:50` reads
`services.AddScoped<MetricsEndpointIpAllowListMiddleware>();`. Highest convergence in the pass
(6 lenses).

---

## 🟠 F12 / D12 — IPv4-mapped IPv6 peers never match IPv4 allow-list entries

**File** `src/PhotoPrint.API/Middleware/MetricsEndpointIpAllowListMiddleware.cs:42`
**Convergence** 2 (correctness, input-validation) · **Verdict** confirmed

**Failure.** `ASPNETCORE_URLS=http://+:8080` makes Kestrel bind a dual-mode IPv6 socket, so an
IPv4 scraper arrives as `::ffff:10.42.0.5` with family `InterNetworkV6`. `IPAddress.Equals`
does not treat that as equal to `IPAddress.Parse("10.42.0.5")`, so a correctly-configured
Prometheus IP is 403'd and metrics collection silently stops.

**Fix.** Normalize both sides before comparing — `if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();`
— and apply the same normalization to parsed allow-list entries.

**Evidence.** Trace (executed on net8.0). `_allowed.Contains(::ffff:10.42.0.5)` against a set
holding `10.42.0.5` returned `False`; same for `::ffff:127.0.0.1`. `docker-compose.prod.yml:33`
supplies the dual-mode bind. Unit tests only use pure v4 or pure v6 addresses.

---

## 🟠 F13 / D13 — Console span exporter silently enabled in production

**File** `src/PhotoPrint.API/Extensions/ObservabilityExtensions.cs:78`
**Convergence** 2 (correctness, security) · **Verdict** confirmed

**Failure.** `Observability:Enabled=true` with the shipped empty `Otlp:Endpoint` passes
validation (the validator only checks the endpoint *if non-blank*) and takes the
`AddConsoleExporter()` branch. The inline comment asserts "Production deployments always set
`Otlp:Endpoint`" — an assumption, not a guard. With `Sampling.Default=1.0` and
`SetDbStatementForText=true`, every request and every EF span prints full SQL and request paths
to stdout, synchronously on the ending thread (`SimpleActivityExportProcessor`).

**Fix.** Fail `ObservabilitySettingsValidator` when `Enabled` is true, the endpoint is empty and
the environment is not Development. Keep the console fallback dev-only.

**Evidence.** Trace. `ObservabilitySettingsValidator.cs:28` guards the endpoint check behind
`!string.IsNullOrWhiteSpace(...)`; `Program.cs:67` calls `AddObservability` with no
`IHostEnvironment` guard; `appsettings.json:127` ships `Otlp:Endpoint = ""`.

---

## 🟠 F14 / D14 — Dashboard and SLO doc query metric names the API never emits

**File** `ops/dashboards/fototipar-overview.json:309`
**Convergence** 2 (requirements, completeness-critic) · **Verdict** confirmed

**Failure.** Panel 8 queries `anaf_submission_total{result}` while the code emits
`invoice_anaf_status_total{status}`. Panels 1/3/4 query `http_request_total{status_class}` and
panel 2 `http_request_duration_seconds_bucket`, while OTel emits
`http_server_request_duration_seconds` with `http_response_status_code`/`http_route`. Five of
eight panels and SLOs 1, 2 and 5 are permanently No Data. Nothing compares the two.

**Fix.** Rename to the emitted names, and extend `MetricsEndpointIntegrationTests` to emit one
observation per instrument, scrape `/metrics`, and assert every dashboard `expr` metric name
appears in the exposition.

**Evidence.** Trace. Scraping `/metrics` with `Observability:Enabled=true` after exercising
endpoints yields only `MetricNames` instruments plus OTel's
`http_server_request_duration_seconds_*`. No views or recording rules rename anything — `ops/`
contains only the dashboard.

---

## 🟠 F15 / D15 — Mapped 5xx and all `LogError` bypass Sentry

**File** `src/PhotoPrint.API/Middleware/ExceptionHandlerMiddleware.cs:141`
**Convergence** 1 (observability) · **Verdict** confirmed

**Failure.** `CaptureException` lives only in the middleware's *unmapped* branch. A
`BadGatewayException` from `GoogleTokenValidator` takes the mapped branch, which only
`LogWarning`s — so every Google sign-in 502 burns SLO 1 while Sentry sees nothing. Separately,
`UseSerilog` replaces the MEL providers and the sinks are File-only with no Sentry.Serilog
package, so `LogError` lines such as `sameday.awb.orphaned` never reach Sentry either.
`slos.md` names Sentry as the notification channel for all five SLOs.

**Fix.** Capture exceptions mapped to status ≥ 500 through the hub as well, and either bridge
Serilog Error events to Sentry or correct the `slos.md` channel table.

**Evidence.** Trace. `ExceptionHandlerMiddleware.cs:75` is the mapping branch (LogWarning only),
`:141` the unmapped branch (`hub?.CaptureException`). `GoogleTokenValidator.cs:43` throws
`BadGatewayException` on `HttpRequestException`. `SerilogExtensions` uses `UseSerilog` with
`writeToProviders` left at its default of false.

---

## 🟠 F16 / D16 — AwbCreator throw path skips the outcome metric

**File** `src/PhotoPrint.API/Services/Sameday/AwbCreator.cs:45`
**Convergence** 2 (observability, completeness-critic) · **Verdict** confirmed

**Failure.** `RecordOutcome` runs only on the normal return path. If the order load or the claim
`ExecuteUpdateAsync` throws — Postgres unreachable, say — the exception propagates past it to
`AwbDispatcher`, which only logs. The attempt enters neither numerator nor denominator, so the
dashboard's `awb_creation_total{result="ok"} / awb_creation_total` stays at 100% while no
labels are being created at all. No test covers the throw path.

**Fix.** Wrap the internal call in try/catch-rethrow and record `result="error"` (adding it to
`AwbResultValues` and `metrics.md`), with a test asserting the increment against a throwing DB.

**Evidence.** Trace. `AwbCreator.cs:75` (`FirstOrDefaultAsync`) and `:107` (claim
`ExecuteUpdateAsync`) both sit inside the try-less path that bypasses `RecordOutcome` at `:45`;
`AwbDispatcher.cs:70` only logs. Dashboard expression at
`ops/dashboards/fototipar-overview.json:270`.

---

## 🟠 F17 / D17 — Processing-duration histogram recorded before the commit

**File** `src/PhotoPrint.API/Services/AdminOrderService.cs:133`
**Convergence** 2 (correctness, race) · **Verdict** confirmed

**Failure.** `Record()` runs at line 133; `SaveChangesAsync` commits at line 148. Two ways this
goes wrong. (a) The admin's client disconnects, the cancellation token fires,
`SaveChangesAsync` throws, the scoped context is discarded and the order stays `Printing` — but
the histogram already holds an observation for a shipment that never happened, and the retry
adds a second. (b) A double-clicked Ship button sends two concurrent PATCHes; both scoped
contexts read `Printing`, both pass the status machine, both Record, both commit — there is no
concurrency token anywhere in this repo. Histograms are cumulative, so the count and the p95 on
the SLO dashboard are permanently wrong.

**Fix.** Compute the duration into a local and Record only after `SaveChangesAsync` returns.
Make the Shipped stamp a conditional write (`WHERE ShippedAt IS NULL`) and record only when it
affected a row.

**Evidence.** Trace. Confirmed no `RowVersion`/`IsConcurrencyToken` exists anywhere in the repo;
both interleavings walk cleanly through `AdminOrderService.cs:133-148`.

---

## 🟠 F18 / D18 — Sentry SDK failures are wholly silent

**File** `src/PhotoPrint.API/Program.cs:56` · **Convergence** 1 (observability) · **Verdict** confirmed

**Failure.** Sentry 4.13.0 nulls the `DiagnosticLogger` when `Debug` is false, which is the
shipped default. When the monthly quota is exhausted and ingest starts returning 429, every
event is dropped inside the SDK: nothing reaches Serilog, no metric counts the drops, no health
check covers reachability. Ops reads "no new Sentry issues" as "no errors" — the worst possible
failure mode for an error-tracking system.

**Fix.** Set `o.DiagnosticLogger` to a MEL/Serilog-backed logger at Warning regardless of
`Debug`, or add a dropped-event counter to `FotoMetrics` plus a Sentry-reachability health
check.

**Evidence.** Trace. `BeforeSend` runs before transport so it cannot count drops; health checks
registered are only `database` and `disk`; no Sentry metric exists. Latent — no checked-in
config enables Sentry yet.

---

## 🟠 F19 / D19 — Test factories leak process-wide env vars under parallel xUnit

**File** `src/PhotoPrint.Tests/Integration/SentryIntegrationFactory.cs:32`
**Convergence** 5 (correctness, requirements, race, tests-coverage, completeness-critic) · **Verdict** confirmed

**Failure.** A **static** constructor sets `Sentry__Enabled=true` and
`Sentry__Dsn=https://dummy@sentry.invalid/0` process-wide and never restores them; the
observability factory does the same with `Observability__Enabled=true`. xUnit runs test
*classes* in parallel by default and there is no `xunit.runner.json` or `CollectionBehavior`
attribute to stop it. Any `WebApplicationFactory` host constructed after those static
constructors run will boot the real Sentry SDK against `sentry.invalid` plus the OTel console
and EF exporters. Which hosts are affected depends on scheduling, so the 1001-green figure is
not reproducible run to run.

**Fix.** Set the variables in an instance constructor and restore them in `Dispose`, and put all
`WebApplicationFactory<Program>` classes in one non-parallel xUnit collection.

**Evidence.** Main-agent recheck: `SentryIntegrationFactory.cs:31-35` is the static constructor;
`ls src/PhotoPrint.Tests/xunit.runner.json` → absent;
`grep "CollectionBehavior|DisableTestParallelization"` → no hits. Accepted on 5-lens
convergence.

---

## 🟠 F20 / D20 — Cardinality tests are arithmetic over constants

**File** `src/PhotoPrint.Tests/Unit/Observability/MetricsCardinalityTests.cs:20`
**Convergence** 2 (quality, tests-coverage) · **Verdict** confirmed

**Failure.** The tests multiply `MetricNames.*Values` array lengths and check snake_case; they
never look at what a call site actually emits. Adding `{ "user_id", userId.ToString() }` to the
`TagList` in `OrderService.cs:186` creates unbounded series on `orders_created_total` — a scrape
memory blowup — and all six tests still pass. The asserted counts are already wrong:
`OrderService` and `AwbCreator` emit a literal `"unknown"` that is absent from the `All` arrays.

**Fix.** Drive the call sites through a `MeterListener`, collect the observed tag names and
values, and assert each observed value is a member of the enumerated set. Add `"unknown"` to
the `All` arrays.

**Evidence.** Trace (executed). No `TagList`/`MeterListener` match anywhere in
`src/PhotoPrint.Tests`; the injected `user_id` tag left all six tests green.

---

## 🟠 F21 / D21 — Scope-enricher unit tests never execute the enrichment body

**File** `src/PhotoPrint.Tests/Unit/Middleware/SentryScopeEnricherMiddlewareTests.cs:17`
**Convergence** 1 (tests-coverage) · **Verdict** confirmed

**Failure.** `NewContextWithoutSentry()` builds an empty `ServiceCollection`, so `IHub` resolves
to null and the entire `ConfigureScope` block is skipped in all three tests — they assert only
that `next` was called. The integration test's mock copies `scope.Tags` but never `scope.User`,
and its request is anonymous. So changing `ClaimTypes.NameIdentifier` to a claim that never
exists, or moving `UseSentryScopeEnricher` before `UseAuthentication`, reddens nothing while
every Sentry issue silently loses its user.

**Fix.** Add a unit test with a stub `IHub` (`IsEnabled=true`) over a real `Scope`, asserting
both the `correlation_id` tag and `scope.User.Id` for an authenticated principal.

**Evidence.** Trace (executed). Mutating the claim type to a nonexistent claim left
`dotnet test --filter FullyQualifiedName~Sentry` at 32/32. Mutation reverted.

---

## 🟠 F22 / D22 — Scrubber tests only exercise hand-built events

**File** `src/PhotoPrint.API/Configuration/SentryDataScrubbers.cs:39`
**Convergence** 1 (tests-coverage) · **Verdict** confirmed

**Failure.** Every scrubber test constructs its own `SentryEvent`. The fields the SDK actually
populates — `Request.QueryString`, `SentryExceptions`/`ex.Data`, `Contexts` — are never present
in a test, so the shapes that leak (F3) are exactly the shapes untested. The suite also runs EF
InMemory only, so no relational-provider exception carrying data in `ex.Data` is ever produced.

**Fix.** Add scrubber tests over an SDK-populated event (query string, `User`, `Contexts`,
exception `Data`), and redact sensitive query parameters by name, not just headers and `Extra`.

**Evidence.** Trace. `SentryIntegrationFactory` swaps `IHub` for a Moq fake, so `Scrub` never
runs on an SDK-shaped event in any test; `Scrub` at lines 41-63 touches only `Request.Data`,
headers and `Extra`.

---

## 🟠 F23 / D23 — Deployment runbook for `/metrics` and OTLP does not exist

**File** `src/PhotoPrint.API/appsettings.json:123`
**Convergence** 3 (requirements, quality, completeness-critic) · **Verdict** confirmed

**Failure.** The config comment points operators at `DEPLOYMENT.md §14`; the document ends at
§13 (Sentry). Nothing documents how to set `AllowedScrapeIps` behind Caddy, how to provision
the OTLP endpoint, or how to bound trace and scrape cost. An operator flipping
`Observability:Enabled=true` is doing it blind — and F1, F10 and F13 are all traps waiting on
that path.

**Fix.** Add `DEPLOYMENT.md §14` for bolt 044 mirroring §13 (flags, provisioning, allow-list
behind the proxy, rollout, cost, playbook), or correct the pointer.

**Evidence.** Accepted on 3-lens convergence; `docs/DEPLOYMENT.md` confirmed to end at §13.

---

## 🟡 F24 / D24 — Scope enricher registered after authentication

**File** `src/PhotoPrint.API/Program.cs:352` · **Convergence** 2 (correctness, observability) · **Verdict** confirmed

**Failure.** Pipeline order is `UseCorrelationId → UseGlobalExceptionHandler → …
UseSecurityBaselines → UseResponseCaching → UseRouting → UseAuthentication → UseAuthorization →
UseSentryScopeEnricher`. An exception thrown in rate limiting, response caching, routing or
authentication unwinds to the exception handler, which captures it — but the enricher, the only
source of the `correlation_id` and user tags, never ran. Those events cannot be joined to the
Serilog line for the same request, and no Serilog→Sentry sink exists to backfill them.

**Fix.** Split the enricher: set `correlation_id` immediately after `UseCorrelationId` (before
the exception handler), and add `user_id` in a second, post-auth pass.

**Evidence.** Trace over `Program.cs:327-352` and `ExceptionHandlerMiddleware.cs:141`.

---

## 🟡 F25 / D25 — EF spans ship full SQL and exception messages to OTLP unscrubbed

**File** `src/PhotoPrint.API/Extensions/ObservabilityExtensions.cs:62`
**Convergence** 2 (quality, completeness-critic) · **Verdict** confirmed

**Failure.** `SetDbStatementForText = true` attaches EF command text to every DB span and
`RecordException = true` attaches exception messages to request spans; both go to the OTLP
collector. EF 8 inlines some values (IN-list constants) into command text. `SentryDataScrubbers`
is wired only into Sentry's `BeforeSend` — a different pipeline — so nothing scrubs the OTLP
path at all. With `Sampling.Default = 1.0` and only two routes overridden, this runs at 100% on
most routes. This is a second egress path that the branch's PII review never looked at.

**Fix.** Default `SetDbStatementForText` to false or config-gate it, and add a span processor
redacting the same key set `SentryDataScrubbers` uses.

**Evidence.** Trace over `ObservabilityExtensions.cs:58-62`, `RouteAwareSampler.cs:50`,
`appsettings.json` sampling defaults, and `Program.cs:57` (scrubber scope).

---

## 🟡 F26 / D26 — `NaN` sample rates pass both validators

**File** `src/PhotoPrint.API/Validators/ObservabilitySettingsValidator.cs:46`
**Convergence** 1 (input-validation) · **Verdict** confirmed

**Failure.** `Observability__Sampling__Default=NaN` binds fine — `double.Parse` accepts `"NaN"`.
The check `Default is < 0.0 or > 1.0` is false for `NaN` on both comparisons, so validation
passes. In the sampler, `rate >= 1.0` is false, `rate <= 0.0` is false, and `ratio < NaN` is
always false, so **every** trace is dropped, silently. `SentrySettingsValidator` has the same
shape: `Sentry:SampleRate=NaN` passes and drops every event.

**Fix.** Add `double.IsFinite` checks to both validators for `Sampling:Default`, the per-route
rates, `Sentry:SampleRate` and `Sentry:TracesSampleRate`.

**Evidence.** Trace (executed). Main-agent recheck confirms
`ObservabilitySettingsValidator.cs:46` is a bare `is < 0.0 or > 1.0` range test.

---

## 🟡 F27 / D27 — `PrometheusEndpoint="/"` would gate the whole site

**File** `src/PhotoPrint.API/Validators/ObservabilitySettingsValidator.cs:36`
**Convergence** 1 (input-validation) · **Verdict** confirmed

**Failure.** The only check is `StartsWith('/')`, which `"/"` satisfies. `Program.cs` then wires
`UseWhen(ctx.Request.Path.StartsWithSegments("/"))`, which matches every path — so every request
from a non-allow-listed IP (that is, all real traffic) gets a bodyless 403, and the exporter
serves metrics at the site root.

**Fix.** Require a single non-root segment: length > 1, no whitespace, no query or fragment
characters, and reject `"/"` explicitly.

**Evidence.** Trace over `ObservabilitySettingsValidator.cs:36-40` and `Program.cs:74, 360-361`.

---

## 🟡 F28 / D28 — `ValidateOnStart` wiring is untested

**File** `src/PhotoPrint.API/Program.cs:72` · **Convergence** 2 (tests-coverage, completeness-critic) · **Verdict** confirmed

**Failure.** All 11 validator tests call `new ObservabilitySettingsValidator().Validate(...)`
directly. No test boots the host through the real
`AddOptions<T>().ValidateOnStart()` wiring, so if that registration regresses nothing catches
it. Combined with a blank `PrometheusEndpoint`, `metricsPath` becomes `""`,
`StartsWithSegments("")` matches every path, and the allow-list middleware 403s the entire API
— with the suite fully green throughout.

**Fix.** Add an integration test that boots the host with `Enabled=true` and an invalid setting
and asserts `OptionsValidationException`; fall back to `/metrics` when the configured path is
blank.

**Evidence.** Trace over `ObservabilityExtensions.cs:36-37`, `Program.cs:72-74`, and the
validator test files.

---

## 🟡 F29 / D29 — Enricher sets `scope.User.Id`, not the `user_id` tag the AC requires

**File** `src/PhotoPrint.API/Middleware/SentryScopeEnricherMiddleware.cs:33`
**Convergence** 1 (requirements) · **Verdict** confirmed

**Failure.** The acceptance criterion and the implementation plan both require a `user_id`
**tag** on every event. The middleware sets `scope.User` instead, so Sentry search and alert
rules filtering on `tag:user_id` match nothing.

**Fix.** Also `SetTag("user_id", userId)` alongside `scope.User`, and assert it in the
integration test's captured tags.

**Evidence.** Trace over lines 31-33; no `SetTag("user_id", ...)` exists. Confirmed untested by
F21's finding that all three unit tests skip the block.

---

## 🟡 F30 / D30 — Sampler startup log (story 003 AC) not implemented

**File** `src/PhotoPrint.API/Observability/Sampling/RouteAwareSampler.cs:40`
**Convergence** 1 (requirements) · **Verdict** confirmed

**Failure.** Nothing in `AddObservability` or the sampler logs anything; only
`Sampler.Description` is set. `ddd-02:384` claims "the constructor logs the resolved table
once" and `ddd-03:56` claims the OTel SDK surfaces it in startup logs (it emits only the sampler
type, to an EventSource). `ddd-01:133`'s "unknown route logged at Debug once per route" is also
absent. An operator cannot confirm which rates loaded — which is how F5 stayed invisible.

**Fix.** Log the resolved default rate and route table once from `AddObservability` at
Information, and correct `ddd-02`/`ddd-03`.

**Evidence.** Trace: grep for `Log`/`logger` across `RouteAwareSampler.cs` and
`ObservabilityExtensions.cs` returns nothing.

---

## 🟡 F31 / D31 — Neither subsystem logs its enabled state at boot

**File** `src/PhotoPrint.API/Program.cs:48` · **Convergence** 1 (observability) · **Verdict** confirmed

**Failure.** A staging deploy that omits `Observability__Enabled` and `Sentry__Enabled` boots
clean with no tracing, no `/metrics`, and no error capture — and not one log line says so. The
gap surfaces only when an incident produces no Sentry issue.

**Fix.** Log one Information line per subsystem at boot: enabled/disabled, service name, OTLP
endpoint or console fallback, metrics path, allow-list size, Sentry environment and release.

**Evidence.** Trace: `Program.cs:38-75` reads both flags and never logs them; the only boot logs
are the SQLite schema warning (`:258`) and the Postgres migrate line (`:277`).

---

## 🟡 F32 / D32 — Unsynchronized capture collections in the shared test fixture

**File** `src/PhotoPrint.Tests/Integration/SentryIntegrationFactory.cs:38`
**Convergence** 1 (race) · **Verdict** **plausible**

**Failure.** `CapturedEvents` (`List<SentryEvent>`) and `CapturedTags` (`Dictionary<string,string>`)
are mutated from Moq callbacks running on request-handling threads, with no lock and no
concurrent collection, while the factory is shared via `IClassFixture`.

**Why plausible, not confirmed.** The skeptic could not build a trace against current code: the
class has exactly one `[Fact]` issuing one awaited request, and xUnit runs methods within a
class sequentially. It is a real design defect that requires a future concurrent-request test to
manifest — the finding's own text concedes "safe today".

**Fix.** Use `ConcurrentBag<SentryEvent>` and `ConcurrentDictionary<string,string>`, or lock
around the callback bodies.

---

## ⚪ F33 / D33 — Magic `"unknown"` label value escapes the `MetricNames` contract

**File** `src/PhotoPrint.API/Services/OrderService.cs:184` · **Convergence** 2 (requirements, quality) · **Verdict** **plausible**

**Severity reduced low → cleanup at synthesis.** The lens scenario — dashboard queries
filtering on enumerated values silently dropping samples — cannot occur, because the skeptic
proved both switch defaults are unreachable: `PaymentProcessor` has only `Stripe`/`EuPlatesc`
and `CreateOrderRequestValidator` applies `.IsInEnum()` with 422 rejection before
`OrderService.CreateAsync` runs, and `AwbCreationOutcome` is a closed set of four sealed records
all matched by name. No input emits `"unknown"`. What remains is real but non-behavioural: a
label value that exists in code, is absent from `MetricNames.*Values`, the docs and the
cardinality budget, and would become live the moment either enum grows.

**Fix.** Add an `Unknown` constant to `ProcessorValues`/`AwbResultValues`, include it in the
`All` arrays, and reference it from both switch defaults.

---

## ⚪ F34 / D34 — Doc-comment blocks on concrete classes citing bolt/ADR IDs

**File** `src/PhotoPrint.API/Observability/FotoMetrics.cs:5` · **Convergence** 1 (quality) · **Verdict** unverified-cleanup

CLAUDE.md permits `///` blocks only on **interface** members and forbids references to bolts,
ADRs, reviews, findings or stories. `FotoMetrics`, `MetricNames`, `RouteAwareSampler`,
`ErrorOverrideProcessor`, both new middlewares, `SentryDataScrubbers` and
`ObservabilityExtensions` break both halves — in the same branch whose other commit strips such
citations out of `OrderService` and `UploadService`. Confirmed by main-agent reading:
`MetricsEndpointIpAllowListMiddleware` says "see the ADR for the reasoning",
`RouteAwareSampler` says "per the technical design", `SentryDataScrubbers` says "gets a CR".

**Fix.** Cut each to one short why-line, drop the references, and leave the rationale in the
bolt docs that already hold it.

---

## ⚪ F35 / D35 — Comment-sweep residue

**File** `src/PhotoPrint.API/Program.cs:139` · **Convergence** 1 (quality) · **Verdict** unverified-cleanup

Main-agent recheck confirms all three sites. `Program.cs:139` reads
`bombs (bolt 042, story 003 AC#1 /). The per-image pixel-area guard` — a dangling `/` where a
citation was spliced out, and the surviving `bolt 042, story 003 AC#1` is itself the kind of
citation the rule forbids (as is `bolt 043: two-tier router + S3 adapter` two lines above).
`OrderService.cs:394` and `UploadService.cs:208` were left as ~130-column run-on lines.

**Cross-target note.** These lines come from the comment sweep (`09173c4`), which belongs to the
`system` target's loop, not to bolts 044/045. Recorded here because this branch carries them;
the fixer should decide which target owns the fix.

---

## ⚪ F36 / D36 — `ddd-02` contradicts ADR-017 within the same bolt

**File** `memory-bank/bolts/044-tracing-and-metrics/ddd-02-technical-design.md:195` · **Convergence** 1 (requirements) · **Verdict** unverified-cleanup

The NFR row states the sampling decision is "a single `Random.Shared.NextDouble() < rate`" over
a `FrozenDictionary`, while ADR-017 forbids `Random` in the sampling path and the code uses a
plain `Dictionary` plus trace-id hashing. A maintainer reading the design doc first implements
exactly what the ADR bans.

**Fix.** Correct the `ddd-02` NFR row to describe the trace-id hash and the plain dictionary,
pointing at ADR-017.

---

## ⚪ F37 / D37 — Metric vocabulary shipped ahead of any emission

**File** `src/PhotoPrint.API/Observability/MetricNames.cs:74` · **Convergence** 1 (quality) · **Verdict** unverified-cleanup

`FotoMetrics.InvoiceAnafStatus` and `MetricNames.AnafStatusValues` have no increment site — they
are reserved for intent 016 — yet are pinned by two test files. `OrderStatusValues.Paid` and
`Cancelled` are likewise never emitted, making `orders_created_total`'s `status` label a
constant `"created"`: a label dimension carrying zero information.

**Fix.** Delete the ANAF instrument and its values until intent 016 emits them; drop the
constant `status` label, or emit the transitions that would justify it.

---

## ⚪ F38 / D38 — Observability config re-read by string key after binding

**File** `src/PhotoPrint.API/Program.cs:72` · **Convergence** 1 (quality) · **Verdict** unverified-cleanup

`Program.cs:69-74` re-reads the Observability section by string key (`"Enabled"`,
`"Metrics:PrometheusEndpoint"`) with a `?? "/metrics"` fallback duplicating
`ObservabilityMetricsSettings.PrometheusEndpoint`'s default. Change the default in the settings
class and the Program.cs fallback silently disagrees. The section is bound four times at boot.

**Fix.** Have `AddObservability` return the bound `ObservabilitySettings` (or expose it via
`IOptions`), and read both values from that single instance.

---

## ⚪ F39 / D39 — Sentry wiring inlined in `Program.cs` at a different altitude from bolt 044

**File** `src/PhotoPrint.API/Program.cs:29` · **Convergence** 1 (quality) · **Verdict** unverified-cleanup

Lines 29-61 hand-roll `Configure` + validator + `ValidateOnStart` + flag read + `UseSentry` +
middleware registration, spelling `PhotoPrint.API.Configuration.SentrySettings` six times
despite the `using` already present at line 5. Bolt 044 got an `AddObservability` extension;
bolt 045 did not.

**Fix.** Extract to `Extensions/SentryExtensions.cs` mirroring `AddObservability`, and drop the
redundant namespace qualification.
