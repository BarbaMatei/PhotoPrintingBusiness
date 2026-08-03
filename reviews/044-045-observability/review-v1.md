---
type: review
target: 044-045-observability
version: 1
supersedes: null
commit: 5cac465
branch: feat/bolt-045-error-tracking-slos
pass-type: discovery
date: 2026-07-31
reviewer: multi-lens (full discovery, 9 of 11 manifest lenses)
lenses: [correctness, security, requirements, quality, input-validation, observability, race, tests-coverage, completeness-critic]
lenses-not-run: [db-parity, frontend-ux]
verdict: request-changes
blockers: [F1, F2, F3, F4, F5, F6, F7, F8, F9]
findings: { high: 9, medium: 14, low: 9, cleanup: 7, refuted: 0 }
tests: { dotnet: "1001/1001 (+10 skipped MinIO)", frontend: "460/460" }
---

# Review v1 — 044-045-observability (full discovery pass)

**Scope.** Bolts **044** (OTel tracing, Prometheus `/metrics`, per-route sampling) and **045**
(Sentry SDK, PII scrubbing, SLO doc, Grafana dashboard) of intent 020, as they stand at
`5cac465` on `feat/bolt-045-error-tracking-slos`. 22 files under `src/PhotoPrint.API`, 11 test
files, plus the ADRs, `memory-bank/operations/{slos,metrics}.md`, `docs/DEPLOYMENT.md` and
`ops/dashboards/fototipar-overview.json`. Backend only — the bolts touch no frontend file.

**Deliberately out of scope.** Two other bodies of work ride the same branch and were **not**
reviewed here: the repo-wide comment-citation sweep (`09173c4`, 124 files) and the
review-system changes under `reviews/**` and `.claude/skills/`. Both belong to the `system`
target's open loop. Two findings below (**F34**, **F35**) nonetheless land on sweep residue
inside bolt-044/045 files and adjacent hot-path files — recorded here because this branch
carries them.

**Entry tier: full loop** (owner's call, 2026-07-31). The change adds a new publicly-routed
HTTP endpoint whose only access control is network identity, and a new egress path that ships
application data to a third party — the README's "auth · new external input" row.

**Pass composition.** Nine blinded lenses in one parallel batch → in-pass dedup → convergence-
weighted adversarial verify ([discovery-review.wf.js](../lib/discovery-review.wf.js)), run
`wf_abe949a7-8af`. 81 raw findings → 39 canonical. Skeptics: 2 guard + 25 trace (a flat
2-per-finding policy would have been 66); 0 re-raises, 0 budget-skipped. 37 agents, ~2.26M
tokens. The main agent synthesized and independently re-checked the seven serious findings
that skipped skeptics on convergence alone (F1, F5, F6, F7, F10, F11, F19) — all held.

**Manifest lenses not run.** `db-parity` and `frontend-ux`: the bolts add no migration and
change no UI file. They are **owed, not waived** — a certification pass must fold them in if
either surface is later touched.

**Verdict: `request-changes`.** Nine confirmed **High** findings. They fall into three groups:

1. **The `/metrics` endpoint is not actually protected in the shipped topology** (F1). The
   allow-list compares `HttpContext.Connection.RemoteIpAddress`, which behind the repo's own
   Caddy edge is always the proxy's container IP. Allow-listing it — the only way to make
   scraping work — opens the endpoint to every anonymous caller on the internet.
2. **Three independent paths ship secrets and customer PII to Sentry** (F2, F3, F4), each
   reproduced live against the real SDK by a skeptic. Latent only because `Sentry:Enabled` is
   `false` today; all three fire on the flag flip.
3. **Both headline features are inoperative, and the tests cannot see it** (F5, F6, F7, F8,
   F9). Per-route sampling never matches a configured route; "errors are always sampled" is
   dead code; whole payment-webhook branches record nothing. No test observes a single metric
   emission, and the one Sentry end-to-end test mocks `IHub`, so the scrubber never runs in
   any test. Two skeptics proved this by deletion: removing `SetBeforeSend` +
   `SendDefaultPii=false` from Program.cs, and removing `FotoMetrics.OrdersCreated.Add` from
   OrderService, both leave the suite green.

**On the green suite.** 1001 backend + 460 frontend tests pass at this commit, and that number
carries less information than usual here. The suite is also **order-dependent** (F19): the new
Sentry/observability test factories set process-wide environment variables in static
constructors and never restore them, while xUnit runs test classes in parallel with no
`xunit.runner.json` to stop it.

## Findings

Ranked by severity, then by how directly the defect bites. `Conv` = independently agreeing
lenses. `Verdict` is the script's adversarial-verify outcome.

### 🔴 High

| ID | File | Title | Conv | Verdict |
|---|---|---|---|---|
| F1 | `Middleware/MetricsEndpointIpAllowListMiddleware.cs:41` | Allow-list checks the TCP peer, which behind the shipped Caddy proxy is the proxy IP — allow-listing it opens `/metrics` to everyone | 3 | confirmed |
| F2 | `Program.cs:57` | Scrubber wired only via `SetBeforeSend`; Sentry **transactions** bypass it and ship raw `X-Guest-Token` headers and query strings (shipped `TracesSampleRate=0.1`) | 1 | confirmed |
| F3 | `Configuration/SentryDataScrubbers.cs:44` | Scrubber never touches `Request.QueryString`/`Url` — admin order-search emails and email-confirmation tokens ship to Sentry | 2 | confirmed |
| F4 | `Configuration/SentryDataScrubbers.cs:46` | Header matching is case-sensitive, so over HTTP/2 (lowercase field names) `x-guest-token` and `cookie` survive scrubbing | 2 | confirmed |
| F5 | `Observability/Sampling/RouteAwareSampler.cs:63` | Per-route rates can never match: no `http.route` tag exists at sampling time, so the key falls back to the activity name — every route samples at `Default` | 5 | confirmed |
| F6 | `Observability/ErrorOverrideProcessor.cs:18` | "Errors are always sampled" is dead code — `OnEnd` never runs for spans the sampler dropped; the processor has no test at all | 4 | confirmed |
| F7 | `Controllers/WebhooksController.cs:216` | Webhook fall-through, re-delivery and unhandled-type branches increment no `payment_webhook_total` and log nothing above Debug — a charged-but-unpaid order is invisible and SLO 3 still reads 100% | 4 | confirmed |
| F8 | `Tests/Integration/SentryIntegrationFactory.cs:85` | The one Sentry end-to-end test mocks `IHub`, so `SetBeforeSend` and `SendDefaultPii=false` never execute — proven by deleting both and staying green | 2 | confirmed |
| F9 | `Tests/Integration/MetricsEndpointIntegrationTests.cs:50` | No test observes any business metric being emitted — every `FotoMetrics` call site can be deleted and the suite stays green | 2 | confirmed |

### 🟠 Medium

| ID | File | Title | Conv | Verdict |
|---|---|---|---|---|
| F10 | `Middleware/MetricsEndpointIpAllowListMiddleware.cs:33` | Unparseable allow-list entries (CIDR, whitespace-padded) are silently dropped; the validator only checks the list is non-empty, so `["10.0.0.0/8"]` blacks out scraping with no error | 5 | confirmed |
| F11 | `Extensions/ObservabilityExtensions.cs:50` | Middleware registered `Scoped`, so the allow-list is re-parsed per request and the once-per-IP deny-log dedupe never fires — a misconfigured scraper logs 4 lines/minute forever | 6 | confirmed |
| F12 | `Middleware/MetricsEndpointIpAllowListMiddleware.cs:42` | IPv4-mapped IPv6 peers (`::ffff:10.42.0.5`, the dual-mode-socket default) never match IPv4 allow-list entries — correctly-configured scraping 403s | 2 | confirmed |
| F13 | `Extensions/ObservabilityExtensions.cs:78` | `Enabled=true` with the shipped empty `Otlp:Endpoint` passes validation and turns on the **console** span exporter in production — full SQL to stdout, synchronously on the request thread | 2 | confirmed |
| F14 | `ops/dashboards/fototipar-overview.json:309` | Dashboard and `slos.md` query metric names the API never emits (`anaf_submission_total`, `http_request_total`, `status_class`) — 5 of 8 panels and SLOs 1, 2, 5 are permanently No Data | 2 | confirmed |
| F15 | `Middleware/ExceptionHandlerMiddleware.cs:141` | Mapped 5xx (e.g. `BadGatewayException` → 502) and every Serilog `LogError` bypass Sentry entirely, contradicting `slos.md`'s notification-channel table | 1 | confirmed |
| F16 | `Services/Sameday/AwbCreator.cs:45` | A thrown exception skips `RecordOutcome`, so `awb_creation_total` misses every failure — SLO 4 reads 100% healthy during a total AWB outage | 2 | confirmed |
| F17 | `Services/AdminOrderService.cs:133` | `order_processing_duration_seconds` recorded **before** `SaveChanges`, with no once-only guard — a cancelled or double-clicked Ship permanently over-counts a monotonic histogram | 2 | confirmed |
| F18 | `Program.cs:56` | Sentry SDK failures are wholly silent (`Debug=false` ⇒ no `DiagnosticLogger`) — a 429 quota exhaustion drops every event with no log, no metric, no health check | 1 | confirmed |
| F19 | `Tests/Integration/SentryIntegrationFactory.cs:32` | Test factories set process-wide env vars in static constructors and never restore them; xUnit runs classes in parallel, so which hosts boot the real Sentry SDK changes run to run | 5 | confirmed |
| F20 | `Tests/Unit/Observability/MetricsCardinalityTests.cs:20` | Cardinality-budget tests are arithmetic over constants — adding an unbounded `user_id` tag to a real call site leaves all six green | 2 | confirmed |
| F21 | `Tests/Unit/Middleware/SentryScopeEnricherMiddlewareTests.cs:17` | All three enricher unit tests run with no `IHub`, so the enrichment body never executes and nothing is asserted | 1 | confirmed |
| F22 | `Configuration/SentryDataScrubbers.cs:39` | Scrubber tests only exercise hand-built events; SDK-populated fields (query string, `ex.Data`, contexts) are never tested | 1 | confirmed |
| F23 | `appsettings.json:123` | No deployment runbook for `/metrics` or OTLP — the config comment points operators at `DEPLOYMENT.md §14`, and the document ends at §13 | 3 | confirmed |

### 🟡 Low

| ID | File | Title | Conv | Verdict |
|---|---|---|---|---|
| F24 | `Program.cs:352` | Scope enricher registered after auth, so pre-auth failures reach Sentry with no `correlation_id` or `user_id` and cannot be joined to the Serilog line | 2 | confirmed |
| F25 | `Extensions/ObservabilityExtensions.cs:62` | EF spans carry full SQL (`SetDbStatementForText=true`) and exception messages to OTLP unscrubbed — a second egress path no scrubber covers, at 100% default sampling | 2 | confirmed |
| F26 | `Validators/ObservabilitySettingsValidator.cs:46` | `NaN` sample rates pass both validators (`NaN < 0.0` and `NaN > 1.0` are both false) and silently drop every trace / every Sentry event | 1 | confirmed |
| F27 | `Validators/ObservabilitySettingsValidator.cs:36` | `PrometheusEndpoint="/"` passes validation and would gate the entire site behind the scrape allow-list | 1 | confirmed |
| F28 | `Program.cs:72` | Validators are only ever invoked directly; the `AddOptions().ValidateOnStart()` boot-abort wiring is untested, and a blank endpoint would lock out the whole API | 2 | confirmed |
| F29 | `Middleware/SentryScopeEnricherMiddleware.cs:33` | Sets `scope.User.Id` instead of the `user_id` **tag** the acceptance criterion requires — Sentry alert rules filtering on that tag match nothing | 1 | confirmed |
| F30 | `Observability/Sampling/RouteAwareSampler.cs:40` | Story 003's "sampler choice logged once at startup with the resolved table" is not implemented; `ddd-02` and `ddd-03` claim it is | 1 | confirmed |
| F31 | `Program.cs:48` | Neither Sentry nor the OTel stack logs its enabled/disabled state at boot — a deploy that omits both flags is silently blind | 1 | confirmed |
| F32 | `Tests/Integration/SentryIntegrationFactory.cs:38` | Shared-fixture capture collections are an unsynchronized `List`/`Dictionary` written from request threads — safe today, breaks on the first concurrent-request test | 1 | plausible |

### ⚪ Cleanup

| ID | File | Title | Conv | Verdict |
|---|---|---|---|---|
| F33 | `Services/OrderService.cs:184` | Magic `"unknown"` label value bypasses `MetricNames`, the docs and the cardinality budget. **Severity reduced low→cleanup at synthesis:** the skeptic proved both switch defaults unreachable (`.IsInEnum()` validation upstream; `AwbCreationOutcome` is a closed set), so the "samples silently dropped" scenario cannot occur — the governance gap is real, the failure is not | 2 | plausible |
| F34 | `Observability/FotoMetrics.cs:5` | New observability files carry multi-paragraph `///` blocks on **concrete** classes citing bolt/ADR/story IDs — both halves of the CLAUDE.md comment rule, in the same branch that strips such citations elsewhere | 1 | unverified-cleanup |
| F35 | `Program.cs:139` | Comment sweep left mangled residue: a dangling `/` where a citation was spliced out (and two surviving bolt-ID citations), plus ~130-column run-on lines at `OrderService.cs:394` and `UploadService.cs:208` | 1 | unverified-cleanup |
| F36 | `memory-bank/bolts/044-tracing-and-metrics/ddd-02-technical-design.md:195` | `ddd-02` describes `Random.Shared.NextDouble()` over a `FrozenDictionary` — the exact approach ADR-017 forbids, in the same bolt | 1 | unverified-cleanup |
| F37 | `Observability/MetricNames.cs:74` | Metric vocabulary shipped ahead of emission (ANAF instrument reserved for intent 016; `OrderStatusValues.Paid/Cancelled` never emitted, making `status` a constant label) | 1 | unverified-cleanup |
| F38 | `Program.cs:72` | Observability config re-read by string key after `AddObservability` already bound it, with a duplicated `?? "/metrics"` default that can silently disagree | 1 | unverified-cleanup |
| F39 | `Program.cs:29` | Sentry wiring inlined in `Program.cs` with fully-qualified names while bolt 044 got an extension method — two sibling bolts at two wiring altitudes | 1 | unverified-cleanup |

## Reconciliation

First pass on a new target: `ledger.md` did not exist, so there was nothing to match against
and the `reconcile-findings` skill had no work to do. All 39 findings are minted new as
**D1–D39**, mapping 1:1 onto **F1–F39** in the order above. From v2 onward the reconciler runs
normally.

## Notes for the fixer

- **F1 is a deployment-topology fix, not only a code fix.** The code change alone (trusting
  `X-Forwarded-For`) would be worse than the current state unless the edge is also made to
  strip and set that header. Blocking `/metrics` at Caddy, or binding the exporter to a
  separate internal port, is the safer shape.
- **F2/F3/F4 are one cluster with one root cause:** the scrubber is a partial allow-list
  applied at one of the two SDK hooks. Fixing them one at a time invites a fourth. Consider
  the inverse posture — scrub everything, re-add what triage needs.
- **F5 and F6 interact.** Fixing the sampler to actually apply low per-route rates will
  *increase* the number of dropped spans, which makes F6's silently-lost error traces worse.
  Fix F6 first, or fix both together.
- **Regression tests are the point here, not an afterthought.** F8, F9, F20 and F21 all say
  the same thing: the existing tests pass on fabricated inputs. Any fix whose test does not
  redden when the fix is reverted has not been proven — that is the definition-of-done rule,
  and this pass found four places it was not met.
