---
type: resolution
target: 044-045-observability
version: 3
answers: review-v3.md
status: resolved
fixed_commit: dc203c7
closed: 2026-08-06
findings:
  F1:  { status: fixed, commit: c363b7a, note: "Verdict now classifies a socket/pipe address by PREFIX before parsing, so the same string no longer takes a throw path on Windows and a Port=0 path on Linux; port 0 is excluded as a dynamic-bind placeholder. APPROACH REVISED by the adversarial check: my first draft skipped pipes but left the `ports.Count == 0` carve-out, which would have turned today's correct abort for a socket-only host into a SILENT BOOT — so 'addresses present but no TCP port' is now its own refuse verdict, and the TestServer carve-out keys on an empty address list instead. Red proof: the pipe case, the Windows-side unix case, the dynamic-port case and the no-TCP-port case all reddened; the Linux leg's proof is CI itself (6 red runs, green again once this lands). New surface: IsSocketOrPipe + the no-TCP-port verdict" }
  F2:  { status: fixed, commit: 7c002a0, note: "APPROACH REFUTED AND REPLACED by the adversarial check. My draft walked the exception chain for a TimeoutException; the check showed that is still sniffing an undocumented internal shape AND still loses a timeout the caller races by microseconds, because HttpClient decides whether to wrap only at failure time. The validator now OWNS the deadline — CancellationTokenSource(_deadline) linked with the caller's token, linked token passed to GetAsync — so the discriminator is a flag we set: `ct.IsCancellationRequested && !deadline.IsCancellationRequested`. HttpClient.Timeout became a 15s backstop behind the 5s deadline. The fabricated handler is gone; its replacement cancels the caller FROM INSIDE the handler once the deadline has already tripped, reaching the both-fired ordering deterministically instead of as a race. Red proof: restoring the GetBaseException filter reddens exactly the both-fired test. DEVIATION from the check: it proposed FakeTimeProvider; handler-driven ordering is deterministic without a clock and adds no TimeProvider dependency to production. New surface: RequestDeadline + HttpBackstop + the owned deadline" }
  F3:  { status: fixed, commit: feb5636, note: "test-only. InvokeAsync_UnmappedServerError_LogsAtErrorWithTheException pins the level AND the attached exception on the unmapped branch; reverting LogError to LogWarning there reddens it — measured, and before this it left 255 green" }
  F4:  { status: fixed, commit: f2a7ef9, note: "o.TracesSampler now answers with the configured rate on EVERY call. The check settled the load-bearing question from the SDK's IL — TracesSampler does win over an inherited sentry-trace decision, but returning null leaves it — so a 'only when there is no parent' sampler would have left the hole open. Caddy strips sentry-trace and baggage at the edge; traceparent is deliberately NOT stripped, which the check caught: §14.7 documents a chosen traceparent as the way to force a trace for debugging, and D41 already made its sampled flag harmless. §14.7 now records both limits — the edge strip only covers Caddy-routed traffic, and an inbound sentry-trace can still supply trace/parent-span ids (continuity, not a quota decision). Red proof: deleting the sampler reddens the new booted-host test. NOT DONE, recorded for the re-reviewer: the check's real-hub precedence test (SentrySdk.Init inside a serialised collection) — the wiring test catches 'someone returns null', not 'the SDK changed precedence'. New surface: the TracesSampler + two edge header strips" }
  F5:  { status: fixed, commit: 2a82f01, note: "test-only. A_second_capture_in_the_same_test_fails_loudly; deleting the nested-capture throw reddens it — measured, and before this it left 738 green" }
  F6:  { status: fixed, commit: 163f912, note: "test-only. The pin reads the installed sampler off the booted TracerProvider by reflection, chosen over a behavioural assertion because the production composition's ActivitySources are shared with parallel test hosts (D51) and any Recorded-flag assertion is flaky across them. Re-wrapping the production call site reddens it — measured, and before this it left 1120 green. The v2 fixer's recorded reason for leaving it unpinned ('TracerProvider does not expose its sampler') is REFUTED: the member is reachable, and a behavioural route exists too. New surface: the reflection pin, which fails loudly if the SDK renames the member" }
  F7:  { status: fixed, commit: caeb866, note: "owner chose 'match the prose': numerator ok+duplicate, signature_invalid out of the denominator. Applied in BOTH places — slos.md and the dashboard panel, which carried a second copy of the query. Kept as literal `=` matchers so ok and duplicate stay build-checked; the denominator's `!=` value is not checked, and the status block now says so. order_not_found and amount_mismatch stay in the denominator on purpose: those are receipts this app failed to act on" }
  F8:  { status: fixed, commit: caeb866, note: "the status block no longer claims SLO 1 is measured — it names the dilution (~5,760 always-200 /health and /metrics requests a day, so the ratio cannot read below ~99.7%) and says not to read it as availability until drained. SLO 3's note no longer offers SLO 1 as its reliable cross-check and points at the 5xx rate on the webhook routes instead. This does NOT fix D46, which stays deferred by the owner's decision; it stops the document misleading a reader while it is parked" }
  F9:  { status: fixed, commit: 55f6441, note: "give-up query widened to Paid||Printing ONLY. APPROACH REVISED by the check: my draft exposed OrderStatusMachine.PaidStatuses and widened to the whole paid-or-later set, which would have false-alarmed on a legitimately labelless manual Shipped/Delivered (AdminOrderService sets AwbNumber only when the admin supplies one) plus two dev-seed rows, i.e. a recurring page nobody can act on. The check also CONFIRMED the leg review-v3 recorded as unverified: AwbCreator returns Skipped for any non-Paid status and AwbDispatcher logs that at Information and drops the job — nothing recovers the order, which is exactly why the alarm is the fix rather than a re-enqueue. Re-enqueue query left strictly Paid on purpose. Log line now carries status=. Red proof: narrowing back to Paid reddens the new test. STILL OPEN, not this finding: AwbCreator's Paid-only guard means recovery is impossible, only visible" }
  F10: { status: fixed, commit: f81626f, note: "metrics.md step 10 no longer promises 'a name that nothing emits fails the build'. It now states what the test proves — a queried name this repo does not DECLARE fails the build, but the exposition is seeded by the test itself, so a declared-but-never-incremented metric (invoice_anaf_status_total today) stays green — and names the seeding obligation a new panel inherits. Doc-only, no test" }
  F11: { status: fixed, commit: cdb5554, note: "StripBraceGroups replaces the first-'}' regex in MetricNamesIn, reusing the quote-aware ClosingBrace the exposition side already had — one brace-matching rule for both sides of the file. Red proof: A_route_template_label_value_is_not_read_as_a_metric_name; against the old regex the parser yielded 'payments' and 'POST' as metric names. The escaped-quote gap in LabelUsagesIn stays open as D88 (backlog), a different parser" }
  F12: { status: deferred, commit: null, note: "🟡 — ledger backlog (D85) per the README router" }
  F13: { status: deferred, commit: null, note: "🟡 — ledger backlog (D86) per the README router" }
  F14: { status: deferred, commit: null, note: "🟡 — ledger backlog (D87) per the README router" }
  F15: { status: deferred, commit: null, note: "🟡 — ledger backlog (D88) per the README router" }
  F16: { status: deferred, commit: null, note: "🟡 — ledger backlog (D89) per the README router" }
  F17: { status: deferred, commit: null, note: "🟡 — ledger backlog (D90) per the README router" }
  F18: { status: deferred, commit: null, note: "🟡 — ledger backlog (D91) per the README router" }
  F19: { status: deferred, commit: null, note: "🟡 — ledger backlog (D92) per the README router" }
  F20: { status: deferred, commit: null, note: "🟡 — ledger backlog (D93) per the README router" }
  F21: { status: deferred, commit: null, note: "🟡 — ledger backlog (D94) per the README router" }
  F22: { status: deferred, commit: null, note: "🟡 — ledger backlog (D95) per the README router" }
  F23: { status: deferred, commit: null, note: "🟡 — ledger backlog (D96) per the README router" }
  F24: { status: deferred, commit: null, note: "🟡 — ledger backlog (D97); flagged to the owner in summary-v3 as the one minor worth their eye" }
  F25: { status: deferred, commit: null, note: "🟡 — ledger backlog (D98) per the README router" }
  F26: { status: deferred, commit: null, note: "⚪ — ledger backlog (D99) per the README router" }
  F27: { status: deferred, commit: null, note: "⚪ — ledger backlog (D100) per the README router" }
  F28: { status: deferred, commit: null, note: "⚪ — ledger backlog (D101) per the README router" }
  F29: { status: deferred, commit: null, note: "⚪ — ledger backlog (D102) per the README router" }
---

# Resolution v3 — 044-045-observability

Fixer's answer to [review-v3.md](review-v3.md) (immutable). The review named 29 findings;
**the 🔴 (F1) and the ten 🟠 (F2–F11), ledger D74–D84, are this fix round**. The 18 🟡/⚪
(F12–F29, D85–D102) are deferred to the [ledger](ledger.md) backlog per the README router.

**Nothing here is `verified`.** Only `review-v4.md` — a re-review by someone who did not fix —
can set that status.

## Process note the re-reviewer must weigh

The v3 verification pass and this fix round run in **the same session**, so the agent that
authored these findings is the agent fixing them — the same caveat
[resolution-v2](resolution-v2.md#process-note-the-re-reviewer-must-weigh) recorded, and for the
same reason it matters: a fixer who inherits someone else's findings disputes some of them, and
that pressure is absent here. Two things partly offset it this time. First, most of the serious
findings in scope were settled by **measurement recorded before this round started** — a real
`HttpClient` probe on net8.0 (F2), mutations that left the suite green (F3, F5, F6), and the CI
log itself (F1) — so confirming them is re-reading evidence, not re-forming a judgment. Second,
F1's red proof is **CI on a platform this machine is not**, which no amount of fixer optimism can
fake green. **The v4 re-review should be run from a fresh session.**

## Fix round scope

| Cluster | Findings | Owner file(s) | Approach-check |
|---|---|---|---|
| A — scrape-listener port parsing (**blocker**) | F1 (D74) | `Observability/ScrapeListenerGuard.cs`, `Tests/Unit/Observability/ScrapeListenerCheckTests.cs` | **required** — narrows the catch that currently classifies "portless", inside a gate that aborts boot |
| B — cancellation vs dependency failure | F2 (D75) | `Services/GoogleTokenValidator.cs`, `Tests/Unit/Services/GoogleTokenValidatorTests.cs` | **required** — changes a catch/mapping layer and what reaches Sentry and SLO 1 |
| C — unmapped-500 log level | F3 (D76) | `Tests/Unit/Middleware/ExceptionHandlerMiddlewareTests.cs` | not needed (test-only) |
| D — Sentry inbound trace decision | F4 (D77) | `Program.cs`, `Configuration/SentrySettings.cs`, `Caddyfile` | **required** — changes sampling semantics, same class as D41; **owner gate** |
| E — nested metric capture | F5 (D78) | `Tests/Unit/Observability/MetricCaptureIsolationTests.cs` | not needed (test-only) |
| F — sampler call-site pin | F6 (D79) | `Tests/Unit/Observability/TracingExporterSelectionTests.cs` | not needed (test-only) |
| G — SLO and metric documents | F7 (D80), F8 (D81), F10 (D83) | `memory-bank/operations/slos.md`, `memory-bank/operations/metrics.md`, `ops/dashboards/fototipar-overview.json` | not needed (doc/query); **owner gate on F7** |
| H — dashboard metric-name parser | F11 (D84) | `Tests/Integration/DashboardMetricNamesTests.cs` | not needed (test-only) |
| I — AWB retry sweep coverage | F9 (D82) | `BackgroundJobs/AwbRetryJob.cs`, `Services/Sameday/AwbDispatcher.cs` | **required** — changes which orders a periodic sweep picks up; **owner gate** |

Ordering: A first (it is the blocker and the branch cannot merge while CI is red), then the
test-only clusters C, E, F, H while the checks fly, then B, then G and I once the gate is answered.

## Findings

<!-- rendered:findings-table:start -->
| ID | Sev | Title | Status | Commit | How |
|---|---|---|---|---|---|
| F1 |  |  | fixed | `c363b7a` | Verdict now classifies a socket/pipe address by PREFIX before parsing, so the same string no longer takes a… |
| F2 |  |  | fixed | `7c002a0` | APPROACH REFUTED AND REPLACED by the adversarial check. My draft walked the exception chain for a TimeoutEx… |
| F3 |  |  | fixed | `feb5636` | test-only. InvokeAsync_UnmappedServerError_LogsAtErrorWithTheException pins the level AND the attached exce… |
| F4 |  |  | fixed | `f2a7ef9` | o.TracesSampler now answers with the configured rate on EVERY call. The check settled the load-bearing ques… |
| F5 |  |  | fixed | `2a82f01` | test-only. A_second_capture_in_the_same_test_fails_loudly; deleting the nested-capture throw reddens it — m… |
| F6 |  |  | fixed | `163f912` | test-only. The pin reads the installed sampler off the booted TracerProvider by reflection, chosen over a b… |
| F7 |  |  | fixed | `caeb866` | owner chose 'match the prose': numerator ok+duplicate, signature_invalid out of the denominator. Applied in… |
| F8 |  |  | fixed | `caeb866` | the status block no longer claims SLO 1 is measured — it names the dilution (~5,760 always-200 /health and… |
| F9 |  |  | fixed | `55f6441` | give-up query widened to Paid||Printing ONLY. APPROACH REVISED by the check: my draft exposed OrderStatusMa… |
| F10 |  |  | fixed | `f81626f` | metrics.md step 10 no longer promises 'a name that nothing emits fails the build'. It now states what the t… |
| F11 |  |  | fixed | `cdb5554` | StripBraceGroups replaces the first-'}' regex in MetricNamesIn, reusing the quote-aware ClosingBrace the ex… |
| F12 |  |  | deferred | — | 🟡 — ledger backlog (D85) per the README router |
| F13 |  |  | deferred | — | 🟡 — ledger backlog (D86) per the README router |
| F14 |  |  | deferred | — | 🟡 — ledger backlog (D87) per the README router |
| F15 |  |  | deferred | — | 🟡 — ledger backlog (D88) per the README router |
| F16 |  |  | deferred | — | 🟡 — ledger backlog (D89) per the README router |
| F17 |  |  | deferred | — | 🟡 — ledger backlog (D90) per the README router |
| F18 |  |  | deferred | — | 🟡 — ledger backlog (D91) per the README router |
| F19 |  |  | deferred | — | 🟡 — ledger backlog (D92) per the README router |
| F20 |  |  | deferred | — | 🟡 — ledger backlog (D93) per the README router |
| F21 |  |  | deferred | — | 🟡 — ledger backlog (D94) per the README router |
| F22 |  |  | deferred | — | 🟡 — ledger backlog (D95) per the README router |
| F23 |  |  | deferred | — | 🟡 — ledger backlog (D96) per the README router |
| F24 |  |  | deferred | — | 🟡 — ledger backlog (D97); flagged to the owner in summary-v3 as the one minor worth their eye |
| F25 |  |  | deferred | — | 🟡 — ledger backlog (D98) per the README router |
| F26 |  |  | deferred | — | ⚪ — ledger backlog (D99) per the README router |
| F27 |  |  | deferred | — | ⚪ — ledger backlog (D100) per the README router |
| F28 |  |  | deferred | — | ⚪ — ledger backlog (D101) per the README router |
| F29 |  |  | deferred | — | ⚪ — ledger backlog (D102) per the README router |
<!-- rendered:findings-table:end -->

## Triage — confirmation that each finding still exists

Confirmed at `cd99cdb` (source identical to `7e28317`).

| F# | Confirmed how |
|---|---|
| F1 | CI red at the branch tip; `ScrapeListenerGuard.cs:23` still classifies "portless" by catching `FormatException`, which only fires on Windows for a unix path |
| F2 | `GoogleTokenValidator.cs:42`/`:65` still use `ex.GetBaseException() is not TimeoutException`; probe on net8.0 shows the base is never a `TimeoutException` |
| F3 | measured this round's predecessor: `LogError`→`LogWarning` at `ExceptionHandlerMiddleware.cs:142` left 255 passed / 0 failed |
| F4 | `Program.cs` configures `TracesSampleRate` with no `TracesSampler`; `Caddyfile` strips no headers |
| F5 | measured: deleting the `Active.Value is not null` throw left 738 passed / 0 failed |
| F6 | measured: re-wrapping the call site at `ObservabilityExtensions.cs:71` left 1120 passed / 0 failed |
| F7 | `slos.md:83` query is `result="ok"` / total while `:72` scopes SLO 3 to include correctly-rejected duplicates |
| F8 | `slos.md:5-7` names only SLO 3; `:86-94` offers SLO 1 as the cross-check |
| F9 | `AwbRetryJob.cs:86` and `:109` both filter `o.Status == OrderStatus.Paid` |
| F10 | `metrics.md:104` still promises "a name that nothing emits fails the build"; `invoice_anaf_status_total` has no emission site |
| F11 | `DashboardMetricNamesTests.cs:275` still does `Regex.Replace(expr, "\\{[^}]*\\}", " ")` |

## Decisions

### Owner gate (2026-08-05) — four answers, all as recommended

Asked once after triage, per the fixer contract. **F4/D77:** fix now, same posture as D41 — ignore
an inbound sampling decision and strip the header at the edge. **F7/D80:** match the prose —
numerator `ok` + `duplicate`, `signature_invalid` out of the denominator. **F9/D82:** widen the
give-up alarm query only, not the re-enqueue query. **F2/D75:** make the carve-out actually work
and report the outage, accepting that some abandoned logins correctly produce 502s.

### All four approach-checks came back "needs revision" — and every revision mattered

This is the round's headline process fact. Four checks ran, none cleared my draft, and in three
cases the draft would have shipped a new defect:

1. **Cluster A** — my draft skipped socket/pipe addresses but kept the `ports.Count == 0`
   carve-out. On Linux a socket-only host currently *aborts* (rule 1, for the wrong reason); my
   draft would have turned that into a **silent boot with a scrape port nothing can serve**. The
   shipped fix splits the carve-out: no addresses reported at all → stay quiet (TestServer);
   addresses present but no TCP port → its own refuse verdict. The check also pushed the
   classification *before* `Parse`, so the platform fork is gone rather than merely worked around.
2. **Cluster B** — my draft walked the exception chain for a `TimeoutException`. The check showed
   that is still type-sniffing an undocumented internal shape, and that `HttpClient` decides
   whether to wrap **at failure time**, so a genuine timeout the caller races by microseconds still
   loses. Owning the deadline removes the guesswork entirely.
3. **Cluster I** — my draft widened the sweep to the full paid-or-later set and exposed
   `PaidStatuses` to do it. The check found that a labelless `Shipped`/`Delivered` is *legitimate*
   manual fulfilment, so that would have converted a silent gap into a recurring page nobody can
   act on. `Paid || Printing` is the correct set, and `PaidStatuses` stayed private.
4. **Cluster D** — the check settled from the SDK's IL that `TracesSampler` *does* win over an
   inherited `sentry-trace`, but **only if it returns a value**; a sampler that returns null for
   parentless transactions would have left the hole open. It also caught that stripping
   `traceparent` at the edge would have been a regression, because §14.7 documents a chosen
   `traceparent` as the supported way to force a trace on.

### Cluster I — the check confirmed what review-v3 could not

review-v3 recorded D82 as **plausible, one leg unverified**. The check verified it: `AwbCreator`
returns `Skipped("status is Printing, not Paid")` and `AwbDispatcher` logs that at **Information**
and drops the job. Nothing retries, nothing dead-letters. That is why the fix is an alarm rather
than a re-enqueue — and it is also the boundary below.

### Deliberate deviations

- **Cluster B, from the check's own advice:** it proposed `FakeTimeProvider` for determinism. The
  handler-driven ordering used instead is deterministic without a clock (the handler cancels the
  caller only *after* the deadline has tripped) and adds no `TimeProvider` dependency to a
  production class. The 50 ms deadline in those two tests has no competing timer, so there is no
  race to lose.
- **Cluster F, from the review's own suggestion:** review-v3 said a behavioural assertion through
  the real composition was available. It is, but the production pipeline's `ActivitySource`s are
  shared with parallel test hosts (D51), so any `Recorded`-flag assertion is flaky by construction.
  The pin reads the installed sampler's type instead — deterministic, and it fails loudly if the
  SDK renames the member.
- **Cluster A, `IsSocketOrPipe` is `Ordinal`, deliberately:** the first micro-review noted that
  `HTTP://UNIX:/…` is therefore not classified by prefix. Matching `BindingAddress`'s own ordinal
  check is the point — a case-insensitive test here would classify addresses the parser does not,
  which is a *new* divergence rather than a fix. Those forms still fall to the retained
  `FormatException` catch, which now only ever means "malformed".

### Fix-diff micro-reviews — the second one caught a defect I shipped

Two fresh-eyes agents over the round's diff, split by risk: one over the five test-and-parser
fixes, one over the behaviour and document fixes. **18 findings between them.** Repaired in
`d1ffee7` and `dc203c7`:

1. **My F7 fix would have blinded SLO 3 in the healthy case.** `sum(A) + sum(B)` is *empty* when
   `B` matches no series, and `payment_webhook_total{result="duplicate"}` does not exist until the
   first duplicate is recorded. So the panel I "fixed" would have read **"No Data"** for as long as
   nothing was wrong — on the one SLO whose prose says a single miss is disproportionately costly.
   Both terms now carry `or vector(0)`, and `slos.md` explains why the guard is load-bearing.
2. **F8 fixed the document and not the wall.** The Availability panel still presented the diluted
   ratio with no caveat, and the operator looks at the panel, not `slos.md`. The panel now carries
   a `description` naming the dilution and D46.
3. **The `status=` field never reached the log catalogue**, and §12.8 still said the give-up alarm
   means "24 h elapsed" without the `Printing` case that is the entire point of F9. Both fixed.
4. **My F2 fix falsified §13.1.** It states a disconnecting caller is never a Sentry issue; that is
   now conditional — if our deadline had already elapsed, it is a real outage and is captured. The
   section now says so, and says why the trade is one-sided on purpose.
5. **The story doc still carried the retired SLO 3 criterion**, so the next verification pass would
   have re-derived the definition F7 just replaced. Amended in place.
6. **First micro-review, on cluster A:** the `Critical` line §14.10 tells operators to grep for had
   no test, and I had just routed a third refusal reason through it. Closed with a real-Kestrel
   boot test (`d1ffee7`) that pins the abort *and* the log line; downgrading it to `Warning`
   reddens that test — measured.

**Checks that came back clean, recorded so they are not re-litigated:** the exception filter runs
in pass one, before the enclosing `using` disposes, so `deadline.IsCancellationRequested` is read
on a live CTS (and `IsCancellationRequested` never throws `ObjectDisposedException`); MS.DI honours
the constructor's default parameter, so `AddScoped<IGoogleTokenValidator, GoogleTokenValidator>`
still resolves; `(Paid || Printing)` over a `HasConversion<string>()` column becomes two
parameterised string comparisons on both SQLite and Npgsql, with no client evaluation; `MarkOnce`
stays one-shot across a status change; `header_up -X` is valid Caddy v2 and strips the *upstream
request* header.

### Genuinely new, outside the finding set — NOT fixed

- **SLO 4 and SLO 5 carry the exact defect F7 fixed.** `awb_creation_total{result="ok"} / total`
  puts `skipped` in the denominator — and `AwbCreator` returns `Skipped` for benign cases including
  "another worker holds a fresh claim", which the retry sweep manufactures on every interval. SLO 5
  counts `pending` the same way. I did **not** fix them: F7's numerator was an owner decision about
  what SLO 3 should mean, and each of these is the same kind of definitional call, not a mechanical
  sweep. They need the same one-line question the owner answered for SLO 3.
- **Nothing tests the Caddyfile.** The two `header_up` strips can be deleted silently; the document
  is their only guard.
- **`tracestate` is still honoured and forwarded** — the remaining attacker-seedable member of the
  trace-header family. Low impact (`DeterministicTraceIdSampler` ignores it), unswept.

### Remaining boundaries — not fixed, for the re-reviewer

- **AWB recovery is still impossible, only visible.** `AwbCreator`'s `Paid`-only guard (plus its
  claim query) is why an order advanced to `Printing` can never get its label; F9 makes it alarm.
  Fixing recovery is an AWB-subsystem behaviour change and was not in scope. The check flagged that
  the stated reason for leaving the re-enqueue query alone is circular in exactly this way.
- **Cluster D has no real-hub precedence test.** The shipped pin catches "someone changed the
  sampler to return null"; it does **not** catch "the SDK changed whether a sampler outranks an
  inherited decision". That needs `SentrySdk.Init` inside a serialised collection, which the check
  spelled out and this round did not build. `Sentry:Enabled=false` everywhere makes this latent.
- **The edge strip only covers Caddy-routed traffic.** Requests reaching `api:8080` inside the
  compose network — health checks, anything on staging bypassing Caddy — still carry
  `sentry-trace`; the sampler is what protects those. Recorded in §14.7.
- **SLO 3's denominator now excludes `signature_invalid` via `!=`, whose value is not
  build-checked.** Renaming that constant silently changes the denominator. `slos.md` now states
  that negative and regex matchers are outside the test's net.
- **D46 is still deferred.** F8 only stops `slos.md` misleading a reader about it; SLO 1's
  availability number is still diluted and still cannot read below about 99.7%.
- **A flaky test, filed to [inbox.md](../inbox.md), not fixed here:**
  `EmailRetryJobTests.Processing_SuccessfulSend_MarksEmailAsSent` failed once under parallel load
  and passed 4/4 in isolation. It surfaced as unexplained collateral in a mutation run and is not
  caused by this round.

### On this round's runtime metric

One continuous session with a single batched owner gate, so `blocked_s` is a real measurement. The
four approach-checks ran in parallel in the background while the four test-only clusters were
implemented, which is why active time is far below the sum of the checks' wall-clock.
