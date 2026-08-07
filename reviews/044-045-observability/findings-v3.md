---
type: findings
target: 044-045-observability
version: 3
for: review-v3.md
commit: 7e28317
date: 2026-08-05
---

# Findings v3 — 044-045-observability

Per-finding detail for [review-v3.md](review-v3.md). Every claim carries the evidence that
settled it; where a claim came from a lens and could not be measured, it says so.

## Part 1 — verification of the ten fixes

Recorded in [review-v3.md](review-v3.md#how-each-fix-was-proven) as a mutation table. Two entries
need their evidence written out, because they are the ones a future reader will re-litigate.

### D45 (v2's F6) — declined to verify

The decision function and the hook are both pinned (mutations 8 and 9 redden 8 and 1 tests). The
fix is nevertheless **not verified**, for a reason that outranks its own tests: its regression
test fails on the CI platform and the guard it protects is wrong there. See F1. This follows the
precedent v2 set with D17 — a fix whose proof does not hold is `fixed`, never `verified`.

### D43 (v2's F4) — verified for its own defect only

Mutation 5 proves the client-abort leg: remove the rethrows and
`ValidateAsync_CallerCancelled_PropagatesCancellationInsteadOfBadGateway` reddens. That is the
defect D43 named, and it is fixed. The *second* leg — the carve-out that was supposed to keep a
genuine Google outage visible — is dead code, and its test passes only on a fabricated exception
shape. That is F2, a new finding rather than a reopen, because the defect it describes did not
exist before this fix.

## Part 2 — new findings

### F1 · 🔴 · D74 · The scrape guard mis-parses socket/pipe listeners off-Windows, and its own test fails on CI

`src/PhotoPrint.API/Observability/ScrapeListenerGuard.cs:23-28` ·
`src/PhotoPrint.Tests/Unit/Observability/ScrapeListenerCheckTests.cs:83-87`

`Verdict` builds its port set by calling `BindingAddress.Parse` and swallowing `FormatException`
for addresses that "carry no port". That behaviour is platform-dependent. From the released
.NET 8 source:

```csharp
private const string UnixPipeHostPrefix = "unix:/";          // length 6

private static string GetUnixPipePath(string host)
{
    var unixPipeHostPrefixLength = UnixPipeHostPrefix.Length;
    if (!OperatingSystem.IsWindows())
        unixPipeHostPrefixLength--;                            // 5 off-Windows
    return host.Substring(unixPipeHostPrefixLength);
}
...
if (isUnixPipe && !Path.IsPathRooted(GetUnixPipePath(host)))
    throw new FormatException($"Invalid url, unix socket path must be absolute: '{address}'");
```

So `http://unix:/tmp/kestrel.sock` yields the **relative** `tmp/kestrel.sock` on Windows → not
rooted → throws; and the **rooted** `/tmp/kestrel.sock` on Linux → no throw, `Port == 0`.

Measured locally on `net8.0` / `8.0.29`, Windows:

```
http://unix:/tmp/kestrel.sock  -> THREW FormatException: unix socket path must be absolute
http://unix:/C:/tmp/app.sock   -> Port=0  IsUnixPipe=True      # a unix address that parses
http://pipe:/mypipe            -> Port=80 IsUnixPipe=False     # a phantom port, even on Windows
```

**Consequence 1 — CI is red, and has been since the fix round.** On `ubuntu-latest` the test's
input produces `ports = {0, 9090}` → count 2 → rule 1 passes, rule 2 cannot fire → `Verdict`
returns null → `.Should().NotBeNull()` fails. From the CI log at the branch tip `c92ad77`:

```
Failed PhotoPrint.Tests.Unit.Observability.ScrapeListenerCheckTests.An_address_with_no_port_is_not_counted_as_a_listener
  Expected ScrapeListenerCheck.Verdict(["http://unix:/tmp/kestrel.sock", "http://+:9090"], 9090) not to be <null>.
```

Attribution is clean: the `ci` workflow was **green** at `8daa977` (2026-08-05 09:35Z, the v2
review commit) and has failed on every run since `e791c40` (10:54Z, the first push after the fix
code) — six consecutive red runs. `.github/workflows/ci.yml:14` is `runs-on: ubuntu-latest` and
`:68` runs `dotnet test PhotoPrint.sln`, so the whole API job fails.

**Consequence 2 — on the deploy platform the guard's second rule is defeated by exactly the
topology it exists to catch.** A Linux host bound to one TCP port plus a unix socket or named
pipe reports two "distinct ports" (`{0, 8080}`), so the "`ScrapePort` is the only listener" rule
does not fire and D45's original exposure ships silently. `Describe(ports)` would also print
`0` as a bound port in a rule-1 message.

Today's `docker-compose.prod.yml` binds TCP only, so consequence 2 is latent; consequence 1 is
live and blocks the branch.

### F2 · 🟠 · D75 · F4's timeout carve-out is dead code, and its test passes on a shape .NET never produces

`src/PhotoPrint.API/Services/GoogleTokenValidator.cs:40-45`, `:64-68` ·
`src/PhotoPrint.Tests/Unit/Services/GoogleTokenValidatorTests.cs` (`TimedOutThenAbortedHttpHandler`)

The shipped filter is
`when (ct.IsCancellationRequested && ex.GetBaseException() is not TimeoutException)`, with the
comment "A timeout keeps a TimeoutException at the base of the chain even once the caller cancels
too." Measured against a real `HttpClient` with a hanging handler on `net8.0` / `8.0.29`:

| scenario | chain | `GetBaseException()` | shipped filter | naive filter |
|---|---|---|---|---|
| pure timeout, caller never cancels | `TCE → TimeoutException → TCE` | `TaskCanceledException` | no rethrow | no rethrow |
| timeout, caller cancels 150 ms later | `TCE → TimeoutException → TCE` | `TaskCanceledException` | **rethrow** | rethrow |
| caller cancels first | `TCE → TCE` | `TaskCanceledException` | rethrow | rethrow |
| both at once | `TCE → TCE` | `TaskCanceledException` | rethrow | rethrow |

`TimeoutException` is the **middle** frame, never the base — `HttpClient` builds it as
`new TimeoutException(oce.Message, oce)`, so the innermost exception is always the cancellation.
The second conjunct therefore never changes an outcome: the shipped filter and the naive filter
the approach-check rejected are behaviourally identical in all four scenarios.

The test that appears to prove otherwise hand-builds the shape:
`throw new TaskCanceledException("timeout", new TimeoutException())` — a `TimeoutException` with
no inner, so `GetBaseException()` returns it. Mutation 6 confirms the test does depend on the
conjunct (reducing the filter to `ct.IsCancellationRequested` reddens it), which is precisely
what makes it misleading: it reports a load-bearing carve-out that is inert in production.

**Failure scenario.** Google's tokeninfo hangs. A user gives up and closes the tab — the common
case under an outage, and the only case where the 5 s timeout has competition. The filter
rethrows, `ExceptionHandlerMiddleware.cs:53` catches the cancellation, logs
`request.client_aborted` at Information, captures nothing, and leaves `Response.StatusCode` at
200. The outage reaches neither Sentry nor SLO 1's 5xx numerator, and the aborted request counts
as a **success**. Only users who wait the full 5 s still produce a 502.

### F3 · 🟠 · D76 · The unmapped-500 branch's log level is unpinned — D49 one branch over

`src/PhotoPrint.API/Middleware/ExceptionHandlerMiddleware.cs:142`

D49 was "the `LogWarning → LogError` half of the D15 fix has no test". F10 fixed that for the
**mapped** 5xx branch. The `else` branch that handles every *unmapped* exception — the bulk of all
500s — is still unpinned. Measured: changing `_logger.LogError` to `_logger.LogWarning` at `:142`
leaves the suite **green (255 passed / 0 failed)**.
`InvokeAsync_UnknownException_Returns500WithGenericMessageInProduction` asserts status and body
only. DEPLOYMENT §13.8's "cross-check `Error`-level logs against Sentry" rests on this branch.

### F4 · 🟠 · D77 · Sentry honours an inbound `sentry-trace` ahead of `TracesSampleRate`

`src/PhotoPrint.API/Program.cs:48-66` · `Caddyfile:16-19`

The hole F2/D41 closed for OpenTelemetry is open one layer over. From the installed Sentry 4.13.0
XML docs, on `SentryOptions.TracesSampleRate`: *"Random sampling rate is only applied to
transactions that don't already have a sampling decision set by other means, such as … by
inheriting it from an incoming trace header"*; and `AutoRegisterTracing` is *"true by default"*,
registering the tracing middleware after `UseRouting()` (`Program.cs:349`). Tracing is live
(`TracesSampleRate = 0.1`), **no `TracesSampler` is configured**, and the `Caddyfile` strips no
headers at all — no `header_up -sentry-trace`, `-traceparent` or `-baggage`.

`curl -H 'sentry-trace: <32hex>-<16hex>-1'` on every request makes every request a sampled Sentry
transaction instead of 10%, burning the quota §13.8's alert rules depend on; `-0` blinds
performance monitoring. Error events are unaffected (`SampleRate` is a separate lever). Latent
while `Sentry:Enabled=false`; it bites the moment the flag flips. DEPLOYMENT §14.7's new
"A caller's `traceparent` does not change any of this" reads as class closure and does not mention
`sentry-trace`. **Not measured** — settled from the SDK's own documentation and this repo's
wiring; a booted-host request carrying the header would settle it outright.

### F5 · 🟠 · D78 · The nested-`MetricCapture` throw has no test

`src/PhotoPrint.Tests/Helpers/MetricCapture.cs:30-35`

The fix round's micro-review found that a nested `MetricCapture` silently blinded the outer one,
making its "nothing was recorded" assertions pass vacuously, and repaired it with a throw. Nothing
tests the throw. Measured: deleting the whole `if (Active.Value is not null) throw` block leaves
the suite **green (738 passed / 0 failed)** — no test constructs a nested capture, and
`MetricCaptureIsolationTests` has exactly three tests, none of them this one. The guard the repair
added is the same shape as the defect D47 named: a mechanism nothing enforces.

### F6 · 🟠 · D79 · Nothing pins F2's production call site, and the recorded reason is refuted

`src/PhotoPrint.API/Extensions/ObservabilityExtensions.cs:71`

Measured (mutation 4): re-wrapping the production call as
`t.SetSampler(new ParentBasedSampler(BuildSampler(settings.Sampling)))` restores D41's defect in
full and leaves **1120 tests green**. The fixer disclosed this. What is refuted is the stated
reason — "`TracerProvider` does not expose its sampler". A behavioural pin needs no access to the
sampler object: `TracingExporterSelectionTests` already resolves a `TracerProvider` from a real
`AddObservability(...)` container, so at `Sampling:Default = 0.0` a Server-kind activity under a
**remote sampled** parent asserted `Recorded == false` reddens under the re-wrap. Note the
asymmetry inside one round: F6/D45 got exactly this kind of wiring test after its micro-review;
F2's call site got none.

### F7 · 🟠 · D80 · SLO 3's query contradicts SLO 3's own definition

`memory-bank/operations/slos.md:72-83`

The prose scopes SLO 3 to requests that end with the order marked Paid *"or correctly rejected
with a 200 for known duplicate/idempotency cases"*. The query is
`payment_webhook_total{result="ok"} / payment_webhook_total`. `duplicate` is a separate label
value, so it lands in the denominator only — and `metrics.md:55`, rewritten in this same round,
now defines `duplicate` as "Idempotent receipt — the order has already been paid". Two documents
and the query disagree about the same event.

The `ok` branch records its counter *before* its side effects, so a SignalR/email/promoter throw
returns 500 with the order already Paid, the provider redelivers, and the redelivery scores
`duplicate`. At a ≥99.9% target over 7 days, one duplicate per 1,000 webhooks breaches. Worse,
`signature_invalid` sits in the same denominator on an `[AllowAnonymous]` endpoint, so anyone who
can POST garbage to `/api/webhooks/stripe` can drive SLO 3 to 0.

### F8 · 🟠 · D81 · `slos.md` still asserts SLO 1 is measured — and now offers it as SLO 3's cross-check

`memory-bank/operations/slos.md:5-7`, `:86-94`

F11's job was to stop the file overclaiming. The new header names exactly one caveat — *"SLOs 1–4
are measured, with one caveat that matters: **SLO 3 cannot see a total outage**"* — and SLO 1 gets
no note at all, though the owner has knowingly parked (D46) the fact that its denominator is
dominated by ~5,760 always-200 `/metrics` and `/health` requests a day, so it cannot read below
about 99.7%. The new block at `:86-94` then points a reader the wrong way: *"the throw itself
surfaces as a 5xx in SLO 1"*, presenting SLO 1 as the reliable cross-check for SLO 3's blind spot
while SLO 1 is the diluted one. `DEPLOYMENT.md:949` reasons from the target as if the denominator
were customer traffic. This is F11's own defect shape in the sentence F11 edited.

### F9 · 🟠 · D82 · `AwbRetryJob`'s `== Paid` filter silences the only never-got-a-label alarm

`src/PhotoPrint.API/BackgroundJobs/AwbRetryJob.cs:86`, `:109`

The fixer's class sweep declared ten `== OrderStatus.Paid` sites as genuinely meaning *strictly*
Paid. Nine hold. Both queries here filter `o.Status == OrderStatus.Paid && o.AwbNumber == null`,
and `:109` feeds the only "this order will never get a shipping label" alarm in the codebase
(`sameday.awb.give-up`, Error) — the class summary calls this job the periodic safety net for AWB
creation.

**Failure scenario.** Sameday is down when order X is paid, so AWB creation fails and retries. An
admin advances X to `Printing` (nothing blocks it). The queued attempt is skipped because the
status is no longer `Paid`, and the retry sweep never sees X again — neither branch matches — so
it is neither re-enqueued nor alarmed. X ships with no AWB, silently.

**Confidence: plausible, not confirmed.** Verified directly: both filters and the give-up Error at
`:109`. Not verified by this pass: the dispatcher's skip-and-drop leg, and how often an admin
advances an order before its label exists. Recorded at 🟠 on the strength of the alarm it
silences; a fixer should confirm the dispatcher leg first.

### F10 · 🟠 · D83 · `metrics.md`'s "fails the build" promise is false, with a live counterexample

`memory-bank/operations/metrics.md:104`

The line promises *"a name that nothing emits fails the build rather than rendering 'No Data'"*.
The exposition the test checks against is seeded **by the test itself**, so the check proves that
the dashboard and `MetricNames` agree — not that production emits anything.
`invoice_anaf_status_total` is queried by the dashboard
(`ops/dashboards/fototipar-overview.json:309`) and by `slos.md`, no site in `src/PhotoPrint.API`
increments it, and the test is green. This is the same over-strong claim F5/D44 corrected in
`slos.md`, surviving one file over; it extends the vocabulary-ahead-of-emission problem already
recorded as D37.

### F11 · 🟠 · D84 · `MetricNamesIn` keeps the truncation F5 fixed in `LabelUsagesIn`

`src/PhotoPrint.Tests/Integration/DashboardMetricNamesTests.cs:275`

The micro-review fixed a regex that truncated at the first `}` so that a route-template value
would silently yield zero label usages. The sibling metric-name extractor in the same file still
does `Regex.Replace(expr, "\\{[^}]*\\}", " ")`. Run against
`http_server_request_duration_seconds_bucket{http_route="api/orders/{id}/payments",http_request_method="POST"}`,
it leaves an unbalanced quote and yields `payments` and `POST` **as metric names**. The first
panel or SLO query filtering on a parameterised route reddens the name tests with a complaint
about a metric called `payments` — loud, but pointing at the wrong thing, and the natural repair
is to delete the matcher. Every `http_route` value in this app except SLO 2's contains `{id}`.

## Part 3 — the 🟡 / ⚪ tail

Recorded as ledger backlog per the README router; detail is one line each because none changes a
decision this pass.

| F# | D# | Detail |
|---|---|---|
| F12 | D85 | `SentryOptionsWiringTests.cs:103` asserts only `NotContain(GuestToken)`, never that the scrubbed URL is present. Measured: `Scrub(Breadcrumb)` returning null for everything leaves this test green (the vacuity is real) but reddens two `SentryDataScrubbersTests` unit tests, so the blanket case is caught elsewhere — the residual is an input-specific throw inside the scrubber, which drops breadcrumbs silently in production. Downgraded from the lens's 🟠 on that measurement |
| F13 | D86 | `metrics.md:99` step 7 tells future authors to prove emissions with `MetricCapture` and never mentions that a measurement emitted outside the test's execution context is now silently invisible. `AwbCreator` is driven in production from a hosted-service dispatcher, so the first integration test that captures `awb_creation_total` through it reads zero and its `BeEmpty()` assertions pass vacuously |
| F14 | D87 | `adr-017-deterministic-trace-id-sampling.md:269` still opens "A promoted error trace is a single root span", 19 lines below the amendment that corrected exactly that wording. Under an inbound `traceparent` the promoted span is remote-parented — the case `An_errored_span_under_an_unsampled_traceparent_is_still_promoted` now exercises |
| F15 | D88 | `DashboardMetricNamesTests.cs:107` walks `panels` only: `templating.list[]` variable queries and `annotations.list[].target` are unreached, and a panel converted to a Grafana library panel leaves the checked set entirely. The D57 row recursion has no dashboard exercising it. `LabelUsagesIn` also mis-handles an escaped quote (`foo_total{bar="a\"b",baz="ok"}` yields zero matches) while the exposition-side `ClosingBrace` handles it — the two sides disagree and the silent one is the query side |
| F16 | D89 | `DashboardMetricNamesTests.cs:73` requires every queried metric to appear in the test-seeded exposition, so a correct future panel on the outbound-HTTP histogram fails the build until someone adds a synthetic emission. Loud and reasonable, but `metrics.md` step 10 does not mention the seeding obligation |
| F17 | D90 | `OrderPhotoPromoter.cs:87` hand-rolls `HasBeenPaid` as `Status < Paid \|\| == PaymentFailed \|\| == Cancelled`. Correct today; unsafe by default — `Status` persists as text, so a future `Refunded` ordered after `Paid` silently passes and gets that order's photos promoted to cloud, the exposure the cancel path purges. `HasBeenPaid` fails closed for the same addition |
| F18 | D91 | `AdminOrderService.cs:183`, `:279`, `:305` are `catch (Exception ex) { LogError(...) }` around work taking the request's token, and the comment at `:174` names client-disconnect cancellation as expected. An admin closing the tab mid-PATCH produces "Refund failed for cancelled order … manual refund required" at Error. Scoped honestly: no 5xx and no Sentry issue — log noise on the highest-signal string in the file |
| F19 | D92 | `OrderStatusMachineTests.cs:27` asserts "reachable from Paid ⇒ `HasBeenPaid`" with `Cancelled` skipped **by name**. Add a future `Refunded` reachable from `Delivered` and this test fails; the cheapest way to make it pass is adding `Refunded` to `PaidStatuses`, which turns the charged-but-not-paid alarm into a silent `duplicate` for exactly the status needing a human. The intended invariant is "reachable from Paid *and still a live fulfilment*" |
| F20 | D93 | `ScrapeListenerGuard.cs:36-49` counts ports and discards the host part, so `http://127.0.0.1:8080;http://+:9090` with `ScrapePort=9090` passes both rules even though the scrape port is the only externally reachable listener and the API port is loopback-only. The shipped compose gets this right, so it is a residual on the new surface |
| F21 | D94 | `Observability:Metrics:PrometheusEndpoint` is configurable while the `Caddyfile:17` hard-codes `handle /metrics*`. Set it to `/telemetry` and Caddy proxies the new path straight from the internet, leaving only the IP allow-list — which is documented as untrustworthy behind a proxy. Documented in three places, enforced by no validator and no test |
| F22 | D95 | `Program.cs:370-375`: `TracingWired == false` outside Development logs `observability.tracing.disabled` and boots, so `Observability:Enabled=true` in Production can mean the whole trace pipeline is silently absent — the same shape as D45 and the same warn-only class as the `ScrapePort == 0` inconsistency the fixer flagged five lines away |
| F23 | D96 | The default propagator is `TraceContext + Baggage` and `AddHttpClientInstrumentation()` injects the current context outbound, so an attacker-supplied `baggage: k=v` rides out to Stripe, Sameday and Google on requests made while handling that request. Low impact, same class as D41, untouched. Also the carrier for Sentry's frozen dynamic-sampling context (F4) |
| F24 | D97 | Nothing exercises `ScrapeListenerGuard.StartedAsync` at all — not the `IServerAddressesFeature` read, not the `observability.metrics.scrape_listener_invalid` log name §14.10 now tells operators to grep for, not the throw. The fixer called this a constraint of TestServer; it is a choice: `WebApplication.CreateBuilder` + `UseUrls("http://127.0.0.1:0")` + `ScrapePort=9090` boots a real Kestrel in-process |
| F25 | D98 | ADR-017 rejects salting the trace-id hash because it would break the "publicly documented, stable" invariant whose stated purpose is that a peer re-derives the same decision — which the F2 amendment has just abandoned ("we no longer agree with a peer, we re-derive"). The recorded reason for keeping the hash unsalted is now weaker than the ADR says, and the amendment does not notice |
| F26 | D99 | `MetricCapture.cs:37` assigns `_outer = Active.Value` only on the path where that value is null (`:30` throws otherwise), so `Dispose`'s restore at `:97` reduces to `Active.Value = null`. Harmless; a trap, because the field name and the restore advertise nesting support the constructor forbids |
| F27 | D100 | DEPLOYMENT §14.8 step 2 says "Set `Enabled=true`, `ScrapePort=9090`, the allow-list" without naming the `ASPNETCORE_URLS` prerequisite. On the shipped compose it holds; anywhere else, following the runbook verbatim now hard-fails boot into a restart loop. §14.10 explains how to diagnose it afterwards; §14.8 could prevent it |
| F28 | D101 | `ddd-01-domain-model.md:57` declares `result ∈ {ok, failed, duplicate, rejected}` — the shipped set has six values and no `rejected`; `ddd-01:121` and `ddd-02:137,244` still present `ParentBasedSampler` as the shipped outer sampler with no amendment note |
| F29 | D102 | `resolution-v2.md` records that TestServer "reports the feature present but empty". `Microsoft.AspNetCore.TestHost` 8.0.11 ships no `IServerAddressesFeature` implementation at all, so `Features.Get<…>()` returns **null** and the guard survives on the `?.` + `?? []` at `ScrapeListenerGuard.cs:77`. Harmless today; misleading to anyone who later simplifies that null-conditional away |
