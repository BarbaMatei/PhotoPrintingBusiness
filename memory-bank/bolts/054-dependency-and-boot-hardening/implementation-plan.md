---
stage: plan
bolt: 054-dependency-and-boot-hardening
created: 2026-09-03T20:42:35Z
design_check: 2026-09-03T21:40:00Z
---

## Implementation Plan: Dependency & Boot Hardening

### Objective

Make the dependency tree auditable and the boot pipeline production-correct:

- Bump the OpenTelemetry suite past GHSA-4625-4j76-fww9 (story 001).
- Adopt Central Package Management so one `<PackageVersion>` governs every project, and pin
  Stripe.net (story 002).
- Add Renovate config so future upgrades arrive as grouped, scheduled PRs (story 003).
- Register `ForwardedHeadersMiddleware` behind an explicit trusted-proxy list, so code that
  reads the client IP sees the client, not Caddy (story 004).

No EF migration, no controller business logic, no frontend file is touched.

**Behaviour-change claim, stated precisely.** Stories 001–003 change no runtime behaviour.
Story 004 does, *by design*, but only once an operator sets `ForwardedHeaders:TrustedProxies`:
an empty list (the shipped default for every existing environment) means the middleware is
never registered and boot is byte-identical to today. What changes when it is set is
enumerated in the caller-impact sweep below — including two consequences the first draft of
this plan missed and the design check caught.

---

### Measured baseline (2026-09-03, `dotnet restore` + `dotnet list package --vulnerable`)

Three facts from the real restore contradict the stories written on 2026-06-05:

1. **`Stripe.net 46.3.0` does not exist on nuget.org.** Both projects request it and both get
   `NU1603` — *"depends on Stripe.net (>= 46.3.0) but Stripe.net 46.3.0 was not found.
   Stripe.net 47.0.0 was resolved instead."* The "silent 46.3.0 / 47.0.0 split" story 002
   describes is really *both projects silently running 47.0.0*. Pinning 47.0.0 is a no-op at
   runtime, and the 46→47 breaking-change risk the bolt notes warn about does not exist.
2. **The direct-package scan flags one package**: `OpenTelemetry.Exporter.OpenTelemetryProtocol`
   1.11.2, Moderate, GHSA-4625-4j76-fww9 — the story-001 CVE.
3. **The transitive scan flags four more**: `OpenTelemetry.Api` 1.11.2 (Moderate,
   GHSA-g94r-2vxg-569j), `System.Security.Cryptography.Xml` 8.0.2 (eight High),
   `System.Net.Http` 4.3.0 (High), `System.Text.RegularExpressions` 4.3.0 (High). No story
   names them; they are in scope because the unit's success criterion is a clean scan for an
   audit-driven intent, and transitive pinning — which story 002 asks for — is the lever.
   They are **not one class**: `System.Security.Cryptography.Xml` reaches the API's shipped
   closure through `Microsoft.AspNetCore.Identity` 2.3.1; the other two exist only in the
   Tests graph, dragged in by `NETStandard.Library` 1.6.1 under xunit 2.5.3, and never load on
   `net8.0`. The plan treats them separately and says which is which.

---

### Deliverables

**Story 001 — OTel bump**

- Every `OpenTelemetry.*` reference moves from 1.11.x to the newest 1.15.x, as a set:
  `Exporter.Console` / `Exporter.OpenTelemetryProtocol` / `Extensions.Hosting` → `1.15.3`;
  `Exporter.Prometheus.AspNetCore` → `1.15.3-beta.1`; `Instrumentation.AspNetCore` → `1.15.2`;
  `Instrumentation.Http` / `Instrumentation.Runtime` → `1.15.1`;
  `Instrumentation.EntityFrameworkCore` → `1.15.1-beta.1` (versions confirmed to exist on the
  nuget.org flat container; the two beta pins are the newest builds those packages have — no
  stable line exists for either).
- 1.15.x, not the newest 1.18.x, because story 001 specifies 1.15.x and Renovate (story 003)
  is the mechanism for going further.
- **No separate `OpenTelemetry.Api` pin.** `Exporter.OpenTelemetryProtocol` 1.15.3 already
  depends on `OpenTelemetry.Api` 1.15.3, so the suite bump clears GHSA-g94r-2vxg-569j on its
  own; a standalone pin would put one member of a lockstep suite outside the set.

**Story 002 — Central Package Management**

- New `Directory.Packages.props` at the repo root: `ManagePackageVersionsCentrally=true`,
  `CentralPackageTransitivePinningEnabled=true`, one `<PackageVersion>` per package.
- Every `Version=` attribute removed from both `.csproj` files. `<PrivateAssets>` /
  `<IncludeAssets>` child elements stay exactly as they are — CPM relocates only the version.
- `Stripe.net` pinned once, to `47.0.0` (already resolving; majors deferred to Renovate).
- **Restore fails, not warns, on the class this story exists to prevent.** `NU1008` (an inline
  version under CPM) and `NU1010` (a reference with no version) are already errors, and the SDK
  already treats `NU1605` (downgrade) as an error. The one gap is `NU1603` — the Stripe.net
  bug — so the props file adds `<WarningsAsErrors>$(WarningsAsErrors);NU1603</WarningsAsErrors>`,
  appending rather than clobbering an inherited value. `NU1608` is deliberately **not**
  promoted: it is the characteristic warning of transitive pinning and would make an unrelated
  upstream constraint change break the build.
- **The shipped closure must not move.** Transitive pinning makes one central version govern
  both projects, and the two projects resolve some shared packages differently today (the Tests
  project pins `Microsoft.Extensions.Configuration*` 10.0.8 where the API resolves 10.0.0). The
  rule for every such package: **pin the version the API already resolves**, so production
  binaries are unchanged and the Tests project moves instead. Enforced by diffing
  `src/PhotoPrint.API/obj/project.assets.json` before and after the conversion; every package
  whose resolved version changes is listed in the walkthrough with a reason.
- **Vulnerable transitive pins**, split by whether they ship:
  - `System.Security.Cryptography.Xml` → `8.0.4` — **ships** (API graph, via
    `Microsoft.AspNetCore.Identity` 2.3.1). Verified empirically: if 8.0.4 does not clear the
    advisories, the pin is reverted and the residual is recorded rather than left looking fixed.
  - `System.Net.Http` → `4.3.4`, `System.Text.RegularExpressions` → `4.3.1` — **test-only
    netstandard1.x shims** from `NETStandard.Library` 1.6.1 under xunit 2.5.3, absent from the
    API graph, never loaded on `net8.0`. Pinned so the audit output is clean, and labelled as
    test-only in the walkthrough so no future reader mistakes them for a production control.
    The real fix is a xunit upgrade past the netstandard1.6 meta-package — recorded, not done.
- `Dockerfile`: the API restore stage copies only `src/PhotoPrint.API/*.csproj`, so under CPM
  its restore fails with `NU1008`. It must also copy `Directory.Packages.props` into `/src`.
  **This is the one change that fails the deploy, not the test suite, if missed.**
- `.github/workflows/ci.yml`: the NuGet cache key `hashFiles('**/*.csproj')` no longer covers
  versions; it gains `Directory.Packages.props`.

**Story 003 — Renovate**

- New `.github/renovate.json`: `dependencyDashboard: true`; grouped rules for
  `^OpenTelemetry\.`, `^Microsoft\.EntityFrameworkCore`/`^Npgsql`, and `^@angular/`; routine
  updates on the first of the month, majors on Jan/Apr/Jul/Oct; `vulnerabilityAlerts` labelled
  `security`; auto-merge off everywhere (payments codebase).
- Installing the Renovate GitHub App is a repo-admin action, not code — recorded as an open
  question in the test report, not claimed as done.

**Story 004 — Forwarded headers**

- New `ForwardedHeadersSettings` (section `ForwardedHeaders`, key `TrustedProxies`: addresses
  or CIDR ranges, default empty) + validator, following the `ObservabilitySettings` /
  `ValidateOnStart` pattern.
- New `Extensions/ForwardedHeadersExtensions.cs`: `AddTrustedProxyForwardedHeaders` (bind,
  validate, configure `ForwardedHeadersOptions`) and `UseTrustedProxyForwardedHeaders`
  (register the middleware, excluding the scrape listener).
- `Program.cs` gains **exactly two lines** — one service registration, one pipeline
  registration placed first, before `UseCorrelationId`. Nothing existing is moved (bolt 055).
- `ScrapeIpAllowList` gains two read-only accessors so the parsed addresses and networks
  populate `ForwardedHeadersOptions`, keeping the IP/CIDR parsing rules in one home. The
  networks need an explicit conversion: `ScrapeIpAllowList` holds `System.Net.IPNetwork`, and
  on `net8.0` `ForwardedHeadersOptions.KnownNetworks` is
  `IList<Microsoft.AspNetCore.HttpOverrides.IPNetwork>` — a different type (the two were
  unified only in .NET 9). A unit test asserts a `/16` entry actually matches through the
  middleware, not merely through `ScrapeIpAllowList.Contains`.
- `docker-compose.prod.yml` declares an explicit network with a fixed subnet and a static
  address for Caddy, so the trusted proxy is a single `/32` an operator can copy.
- `.env.example`, `docs/DEPLOYMENT.md` (§14.3 amendment + new §16), `Caddyfile` and
  ADR-018 document and enforce the trust boundary.

---

### Technical approach — story 004 in detail

**The design conflict, and how it is resolved (D4).** Story 004 says the `/metrics` allow-list
is silently wrong behind Caddy and that an `X-Forwarded-For` case should make an allow-listed
IP return 200. That premise was true when the story was written (2026-06-05) and is not true
now. Bolt 044's **ADR-018 amendment of 2026-07-31** closed the hole topologically — `/metrics`
is served only on the unproxied `:9090` listener and the `Caddyfile` answers `/metrics*` with a
404 — and explicitly *rejected* trusting `X-Forwarded-For` there, for two stated reasons: a
header a client can set is weaker than a peer address, and `ForwardedHeadersMiddleware` consumes
and removes the header, so any rule keyed on it breaks silently the day the middleware is wired
up. `DEPLOYMENT.md` §14.3 repeats it as an operator instruction.

Honouring the story's third acceptance criterion would *reintroduce* the exposure ADR-018
removed. **Resolution: the middleware is registered, and the scrape listener is excluded from
it.** Criteria 1, 2 and 4 are implemented as written; criterion 3 is implemented inverted — the
regression test asserts that an `X-Forwarded-For` claiming an allow-listed address still gets
403 when the real peer is not allow-listed. Recorded as consciously unmet in `bolt.md`, amended
into ADR-018 as a third invariant, and reported to the coordinator.

**How the exclusion is keyed — by listener, not by path.** The obvious predicate ("skip when the
path is the metrics path") is a trap: `PathString.StartsWithSegments("/")` matches every path,
and `Observability:Metrics:PrometheusEndpoint = "/"` is an accepted value (backlog PPW-362),
while an env var set to empty yields `""` — either would silently disable forwarded headers for
the whole site with nothing logged. The predicate is therefore
`Observability:Metrics:ScrapePort != 0 && Connection.LocalPort == ScrapePort`, which is
byte-for-byte the gate's own first check, cannot be widened by a path typo, and adds no fourth
place that must track the metrics path.

That leaves one hole: with `ScrapePort = 0` the exclusion never fires. So the validator
**fails boot** when `TrustedProxies` is non-empty, observability is enabled, and `ScrapePort`
is 0 — a non-empty trusted-proxy list is the operator declaring "a reverse proxy sits in front
of me", and ADR-018 already says such a deployment MUST bind a scrape listener the proxy does
not route. Nothing enforced that MUST before; now something does.

**What the middleware is actually for.** Three call sites read
`HttpContext.Connection.RemoteIpAddress`, and behind Caddy all three see Caddy's container
address for every request on earth. Two more read `Request.Scheme` through `Request.IsHttps`.
Both contracts are swept in full below.

**Anti-spoofing.** Four layers, and the design check pushed the first one from "the Compose
subnet" to "Caddy's address":

1. `KnownNetworks` and `KnownProxies` are **cleared** first — ASP.NET ships loopback defaults
   that would otherwise trust a header from anything on localhost.
2. Only entries from `ForwardedHeaders:TrustedProxies` are added back, and the runbook
   documents **Caddy's single address**, not the Compose subnet. The subnet form is what §14.5
   teaches for `AllowedScrapeIps`, and copying it here would be a real weakening:
   `docker-compose.prod.yml` `expose`s 8080 and 9090 on that same network, so any container on
   the bridge could reach the API directly, pass `CheckKnownAddress`, and then name any client
   IP it liked — unlimited rate-limit budget for itself, a targeted denial of service against
   one real customer's partition, and forged addresses written into the auth audit trail. To
   make the `/32` knowable, `docker-compose.prod.yml` declares the network's subnet and pins
   Caddy's address; the runbook states the subnet-collision caveat and how to change it.
3. `ForwardLimit = 1`. The middleware walks `X-Forwarded-For` right-to-left, so a limit of one
   takes the rightmost entry — the address Caddy observed. Client-injected entries sit to its
   left and are never read.
4. The `Caddyfile` sets `header_up X-Forwarded-For {remote_host}`, replacing the inbound header
   rather than appending, so layer 3's arithmetic is not the only thing between a spoofed header
   and the rate limiter.

**Boot-time observability** (D-o-D class 6): the extension logs its state once at boot —
`Information` naming the trusted entries when configured, `Warning` in Production when
`TrustedProxies` is empty. Because enabling it is also the moment the per-client rate-limit
budget starts being real, the enabled line states the effective budget, so the operator sees
the number at the moment it starts mattering.

**The rate-limit budget, stated rather than assumed.** `UseRateLimiter()` runs inside
`UseSecurityBaselines()` (`Program.cs:378`) — before `UseStaticFiles()` (`:388`) and before
`UseRouting()` (`:391`). So every static asset of the combined SPA image consumes a permit, and
the shipped `RateLimit:Public:PermitLimit = 100`/60s has never been exercised as a per-client
budget, because behind Caddy it has always been one bucket for the whole internet. This bolt is
what makes it real. The limiter's own policy belongs to intent 029 P08, so this bolt does not
change the number; it makes the change visible (boot log) and makes reviewing it a required
step of the rollout sequence in §16 before `TrustedProxies` is set.

**Trusted CIDR provenance.** `docker-compose.prod.yml` declares no explicit subnet today, so
Docker allocates the bridge range at `up` time and no fixed value can be committed. The file now
declares one, which is what makes a `/32` for Caddy possible and lets `.env.example` ship the
real value uncommented — so a deploy that follows the runbook gets the PPW-460 fix switched on
rather than leaving it opt-in-and-forgotten.

---

### Caller-impact sweep

**Contract: "package versions live in the project file".** Changed by CPM.

| Consumer | Verdict |
|---|---|
| `src/PhotoPrint.API/PhotoPrint.API.csproj` | Updated — all `Version=` removed |
| `src/PhotoPrint.Tests/PhotoPrint.Tests.csproj` | Updated — all `Version=` removed |
| `Dockerfile` (api-build restore stage) | Updated — copies `Directory.Packages.props` into `/src`, else `NU1008` at image build |
| `.github/workflows/ci.yml` (NuGet cache key) | Updated — key hashes `Directory.Packages.props` too |
| `.github/workflows/deploy.yml` | Unaffected — no `dotnet` step; builds the image, which the Dockerfile change covers |
| `.github/workflows/secret-scan.yml` | Unaffected — gitleaks only, no restore |
| `PhotoPrint.sln` | Unaffected — CPM is MSBuild-level, no solution entry |
| `global.json` / `nuget.config` / `Directory.Build.props` | None exist anywhere in the tree — nothing to reconcile, and nothing else the image context is missing |
| `src/PhotoPrint.UI` | Unaffected — npm, not NuGet; no MSBuild project file |
| `scripts/`, `ops/`, `hooks/`, `secrets/` | Unaffected — grepped: no `dotnet restore` / `csproj` reference |
| **The API's resolved package closure** | **Must not move.** Verified by an assets diff; every changed row listed in the walkthrough |

**Contract: `HttpContext.Connection.RemoteIpAddress` is the TCP peer.** Changed for requests
from a trusted proxy only.

| Consumer | Verdict |
|---|---|
| `SecurityExtensions.cs:61` — rate-limiter partition key | **Intended change**: partitions on the real client (backlog PPW-460 🔴). Two consequences named above: static assets are inside the budget, and per-IP partitions are per-address, so an IPv6 client with a routed `/64` can rotate addresses. Both belong to the limiter's policy (intent 029 P08); both are written down here and in §16 rather than silently inherited |
| `AuthController.cs:54,72,160` → `AuthService` login / refresh audit IP | **Input corrected, nothing consumes it.** Found by the stage-4 gate: `AuthService.cs:95,163` and `SocialAuthService.cs:36` accept `ipAddress` and never reference it — no log, no column. So PPW-462 is **not** fixed here; this bolt only makes the value correct for whoever records it |
| `MetricsEndpointIpAllowListMiddleware.cs:42,55` | **Deliberately unaffected** — the scrape listener is excluded (ADR-018). Pinned by a regression test |
| Grep for `X-Forwarded`, `X-Real-IP`, `ClientIp`, `GetIp` across `src/PhotoPrint.API` | No other reader exists |
| `src/PhotoPrint.UI` | Unaffected — no client-side IP use |

**Contract: `Request.Scheme` is the connection scheme.** Changed by `XForwardedProto`, which
story 004's technical note specifies. Missed by the first draft; found by the design check.

| Consumer | Verdict |
|---|---|
| `AuthService.cs:354` — refresh-token cookie `Secure = Request.IsHttps` | **Changes, in the safe direction**: behind Caddy the cookie ships today *without* `Secure`; it will now carry it. Pinned by a new regression test. Runbook caveat: a trusted proxy that terminates plain HTTP would now set `Secure` on a cookie delivered over HTTP and the browser would drop it — §16 says to enable `TrustedProxies` only for a TLS-terminating edge |
| `SocialAuthService.cs:122` — same cookie, Google sign-in path | Same change, same test coverage |
| `SecurityExtensions.cs:117` `UseHsts()` | Emits HSTS once the request reads as HTTPS, duplicating the `Caddyfile:32` header. Harmless (browsers take the first) and noted in §16 |
| `SecurityExtensions.cs:118` `UseHttpsRedirection()` | Inert before and after: no HTTPS port is configured (`ASPNETCORE_URLS` is http-only), and a request that already reads as HTTPS is not redirected either way. No redirect loop |
| `Request.Host` / `GetDisplayUrl` / `Url.Action` / `Url.Link` | No consumer exists — every absolute URL comes from configured `App:BaseUrl`. `XForwardedHost` is therefore deliberately **not** enabled |

**Contract: OTel package versions.** `ObservabilityExtensions` is the only wiring site; the
exporter and instrumentation registration APIs are unchanged across 1.11→1.15 and are proven by
the existing `MetricsEndpointIntegrationTests` (a real exposition, and a business counter
reaching it).

---

### Failure-mode table

Named tests are written **with** the change. Carried into `test-walkthrough.md` with the real
test names filled in.

| What can fail | What should happen | Which test proves it | What log line fires |
|---|---|---|---|
| A `TrustedProxies` entry is unparseable, or a CIDR has host bits set | Boot aborts with an `OptionsValidationException` naming the entry | `ForwardedHeadersSettingsValidatorTests` — unparseable case + host-bits case | `OptionsValidationException` at startup |
| `TrustedProxies` set while observability is on and `ScrapePort` is 0 (the exclusion could not fire) | Boot aborts naming both keys | `ForwardedHeadersSettingsValidatorTests` — scrape-port conflict case | `OptionsValidationException` at startup |
| `TrustedProxies` empty (operator forgot it) | Middleware not registered; behaviour identical to today; a Production boot warns | `ForwardedHeadersExtensionsTests` — empty list configures nothing | `forwarded_headers.disabled` (Warning in Production) |
| The known-proxy lists are not cleared, so a loopback default is left trusted | `KnownNetworks`/`KnownProxies` contain only configured entries | `ForwardedHeadersExtensionsTests` — options-shape test (cleared lists, `ForwardLimit == 1`) | — |
| A CIDR entry is converted to the wrong `IPNetwork` type and matches nothing | An address inside a `/16` is accepted as a trusted proxy through the middleware | `ForwardedHeadersIntegrationTests` — CIDR-entry case | — |
| A client spoofs `X-Forwarded-For` from an **untrusted** peer | Header ignored; the resolved client IP stays the peer | `ForwardedHeadersIntegrationTests` — untrusted-peer case | — |
| A client injects extra `X-Forwarded-For` entries **through** the trusted proxy | Only the rightmost entry — what the proxy observed — is honoured | `ForwardedHeadersIntegrationTests` — injected-chain case | — |
| `X-Forwarded-For` is used to claim an allow-listed scraper address | `/metrics` still 403 — the gate reads the true peer | `MetricsEndpointIntegrationTests.Forwarded_for_cannot_open_the_scrape_gate` | `metrics.scrape.denied ip=<true peer>` |
| Forwarded headers configured, real scraper still allow-listed | `/metrics` still 200 — no regression | `MetricsEndpointIntegrationTests` — allow-listed-peer case | — |
| The refresh cookie loses its `Secure` flag behind a TLS edge | `X-Forwarded-Proto: https` from a trusted proxy produces `Secure` | `ForwardedHeadersIntegrationTests` — cookie-secure case | — |
| A `PackageReference` keeps an inline `Version=` under CPM | Restore fails (`NU1008`, already an error) | Build gate — restore is the test; recorded in the test report | MSBuild error |
| A requested package version does not exist (the Stripe.net 46.3.0 class) | Restore fails, not warns | Build gate — `NU1603` promoted to error | MSBuild error |
| CPM silently moves a package in the API's shipped closure | The assets diff catches it before merge | `project.assets.json` before/after diff, recorded in the walkthrough | — |
| The Docker image build restores without the central manifest | Image build fails loudly at `dotnet restore` | Verified by inspection and by the `.dockerignore` check; **no Docker daemon here** — stated as a suite gap | MSBuild error |
| An OTel 1.15 package changes exporter registration | `/metrics` stops serving an exposition | Existing `MetricsEndpointIntegrationTests` (6 cases incl. a business counter) | — |
| Stripe.net pin diverges from what the webhook code compiles against | Webhook suite reddens | Existing `PhotoPrint.Tests.Unit.Controllers` webhook suite | — |

---

### Design check — findings and disposition

One adversarial agent, fresh context, run against the first draft of this plan before any code
(`bolt-process.md` stage-2 gate). Nineteen findings; six raised as blockers. Disposition:

| # | Finding | Disposition |
|---|---|---|
| 1 | Keying the metrics exclusion on the configured path silently disables forwarded headers site-wide when the path is `/` or empty | **Adopted** — keyed on the scrape listener port instead, plus a boot-time conflict check for `ScrapePort = 0` |
| 2 | The rate limiter counts static assets, and the 100/min budget has never been exercised per-client | **Adopted as documentation + a boot log**, not as a policy change: the limiter's numbers are intent 029 P08. §16 makes reviewing the budget a required rollout step |
| 3 | Per-IP partitions keyed on a raw IPv6 address are both an evasion route and unbounded growth | **Recorded, deferred to 029 P08** with the reason written down. It is the limiter's partitioning policy, not this bolt's wiring; .NET 8 reclaims idle partitions, so growth is bounded by request rate within one idle window, not permanently. Reported to the coordinator |
| 4 | "Trusted proxy = the whole Compose subnet" lets any container on the bridge forge the client IP | **Adopted** — the compose file declares a subnet and a fixed Caddy address; the runbook documents a `/32`, with the collision caveat stated |
| 5 | `XForwardedProto` changes `Request.IsHttps`, which drives the refresh cookie's `Secure` flag — two callers the sweep missed | **Adopted** — both callers added to a new scheme contract table, a regression test added, and the plain-HTTP-edge caveat documented. `XForwardedProto` is kept: it is story-specified and the change fixes a live defect |
| 6 | CPM transitive pinning lets the Tests project's versions govern the API's shipped closure | **Adopted** — the rule is now "pin what the API already resolves", verified by an assets diff |
| 7 | Two of the four vulnerable transitive packages are test-only xunit shims that never load | **Adopted** — split into shipped vs test-only, each labelled |
| 8 | Pinned versions and advisory fixed-in versions are unverified | **Adopted** — OTel versions confirmed against the nuget.org flat container; every pin is validated by re-running the scan, and any pin that does not clear its advisory is reverted and recorded as a residual |
| 9 | `WarningsAsErrors` clobbers instead of appending; `NU1009` is the wrong code; `NU1605`/`NU1008` are already errors; `NU1608` is risky | **Adopted in full** — `$(WarningsAsErrors);NU1603` only |
| 10 | `DEPLOYMENT.md` already has a §15 (invoicing), cross-referenced from ten places | **Adopted** — the new runbook section is §16 |
| 11 | ADR-018 needs the exclusion as an invariant; §14.3's absolute sentence becomes false; `bolt.md` still lists the criterion D4 refuses | **Adopted** — all three updated |
| 12 | `ScrapeIpAllowList` holds `System.Net.IPNetwork`; `ForwardedHeadersOptions` on net8.0 wants `Microsoft.AspNetCore.HttpOverrides.IPNetwork` | **Adopted** — explicit conversion, with a test that a `/16` matches through the middleware |
| 13 | The fix ships switched off and nothing in the criteria turns it on | **Adopted** — `.env.example` ships the real value uncommented (possible now that Caddy has a fixed address), and §16 makes it a rollout step |
| 14 | A better test seam exists than the 429-based one: a startup filter that reads `RemoteIpAddress` *after* awaiting the pipeline | **Adopted** — the rate-limiter test is dropped in favour of the direct probe plus an options-shape unit test. Harness caveats noted (`ObservabilityHostCollection`, the factory's `PermitLimit = 10000`) |
| 15 | Every `[EnableRateLimiting]` attribute is dead, because `UseRateLimiter` runs before `UseRouting` — login has no brute-force protection | **Recorded, not fixed** — it is a pipeline-ordering change in `Program.cs`, which is bolt 055/029 P08. The PPW-461 re-deferral note below is corrected to say so, and it is reported to the coordinator as a discovered risk |
| 16 | A path-keyed exclusion would add a third place tracking the metrics path | Moot — finding 1's fix removes it |
| 17 | A standalone `OpenTelemetry.Api` pin will rot outside its lockstep suite | **Adopted** — pin dropped |
| 18 | `TrustedProxies` inherits §14.5's indexed-env-var merge trap | **Adopted** — the default array is empty in `appsettings.json` too, and §16 repeats the warning |
| 19 | Anti-spoofing layer 4 rests on a floating `caddy:2-alpine` tag and breaks under a CDN in front of Caddy | **Adopted as runbook notes** in §16 |

Two findings the check verified as *clean* are load-bearing and worth recording: `ForwardLimit = 1`
does take the rightmost entry (the anti-spoofing story does not invert), and a `UseWhen` branch's
mutation of `HttpContext.Connection` does survive the rejoin, because the branch shares one
feature collection.

---

### Backlog sweep (`reviews/state/backlog.md`, rows in the Areas this bolt touches)

Areas touched: `edge` (client-IP handling, metrics gate), `observability` (OTel packages).
Rows are **not** edited here — the coordinator writes re-deferral notes at merge time.

| Row | Disposition |
|---|---|
| PPW-460 🔴 Global rate limiter partitions on `RemoteIpAddress`; behind Caddy one value for all traffic | **Pulled in (keying half).** Forwarded headers make the partition key the real client. The limiter's own budget, its static-asset exposure and IPv6-prefix keying stay with intent 029 P08, all three written down in §16 |
| PPW-462 🟠 Security-audit log records Caddy's address as the client IP | **Re-deferred — the stage-4 gate corrected an earlier claim that this was pulled in.** There is no audit sink: the three services take `ipAddress` and never use it, so nothing records Caddy's address *or* the client's. This bolt makes the input correct; the row stays open until something writes it |
| PPW-461 🔴 Auth limiters are unpartitioned `AddFixedWindowLimiter` calls — site-wide budgets | **Re-deferred**, with the reason corrected by the design check: the sharper problem is that they never run at all. `UseRateLimiter()` (`SecurityExtensions.cs:122`, via `Program.cs:378`) executes before `UseRouting()` (`:391`), so `GetEndpoint()` is null and every `[EnableRateLimiting]` policy on `AuthController` and in `AuthExtensions` is inert — `/api/auth/login` has no brute-force limit today. **Routed to bolt 055 by owner ruling 2026-09-04** (the Program.cs rewrite in Wave 2), not to 063 |
| PPW-467 🟡 `/health` proxied ungated, echoes each check's `Data` bag | **Re-deferred**: health-endpoint exposure, unrelated to dependencies or client-IP resolution |
| PPW-428 🟡 `Verdict` counts ports, not reachability | **Re-deferred**: bolt 044's scrape-gate diagnostic; untouched here |
| PPW-429 🟡 `PrometheusEndpoint` coupled to the `Caddyfile` path by comment only | **Re-deferred**: this bolt adds a `Caddyfile` line but does not change the metrics path or its matcher, and the listener-keyed exclusion deliberately avoids adding a third coupled site |
| PPW-396 🟡 `wrong_listener` / `not_allowed` denials share one 512-entry log budget | **Re-deferred**: allow-list middleware internals; unmodified |
| PPW-388 🟡 `MaskedForm` suggests an `::ffff:…/112` form the parser rejects | **Re-deferred**: `ScrapeIpAllowList` gains two accessors only; its parse and diagnostic logic is untouched |
| PPW-400 ⚪ Empty allow-list entry error names neither value nor index | **Re-deferred**: same reason as PPW-388 |
| PPW-362 🟡 `PrometheusEndpoint="/"` passes validation and would gate the whole site | **Re-deferred as a defect, designed around here**: the listener-keyed exclusion means a `/` path cannot disable forwarded headers. The validator gap itself stays with bolt 044's area |
| PPW-359/360/361/364/365/366/368–374/391/398/399/401/406/407/408/430/431 (`observability`) | **Re-deferred**: bolt 044/045 instrumentation-behaviour rows. This bolt changes OTel *package versions*, not the wiring or the scrubbing |
| PPW-134 ⚪ Client-abort log reads the raw correlation-id item | **Re-deferred**: upload-path logging, no overlap |

---

### Explicitly out of scope, with reasons

- **A CI gate on `dotnet list package --vulnerable`.** The unit brief's chosen mechanism is
  Renovate's `vulnerabilityAlerts` (story 003). A build that goes red the hour a new advisory
  publishes is a new mechanism and would ship at the D-o-D rule-2 bar. Recommended as follow-up.
- **Replacing `Microsoft.AspNetCore.Identity` 2.3.1** (the legacy 2.x shim, and the root of the
  `System.Security.Cryptography.Xml` advisories). Only `IPasswordHasher<T>` / `PasswordHasher<T>`
  are used, which live in `Microsoft.Extensions.Identity.Core` — but moving from the 2.3.0
  hasher to the 8.0.x one changes the PBKDF2 iteration-count default (10k → 100k), a real change
  on the password path and outside this bolt's stories. Recorded with that caveat; the
  transitive pin is the in-scope mitigation.
- **Upgrading xunit past `NETStandard.Library` 1.6.1**, the true fix for the two test-only
  advisories.
- **The global rate limiter's policy** — its budget, its static-asset exposure, its IPv6
  keying, and the dead `[EnableRateLimiting]` attributes (intent 029 P08 / bolt 055).
- **Stripe.net 47 → 52**, Sentry/AWS/EF majors: deferred to the Renovate cadence this bolt builds.
- **An EF migration**: none is needed.

---

### Key decisions

- **D1 — 1.15.x, not 1.18.x.** Story 001 specifies it; Renovate carries it forward.
- **D2 — One file, not two.** CPM needs only `Directory.Packages.props`; story 002's note
  suggests also creating `Directory.Build.props`, which would add an empty file with no job.
- **D3 — Stripe.net pinned to 47.0.0**, the version already resolving, so the pin changes no
  behaviour and the "46→47 may break" risk is void.
- **D4 — The scrape listener is excluded from forwarded headers**, against story 004's third
  acceptance criterion and in favour of ADR-018's 2026-07-31 amendment.
- **D5 — Empty `TrustedProxies` means the middleware is not registered.** Opt-in, so no
  environment silently changes how it resolves a client IP — paired with a shipped
  `.env.example` value and a rollout step so opt-in does not become never.
- **D6 — Two added lines in `Program.cs`**, both calling new extension methods, matching
  `AddObservability` / `UseSecurityBaselines`. Nothing existing is moved.
- **D7 — The API's resolved package closure is the CPM tie-breaker.** Where the two projects
  differ, the central pin is what the API already resolves.

---

### Acceptance criteria

- [ ] `dotnet list package --vulnerable` reports zero packages; `--include-transitive` is clean
      or every residual is named with the reason it could not be pinned.
- [ ] `dotnet restore` succeeds with no `NU1603`, and no `Version=` attribute remains in either
      `.csproj`; a reintroduced inline version fails the build.
- [ ] Stripe.net resolves to exactly one version across both projects.
- [ ] The API's resolved package closure is unchanged, or every moved row is listed with a reason.
- [ ] `Dockerfile` and `ci.yml` reconciled with the manifest's new home.
- [ ] `.github/renovate.json` exists with the three groups, the two schedules, the dashboard and
      labelled non-auto-merged security PRs.
- [ ] `UseForwardedHeaders` runs before `UseCorrelationId`; `KnownNetworks`/`KnownProxies`
      cleared and repopulated only from `ForwardedHeaders:TrustedProxies`; `ForwardLimit = 1`.
- [ ] A spoofed `X-Forwarded-For` from an untrusted peer changes nothing; from a trusted proxy
      the resolved client IP follows the rightmost entry.
- [ ] `X-Forwarded-For` cannot open the `/metrics` gate, and an allow-listed peer still scrapes.
- [ ] `TrustedProxies` + observability on + `ScrapePort = 0` fails boot.
- [ ] The refresh cookie carries `Secure` when `X-Forwarded-Proto: https` arrives from a trusted
      proxy.
- [ ] The scoped test suites are green.
- [ ] `DEPLOYMENT.md` §14.3 amended and §16 added; ADR-018 amended; `.env.example`,
      `docker-compose.prod.yml` and the `Caddyfile` match.

---

### Self-validation (specsmd human checkpoint, validated by the executing agent)

Stories reviewed against the real restore output; deliverables enumerated per story;
dependencies identified (NuGet, GitHub/Renovate app, Caddy); acceptance criteria testable;
caller sweep covers three contracts with no blank rows; failure-mode table complete; backlog
swept. The stage-2 adversarial gate ran as a fresh subagent, and all nineteen findings are
dispositioned above — six adopted as design changes before any code, three recorded as deferred
with reasons, the rest folded in as documentation or tests. One story acceptance criterion (D4)
is consciously unmet and recorded rather than silently resolved. **Approved to proceed to
stage 2.**
