---
type: review-ledger
target: 054-dependency-hardening
updated: 2026-09-04
---

# Ledger — 054-dependency-hardening

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-711 | 🟠 | v1 | UseRateLimiter() runs before UseRouting(), so every [EnableRateLimiting] endpoint policy is inert | `src/PhotoPrint.API/Extensions/SecurityExtensions.cs:122` | deferred | |
| PPW-712 | 🟠 | v1 | Named auth rate-limit policies have no per-IP partition, so login/registration/password-reset share one global bucket | `src/PhotoPrint.API/Extensions/SecurityExtensions.cs:72` | deferred | |
| PPW-713 | 🔴 | v1 | The forwarded-headers/trusted-proxy mechanism ships commented out, so it is inert in production despite the record saying it is on | `.env.example:62` | verified | `b29fb2c` |
| PPW-714 | 🔴 | v1 | Production log configuration keeps the new forwarded-header lines off stdout, blinding the documented verification greps | `src/PhotoPrint.API/appsettings.json:183` | verified | `b29fb2c`, `8ae0953` |
| PPW-715 | 🟠 | v1 | Boot validator accepts an over-broad trusted-proxy range (0.0.0.0/0, ::/0, whole subnets) with no refusal or warning | `src/PhotoPrint.API/Validators/ForwardedHeadersSettingsValidator.cs:22` | verified | `23d99d3` |
| PPW-716 | 🟠 | v1 | Untrusted-peer warning infers trust from a before/after RemoteIpAddress comparison, mislabelling trusted proxies | `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:27` | verified | `23d99d3` |
| PPW-717 | 🟠 | v1 | AllowedScrapeIps examples name the stale 172.20.0.0/16 bridge subnet, which contains no container on the pinned network | `.env.example:71` | verified | `b29fb2c`, `8ae0953` |
| PPW-718 | 🟠 | v1 | No build or CI gate detects vulnerable resolved packages, so the intent's "verified in CI" NFR is unenforced | `.github/workflows/ci.yml:52` | verified | `0c0cc3b` |
| PPW-719 | 🟠 | v1 | Intent open question Q3 still instructs Ops to trust the container bridge CIDR | `memory-bank/intents/025-security-dependency-hygiene/requirements.md:114` | verified | `bc4aa21` |
| PPW-720 | 🟠 | v1 | The ScrapePort=0 + TrustedProxies boot guard is only unit-tested, never exercised by a real boot | `src/PhotoPrint.Tests/Unit/Configuration/ForwardedHeadersSettingsValidatorTests.cs:58` | verified | `23d99d3` |
| PPW-721 | 🟠 | v1 | No test pins the middleware branch order the untrusted-peer inference depends on | `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:88` | verified | `23d99d3` |
| PPW-722 | 🟠 | v1 | Neither conjunct of the metrics-scrape exclusion predicate is individually pinned by a test | `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:95` | verified | `23d99d3` |
| PPW-723 | 🟠 | v1 | Caddy's static 172.28.0.2 sits inside the dynamic IPAM pool while api starts first, risking address collision | `docker-compose.prod.yml:29` | verified | `b29fb2c` |
| PPW-724 | 🟡 | v1 | Untrusted-peer warning is computed after the pipeline returns, so a downstream throw loses it | `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:35` | open | |
| PPW-725 | 🟡 | v1 | The metrics scrape listener serves the whole API to any container on the compose network | `docker-compose.prod.yml:47` | open | |
| PPW-726 | 🟡 | v1 | Test walkthrough credits a PR image build that would verify the Dockerfile change but does not exist | `memory-bank/bolts/054-dependency-and-boot-hardening/test-walkthrough.md:163` | open | |
| PPW-727 | 🟡 | v1 | Disproven auth-audit-log claim survives in the boot warning text and in ADR-018 | `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:65` | open | |
| PPW-728 | 🟡 | v1 | System-context diagram still routes the metrics scraper through Caddy to /metrics | `memory-bank/intents/025-security-dependency-hygiene/system-context.md:31` | open | |
| PPW-729 | 🟡 | v1 | A forged X-Forwarded-For aimed at the scrape listener produces no distinguishable log signal | `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:84` | open | |
| PPW-730 | 🟡 | v1 | Untrusted-peer warning carries no correlation id, breaking the documented triage path | `src/PhotoPrint.API/Program.cs:375` | open | |
| PPW-731 | 🟡 | v1 | Once-per-process dedupe with no counter makes ongoing proxy drift look like a one-off | `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:49` | open | |
| PPW-732 | 🟡 | v1 | Singleton lifetime — the basis of "warned once" — is unverified in the real pipeline | `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:26` | open | |
| PPW-733 | 🟡 | v1 | An_unparseable_trusted_proxy_aborts_boot cannot fail for the reason it is credited with | `src/PhotoPrint.Tests/Integration/ForwardedHeadersIntegrationTests.cs:89` | open | |
| PPW-734 | 🟡 | v1 | KnownProxies is read eagerly at registration and no booted host asserts it is populated | `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:22` | open | |
| PPW-735 | 🟡 | v1 | A scalar-shaped TrustedProxies env var binds to an empty array, silently disabling the mechanism | `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:22` | open | |
| PPW-736 | 🟡 | v1 | The proxy IP is hardcoded in docker-compose.prod.yml and again in .env — one address, two sources | `docker-compose.prod.yml:29` | open | |
| PPW-737 | 🟡 | v1 | No test or check that EF spans still carry SQL after the OpenTelemetry 1.11→1.15 bump | `src/PhotoPrint.API/Extensions/ObservabilityExtensions.cs:75` | open | |
| PPW-738 | 🟡 | v1 | system-architecture.md still asserts per-endpoint rate limits this bolt proved never run | `memory-bank/standards/system-architecture.md:52` | open | |
| PPW-739 | ⚪ | v1 | Check-then-act on the 512-entry log cap lets _loggedPeers exceed the cap | `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:35` | open | |
| PPW-740 | ⚪ | v1 | Bolt notes still warn about a Stripe.net 46→47 break the bolt disproved | `memory-bank/bolts/054-dependency-and-boot-hardening/bolt.md:85` | open | |
| PPW-741 | ⚪ | v1 | The metrics path is re-derived in three places with divergent empty-value handling and a silent fallback | `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:108` | open | |
| PPW-742 | ⚪ | v1 | Trusted-proxy list is read and validated twice, leaving a second unreachable failure path | `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:35` | open | |
| PPW-743 | ⚪ | v1 | Capped once-per-peer logger copy-pasted from MetricsEndpointIpAllowListMiddleware | `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:33` | open | |
| PPW-744 | ⚪ | v1 | Third hand-rolled bind of the "RateLimit" section, only to log one number | `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:72` | open | |
| PPW-745 | ⚪ | v1 | Scrape-named observability type ScrapeIpAllowList is now the shared IP-list parser for proxy trust | `src/PhotoPrint.API/Observability/ScrapeIpAllowList.cs:56` | open | |
| PPW-746 | ⚪ | v1 | A whole ServiceProvider is built (and never disposed) per simulated request in the middleware tests | `src/PhotoPrint.Tests/Unit/Middleware/UntrustedForwardedPeerMiddlewareTests.cs:85` | open | |
| PPW-747 | 🟠 | v3 | No test proves the rate limiter partitions per forwarded client, so a one-line reorder silently restores one bucket for the whole internet | `src/PhotoPrint.API/Program.cs:375` | open | |
| PPW-748 | 🟠 | v3 | The metrics-scrape exclusion's excluded branch is untested in the changed test file; only an out-of-scope file guards it | `src/PhotoPrint.Tests/Integration/ForwardedHeadersIntegrationTests.cs:122` | open | |
| PPW-749 | 🟡 | v3 | The 512-peer log budget never resets, so cheap in-network noise can permanently silence the proxy-drift warning | `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:112` | open | |
| PPW-750 | 🟡 | v3 | DEPLOYMENT.md §16.3 names three changed behaviours and omits the HSTS header the trusted-proxy switch now makes reachable | `docs/DEPLOYMENT.md:1702` | open | |
| PPW-751 | 🟡 | v3 | Serilog WriteTo merges by array index, so the Development overlay collides with the base Console sink's formatter at WriteTo:0 | `src/PhotoPrint.API/appsettings.Development.json:51` | open | |
| PPW-752 | 🟡 | v3 | TrustedProxyList re-parses the trusted-proxy list and discards parse errors, so the validator's caps do not guard the type that decides trust | `src/PhotoPrint.API/Configuration/TrustedProxyList.cs:12` | open | |
| PPW-753 | 🟡 | v3 | Log assertions capture around Serilog, so no test executes the production logging configuration this round rewrote | `src/PhotoPrint.Tests/Integration/ForwardedHeadersIntegrationTests.cs:244` | open | |
| PPW-754 | 🟡 | v3 | The new production rolling file sink writes into the container's ephemeral layer — no volume backs /app/logs | `src/PhotoPrint.API/appsettings.Production.json:13` | open | |
| PPW-755 | 🟡 | v3 | The new production File sink can be dropped or fail to open with no diagnostic, because SelfLog is enabled nowhere and the package is transitive | `src/PhotoPrint.API/appsettings.Production.json:11` | open | |
| PPW-756 | 🟡 | v3 | A null RemoteIpAddress returns before judging, on the one transport where ASP.NET honours X-Forwarded-For with no peer check | `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:36` | open | |
| PPW-757 | 🟡 | v3 | The NuGet audit gate is asserted as a command string and never executed, and the shipping image restores without it | `src/PhotoPrint.Tests/Unit/Configuration/DeploymentDefaultsTests.cs:121` | open | |

## Details

### PPW-711 — UseRateLimiter() runs before UseRouting(), so every [EnableRateLimiting] endpoint policy is inert

- **What:** `UseSecurityBaselines()` calls `app.UseRateLimiter()` twelve pipeline lines before `app.UseRouting()`, so no endpoint is resolved when the limiter runs and every `[EnableRateLimiting]` attribute is dead metadata. Login, register, resend-confirmation and forgot-password have only the global 100/min budget.
- **Evidence:** `SecurityExtensions.cs:122` invoked from `Program.cs:381`; `UseRouting()` at `Program.cs:393`; the attribute sits on `AuthController.cs:45`. Eleven POST `/api/auth/login` from one IP inside the window return 401, never 429. The bolt itself documents this defect as a deferral in `docs/DEPLOYMENT.md:1758-1768` (§16.7 item 3).
- **Suggested fix:** Move `app.UseRateLimiter()` out of `UseSecurityBaselines()` into `Program.cs` between `UseRouting()` (393) and `UseAuthentication()` (394).
  - **Fix brief:** `src/PhotoPrint.API/Extensions/SecurityExtensions.cs:122` · `src/PhotoPrint.API/Program.cs:380-381,390,393-394,456` · `src/PhotoPrint.API/Controllers/AuthController.cs:45`. Leave `UseStaticFiles` (390) before routing — `MapFallbackToFile` (456) matches everything and StaticFileMiddleware skips a request with a matched endpoint. Do not add a second `UseRateLimiter()` for pre-routing coverage: each instance takes its own `GlobalLimiter` lease and halves the effective 100/min. Fix in one change with PPW-712, which is latent only while this stands.
  - **testShape:** `Login_Enforces_AuthPolicy_At_11th_Attempt` — WebApplicationFactory with `RateLimit:Public:PermitLimit` high and the `auth` limit 10; POST `/api/auth/login` eleven times with bad credentials; assert response 11 is 429 carrying `Retry-After`. The high public limit is required, or a reintroduced ordering defect still yields 429 from the global bucket and the test cannot fail.
  - **Trigger-list-shaped:** yes (middleware-ordering change in the shared request pipeline, affecting every endpoint)
- **History:** <append-only, one line per event>
  - v1: found by correctness (convergence 1, not hinted), verdict confirmed by trace.
  - v1: severity re-judged high→medium — the bolt discloses this defect and defers it to intent 029 / bolt 063 in DEPLOYMENT.md §16.7 item 3; prior decision attached, the find is not suppressed.
  - v1: Approach pre-check: revised (put the call between `UseRouting()` and `UseAuthentication()`, keep `UseStaticFiles` ahead of routing, never register a second limiter, and set the public limit high in the test).
  - v1: fix round — deferred

### PPW-712 — Named auth rate-limit policies have no per-IP partition, so login/registration/password-reset share one global bucket

- **What:** `AddFixedWindowLimiter(policyName, …)` partitions on the policy name, so each named policy is one un-partitioned bucket shared by every caller on earth. Three POST `/api/auth/forgot-password` (3/hour) or five `/register` (5/hour) from one attacker lock the endpoint for all other users for the rest of the hour.
- **Evidence:** `SecurityExtensions.cs:72` and `AuthExtensions.cs:84,92,100`; reproduced on net8.0 with `PermitLimit=1` — request from 10.0.0.1 got 200, the next from 10.0.0.2 got 429. Only `GlobalLimiter` (`SecurityExtensions.cs:59`) partitions by IP; the "per IP" comment at line 70 is false. Not covered by DEPLOYMENT.md §16.7, which discloses only the ordering defect.
- **Suggested fix:** Replace each `AddFixedWindowLimiter(name, …)` with `AddPolicy(name, ctx => RateLimitPartition.GetFixedWindowLimiter(<key>, …))` and correct the comment.
  - **Fix brief:** `src/PhotoPrint.API/Extensions/SecurityExtensions.cs:59,70-72,95-96` · `src/PhotoPrint.API/Extensions/AuthExtensions.cs:84,92,100` · `src/PhotoPrint.API/Configuration/RateLimitSettings.cs`. Only these four limiter sites exist; `GuestSessionExtensions.cs:22` is an authorization policy, not a limiter. Resolve limits inside the `AddPolicy` factory via `IOptionsMonitor`, not at registration time — `SecurityBaselineFactory.cs:67-69` records that registration captures config before test config applies. Key forgot-password and resend on normalised email as well as IP, and bucket IPv6 by /64, because with `TrustedProxies` empty (PPW-713) the IP key is the proxy's own address. `OnRejected`'s fallback `Retry-After` writes `WindowSeconds` (60), wrong for the hourly policies.
  - **testShape:** `ForgotPassword_LimitIsPerIp` — WebApplicationFactory with `RateLimit:Public:PermitLimit` high and the forgot-password limit 3; POST `/api/auth/forgot-password` three times as peer A, then once as peer B; assert B is not 429. Vary the peer with an `IStartupFilter` stamping `ctx.Connection.RemoteIpAddress` (pattern at `ForwardedHeadersIntegrationTests.cs:123-141`) — TestServer reports it null, so without that every client shares the `"unknown"` partition and the test passes with the defect intact.
  - **Trigger-list-shaped:** yes (rate-limiter partitioning change on the shared auth surface)
- **History:** <append-only, one line per event>
  - v1: found by security (convergence 1, not hinted), verdict confirmed by trace with a standalone net8.0 reproduction.
  - v1: severity re-judged high→medium — the policies never execute today because of PPW-711, so the impact is latent; it goes live the moment the ordering is fixed, which is why the two must ship together.
  - v1: Approach pre-check: revised (fixing this alone, with `TrustedProxies` empty, converts three inert hourly caps into a site-wide budget — 5 registrations/hour for the whole internet behind the proxy; hence the email co-key and the PPW-713 dependency).
  - v1: fix round — deferred
  - v3 delta: re-raised by security + correctness (convergence 2) at high severity, arguing the round’s per-client identity and `PermitLimit` 600 make the shared auth buckets live. Prior decision, carried verbatim: "parked: must land in the same change as PPW-711, or three hourly caps become site-wide budgets. Default taken: §16.7’s deferral stands." The escalation premise fails — `UseRateLimiter()` (`SecurityExtensions.cs:122`, via `Program.cs:380`) still runs before `UseRouting()` (`Program.cs:393`), so the named policies remain inert; severity stays 🟠, the deferral stands, still paired with PPW-711.

### PPW-713 — The forwarded-headers/trusted-proxy mechanism ships commented out, so it is inert in production despite the record saying it is on

- **What:** `.env.example` ships `ForwardedHeaders__TrustedProxies__0` commented out, so a deploy following the runbook boots with an empty trusted-proxy list and the whole story-004 mechanism unregistered. `RemoteIpAddress` stays Caddy's 172.28.0.2 for every request: the global rate limiter puts all visitors in one partition, so one page load can 429 everyone, and the 30-day refresh cookie ships without `Secure` because `Request.IsHttps` is false behind the proxy.
- **Evidence:** `.env.example:62` (`# ForwardedHeaders__TrustedProxies__0=172.28.0.2`); the empty-list early return at `ForwardedHeadersExtensions.cs:57-66`; the partition key at `SecurityExtensions.cs:61`; `Secure = response.HttpContext?.Request?.IsHttps ?? true` at `AuthService.cs:354` and `SocialAuthService.cs:122`. The bolt's own record claims the opposite — `implementation-walkthrough.md:93-94`, "Off by default, but shipped switched on. … `.env.example` carries the real value so a fresh deploy is correct."
- **Suggested fix:** Uncomment the line so the shipped default matches the pinned Caddy address, and state the required `RateLimit` permit alongside it.
  - **Fix brief:** `.env.example:62` · `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:57-66` · `src/PhotoPrint.API/Extensions/SecurityExtensions.cs:61` · `src/PhotoPrint.API/Services/AuthService.cs:354` · `src/PhotoPrint.API/Services/SocialAuthService.cs:122` · `memory-bank/bolts/054-dependency-and-boot-hardening/implementation-walkthrough.md:93-94` · `docs/DEPLOYMENT.md` §16. Either uncomment the value or correct every record that says it ships on — the two must agree. The cookie leg is independent of the rate-limit leg: it is worth asserting `Secure` explicitly rather than deriving it from `Request.IsHttps` in production.
  - **testShape:** `EnvExample_ShipsTrustedProxyMatchingCompose` — read `.env.example` and `docker-compose.prod.yml`; assert an uncommented `ForwardedHeaders__TrustedProxies__0` exists and equals caddy's `ipv4_address`. Reverting the fix re-comments the line and reddens it.
  - Not trigger-list-shaped (a one-line deployment-default correction plus record alignment, no protocol or shared-surface change)
- **History:** <append-only, one line per event>
  - v1: found by correctness, security, requirements and completeness-critic (convergence 4, not hinted), verdict confirmed on agreement; the four consequences were rechecked against the code by the synthesiser rather than taken from the lenses.
  - v1: fix round — fixed at `b29fb2c`
  - v2: verification — held

### PPW-714 — Production log configuration keeps the new forwarded-header lines off stdout, blinding the documented verification greps

- **What:** Production Serilog has only a `File` sink and `UseSerilog(ReadFrom.Configuration)` replaces the logging providers, so no application log reaches stdout. `docker compose logs api | grep forwarded_headers.untrusted_peer` — the verification this bolt documents in DEPLOYMENT.md §16.6 — always prints nothing, which an operator reads as "no peer ignored" while the proxy address has in fact drifted. `/app/logs` has no volume, so the file copy dies on every redeploy.
- **Evidence:** `appsettings.json:175-193` — `WriteTo` holds a single `File` sink (`logs/log-.json`); the `Console` sink exists only in `appsettings.Development.json:51-57`. `SerilogExtensions.cs:9` uses the configure-delegate overload, whose `writeToProviders` defaults to false. `docker-compose.prod.yml:31-46` mounts only `apidata:/app/Storage`. The greps live at `docs/DEPLOYMENT.md:1746`.
- **Suggested fix:** Add a `Console` sink (CompactJsonFormatter) to `appsettings.json`'s `WriteTo` alongside `File`, or mount `/app/logs` in `docker-compose.prod.yml` and rewrite §16.6 to read that file instead.
  - **Fix brief:** `src/PhotoPrint.API/appsettings.json:175-193` · `src/PhotoPrint.API/Extensions/SerilogExtensions.cs:9` · `docker-compose.prod.yml:31-46` · `docs/DEPLOYMENT.md:1746` · `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:54`. The console sink is the smaller change and makes every other documented `docker compose logs` step true as well; if the file route is chosen instead, every `docker compose logs` instruction in DEPLOYMENT.md needs the same correction.
  - **testShape:** `ProductionSerilogConfig_HasConsoleSink` — load `appsettings.json` alone (no Development overlay); assert `Serilog:WriteTo` contains a sink named `Console`. Reverting the fix leaves the file `File`-only and reddens it.
  - Not trigger-list-shaped (a logging-configuration addition; it changes no request path and no shared contract)
- **History:** <append-only, one line per event>
  - v1: found by observability (convergence 1, not hinted), verdict confirmed by trace; kept at 🔴 because the mechanism this bolt shipped has no working verification path in production and the log record of a security control does not survive a redeploy.
  - v1: fix round — fixed at `b29fb2c`, `8ae0953`
  - v2: verification — held

### PPW-715 — Boot validator accepts an over-broad trusted-proxy range (0.0.0.0/0, ::/0, whole subnets) with no refusal or warning

- **What:** The validator checks only that each entry parses, so `172.28.0.0/24` — or `0.0.0.0/0` — boots happily. Any other container on the compose network, and with `/0` any client reaching port 8080 directly, can then send `X-Forwarded-For` naming a victim: it escapes its own rate-limit partition and exhausts the victim's, and the address reaching the application is attacker-chosen.
- **Evidence:** `ForwardedHeadersSettingsValidator.cs:18-22` validates parse-ability and the scrape-listener rule and nothing else; the parsed networks flow into `ForwardedHeadersOptions.KnownNetworks` via `ForwardedHeadersExtensions.cs:45`. `docs/DEPLOYMENT.md:1665` (§16.2) forbids the subnet form in prose only, and the bolt's own Key Decision ("Trust the proxy's address, not the container subnet") states the threat.
- **Suggested fix:** Fail validation when an IPv4 entry's prefix is shorter than /31 (IPv6 /127), with a message pointing at the single pinned proxy address; at minimum reject `/0`.
  - **Fix brief:** `src/PhotoPrint.API/Validators/ForwardedHeadersSettingsValidator.cs:18-22` · `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:45` · `src/PhotoPrint.API/Observability/ScrapeIpAllowList.cs` (the shared parser exposing `Networks`) · `src/PhotoPrint.Tests/Unit/Configuration/ForwardedHeadersSettingsValidatorTests.cs`. Note the shared parser also serves the metrics allow-list, where CIDR ranges are legitimate — the width rule belongs in the forwarded-headers validator, not in the parser.
  - **testShape:** `ForwardedHeadersSettingsValidator` — arrange `TrustedProxies = ["172.28.0.0/24"]`, act `Validate`, assert `Fail` and that the message names `ForwardedHeaders:TrustedProxies`; a second case with `["0.0.0.0/0"]` asserts `Fail`; a third with `["172.28.0.2"]` asserts `Success`. Today the first two return `Success`, so the cases redden on revert.
  - Not trigger-list-shaped (adds one boot-validation rule; no shared protocol, no pipeline ordering)
- **History:** <append-only, one line per event>
  - v1: found by correctness, security, requirements and input-validation (convergence 4, not hinted), verdict confirmed on agreement.
  - v1: fix round — fixed at `23d99d3`, `8ae0953`
  - v2: verification — held

### PPW-716 — Untrusted-peer warning infers trust from a before/after RemoteIpAddress comparison, mislabelling trusted proxies

- **What:** The middleware decides a peer is untrusted by observing that `RemoteIpAddress` did not change across `UseForwardedHeaders`. A *trusted* proxy sending an unparseable value (`X-Forwarded-For: unknown`, or an empty CDN header) leaves the address alone, so the warning fires and asserts the peer's "address has drifted from the configured one" — a false accusation, while the real cause (ForwardedHeadersMiddleware's Debug line) is suppressed by the `Microsoft.AspNetCore: Warning` override. Neither false-positive branch is tested.
- **Evidence:** `UntrustedForwardedPeerMiddleware.cs:22-27` (declared-header test, then the unchanged-address inference); branch order fixed at `ForwardedHeadersExtensions.cs:88-89`; the log-level override in `appsettings.json`. The unit tests hand-compose the two middlewares (`UntrustedForwardedPeerMiddlewareTests.cs:78-85`) and cover only the true-positive path.
- **Suggested fix:** Compare the peer against the parsed trusted-proxy list directly instead of inferring from the address change, and emit a distinct warning when a *trusted* peer's forwarded value fails to parse.
  - **Fix brief:** `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:22-35` · `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:88-89` · `src/PhotoPrint.API/Observability/ScrapeIpAllowList.cs` (the same parsed `Addresses`/`Networks` the options already consume) · `src/PhotoPrint.Tests/Unit/Middleware/UntrustedForwardedPeerMiddlewareTests.cs`. Deciding from the list removes the dependency on branch order that PPW-721 records, so fixing this narrows that finding rather than colliding with it.
  - **testShape:** Two unit cases. `Trusted_peer_with_unparseable_forwarded_value_is_not_reported_untrusted` — peer 172.28.0.2 in `TrustedProxies`, header `X-Forwarded-For: unknown`; assert zero `forwarded_headers.untrusted_peer` warnings and one distinct parse-failure warning. `Untrusted_peer_is_reported` — peer 10.9.9.9, valid header; assert exactly one `untrusted_peer` warning naming 10.9.9.9. The first case passes today only by accident of ordering and reddens on revert.
  - Not trigger-list-shaped (a self-contained change to one middleware's decision rule)
- **History:** <append-only, one line per event>
  - v1: found by correctness, input-validation, observability, tests-coverage and completeness-critic (convergence 5, not hinted), verdict confirmed on agreement — the highest convergence of the pass.
  - v1: fix round — fixed at `23d99d3`
  - v2: verification — held

### PPW-717 — AllowedScrapeIps examples name the stale 172.20.0.0/16 bridge subnet, which contains no container on the pinned network

- **What:** This bolt pins the production compose network to 172.28.0.0/24, but the scrape allow-list examples still offer 172.20.0.0/16 as "the Compose network". An operator who copies either one allow-lists a range no container sits in: Prometheus gets 403 and the dashboards stay dark, with no boot-time hint that the allow-list can never match.
- **Evidence:** `.env.example:71` (`# Observability__Metrics__AllowedScrapeIps__1=172.20.0.0/16`) against the pinned subnet at `docker-compose.prod.yml:69`; the same stale range at `docs/DEPLOYMENT.md:1103` and `:1123` (§14.5). 172.20.0.0/16 also *contains* 172.28.0.2 is false — the two ranges are disjoint — so the copied value fails closed rather than opening the gate.
- **Suggested fix:** Reconcile every `AllowedScrapeIps` example to 172.28.0.0/24 (or to the scraper's own /32) in `.env.example` and DEPLOYMENT.md §14.5.
  - **Fix brief:** `.env.example:71` · `docs/DEPLOYMENT.md:1103,1123` · `docker-compose.prod.yml:69`. The bolt pinned the subnet, so these examples are drift it introduced; fix them in the same style as the trusted-proxy example so the two sections agree on one network.
  - **testShape:** `EnvExampleScrapeIpsMatchComposeSubnet` — parse `.env.example`'s `AllowedScrapeIps` examples and `docker-compose.prod.yml`'s ipam subnet; assert every CIDR example is contained in the declared subnet. Reverting the fix restores 172.20.0.0/16 and reddens it.
  - Not trigger-list-shaped (documentation and example-value alignment)
- **History:** <append-only, one line per event>
  - v1: found by correctness, security and requirements (convergence 3, not hinted), verdict confirmed on agreement; the finders' claim that the stale range "would also include Caddy" is wrong and is corrected in the Evidence line above — the ranges are disjoint, which is why the failure is a silent 403 rather than an opened gate.
  - v1: fix round — fixed at `b29fb2c`, `8ae0953`
  - v2: verification — held

### PPW-718 — No build or CI gate detects vulnerable resolved packages, so the intent's "verified in CI" NFR is unenforced

- **What:** The intent states that a clean `dotnet list package --vulnerable` scan is "Verified in CI". No CI step runs one, restore-time NuGet audit warnings are not errors, and on the .NET 8 SDK audit defaults to direct packages only — so it cannot see the three transitive pins this bolt added. The next advisory against any pinned package reaches main with no signal anywhere.
- **Evidence:** `.github/workflows/ci.yml:52` (restore step, no audit gate, no `-warnaserror`); `Directory.Packages.props:3-12` sets transitive pinning and promotes only NU1603; `memory-bank/intents/025-security-dependency-hygiene/requirements.md:74` carries the NFR; `.github/renovate.json:38-43` depends on GitHub Dependabot alerts being enabled, which Q1 records as still Pending.
- **Suggested fix:** Enforce the NFR at restore rather than with a `dotnet list` grep, or downgrade the NFR text to what actually runs.
  - **Fix brief:** `Directory.Packages.props:3-12,11,63-71` · `.github/workflows/ci.yml:6-9,31-33,52,102-106` · `.github/deploy.yml:7-10` · `.github/renovate.json:38-43` · `memory-bank/intents/025-security-dependency-hygiene/requirements.md:74`. Add `NuGetAuditMode=all` and `NuGetAuditLevel=low`, and append `NU1901;NU1902;NU1903;NU1904;NU1905` to the existing `WarningsAsErrors` at line 11 (NU1603 is the precedent) — `TreatWarningsAsErrors` is wrong: it is not honoured for restore warnings and promotes every C# warning. NU1905 matters because an audit source with no data otherwise yields a green build. Hard-fail on the CI restore only (`-warnaserror:` on that command), leaving the repo props at warning so a new CVE cannot block the Docker publish (`Dockerfile:15,17`) or a hotfix deploy. Drop `dotnet list package --vulnerable` as the gate: it exits 0 on findings and prints nothing when the source has no data. `--format json` does not exist before the .NET 9 SDK.
  - **testShape:** No unit test can assert a CI gate. The mechanical proof is a scratch branch pinning a package with a known advisory and observing the CI restore fail, recorded in the resolution; plus a config assertion `DirectoryPackagesProps_PromotesAuditWarningsToErrors` reading `Directory.Packages.props` and asserting `NuGetAuditMode` is `all` and `WarningsAsErrors` contains NU1901–NU1905.
  - **Trigger-list-shaped:** yes (CI gate and dependency-cadence change; it can break every build in the repo)
- **History:** <append-only, one line per event>
  - v1: found by security, requirements and completeness-critic (convergence 3, not hinted), verdict confirmed on agreement.
  - v1: Approach pre-check: revised (the premise was half wrong — SDK 8 already audits direct packages at restore; what is missing is `NuGetAuditMode=all` plus warning promotion, and the proposed `dotnet list --vulnerable` gate fails open exactly when it matters). Also named four gaps to record separately: no `global.json`, so the SDK — and therefore the audit defaults — drifts with the runner image; `ci.yml:6-9` never runs on pushes to main, which makes `deploy.yml:7-10`'s `workflow_run` gate dead and leaves main unaudited; `renovate.json` needs `osvVulnerabilityAlerts`; and the web job has no `npm audit`.
  - v1: fix round — fixed at `0c0cc3b`
  - v2: verification — held

### PPW-719 — Intent open question Q3 still instructs Ops to trust the container bridge CIDR

- **What:** Q3's recorded resolution ("Derive from docker-compose.prod.yml bridge network") and story 004's technical note survive beside the NFR that forbids exactly that. An operator following the intent sets `TrustedProxies=172.28.0.0/24`; the validator only checks parsing, so boot succeeds and any co-network container can forge `X-Forwarded-For`.
- **Evidence:** `memory-bank/intents/025-security-dependency-hygiene/requirements.md:114` (Q3) echoing `units/001-dependency-and-boot-hardening/stories/004-forwarded-headers-metrics.md:40` ("anchor to the actual docker-compose.prod.yml bridge CIDR"), against the NFRs at `requirements.md:61` and `:75` and `docs/DEPLOYMENT.md:1665`. Mitigating: DEPLOYMENT.md §16.2 and the compose comment prohibit it explicitly, so the damage is record drift, not shipped code.
- **Suggested fix:** Record Q3's real resolution (Caddy's /32, 172.28.0.2) and rewrite story 004's technical note to match §16.2 and the NFR.
  - **Fix brief:** `memory-bank/intents/025-security-dependency-hygiene/requirements.md:114` · `memory-bank/intents/025-security-dependency-hygiene/units/001-dependency-and-boot-hardening/stories/004-forwarded-headers-metrics.md:40` · `docs/DEPLOYMENT.md:1665`. The code-side guard for the same mistake is PPW-715; fixing that one makes this record drift non-exploitable, and the two belong in one change.
  - **testShape:** No test reddens on stale prose. The enforceable half is PPW-715's validator case (`TrustedProxies=["172.28.0.0/24"]` → `Fail`); this row's proof is the corrected text plus that test.
  - Not trigger-list-shaped (record correction in the intent and story files)
- **History:** <append-only, one line per event>
  - v1: found by requirements (convergence 1, not hinted), verdict confirmed by trace.
  - v1: fix round — fixed at `bc4aa21`
  - v2: verification — held

### PPW-720 — The ScrapePort=0 + TrustedProxies boot guard is only unit-tested, never exercised by a real boot

- **What:** Delete the `AddSingleton<IValidateOptions<ForwardedHeadersSettings>>` registration and all 28 new tests stay green, because the validator tests construct the validator directly. Production then boots with Observability on, `ScrapePort=0` and trusted proxies set — the one combination where the scrape exclusion cannot fire — so `/metrics` is served on the proxied listener with forwarded headers applied, and a forged `X-Forwarded-For` naming an allow-listed scraper returns the whole metric store.
- **Evidence:** `ForwardedHeadersSettingsValidatorTests.cs:108` constructs the validator; the registration under test is `ForwardedHeadersExtensions.cs:16-17`; the only boot-abort integration test (`ForwardedHeadersIntegrationTests.cs:86`) still aborts through the redundant throw in the `Configure` lambda (`ForwardedHeadersExtensions.cs:36`), so it cannot see the validator's absence. `ScrapePort=0` makes `IsMetricsScrape` always false, sending `/metrics` through `UseForwardedHeaders` (`Program.cs:375`) before the allow-list at `MetricsEndpointIpAllowListMiddleware.cs:40`.
- **Suggested fix:** Add a booted-host test for the guard, following the pattern of `An_unparseable_allow_list_entry_aborts_boot`.
  - **Fix brief:** `src/PhotoPrint.Tests/Integration/ForwardedHeadersIntegrationTests.cs:86,150` · `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:16-17,36` · `src/PhotoPrint.Tests/Integration/MetricsEndpointIntegrationTests.cs:56`. Removing the redundant throw (PPW-742) is what makes this test able to fail for the right reason, so the two are one change.
  - **testShape:** `Trusted_proxies_with_observability_on_and_no_scrape_listener_aborts_boot` — factory configured with `Observability:Enabled=true`, `Observability:Metrics:ScrapePort=0`, `ForwardedHeaders:TrustedProxies:0=172.28.0.2`; act `CreateClient()`; assert it throws `OptionsValidationException` whose message contains `Observability:Metrics:ScrapePort`. Deleting the validator registration must redden it.
  - Not trigger-list-shaped (adds one integration test; no production code change beyond PPW-742's cleanup)
- **History:** <append-only, one line per event>
  - v1: found by tests-coverage (convergence 1, not hinted), verdict confirmed by trace — the mutant was run and stayed green.
  - v1: fix round — fixed at `23d99d3`, `8ae0953`
  - v2: verification — held

### PPW-721 — No test pins the middleware branch order the untrusted-peer inference depends on

- **What:** Swap the two registration lines so `UseForwardedHeaders` runs before the untrusted-peer middleware and every scoped test stays green. Production then warns `forwarded_headers.untrusted_peer` naming the *real client* on any multi-entry `X-Forwarded-For`, up to 512 distinct addresses, and genuine proxy drift becomes indistinguishable from normal traffic.
- **Evidence:** `ForwardedHeadersExtensions.cs:88-89`; the unit test composes its own ordering (`_sut.InvokeAsync(context, forwardedHeaders.Invoke)`, `UntrustedForwardedPeerMiddlewareTests.cs:78`) and no booted-host test asserts any log line. The swap was run: all 35 scoped tests stayed green. With a two-entry header from a trusted peer, ForwardedHeaders consumes only the rightmost entry and leaves the header in place, so the downstream middleware sees a declared header, an unchanged peer, and warns naming the client. Single-entry headers are removed, so "every proxied request" overstates it.
- **Suggested fix:** Add a booted-host log assertion using the existing `LogCaptureProvider`.
  - **Fix brief:** `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:88-89` · `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:22,27` · `src/PhotoPrint.Tests/Integration/ForwardedHeadersIntegrationTests.cs:35` · `src/PhotoPrint.Tests/Unit/Middleware/UntrustedForwardedPeerMiddlewareTests.cs:78`. PPW-716's list-based decision removes the ordering dependency altogether; if that lands first, this test pins the new rule instead of the order.
  - **testShape:** `Trusted_proxy_with_multi_entry_forwarded_for_emits_no_untrusted_warning` — booted host with `LogCapture`, trusted peer 172.28.0.2 sends `X-Forwarded-For: 198.51.100.1, 203.0.113.9`; assert the resolved client identity is 203.0.113.9 **and** zero `forwarded_headers.untrusted_peer` warnings. A companion case: untrusted peer 10.9.9.9 emits exactly one. Swapping lines 88/89 must redden the first.
  - Not trigger-list-shaped (test-only addition)
- **History:** <append-only, one line per event>
  - v1: found by tests-coverage (convergence 1, not hinted), verdict confirmed by trace; the trace corrected the finder's "every proxied request" to multi-entry headers only.
  - v1: fix round — fixed at `23d99d3`
  - v2: verification — held

### PPW-722 — Neither conjunct of the metrics-scrape exclusion predicate is individually pinned by a test

- **What:** `IsMetricsScrape` is a two-conjunct predicate (scrape port **and** metrics path) that the bolt's own Key Decisions call load-bearing in both directions. Deleting either conjunct leaves all 50 related tests green. Without the path conjunct, on a host where the scrape port equals the public port every request skips `UseForwardedHeaders` while the boot log still says the feature is on, and rate limits collapse onto the proxy IP.
- **Evidence:** `ForwardedHeadersExtensions.cs:95`; both mutants were run and all 50 ForwardedHeaders + MetricsEndpoint tests stayed green (file restored). No test sends a non-`/metrics` path on the scrape port, nor `/metrics` on the public port with proxies trusted. `ScrapeListenerGuard.cs:56` rejects only the case where the scrape port is the sole bound port, so two ports with `ScrapePort` equal to the proxied one boots fine.
- **Suggested fix:** Add the two missing cases, one per conjunct, and extend the mutation list so each conjunct is covered separately.
  - **Fix brief:** `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:95` · `src/PhotoPrint.Tests/Integration/ForwardedHeadersIntegrationTests.cs:103` · `src/PhotoPrint.Tests/Integration/MetricsEndpointIntegrationTests.cs:89,297` · `src/PhotoPrint.API/Observability/ScrapeListenerGuard.cs:56`. Both cases need the existing `LocalPort`-stamping start-up filter pattern.
  - **testShape:** Path conjunct — scrape port 9090, request `LocalPort` 9090, GET `/__probe/client-identity` from trusted peer 172.28.0.2 with `X-Forwarded-For: 203.0.113.9`; assert the resolved client is 203.0.113.9 (dropping the path conjunct makes it 172.28.0.2 and reddens). Port conjunct — same trusted peer, `LocalPort` 8080, GET `/metrics`; assert the forwarded value is honoured (dropping the port conjunct reddens it).
  - Not trigger-list-shaped (test-only addition)
- **History:** <append-only, one line per event>
  - v1: found by tests-coverage and completeness-critic (convergence 2, not hinted), verdict confirmed by trace with both mutants executed.
  - v1: fix round — fixed at `23d99d3`
  - v2: verification — held

### PPW-723 — Caddy's static 172.28.0.2 sits inside the dynamic IPAM pool while api starts first, risking address collision

- **What:** The compose file pins caddy to 172.28.0.2 inside the 172.28.0.0/24 subnet without reserving that address from the dynamic pool, and `caddy depends_on api`. On a fresh host `docker compose up -d` therefore starts api first, Docker hands it the first free address — 172.28.0.2 — and caddy then fails to start with "Address already in use": no reverse proxy, no TLS, site down.
- **Evidence:** `docker-compose.prod.yml:13` (`depends_on`), `:29` (`ipv4_address: 172.28.0.2`), `:69` (the ipam subnet with no `ip_range`). Nothing in the bolt's records shows the compose file ever being brought up; the pinned address is also the one `.env.example:62` is meant to trust (PPW-713), so a collision silently changes which address is correct.
- **Suggested fix:** Reserve the static address outside the dynamic pool — add `ip_range: 172.28.0.128/25` beside the subnet — or give api a static address too, then verify with one real `docker compose up -d`.
  - **Fix brief:** `docker-compose.prod.yml:13,29,69` · `.env.example:62` · `docs/DEPLOYMENT.md:1671`. `ip_range` is the smaller change and keeps the /32 trust model the bolt chose. Whichever is picked, the trusted-proxy value and §16 must still name the same address.
  - **testShape:** `ProdComposeStaticProxyAddressIsOutsideDynamicPool` — parse `docker-compose.prod.yml`; assert `networks.default.ipam` declares an `ip_range` and that caddy's `ipv4_address` falls outside it. Reddens today, since no `ip_range` exists.
  - Not trigger-list-shaped (a compose-file network declaration, verifiable by parsing the file)
- **History:** <append-only, one line per event>
  - v1: found by completeness-critic (convergence 1, not hinted), verdict confirmed by trace.
  - v1: fix round — fixed at `b29fb2c`
  - v2: verification — held

### PPW-724 — Untrusted-peer warning is computed after the pipeline returns, so a downstream throw loses it

- **What:** `LogUntrusted` runs only after `await next(context)` returns. If anything downstream throws past the exception handler — a client-abort `ConnectionResetException` surfacing from a response write — the warning for that request is never emitted. During a proxy-address drift where most requests fail, the one diagnostic operators are told to grep for can stay silent.
- **Evidence:** `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:35`; the documented grep is `docs/DEPLOYMENT.md` §16.6 step 3.
- **Suggested fix:** Capture the verdict before `next()` and log it in a `finally`, so the post-check runs on the exception path too.
- **History:** <append-only, one line per event>
  - v1: found by correctness and observability (convergence 2, not hinted), verdict confirmed.

### PPW-725 — The metrics scrape listener serves the whole API to any container on the compose network

- **What:** `ASPNETCORE_URLS` binds `http://+:9090` for the whole application and only the `/metrics` path is gated by the IP allow-list. The Prometheus container the runbook tells operators to add to this network — or the commented-in db service — can POST `/api/auth/login` and every other endpoint straight to `api:9090`, bypassing Caddy's header stripping and edge refusals.
- **Evidence:** `docker-compose.prod.yml:47`; the path-scoped gate is the `UseWhen` in `Program.cs:82-84`; the runbook's scraper instructions are `docs/DEPLOYMENT.md` §14.4.
- **Suggested fix:** On the scrape listener short-circuit every path except the metrics path with 404 (a `UseWhen` keyed on `LocalPort == ScrapePort`), so 9090 exposes only the exposition endpoint.
- **History:** <append-only, one line per event>
  - v1: found by security (convergence 1, not hinted), verdict confirmed; pre-existing exposure this bolt did not create, but it is now the listener the new exclusion predicate depends on.

### PPW-726 — Test walkthrough credits a PR image build that would verify the Dockerfile change but does not exist

- **What:** Gap #1 says "CI's image build on the PR is the real check" for the new `COPY Directory.Packages.props`. `ci.yml` has no docker build; `deploy.yml` builds only after a push to main. A wrong path in that COPY first fails during a release deploy, not on the PR.
- **Evidence:** `memory-bank/bolts/054-dependency-and-boot-hardening/test-walkthrough.md:163` against `.github/workflows/ci.yml` (no build job) and `.github/workflows/deploy.yml`.
- **Suggested fix:** Add a build-only `docker build` job to `ci.yml`, or correct the claim to say the Dockerfile change is unverified until the first main deploy.
- **History:** <append-only, one line per event>
  - v1: found by tests-coverage (convergence 1, not hinted), verdict confirmed.

### PPW-727 — Disproven auth-audit-log claim survives in the boot warning text and in ADR-018

- **What:** The bolt proved nothing records the auth client IP — `AuthService` and `SocialAuthService` accept the argument and never use it — and states every contrary claim was corrected. The disabled-feature warning still promises "an audit trail that names the proxy", and ADR-018 still lists the auth audit log as a reader, so an operator acting on the warning hunts for a log that does not exist.
- **Evidence:** `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:65`; `memory-bank/bolts/044-tracing-and-metrics/adr-018-*.md:283`; the disproof is recorded in `implementation-walkthrough.md:106-109`.
- **Suggested fix:** Drop the audit-trail clause from the `forwarded_headers.disabled` message and from ADR-018's amendment; keep the rate-limit and Secure-cookie clauses, which are real.
- **History:** <append-only, one line per event>
  - v1: found by requirements (convergence 1, not hinted), verdict confirmed.

### PPW-728 — System-context diagram still routes the metrics scraper through Caddy to /metrics

- **What:** The C4 diagram keeps `Rel(scraper, caddy, "GET /metrics")` plus `Rel(caddy, api, "Forwards with X-Forwarded-For")`, while the shipped Caddyfile answers `/metrics*` with 404 and the scraper must reach :9090 on the compose network. A reader designing the scrape path from this diagram builds the topology ADR-018 forbids.
- **Evidence:** `memory-bank/intents/025-security-dependency-hygiene/system-context.md:31` against the shipped `Caddyfile`; the bolt's doc sweep claimed every stale row was fixed.
- **Suggested fix:** Point the scraper at the API's scrape listener in the diagram and mark Caddy as refusing `/metrics`.
- **History:** <append-only, one line per event>
  - v1: found by requirements (convergence 1, not hinted), verdict confirmed.

### PPW-729 — A forged X-Forwarded-For aimed at the scrape listener produces no distinguishable log signal

- **What:** A container on the compose network sends `X-Forwarded-For: <allow-listed ip>` to `api:9090/metrics`. The exclusion predicate skips the whole forwarded-headers branch, so the untrusted-peer middleware never runs: an allow-listed peer scrapes silently, and a non-allow-listed one logs `metrics.scrape.denied ip=…`, identical to a benign scraper-IP change. The spoof attempt — the exact attack ADR-018 names — is invisible.
- **Evidence:** `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:84`; the denial line is in `MetricsEndpointIpAllowListMiddleware`.
- **Suggested fix:** Register the untrusted-peer middleware (log-only) inside the excluded metrics branch as well, or have the metrics gate note a forwarded header's presence in its denial line.
- **History:** <append-only, one line per event>
  - v1: found by observability (convergence 1, not hinted), verdict confirmed.

### PPW-730 — Untrusted-peer warning carries no correlation id, breaking the documented triage path

- **What:** `UseTrustedProxyForwardedHeaders` runs before `UseCorrelationId`, and the middleware logs after `await next(context)` — outside the correlation-id middleware's `LogContext.PushProperty` scope. The warning therefore has no correlation id, path, or method, so it cannot be joined to the request log or to Sentry the way the runbook tells operators to triage every other line.
- **Evidence:** `src/PhotoPrint.API/Program.cs:375` (before the correlation-id registration); `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:35`; the triage instructions are `docs/DEPLOYMENT.md` §13.9.
- **Suggested fix:** Log inside a scope carrying the request path and method, or move the untrusted-peer check after correlation-id stamping so the line is enriched like every other.
- **History:** <append-only, one line per event>
  - v1: found by observability (convergence 1, not hinted), verdict confirmed.

### PPW-731 — Once-per-process dedupe with no counter makes ongoing proxy drift look like a one-off

- **What:** After a `down`/`up` moves Caddy's address, every request is untrusted; one warning is written at the first request and never again for the process lifetime, and no metric is emitted. An operator grepping the last hour sees nothing and concludes the drift was transient, while the rate limiter stays a single global bucket indefinitely.
- **Evidence:** `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:49`; no counter exists under `MetricNames`.
- **Suggested fix:** Re-warn on an interval (hourly per peer) with an occurrence count, and add a Prometheus counter so the condition is alertable rather than grep-only.
- **History:** <append-only, one line per event>
  - v1: found by observability (convergence 1, not hinted), verdict confirmed; the grep-only diagnosis compounds PPW-714, which keeps the line off stdout entirely.
  - v3 delta: re-found by observability (convergence 1) with the operator consequence spelled out — `docs/DEPLOYMENT.md` §16.6 step 3 greps recent `docker compose logs`, so a weeks-old one-off warning reads as a healthy trust chain. Still open.

### PPW-732 — Singleton lifetime — the basis of "warned once" — is unverified in the real pipeline

- **What:** Change `AddSingleton<UntrustedForwardedPeerMiddleware>` to `AddScoped` and every test stays green, because the "warned once" test holds one hand-constructed instance across its three calls. In production each request would then resolve a fresh middleware with an empty peer map, producing one warning per request instead of one per restart — the log flood the 512 cap exists to prevent.
- **Evidence:** `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:26`; the test is `Untrusted_peer_sending_forwarded_for_is_warned_once` in `UntrustedForwardedPeerMiddlewareTests.cs`.
- **Suggested fix:** In the booted-host log test of PPW-721, send three requests from one untrusted peer and assert exactly one warning — an assertion that needs the real DI lifetime to hold.
- **History:** <append-only, one line per event>
  - v1: found by tests-coverage (convergence 1, not hinted), verdict confirmed.
  - v3 delta: re-found by correctness (convergence 1) at `ForwardedHeadersExtensions.cs:22` — unchanged; switching the registration to scoped or transient still reddens no test. Still open.

### PPW-733 — An_unparseable_trusted_proxy_aborts_boot cannot fail for the reason it is credited with

- **What:** The test asserts only that the boot exception text contains "TrustedProxies" and "not.an.ip". Both the validator and the redundant throw inside the `Configure<ForwardedHeadersOptions>` lambda produce matching text, so removing the validator entirely leaves it green — it proves nothing about the options-validation path its sibling's comment claims to guarantee.
- **Evidence:** `src/PhotoPrint.Tests/Integration/ForwardedHeadersIntegrationTests.cs:89`; the second throw is `ForwardedHeadersExtensions.cs:38`; the sibling claim is `MetricsEndpointIntegrationTests.cs:56`.
- **Suggested fix:** Assert on the `OptionsValidationException` type (or the validator's distinctive message prefix), and add the ScrapePort-conflict boot case from PPW-720.
- **History:** <append-only, one line per event>
  - v1: found by tests-coverage (convergence 1, not hinted), verdict confirmed; same root as PPW-742's duplicate validation path.

### PPW-734 — KnownProxies is read eagerly at registration and no booted host asserts it is populated

- **What:** The trusted-proxy list is read once at service-registration time and captured by the `Configure` lambda, while the on/off decision and the boot log read the live-bound settings. A configuration source added after registration — the exact hazard the observability factory's own comment warns about — yields "branch registered, nothing trusted": headers ignored plus a warning per request. `KnownProxies` is asserted only against a bare `ServiceCollection`.
- **Evidence:** `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:22` (raw read) versus `:54` (bound read); the factory comment is at `ObservabilityFactoryBase` line 178.
- **Suggested fix:** In a booted host, resolve `IOptions<ForwardedHeadersOptions>` and assert `KnownProxies` equals the configured list; or bind both reads from `IOptions` so they cannot diverge.
- **History:** <append-only, one line per event>
  - v1: found by tests-coverage (convergence 1, not hinted), verdict confirmed; the divergent double read is PPW-742.

### PPW-735 — A scalar-shaped TrustedProxies env var binds to an empty array, silently disabling the mechanism

- **What:** An operator who writes `ForwardedHeaders__TrustedProxies=172.28.0.2` instead of the indexed `__0` form gets an empty array: the binder finds no child keys, the validator short-circuits to `Success`, and boot logs `forwarded_headers.disabled` saying the list "is empty" while the operator can see they set it. Forwarded headers stay off in production.
- **Evidence:** `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:22` (`Get<string[]>()`); the short-circuit is `ForwardedHeadersSettingsValidator.cs:17`.
- **Suggested fix:** When the section has a value but no children, fail validation with a message naming the required `ForwardedHeaders__TrustedProxies__0` indexed form.
- **History:** <append-only, one line per event>
  - v1: found by input-validation (convergence 1, not hinted), verdict confirmed; the same "silently off" outcome as PPW-713, by a different mechanism.

### PPW-736 — The proxy IP is hardcoded in docker-compose.prod.yml and again in .env — one address, two sources

- **What:** The host already routes 172.28.0.0/24, so an operator changes the compose subnet and `ipv4_address` to 172.30.0.2 and forgets `.env`. Caddy's forwarded headers then arrive from an untrusted peer: every client shares one rate-limit partition and the resolved address names Caddy — visible only as a warning log line.
- **Evidence:** `docker-compose.prod.yml:29` and `.env.example:62` carry the same literal address with nothing tying them together.
- **Suggested fix:** Define the address once — a compose variable such as `${PROXY_IP:-172.28.0.2}` used by `ipv4_address` — and have `.env.example` and DEPLOYMENT.md §16 reference that single name.
- **History:** <append-only, one line per event>
  - v1: found by completeness-critic (convergence 1, not hinted), verdict confirmed; shares the compose network declaration with PPW-723.

### PPW-737 — No test or check that EF spans still carry SQL after the OpenTelemetry 1.11→1.15 bump

- **What:** `SetDbStatementForText` is gone and 1.15's beta moves database spans to the new semantic conventions. If the statement now rides `db.query.text` — or nothing at all by default — tracing silently loses the SQL text bolt 044 shipped, because no test asserts any database span attribute.
- **Evidence:** `src/PhotoPrint.API/Extensions/ObservabilityExtensions.cs:75` (the option removed by the bump); the walkthrough's Developer Notes assert the text "is emitted by default now" without a check.
- **Suggested fix:** Run one EF query through an in-memory exporter in a test and assert the span carries the SQL text under whichever attribute 1.15 emits; pin that attribute name.
- **History:** <append-only, one line per event>
  - v1: found by tests-coverage (convergence 1, not hinted), verdict plausible — the attribute name was not confirmed against the shipped package.

### PPW-738 — system-architecture.md still asserts per-endpoint rate limits this bolt proved never run

- **What:** The standard tells a reader to plan auth-abuse defences on "`auth` 10/min; register 5/h; resend and forgot-password 3/h". Those policies never bind (PPW-711), which DEPLOYMENT.md §16.7 admits. The bullet was touched by this bolt without being corrected, and standards here are descriptive.
- **Evidence:** `memory-bank/standards/system-architecture.md:52` against `docs/DEPLOYMENT.md:1758-1768`.
- **Suggested fix:** Mark the per-endpoint limits as currently inert in the same bullet, or fix the ordering (PPW-711) and delete both disclaimers.
- **History:** <append-only, one line per event>
  - v1: found by requirements (convergence 1, not hinted), verdict confirmed; resolves either way with PPW-711.

### PPW-739 — Check-then-act on the 512-entry log cap lets _loggedPeers exceed the cap

- **What:** Concurrent requests from distinct peers can all read the count as 511, all pass the cap check, and all add, so the dictionary retains 511+N entries. Bounded by in-flight request count, so the memory cost is trivial — the cap simply is not the hard bound the constant implies.
- **Evidence:** `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:35`; same pattern as `MetricsEndpointIpAllowListMiddleware`.
- **Suggested fix:** Gate on the post-increment value — if `Interlocked.Increment` returns more than the cap, decrement and take the cap-warning path.
- **History:** <append-only, one line per event>
  - v1: found by correctness (convergence 1, not hinted), verdict unverified-cleanup (cleanups get no skeptic by design).
  - v3 delta: re-found by correctness (convergence 1) with one added sub-claim — past the cap, every request runs `Interlocked.Exchange` on `_capWarned`, a contended write during exactly the storm the cap exists to bound. Still open; the never-resets mechanism is PPW-749, a separate row.

### PPW-740 — Bolt notes still warn about a Stripe.net 46→47 break the bolt disproved

- **What:** The notes say "Stripe.net 46→47 may break — keep a rollback PR ready", while the plan and test report establish that 46.3.0 never existed on nuget.org and both projects already ran 47.0.0. A reader plans rollback work for a risk that does not exist.
- **Evidence:** `memory-bank/bolts/054-dependency-and-boot-hardening/bolt.md:85` against the walkthrough's Key Decision "Stripe.net pinned at 47.0.0, not 52.x".
- **Suggested fix:** Replace the note with the measured fact: 46.3.0 was never published, 47.0.0 was already resolving, so the pin is a no-op.
- **History:** <append-only, one line per event>
  - v1: found by requirements (convergence 1, not hinted), verdict unverified-cleanup.

### PPW-741 — The metrics path is re-derived in three places with divergent empty-value handling and a silent fallback

- **What:** `Program.cs` already computes the metrics path with a `?? "/metrics"` rule and uses it for both the allow-list `UseWhen` and the scraping endpoint. `MetricsPath()` re-derives it from `IOptions` with a different rule (blank or non-`/` → `/metrics`), so a blank configured endpoint yields `""` in one place and `/metrics` in the other — two answers for one setting.
- **Evidence:** `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:108` against `src/PhotoPrint.API/Program.cs:82-84`.
- **Suggested fix:** Resolve the metrics path once — a shared helper or a normalised property on the metrics settings — and pass it to both the pipeline and the exclusion predicate.
- **History:** <append-only, one line per event>
  - v1: found by quality-altitude and completeness-critic (convergence 2, not hinted), verdict unverified-cleanup.
  - v3 delta: re-found by security (convergence 1) at `ForwardedHeadersExtensions.cs:96`, with the blank-endpoint path spelled out: `Program.cs:82` keeps `""` so `PathString.Empty` matches every path and the gate owns the whole site, while `MetricsPath()` substitutes `/metrics` and guards a path the gate no longer owns. Still open.

### PPW-742 — Trusted-proxy list is read and validated twice, leaving a second unreachable failure path

- **What:** With an unparseable entry the `ValidateOnStart` validator fails boot first, so the `InvalidOperationException` built inside the `Configure` lambda never runs in the application — it fires only in unit tests that resolve `IOptions` directly. Two messages for one error, two parses, two reads of the same section.
- **Evidence:** `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:35-41` (second path), `:22` (raw read) and `:54` (bound read).
- **Suggested fix:** Parse once — keep the invalid-entry message in the validator only, and read the list from the bound settings rather than re-reading the raw section.
- **History:** <append-only, one line per event>
  - v1: found by quality-altitude (convergence 1, not hinted), verdict unverified-cleanup; removing this path is what lets PPW-733 and PPW-720 assert what they claim.

### PPW-743 — Capped once-per-peer logger copy-pasted from MetricsEndpointIpAllowListMiddleware

- **What:** `LogUntrusted` duplicates `MetricsEndpointIpAllowListMiddleware.LogDenied` line for line: the same 512 cap constant, the same cap-warned exchange, the same `log_cap_reached distinct_ips={Cap}` wording, the same canonicalise-add-increment sequence and the redundant counter beside the dictionary. Any fix to the cap logic must now be made twice or drift.
- **Evidence:** `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:33` and the corresponding block in `MetricsEndpointIpAllowListMiddleware`.
- **Suggested fix:** Extract one capped-distinct-peer log helper (key, cap, cap warning) used by both middlewares, with the cap constant in one place.
- **History:** <append-only, one line per event>
  - v1: found by quality-altitude (convergence 1, not hinted), verdict unverified-cleanup; PPW-739 is the defect inside the duplicated logic.

### PPW-744 — Third hand-rolled bind of the "RateLimit" section, only to log one number

- **What:** `GetSection("RateLimit").Get<RateLimitSettings>() ?? new RateLimitSettings()` now exists in two places with the section name as a bare literal in both, while every other settings class carries a `SectionName` constant. Renaming the section breaks the boot log silently — it falls back to defaults and prints a permit limit of 100 whatever is configured.
- **Evidence:** `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:72` and `src/PhotoPrint.API/Extensions/SecurityExtensions.cs:51`.
- **Suggested fix:** Add `RateLimitSettings.SectionName` like the other settings classes, bind it once in `AddSecurityBaselines`, and read `IOptions<RateLimitSettings>` here.
- **History:** <append-only, one line per event>
  - v1: found by quality-altitude (convergence 1, not hinted), verdict unverified-cleanup; PPW-712's fix also needs an options-based read, so the two belong together.

### PPW-745 — Scrape-named observability type ScrapeIpAllowList is now the shared IP-list parser for proxy trust

- **What:** Forwarded-header trust parsing, the untrusted-peer middleware's canonicalisation and the metrics allow-list all route through a class named `ScrapeIpAllowList` in the Observability namespace. The new `Addresses`/`Networks` members exist only to copy entries into the forwarded-headers options, and `Networks` allocates a fresh read-only wrapper on every access.
- **Evidence:** `src/PhotoPrint.API/Observability/ScrapeIpAllowList.cs:56`.
- **Suggested fix:** Move the parser to a neutral IP-list type (for example `Configuration/IpAllowList`) and expose the parsed entries as stored read-only collections rather than per-call wrappers.
- **History:** <append-only, one line per event>
  - v1: found by quality-altitude (convergence 1, not hinted), verdict unverified-cleanup; reuse over reimplementation was the bolt's deliberate choice, so only the naming and the per-call allocation are at issue.

### PPW-746 — A whole ServiceProvider is built (and never disposed) per simulated request in the middleware tests

- **What:** The test helper builds a configuration builder, a service collection and a service provider on every simulated request, none of them disposed — three leaked providers in the "warned once" test alone. The same provider-building block is duplicated in the options tests.
- **Evidence:** `src/PhotoPrint.Tests/Unit/Middleware/UntrustedForwardedPeerMiddlewareTests.cs:85`; the duplicate is `ForwardedHeadersOptionsTests.BuildOptions`.
- **Suggested fix:** Build the options once per test class (a readonly field or a shared helper used by both new test files) and dispose the provider.
- **History:** <append-only, one line per event>
  - v1: found by quality-altitude (convergence 1, not hinted), verdict unverified-cleanup.

### PPW-747 — No test proves the rate limiter partitions per forwarded client, so a one-line reorder silently restores one bucket for the whole internet

- **What:** The bolt's headline promise — one rate-limit bucket per real visitor instead of one for the whole site — holds only because `app.UseTrustedProxyForwardedHeaders()` (`src/PhotoPrint.API/Program.cs:375`) runs before `app.UseSecurityBaselines()` (`:380`), which installs `UseRateLimiter()` (`src/PhotoPrint.API/Extensions/SecurityExtensions.cs:122`) whose global limiter partitions on `Connection.RemoteIpAddress` (`:59-61`). Swap those two lines and the limiter keys on the proxy's own address again. No test reddens, and §16 of `docs/DEPLOYMENT.md` still reads as if the promise held.
- **Evidence:** Confirmed by mutation: moving `UseTrustedProxyForwardedHeaders()` below `UseSecurityBaselines()` leaves the whole scoped set green. `src/PhotoPrint.Tests/Integration/RateLimitIntegrationTests.cs:13` builds `SecurityBaselineFactory` with no `ForwardedHeaders:TrustedProxies` and TestServer reports a null peer, so every client already shares one partition there; `src/PhotoPrint.Tests/Integration/ForwardedHeadersIntegrationTests.cs:169` asserts only its own probe middleware's resolved IP and never a limiter response. `docs/DEPLOYMENT.md` §16.7 item 3 records an ordering defect of exactly this class (PPW-711), so the class is known to recur here.
- **Suggested fix:** Add an integration test that drives the limiter through a trusted proxy with two distinct forwarded clients and asserts each gets its own budget.
  - **Fix brief:** `src/PhotoPrint.Tests/Integration/RateLimitIntegrationTests.cs` · `src/PhotoPrint.Tests/Integration/ForwardedHeadersIntegrationTests.cs:109-141` (the `TrustedProxyFactory` + peer-stamping `IStartupFilter` pattern). Resolve the limits through `IOptionsMonitor` inside the factory, not at registration — `src/PhotoPrint.Tests/Integration/SecurityBaselineFactory.cs:67-69` records that registration captures config before test config applies. Do not touch `SecurityExtensions.cs` ordering as part of this row: that is PPW-711's deferred change.
  - **testShape:** `RateLimit_PartitionsPerForwardedClient` — `TrustedProxyFactory("172.28.0.2")` with `RateLimit:Public:PermitLimit=3`, peer stamped as the proxy; spend 3 requests as `X-Forwarded-For: 203.0.113.1`, then one as `198.51.100.7`; assert 200, not 429. Reverting the Program.cs order must redden it.
  - **Trigger-list-shaped:** yes (rate-limiter partitioning on the public surface)
- **History:** <append-only, one line per event>
  - v3 delta: found by completeness-critic (convergence 1, not hinted), verdict confirmed by mutation trace.

### PPW-748 — The metrics-scrape exclusion's excluded branch is untested in the changed test file; only an out-of-scope file guards it

- **What:** `IsMetricsScrape` exists for one combination — path `/metrics` **and** `LocalPort == scrapePort` — and that combination is asserted nowhere in the file the fix round changed. All three new observability tests assert the client **is** resolved: `/metrics` only on port 8080, port 9090 only on the probe path. Deleting the predicate, or making it return false, leaves `ForwardedHeadersIntegrationTests` fully green.
- **Evidence:** Confirmed by mutation: with `IsMetricsScrape` forced false, `src/PhotoPrint.Tests/Integration/ForwardedHeadersIntegrationTests.cs:122`, `:132` and the boot test all still pass. The only test that reddens is `MetricsEndpointIntegrationTests.Forwarded_for_cannot_open_the_scrape_gate` (`src/PhotoPrint.Tests/Integration/MetricsEndpointIntegrationTests.cs:89`, 403 becomes 200) — an unchanged file that reached no lens, whose assertion never names forwarded headers, so a future edit can weaken it without any signal that this predicate lost its guard.
- **Suggested fix:** Assert the excluded branch where the predicate lives, and put the scrape-gate test file in a lens's scope.
  - **Fix brief:** `src/PhotoPrint.Tests/Integration/ForwardedHeadersIntegrationTests.cs:109-141` (add a `TrustedProxyOnScrapeListenerFactory` alongside the existing observability factories) · `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:72-86`. One test, in the `ForwardedHeadersWithObservabilityTests` class — the class the round's recorded test filters omitted.
  - **testShape:** `The_metrics_path_on_the_scrape_port_keeps_its_peer` — `ResolveAsync(peer "172.28.0.2", forwardedFor "203.0.113.9", path "/metrics")` on the scrape listener; assert `ClientIp == "172.28.0.2"`. Forcing `IsMetricsScrape` false must redden it.
  - **Trigger-list-shaped:** no
- **History:** <append-only, one line per event>
  - v3 delta: found by completeness-critic (convergence 1, not hinted), verdict confirmed by mutation trace.
  - v3 delta: `residual-of: PPW-722` — the round's fix added the two non-excluded cases and left the excluded one uncovered (seed round 1, area forwarded-headers).

### PPW-749 — The 512-peer log budget never resets, so cheap in-network noise can permanently silence the proxy-drift warning

- **What:** `PeerBudget`'s 512-entry cap (`src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:110-120`) is process-lifetime and never resets, and the middleware is a singleton (`src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:22`). Once 512 distinct untrusted peers have been seen, one `log_cap_reached` line is written and every later untrusted peer is silent — including a genuinely drifted Caddy, whose requests then lose their forwarded identity (shared rate-limit bucket, refresh cookie without `Secure`) with nothing in the log to say so.
- **Evidence:** `:110-120` (cap and warning) with `:46` showing the budget keys on the canonical TCP peer address, not on header contents — so the finder's "512 forwarded values from one container" route does not work; each peer costs one slot. Filling the cap needs 512 distinct source addresses able to reach the API port, which `docker-compose.prod.yml:43` does not publish in production, so it takes an in-network peer; that is why this is 🟡 and not 🟠.
- **Suggested fix:** Never let cheap noise evict the drift signal — keep an uncapped counter or metric of untrusted-peer requests, or reserve budget slots for peers seen on a majority of requests.
- **History:** <append-only, one line per event>
  - v3 delta: found by observability (convergence 1, not hinted), verdict confirmed by trace with the mechanism corrected; severity re-judged from the lens's medium to 🟡 because reaching the cap requires 512 distinct in-network source addresses.
  - v3 delta: distinct from PPW-731 (once-per-peer dedupe) and PPW-739 (cap overshoot under concurrency) — same file, different mechanism; a fix that replaces the budget wholesale would settle all three.

### PPW-750 — DEPLOYMENT.md §16.3 names three changed behaviours and omits the HSTS header the trusted-proxy switch now makes reachable

- **What:** `HstsMiddleware` skips any request where `Request.IsHttps` is false, so with an empty trusted-proxy list the API never sent `Strict-Transport-Security`. Honouring `X-Forwarded-Proto` at `src/PhotoPrint.API/Program.cs:375` flips `IsHttps` to true before `UseHsts()` runs (`src/PhotoPrint.API/Extensions/SecurityExtensions.cs:117`, `MaxAge` 365 days, `IncludeSubDomains`), so the API emits that header for the first time. `docs/DEPLOYMENT.md:1702` still says three behaviours change, and no record or test mentions the fourth.
- **Evidence:** Constructible at the API: a TLS request to Caddy, proxied as plain HTTP with `X-Forwarded-Proto: https` from a trusted peer, produces the header — impossible with an empty list. The consequence is masked: `Caddyfile:36` already sets the same header (plus `preload`) and its `header` directive replaces the upstream value, so no browser sees a change. Real omission, low visible impact — hence 🟡 rather than the lens's medium.
- **Suggested fix:** Add HSTS as a fourth item in §16.3, and confirm in the same edit that Caddy's `header` directive replaces rather than appends `Strict-Transport-Security`, so responses cannot carry two values.
- **History:** <append-only, one line per event>
  - v3 delta: found by completeness-critic (convergence 1, not hinted), verdict confirmed by trace; severity re-judged medium→🟡 because Caddy overwrites the header before any client sees it.
  - v3 delta: `residual-of: PPW-713` — the round switching trust on is what made this behaviour reachable and the §16.3 list incomplete (seed round 1, area docs).

### PPW-751 — Serilog WriteTo merges by array index, so the Development overlay collides with the base Console sink's formatter at WriteTo:0

- **What:** Configuration merges per leaf key, not per array element. The round removed the File sink from the base `WriteTo` array (`src/PhotoPrint.API/appsettings.json:183`), which moved `Console` from index 1 to index 0 — where `src/PhotoPrint.API/appsettings.Development.json:51`'s own Console entry now merges into it. The merged sink carries both `Args:formatter` (base, compact JSON) and `Args:outputTemplate` (Development), and `Serilog.Settings.Configuration` has no Console overload taking both, so one is discarded by whichever overload it picks.
- **Evidence:** Ran it: the merged configuration really does carry both leaves at `Serilog:WriteTo:0:Args`, and with the pinned `Serilog.AspNetCore` 10.0.0 (`Directory.Packages.props:44`) the `outputTemplate` overload wins — the dev console printed `[00:06:20 INF] : hello dev console 42`, not compact JSON. So the claimed harm does not occur today; what remains is a dev log format that depends on the library's overload choice, with the only test comparing sink names (`src/PhotoPrint.Tests/Unit/Configuration/DeploymentDefaultsTests.cs:132-152`) and no assertion anywhere on `Args`. Production (formatter only, no overlay) and Testing (no Serilog block) are unaffected.
- **Suggested fix:** Assert on the merged `WriteTo:*:Args` keys rather than sink names alone, or give each overlay a complete sink array so no index is shared.
- **History:** <append-only, one line per event>
  - v3 delta: found by completeness-critic (convergence 1, not hinted), verdict `plausible` — the merge is real, the claimed compact-JSON output is not; recorded 🟡, Development only.
  - v3 delta: `residual-of: PPW-714` — the sink relocation moved Console onto the overlay's index (seed round 1, area logging).

### PPW-752 — TrustedProxyList re-parses the trusted-proxy list and discards parse errors, so the validator's caps do not guard the type that decides trust

- **What:** `src/PhotoPrint.API/Configuration/TrustedProxyList.cs:12` calls `Parse(…, out _)` and drops every error, so the type that actually decides whether a peer is trusted would accept `0.0.0.0/0` or `fe80::1` on its own. The width and link-local refusals live only in `src/PhotoPrint.API/Validators/ForwardedHeadersSettingsValidator.cs:29-48`. Today that validator always runs first through `IOptions.Value` with `ValidateOnStart`, so boot refuses such values; any later path that builds the list without it — a test host, a second registration — trusts the whole internet to set `X-Forwarded-For`.
- **Evidence:** `TrustedProxyList.cs:12` and the validator's cap block; reported as read by two lenses, not proven by a trace (the delta's low rows had no skeptic).
- **Suggested fix:** Enforce the pair-width and link-local rules inside `TrustedProxyList` itself — or in one parse routine it and the validator both call — and throw on any error instead of discarding it.
- **History:** <append-only, one line per event>
  - v3 delta: found by security + completeness-critic (convergence 2, not hinted), reported as read, not proven — skeptics were not run on this pass's low rows.
  - v3 delta: `residual-of: PPW-715` — the type is the round's own, added by the same fix that put the caps in the validator (seed round 1, area proxy-trust).

### PPW-753 — Log assertions capture around Serilog, so no test executes the production logging configuration this round rewrote

- **What:** `LogCapture` replaces `ILoggerFactory` wholesale and its `CaptureLogger` returns `IsEnabled` true for every level and drops the category, so every forwarded-header log assertion runs around Serilog rather than through it. Adding `"PhotoPrint.API.Middleware": "Error"` to `MinimumLevel.Override` — in the base file or the round's new `appsettings.Production.json` — removes every forwarded-header warning in production while all scoped tests, including the new Serilog sink assertions, stay green.
- **Evidence:** `src/PhotoPrint.Tests/Integration/ForwardedHeadersIntegrationTests.cs:244`; the round recorded the same limit itself under "Known limits of this round's evidence" in [resolution-v1.md](resolution-v1.md). Reported as read, not proven by a trace.
- **Suggested fix:** Assert that the merged Production configuration admits `Warning` for the `PhotoPrint.API.Middleware` category, or drive one case through a real `ReadFrom.Configuration` logger instead of the capture factory.
- **History:** <append-only, one line per event>
  - v3 delta: found by observability + completeness-critic (convergence 2, not hinted), reported as read, not proven.
  - v3 delta: `residual-of: PPW-714` — the round rewrote the production logging configuration and its tests assert around it (seed round 1, area logging).

### PPW-754 — The new production rolling file sink writes into the container's ephemeral layer — no volume backs /app/logs

- **What:** `src/PhotoPrint.API/appsettings.Production.json:13` writes `logs/log-.json` under the image's working directory, and `docker-compose.prod.yml` mounts only `apidata:/app/Storage`. The files therefore live in the api container's writable layer, so every redeploy or `docker compose up --force-recreate` destroys the 30-day retention the new `DeploymentDefaultsTests` asserts production keeps.
- **Evidence:** `appsettings.Production.json:13` against the api service's volume list in `docker-compose.prod.yml`; reported as read, not proven. What the runbook's verification greps read is the console sink added for PPW-714, so what is lost here is the on-disk trail only.
- **Suggested fix:** Add a named volume (for example `apilogs:/app/logs`) to the api service, or drop the File sink and rely on the console stream the runbook already greps.
- **History:** <append-only, one line per event>
  - v3 delta: found by correctness (convergence 1, not hinted), reported as read, not proven.
  - v3 delta: `residual-of: PPW-714` — re-opens the part of PPW-714's evidence its fix did not address, the missing `/app/logs` volume (seed round 1, area logging).

### PPW-755 — The new production File sink can be dropped or fail to open with no diagnostic, because SelfLog is enabled nowhere and the package is transitive

- **What:** `Serilog.Sinks.File` is only a transitive dependency of `Serilog.AspNetCore` and the sink is named by a configuration string, so if that transitive pin moves or an `Args` name stops binding, `Serilog.Settings.Configuration` writes to `SelfLog` and skips the sink. A read-only or root-owned `logs` mount fails the same way. `Serilog.Debugging.SelfLog` is enabled nowhere in `src`, so the audit trail is simply absent with no error.
- **Evidence:** `src/PhotoPrint.API/appsettings.Production.json:11`; `Directory.Packages.props:44-47` pins `Serilog.AspNetCore`, `Serilog.Enrichers.*` and `Serilog.Formatting.Compact` but no `Serilog.Sinks.File`; a `SelfLog` search over `src` matches compiled assemblies only, no source. Reported as read, not proven by a trace.
- **Suggested fix:** Enable `Serilog.Debugging.SelfLog` to stderr in `AddSerilogLogging`, and add a direct `Serilog.Sinks.File` package reference so the sink cannot silently vanish.
- **History:** <append-only, one line per event>
  - v3 delta: found by observability (convergence 1, not hinted), reported as read; the two package facts were checked and hold.
  - v3 delta: `residual-of: PPW-714` — the sink is the round's own (seed round 1, area logging).

### PPW-756 — A null RemoteIpAddress returns before judging, on the one transport where ASP.NET honours X-Forwarded-For with no peer check

- **What:** `JudgeForwardedValue` returns as soon as `context.Connection.RemoteIpAddress` is null (`src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:36`). That is exactly the case where `ForwardedHeadersMiddleware` skips its known-proxy check and honours `X-Forwarded-For` unconditionally — a transport with no remote IP, such as a Unix socket, a named pipe or TestServer. Any caller on such a transport sets its own client IP for rate limiting and audit, and no `untrusted_peer` warning is emitted for it.
- **Evidence:** `:36` read against `ForwardedHeadersMiddleware`'s null-peer path; reported as read, not proven by a trace. The API is reached over TCP in every shipped configuration, which is why this is 🟡.
- **Suggested fix:** Warn once, on its own budget, when `X-Forwarded-For` arrives with a null peer address — the header is being honoured with no peer check at all.
- **History:** <append-only, one line per event>
  - v3 delta: found by correctness (convergence 1, not hinted), reported as read, not proven.
  - v3 delta: NEW — possible remainder of PPW-716's rewrite: the guard line is the round's code, but the blind spot may predate it, so no lineage is recorded rather than a guessed one (`seed_round: null`).

### PPW-757 — The NuGet audit gate is asserted as a command string and never executed, and the shipping image restores without it

- **What:** `CiRestore_HardFailsOnAuditWarnings` matches the literal `dotnet restore PhotoPrint.sln -p:FailOnAudit=true`, so nothing proves NU1901–NU1905 actually become errors: a `NoWarn`, or the props not being imported before restore, leaves the gate inert with the test still green. `Dockerfile:15` restores `PhotoPrint.API.csproj` with no audit flag, and `deploy.yml`'s `workflow_dispatch` path publishes an image with no CI run at all (`if: github.event_name == 'workflow_dispatch' || …conclusion == 'success'`), so the shipping image's own restore is never audited.
- **Evidence:** `src/PhotoPrint.Tests/Unit/Configuration/DeploymentDefaultsTests.cs:121` (string assertion), `Dockerfile:15`, `.github/workflows/deploy.yml:6-15` — the three claims were checked and hold. The round recorded the same limit itself: PPW-718 is proven by configuration assertions only ([resolution-v1.md](resolution-v1.md), "Known limits of this round's evidence").
- **Suggested fix:** Pin a known-vulnerable package version once and confirm `dotnet restore PhotoPrint.sln -p:FailOnAudit=true` exits non-zero; give the Dockerfile the same flag, and put `Dockerfile` and `deploy.yml` in a lens's scope.
- **History:** <append-only, one line per event>
  - v3 delta: found by completeness-critic (convergence 1, not hinted), reported as read; the Dockerfile and deploy-trigger claims were verified.
  - v3 delta: `residual-of: PPW-718` — the gate is the round's own, and its only proof is the string assertion the round disclosed as a limit (seed round 1, area ci).
