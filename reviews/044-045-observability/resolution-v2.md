---
type: resolution
target: 044-045-observability
version: 2
answers: review-v2.md
status: resolved
fixed_commit: 7e28317
closed: 2026-08-05
findings:
  F1:  { status: fixed, commit: 22bede9, note: "new OrderStatusMachine.HasBeenPaid (Paid/Printing/Shipped/Delivered — an explicit set, not an enum comparison: PaymentFailed and Cancelled sort after Delivered) replaces the `== Paid` duplicate guard in BOTH webhook handlers, so a redelivery for a fulfilled order records `duplicate` and logs nothing. Cancelled deliberately still alarms — a paid-then-cancelled order needs a refund. Red proof: 10 tests, incl. 6 new theory cases and a test that every status reachable from Paid except Cancelled is covered, so a future status cannot silently read as unpaid. New surface: HasBeenPaid" }
  F2:  { status: fixed, commit: d932343, note: "ParentBasedSampler REMOVED rather than re-parameterised — with all arms delegating to the same sampler it is a no-op wrapper that keeps the one-arg regression one keystroke away. New ObservabilityExtensions.BuildSampler seam so the pipeline tests build the sampler production uses; the approach-check caught that the planned test could not have reddened, because SamplingPipelineTests built its own copy. 3 new tests at the ActivitySource seam with a remote parent. NOT CLOSED by this fix: the trace id is still caller-supplied and brute-forceable — see decisions. New surface: BuildSampler" }
  F3:  { status: fixed, commit: d932343, note: "documentation only, per the owner's choice of no benchmark. ADR-017's cost bullet and DEPLOYMENT §14.7 now say RecordOnly sets IsAllDataRequested, so lowering the rate saves child spans and egress, not per-request span work — an egress lever, not a CPU one. The ADR's 'one root span' wording also corrected: after F2 these are no longer necessarily roots" }
  F4:  { status: fixed, commit: 60c5866, note: "GoogleTokenValidator rethrows a caller's cancellation instead of translating it to BadGatewayException. DEVIATES from my own first draft: the approach-check showed `ct.IsCancellationRequested` alone loses a real Google outage to a late client abort, and I then measured that HttpClient NESTS the exception (TaskCanceledException -> TaskCanceledException -> TimeoutException), so a one-level inner check misses it too — the discriminator is `ex.GetBaseException() is not TimeoutException`. Same class fixed at the body-read catch, which was turning a client abort into a 401 (a forced SPA logout). 2 tests; the timeout-then-abort one reddens under the naive filter, demonstrated. New surface: the catch filter" }
  F5:  { status: fixed, commit: ba1c182, note: "DashboardMetricNamesTests now holds queried label NAMES against the real exposition and literal label VALUES against MetricNames.LabelContract; the panel walker recurses into Grafana row panels (which also closes the deferred D57). Both of the finding's own one-line edits — Labels.Result and WebhookResultValues.Ok — now redden, demonstrated. slos.md's promise rewritten to state exactly what is checked. New surface: LabelUsagesIn + ExposedSeriesLabelsAsync" }
  F6:  { status: fixed, commit: 67b0be7, note: "APPROACH REFUTED AND REPLACED by the adversarial check, which measured that a throw from ApplicationStarted is swallowed by the host and that IHostedService.StartAsync runs before Kestrel binds — my original design would have shipped a check that logs and shrugs. Now IHostedLifecycleService.StartedAsync, the only hook that both sees bound addresses and aborts. Pure decision function ScrapeListenerCheck.Verdict (12 unit tests) refuses boot when ScrapePort is unbound or is the only listener; parses with BindingAddress (Uri rejects '+'/'*' and gives unix sockets port 80) and skips when no addresses are reported, which is what keeps TestServer hosts booting. DEPLOYMENT §14.10 playbook updated incl. the restart-loop presentation. New surface: the guard, the verdict function, the Critical log line" }
  F7:  { status: deferred, commit: null, note: "PARKED BY THE OWNER 2026-08-05. The option chosen at the gate (exclude /metrics and /health at the instrumentation) is NOT AVAILABLE on this stack: IHttpMetricsTagsFeature.MetricsDisabled is .NET 9, and OTel 1.11's metrics AddAspNetCoreInstrumentation takes no options at all, so there is no Filter — verified by compiling the attempt and reading the package's XML docs. The two remaining routes both change meaning or duplicate per query, so the owner parked it rather than decide under a false premise. SLO 1 therefore still counts ~5,760 self-monitoring requests a day and cannot read below ~99.7%; D46 stays in the ledger as deferred, not fixed" }
  F8:  { status: fixed, commit: d96d6f4, note: "MetricCapture now scopes captures to the emitting test's execution context (AsyncLocal token), because FotoMetrics.Meter is one process-wide static and the old ReferenceEquals meter filter therefore excluded nothing. Chosen over serialising the tests into one non-parallel collection: the set that EMITS business metrics is far wider than the set that captures them, so a collection could not have closed it. 3 tests incl. an ExecutionContext.SuppressFlow case standing in for an unrelated test; removing the gate reddens it, demonstrated. New surface: the AsyncLocal token" }
  F9:  { status: fixed, commit: 82342dd, note: "a breadcrumb carrying a token-bearing URL is pushed through the booted host's real SentryClient and the serialized envelope asserted. Deleting SetBeforeBreadcrumb now reddens exactly this one test — measured; before it left 358 green" }
  F10: { status: fixed, commit: 2c92655, note: "two tests: a mapped 5xx logs at Error WITH the exception attached, and a mapped 4xx stays off Error. Reverting LogError to LogWarning reddens the first — measured; before it left 24 green" }
  F11: { status: fixed, commit: ba1c182, note: "documentation only. slos.md's status block no longer says 'SLOs 1-4 are measured' without qualification, and SLO 3 carries a named blind spot: the counter increments inside a terminal branch, so a throw before any branch (database down) moves neither side of the ratio and it holds at its last reading while customers are charged" }
  F12: { status: deferred, commit: null, note: "🟡 — ledger backlog (D51) per the README router" }
  F13: { status: deferred, commit: null, note: "🟡 — ledger backlog (D52) per the README router" }
  F14: { status: deferred, commit: null, note: "🟡 — ledger backlog (D53) per the README router" }
  F15: { status: deferred, commit: null, note: "🟡 — ledger backlog (D54) per the README router" }
  F16: { status: deferred, commit: null, note: "🟡 — ledger backlog (D55) per the README router" }
  F17: { status: deferred, commit: null, note: "🟡 — ledger backlog (D56) per the README router" }
  F18: { status: deferred, commit: null, note: "🟡 — ledger backlog (D57) per the README router" }
  F19: { status: deferred, commit: null, note: "🟡 — ledger backlog (D58) per the README router" }
  F20: { status: deferred, commit: null, note: "🟡 — ledger backlog (D59); flagged to the owner in summary-v2 as the first finding in the still-owed db-parity gap" }
  F21: { status: deferred, commit: null, note: "🟡 — ledger backlog (D60) per the README router" }
  F22: { status: deferred, commit: null, note: "🟡 — ledger backlog (D61) per the README router" }
  F23: { status: deferred, commit: null, note: "🟡 — ledger backlog (D62) per the README router" }
  F24: { status: deferred, commit: null, note: "🟡 — ledger backlog (D63) per the README router" }
  F25: { status: deferred, commit: null, note: "🟡 — ledger backlog (D64) per the README router" }
  F26: { status: deferred, commit: null, note: "⚪ — ledger backlog (D65) per the README router" }
  F27: { status: deferred, commit: null, note: "⚪ — ledger backlog (D66) per the README router" }
  F28: { status: deferred, commit: null, note: "⚪ — ledger backlog (D67) per the README router" }
  F29: { status: deferred, commit: null, note: "⚪ — ledger backlog (D68) per the README router" }
  F30: { status: deferred, commit: null, note: "⚪ — ledger backlog (D69) per the README router" }
  F31: { status: deferred, commit: null, note: "⚪ — ledger backlog (D70) per the README router" }
  F32: { status: deferred, commit: null, note: "⚪ — ledger backlog (D71) per the README router" }
  F33: { status: deferred, commit: null, note: "⚪ — ledger backlog (D72) per the README router" }
  F34: { status: deferred, commit: null, note: "⚪ — ledger backlog (D73) per the README router" }
---

# Resolution v2 — 044-045-observability

Fixer's answer to [review-v2.md](review-v2.md) (immutable). The review named 34 findings;
**the 11 🟠 (F1–F11, ledger D40–D50) are this fix round**. The 23 🟡/⚪ (F12–F34, D51–D73) are
deferred to the [ledger](ledger.md) backlog per the README router.

**10 of the 11 are fixed at `7e28317`. F7 (D46) is `deferred` — the owner parked it** rather than
re-decide under a premise that turned out false (see decisions). That makes every finding terminal,
so the round is `resolved` and a re-review is owed; it does **not** mean SLO 1 is fixed.

**Nothing here is `verified`.** Only `review-v3.md` — a re-review by someone who did not fix —
can set that status.

## Process note the re-reviewer must weigh

The v2 verification pass and this fix round ran in **the same session**, so the agent that
authored these findings is the agent fixing them. That does not breach the loop's rule (the rule
is that a fix round's *verifier* must be independent, and v3 will be), but it removes one
accidental safeguard: a fixer who inherits someone else's findings routinely disputes some of
them, and that pressure is absent here. Every finding in scope carries measured evidence in
[findings-v2.md](findings-v2.md) — two of them (D48, D49) were proven by deleting the production
line and watching the suite stay green — so the confirmation step below is a re-read of that
evidence, not a fresh judgment. **The v3 re-review should be run from a fresh session.**

## Fix round scope

| Cluster | Findings | Owner file(s) | Approach-check |
|---|---|---|---|
| A — webhook classification | F1 (D40) | `Controllers/WebhooksController.cs` | not needed (conditional logic, no new mechanism) |
| B — sampling posture and its cost claim | F2 (D41), F3 (D42) | `Extensions/ObservabilityExtensions.cs`, `Observability/Sampling/DeterministicTraceIdSampler.cs`, `adr-017`, `DEPLOYMENT.md §14.7` | **required** — changes sampling semantics and the trace-volume budget |
| C — Sentry capture scope | F4 (D43) | `Services/GoogleTokenValidator.cs`, `Middleware/ExceptionHandlerMiddleware.cs` | **required** — changes a catch/mapping layer |
| D — guarantees nothing enforces | F5 (D44), F9 (D48), F10 (D49) | `Tests/Integration/DashboardMetricNamesTests.cs`, `Tests/Integration/SentryOptionsWiringTests.cs`, `Tests/Unit/Middleware/ExceptionHandlerMiddlewareTests.cs` | not needed (test-only) |
| E — scrape-port boot check | F6 (D45) | `Program.cs`, `Validators/ObservabilitySettingsValidator.cs` | **required** — adds a boot gate that can abort or warn |
| F — SLO claims | F7 (D46), F11 (D50) | `memory-bank/operations/slos.md`, `ops/dashboards/fototipar-overview.json`, possibly `Extensions/ObservabilityExtensions.cs` | depends on the owner's answer to gate Q2 |
| G — test-harness isolation | F8 (D47) | `Tests/Helpers/MetricCapture.cs` + its six consumers | not needed (test helper) |

Ordering is by severity-then-bite: all eleven are 🟠 and there are no blockers, so A goes first
(it is the only one with a live customer-facing symptom), then C, B, E, D, F, G.

## Triage — confirmation that each finding still exists

Confirmed at `8daa977` (source identical to `e965c99`). All eleven were read against the code
during the v2 pass and none has been touched since; the re-confirmation below is per finding.

| F# | Confirmed how |
|---|---|
| F1 | `WebhooksController.cs:264` guards on `OrderStatus.Paid` only; `OrderStatusMachine.cs:22-26` puts `Printing`/`Shipped`/`Delivered` after `Paid`; the `else` at `:287` still logs and records `failed` |
| F2 | `ObservabilityExtensions.cs:66` still uses the one-argument `ParentBasedSampler`; no `header_up -traceparent` in `Caddyfile` |
| F3 | `DeterministicTraceIdSampler.cs:42` still returns `RecordOnly` for out-of-rate server spans; ADR-017 and §14.7 still state only the memory bound |
| F4 | `GoogleTokenValidator.cs:40-43` still catches `TaskCanceledException` and rethrows `BadGatewayException`; it is the only production source of that type |
| F5 | `DashboardMetricNamesTests.cs:144` still strips `{…}` before extracting; `slos.md:6-7` still promises the build fails on a rename |
| F6 | `Program.cs:378` still warns only on `ScrapePort == 0`; the validator only range-checks |
| F7 | SLO 1's query still has no route filter; `AddAspNetCoreInstrumentation()` at `:93` still sets no `Filter` |
| F8 | `MetricCapture.cs:26` still compares against `FotoMetrics.Meter`, the single process-wide static |
| F9 | measured this round: deleting `SetBeforeBreadcrumb` left 358 passed / 0 failed |
| F10 | measured this round: reverting `LogError` to `LogWarning` left 24 passed / 0 failed |
| F11 | `slos.md:3-5` still says "SLOs 1–4 are measured" with no caveat |

## Decisions

### Owner gate (2026-08-05) — four answers, all as recommended

Asked once after triage, per the fixer contract. The owner took the recommended option on all four:
**D41** ignore an inbound `traceparent`'s sampling decision (public edge, no service mesh);
**D46** exclude `/metrics` and `/health` from the availability metric; **D42** correct the cost
claim without commissioning a benchmark; **D45** refuse to start on a scrape-port/listener mismatch.

### F7 — the approved option does not exist on this stack

**This needs a second decision and is the one thing left open.** The gate chose "exclude
`/metrics` and `/health` at the instrumentation", which I recommended. It is not implementable here:

- `IHttpMetricsTagsFeature.MetricsDisabled` — the .NET mechanism for exactly this — **was added in
  .NET 9**. This project targets `net8.0`; the attempt fails to compile with
  `CS1061: 'IHttpMetricsTagsFeature' does not contain a definition for 'MetricsDisabled'`.
- OpenTelemetry.Instrumentation.AspNetCore 1.11's **metrics** overload is
  `AddAspNetCoreInstrumentation(MeterProviderBuilder)` with no options argument at all — only the
  tracing overloads take `Filter`. Confirmed against the package's own XML docs.

So the remaining routes are the two the owner did not pick: filter in each PromQL query, or correct
SLO 1's prose. **The query filter has a wrinkle worth knowing before choosing it:** `/metrics` is
served by terminal middleware rather than a routed endpoint, so it carries no `http_route` label —
excluding it means excluding *unrouted* requests (`http_route=""`), which also drops 404s to unknown
paths. That may be the right definition of "requests to the site", but it is a change in meaning,
not just a filter. `/health` is a real endpoint and excludes cleanly.

I did not substitute my own judgement here.

**Owner decision (2026-08-05): parked.** Shown the constraint and the two remaining routes with
their trade-offs, the owner chose to defer rather than pick one now. `D46` is `deferred` in the
ledger, not fixed and not silently dropped. **What that leaves standing:** SLO 1's availability
ratio still includes roughly 5,760 always-200 self-monitoring requests a day, so it cannot read
below about 99.7% however broken the site is, and the p50/p95/p99 latency panels and the RPS panel
are diluted the same way. Anyone reading that dashboard before this is drained should know the
availability number is not yet trustworthy. A re-review may re-raise it; the prior decision is
attached rather than suppressed, per the README.

### Cluster A — webhook classification (F1)

- **The class, not the instance.** Both handlers had the same shape — a duplicate guard testing
  `== OrderStatus.Paid` where "paid, and possibly moved on" was meant. Swept the rest of the
  codebase for `== OrderStatus.Paid`: the other ten sites (`AwbRetryJob`, `AwbCreator`'s
  load-bearing re-check, `OriginalPurger`, `UploadCleanupJob`, `BackfillCommand`,
  `AdminOrderService`) genuinely mean *strictly* Paid, and were left alone.
- **`HasBeenPaid` is an explicit set, deliberately not an enum comparison.** `PaymentFailed` (5) and
  `Cancelled` (6) sort after `Delivered` (4), so `>= Paid` would have read both as paid — the same
  category of mistake as the finding itself.
- **Cancelled still alarms, on purpose.** An order that was paid and then cancelled genuinely needs
  a human: the money moved and the fulfilment did not. The two pre-existing Cancelled tests stayed
  green throughout and pin that.
- **Boundary, not fixed:** `HandleStripePaymentFailedAsync` still records `failed` unconditionally,
  including for an already-`Paid` order where its sibling would say `duplicate`. That is `D52`,
  deferred to the backlog, and it is adjacent to this fix — whoever drains it should do it here.

### Cluster B — sampling posture (F2, F3)

- **Adversarial approach-check ran and changed the design twice.** It established, against the OTel
  1.11.2 sources, that a five-arm `ParentBasedSampler` all pointing at one sampler is an
  unconditional pass-through — so the honest fix is to delete the wrapper, not re-parameterise it,
  because the one-arg form is then impossible to write by accident. More importantly it caught that
  **my planned regression test could not have reddened**: `SamplingPipelineTests` constructed its own
  `ParentBasedSampler`, so it tested a copy of the composition rather than the composition. The
  `BuildSampler` seam fixes that and retro-fits all five pre-existing pipeline tests onto the real
  production sampler as a side effect.
- **NOT closed, and the fix must not be read as closing it:** the sampler still hashes a
  caller-supplied trace id. An attacker can precompute (about `1/rate` tries, offline, free) a trace
  id that falls below the rate and reuse it forever — restoring 100% sampling *and* collapsing all
  their traffic into one trace id, which is harder on a trace backend than many small traces. The
  inverse picks an out-of-rate id to stay untraced, though errors still survive via the `RecordOnly`
  path. Salting the hash would kill precomputation but breaks ADR-017's "publicly documented, stable,
  industry-standard hash" invariant and its cross-service agreement claim. **Recorded, not fixed.**
- **A semantic regression the owner accepted implicitly:** ADR-017 sold "cross-service trace
  consistency for free" — a peer running the same algorithm agreed with us. We now re-derive rather
  than agree, so a peer at a *different* rate will disagree where it previously did not. The ADR
  amendment says so.
- **Genuinely new, outside the finding set — NOT fixed.** The approach-check flagged that Sentry has
  the same hole: the SDK's ASP.NET Core integration continues an inbound `sentry-trace` header and
  honours its sampled flag ahead of `TracesSampleRate` (`Program.cs:54` ships `0.1`). It asked me to
  verify before filing and I have not, so it is recorded here rather than in the inbox.
- **Boundary:** `DeterministicTraceIdSampler`'s `Kind == ActivityKind.Server` guard was written
  assuming Server implies root or remote. A Server-kind child under a *local* parent would now be
  held rather than dropped. Nothing in this app creates one.

### Cluster C — Sentry capture scope (F4)

- **Adversarial approach-check ran and corrected the discriminator**, then reality corrected it
  again. The check showed `ct.IsCancellationRequested` alone is wrong: .NET decides
  timeout-vs-cancellation *at throw time*, so a genuine 5s Google timeout followed a few
  milliseconds later by the user closing the tab reads as "caller left" and the outage never reaches
  Sentry — and that is the population that aborts most. It proposed
  `ex.InnerException is not TimeoutException`. **I implemented that and it still failed**, because
  `HttpClient.HandleFailure` wraps: the observed chain is
  `TaskCanceledException → TaskCanceledException → TimeoutException`. The shipped discriminator is
  `ex.GetBaseException() is not TimeoutException`, which is depth-independent.
- **The check also found the same class twelve lines down, and it was worse.** A bare
  `catch { throw new UnauthorizedException(...); }` around the response body read turned a client
  abort into a **401**, and per CLAUDE.md the SPA treats 401 as logout. Fixed with the same
  client-abort rethrow.
- **The test harness could not have exercised either fix.** `StubHttpHandler` ignored its
  cancellation token, and the pre-existing "timeout" test hand-threw a `TaskCanceledException` with
  a null inner — which passes under a correct discriminator *and* a broken one. Added a hanging
  handler and a handler reproducing the real timeout-then-abort shape.
- **Deliberately NOT claimed: this does not eliminate the log noise.** `UseSerilogRequestLogging` is
  registered inside the exception handler, so it still logs the aborted request at `Error` with the
  exception present. What the fix removes is the Sentry issue and the middleware's own Error line.
- **Boundary:** the body-read guard is defensive only. `GetAsync` uses the default
  `ResponseContentRead`, so the content is already buffered before `ReadAsStringAsync` runs and the
  cancellation path there is not reachable today. No test — stated rather than faked.

### Cluster E — scrape-port boot check (F6)

- **The approach-check REFUTED my design outright, with measurements from a throwaway probe.** A
  throw from an `ApplicationStarted` callback is caught by `ApplicationLifetime.NotifyStarted` and
  logged as `crit`, and the app keeps serving — I would have shipped a guard that does nothing,
  which is worse than no guard. A plain `IHostedService.StartAsync` is no good either: `Build()`
  re-appends `GenericWebHostService` last, so app services start *before* Kestrel binds and the
  address list is empty. `IHostedLifecycleService.StartedAsync` is the only hook that both sees the
  bound addresses and aborts the host.
- **Parsing:** `BindingAddress.Parse`, not `new Uri` — `Uri.TryCreate` returns false for both
  `http://+:8080` and `http://*:8080`, and returns *true with Port=80* for a unix socket, which
  would have been a silently wrong answer.
- **The carve-out is "no addresses reported", not an environment check.** A TestServer host reports
  the feature present but empty, which is what keeps the two integration tests that set
  `ScrapePort=9090` booting. A real Kestrel always reports at least one address, so the carve-out
  cannot mask a production misconfiguration. An environment carve-out was rejected: §14.8 rolls
  metrics out on staging first, so a Production-only check would miss the first place it matters.
- **Rule 2 compares distinct ports, not address count** — `http://127.0.0.1:9090;http://10.0.0.5:9090`
  is two addresses and one port, still the exposure.
- **Deliberate boundary — no test proves the abort.** Every integration test in this repo uses
  TestServer, where the check is skipped by design, so a `WebApplicationFactory` test could only
  prove the skip. The decision function is unit-tested against the real measured address strings
  instead; a real-Kestrel boot test is the honest gap.
- **Not folded in, needs the owner:** `ScrapePort == 0` (serve `/metrics` on every listener) is
  still only a warning, which is now inconsistent — it is the same exposure rule 2 makes fatal. A
  one-line change, but a scope change.
- **Genuinely new, outside the finding set — NOT fixed.** `Dockerfile:41` sets
  `ASPNETCORE_URLS=http://+:8080` with `EXPOSE 8080` only, and `docker-compose.yml` is single-port;
  only `docker-compose.prod.yml` adds 9090. After this change, running the image with a copied
  `.env` carrying `ScrapePort=9090` refuses to start — which is the intent, but the Dockerfile
  should carry the invariant.

### Cluster D and G — guarantees nothing enforced (F5, F9, F10) and test isolation (F8)

- **No approach-check: all four are test-only** apart from nothing. The one production-adjacent
  judgement is `MetricCapture`'s scoping mechanism, which runs only in tests.
- **Each was proven by the mutation the review named, not by assertion.** F9: deleting
  `SetBeforeBreadcrumb` left 358 tests green before, reddens exactly one now. F10: reverting
  `LogError` to `LogWarning` left 24 green before, reddens one now. F5: both one-line renames the
  finding cited (`Labels.Result`, `WebhookResultValues.Ok`) now redden. F8: removing the context
  gate reddens the isolation test.
- **F8's mechanism, and why not the obvious alternative.** Serialising the metric tests into one
  non-parallel collection was the first idea and it does not work: the set of classes that *emit*
  business metrics is much wider than the set that *captures* them (anything exercising an order,
  upload, webhook or AWB path), so the collection would have had to swallow most of the suite. The
  execution context is the thing that actually distinguishes "this test's work" from "another
  test's work", and it flows into awaited work, which is what real call sites do.
- **F8 replaces an unproven flake risk with a real, if narrow, false-green risk** — stated plainly
  because it cuts against the fix: a measurement emitted on a thread that did **not** inherit the
  test's execution context is now silently invisible rather than wrongly attributed. Nothing in the
  current call sites does that. The micro-review was asked about it specifically.
- **F5 also closes a deferred backlog item as a side effect:** `D57` (the panel walker ignoring
  Grafana row panels) is fixed by the same function, since leaving it would have been a new blind
  spot in the very check being strengthened.
- **Boundary:** F5 checks literal `=` matchers only. A regex or negative matcher (`=~`, `!~`, `!=`)
  has its label *name* checked but not its value, because such a matcher does not have to name a
  value that exists.

### Fix-diff micro-reviews — both found real defects in my own work

Two fresh-eyes agents over the round's diff, split by risk: one over the behaviour changes
(F1, F2, F4, F6), one over the contract and test changes (F3, F5, F8, F9, F10, F11). **15 findings
between them.** Repaired in `7e28317`:

1. **I corrupted `slos.md` while documenting F11.** My inserted paragraph swallowed SLO 3's
   `**Action on breach:**` heading, leaving an orphan sentence fragment — so the one SLO whose
   ownership row says "Sentry pages immediately" was the one with no owner action. Restored.
2. **My F5 fix re-introduced the class it was closing.** The new `slos.md` text promised "every
   literal label value against `MetricNames`", but the value arm skips any metric not in
   `LabelContract` — which is exactly SLO 2's `http_route="api/payments/stripe/intent"` and
   `http_request_method="POST"` on the framework histogram. Renaming that controller route would
   still empty the panel with a green build. Fixed both ways: the claim now says what is actually
   checked, and the test **names the two unverifiable matchers in an assertion**, so a third one is
   a test failure rather than a silent gap.
3. **Nothing pinned the hook choice on F6 or the call site on F2** — the two places where the whole
   fix lives. A refactor to `AddHostedService<ScrapeListenerGuard>()` with only `StartAsync`, or
   back to `ApplicationStarted`, returns to log-and-shrug with every test green. Added a test that
   `AddObservability` registers the guard *and* that it implements `IHostedLifecycleService`. The
   `BuildSampler` call-site equivalent is **not** closed — see boundaries.
4. **The label-matcher regex truncated at the first `}`**, so a route-template value
   (`http_route="api/orders/{id}/payments"`) would have yielded zero usages for that query and been
   skipped silently. Both the query parser and the exposition parser now find the closing brace
   outside quotes.
5. **Nested `MetricCapture`s silently blinded the outer one** — the second constructor took the
   context, and the first's "nothing was recorded" assertions would then pass vacuously. Now throws.
6. **My own DEPLOYMENT.md sentence was false.** I wrote that forcing a trace on with `…-01` no
   longer works. The trace id still steers a deterministic hash, so a chosen `traceparent` does
   force a trace — which is the brute-force residual recorded above, and also the legitimate way to
   debug one request. Corrected, and the "raise the rate" advice with it.
7. **`metrics.md`'s `duplicate` and `failed` rows were stale after F1** — still describing
   `duplicate` as "order already in `Paid`". Rewritten.
8. **The second catch in `GoogleTokenValidator` was asymmetric** — it had the client-abort rethrow
   but not the `TimeoutException` carve-out its sibling grew, so a slow-body Google outage racing a
   navigation would have vanished from both the 502 SLO and Sentry. Same discriminator now.

**One micro-review hypothesis was wrong and is recorded as such:** the behaviour reviewer suspected
`BindingAddress.Parse` returns port 80 for a unix socket rather than throwing, which would make the
carve-out dead code and `An_address_with_no_port_is_not_counted_as_a_listener` fail on every run.
That test passes, which disproves it — had it returned 80, the verdict would have been `null` and
the assertion would have failed.

### Remaining boundaries — not fixed, for the re-reviewer

- **Nothing proves `ObservabilityExtensions.cs:71` calls `BuildSampler`.** The pipeline tests build
  through the seam, so re-wrapping the production call as
  `new ParentBasedSampler(BuildSampler(...))` restores F2's defect with no red test.
  `TracingExporterSelectionTests` boots the real `AddObservability` and would be the seam, but
  `TracerProvider` does not expose its sampler, so pinning it needs reflection or a redesign.
- **`MetricCapture`'s context scoping can now miss a real emission** rather than over-capture one:
  a capture constructed inside an `async` helper, or an emission from a thread that never inherited
  the context, records nothing. Four existing "no measurement was recorded" assertions would go
  vacuously green in that case. Every current construction site is the test body, so this is latent
  — but it is a false-green risk traded for a flake risk, and the re-reviewer should weigh it.
- **`ScrapeListenerGuard` aborts after Kestrel has bound.** `StartedAsync` is the earliest hook that
  can both see addresses and abort, so in the "only listener" verdict the process does serve
  `/metrics` on the proxied port for the start→exit window, repeating each restart-loop iteration.
- **`ScrapePort == 0` is still only a warning**, which is inconsistent now that its equivalent is
  fatal. Flagged to the owner, not folded in.
- **Docker image defaults still contradict the new guard**: `Dockerfile:41` is single-port with
  `EXPOSE 8080` only, so the image plus a copied prod `.env` refuses to start. Intended, but the
  Dockerfile should carry the invariant.

### On this round's runtime metric

`round-start` through `round-end` spans one continuous working session with a single owner gate,
so the `blocked_s` figure is a real measurement rather than a backfill — unlike round 1, whose
resolution records why its runtime is not comparable.
