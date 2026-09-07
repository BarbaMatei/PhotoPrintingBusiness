---
type: resolution
target: 054-dependency-hardening
version: 2
answers: review-v3.md
status: resolved
fixed_commit: 10cd81c
closed: 2026-09-07
---

# Resolution v2 — 054-dependency-hardening

Scope of this round: the two queued 🟠 rows from delta discovery v3, PPW-747 and PPW-748,
and nothing else. Both are test-coverage gaps: no production behaviour changes. The 🟡/⚪
rows (PPW-749…PPW-757, PPW-731, PPW-732, PPW-739, PPW-741) and the deferred limiter rows
(PPW-711, PPW-712) are not answered here.

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-747 | fixed | `10cd81c` | `ForwardedClientRateLimitTests.RateLimit_PartitionsPerForwardedClient` drives the production limiter through a trusted proxy at PermitLimit=3, so two forwarded clients get separate budgets. `SecurityExtensions.cs` untouched. |
| PPW-748 | fixed | `afa1418` | One test in `ForwardedHeadersWithObservabilityTests` drives `/metrics` on the scrape listener and asserts the peer `172.28.0.2` survives, so `IsMetricsScrape`'s excluded branch is asserted where the predicate lives. No production change. |

## Scope

| Cluster | Findings | Files | Protocol |
|---|---|---|---|
| A — limiter partitions per forwarded client | PPW-747 | `src/PhotoPrint.Tests/Integration/RateLimitIntegrationTests.cs`, `src/PhotoPrint.Tests/Integration/ForwardedHeadersIntegrationTests.cs` (one added helper method) | — |
| B — metrics-scrape exclusion, excluded branch | PPW-748 | `src/PhotoPrint.Tests/Integration/ForwardedHeadersIntegrationTests.cs` | — |

## Decisions

### Resolution version 2 answers review v3 (numbering)

Resolution versions count fix rounds on this target, not review versions: v1 answered
review-v1, pass v2 was a verification (no file of its own), and this is the second fix
round. `version: 2` with `answers: review-v3.md` is therefore the next free resolution
version, per the fixer contract's rule for a round that runs ahead of the newest review.

### No protocol block for these two clusters (PPW-747, PPW-748)

Both rows are 🟠 and both fix briefs name `ForwardedHeadersIntegrationTests.cs`, which is
the mechanical shape of a protocol cluster. It is not one in substance: neither fix changes
production code, so there is no state machine, no key scheme and no invariant with an owner
to order. The overlap is a shared *test helper* (`TrustedProxyFactory`'s peer-stamping
startup filter), which PPW-747 extends by one method and PPW-748 only calls. Each brief
specifies exactly one test and the round's instruction was to honour them literally, so a
composed-flow invariant test would have had to invent behaviour neither row claims. Written
here rather than as a `### Protocol —` block so the omission is a recorded judgment, not a
skipped step.

### PPW-747 deviates from the brief's `IOptionsMonitor` advice (approach-check ran)

The fix brief suggested resolving the limits through `IOptionsMonitor` inside the factory
rather than at registration, so a test could lower the permit limit. Done literally here that
means a `PostConfigure<RateLimiterOptions>` replacing the production `GlobalLimiter` with a
test copy of the partition-key logic — the very logic under test — which CLAUDE.md's "mock
only at system boundaries" rule forbids, and which would keep passing with the Program.cs
reorder in place. The test instead lowers `RateLimit:Public:PermitLimit` to 3 through
configuration: `ObservabilityFactoryBase` routes `ExtraConfig()` through `builder.UseSetting`,
which reaches `builder.Configuration` before `AddSecurityBaselines` reads the section at
`src/PhotoPrint.API/Program.cs:121`, so the production limiter runs unmodified at permit 3 and
nothing is substituted. The deviation is itself trigger-list-shaped (a limiter and a resource
budget), so an adversarial approach-check ran on it and returned `cleared`: it confirmed the
configuration reaches registration before `builder.Build()` (`src/PhotoPrint.API/Program.cs:314`),
and confirmed the test is not vacuous — `appsettings.json` ships `PermitLimit: 100`, so a
value that failed to arrive would fail the "4th request from the same client is 429" assertion
loudly instead of passing silently.

### The approach-check overran its token cap (PPW-747)

The contract caps an approach-check at ~20–30k output tokens and the dispatch prompt repeated
that cap; the check still consumed 101,089 tokens, stamped on its `check-returned` event.
Recorded rather than smoothed over: a cap stated in the prompt did not hold, so the round's
check cost roughly four times its budget for a verdict of `cleared`.

### Revert proofs

Each proof is the smallest lever that reintroduces the defect, applied to the round's tip,
run scoped, then reverted. The production tree was byte-identical again after each.

- **PPW-748** — lever: `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:84`, the
  first conjunct of `IsMetricsScrape` replaced by `false`, which forces the predicate false and
  runs the forwarded-headers branch on the scrape listener. Red line:
  `ForwardedHeadersWithObservabilityTests.The_metrics_path_on_the_scrape_port_keeps_its_peer`
  failed — `passed 4, failed 1`. The other four in the class stayed green: they all assert the
  client **is** resolved, so none can see this predicate lose its guard.

- **PPW-747** — lever: `src/PhotoPrint.API/Program.cs:375`, `app.UseTrustedProxyForwardedHeaders()`
  moved to below `app.UseSecurityBaselines()` — the one-line reorder the row names — so the
  limiter partitions on the proxy's own peer address again. Red line:
  `Expected firstFromAnotherClient to be HttpStatusCode.OK {value: 200}, but found HttpStatusCode.TooManyRequests {value: 429}.`
  — `passed 0, failed 1`. The pre-existing `RateLimitIntegrationTests` cannot see this lever:
  they send every request from a single client with no forwarded header, so one bucket for the
  whole internet is indistinguishable there from a per-client budget.

### Test runs were stamped, so `--no-events` was dropped (both findings)

The round's instruction quoted the cheap-working command line from CLAUDE.md, which carries
`--no-events`. In `reviews/lib/fix/run-scoped-tests.mjs:210` that flag suppresses the
`test-run` worklog stamp, and the records auditor's evidence gate reads those stamps — a
round run that way would hand the driver zero test evidence. Every run here therefore used
`--summary` (cheap output, the flag's actual intent) without `--no-events`, in the
foreground, one process at a time.

### Class sweep — the other conditional-branch middleware site (PPW-748)

The defect class is a conjunctive `UseWhen` predicate whose *excluded* branch no test in the
changed file asserts. The API has exactly one other `UseWhen`,
`src/PhotoPrint.API/Program.cs:430`, gating `MetricsEndpointIpAllowListMiddleware` on the
metrics path. Both of its branches are already asserted: the included one by
`MetricsEndpointIntegrationTests`, the excluded one by every integration test that reaches a
non-metrics path without a 403. No sibling gap, so only the instance was fixed.

### No owner decisions to park (unattended round)

Triage found none: both rows are in scope, both have a fix brief with an explicit
`testShape`, and neither needs a capability removal, a scope ruling or a wont-fix. Nothing
was noticed outside the finding set during triage.

### Test filters for this round — every class the fixes rely on

Round 1 recorded filters that omitted `ForwardedHeadersWithObservabilityTests`, which is why
the verification's first hand proofs read green against a real defect. The classes this round
depends on, in full:

- `PhotoPrint.Tests.Integration.ForwardedHeadersWithObservabilityTests` — PPW-748's new test
  and the four pre-existing observability tests it sits with.
- `PhotoPrint.Tests.Integration.ForwardedClientRateLimitTests` — PPW-747's new test.
- `PhotoPrint.Tests.Integration.ForwardedHeadersIntegrationTests` — the other class in the
  same file, which shares the `TrustedProxyFactory` that PPW-747 extended by one method.
- `PhotoPrint.Tests.Integration.RateLimitIntegrationTests` — the pre-existing limiter tests in
  the file PPW-747 changed.

One filter covers all four, and is what the round's final run used:
`--filter "ForwardedHeaders|FullyQualifiedName~RateLimit"` (the wrapper wraps it as
`FullyQualifiedName~ForwardedHeaders|FullyQualifiedName~RateLimit`).

That run was green: `passed 38, failed 0, skipped 0` — the 16 tests in those four classes plus
the unit tests named for `ForwardedHeaders` or `RateLimit`, which the one filter also catches.

### Test-meaning audit — pass, with one declared deviation from PPW-747's `testShape`

The audit read both tests against their briefs' words and returned `pass`. One deviation:
PPW-747's test adds a 4th same-client request asserted `429`, which the brief's shape does not
name. It is kept as a strengthening, not a substitute — that assertion cannot tell the fixed
order from the reverted one (under the reorder all five requests share the proxy's single
partition, so the 4th is `429` either way); the brief's own assertion, the 5th request from a
second client returning `200`, is the one that reddens on the reorder and is present. The extra
request proves the `PermitLimit=3` override actually bites, which the two-client comparison
alone would not catch.

The audit also recorded that PPW-748's request answers `403`: the scrape-listener factory
allow-lists only `10.42.0.5`, so `MetricsEndpointIpAllowListMiddleware` rejects the peer
`172.28.0.2`. That is upstream-irrelevant — the probe start-up filter records
`Connection.RemoteIpAddress` as the pipeline unwinds, the allow-list middleware never touches
it, and the test asserts the address only. Left as the brief specified rather than widened to
an allow-listed peer, so the test keeps asserting one thing.

### Round-scope composition review — 0 findings, 2 observations parked

The review over the round's whole diff (`afa1418`, `10cd81c`) plus these notes returned
`found: 0`: no production file changed (`git diff b7c1008..10cd81c -- src/PhotoPrint.API/` is
empty), `SendForwardedAsync` is additive and no test mixes it with `ResolveAsync` on one factory
(so its unreset `_probe.Resolved` has no reader), the new factory needs no
`ObservabilityHostCollection` because it inherits `Observability:Enabled=false`, and no name
or collection collides.

Two defects it noticed **outside** the finding set are parked — unattended round, so routing
them is the owner's ruling (`gate-parked`, default "not fixed, not filed"):

- **HSTS is PPW-747's defect class, unpinned.** `Strict-Transport-Security` is emitted only
  because `Program.cs:375` flips `IsHttps` from `X-Forwarded-Proto` before `UseHsts()`
  (`SecurityExtensions.cs:117`); no test asserts that header, so the same reorder drops it
  silently. Related to the queued docs row PPW-750.
- **`UseSentryScopeEnricher()` ordering is unpinned.** `SentryIntegrationTests.cs:39` asserts
  only `correlation_id` on an anonymous endpoint, so the `user_id` enrichment its
  after-authentication placement (`Program.cs:395`) exists for is untested.
