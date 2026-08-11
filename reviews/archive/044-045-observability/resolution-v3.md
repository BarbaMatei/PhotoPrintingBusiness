---
type: resolution
target: 044-045-observability
version: 3
answers: review-v3.md
status: resolved
fixed_commit: dc203c7
closed: 2026-08-06
---

# Resolution v3 — 044-045-observability

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-409 | fixed | `c363b7a` | Socket/pipe classified by prefix before `Parse`; port 0 excluded as dynamic-bind. 'Addresses but no TCP port' is its own refuse verdict; the TestServer carve-out keys on an empty address list. Linux proof is CI: 6 red runs, then green. |
| PPW-410 | fixed | `7c002a0` | The validator owns the deadline: `CancellationTokenSource(_deadline)` linked with the caller's token; the discriminator is `ct.IsCancellationRequested && !deadline.IsCancellationRequested`. `HttpClient.Timeout` is a 15 s backstop. |
| PPW-411 | fixed | `feb5636` | Test-only. `InvokeAsync_UnmappedServerError_LogsAtErrorWithTheException` pins the level and the attached exception on the unmapped branch; reverting `LogError` to `LogWarning` there reddens it — measured; before, 255 green. |
| PPW-412 | fixed | `f2a7ef9` | `o.TracesSampler` answers the configured rate on every call; per the SDK IL it outranks an inherited `sentry-trace` only when non-null. Caddy now strips `sentry-trace` and `baggage`; `traceparent` kept on purpose. See Decisions. |
| PPW-413 | fixed | `2a82f01` | Test-only. `A_second_capture_in_the_same_test_fails_loudly`; deleting the nested-capture throw reddens it — measured; before, 738 green. |
| PPW-414 | fixed | `163f912` | Test-only. The pin reads the installed sampler off the booted TracerProvider by reflection, failing loudly on a rename; re-wrapping the call site reddens it — measured; before, 1120 green. Behavioural route rejected as flaky. |
| PPW-415 | fixed | `caeb866` | Owner chose 'match the prose': numerator `ok`+`duplicate`; `signature_invalid` leaves the denominator, in both copies (slos.md and the dashboard panel). Literal `=` matchers keep `ok` and `duplicate` build-checked. See Decisions. |
| PPW-416 | fixed | `caeb866` | The status block no longer claims SLO 1 is measured; it names the dilution (~5,760 always-200 requests/day, ~99.7% floor) and says not to read it as availability. SLO 3 cross-checks the webhook-route 5xx rate. PPW-381 stays deferred. |
| PPW-417 | fixed | `55f6441` | Give-up query widened to `Paid ·· Printing` only; the re-enqueue query stays strictly `Paid` on purpose. The log line now carries `status=`. Narrowing back to `Paid` reddens the new test — measured. See Decisions. |
| PPW-418 | fixed | `f81626f` | Doc-only. metrics.md step 10 now states what the test proves: an undeclared queried name fails the build; the exposition is test-seeded, so a never-incremented metric (`invoice_anaf_status_total`) stays green. Seeding obligation named. |
| PPW-419 | fixed | `cdb5554` | `StripBraceGroups` replaces the first-`}` regex in `MetricNamesIn`, reusing the exposition's quote-aware `ClosingBrace`. Red proof: `A_route_template_label_value_is_not_read_as_a_metric_name`. `LabelUsagesIn` gap stays open. |
| PPW-420 | backlog | — | 🟡 — ledger backlog per the README router |
| PPW-421 | backlog | — | 🟡 — ledger backlog per the README router |
| PPW-422 | backlog | — | 🟡 — ledger backlog per the README router |
| PPW-423 | backlog | — | 🟡 — ledger backlog per the README router |
| PPW-424 | backlog | — | 🟡 — ledger backlog per the README router |
| PPW-425 | backlog | — | 🟡 — ledger backlog per the README router |
| PPW-426 | backlog | — | 🟡 — ledger backlog per the README router |
| PPW-427 | backlog | — | 🟡 — ledger backlog per the README router |
| PPW-428 | backlog | — | 🟡 — ledger backlog per the README router |
| PPW-429 | backlog | — | 🟡 — ledger backlog per the README router |
| PPW-430 | backlog | — | 🟡 — ledger backlog per the README router |
| PPW-431 | backlog | — | 🟡 — ledger backlog per the README router |
| PPW-432 | backlog | — | 🟡 — ledger backlog; flagged to the owner in summary-v3 as the one minor worth their eye |
| PPW-433 | backlog | — | 🟡 — ledger backlog per the README router |
| PPW-434 | backlog | — | ⚪ — ledger backlog per the README router |
| PPW-435 | backlog | — | ⚪ — ledger backlog per the README router |
| PPW-436 | backlog | — | ⚪ — ledger backlog per the README router |
| PPW-437 | backlog | — | ⚪ — ledger backlog per the README router |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — scrape-listener port parsing (🔴) | PPW-409 | `Observability/ScrapeListenerGuard.cs`, `Tests/Unit/Observability/ScrapeListenerCheckTests.cs` | needed (narrows a catch inside a gate that aborts boot) |
| B — cancellation vs dependency failure | PPW-410 | `Services/GoogleTokenValidator.cs`, `Tests/Unit/Services/GoogleTokenValidatorTests.cs` | needed (changes a catch/mapping layer and what reaches Sentry and SLO 1) |
| C — unmapped-500 log level | PPW-411 | `Tests/Unit/Middleware/ExceptionHandlerMiddlewareTests.cs` | not needed (test-only) |
| D — Sentry inbound trace decision | PPW-412 | `Program.cs`, `Configuration/SentrySettings.cs`, `Caddyfile` | needed (changes sampling semantics, same class as PPW-376; owner gate) |
| E — nested metric capture | PPW-413 | `Tests/Unit/Observability/MetricCaptureIsolationTests.cs` | not needed (test-only) |
| F — sampler call-site pin | PPW-414 | `Tests/Unit/Observability/TracingExporterSelectionTests.cs` | not needed (test-only) |
| G — SLO and metric documents | PPW-415, PPW-416, PPW-418 | `memory-bank/operations/slos.md`, `memory-bank/operations/metrics.md`, `ops/dashboards/fototipar-overview.json` | not needed (doc/query; owner gate on PPW-415) |
| H — dashboard metric-name parser | PPW-419 | `Tests/Integration/DashboardMetricNamesTests.cs` | not needed (test-only) |
| I — AWB retry sweep coverage | PPW-417 | `BackgroundJobs/AwbRetryJob.cs`, `Services/Sameday/AwbDispatcher.cs` | needed (changes which orders a periodic sweep picks up; owner gate) |
| J — backlog triage | PPW-420–PPW-437 | — | not needed (🟡/⚪ routed to the ledger backlog) |

## Decisions

### Fixer and finder shared one session (PPW-409–PPW-419)

- The v3 verification pass and this fix round ran in one session, so the finding author fixed them — the caveat resolution v2 recorded, for the same reason: the dispute pressure a fixer applies to inherited findings was absent. The v4 verification pass ran from a fresh session.
- Two offsets. Most serious findings were settled by measurement recorded before the round started: a real `HttpClient` probe on net8.0, mutations that left the suite green (PPW-411: 255, PPW-413: 738, PPW-414: 1120), and the CI log itself. And PPW-409's red proof is CI on a platform this machine is not, which no fixer optimism can fake green.
- Triage confirmed all eleven at `cd99cdb` (source identical to `7e28317`). CI was red at the branch tip and the branch could not merge until green, so cluster A went first.

### Owner gate answers, all as recommended (PPW-410, PPW-412, PPW-415, PPW-417)

- Asked once after triage on 2026-08-05. PPW-412: fix now, same posture as PPW-376 — ignore an inbound sampling decision and strip the header at the edge. PPW-415: match the prose — numerator `ok` + `duplicate`, `signature_invalid` out of the denominator. PPW-417: widen the give-up alarm query only, not the re-enqueue query. PPW-410: make the carve-out actually work and report the outage, accepting that some abandoned logins correctly produce 502s.

### Every approach-check came back "needs revision" (PPW-409, PPW-410, PPW-412, PPW-417)

- Four checks ran, none cleared its draft, and in three cases the draft would have shipped a new defect.
- PPW-409: the draft skipped socket/pipe addresses but kept the `ports.Count == 0` carve-out, which would have turned a socket-only host's current abort into a silent boot with a scrape port nothing can serve. The shipped fix splits the carve-out — no addresses reported at all stays quiet (TestServer); addresses present but no TCP port is its own refuse verdict — and classifies before `Parse`, removing the platform fork. The pipe, Windows-side unix, dynamic-port and no-TCP-port cases all reddened locally; the Linux leg's proof is CI.
- PPW-410: the draft walked the exception chain for a `TimeoutException` — still sniffing an undocumented internal shape, and `HttpClient` decides whether to wrap only at failure time, so a genuine timeout the caller races by microseconds still loses. Owning the deadline removes the guesswork; restoring the old `GetBaseException` filter reddens exactly the both-fired ordering test.
- PPW-417: the draft exposed `OrderStatusMachine.PaidStatuses` and widened to the whole paid-or-later set. A labelless `Shipped`/`Delivered` is legitimate manual fulfilment (`AdminOrderService` sets `AwbNumber` only when the admin supplies one), so that would have converted a silent gap into a recurring page nobody can act on. `Paid || Printing` is the correct set; `PaidStatuses` stayed private.
- PPW-412: the check settled from the SDK's IL that `TracesSampler` wins over an inherited `sentry-trace` only when it returns a value — a sampler returning null for parentless transactions would have left the hole open. It also caught that stripping `traceparent` at the edge would regress §14.7's documented way to force a trace for debugging; PPW-376 already made its sampled flag harmless. §14.7 now records both limits of the edge strip.

### An alarm, not a re-enqueue

- The check confirmed the leg the v3 pass recorded as unverified: `AwbCreator` returns `Skipped("status is Printing, not Paid")` and `AwbDispatcher` logs that at Information and drops the job. Nothing retries, nothing dead-letters.
- So the fix is visibility. Recovery is still impossible for an order advanced to `Printing` — an AWB-subsystem behaviour change, out of scope. The check flagged that the stated reason for leaving the re-enqueue query alone is circular in exactly this way.

### Deliberate deviations from the checks and the review (PPW-409, PPW-410, PPW-414)

- PPW-410: the check proposed `FakeTimeProvider`. Handler-driven ordering is deterministic without a clock — the replacement handler cancels the caller only after the deadline has already tripped — and adds no `TimeProvider` dependency to a production class. The 50 ms deadline in those two tests has no competing timer, so there is no race to lose.
- PPW-414: the review said a behavioural assertion through the real composition was available. It is, but the production pipeline's ActivitySources are shared with parallel test hosts, so any Recorded-flag assertion is flaky by construction; the pin reads the installed sampler's type instead. The v2 record's reason for leaving this unpinned — that TracerProvider does not expose its sampler — is refuted: the member is reachable by reflection.
- PPW-409: `IsSocketOrPipe` matches with `Ordinal` on purpose, mirroring `BindingAddress`'s own ordinal check; a case-insensitive test would classify addresses the parser does not — a new divergence, not a fix. `HTTP://UNIX:/…` falls to the retained `FormatException` catch, which now only ever means "malformed".

### Micro-review repairs to the round's own diff (PPW-409, PPW-410, PPW-415, PPW-416, PPW-417)

- Two fresh-eyes agents over the round's diff, split by risk: one over the five test-and-parser fixes, one over the behaviour and document fixes. 18 findings between them, repaired in `d1ffee7` and `dc203c7`.
- The PPW-415 fix would have blinded SLO 3 in the healthy case: `sum(A) + sum(B)` is empty when `B` matches no series, and `payment_webhook_total{result="duplicate"}` does not exist until the first duplicate — so the repaired panel read "No Data" exactly while nothing was wrong. Both terms now carry `or vector(0)`, and slos.md explains why the guard is load-bearing.
- The PPW-416 fix changed the document and not the wall: the Availability panel operators actually read still presented the diluted ratio bare. The panel now carries a `description` naming the dilution and PPW-381.
- The `status=` field never reached the log catalogue, and §12.8 still said the give-up alarm means "24 h elapsed" without the `Printing` case that is the point of the PPW-417 fix. Both fixed.
- The PPW-410 fix falsified §13.1: "a disconnecting caller is never a Sentry issue" is now conditional — past our own deadline it is a real outage and is captured. The section says so, and why the trade is one-sided on purpose.
- The story doc still carried the retired SLO 3 criterion, so the next verification pass would have re-derived the definition the PPW-415 fix replaced. Amended in place.
- The `Critical` line §14.10 tells operators to grep for had no test, while a third refusal reason now routes through it. Closed with a real-Kestrel boot test (`d1ffee7`) pinning the abort and the log line; downgrading it to `Warning` reddens it — measured.
- Checks that came back clean, recorded so they are not re-litigated: the exception filter runs before the enclosing `using` disposes, and `IsCancellationRequested` never throws `ObjectDisposedException`; MS.DI honours the constructor's default parameter, so `AddScoped<IGoogleTokenValidator, GoogleTokenValidator>` still resolves; `(Paid || Printing)` over a `HasConversion<string>()` column becomes two parameterised string comparisons on both SQLite and Npgsql; `MarkOnce` stays one-shot across a status change; `header_up -X` is valid Caddy v2 and strips the upstream request header.

### Found outside the finding set, not fixed (PPW-412, PPW-415)

- SLO 4 and SLO 5 carry the exact defect the PPW-415 fix removed: `awb_creation_total{result="ok"} / total` puts benign `skipped` in the denominator — the retry sweep manufactures "another worker holds a fresh claim" on every interval — and SLO 5 counts `pending` the same way. Not fixed here: each is the same definitional owner call SLO 3 got, not a mechanical sweep. Later minted as PPW-440.
- Nothing tests the `Caddyfile`: the two `header_up` strips can be deleted silently; the document is their only guard.
- `tracestate` is still honoured and forwarded — the remaining attacker-seedable member of the trace-header family. Low impact (`DeterministicTraceIdSampler` ignores it), unswept.

### Left unfixed, disclosed to the re-reviewer (PPW-381, PPW-410, PPW-412, PPW-415)

- PPW-412 has no real-hub precedence test: the shipped pin catches "someone returns null", not "the SDK changed whether a sampler outranks an inherited decision". That needs `SentrySdk.Init` inside a serialised collection, which the check spelled out and this round did not build. `Sentry:Enabled=false` everywhere keeps it latent.
- The edge strip covers only Caddy-routed traffic: requests reaching `api:8080` inside the compose network — health checks, anything on staging bypassing Caddy — still carry `sentry-trace`; the sampler is what protects those. Recorded in §14.7.
- SLO 3's denominator now excludes `signature_invalid` via `!=`, whose value is not build-checked; renaming that constant silently changes the denominator. slos.md states that negative and regex matchers are outside the test's net.
- PPW-381 stays deferred: the PPW-416 fix only stops slos.md misleading a reader while it is parked; the availability number is still diluted and cannot read below about 99.7%.
- A flaky test filed to the review inbox, not caused by this round: `EmailRetryJobTests.Processing_SuccessfulSend_MarksEmailAsSent` failed once under parallel load and passed 4/4 in isolation, surfacing as unexplained collateral in a mutation run.
