---
type: review
target: 044-045-observability
version: 1
supersedes: null
commit: 5cac465
branch: feat/bolt-045-error-tracking-slos
pass-type: discovery
date: 2026-07-31
lenses: [correctness, security, requirements, quality, input-validation, observability, race, tests-coverage, completeness-critic]
lenses-not-run: [db-parity, frontend-ux]
verdict: request-changes
blockers: [PPW-336, PPW-337, PPW-338, PPW-339, PPW-340, PPW-341, PPW-342, PPW-343, PPW-344]
findings: { high: 9, medium: 14, low: 9, cleanup: 7, refuted: 0 }
tests: { dotnet: "1001/1001 (+10 skipped MinIO)", frontend: "460/460" }
---

# Review v1 — 044-045-observability

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-336 | 🔴 | `/metrics` allow-list checks the TCP peer, which behind Caddy is the proxy IP | `Middleware/MetricsEndpointIpAllowListMiddleware.cs:41` | yes |
| PPW-337 | 🔴 | Sentry transactions bypass the scrubber (`SetBeforeSendTransaction` absent) | `Program.cs:57` | yes |
| PPW-338 | 🔴 | Scrubber never touches `Request.QueryString`/`Url` — emails and tokens ship | `Configuration/SentryDataScrubbers.cs:44` | yes |
| PPW-339 | 🔴 | Case-sensitive header scrubbing — HTTP/2 lowercase names survive | `Configuration/SentryDataScrubbers.cs:46` | yes |
| PPW-340 | 🔴 | Per-route sample rates can never match; every route uses `Default` | `Observability/Sampling/RouteAwareSampler.cs:63` | yes |
| PPW-341 | 🔴 | "Errors always sampled" is dead code — `OnEnd` skipped for dropped spans | `Observability/ErrorOverrideProcessor.cs:18` | yes |
| PPW-342 | 🔴 | Webhook fall-through / re-delivery branches record no metric and no log | `Controllers/WebhooksController.cs:216` | yes |
| PPW-343 | 🔴 | Sentry e2e test mocks `IHub`, so the scrubber never runs in any test | `Tests/Integration/SentryIntegrationFactory.cs:85` | yes |
| PPW-344 | 🔴 | No test observes any business metric being emitted | `Tests/Integration/MetricsEndpointIntegrationTests.cs:50` | yes |
| PPW-345 | 🟠 | Unparseable allow-list entries silently dropped; validator too weak | `Middleware/MetricsEndpointIpAllowListMiddleware.cs:33` | yes |
| PPW-346 | 🟠 | Middleware registered `Scoped` — deny-log dedupe never fires | `Extensions/ObservabilityExtensions.cs:50` | yes |
| PPW-347 | 🟠 | IPv4-mapped IPv6 peers never match IPv4 allow-list entries | `Middleware/MetricsEndpointIpAllowListMiddleware.cs:42` | yes |
| PPW-348 | 🟠 | Empty `Otlp:Endpoint` silently enables the console span exporter in production | `Extensions/ObservabilityExtensions.cs:78` | yes |
| PPW-349 | 🟠 | Dashboard and `slos.md` query metric names the API never emits | `ops/dashboards/fototipar-overview.json:309` | yes |
| PPW-350 | 🟠 | Mapped 5xx and all Serilog `LogError` bypass Sentry | `Middleware/ExceptionHandlerMiddleware.cs:141` | yes |
| PPW-351 | 🟠 | AwbCreator throw path skips `RecordOutcome` | `Services/Sameday/AwbCreator.cs:45` | yes |
| PPW-352 | 🟠 | Processing-duration histogram recorded before `SaveChanges`, no once-only guard | `Services/AdminOrderService.cs:133` | yes |
| PPW-353 | 🟠 | Sentry SDK failures wholly silent (`Debug=false` ⇒ no `DiagnosticLogger`) | `Program.cs:56` | yes |
| PPW-354 | 🟠 | Test factories set process-wide env vars in static ctors under parallel xUnit | `Tests/Integration/SentryIntegrationFactory.cs:32` | yes |
| PPW-355 | 🟠 | Cardinality tests are arithmetic over constants | `Tests/Unit/Observability/MetricsCardinalityTests.cs:20` | yes |
| PPW-356 | 🟠 | Scope-enricher unit tests run with no `IHub`; body never executes | `Tests/Unit/Middleware/SentryScopeEnricherMiddlewareTests.cs:17` | yes |
| PPW-357 | 🟠 | Scrubber tests only exercise hand-built events, never SDK-populated ones | `Configuration/SentryDataScrubbers.cs:39` | yes |
| PPW-358 | 🟠 | `DEPLOYMENT.md §14` referenced by config does not exist | `appsettings.json:123` | yes |
| PPW-359 | 🟡 | Scope enricher registered after auth — pre-auth failures reach Sentry untagged | `Program.cs:352` | no |
| PPW-360 | 🟡 | EF spans ship full SQL and exception messages to OTLP unscrubbed | `Extensions/ObservabilityExtensions.cs:62` | no |
| PPW-361 | 🟡 | `NaN` sample rates pass both validators and silently drop everything | `Validators/ObservabilitySettingsValidator.cs:46` | no |
| PPW-362 | 🟡 | `PrometheusEndpoint="/"` passes validation and would gate the whole site | `Validators/ObservabilitySettingsValidator.cs:36` | no |
| PPW-363 | 🟡 | `ValidateOnStart` wiring untested | `Program.cs:72` | no |
| PPW-364 | 🟡 | Enricher sets `scope.User.Id` instead of the required `user_id` tag | `Middleware/SentryScopeEnricherMiddleware.cs:33` | no |
| PPW-365 | 🟡 | Sampler startup log (story 003 AC) not implemented | `Observability/Sampling/RouteAwareSampler.cs:40` | no |
| PPW-366 | 🟡 | Neither subsystem logs its enabled state at boot | `Program.cs:48` | no |
| PPW-367 | 🟡 | Unsynchronized capture collections in the shared test fixture | `Tests/Integration/SentryIntegrationFactory.cs:38` | no |
| PPW-368 | ⚪ | Magic `"unknown"` label escapes `MetricNames`, docs and the cardinality budget | `Services/OrderService.cs:184` | no |
| PPW-369 | ⚪ | `///` blocks on concrete classes citing bolt/ADR/story IDs | `Observability/FotoMetrics.cs:5` | no |
| PPW-370 | ⚪ | Comment-sweep residue: dangling `/`, surviving bolt citations, run-on lines | `Program.cs:139` | no |
| PPW-371 | ⚪ | `ddd-02` describes the `Random` approach ADR-017 forbids | `memory-bank/bolts/044-tracing-and-metrics/ddd-02-technical-design.md:195` | no |
| PPW-372 | ⚪ | Metric vocabulary shipped ahead of emission (ANAF; constant `status` label) | `Observability/MetricNames.cs:74` | no |
| PPW-373 | ⚪ | Observability config re-read by string key after binding; duplicated default | `Program.cs:72` | no |
| PPW-374 | ⚪ | Sentry wiring inlined in `Program.cs` while bolt 044 got an extension method | `Program.cs:29` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| none | — |

## Notes for the fixer

- PPW-336 is a deployment-topology fix, not only a code fix. Trusting `X-Forwarded-For` alone is worse than the current state unless the edge also strips and sets that header. Blocking `/metrics` at Caddy, or binding the exporter to a separate internal port, is the safer shape.
- PPW-337, PPW-338 and PPW-339 share one root cause: the scrubber is a partial allow-list applied at one of the two SDK hooks. Fix them as one cluster; consider the inverse posture — scrub everything, re-add what triage needs. Fixing them one at a time invites a fourth.
- PPW-340 and PPW-341 interact. Making the sampler apply low per-route rates increases the number of dropped spans, which worsens PPW-341. Fix PPW-341 first, or fix both together.
- Regression tests are the point, not an afterthought. PPW-343, PPW-344, PPW-355 and PPW-356 all say the existing tests pass on fabricated inputs. Any fix whose test does not redden when the fix is reverted is unproven — the definition-of-done rule.
