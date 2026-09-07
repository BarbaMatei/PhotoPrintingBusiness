---
type: review
target: 054-dependency-hardening
version: 1
supersedes: null
commit: e1febe5
branch: feat/bolt-054-dependency-hardening
pass-type: discovery
date: 2026-09-04
lenses: [correctness, security, requirements, quality, tests-coverage, completeness-critic, input-validation, observability]
lenses-not-run: [race]
verdict: request-changes
blockers: [PPW-713, PPW-714]
findings: { high: 2, medium: 11, low: 15, cleanup: 8, refuted: 0 }
tests: { dotnet: "56/56", frontend: "not run — no frontend change" }
---

# Review v1 — 054-dependency-hardening

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-713 | 🔴 | The forwarded-headers/trusted-proxy mechanism ships commented out, so it is inert in production despite the record saying it is on | `.env.example:62` | yes |
| PPW-714 | 🔴 | Production log configuration keeps the new forwarded-header lines off stdout, blinding the documented verification greps | `src/PhotoPrint.API/appsettings.json:183` | yes |
| PPW-711 | 🟠 | UseRateLimiter() runs before UseRouting(), so every [EnableRateLimiting] endpoint policy is inert | `src/PhotoPrint.API/Extensions/SecurityExtensions.cs:122` | owner decides |
| PPW-712 | 🟠 | Named auth rate-limit policies have no per-IP partition, so login/registration/password-reset share one global bucket | `src/PhotoPrint.API/Extensions/SecurityExtensions.cs:72` | owner decides |
| PPW-715 | 🟠 | Boot validator accepts an over-broad trusted-proxy range (0.0.0.0/0, ::/0, whole subnets) with no refusal or warning | `src/PhotoPrint.API/Validators/ForwardedHeadersSettingsValidator.cs:22` | yes |
| PPW-716 | 🟠 | Untrusted-peer warning infers trust from a before/after RemoteIpAddress comparison, mislabelling trusted proxies | `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:27` | yes |
| PPW-717 | 🟠 | AllowedScrapeIps examples name the stale 172.20.0.0/16 bridge subnet, which contains no container on the pinned network | `.env.example:71` | yes |
| PPW-718 | 🟠 | No build or CI gate detects vulnerable resolved packages, so the intent's "verified in CI" NFR is unenforced | `.github/workflows/ci.yml:52` | yes |
| PPW-719 | 🟠 | Intent open question Q3 still instructs Ops to trust the container bridge CIDR | `memory-bank/intents/025-security-dependency-hygiene/requirements.md:114` | yes |
| PPW-720 | 🟠 | The ScrapePort=0 + TrustedProxies boot guard is only unit-tested, never exercised by a real boot | `src/PhotoPrint.Tests/Unit/Configuration/ForwardedHeadersSettingsValidatorTests.cs:58` | yes |
| PPW-721 | 🟠 | No test pins the middleware branch order the untrusted-peer inference depends on | `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:88` | yes |
| PPW-722 | 🟠 | Neither conjunct of the metrics-scrape exclusion predicate is individually pinned by a test | `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:95` | yes |
| PPW-723 | 🟠 | Caddy's static 172.28.0.2 sits inside the dynamic IPAM pool while api starts first, risking address collision | `docker-compose.prod.yml:29` | yes |
| PPW-724 | 🟡 | Untrusted-peer warning is computed after the pipeline returns, so a downstream throw loses it | `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:35` | no |
| PPW-725 | 🟡 | The metrics scrape listener serves the whole API to any container on the compose network | `docker-compose.prod.yml:47` | no |
| PPW-726 | 🟡 | Test walkthrough credits a PR image build that would verify the Dockerfile change but does not exist | `memory-bank/bolts/054-dependency-and-boot-hardening/test-walkthrough.md:163` | no |
| PPW-727 | 🟡 | Disproven auth-audit-log claim survives in the boot warning text and in ADR-018 | `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:65` | no |
| PPW-728 | 🟡 | System-context diagram still routes the metrics scraper through Caddy to /metrics | `memory-bank/intents/025-security-dependency-hygiene/system-context.md:31` | no |
| PPW-729 | 🟡 | A forged X-Forwarded-For aimed at the scrape listener produces no distinguishable log signal | `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:84` | no |
| PPW-730 | 🟡 | Untrusted-peer warning carries no correlation id, breaking the documented triage path | `src/PhotoPrint.API/Program.cs:375` | no |
| PPW-731 | 🟡 | Once-per-process dedupe with no counter makes ongoing proxy drift look like a one-off | `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:49` | no |
| PPW-732 | 🟡 | Singleton lifetime — the basis of "warned once" — is unverified in the real pipeline | `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:26` | no |
| PPW-733 | 🟡 | An_unparseable_trusted_proxy_aborts_boot cannot fail for the reason it is credited with | `src/PhotoPrint.Tests/Integration/ForwardedHeadersIntegrationTests.cs:89` | no |
| PPW-734 | 🟡 | KnownProxies is read eagerly at registration and no booted host asserts it is populated | `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:22` | no |
| PPW-735 | 🟡 | A scalar-shaped TrustedProxies env var binds to an empty array, silently disabling the mechanism | `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:22` | no |
| PPW-736 | 🟡 | The proxy IP is hardcoded in docker-compose.prod.yml and again in .env — one address, two sources | `docker-compose.prod.yml:29` | no |
| PPW-737 | 🟡 | No test or check that EF spans still carry SQL after the OpenTelemetry 1.11→1.15 bump | `src/PhotoPrint.API/Extensions/ObservabilityExtensions.cs:75` | no |
| PPW-738 | 🟡 | system-architecture.md still asserts per-endpoint rate limits this bolt proved never run | `memory-bank/standards/system-architecture.md:52` | no |
| PPW-739 | ⚪ | Check-then-act on the 512-entry log cap lets _loggedPeers exceed the cap | `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:35` | no |
| PPW-740 | ⚪ | Bolt notes still warn about a Stripe.net 46→47 break the bolt disproved | `memory-bank/bolts/054-dependency-and-boot-hardening/bolt.md:85` | no |
| PPW-741 | ⚪ | The metrics path is re-derived in three places with divergent empty-value handling and a silent fallback | `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:108` | no |
| PPW-742 | ⚪ | Trusted-proxy list is read and validated twice, leaving a second unreachable failure path | `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:35` | no |
| PPW-743 | ⚪ | Capped once-per-peer logger copy-pasted from MetricsEndpointIpAllowListMiddleware | `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:33` | no |
| PPW-744 | ⚪ | Third hand-rolled bind of the "RateLimit" section, only to log one number | `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:72` | no |
| PPW-745 | ⚪ | Scrape-named observability type ScrapeIpAllowList is now the shared IP-list parser for proxy trust | `src/PhotoPrint.API/Observability/ScrapeIpAllowList.cs:56` | no |
| PPW-746 | ⚪ | A whole ServiceProvider is built (and never disposed) per simulated request in the middleware tests | `src/PhotoPrint.Tests/Unit/Middleware/UntrustedForwardedPeerMiddlewareTests.cs:85` | no |

## Refuted

No finding was refuted: every candidate that reached a skeptic built a trace, and none of the
53 raw lens reports collapsed on checking. Two sub-claims inside real findings did not survive
and are corrected on the ledger rows rather than carried forward — PPW-717's claim that the
stale 172.20.0.0/16 range "would also include Caddy" (the ranges are disjoint, so the copied
value fails closed with a 403 rather than opening the scrape gate), and PPW-721's claim that a
swapped branch order warns on "every proxied request" (only multi-entry forwarded headers
survive the middleware, so single-hop traffic is unaffected).

## Notes for the fixer

- **Order.** PPW-713 first: it decides how bad PPW-712, PPW-736 and the untrusted-peer findings
  actually are in production, and it is a one-line change. Then PPW-714, then the record rows
  (PPW-717, PPW-719, PPW-727, PPW-728, PPW-738, PPW-740), then the test rows.
- **PPW-711 and PPW-712 are one change or neither.** PPW-712 is harmless only while PPW-711
  keeps the named policies from running; fixing the ordering alone arms three un-partitioned
  hourly buckets. This bolt deliberately deferred PPW-711 to intent 029 / bolt 063 and says so
  in `docs/DEPLOYMENT.md` §16.7 item 3 — the owner decides whether to uphold that. If it is
  upheld, record both as `deferred` citing §16.7, and state on PPW-712 that it must land in the
  same change as PPW-711.
- **Both approach pre-checks came back `revised`**, and the revisions are already folded into the
  fix briefs on PPW-711, PPW-712 and PPW-718 — including four extra CI gaps the check found (no
  `global.json`, `ci.yml` never running on pushes to main, `renovate.json` needing
  `osvVulnerabilityAlerts`, no `npm audit` in the web job). File those as new rows rather than
  widening PPW-718 silently. Deviating from a revised approach needs its own check.
- **Coupled pairs.** PPW-716 removes the branch-order dependence PPW-721 exists to pin, so do
  PPW-716 first and let PPW-721 pin the new rule. PPW-742 must land before PPW-733 or PPW-720
  can fail for the reason they claim. PPW-715 is the code-side guard for PPW-719's record drift.
- **Test-harness traps that make a green test meaningless here.** TestServer reports
  `Connection.RemoteIpAddress` as null, so every client shares the `"unknown"` rate-limit
  partition unless an `IStartupFilter` stamps it (`ForwardedHeadersIntegrationTests.cs:123-141`).
  `AddSecurityBaselines` captures configuration at registration, before test config applies
  (`SecurityBaselineFactory.cs:67-69`), so limits read there are already inert. Set the public
  permit limit high in any named-policy test, or a reintroduced defect still returns 429.
- **Do not touch `GuestSessionExtensions.cs:22`** — it is an authorization policy, not a rate
  limiter. Only four limiter sites exist: `SecurityExtensions.cs:72` and `AuthExtensions.cs:84`,
  `:92`, `:100`.
- **PPW-737 is `plausible`, not confirmed** — the OpenTelemetry 1.15 attribute name was never
  checked against the shipped package. Confirm it before writing an assertion against it.
