---
type: review-ledger
target: 044-045-observability
updated: 2026-07-31
---

# Canonical finding ledger — 044-045-observability

Stable `D#` identities for this target, per the README's persistent-ledger standard. Each real
defect gets a `D#` that lives forever; each pass's pass-local `F#` maps onto a `D#` **after**
the blinded pass completes (finders never see `D#`).

**v1 is the first pass**, so `F#` ↔ `D#` is 1:1 and the `reconcile-findings` skill had nothing
to match against — the ledger did not exist before this pass. All 39 rows are minted new. From
v2 onward the reconciler runs normally.

**Severity is the v1 synthesis severity.** One row differs from the lens maximum: **D33** was
reduced low → cleanup because the adversarial skeptic proved both switch defaults unreachable
(see [findings-v1.md](findings-v1.md#-f33--d33--magic-unknown-label-value-escapes-the-metricnames-contract)).

**Status vocabulary.** `open` = named, not yet fixed · `in-progress` · `fixed` = fixed with a
regression test, awaiting verification · `verified` = a re-review proved the fix holds ·
terminal: `wont-fix` · `deferred` · `disputed` · `false-positive` · `backlog` = triaged
Low/Cleanup that does not re-arm the loop. **Nothing here is `verified` — only a verification
pass can set that.**

| D# | Sev | First seen | Title | File | Status |
|---|---|---|---|---|---|
| D1 | 🔴 | v1 (F1) | `/metrics` allow-list checks the TCP peer, which behind Caddy is the proxy IP | `Middleware/MetricsEndpointIpAllowListMiddleware.cs:41` | open |
| D2 | 🔴 | v1 (F2) | Sentry transactions bypass the scrubber (`SetBeforeSendTransaction` absent) | `Program.cs:57` | open |
| D3 | 🔴 | v1 (F3) | Scrubber never touches `Request.QueryString`/`Url` — emails and tokens ship | `Configuration/SentryDataScrubbers.cs:44` | open |
| D4 | 🔴 | v1 (F4) | Case-sensitive header scrubbing — HTTP/2 lowercase names survive | `Configuration/SentryDataScrubbers.cs:46` | open |
| D5 | 🔴 | v1 (F5) | Per-route sample rates can never match; every route uses `Default` | `Observability/Sampling/RouteAwareSampler.cs:63` | open |
| D6 | 🔴 | v1 (F6) | "Errors always sampled" is dead code — `OnEnd` skipped for dropped spans | `Observability/ErrorOverrideProcessor.cs:18` | open |
| D7 | 🔴 | v1 (F7) | Webhook fall-through / re-delivery branches record no metric and no log | `Controllers/WebhooksController.cs:216` | open |
| D8 | 🔴 | v1 (F8) | Sentry e2e test mocks `IHub`, so the scrubber never runs in any test | `Tests/Integration/SentryIntegrationFactory.cs:85` | open |
| D9 | 🔴 | v1 (F9) | No test observes any business metric being emitted | `Tests/Integration/MetricsEndpointIntegrationTests.cs:50` | open |
| D10 | 🟠 | v1 (F10) | Unparseable allow-list entries (CIDR, padded) silently dropped; validator too weak | `Middleware/MetricsEndpointIpAllowListMiddleware.cs:33` | open |
| D11 | 🟠 | v1 (F11) | Middleware registered `Scoped` — deny-log dedupe never fires, list re-parsed per request | `Extensions/ObservabilityExtensions.cs:50` | open |
| D12 | 🟠 | v1 (F12) | IPv4-mapped IPv6 peers never match IPv4 allow-list entries | `Middleware/MetricsEndpointIpAllowListMiddleware.cs:42` | open |
| D13 | 🟠 | v1 (F13) | Empty `Otlp:Endpoint` silently enables the console span exporter in production | `Extensions/ObservabilityExtensions.cs:78` | open |
| D14 | 🟠 | v1 (F14) | Dashboard and `slos.md` query metric names the API never emits | `ops/dashboards/fototipar-overview.json:309` | open |
| D15 | 🟠 | v1 (F15) | Mapped 5xx and all Serilog `LogError` bypass Sentry | `Middleware/ExceptionHandlerMiddleware.cs:141` | open |
| D16 | 🟠 | v1 (F16) | AwbCreator throw path skips `RecordOutcome` — SLO 4 reads healthy during an outage | `Services/Sameday/AwbCreator.cs:45` | open |
| D17 | 🟠 | v1 (F17) | Processing-duration histogram recorded before `SaveChanges`, no once-only guard | `Services/AdminOrderService.cs:133` | open |
| D18 | 🟠 | v1 (F18) | Sentry SDK failures wholly silent (`Debug=false` ⇒ no `DiagnosticLogger`) | `Program.cs:56` | open |
| D19 | 🟠 | v1 (F19) | Test factories set process-wide env vars in static ctors under parallel xUnit | `Tests/Integration/SentryIntegrationFactory.cs:32` | open |
| D20 | 🟠 | v1 (F20) | Cardinality tests are arithmetic over constants; asserted counts already wrong | `Tests/Unit/Observability/MetricsCardinalityTests.cs:20` | open |
| D21 | 🟠 | v1 (F21) | Scope-enricher unit tests run with no `IHub`; body never executes | `Tests/Unit/Middleware/SentryScopeEnricherMiddlewareTests.cs:17` | open |
| D22 | 🟠 | v1 (F22) | Scrubber tests only exercise hand-built events, never SDK-populated ones | `Configuration/SentryDataScrubbers.cs:39` | open |
| D23 | 🟠 | v1 (F23) | `DEPLOYMENT.md §14` referenced by config does not exist; no `/metrics` runbook | `appsettings.json:123` | open |
| D24 | 🟡 | v1 (F24) | Scope enricher registered after auth — pre-auth failures reach Sentry untagged | `Program.cs:352` | backlog |
| D25 | 🟡 | v1 (F25) | EF spans ship full SQL and exception messages to OTLP unscrubbed at 100% sampling | `Extensions/ObservabilityExtensions.cs:62` | backlog |
| D26 | 🟡 | v1 (F26) | `NaN` sample rates pass both validators and silently drop everything | `Validators/ObservabilitySettingsValidator.cs:46` | backlog |
| D27 | 🟡 | v1 (F27) | `PrometheusEndpoint="/"` passes validation and would gate the whole site | `Validators/ObservabilitySettingsValidator.cs:36` | backlog |
| D28 | 🟡 | v1 (F28) | `ValidateOnStart` boot-abort wiring untested; blank path would lock out the API | `Program.cs:72` | backlog |
| D29 | 🟡 | v1 (F29) | Enricher sets `scope.User.Id` instead of the required `user_id` tag | `Middleware/SentryScopeEnricherMiddleware.cs:33` | backlog |
| D30 | 🟡 | v1 (F30) | Story 003's sampler startup log not implemented; `ddd-02`/`ddd-03` claim it is | `Observability/Sampling/RouteAwareSampler.cs:40` | backlog |
| D31 | 🟡 | v1 (F31) | Neither Sentry nor OTel logs its enabled/disabled state at boot | `Program.cs:48` | backlog |
| D32 | 🟡 | v1 (F32) | Unsynchronized capture collections in the shared test fixture (plausible; safe today) | `Tests/Integration/SentryIntegrationFactory.cs:38` | backlog |
| D33 | ⚪ | v1 (F33) | Magic `"unknown"` label escapes `MetricNames`, docs and the cardinality budget | `Services/OrderService.cs:184` | backlog |
| D34 | ⚪ | v1 (F34) | `///` blocks on concrete classes citing bolt/ADR/story IDs (CLAUDE.md rule) | `Observability/FotoMetrics.cs:5` | backlog |
| D35 | ⚪ | v1 (F35) | Comment-sweep residue: dangling `/`, surviving bolt citations, run-on lines | `Program.cs:139` | backlog |
| D36 | ⚪ | v1 (F36) | `ddd-02` describes the `Random` approach ADR-017 forbids | `memory-bank/bolts/044-tracing-and-metrics/ddd-02-technical-design.md:195` | backlog |
| D37 | ⚪ | v1 (F37) | Metric vocabulary shipped ahead of emission (ANAF; constant `status` label) | `Observability/MetricNames.cs:74` | backlog |
| D38 | ⚪ | v1 (F38) | Observability config re-read by string key after binding; duplicated default | `Program.cs:72` | backlog |
| D39 | ⚪ | v1 (F39) | Sentry wiring inlined in `Program.cs` while bolt 044 got an extension method | `Program.cs:29` | backlog |

## Clusters worth fixing together

- **Sentry egress (D2, D3, D4, and D22 as its test):** one root cause — the scrubber is a
  partial allow-list applied at one of two SDK hooks. Fixing them individually invites a
  fourth leak.
- **`/metrics` access (D1, D10, D12, and D23 as its runbook):** the endpoint's access control
  is wrong at the topology level *and* brittle at the parsing level.
- **Sampling (D5, D6):** fixing D5 makes D6 worse, because correctly applying low per-route
  rates increases the number of dropped spans whose error traces D6 loses. Fix together.
- **Test vacuity (D8, D9, D20, D21, D28):** five places where a green suite proves nothing.
  These are the `definition-of-done` revert-test rule, unmet.

## Cross-target note

**D35** lands on residue from the repo-wide comment sweep (`09173c4`), which belongs to the
`system` target's loop rather than to bolts 044/045. It is recorded here because this branch
carries the lines; the fixer decides which target owns the fix.
