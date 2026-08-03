---
type: resolution
target: 044-045-observability
version: 1
answers: review-v1.md
status: open
fixed_commit: null
closed: null
findings:
  F1:  { status: fixed, commit: "9fb6858, a054fdd", note: "topology fix, not a header fix: /metrics is now served only on an unproxied scrape listener (Observability:Metrics:ScrapePort; any other listener gets 404) and the Caddyfile refuses /metrics* at the edge; ADR-018 amended (its 'bind to a non-default port' rejection superseded). Adversarial approach-check ran and CHANGED the design — see decisions. New surface: the scrape-port gate and its wrong-listener log" }
  F2:  { status: fixed, commit: 44c3e2d, note: "new SentryDataScrubbers.Register wires all three egress hooks (BeforeSend, BeforeSendTransaction, BeforeBreadcrumb); regression test captures real SentryClient envelopes via a stub ITransport and fails when the transaction hook is removed. New surface: the Register mechanism plus a fail-closed catch that drops the payload — the SDK sends the RAW payload if the hook throws (measured, see decisions)" }
  F3:  { status: fixed, commit: "44c3e2d, bea8c98", note: "query-string values redacted (parameter names kept), URL query/fragment/credentials stripped, applied to Request.Url, span descriptions and breadcrumb URLs" }
  F4:  { status: fixed, commit: 44c3e2d, note: "header matching replaced by a case-insensitive allow-list, so HTTP/2 lowercase names cannot leak by omission; the defect class (deny-list miss) is structurally removed rather than patched" }
  F5:  { status: fixed, commit: "3438475, 3ca89b4", note: "route matching removed rather than repaired: measured against the running stack, the sampler is handed Tags=null at span start, so neither http.route NOR the review's suggested url.path exists. RouteAwareSampler -> DeterministicTraceIdSampler, one service-wide rate; Observability:Sampling:Routes deleted from settings/appsettings/validator and boot now ABORTS if a deployment still sets it. ADR-017 amended (2 amendments), decision-index + DEPLOYMENT §14.7/§14.11 + .env.example updated. Adversarial approach-check ran and killed the in-process alternative — see decisions. OWNER CALL: this moves story 003's capability to the collector" }
  F6:  { status: fixed, commit: "33474bc, 3ca89b4", note: "out-of-rate spans are sampled RecordOnly instead of Drop, so ErrorOverrideProcessor.OnEnd actually runs and promotes errored spans to exported; holding is limited to ActivityKind.Server so background-job EF roots keep dropping (they would materialise SQL text). Promoted spans carry fototipar.sampling.error_override because they arrive with no children. Regression tests run the real OTel SDK (SamplingPipelineTests) and redden when the decision goes back to Drop; ErrorOverrideProcessor got its first test file. New surface: the promotion tag + the RecordOnly hold — see decisions for what it does NOT give you" }
  F7:  { status: fixed, commit: 6df47b2, note: "every terminal webhook branch now records payment_webhook_total and logs, including the EuPlatesc fall-through (charged-but-unpaid) and the Stripe payment-failed early returns; new Tests/Helpers/MetricCapture.cs drives a MeterListener so the assertions observe real emissions, 8 regression tests one per branch, metrics.md updated. RECORDED BY THE LOOP DRIVER FROM THE COMMIT — the fixer was cancelled before reporting, so what its adversarial approach-check flagged is LOST and the reddening was never demonstrated to me" }
  F8:  { status: open, commit: null, note: null }
  F9:  { status: open, commit: null, note: null }
  F10: { status: fixed, commit: "b4a3789, a054fdd", note: "new ScrapeIpAllowList parser shared by the middleware and the validator: entries are trimmed, CIDR is supported, and every unparseable entry aborts boot naming itself. Also rejects silently-useless forms the review did not name (octal 010.0.0.1, inet_aton '10', IPv4-mapped IPv6 ranges). New surface: the parser and its per-entry error messages" }
  F11: { status: fixed, commit: 7266f21, note: "registered AddSingleton so the parsed allow-list and the deny-log dedupe survive across requests; dedupe keyed on (peer, reason), bounded at 512 distinct entries with a one-shot Warning at the cap. New surface: that bounded cache" }
  F12: { status: fixed, commit: b4a3789, note: "peer and allow-list entries are canonicalized before comparison (IsIPv4MappedToIPv6 -> MapToIPv4, scope id stripped), covering plain entries and CIDR ranges; regression tests use ::ffff:10.42.0.5, which the pre-existing pure-v4/pure-v6 tests never did" }
  F13: { status: fixed, commit: "144584e, 3ca89b4", note: "DEVIATES from the suggested fix: instead of failing boot, a blank Otlp:Endpoint outside Development now skips the whole WithTracing pipeline (metrics keep working) and logs observability.tracing.disabled once at boot — failing boot would have broken DEPLOYMENT §14.8's documented metrics-first rollout stage and, via the F19 env-var leak, aborted unrelated test hosts nondeterministically. Console exporter is reachable only in Development. AddObservability gained an IHostEnvironment parameter; new TracingWired decision function; §14.6/§14.8/§14.11, .env.example and the settings doc corrected" }
  F14: { status: open, commit: null, note: null }
  F15: { status: open, commit: null, note: null }
  F16: { status: fixed, commit: ead3c12, note: "CreateForOrderAsync wraps the internal call in try/catch-rethrow and records awb_creation_total{result=error}, so a thrown attempt stays in the SLO denominator; shutdown cancellation (OperationCanceledException when ct.IsCancellationRequested) deliberately records nothing. New Error constant added to AwbResultValues.All and metrics.md, cardinality budget updated. 3 tests incl. the throw and the cancellation legs. RECORDED BY THE LOOP DRIVER FROM THE COMMIT — fixer cancelled before reporting; approach-check output LOST, reddening never demonstrated to me" }
  F17: { status: fixed, commit: c407685, note: "PARTIAL — only one of the two legs the review named. Fixed: the duration is computed into a nullable local and Recorded only after SaveChangesAsync returns, so a cancelled or failed commit records nothing (2 tests, incl. the commit-fails leg). NOT fixed: the concurrent double-click leg — no conditional write (WHERE ShippedAt IS NULL) and no once-only guard were added, so two simultaneous PATCHes can still both commit and both Record. The re-review must decide whether that leg is closed. RECORDED BY THE LOOP DRIVER FROM THE COMMIT — fixer cancelled before reporting; approach-check output LOST" }
  F18: { status: open, commit: null, note: null }
  F19: { status: fixed, commit: c809f30, note: "DEVIATES from the suggested fix: rather than set-and-restore the env vars, the test hosts no longer touch process state at all — IWebHostBuilder.UseSetting reaches builder.Configuration before Build() (WAF's DeferredHostBuilder passes host configuration to the entry point as --key=value args), so Sentry:Enabled/Dsn and Observability:* are per-host. The review's non-parallel collection is kept but narrowed to hosts that switch observability or Sentry ON (OTel providers and the Sentry SDK stay process-global listeners even with the config leak gone). New file ObservabilityHostCollection.cs; 2 regression tests in TestHostConfigurationIsolationTests both redden when a static-ctor env var is put back" }
  F20: { status: open, commit: null, note: null }
  F21: { status: fixed, commit: 2d25b03, note: "the enricher now runs against a real Sentry Scope behind a hub stub reporting IsEnabled=true, so the ConfigureScope body executes: correlation_id tag and scope.User.Id are asserted for an authenticated principal, plus the anonymous and no-correlation-id legs and a disabled-hub leg. Mutating ClaimTypes.NameIdentifier to a claim that never exists reddens it" }
  F22: { status: fixed, commit: 44c3e2d, note: "tests now cover SDK-populated shapes: a real DefaultHttpContext through Sentry.AspNetCore's ScopeExtensions.Populate, an SDK-shaped transaction with spans via SentryTransaction.FromJson, exception Mechanism.Data, Contexts.Response, SentryMessage, and a real SentryClient end-to-end asserting the serialized envelope carries no token or email" }
  F23: { status: fixed, commit: "4711fac, a054fdd", note: "DEPLOYMENT.md §14 written (14.1-14.12: flags, the two gates, Prometheus provisioning incl. the off-box case, allow-list syntax, OTLP, cost, rollout, an external-bypass curl matrix, playbook, env-var table). Stale-pointer class swept: the same missing §14 was cited by ADR-018 and ddd-02 and both now resolve; also added the Observability block to .env.example and §14 links to README and metrics.md" }
  F24: { status: deferred, commit: null, note: "🟡 — ledger backlog per the README router; 🟡/⚪ do not enter a fix round" }
  F25: { status: deferred, commit: null, note: "🟡 — ledger backlog; flagged to the owner in summary-v1 as a second unscrubbed egress path" }
  F26: { status: deferred, commit: null, note: "🟡 — ledger backlog per the README router" }
  F27: { status: deferred, commit: null, note: "🟡 — ledger backlog per the README router" }
  F28: { status: deferred, commit: null, note: "🟡 — ledger backlog per the README router" }
  F29: { status: deferred, commit: null, note: "🟡 — ledger backlog per the README router" }
  F30: { status: deferred, commit: null, note: "🟡 — ledger backlog per the README router" }
  F31: { status: deferred, commit: null, note: "🟡 — ledger backlog per the README router" }
  F32: { status: deferred, commit: null, note: "🟡 — ledger backlog per the README router" }
  F33: { status: deferred, commit: null, note: "⚪ — ledger backlog per the README router" }
  F34: { status: deferred, commit: null, note: "⚪ — ledger backlog per the README router" }
  F35: { status: deferred, commit: null, note: "⚪ — ledger backlog; cross-target (comment sweep belongs to the system loop)" }
  F36: { status: deferred, commit: null, note: "⚪ — ledger backlog per the README router" }
  F37: { status: deferred, commit: null, note: "⚪ — ledger backlog per the README router" }
  F38: { status: deferred, commit: null, note: "⚪ — ledger backlog per the README router" }
  F39: { status: deferred, commit: null, note: "⚪ — ledger backlog per the README router" }
---

# Resolution v1 — 044-045-observability

Fixer's answer to [review-v1.md](review-v1.md) (immutable). The review named 39 findings;
**23 serious ones (F1–F23) are the fix round**. The 16 🟡/⚪ (F24–F39) are deferred to the
[ledger](ledger.md) backlog per the README router — new low/cleanup findings do not enter a
fix round.

**Nothing here is `verified`.** Only `review-v2.md` (a re-review by someone who did not fix)
can set that status.

## Fix round scope

| Cluster | Findings | Owner file(s) |
|---|---|---|
| A — `/metrics` access control | F1, F10, F11, F12, F23 | `MetricsEndpointIpAllowListMiddleware.cs`, `ObservabilitySettingsValidator.cs`, `Caddyfile`, `DEPLOYMENT.md` |
| B — Sentry data egress | F2, F3, F4, F22 | `SentryDataScrubbers.cs`, `Program.cs` |
| C — tracing correctness | F5, F6, F13 | `Sampling/DeterministicTraceIdSampler.cs` (was `RouteAwareSampler.cs`), `ErrorOverrideProcessor.cs`, `ObservabilityExtensions.cs` |
| D — metric emission gaps | F7, F16, F17 | `WebhooksController.cs`, `AwbCreator.cs`, `AdminOrderService.cs` |
| E — test vacuity | F8, F9, F19, F20, F21 | the new test files + `SentryIntegrationFactory.cs` |
| F — error-signal routing | F14, F15, F18 | `ExceptionHandlerMiddleware.cs`, dashboard JSON, `slos.md` |

## Findings

| ID | Sev | Title | Status | Commit | How |
|---|---|---|---|---|---|
| F1 | 🔴 | `/metrics` allow-list checks the TCP peer (Caddy) | fixed | `9fb6858`, `a054fdd` | Scrape path served only on an unproxied listener (404 elsewhere) + Caddy refuses `/metrics*`; ADR-018 amended |
| F2 | 🔴 | Sentry transactions bypass the scrubber | fixed | `44c3e2d` | `Register` wires all three hooks; real-transport envelope test |
| F3 | 🔴 | Scrubber never touches query string / URL | fixed | `44c3e2d`, `bea8c98` | Query values redacted, URLs stripped of query/fragment/credentials |
| F4 | 🔴 | Case-sensitive header scrubbing | fixed | `44c3e2d` | Case-insensitive allow-list; the deny-list class is gone |
| F5 | 🔴 | Per-route sample rates never match | fixed | `3438475`, `3ca89b4` | Route matching removed (no tags exist at span start, measured); one deterministic service-wide rate, `Sampling:Routes` deleted and now aborts boot if still set; ADR-017 amended |
| F6 | 🔴 | "Errors always sampled" is dead code | fixed | `33474bc`, `3ca89b4` | Out-of-rate server spans held as `RecordOnly` so `OnEnd` runs and promotes errors; real-SDK pipeline tests redden on `Drop` |
| F7 | 🔴 | Webhook branches record no metric or log | fixed | `6df47b2` | Every terminal branch records + logs; `MetricCapture` MeterListener helper, 8 tests one per branch. Recorded by the loop driver from the commit — fixer cancelled before reporting |
| F8 | 🔴 | Sentry e2e test mocks `IHub` | open | — | — |
| F9 | 🔴 | No test observes a business metric | open | — | — |
| F10 | 🟠 | Unparseable allow-list entries silently dropped | fixed | `b4a3789`, `a054fdd` | Shared `ScrapeIpAllowList` parser; CIDR supported, every bad entry aborts boot naming itself |
| F11 | 🟠 | Allow-list middleware registered `Scoped` | fixed | `7266f21` | `AddSingleton`; deny-log dedupe bounded at 512 with a one-shot Warning at the cap |
| F12 | 🟠 | IPv4-mapped IPv6 peers never match | fixed | `b4a3789` | Peer and entries canonicalized before comparison, for plain entries and CIDR alike |
| F13 | 🟠 | Console span exporter silently on in production | fixed | `144584e`, `3ca89b4` | No OTLP endpoint outside Development ⇒ no trace pipeline at all (metrics unaffected) + a boot warning; console exporter is Development-only. Deviates from "fail boot" — see decisions |
| F14 | 🟠 | Dashboard queries metric names never emitted | open | — | — |
| F15 | 🟠 | Mapped 5xx and `LogError` bypass Sentry | open | — | — |
| F16 | 🟠 | AwbCreator throw path skips `RecordOutcome` | fixed | `ead3c12` | try/catch-rethrow records `result=error`; shutdown cancellation exempt. Recorded by the loop driver from the commit |
| F17 | 🟠 | Duration histogram recorded before `SaveChanges` | fixed | `c407685` | **PARTIAL** — recorded only after the commit succeeds (2 tests); the concurrent double-click leg is **not** guarded, no conditional write was added. Recorded by the loop driver from the commit |
| F18 | 🟠 | Sentry SDK failures wholly silent | open | — | — |
| F19 | 🟠 | Test factories leak process-wide env vars | fixed | `c809f30` | Per-host `UseSetting` replaces the env vars entirely; observability/Sentry hosts serialized in one collection; 2 leak-detecting tests |
| F20 | 🟠 | Cardinality tests are arithmetic over constants | open | — | — |
| F21 | 🟠 | Scope-enricher unit tests never run the body | fixed | `2d25b03` | Real `Scope` behind an enabled hub stub; `correlation_id` and `scope.User.Id` asserted; claim-type mutation reddens |
| F22 | 🟠 | Scrubber tests only use hand-built events | fixed | `44c3e2d` | SDK-populated event + SDK-shaped transaction + real-client envelope |
| F23 | 🟠 | `DEPLOYMENT.md §14` does not exist | fixed | `4711fac`, `a054fdd` | §14.1–14.12 written; the same missing pointer in ADR-018 and ddd-02 now resolves; `.env.example` + README updated |
| F24–F39 | 🟡⚪ | see [ledger](ledger.md) | deferred | — | Backlog per the README router |

## Decisions

- **F24–F39 deferred to backlog (16 findings).** The README router sends new 🟡/⚪ to the
  ledger rather than the fix round; they are drained deliberately by a groomer sweep, the next
  bolt in the area, or re-judged at certification. `F25` (EF spans ship unscrubbed SQL to the
  OTLP collector) was flagged to the owner in [summary-v1.md](summary-v1.md) as worth an early
  look despite its severity.
- **`F35` is cross-target.** The mangled comment residue comes from the repo-wide sweep
  (`09173c4`), which belongs to the `system` target's loop. Recorded here because this branch
  carries the lines; whichever loop fixes it should claim it.

### Cluster A — `/metrics` access control (F1, F10, F11, F12, F23)

- **Adversarial approach-check ran BEFORE implementation (security + deployment-topology lens,
  ~95k tokens) and changed the design.** The approach it was given was: (1) refuse `/metrics` at
  the Caddy edge, and (2) in the middleware, deny any request carrying `X-Forwarded-For` or
  `Forwarded` — using the header only to deny, never to grant, so that allow-listing the proxy
  could not re-open the endpoint. **It refuted (2) and I dropped it.** Three reasons, all
  checked against this repo: ASP.NET's `ForwardedHeadersMiddleware` *removes* the header once it
  consumes it, so the rule would silently stop working the day anyone wires it up (and this repo
  needs to — see the rate-limiter item below); a service-mesh sidecar adds `X-Forwarded-For` to
  legitimate in-pod scrapes, which ADR-018 explicitly targets, so it would deny correct
  configurations with a misleading "not in AllowedScrapeIps" message; and it only fires in a
  configuration that is already wrong. It also argued that edge-blocking **alone** is a
  config-only control that no test in `dotnet test` can turn red, and recommended the review's
  other suggestion — a separate internal listener — as the primary control. I took that.
  Two further corrections from the same check: `docs/DEPLOYMENT.md` already existed (F23 is an
  append, not a new file), and `respond 404` is not camouflage here because the SPA fallback
  makes every *other* unknown path return `200`; the ADR amendment therefore claims "no route",
  not "indistinguishable".

- **Why the scrape listener over edge-blocking alone.** The edge block is one Caddyfile edit away
  from being undone and is unprovable in the test suite; the listener gate is structural and is
  pinned by `Scrape_port_configured_makes_metrics_absent_on_the_public_listener`. Both shipped:
  the listener is the control, the edge refusal is the belt. ADR-018's own "bind to a non-default
  port = security by obscurity" rejection was **superseded in the amendment**, on the grounds
  that it conflated "hard to guess" with "no route exists" — this port is neither published nor
  proxied.

- **Fix-diff micro-review ran** (fresh-eyes agent over `bea8c98..HEAD`) and found four real
  defects in my own fix, all repaired in `a054fdd`:
  1. **The CIDR error message misdirected the operator to a real public network.** `010.0.0.0/16`
     parses as octal, so the "write it like this" suggestion said `8.0.0.0/16` — Level 3 address
     space. An operator following the message would have allow-listed 65k public addresses and
     the config would have validated. The suggestion now round-trip-checks its own re-parse.
  2. **`::ffff:10.42.0.0/112` validated but could match nothing** (peers are canonicalized to
     IPv4 before the family check) — the exact F10 class, reintroduced. Now rejected at boot.
  3. **The 404 path logged nothing** while §14.10 told operators to grep for it. Added
     `metrics.scrape.wrong_listener`, deduped through the same bounded map.
  4. **`.env.example` shipped `ASPNETCORE_URLS=http://+:8080` next to `ScrapePort=9090`** — the
     template I had just written was internally contradictory, and `docker run --env-file` would
     have 404'd every scrape. Fixed, and §14.4 now warns that publishing 9090 exposes the whole
     API, not a metrics-only port.
  It also caught that **nothing proved boot actually aborts** on a bad entry — the promise §14.5
  makes rests on `ValidateOnStart` wiring that no test touched, and I had deleted the suite's one
  malformed-entry factory because it now fails boot. `An_unparseable_allow_list_entry_aborts_boot`
  closes that (it also partly covers the deferred `F28`).

- **Deliberate boundaries — not fixed, flagged for the re-reviewer.**
  - **The scrape listener serves the entire API, not just `/metrics`.** `http://+:9090` is a
    second full pipeline; only the metrics path is gated. Inside the Compose network that is no
    new exposure (8080 is equally reachable there), and §14.4 warns against publishing it. A
    metrics-only listener would need separate Kestrel endpoint configuration — deliberately out
    of scope.
  - **The deny-log cap can overshoot** by the number of concurrent threads (the count is read
    before `TryAdd`). Bounded in practice, not worth a lock.
  - **`Observability:Metrics:PrometheusEndpoint` stays configurable** while the `Caddyfile`
    matcher is a literal. `F27` (that same knob accepting `"/"`) is deferred to the ledger, so I
    left the knob alone; the pairing obligation is now an ADR-018 invariant and is stated in
    §14.3, §14.11 and the property's own doc comment. The scrape-port gate means a missed pairing
    is a broken edge rule, not an exposure.
  - **No metric instrument for denials.** The micro-review suggested a counter on the existing
    meter; that would touch `MetricNames`, the cardinality-budget tests and `metrics.md`, which
    belong to clusters D/E/F. Logs only, for now.

- **Genuinely new, outside the finding set — NOT fixed, recorded for the re-reviewer.** The
  micro-review found F1's defect class alive in three other places. None are observability files
  and none are in this review's finding set, so I changed nothing:
  1. **The global rate limiter has F1 exactly.**
     `src/PhotoPrint.API/Extensions/SecurityExtensions.cs:58-69` partitions on
     `context.Connection.RemoteIpAddress`, which behind Caddy is one value for the whole
     internet — so the documented "100/min per IP" is in fact 100/min *in total*, and one client
     at ~2 rps can 429 the entire site. Nothing in `src/` calls `UseForwardedHeaders`. This is
     arguably more damaging than the finding I was asked to fix, and it is the reason the
     `X-Forwarded-For` approach was rejected above: the fix for this one is precisely to wire
     `UseForwardedHeaders` with `KnownProxies`.
  2. **The auth rate limiters are unpartitioned**, not per-IP:
     `src/PhotoPrint.API/Extensions/AuthExtensions.cs:84-106` — 5 registrations/hour and 3
     forgot-password/hour site-wide, while `SecurityExtensions.cs:70` describes them as per-IP.
  3. **Security-audit logs record the proxy's IP as the client's.**
     `src/PhotoPrint.API/Controllers/AuthController.cs:54, 72, 160` log
     `Connection.RemoteIpAddress` as the source of login/register attempts, so the brute-force
     trail is uniformly Caddy.
  Also stale, and left alone deliberately: `ddd-02-technical-design.md` (lines 96, 119, 155,
  160-166, 184, 316) and `ddd-03-test-report.md` (27, 28) still describe the pre-fix `/metrics`
  design and test counts. These are bolt construction records rather than live standards, and
  `F36` already covers ddd-02 drift as a deferred cleanup — but `ddd-02` is what an implementer
  reads for "how does this work today", so whoever drains `F36` should fold these in.

### Cluster B — Sentry data egress (F2, F3, F4, F22)

- **Took the review's inverse posture.** F2/F3/F4 were fixed as one class rather than three
  patches: `SentryDataScrubbers` is now deny-by-default (allow-lists for headers, request env,
  extras, span/breadcrumb diagnostic keys) applied at **all three** SDK egress hooks through
  `SentryDataScrubbers.Register(SentryOptions)`. A field the SDK adds in future arrives
  redacted, so the "fourth leak" the review predicted cannot happen by omission. This deletes
  the old public surface (`SensitiveHeaders`, `SensitiveFieldNames`, `IsSensitiveKey`) and
  redacts things nobody asked about — all `Extra` values, `User` fields other than `Id`,
  `Contexts.Response`, exception `Mechanism.Data`, `SentryMessage` params. Nothing in the repo
  writes those today, so the triage cost is zero now and the posture holds later.

- **Adversarial approach-check ran** (security/privacy lens, ~95k tokens) before implementation.
  It flagged, and this fix folded in: (1) **span descriptions and breadcrumbs are a live
  credential path** — `GoogleTokenValidator.cs:37` puts a raw Google OAuth `id_token` in a query
  string, and Sentry's HttpClient instrumentation copies that URL into `SentryTransaction.Spans[].Description`
  and a breadcrumb, neither reachable from `BeforeSend`; hence the third hook and span/breadcrumb
  URL sanitising. (2) `IEventLike` does **not** carry `Extra`/`Message`/`SentryExceptions`/`Spans`,
  so the "one shared implementation" had to be per-type — it is. (3) `docs/DEPLOYMENT.md:884`
  and the bolt-045 plan/walkthrough documented the old contract; all updated. It also asserted
  Serilog kills the MEL→Sentry breadcrumb path (**confirmed**), and that a throwing `BeforeSend`
  fails closed (**refuted** — see below).

- **The SDK fails OPEN when the scrub hook throws — measured, not assumed.** A probe against
  real Sentry 4.13.0 with a capturing `ITransport` showed that when `BeforeSend` or
  `BeforeSendTransaction` throws, the SDK logs and then sends the **original, unscrubbed**
  payload (guest token and email both present in the envelope). `implementation-walkthrough.md:80`
  claimed the opposite ("a throw drops the event silently") — corrected in the same change. The
  scrubber therefore catches everything, logs at `Error` via Serilog's global logger, and
  returns null so the payload is dropped. **New surface for the re-reviewer:** that catch, and
  the three-hook `Register` mechanism.

- **Fix-diff micro-review ran** (fresh-eyes agent over the diff). It found no leak, and its
  actionable items were fixed in `bea8c98`: a schemeless URL value carrying an address
  (`mailto:…`) passed through unredacted, and a degenerate query string (`?`, `?&&`) was turned
  into fabricated `<scrubbed>` parameters. It also confirmed `Serilog.Log.Logger` really is
  assigned globally by `UseSerilog`, so the drop is not a silent failure.

- **Deliberate boundaries — not fixed, flagged for the re-reviewer.**
  - **Exception messages and span descriptions are kept in full.** An exception whose text
    interpolates a customer email still ships; so does an EF span description (parameterised
    SQL — `EnableSensitiveDataLogging` is off, so no literal values). Redacting either would
    gut triage. The same applies to `SentryMessage.Message` when a caller interpolates instead
    of using a template. No such call site exists in `src/PhotoPrint.API` today (grepped).
  - **`Contexts` keys other than `response` are left alone** — they are SDK-owned (app, os,
    runtime, trace) and no app code writes contexts.
  - **The OTLP exporter is a second, unscrubbed egress path** and is untouched here; that is
    `F25`, deferred to the ledger.
  - **This fix does not prove `Program.cs` calls `Register`.** The new envelope test proves
    `Register` wires all three hooks, but the boot wiring itself is still only covered by the
    `IHub`-mocking integration test that `F8` condemns. Closing `F8` (cluster E) is what makes
    the wiring provable.

- **Genuinely new, outside the finding set — not fixed.**
  `ReliableEmailServiceTests.SendAsync_FailedSend_QueuesEmailToDatabase`
  (`src/PhotoPrint.Tests/Unit/Services/ReliableEmailServiceTests.cs:75`) asserts
  `NextRetryAt > UtcNow.AddSeconds(-1)` against a value set to `queue-time + 1s`, giving the
  test a 2-second wall-clock budget that includes EF InMemory's first-query compilation. Under
  machine load it failed once during this round (11s test, 10.2s between queue and assert) and
  passed on a quiet re-run. Latent flake, unrelated to observability, present at the reviewed
  commit too.

### Cluster C — tracing correctness (F5, F6, F13)

- **Adversarial approach-check ran BEFORE implementation (~154k tokens, OTel-SDK + resource
  lens) and changed the design twice.** The approach it was given was the review's own
  suggestion plus a rescue for F5: sample `RecordOnly` instead of `Drop`, and key per-route
  rates off `IHttpContextAccessor` (method + raw path) since no route tag exists at span
  start. It verified the promotion mechanism against the OTel 1.11.2 sources — `OnEnd` is
  gated on `IsAllDataRequested` only, never on `Recorded`, and both export processors re-read
  `Recorded` live — so F6's fix stands. It then refuted the F5 rescue on two grounds I had not
  weighed: registering `IHttpContextAccessor` makes ASP.NET stop pooling `DefaultHttpContext`,
  so **every request in the process** pays for a telemetry feature; and matching a raw path
  against `{id}`-shaped keys needs a matcher whose case / trailing-slash / `HEAD` / `OPTIONS` /
  catch-all edges are each a silent miss — the same defect class as F5 itself. It also warned
  that ASP.NET route templates arrive as `api/products/{id:guid}` (no leading slash,
  constraints kept), so the config key shape was wrong in both the old and the proposed
  design. I dropped the in-process rescue and removed the feature instead.

- **Measurement, not reading.** Before designing anything I ran a throwaway probe (real
  ASP.NET Core 8 host + real OTel pipeline + a capturing sampler) and deleted it afterwards.
  It established, at this stack's versions: at span start the sampler receives
  `name = "Microsoft.AspNetCore.Hosting.HttpRequestIn"`, `kind = Server`, and **`Tags` is
  literally null**; `Drop` produces no `OnEnd` and no export; `RecordOnly` produces `OnEnd`
  with `Recorded = false`, the `http.route` tag present, and `Status = Error` for a 500 that
  `ExceptionHandlerMiddleware` produced; and a child started under a `RecordOnly` parent is
  never created. **One correction to the review**: findings-v1 F5 says to key on `url.path`,
  "which *are* available at start". They are not — `url.path` is written in the
  instrumentation's `OnStartActivity`, which runs after the sampler has already returned.
  That half of the suggested fix is not implementable on .NET 8.

- **What F6's fix does NOT give you, stated plainly.** A promoted span is exported **alone** —
  its children were dropped when it started, and no head sampler can know at start that a
  request will fail. It also carries **no exception**: `ExceptionHandlerMiddleware` catches
  everything, so the hosting layer never reports an unhandled exception and the
  instrumentation's `RecordException = true` never fires. What you get is route, status code,
  duration and a trace id. Stack traces come from Serilog and Sentry. Whole-trace error
  sampling needs the collector. All of this is written into the ADR-017 amendment rather than
  left for the next reviewer to rediscover.

- **`Sampling:Default = 0.0` changed meaning** from "drop everything" to "export errored spans
  only". The off switch is `Observability:Enabled = false`. The approach-check argued for
  keeping `0.0` as a true no-op cost lever; I kept the rescue unconditional because "errors
  are always sampled" is unconditional in the story and in findings-v1's suggested test, and
  because with per-route rates gone `0.0` is now a coherent errors-only mode. Cost is bounded
  to one server span per out-of-rate request: children are never created.

- **Fix-diff micro-review ran** (two fresh-eyes agents, one per change) and found one real
  behavioural defect plus doc drift, all repaired in `3ca89b4`:
  1. **Background jobs got worse, not better.** Every EF command a `BackgroundService` issues
     is its own **root** span, so `RecordOnly` would have held it, materialising
     `db.statement`, and an errored one would have been promoted and exported **with SQL
     text** — in the same round whose F13 fix is about not leaking SQL. Holding is now limited
     to `ActivityKind.Server`; non-server roots keep `Drop`. The ADR amendment's cost claim was
     corrected in the same commit (it had said no SQL is ever materialised, false for
     background roots).
  2. **`Sampling:Routes` left in a deployment would have been silently ignored** — binding
     drops unknown keys and the validator no longer had a rule. Boot now aborts naming the key
     and where the rate moved.
  3. **Doc self-contradictions**: §14.7 still said "drop individual routes" eleven lines under
     the new "there is no per-route rate"; the unqualified "never a partial trace" survived the
     promotion change; `.env.example` still said a blank endpoint sends spans to stdout.
  4. **My own new advice was wrong**: §14.6 told operators to grep stdout for
     `observability.tracing.disabled`, but production Serilog writes to a rolling **file**, not
     stdout. Reworded. (The underlying doc error is recorded below.)
  5. **Comment-rule breaches I had introduced** — `///` blocks on concrete classes and
     multi-line comments narrating the change — removed across all six touched files, including
     the two the brief named. `ObservabilityExtensions`'s pre-existing `///` block (with its
     `ddd-02` citation) went with them.

- **Deliberate boundaries — not fixed, flagged for the re-reviewer.**
  - **No test proves the production processor order.** `SamplingPipelineTests` builds its own
    provider; move `AddProcessor(new ErrorOverrideProcessor())` below the exporter in
    `ObservabilityExtensions` and every test still passes while promotion silently dies in
    prod. I could not close this without flakiness: a host-based test **was** written, passed
    in isolation, and failed in the full suite because every `TracerProvider` alive in the
    process samples the same `Microsoft.AspNetCore.Hosting` source — a long-lived
    `IClassFixture` host booted with `Observability__Enabled=true` (the F19 leak) forces every
    span in the process to `Recorded`, so per-host sampling assertions cannot hold. It is
    deleted, not disabled. **Fixing F19 first would make this test possible**; whoever takes
    F19 (cluster E) should re-add it.
  - **Only rates 0.0 and 1.0 run through the real SDK**, and only via
    `SimpleActivityExportProcessor`; production uses the batch processor from
    `AddOtlpExporter`. Intermediate rates are covered by direct sampler calls only.
  - **`ddd-01`, `ddd-02`, `ddd-03`, `bolt.md` and story 003 still describe per-route sampling
    as shipped**, and `ddd-03` cites `RouteAwareSamplerTests.cs`, a file that no longer exists.
    Left alone: these are point-in-time bolt/story records, and rewriting a story's acceptance
    criteria is an owner decision, not a fixer's. `F36` already covers ddd-02 drift.
    `docs/architecture-analysis-2026-05-25.md:537` (a dated analysis) still suggests tuning
    per-endpoint sample rates.
  - **`fototipar.` is a new telemetry namespace** — the promotion tag is the only identifier
    using it, and `MetricNames.cs` (documented as the single source of truth for names) does
    not know about it. Span attributes are not metric labels, so there is no cardinality budget
    to blow, but a groomer may want them in one place.

- **OWNER DECISION REQUIRED: F5's fix removes a shipped capability.** Story 003
  (per-route sampling) is no longer implemented in the application. It never worked — that is
  the finding — but the fix relocates it to a component this repo does not yet run: an OTel
  collector with a `tail_sampling` processor. `DEPLOYMENT.md` §14 already requires a collector
  in production, so the landing place exists, but nothing in `docker-compose.prod.yml`
  provisions one and no tail-sampling policy is written. Until then the service has one global
  trace rate. If the owner would rather keep an in-process approximation, the
  `IHttpContextAccessor` path is the only one available and its costs are recorded above.

- **Genuinely new, outside the finding set — NOT fixed, recorded for the re-reviewer.**
  1. **`docs/DEPLOYMENT.md:1220` states "Serilog writes to stdout only". It is false in
     production.** `appsettings.json` configures a rolling **File** sink; the Console sink
     exists only in `appsettings.Development.json`, and nothing in `docker-compose.prod.yml`,
     `Dockerfile` or `.env.example` overrides `Serilog__WriteTo`. So `docker compose logs api`
     shows no application logs at all in production, and §14.9's and §14.10's log-grep
     instructions — plus the §5 boot check ("Logs clean … no `OptionsValidationException`") —
     do not work as written. This is bigger than the tracing cluster and needs its own finding.
  2. **`ScrapePort = 0` is F13's defect class, unfixed.** A dev-only permissive default
     (`0` = serve `/metrics` on every listener) is reachable in production and is warn-only.
     Cluster A chose the warning deliberately; noting it because the two decisions should be
     consistent — F13 hard-gated its dev convenience on `IsDevelopment()`, this one did not.
  3. **`IsDevelopment()` is a case-insensitive string comparison**, so
     `ASPNETCORE_ENVIRONMENT=development` reaches the console exporter. Consistent with
     `SecurityExtensions.cs:115` (HSTS), so not new — but nothing pins it.

## Owner decisions (recorded by the loop driver, 2026-08-03)

- **F5 — deleting per-route sampling is ACCEPTED.** The owner reviewed the trade-off (the
  measured `Tags = null` at span start, the `DefaultHttpContext` de-pooling cost of the only
  in-process alternative, and the matcher edge cases that would reproduce F5's own defect
  class) and accepted the removal rather than a repair or an immediate collector build-out.
  Story 003's per-route capability is therefore **a known, accepted gap**, not an oversight:
  the service runs one deterministic trace rate, and per-route control is deferred to
  collector-side tail sampling that is not yet provisioned. The re-review should judge the
  fix as delivered, not reopen the design.
- **The rate-limiting defect found during this round goes to the review inbox, not this
  ledger.** It is auth/security rather than observability, so it does not enter this
  ledger; see [`reviews/inbox.md`](../inbox.md). The owner declined to open a review
  target for it (2026-08-03) — a target folder is created only when the owner opens a
  loop. Recorded here only so the trail from the F1 defect class to its siblings is not
  lost.
