---
type: findings
target: 044-045-observability
version: 4
for: review-v4.md
commit: dc203c7
date: 2026-08-06
---

# Findings detail — v4 verification of the 044-045 v3 fix round

Companion to [review-v4.md](review-v4.md). Canonical identities in [ledger.md](ledger.md).
Severity vocabulary and the re-arm rule are in [reviews/README.md](../README.md).

Every measurement below was taken by the main agent at `dc203c7` (source identical to the branch
tip `f0aadd7`), scoped to the observability namespaces — `Integration` +
`Unit.{Observability,Middleware,Configuration,Validators,Services,Controllers,BackgroundJobs}`,
**1133 tests**, 10 MinIO skips — per the repo's scoped-run rule. "0 red" always means the full
1133 stayed green with the mutation applied.

---

## F1 · 🟠 · D103 · SLO 3's `or vector(0)` guards are pinned by nothing

**Files:** `memory-bank/operations/slos.md:95-97`, `ops/dashboards/fototipar-overview.json:232`
**Cause:** fix-caused (D80) — the guard was added mid-round by the fixer's own second micro-review.

The round's own micro-review caught that `sum(A) + sum(B)` returns an **empty** vector when `B`
matches no series, and `payment_webhook_total{result="duplicate"}` does not exist until the first
duplicate is recorded — so the panel the fix had just "corrected" would have read **"No Data" for
as long as nothing was wrong**, on the one SLO whose prose says a single miss is disproportionately
costly. The repair was `or vector(0)` on both numerator terms, in both copies of the query.

Nothing tests it. **Measured (M15):** deleting both `or vector(0)` guards from `slos.md` leaves
**1133 green**. `DashboardMetricNamesTests` extracts metric names and label matchers only; `vector`
is discarded as a function call, so the guard is invisible to every check in the repo.

**Failure scenario.** A later author simplifies the expression back to `(sum(A) + sum(B)) / sum(…)`
— the obvious readability edit, and the exact form the fixer first shipped. The suite stays green,
the build passes, and SLO 3's panel reads "No Data" until the first duplicate webhook ever arrives.
The one condition the panel exists to detect is a payment that silently failed; the panel is blank
in precisely the state where nothing has gone wrong yet, so the blankness reads as normal.

**Why this is 🟠 and not a nit.** This is the same class as review-v3's D78/D79 — a mechanism that
is the entire point of a fix, with no revert-proof — and here the defect it guards against was
shipped once already, inside this round.

---

## F2 · 🟠 · D104 · Both invariants F2's new discriminator rests on are unpinned

**Files:** `src/PhotoPrint.API/Services/GoogleTokenValidator.cs:43-50`,
`src/PhotoPrint.API/Extensions/SocialAuthExtensions.cs:17`
**Cause:** fix-caused (D75).

The F2 fix replaced the refuted `GetBaseException() is not TimeoutException` filter with an owned
deadline: a 5 s `CancellationTokenSource` linked with the caller's token, the linked token passed to
`GetAsync`, and `HttpClient.Timeout` raised to a 15 s backstop behind it. The discriminator is then
`ct.IsCancellationRequested && !deadline.IsCancellationRequested`.

The discriminator itself **is** pinned — **measured (M6):** restoring the old filter reddens exactly
`ValidateAsync_DeadlineElapsedThenTheCallerAborted_StillThrowsBadGatewayException`, one red, no
collateral. But the two facts that make it *mean* anything are not:

1. **The deadline actually bounds the request.** **Measured (M7):** passing `ct` instead of
   `attempt.Token` to `GetAsync` leaves **1133 green** — the run took 38 s instead of ~20 s, because
   both deadline tests then passed via the 15 s `HttpClient` backstop rather than the 5 s deadline.
2. **`HttpBackstop > RequestDeadline`.** **Measured (M16a):** putting `TimeSpan.FromSeconds(5)` back
   in the DI registration leaves **1133 green**.

**Failure scenario.** Leg 2 is the dangerous one. With the registered timeout back at 5 s, the two
timers race: when `HttpClient`'s own timeout wins, `deadline.IsCancellationRequested` is still
`false`, so a genuine Google outage that a user then gives up on takes the rethrow path again — a
client abort, invisible to Sentry and to the 5xx numerator. That is **D75 restored**, with a green
suite. Leg 1 is milder but real: the user-facing wait silently triples from 5 s to 15 s, and the
fix is what raised the ceiling that makes 15 s possible.

**What would close it.** One test that resolves the registered `"Google"` client and asserts
`Timeout > GoogleTokenValidator.RequestDeadline`, and one that asserts the deadline bounds a real
hanging handler in bounded wall-clock (e.g. a 50 ms deadline completing well under the backstop).

---

## F3 · 🟠 · D105 · SLO 4 and SLO 5 carry the defect F7 fixed, and `slos.md` now implies there are only two caveats

**Files:** `memory-bank/operations/slos.md:6` (the enumeration), `:135`, `:158` (the queries)
**Cause:** pre-existing defect (extends D80's class); the misleading enumeration is fix-caused (D81).

The round rewrote the status block to say **"Two caveats that matter:"** and list SLO 1's dilution
and SLO 3's blind spot. The fixer's own resolution records a third of exactly the same kind, and
deliberately did not fix it:

- `slos.md:135` — SLO 4: `awb_creation_total{result="ok"} / awb_creation_total`
- `slos.md:158` — SLO 5: `invoice_anaf_status_total{status="accepted"} / invoice_anaf_status_total`

Both put benign outcomes in the denominator. `AwbCreator` returns `Skipped` for cases including
"status is X, not Paid" and "AwbNumber already populated"; SLO 5 counts `pending` the same way.
This is the identical shape F7 just removed from SLO 3 — and F7's removal was an **owner decision**
about what the ratio should mean, which is why the fixer correctly declined to sweep it silently.

**Failure scenario.** An operator opens the dashboard in a week where every order got its label,
sees SLO 4 at 94% against a 98% target, follows the documented action ("check Sameday's status
page"), finds nothing wrong, and learns to discount the panel. The status block told them there
were two caveats and this was not one of them. Unlike SLO 3's original defect, which was
occasionally wrong, this panel is *persistently* wrong, which is worse for trust.

**What it needs:** the same one-line question the owner answered for SLO 3, once for SLO 4 and once
for SLO 5 — and until then, a third bullet in the enumeration.

---

## F4 · 🟠 · D113 · `secret-scan` fails on every pull-request run of this branch, and has since before the fix round

**Files:** `src/PhotoPrint.Tests/Unit/Configuration/SentryDataScrubbersTests.cs:16`, `.gitleaks.toml`
**Cause:** pre-existing (introduced `44c3e2d`, 2026-07-31, inside the bolt-045 work).

review-v3's blocker was CI red. That blocker is genuinely gone — the `ci` workflow is **green on
`ubuntu-latest`** at the branch tip. But `ci` is not the only gate. The `secret-scan` workflow has
failed on **every `pull_request` run of this branch** — f0aadd7, 86f4cc1, 9884ca2, cd99cdb, c92ad77,
1068837, fa95883, e791c40, 8daa977 — while passing on every `push` run, because the push scan only
covers new commits and the PR scan covers the whole PR range.

gitleaks reports one leak:

```
Finding:  ...rivate const string GuestToken = "REDACTED"
RuleID:   generic-api-key   Entropy: 3.684184
File:     src/PhotoPrint.Tests/Unit/Configuration/SentryDataScrubbersTests.cs   Line: 16
```

The value is `"5f0c-live-guest-guid"` — a fabricated test string, not a credential. It trips
`generic-api-key` on entropy plus the substring `live`. `.gitleaks.toml` allowlists
`src/PhotoPrint.Tests/Helpers/TestKeys.cs`, `docs/*`, `memory-bank/*`, `README.md` and `hooks/*` —
not this file. The repo's own note says the set must stay in sync with `hooks/pre-commit`, which is
why the commit passed locally and the PR gate did not.

**Failure scenario.** The branch cannot show an all-green PR. Worse, the signal has been red for six
days across two full review passes and a fix round without being noticed — the same
nobody-was-watching failure review-v3 named for the `ci` workflow, one workflow over.

**Fix is trivial** (allowlist the path, add the fingerprint, or rename the constant to something
low-entropy) **but it is a decision about the scanner's policy**, so it is recorded rather than
assumed.

---

## F5 · 🟡 · D106 · `Sentry:TracesSampleRate=0` no longer switches performance monitoring off

**File:** `src/PhotoPrint.API/Program.cs:59` · **Cause:** fix-caused (D77).

`SentryOptions.IsPerformanceMonitoringEnabled` is
`EnableTracing switch { … null => TracesSampler is not null || TracesSampleRate is > 0.0, … }`, and
`EnableTracing` is never assigned. Assigning `TracesSampler` unconditionally therefore makes
performance monitoring enabled **forever**: at rate 0 the SDK still allocates a `TransactionTracer`
per request and invokes the sampler, then discards the result at `NextBool(0.0)`.

**Failure scenario.** `docs/DEPLOYMENT.md` §13 tells an operator to drop the rate to shed
transaction cost during an incident; 0 is the natural floor. Before this round that turned the
machinery off. Now it only turns the *output* off, and the per-request work stays. No data leaves
the process and no quota is consumed, which is why this is 🟡 rather than 🟠 — but an off switch
that no longer switches off is worth one line of documentation or an `EnableTracing` assignment.

*Evidence: `sentry-dotnet` 4.13.0 `src/Sentry/SentryOptions.cs` and `Internal/Hub.cs`, read at the
installed version's tag. Not measured at runtime.*

---

## F6 · 🟡 · D107 · The booted-host sampler test covers only the "caller says sampled" direction

**File:** `src/PhotoPrint.Tests/Integration/SentryOptionsWiringTests.cs:38-48` · **Cause:** fix-caused (D77).

review-v3's F4 named both halves of the hole: an inbound `sentry-trace` ending `-1` bought full
sampling, and one ending `-0` **blinded** performance monitoring. The new test builds exactly one
context, `isSampled: true, isParentSampled: true`.

**Failure scenario.** A later edit to `ctx => ctx.TransactionContext.IsParentSampled == false ? null : rate`
— an "abstain when the caller asked not to be traced" change that reads reasonable — returns the
rate for the only tested context and stays green, while `Hub.StartTransaction` skips its sampling
block because `IsSampled` is already `false` from the header. The `-0` half of D77 is back.

**What would close it:** a second row with `isSampled: false, isParentSampled: false`.

---

## F7 · 🟡 · D108 · The re-enqueue query's `Paid`-only scope — an explicit owner decision — is pinned by nothing

**Files:** `src/PhotoPrint.API/BackgroundJobs/AwbRetryJob.cs:86`,
`src/PhotoPrint.Tests/Unit/Services/Sameday/AwbRetryJobTests.cs:244-245`
**Cause:** fix-caused (D82).

At the owner gate the decision was explicit: **widen the give-up alarm query only, not the
re-enqueue query.** The alarm side is pinned — **measured (M12):** narrowing it back to `Paid`
reddens the new test, one red. The re-enqueue side is not: **measured (M16e):** widening it to
`Paid || Printing` leaves **1133 green**.

The new test's second assertion looks like the guard but cannot be one: its order is seeded
`paidAt: T0.AddHours(-25)` against a 24 h give-up window, so it is outside the re-enqueue window
whatever statuses that query admits. The pre-existing `Does_not_enqueue_orders_in_non_Paid_status`
seeds `Cancelled`, so it does not catch `Printing` either.

**Failure scenario.** A later author "completes" F9 by widening both queries for symmetry. Every
sweep interval then enqueues orders in `Printing`; `AwbCreator` returns `Skipped`, `AwbDispatcher`
logs at `Information` and drops the job, and the sweep churns indefinitely with no test objecting
and no alarm — the outcome the approach-check specifically talked the fixer out of.

---

## F8 · 🟡 · D109 · Rule 3 now aborts boot on a unix-socket API plus a dedicated TCP metrics port, and says something false while doing it

**Files:** `src/PhotoPrint.API/Observability/ScrapeListenerGuard.cs:57-63`,
`src/PhotoPrint.Tests/Unit/Observability/ScrapeListenerCheckTests.cs:123-130`
**Cause:** fix-caused (D74).

Excluding socket and pipe addresses from the port set is correct for the blocker it fixed, but it
also changes what rule 3 (`ports.Count == 1`) sees. With
`ASPNETCORE_URLS=http://unix:/run/api.sock;http://+:9090` and `ScrapePort=9090`, post-bind
addresses are `["http://unix:/run/api.sock", "http://[::]:9090"]`; the socket is skipped, `ports`
is `{9090}`, and the guard throws from `StartedAsync` with:

> …is the only port this process listens on, so /metrics is served on the same listener the reverse
> proxy talks to and the scrape-port gate protects nothing

which is false for that topology — the proxy talks to the socket, so the gate protects exactly what
it should. Pre-fix on Linux the socket parsed to `Port=0`, `ports` was `{0, 9090}`, and boot
succeeded. The shipped theory at `ScrapeListenerCheckTests.cs:123-130` asserts this abort as
intended, so it is a decision, not an oversight — but the decision is not recorded anywhere and the
message is wrong.

**Failure scenario.** Someone moves the API behind a unix socket (a normal Caddy hardening step),
deploys, and the container enters a restart loop under `restart: unless-stopped` with a `Critical`
line that tells them the opposite of what is true. Today's compose file proxies `api:8080` over TCP,
so nothing is broken now, which is why this is 🟡.

---

## F9 · 🟡 · D110 · The dilution numbers now stamped on the operator-facing panel are wrong

**Files:** `memory-bank/operations/slos.md:8-12`, `ops/dashboards/fototipar-overview.json:60`
**Cause:** fix-caused (D81) — the figures are inherited from D46, but this round moved them onto the wall.

The caveat says the denominator includes "roughly 5,760 always-200 `/health` and `/metrics`
self-monitoring requests a day, so the ratio cannot read below about 99.7%". Both halves fail
against the repo's own numbers:

| Source | Value | Per day |
|---|---|---|
| `docs/DEPLOYMENT.md:1048` | `scrape_interval: 15s` | 5,760 `/metrics` |
| `Dockerfile:43` | `HEALTHCHECK --interval=30s` | 2,880 `/health` |
| `docs/DEPLOYMENT.md:950` | "~500 req/day" customer traffic | 500 |

So 5,760 is `/metrics` **alone**, not both; self-monitoring is ~8,640/day. And the floor — every
customer request failing — is `8640 / 9140 ≈ 94.5%`, not 99.7%. Reaching a 99.7% floor would need
customer traffic of about 17 requests a day.

**Failure scenario.** An operator told the number cannot go below 99.7% sees 95% and concludes the
instrumentation is broken, when in fact every customer request is failing. The wrong number is now
on the panel itself, which is the surface F8's own repair argued operators actually read.

---

## F10 · 🟡 · D111 · SLO 3's documented query has no time window, while its heading and its dashboard twin do

**Files:** `memory-bank/operations/slos.md:80` (heading), `:95-97` (query),
`ops/dashboards/fototipar-overview.json:232` (twin)
**Cause:** pre-existing shape; the block was rewritten this round without adding the window.

The heading is `≥ 99.9% rolling 7 days`. The documented query is over bare cumulative counters —
no `rate`, no `[7d]`. The dashboard copy is `sum(rate(…[7d]))`. The two copies now agree on the
numerator, the denominator and the guards, but they are still not the same measurement.

**Failure scenario.** Someone builds the alert `slos.md` asks for from the documented query and gets
an all-time average, which after one good month cannot fall below 99.9% inside a 7-day breach — an
alert that can never fire. SLO 4 and SLO 5 carry the same windowless shape.

---

## F11 · 🟡 · D112 · F2's class is unswept: two sibling sites still infer "our own timeout" from a token flag

**Files:** `src/PhotoPrint.API/Services/Sameday/AwbCreator.cs:166`,
`src/PhotoPrint.API/BackgroundJobs/ShipmentTrackingJob.cs:184`
**Cause:** pre-existing.

The round's class sweep covered `== OrderStatus.Paid` thoroughly (nine sibling status filters
checked, all correct; the two remaining `Paid`-only sites are the disclosed boundaries). The
*cancellation-discrimination* class — the inference F2 removed — was not swept. Both sites above
still decide "this was our timeout, not the caller leaving" from `!ct.IsCancellationRequested`.

**Failure scenario.** Host shutdown lands while a Sameday `CreateAwbAsync` call has already
exceeded its `HttpClient` timeout. `!ct.IsCancellationRequested` is false, so the timeout arm is
skipped; the exception reaches `AwbCreator.cs:50`, is rethrown, and `AwbDispatcher.cs:69` swallows
it as shutdown. `RetryLater(PreserveClaim: true)` never happens, so the claim written earlier is
neither released nor deliberately held, and the next boot's sweep excludes the order until the
claim TTL expires — on an order that may already carry a billed AWB, with no metric and no log.
Bounded by the claim TTL, which is why it is 🟡.

*Reported by the behaviour lens; confirmed by reading both catch sites. Not measured.*

---

## F12 · 🟡 · D114 · The new real-Kestrel boot test runs in the un-collectioned parallel pool, and its side effects depend on an unpinned environment variable

**File:** `src/PhotoPrint.Tests/Unit/Observability/ScrapeListenerCheckTests.cs:94-120`
**Cause:** fix-caused (D74) · extends D51.

The test is valuable — it is the only thing that exercises `StartedAsync`, and **measured (M5):**
downgrading the `Critical` line to `Warning` reddens it. Two hazards come with it. The class carries
no `[Collection]`, so it boots a real Kestrel plus a real meter pipeline inside the parallel unit
pool, which is the shared-state hazard D51 already records for `TracingExporterSelectionTests`.
And `builder.Environment` comes from ambient configuration: CI sets neither `ASPNETCORE_ENVIRONMENT`
nor `DOTNET_ENVIRONMENT` and there is no runsettings, so it is `Production` and only a metrics
pipeline is built — but on a developer machine with `ASPNETCORE_ENVIRONMENT=Development` the same
unit test installs a full `TracerProvider` with AspNetCore and EF Core instrumentation and a
**console exporter**, process-wide, onto the `ActivitySource`s D51 records as shared.

**Failure scenario.** Environment-dependent test behaviour, in the round whose blocker was an
environment-dependent test. It does not fail an assertion today; it adds cross-test coupling and
console noise, and it makes a local run and a CI run different experiments.

---

## F13 · 🟡 · D115 · The standard CLAUDE.md routes readers to still describes the old 5 s `HttpClient` timeout

**File:** `memory-bank/standards/system-architecture.md:45` · **Cause:** fix-caused (D75).

The line still reads `(5s timeout; unreachable → 502)`. Two things changed and neither is recorded
there: the real bound is now owned by `GoogleTokenValidator.RequestDeadline` with `HttpClient.Timeout`
demoted to a 15 s backstop, and "unreachable" now also covers a caller who disconnects **after** the
5 s deadline elapsed. `docs/DEPLOYMENT.md` §13.1 records the second half; the standard does not.

5 s is still the effective wall-clock, so this is incompleteness rather than falsehood — but
CLAUDE.md's "standards are descriptive: if you change reality, update the standard that states it
in the same change" targets exactly this.

---

## F14 · 🟡 · D120 · The give-up alarm re-pages every order in the window after a restart, over a population F9 enlarged

**File:** `src/PhotoPrint.API/BackgroundJobs/AwbGiveUpRegistry.cs:21-23` · **Cause:** pre-existing, amplified by D82's fix.

`MarkOnce` is a per-process `MemoryCache` keyed on `sameday.awb.give-up::{orderId:N}` with a 32-day
sliding expiration. One-shot therefore means one-shot **per process**: every restart re-fires the
`Error` log for every order still inside `queryFloor`, and `docs/DEPLOYMENT.md` §12.8 says to page
on that line. F9 widened the qualifying set from `Paid` to `Paid || Printing`, so the re-page
population is larger than before.

A second, smaller consequence of the same keying: the `status=` value an operator reads is whichever
status the order held at the **first** alarm, so an order that later advances is not re-described.

---

## F15 · ⚪ · D116 · `DEPLOYMENT.md:949` still reasons from the availability target as if the denominator were customer traffic

**File:** `docs/DEPLOYMENT.md:949` · **Cause:** fix-caused (incomplete fix of D81).

review-v3's F8 named this line as part of the defect. The round amended `slos.md` and the panel and
left this third copy, which now contradicts both:

> Availability target ≥ 99.5% → ≤ 1/200 requests is a 5xx → ≤ 0.5% of a few hundred req/day daily

A reader sizing the Sentry budget still reasons as if the ratio were about customer requests.

---

## F16 · ⚪ · D117 · Two of the round's surfaces are unpinned, and the panel description cites an id operators cannot resolve

**Files:** `ops/dashboards/fototipar-overview.json:60`,
`src/PhotoPrint.API/BackgroundJobs/AwbRetryJob.cs:123`

Neither is measured by mutation — both were settled by reading, and both are recorded as unpinned
rather than as behaviour defects:

- The Availability panel `description` (F8's answer to "the operator reads the panel, not the doc")
  is read by nothing; deleting it returns the wall to its pre-fix state silently. It also ends
  "Tracked as D46", an identifier that exists only inside `reviews/**`.
- `status={Status}` on the give-up log is read by no test — the only give-up test counts through the
  registry, never the message — while `docs/DEPLOYMENT.md:775` now promises the field to operators.

---

## F17 · ⚪ · D118 · Comment-rule residue in this round's diff

**Files:** `src/PhotoPrint.API/Program.cs:57-58` (plus a double blank line at `:60-61`),
`src/PhotoPrint.API/BackgroundJobs/AwbRetryJob.cs:105-106`

CLAUDE.md's rule is "a last resort, kept to **one short line**". Both new comments run to two lines.
Both state a genuine non-obvious constraint, which is the one allowed reason, so this is about
length and the stray blank line, not about deleting them. Same family as D35.

---

## F18 · ⚪ · D119 · `resolution-v3.md`'s F11 note overstates the parser unification

**File:** `reviews/044-045-observability/resolution-v3.md:20`

The note says the fix gives "one brace-matching rule for both sides of the file". Three parsers
exist; `MetricNamesIn` now shares `ClosingBrace` with the exposition side, but `LabelUsagesIn` —
also query-side — keeps its own regex and still drops a matcher containing an escaped quote. A
re-reviewer trusting the note would conclude D88's escaped-quote gap is confined to the exposition
side. Recorded for the record's accuracy; `resolution-v3.md` is the fixer's file and is not edited
by this pass.

---

## Corrections to lens claims, on measurement

Recorded because a verification pass that forwards agent claims unmeasured is doing the thing this
loop exists to prevent.

1. **The tests lens claimed `IsSocketOrPipe` is behaviourally unobservable on both platforms** —
   that deleting it plus all three theory cases would stay green, because it classifies exactly what
   `BindingAddress.Parse` already treats as portless. **REFUTED by measurement (M1):** making
   `IsSocketOrPipe` return `false` reddens
   `A_socket_or_pipe_address_is_never_counted_as_a_listener(portless: "http://pipe:/metrics")`.
   `BindingAddress.Parse` gives a named-pipe address the **default port 80**, not 0, so without the
   prefix check `ports` becomes `{80, 9090}`, rule 3 does not fire and the verdict is `null`. The
   helper is load-bearing. The lens's own hedge named this possibility and dismissed it.
2. **My own prediction for M1 was wrong in the same way** — I predicted 0 red for the same reason
   the lens gave. Recorded as a prediction miss, not silently corrected: 12 of 13 predictions
   matched, this one did not.
3. **The behaviour lens filed the DI resolution of the new `TimeSpan? deadline = null` parameter as
   an untested risk** ("a resolution failure would ship green"), correctly noting no test resolves
   `GoogleTokenValidator` from a container. **Measured and settled:** a throwaway probe building the
   real `AddSocialAuth` registration and resolving `IGoogleTokenValidator` from a scope **passes**,
   so MS.DI does fill the defaulted parameter. The claim that the fix is safe is confirmed; the
   coverage gap is real but its consequence is not, so no finding is filed. The probe was deleted.
4. **The tests lens rated the SLO 4/SLO 5 exposure as "cries wolf, not goes blind" and corrected the
   resolution's frequency claim** — the re-enqueue sweep skips orders with a fresh claim, so
   "another worker holds a fresh claim" is a two-worker race, not something manufactured every
   interval. Accepted; F3 is written on the routine `Skipped` reasons instead.

## Records corrections carried into the ledger

- **D97 closes.** "Nothing exercises `StartedAsync`" is no longer true: the real-Kestrel boot test
  added in `d1ffee7` reads the addresses, asserts the throw, and pins the `Critical` line — M5 proves
  the last of those. It was recorded `backlog` and flagged to the owner in `summary-v3` as the one
  minor worth their eye; it was already closed when that was written.
- **D100 closes.** `docs/DEPLOYMENT.md:1183` now says "`ASPNETCORE_URLS` must already carry
  `http://+:9090`". Recorded `backlog`, shipped in `d1ffee7`.
- **D89 closes.** The undocumented seeding obligation is now documented — `metrics.md` step 10 states
  that the test expects every queried metric to appear in the seeded exposition.
- **D88 stands and is extended:** the two query-side parsers now disagree with each other.
- **D51 stands and is extended:** this round added a second live `TracerProvider` build plus the
  un-collectioned real-Kestrel boot (F12).
