---
type: resolution
target: 044-045-observability
version: 1
answers: review-v1.md
status: resolved
fixed_commit: e965c99
closed: 2026-08-04
findings:
  D1:  { status: fixed, commit: "9fb6858, a054fdd", note: "/metrics now served only on an unproxied scrape listener (Observability:Metrics:ScrapePort; other listeners 404); Caddyfile refuses /metrics* at the edge. ADR-018 amended. Approach-check changed the design (Decisions)." }
  D2:  { status: fixed, commit: 44c3e2d, note: "SentryDataScrubbers.Register wires all three egress hooks (BeforeSend, BeforeSendTransaction, BeforeBreadcrumb); test asserts on real SentryClient envelopes via a stub ITransport. A throwing hook now drops the payload (Decisions)." }
  D3:  { status: fixed, commit: "44c3e2d, bea8c98", note: "Query-string values redacted (parameter names kept); URL query, fragment and credentials stripped; applied to Request.Url, span descriptions and breadcrumb URLs." }
  D4:  { status: fixed, commit: 44c3e2d, note: "Header matching replaced by a case-insensitive allow-list, so lowercase HTTP/2 names cannot leak by omission; the deny-list-miss class is removed structurally rather than patched." }
  D5:  { status: fixed, commit: "3438475, 3ca89b4", note: "Per-route matching removed, not repaired: the sampler receives Tags=null at span start (measured), so no key can match. One service-wide rate (DeterministicTraceIdSampler); leftover Sampling:Routes aborts boot. Owner accepted (Decisions)." }
  D6:  { status: fixed, commit: "33474bc, 3ca89b4", note: "Out-of-rate Server spans sampled RecordOnly instead of Drop, so ErrorOverrideProcessor.OnEnd runs and promotes errored spans; non-server roots keep Drop. SamplingPipelineTests run the real OTel SDK and redden on Drop (Decisions)." }
  D7:  { status: fixed, commit: 6df47b2, note: "Every terminal webhook branch records payment_webhook_total and logs, incl. the EuPlatesc fall-through; new MetricCapture helper drives a MeterListener, 8 tests. Recorded from the commit — fixer cancelled before reporting (Decisions)." }
  D8:  { status: fixed, commit: 295a51c, note: "The booted host's SentryAspNetCoreOptions are pushed through a real SentryClient with a capturing ITransport; assertions run over serialized envelope bytes. Reddening shown twice. The mocked IHub stays only for the scope-enricher capture." }
  D9:  { status: fixed, commit: fbaf9f4, note: "MeterListener emission tests at both uncovered call sites (orders_created_total incl. idempotent replay; upload_size_bytes incl. rejected upload) plus a test that emits then scrapes /metrics. All 4 redden under the review's own mutations." }
  D10: { status: fixed, commit: "b4a3789, a054fdd", note: "New ScrapeIpAllowList parser shared by the middleware and the validator: entries trimmed, CIDR supported, every unparseable entry aborts boot naming itself; octal, inet_aton and IPv4-mapped IPv6 range forms rejected too." }
  D11: { status: fixed, commit: 7266f21, note: "Registered AddSingleton so the parsed allow-list and the deny-log dedupe survive across requests; dedupe keyed on (peer, reason), bounded at 512 distinct entries with a one-shot Warning at the cap." }
  D12: { status: fixed, commit: b4a3789, note: "Peer and allow-list entries canonicalized before comparison (IsIPv4MappedToIPv6 -> MapToIPv4, scope id stripped) for plain entries and CIDR ranges; regression tests use ::ffff:10.42.0.5." }
  D13: { status: fixed, commit: "144584e, 3ca89b4", note: "Deviates: a blank Otlp:Endpoint outside Development skips the WithTracing pipeline (metrics keep working) and logs observability.tracing.disabled at boot instead of failing it (Decisions). Console exporter reachable only in Development." }
  D14: { status: fixed, commit: "4fb0386, e965c99", note: "Dashboard and slos.md renamed to the emitted names, discovered by scraping a real exposition rather than read off SDK docs; DashboardMetricNamesTests holds every dashboard and slos.md query name against the exposition (Decisions)." }
  D15: { status: fixed, commit: "c7b6a75, e965c99", note: "Capture keyed on the mapped status code (>= 500), not an exception list, so a later mapping cannot skip it; mapped 5xx moves LogWarning to LogError. Deviates on the Serilog half: docs corrected, no Sentry sink added (Decisions)." }
  D16: { status: fixed, commit: ead3c12, note: "CreateForOrderAsync wraps the inner call in try/catch-rethrow and records awb_creation_total{result=error}; shutdown cancellation records nothing by design. 3 tests. Recorded from the commit — fixer cancelled before reporting (Decisions)." }
  D17: { status: fixed, commit: c407685, note: "Partial: the duration is recorded only after SaveChangesAsync returns (2 tests, incl. the commit-fails leg). The concurrent double-click leg got no guard and no test; the re-review must judge whether that leg is closed (Decisions)." }
  D18: { status: fixed, commit: "98a5671, e965c99", note: "Deviates — the suggested fix is not implementable: DiagnosticLogger's getter returns null when Debug=false (measured). Debug now always true; Sentry:Debug picks the verbosity (Warning/Debug). Owner accepted the posture (Decisions)." }
  D19: { status: fixed, commit: c809f30, note: "Deviates: test hosts no longer touch process state — IWebHostBuilder.UseSetting makes Sentry and Observability config per-host; the non-parallel collection kept but narrowed. 2 tests redden when a static-ctor env var is restored." }
  D20: { status: fixed, commit: "0ef6cf0, 972d057", note: "Split: MetricNames.LabelContract derives the budget and MetricCapture.ContractViolations() checks observed tags at every call site; the review's user_id mutation now reddens. 'unknown' deliberately not whitelisted (Decisions)." }
  D21: { status: fixed, commit: 2d25b03, note: "The enricher now runs against a real Sentry Scope behind a hub stub reporting IsEnabled=true; authenticated, anonymous, no-correlation-id and disabled-hub legs asserted. Mutating the claim type to one that never exists reddens it." }
  D22: { status: fixed, commit: 44c3e2d, note: "Tests cover SDK-populated shapes: a real DefaultHttpContext through ScopeExtensions.Populate, an SDK-shaped transaction with spans, Mechanism.Data, Contexts.Response, SentryMessage, and an end-to-end envelope with no token or email." }
  D23: { status: fixed, commit: "4711fac, a054fdd", note: "DEPLOYMENT.md §14 written (14.1-14.12: flags, gates, Prometheus provisioning, allow-list syntax, OTLP, cost, rollout, curl matrix, playbook, env-var table); ADR-018 and ddd-02 pointers now resolve; .env.example gained the block." }
  D24: { status: backlog, commit: null, note: "🟡 routed to the ledger backlog per the README router; new 🟡/⚪ findings do not enter a fix round." }
  D25: { status: backlog, commit: null, note: "🟡 routed to the ledger backlog; flagged to the owner in summary-v1 as a second unscrubbed egress path (EF spans ship SQL to OTLP)." }
  D26: { status: backlog, commit: null, note: "🟡 routed to the ledger backlog per the README router." }
  D27: { status: backlog, commit: null, note: "🟡 routed to the ledger backlog per the README router." }
  D28: { status: backlog, commit: null, note: "🟡 routed to the ledger backlog per the README router." }
  D29: { status: backlog, commit: null, note: "🟡 routed to the ledger backlog per the README router." }
  D30: { status: backlog, commit: null, note: "🟡 routed to the ledger backlog per the README router." }
  D31: { status: backlog, commit: null, note: "🟡 routed to the ledger backlog per the README router." }
  D32: { status: backlog, commit: null, note: "🟡 routed to the ledger backlog per the README router." }
  D33: { status: backlog, commit: null, note: "⚪ routed to the ledger backlog per the README router." }
  D34: { status: backlog, commit: null, note: "⚪ routed to the ledger backlog per the README router." }
  D35: { status: backlog, commit: null, note: "⚪ routed to the ledger backlog; cross-target — the residue comes from the repo-wide comment sweep (09173c4), which belongs to the system target's loop." }
  D36: { status: backlog, commit: null, note: "⚪ routed to the ledger backlog per the README router." }
  D37: { status: backlog, commit: null, note: "⚪ routed to the ledger backlog per the README router." }
  D38: { status: backlog, commit: null, note: "⚪ routed to the ledger backlog per the README router." }
  D39: { status: backlog, commit: null, note: "⚪ routed to the ledger backlog per the README router." }
---

# Resolution v1 — 044-045-observability

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — /metrics access control | D1, D10, D11, D12, D23 | `MetricsEndpointIpAllowListMiddleware.cs`, `ObservabilitySettingsValidator.cs`, `Caddyfile`, `DEPLOYMENT.md` | ran — refuted the header-deny rule; the listener gate became the control |
| B — Sentry data egress | D2, D3, D4, D22 | `SentryDataScrubbers.cs`, `Program.cs` | ran — surfaced the span/breadcrumb credential path; refuted fail-closed |
| C — tracing correctness | D5, D6, D13 | `Sampling/DeterministicTraceIdSampler.cs` (was `RouteAwareSampler.cs`), `ErrorOverrideProcessor.cs`, `ObservabilityExtensions.cs` | ran — refuted the in-process per-route rescue |
| D — metric emission gaps | D7, D16, D17 | `WebhooksController.cs`, `AwbCreator.cs`, `AdminOrderService.cs` | ran — output lost; the fixer was cancelled before reporting |
| E — test vacuity | D8, D9, D19, D20, D21 | new test files, `SentryIntegrationFactory.cs` | not needed (test-only; no new mechanism in the running service) |
| F — error-signal routing | D14, D15, D18 | `ExceptionHandlerMiddleware.cs`, `ops/dashboards/fototipar-overview.json`, `slos.md` | ran — refuted the suggested D18 fix as not implementable |
| Backlog | D24–D39 | ledger | not needed (no fix; routed per the README router) |

## Decisions

### Scrape listener over header rules (D1)

The approach-check refuted the planned deny-on-`X-Forwarded-For` rule: `ForwardedHeadersMiddleware`
removes the header it consumes, a sidecar adds it to legitimate in-pod scrapes, and it fires only in
already-wrong configs. Edge-blocking alone is config-only — no test can redden it. So the listener
gate is the control (pinned by `Scrape_port_configured_makes_metrics_absent_on_the_public_listener`)
and the Caddy refusal is the belt. ADR-018's "non-default port = obscurity" rejection is superseded:
this port is neither published nor proxied. Fresh-eyes review repairs in `a054fdd`: the CIDR error
message suggested `8.0.0.0/16` (public space) for octal `010.0.0.0/16` and now round-trip-checks its
own suggestion; `::ffff:10.42.0.0/112` validated but could match nothing — rejected at boot; the 404
path now logs `metrics.scrape.wrong_listener`; `An_unparseable_allow_list_entry_aborts_boot` pins
`ValidateOnStart`. Boundary: the scrape listener serves the whole API; only `/metrics` is gated
(§14.4 warns against publishing it). The same class was found live outside the finding set — the
global rate limiter partitions on the proxy IP (`SecurityExtensions.cs:58-69`), the auth limiters
are unpartitioned (`AuthExtensions.cs:84-106`), and audit logs record the proxy IP as the client's
(`AuthController.cs:54,72,160`). Owner routed all three to `reviews/inbox.md` (2026-08-03).

### Deny-by-default scrubbing; the SDK fails open (D2)

D2, D3 and D4 were fixed as one class: allow-lists at all three egress hooks via
`SentryDataScrubbers.Register`, so a field the SDK adds later arrives redacted. Measured against
Sentry 4.13.0: when `BeforeSend` or `BeforeSendTransaction` throws, the SDK sends the original
unscrubbed payload — `implementation-walkthrough.md:80` claimed the opposite and was corrected. The
scrubber therefore catches everything, logs at Error and returns null, dropping the payload. The
approach-check surfaced a live credential path: the Google OAuth `id_token` in a query string
(`GoogleTokenValidator.cs:37`) reaches span descriptions and breadcrumbs, neither reachable from
`BeforeSend` — hence the third hook and URL sanitising. Boundaries: exception messages and span
descriptions ship in full (redacting them guts triage; no interpolating call site exists today),
and the OTLP exporter stays a second unscrubbed egress path (D25).

### Per-route sampling removed, not repaired (D5)

Measured with a throwaway probe on the real stack: at span start the sampler gets `Tags = null`, so
neither `http.route` nor the review's suggested `url.path` exists — that half of the suggestion is
not implementable on .NET 8 (`url.path` is written after the sampler returns). The only in-process
rescue, `IHttpContextAccessor`, was refuted: registering it stops `DefaultHttpContext` pooling, so
every request pays for telemetry, and raw-path matching reproduces D5's own silent-miss class. So
the capability moved out: one service-wide deterministic rate, `Observability:Sampling:Routes`
deleted everywhere, and a leftover key aborts boot naming where the rate moved. ADR-017 amended
twice; §14.7/§14.11, `.env.example` and the settings doc updated. Owner accepted the removal
2026-08-03: story 003's per-route capability is a known gap, deferred to collector-side tail
sampling that is not yet provisioned.

### Error promotion holds Server spans only (D6)

Out-of-rate spans go `RecordOnly` instead of `Drop`, verified against the OTel 1.11.2 sources:
`OnEnd` is gated on `IsAllDataRequested` only. What the fix does not give: a promoted span exports
alone (children were dropped at start) and carries no exception (`ExceptionHandlerMiddleware`
catches everything first) — route, status, duration and trace id only. Written into the ADR-017
amendment rather than left to rediscover. A fresh-eyes review caught that background-job EF roots
would have been held and exported with SQL text, so holding is limited to `ActivityKind.Server`;
non-server roots keep `Drop`. `Sampling:Default = 0.0` changed meaning to "export errored spans
only"; the off switch is `Observability:Enabled = false`. Boundary: no test pins the production
processor order — a host-based test passed alone but failed in the full suite (every live
TracerProvider samples the same hosting source), so it was deleted, not disabled; it becomes
possible once test hosts stop leaking observability config (the D19 fix).

### Blank OTLP endpoint skips tracing instead of failing boot (D13)

The suggested fail-at-boot was rejected: it would break DEPLOYMENT §14.8's documented metrics-first
rollout stage and, through the env-var leak D19 fixed, abort unrelated test hosts
nondeterministically. Instead a blank `Otlp:Endpoint` outside Development skips the whole
`WithTracing` pipeline, keeps metrics working, and logs `observability.tracing.disabled` once at
boot. The console exporter is reachable only in Development. `AddObservability` gained an
`IHostEnvironment` parameter.

### Three fixes recorded from commits alone (D7, D16, D17)

The cluster-D fixer was cancelled before reporting; the loop driver recorded D7, D16 and D17 from
their commits. The approach-check output is lost and the test reddening was never demonstrated to
the driver. D17 is knowingly partial: the after-commit leg is fixed and tested, but the concurrent
double-click leg has no conditional write and no once-only guard — left to the re-review to judge.
The cancelled round had also left `o.SendDefaultPii = false;` replaced by a mutation marker in
`Program.cs`, uncommitted — restored this round and called out as a process failure.

### Budget derived from a label contract; "unknown" not whitelisted (D20)

Split what the finding conflated: `MetricNames.LabelContract` derives the cardinality budget, and
`MetricCapture.ContractViolations()` holds observed tags against it at each call site — the
review's own `user_id` mutation now reddens. Against the suggestion, `"unknown"` was not added to
the `All` arrays: `PaymentProcessor` has two members and `AwbCreationOutcome` is a closed record
hierarchy, so both `_ => "unknown"` arms are unreachable; whitelisting would inflate the budget and
make a real occurrence invisible. An emitted `"unknown"` is now a test failure; deleting the dead
literals is D33. A fresh-eyes review restored the exact per-instrument series counts the rewrite
had loosened to `<= 100` (`972d057`). `LabelContract` stays in the API assembly deliberately: it is
the documented single source of truth for names and label values, and moving it to the test project
would let production drift from it silently.

### Dashboard names measured from a live exposition (D14)

The names came from scraping a booted host, not SDK docs: the HTTP metric is
`http_server_request_duration_seconds` (histogram) with `http_response_status_code` and
`http_route`; there is no request counter at all, so availability and request-rate use the
histogram's `_count`. Also measured: the Prometheus exporter does not append a second `_total`, so
`orders_created_total` is exposed under exactly that name. A first test asserting every dashboard
metric also appears in `slos.md` was wrong (business panels have no SLO behind them) and was
replaced by the direction that matters: every name `slos.md` queries must be emitted.

### Status-code keying; no Serilog-to-Sentry sink (D15)

Capture fires on `mapping.StatusCode >= 500`, not on an exception list — an enumerated list would
repeat the finding's own defect class. On the Serilog half the docs were corrected instead of
adding a sink: bridging would double-capture what the middleware already reports and auto-ship
every `LogError` in the repo into a 5k-events/month tier; §13.1 now states this. Background jobs
never pass through `ExceptionHandlerMiddleware`, so the fix does not reach them; the owner declined
a review target for that gap and routed it to `reviews/inbox.md` (2026-08-04).

### Sentry Debug always on; the knob picks verbosity (D18)

The suggested fix cannot work: `SentryOptions.DiagnosticLogger`'s getter returns null whenever
`Debug` is false, so an assigned logger is discarded — probed in-repo against Sentry 4.13.0
(`debugOff.DiagnosticLogger=NULL`, `debugOn.DiagnosticLogger=NoopLogger`). `Debug` is now always
true and `Sentry:Debug` selects `DiagnosticLevel` (Warning normally, Debug when set); the SDK's own
MEL logger reports 429s and transport failures through Serilog. Deliberately not done: no
dropped-event counter and no Sentry-reachability health check — recorded as a remaining gap in
DEPLOYMENT.md §13.4. Owner accepted the production posture change 2026-08-04.
