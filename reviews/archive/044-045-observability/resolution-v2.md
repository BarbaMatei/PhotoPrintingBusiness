---
type: resolution
target: 044-045-observability
version: 2
answers: review-v2.md
status: resolved
fixed_commit: 7e28317
closed: 2026-08-05
findings:
  D40: { status: fixed, commit: 22bede9, note: "`OrderStatusMachine.HasBeenPaid` (explicit set: Paid/Printing/Shipped/Delivered) replaces the `== Paid` guard in both webhook handlers; a redelivery past `Paid` records `duplicate` and logs nothing. Cancelled still alarms. 10 tests." }
  D41: { status: fixed, commit: d932343, note: "One-arg `ParentBasedSampler` removed as a no-op wrapper; new `BuildSampler` seam so pipeline tests build the sampler production uses. 3 tests at the ActivitySource seam with a remote parent. Trace-id residual not closed — see Decisions." }
  D42: { status: fixed, commit: d932343, note: "Doc only, per the owner's no-benchmark choice. ADR-017's cost bullet and §14.7 now say `RecordOnly` sets `IsAllDataRequested`: lowering the rate is an egress lever, not a CPU one. The ADR's 'one root span' wording corrected." }
  D43: { status: fixed, commit: 60c5866, note: "Caller cancellation is rethrown, not mapped to `BadGatewayException`; the discriminator is `ex.GetBaseException() is not TimeoutException`. Same fix at the body-read catch that turned an abort into a 401. 2 tests; see Decisions." }
  D44: { status: fixed, commit: ba1c182, note: "DashboardMetricNamesTests checks queried label names against the real exposition and literal label values against `MetricNames.LabelContract`; the walker recurses into row panels, closing D57. Both cited renames redden — measured." }
  D45: { status: fixed, commit: 67b0be7, note: "`IHostedLifecycleService.StartedAsync` guard; pure `ScrapeListenerCheck.Verdict` (12 unit tests) refuses boot when ScrapePort is unbound or the only listener; skips when no addresses are reported (TestServer). §14.10 updated." }
  D46: { status: deferred, commit: null, note: "Owner parked 2026-08-05: the gate-chosen fix (exclude at the instrumentation) needs .NET 9, and the two remaining routes change SLO 1's meaning. SLO 1 counts ~5,760 self-monitoring requests/day; availability cannot read below ~99.7%." }
  D47: { status: fixed, commit: d96d6f4, note: "`MetricCapture` scopes captures to the test's execution context (AsyncLocal token); the old `ReferenceEquals` meter filter excluded nothing. 3 tests, one using `ExecutionContext.SuppressFlow`; removing the gate reddens one — measured." }
  D48: { status: fixed, commit: 82342dd, note: "A breadcrumb carrying a token-bearing URL is pushed through the booted host's real SentryClient and the serialized envelope asserted. Deleting `SetBeforeBreadcrumb` reddens exactly this one test — measured; before it left 358 green." }
  D49: { status: fixed, commit: 2c92655, note: "Two tests: a mapped 5xx logs at Error with the exception attached, and a mapped 4xx stays off Error. Reverting `LogError` to `LogWarning` reddens the first — measured; before it left 24 green." }
  D50: { status: fixed, commit: ba1c182, note: "Doc only. slos.md's status block no longer says 'SLOs 1-4 are measured' unqualified, and SLO 3 names its blind spot: the counter increments inside a terminal branch, so a throw before any branch moves neither side of the ratio." }
  D51: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D52: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D53: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D54: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D55: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D56: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D57: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D58: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D59: { status: backlog, commit: null, note: "🟡 — ledger backlog; flagged to the owner in summary-v2 as the first finding in the still-owed db-parity gap" }
  D60: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D61: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D62: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D63: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D64: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D65: { status: backlog, commit: null, note: "⚪ — ledger backlog per the README router" }
  D66: { status: backlog, commit: null, note: "⚪ — ledger backlog per the README router" }
  D67: { status: backlog, commit: null, note: "⚪ — ledger backlog per the README router" }
  D68: { status: backlog, commit: null, note: "⚪ — ledger backlog per the README router" }
  D69: { status: backlog, commit: null, note: "⚪ — ledger backlog per the README router" }
  D70: { status: backlog, commit: null, note: "⚪ — ledger backlog per the README router" }
  D71: { status: backlog, commit: null, note: "⚪ — ledger backlog per the README router" }
  D72: { status: backlog, commit: null, note: "⚪ — ledger backlog per the README router" }
  D73: { status: backlog, commit: null, note: "⚪ — ledger backlog per the README router" }
---

# Resolution v2 — 044-045-observability

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — webhook classification | D40 | `Controllers/WebhooksController.cs` | not needed (conditional logic, no new mechanism) |
| B — sampling posture and its cost claim | D41, D42 | `Extensions/ObservabilityExtensions.cs`, `Observability/Sampling/DeterministicTraceIdSampler.cs`, `adr-017`, `DEPLOYMENT.md §14.7` | needed (changes sampling semantics and the trace-volume budget) |
| C — Sentry capture scope | D43 | `Services/GoogleTokenValidator.cs`, `Middleware/ExceptionHandlerMiddleware.cs` | needed (changes a catch/mapping layer) |
| D — guarantees nothing enforces | D44, D48, D49 | `Tests/Integration/DashboardMetricNamesTests.cs`, `Tests/Integration/SentryOptionsWiringTests.cs`, `Tests/Unit/Middleware/ExceptionHandlerMiddlewareTests.cs` | not needed (test-only) |
| E — scrape-port boot check | D45 | `Program.cs`, `Validators/ObservabilitySettingsValidator.cs` | needed (adds a boot gate that can abort or warn) |
| F — SLO claims | D46, D50 | `memory-bank/operations/slos.md`, `ops/dashboards/fototipar-overview.json` | not needed (doc-only after the owner parked D46) |
| G — test-harness isolation | D47 | `Tests/Helpers/MetricCapture.cs` and its six consumers | not needed (test helper) |
| H — backlog triage | D51–D73 | — | not needed (🟡/⚪ routed to the ledger backlog) |

## Decisions

### Fixer and finder shared one session (D40–D50)

- The v2 verification pass and this fix round ran in one session, so the finding author fixed them. The verifier-independence rule holds — the v3 verification pass ran from a fresh session — but the dispute pressure a fixer applies to inherited findings was absent.
- Every in-scope finding carried measured evidence from the pass itself; D48 and D49 were proven by deleting the production line and watching the suite stay green. Triage was a re-read of that evidence at `8daa977` (source identical to `e965c99`), not a fresh judgment.

### Owner gate answers, all as recommended (D41, D42, D45, D46)

- Asked once after triage on 2026-08-05, per the fixer contract.
- D41: ignore an inbound `traceparent` sampling decision — public edge, no service mesh. D42: correct the cost claim without commissioning a benchmark. D45: refuse to start on a scrape-port/listener mismatch. D46: exclude `/metrics` and `/health` from the availability metric — later found unimplementable, next block.

### The approved exclusion does not exist on this stack (D46)

- `IHttpMetricsTagsFeature.MetricsDisabled` was added in .NET 9; on net8.0 the attempt fails with `CS1061: 'IHttpMetricsTagsFeature' does not contain a definition for 'MetricsDisabled'`. OpenTelemetry.Instrumentation.AspNetCore 1.11's metrics overload `AddAspNetCoreInstrumentation(MeterProviderBuilder)` takes no options argument — only the tracing overloads take `Filter`. Confirmed against the package's XML docs.
- The remaining routes are the two the owner did not pick: filter each PromQL query, or correct SLO 1's prose. The query filter changes meaning: `/metrics` is served by terminal middleware and carries no `http_route` label, so excluding it means excluding all unrouted requests (`http_route=""`), which also drops 404s to unknown paths. `/health` excludes cleanly.
- Owner decision 2026-08-05: parked rather than re-decide under a false premise. D46 is `deferred` on the ledger — not fixed, not silently dropped.
- Left standing: SLO 1 includes roughly 5,760 always-200 self-monitoring requests a day, so availability cannot read below about 99.7%, and the p50/p95/p99 latency panels and the RPS panel are diluted the same way. The availability number is not yet trustworthy.

### An explicit paid set, not an enum comparison (D40)

- `PaymentFailed` (5) and `Cancelled` (6) sort after `Delivered` (4), so a `>= Paid` comparison would read both as paid — the finding's own category of mistake.
- Swept every `== OrderStatus.Paid` site: the other ten (`AwbRetryJob`, `AwbCreator`'s load-bearing re-check, `OriginalPurger`, `UploadCleanupJob`, `BackfillCommand`, `AdminOrderService`) genuinely mean strictly Paid and were left alone.
- Cancelled still alarms on purpose: a paid-then-cancelled order needs a human — the money moved and the fulfilment did not. The two pre-existing Cancelled tests stayed green throughout and pin that.
- Test set: 6 new theory cases plus one asserting every status reachable from `Paid` except `Cancelled` is covered, so a future status cannot silently read as unpaid.
- Adjacent, not fixed: `HandleStripePaymentFailedAsync` still records `failed` unconditionally for an already-paid order — D52, backlog; whoever drains it should fix it here.

### Sampler wrapper deleted, not re-parameterised (D41)

- Against the OTel 1.11.2 sources, a five-arm `ParentBasedSampler` delegating every arm to one sampler is an unconditional pass-through; deleting it makes the one-arg regression impossible to write by accident.
- The approach-check caught that the planned regression test could not have reddened: `SamplingPipelineTests` built its own `ParentBasedSampler`, testing a copy of the composition. The `BuildSampler` seam fixes that and retrofits the five pre-existing pipeline tests onto the production sampler.
- Not closed: the sampler still hashes a caller-supplied trace id. About `1/rate` offline tries yield an always-sampled id, reusable forever; the inverse picks an always-dropped id, though errors survive via `RecordOnly`. Salting would break ADR-017's stable public-hash invariant and its cross-service claim. Recorded, not fixed.
- Accepted semantic regression: a peer at a different rate now disagrees where the shared algorithm previously agreed. The ADR amendment says so.
- Noticed but unverified, outside the finding set: Sentry honours an inbound `sentry-trace` sampled flag ahead of `TracesSampleRate`. Recorded here rather than filed; later minted as D77.
- Boundary: `DeterministicTraceIdSampler`'s `Kind == ActivityKind.Server` guard assumes Server implies root-or-remote; a Server-kind child under a local parent would be held rather than dropped. Nothing in this app creates one.

### The client-abort discriminator was measured, twice (D43)

- The approach-check corrected the draft: `ct.IsCancellationRequested` alone loses a genuine 5 s Google timeout raced by a late client abort — the population that aborts most. Its own proposal, `ex.InnerException is not TimeoutException`, also failed when implemented: `HttpClient` nests `TaskCanceledException → TaskCanceledException → TimeoutException`, measured. Shipped: `ex.GetBaseException() is not TimeoutException`, depth-independent.
- The check found the same class twelve lines down, worse: a bare catch around the response body read turned a client abort into a 401 — a forced SPA logout. Same client-abort rethrow applied.
- The old harness could not exercise either fix: `StubHttpHandler` ignored its cancellation token, and the "timeout" test hand-threw a null-inner `TaskCanceledException` — green under a correct and a broken discriminator alike. Added a hanging handler and a timeout-then-abort handler; the latter reddens under the naive filter — demonstrated.
- Not claimed: log noise remains. `UseSerilogRequestLogging` sits inside the exception handler and still logs the aborted request at Error with the exception. The fix removes the Sentry issue and the middleware's own Error line.
- Boundary: the body-read guard is defensive only — `GetAsync` uses the default `ResponseContentRead`, so the content is buffered before `ReadAsStringAsync` and that cancellation path is unreachable today. No test; stated rather than faked.

### The drafted boot guard was refuted; StartedAsync shipped (D45)

- Measured with a throwaway probe: a throw from an `ApplicationStarted` callback is caught by `ApplicationLifetime.NotifyStarted`, logged as `crit`, and the app keeps serving — the drafted guard would log and shrug. A plain `IHostedService.StartAsync` runs before Kestrel binds (`Build()` re-appends `GenericWebHostService` last), so the address list is empty. `IHostedLifecycleService.StartedAsync` is the only hook that both sees bound addresses and aborts the host.
- Parsing uses `BindingAddress.Parse`, not `Uri`: `Uri.TryCreate` returns false for both `http://+:8080` and `http://*:8080`, and returns true with Port=80 for a unix socket — a silently wrong answer.
- The carve-out is "no addresses reported", not an environment check. A TestServer host reports the feature present but empty, which keeps the two integration tests setting `ScrapePort=9090` booting; a real Kestrel always reports at least one address. An environment carve-out was rejected: §14.8 rolls metrics out on staging first, so a Production-only check would miss the first place it matters.
- Rule 2 compares distinct ports, not address count: `http://127.0.0.1:9090;http://10.0.0.5:9090` is two addresses and one port, still the exposure.
- Deliberate gap: no test proves the abort. Every integration test uses TestServer, where the check skips by design, so a `WebApplicationFactory` test could only prove the skip; the verdict function is unit-tested against the real measured address strings instead. A real-Kestrel boot test is the honest gap.
- Flagged to the owner, not folded in: `ScrapePort == 0` (serve `/metrics` on every listener) is still only a warning — the same exposure rule 2 makes fatal.

### What the dashboard check pins, and what it cannot (D44)

- Literal `=` matchers only: a regex or negative matcher (`=~`, `!~`, `!=`) has its label name checked but not its value, because such a matcher does not have to name a value that exists.
- The value arm skips metrics outside `LabelContract` — exactly SLO 2's `http_route="api/payments/stripe/intent"` and `http_request_method="POST"` on the framework histogram, so renaming that controller route would still empty the panel with a green build. slos.md now states exactly what is checked, and the test names the two unverifiable matchers in an assertion, so a third is a test failure rather than a silent gap.
- Closes D57 as a side effect: the panel walker now recurses into Grafana row panels — leaving it would have been a new blind spot in the very check being strengthened.

### Execution-context scoping, not a serial collection (D47)

- Serialising the metric tests into one non-parallel collection was rejected: the set that emits business metrics (anything exercising an order, upload, webhook or AWB path) is far wider than the set that captures them, so the collection would have swallowed most of the suite.
- The AsyncLocal token is the thing that actually distinguishes "this test's work", and it flows into awaited work, which is what real call sites do.
- Stated against the fix: it trades an unproven flake risk for a narrow false-green risk. An emission from a thread that never inherited the test's execution context is now silently invisible rather than wrongly attributed, and four existing "no measurement was recorded" assertions would then pass vacuously. No current call site does that. The micro-review was asked about this specifically.

### Micro-review repairs to the round's own diff (D40–D50)

- Two fresh-eyes agents over the round's diff, split by risk: behaviour changes (D40, D41, D43, D45) and contract/test changes (D42, D44, D47, D48, D49, D50). 15 findings between them, repaired in `7e28317`.
- The D50 edit had swallowed SLO 3's `**Action on breach:**` heading, leaving the one SLO whose ownership row says "Sentry pages immediately" with no owner action. Restored.
- The D44 fix had re-introduced the class it was closing — the value-arm gap recorded in the D44 block above.
- Nothing pinned the D45 hook choice or the D41 call site — the places the fixes live. Added a test that `AddObservability` registers the guard and that it implements `IHostedLifecycleService`; the `BuildSampler` call-site pin stayed open (last block).
- The label-matcher regex truncated at the first `}`, silently skipping route-template values like `http_route="api/orders/{id}/payments"`; both the query parser and the exposition parser now find the closing brace outside quotes.
- Nested `MetricCapture`s silently blinded the outer capture, whose "nothing was recorded" assertions then passed vacuously. The second constructor now throws.
- A DEPLOYMENT.md sentence written this round was false: a chosen `traceparent` does still force a trace — the brute-force residual above, and the legitimate way to debug one request. Corrected, along with the "raise the rate" advice.
- `metrics.md`'s `duplicate` and `failed` rows were stale after the D40 fix — rewritten. The second `GoogleTokenValidator` catch lacked its sibling's `TimeoutException` carve-out — same discriminator now.
- One micro-review hypothesis was disproved: `BindingAddress.Parse` returning port 80 for a unix socket would fail `An_address_with_no_port_is_not_counted_as_a_listener` on every run; the test passes.

### Left unfixed, disclosed to the re-reviewer (D41, D45, D47)

- Nothing proves `ObservabilityExtensions.cs:71` calls `BuildSampler`: re-wrapping the call as `new ParentBasedSampler(BuildSampler(...))` restores D41's defect with no red test. `TracerProvider` does not expose its sampler, so pinning it needs reflection or a redesign. Later minted as D79.
- `MetricCapture`'s context scoping can miss a real emission — a capture constructed inside an async helper, or an emission from a thread that never inherited the context — sending four "nothing recorded" assertions vacuously green. Latent today; the re-reviewer should weigh it.
- `ScrapeListenerGuard` aborts after Kestrel has bound: in the "only listener" verdict the process serves `/metrics` on the proxied port for the start-to-exit window, repeating each restart-loop iteration.
- Docker image defaults contradict the new guard: `Dockerfile:41` sets `ASPNETCORE_URLS=http://+:8080` with `EXPOSE 8080` only and `docker-compose.yml` is single-port (only `docker-compose.prod.yml` adds 9090), so the image plus a copied prod `.env` carrying `ScrapePort=9090` refuses to start. Intended, but the Dockerfile should carry the invariant.
