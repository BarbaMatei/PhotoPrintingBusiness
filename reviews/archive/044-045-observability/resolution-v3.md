---
type: resolution
target: 044-045-observability
version: 3
answers: review-v3.md
status: resolved
fixed_commit: dc203c7
closed: 2026-08-06
findings:
  D74: { status: fixed, commit: c363b7a, note: "Socket/pipe classified by prefix before `Parse`; port 0 excluded as dynamic-bind. 'Addresses but no TCP port' is its own refuse verdict; the TestServer carve-out keys on an empty address list. Linux proof is CI: 6 red runs, then green." }
  D75: { status: fixed, commit: 7c002a0, note: "The validator owns the deadline: `CancellationTokenSource(_deadline)` linked with the caller's token; the discriminator is `ct.IsCancellationRequested && !deadline.IsCancellationRequested`. `HttpClient.Timeout` is a 15 s backstop." }
  D76: { status: fixed, commit: feb5636, note: "Test-only. `InvokeAsync_UnmappedServerError_LogsAtErrorWithTheException` pins the level and the attached exception on the unmapped branch; reverting `LogError` to `LogWarning` there reddens it — measured; before, 255 green." }
  D77: { status: fixed, commit: f2a7ef9, note: "`o.TracesSampler` answers the configured rate on every call; per the SDK IL it outranks an inherited `sentry-trace` only when non-null. Caddy now strips `sentry-trace` and `baggage`; `traceparent` kept on purpose. See Decisions." }
  D78: { status: fixed, commit: 2a82f01, note: "Test-only. `A_second_capture_in_the_same_test_fails_loudly`; deleting the nested-capture throw reddens it — measured; before, 738 green." }
  D79: { status: fixed, commit: 163f912, note: "Test-only. The pin reads the installed sampler off the booted TracerProvider by reflection, failing loudly on a rename; re-wrapping the call site reddens it — measured; before, 1120 green. Behavioural route rejected as flaky (D51)." }
  D80: { status: fixed, commit: caeb866, note: "Owner chose 'match the prose': numerator `ok`+`duplicate`; `signature_invalid` leaves the denominator, in both copies (slos.md and the dashboard panel). Literal `=` matchers keep `ok` and `duplicate` build-checked. See Decisions." }
  D81: { status: fixed, commit: caeb866, note: "The status block no longer claims SLO 1 is measured; it names the dilution (~5,760 always-200 requests/day, ~99.7% floor) and says not to read it as availability. SLO 3's cross-check is now the webhook-route 5xx rate. D46 stays deferred." }
  D82: { status: fixed, commit: 55f6441, note: "Give-up query widened to `Paid || Printing` only; the re-enqueue query stays strictly `Paid` on purpose. The log line now carries `status=`. Narrowing back to `Paid` reddens the new test — measured. See Decisions." }
  D83: { status: fixed, commit: f81626f, note: "Doc-only. metrics.md step 10 now states what the test proves: an undeclared queried name fails the build; the exposition is test-seeded, so a never-incremented metric (`invoice_anaf_status_total`) stays green. Seeding obligation named." }
  D84: { status: fixed, commit: cdb5554, note: "`StripBraceGroups` replaces the first-`}` regex in `MetricNamesIn`, reusing the exposition's quote-aware `ClosingBrace`. Red proof: `A_route_template_label_value_is_not_read_as_a_metric_name`. `LabelUsagesIn` gap stays open (D88)." }
  D85: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D86: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D87: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D88: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D89: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D90: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D91: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D92: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D93: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D94: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D95: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D96: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D97: { status: backlog, commit: null, note: "🟡 — ledger backlog; flagged to the owner in summary-v3 as the one minor worth their eye" }
  D98: { status: backlog, commit: null, note: "🟡 — ledger backlog per the README router" }
  D99: { status: backlog, commit: null, note: "⚪ — ledger backlog per the README router" }
  D100: { status: backlog, commit: null, note: "⚪ — ledger backlog per the README router" }
  D101: { status: backlog, commit: null, note: "⚪ — ledger backlog per the README router" }
  D102: { status: backlog, commit: null, note: "⚪ — ledger backlog per the README router" }
---

# Resolution v3 — 044-045-observability

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — scrape-listener port parsing (🔴) | D74 | `Observability/ScrapeListenerGuard.cs`, `Tests/Unit/Observability/ScrapeListenerCheckTests.cs` | needed (narrows a catch inside a gate that aborts boot) |
| B — cancellation vs dependency failure | D75 | `Services/GoogleTokenValidator.cs`, `Tests/Unit/Services/GoogleTokenValidatorTests.cs` | needed (changes a catch/mapping layer and what reaches Sentry and SLO 1) |
| C — unmapped-500 log level | D76 | `Tests/Unit/Middleware/ExceptionHandlerMiddlewareTests.cs` | not needed (test-only) |
| D — Sentry inbound trace decision | D77 | `Program.cs`, `Configuration/SentrySettings.cs`, `Caddyfile` | needed (changes sampling semantics, same class as D41; owner gate) |
| E — nested metric capture | D78 | `Tests/Unit/Observability/MetricCaptureIsolationTests.cs` | not needed (test-only) |
| F — sampler call-site pin | D79 | `Tests/Unit/Observability/TracingExporterSelectionTests.cs` | not needed (test-only) |
| G — SLO and metric documents | D80, D81, D83 | `memory-bank/operations/slos.md`, `memory-bank/operations/metrics.md`, `ops/dashboards/fototipar-overview.json` | not needed (doc/query; owner gate on D80) |
| H — dashboard metric-name parser | D84 | `Tests/Integration/DashboardMetricNamesTests.cs` | not needed (test-only) |
| I — AWB retry sweep coverage | D82 | `BackgroundJobs/AwbRetryJob.cs`, `Services/Sameday/AwbDispatcher.cs` | needed (changes which orders a periodic sweep picks up; owner gate) |
| J — backlog triage | D85–D102 | — | not needed (🟡/⚪ routed to the ledger backlog) |

## Decisions

### Fixer and finder shared one session (D74–D84)

- The v3 verification pass and this fix round ran in one session, so the finding author fixed them — the caveat resolution v2 recorded, for the same reason: the dispute pressure a fixer applies to inherited findings was absent. The v4 verification pass ran from a fresh session.
- Two offsets. Most serious findings were settled by measurement recorded before the round started: a real `HttpClient` probe on net8.0 (D75), mutations that left the suite green (D76: 255, D78: 738, D79: 1120), and the CI log itself (D74). And D74's red proof is CI on a platform this machine is not, which no fixer optimism can fake green.
- Triage confirmed all eleven at `cd99cdb` (source identical to `7e28317`). CI was red at the branch tip and the branch could not merge until green, so cluster A went first.

### Owner gate answers, all as recommended (D75, D77, D80, D82)

- Asked once after triage on 2026-08-05. D77: fix now, same posture as D41 — ignore an inbound sampling decision and strip the header at the edge. D80: match the prose — numerator `ok` + `duplicate`, `signature_invalid` out of the denominator. D82: widen the give-up alarm query only, not the re-enqueue query. D75: make the carve-out actually work and report the outage, accepting that some abandoned logins correctly produce 502s.

### Every approach-check came back "needs revision" (D74, D75, D77, D82)

- Four checks ran, none cleared its draft, and in three cases the draft would have shipped a new defect.
- D74: the draft skipped socket/pipe addresses but kept the `ports.Count == 0` carve-out, which would have turned a socket-only host's current abort into a silent boot with a scrape port nothing can serve. The shipped fix splits the carve-out — no addresses reported at all stays quiet (TestServer); addresses present but no TCP port is its own refuse verdict — and classifies before `Parse`, removing the platform fork. The pipe, Windows-side unix, dynamic-port and no-TCP-port cases all reddened locally; the Linux leg's proof is CI.
- D75: the draft walked the exception chain for a `TimeoutException` — still sniffing an undocumented internal shape, and `HttpClient` decides whether to wrap only at failure time, so a genuine timeout the caller races by microseconds still loses. Owning the deadline removes the guesswork; restoring the old `GetBaseException` filter reddens exactly the both-fired ordering test.
- D82: the draft exposed `OrderStatusMachine.PaidStatuses` and widened to the whole paid-or-later set. A labelless `Shipped`/`Delivered` is legitimate manual fulfilment (`AdminOrderService` sets `AwbNumber` only when the admin supplies one), so that would have converted a silent gap into a recurring page nobody can act on. `Paid || Printing` is the correct set; `PaidStatuses` stayed private.
- D77: the check settled from the SDK's IL that `TracesSampler` wins over an inherited `sentry-trace` only when it returns a value — a sampler returning null for parentless transactions would have left the hole open. It also caught that stripping `traceparent` at the edge would regress §14.7's documented way to force a trace for debugging; D41 already made its sampled flag harmless. §14.7 now records both limits of the edge strip.

### An alarm, not a re-enqueue (D82)

- The check confirmed the leg the v3 pass recorded as unverified: `AwbCreator` returns `Skipped("status is Printing, not Paid")` and `AwbDispatcher` logs that at Information and drops the job. Nothing retries, nothing dead-letters.
- So the fix is visibility. Recovery is still impossible for an order advanced to `Printing` — an AWB-subsystem behaviour change, out of scope. The check flagged that the stated reason for leaving the re-enqueue query alone is circular in exactly this way.

### Deliberate deviations from the checks and the review (D74, D75, D79)

- D75: the check proposed `FakeTimeProvider`. Handler-driven ordering is deterministic without a clock — the replacement handler cancels the caller only after the deadline has already tripped — and adds no `TimeProvider` dependency to a production class. The 50 ms deadline in those two tests has no competing timer, so there is no race to lose.
- D79: the review said a behavioural assertion through the real composition was available. It is, but the production pipeline's ActivitySources are shared with parallel test hosts (D51), so any Recorded-flag assertion is flaky by construction; the pin reads the installed sampler's type instead. The v2 record's reason for leaving this unpinned — that TracerProvider does not expose its sampler — is refuted: the member is reachable by reflection.
- D74: `IsSocketOrPipe` matches with `Ordinal` on purpose, mirroring `BindingAddress`'s own ordinal check; a case-insensitive test would classify addresses the parser does not — a new divergence, not a fix. `HTTP://UNIX:/…` falls to the retained `FormatException` catch, which now only ever means "malformed".

### Micro-review repairs to the round's own diff (D74, D75, D80, D81, D82)

- Two fresh-eyes agents over the round's diff, split by risk: one over the five test-and-parser fixes, one over the behaviour and document fixes. 18 findings between them, repaired in `d1ffee7` and `dc203c7`.
- The D80 fix would have blinded SLO 3 in the healthy case: `sum(A) + sum(B)` is empty when `B` matches no series, and `payment_webhook_total{result="duplicate"}` does not exist until the first duplicate — so the repaired panel read "No Data" exactly while nothing was wrong. Both terms now carry `or vector(0)`, and slos.md explains why the guard is load-bearing.
- The D81 fix changed the document and not the wall: the Availability panel operators actually read still presented the diluted ratio bare. The panel now carries a `description` naming the dilution and D46.
- The `status=` field never reached the log catalogue, and §12.8 still said the give-up alarm means "24 h elapsed" without the `Printing` case that is the point of the D82 fix. Both fixed.
- The D75 fix falsified §13.1: "a disconnecting caller is never a Sentry issue" is now conditional — past our own deadline it is a real outage and is captured. The section says so, and why the trade is one-sided on purpose.
- The story doc still carried the retired SLO 3 criterion, so the next verification pass would have re-derived the definition the D80 fix replaced. Amended in place.
- The `Critical` line §14.10 tells operators to grep for had no test, while a third refusal reason now routes through it. Closed with a real-Kestrel boot test (`d1ffee7`) pinning the abort and the log line; downgrading it to `Warning` reddens it — measured.
- Checks that came back clean, recorded so they are not re-litigated: the exception filter runs before the enclosing `using` disposes, and `IsCancellationRequested` never throws `ObjectDisposedException`; MS.DI honours the constructor's default parameter, so `AddScoped<IGoogleTokenValidator, GoogleTokenValidator>` still resolves; `(Paid || Printing)` over a `HasConversion<string>()` column becomes two parameterised string comparisons on both SQLite and Npgsql; `MarkOnce` stays one-shot across a status change; `header_up -X` is valid Caddy v2 and strips the upstream request header.

### Found outside the finding set, not fixed (D77, D80)

- SLO 4 and SLO 5 carry the exact defect the D80 fix removed: `awb_creation_total{result="ok"} / total` puts benign `skipped` in the denominator — the retry sweep manufactures "another worker holds a fresh claim" on every interval — and SLO 5 counts `pending` the same way. Not fixed here: each is the same definitional owner call SLO 3 got, not a mechanical sweep. Later minted as D105.
- Nothing tests the `Caddyfile`: the two `header_up` strips can be deleted silently; the document is their only guard.
- `tracestate` is still honoured and forwarded — the remaining attacker-seedable member of the trace-header family. Low impact (`DeterministicTraceIdSampler` ignores it), unswept.

### Left unfixed, disclosed to the re-reviewer (D46, D75, D77, D80)

- D77 has no real-hub precedence test: the shipped pin catches "someone returns null", not "the SDK changed whether a sampler outranks an inherited decision". That needs `SentrySdk.Init` inside a serialised collection, which the check spelled out and this round did not build. `Sentry:Enabled=false` everywhere keeps it latent.
- The edge strip covers only Caddy-routed traffic: requests reaching `api:8080` inside the compose network — health checks, anything on staging bypassing Caddy — still carry `sentry-trace`; the sampler is what protects those. Recorded in §14.7.
- SLO 3's denominator now excludes `signature_invalid` via `!=`, whose value is not build-checked; renaming that constant silently changes the denominator. slos.md states that negative and regex matchers are outside the test's net.
- D46 stays deferred: the D81 fix only stops slos.md misleading a reader while it is parked; the availability number is still diluted and cannot read below about 99.7%.
- A flaky test filed to the review inbox, not caused by this round: `EmailRetryJobTests.Processing_SuccessfulSend_MarksEmailAsSent` failed once under parallel load and passed 4/4 in isolation, surfacing as unexplained collateral in a mutation run.
