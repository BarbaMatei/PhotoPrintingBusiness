---
type: resolution
target: 054-dependency-hardening
version: 1
answers: review-v1.md
status: resolved
fixed_commit: 8ae0953
closed: 2026-09-04
---

# Resolution v1 — 054-dependency-hardening

Scope of this round: the 2 🔴 blockers and the 11 🟠 rows. The 23 🟡/⚪ rows
(PPW-724…PPW-746) are backlogged by the loop's router and are not answered here.

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-713 | fixed | b29fb2c | Ships `ForwardedHeaders__TrustedProxies__0=172.28.0.2` uncommented with `RateLimit__Public__PermitLimit=600`; §16.1/§16.3 and the walkthrough corrected. Per-site permit measurement stays an operator step. |
| PPW-714 | fixed | b29fb2c, 8ae0953 | `appsettings.json` ships the Console sink (CompactJsonFormatter), so §16.6's `docker compose logs` greps work. The File sink moved to a new `appsettings.Production.json` in the follow-up, so no dev or test host opens `logs/*.json`. |
| PPW-711 | deferred | — | parked: needs the owner's ruling on whether §16.7's deferral to intent 029 / bolt 063 stands. Default taken: it stands, no limiter code changed. See Decisions. |
| PPW-712 | deferred | — | parked: must land in the same change as PPW-711, or three hourly caps become site-wide budgets. Default taken: §16.7's deferral stands. See Decisions. |
| PPW-715 | fixed | 23d99d3, 8ae0953 | The validator now fails an IPv4 entry wider than `/31` (IPv6 `/127`) and any IPv6 link-local entry, naming it. The width rule sits in the forwarded-headers validator, not the shared parser, as the brief asked. §16.1/§16.2/§16.5 restated. |
| PPW-716 | fixed | 23d99d3 | Trust is judged before `next()` against the same parse that fills `KnownProxies`, so a trusted peer is never called untrusted; an unreadable rightmost entry gets its own message and its own cap. Incidentally resolves out-of-scope PPW-724. |
| PPW-717 | fixed | b29fb2c, 8ae0953 | Every `AllowedScrapeIps` example is now `172.28.0.128/25`, Compose's `ipam.ip_range` — `.env.example` and DEPLOYMENT §14.5's row and env-var block. The test asserts each example sits in that pool and excludes Caddy's pinned address. |
| PPW-718 | fixed | 0c0cc3b | `NuGetAuditMode=all`, `NuGetAuditLevel=low`, NU1901–NU1905 as errors under `FailOnAudit`, passed by the CI restore only: the brief wanted promotion *and* props left at warning. NFR rewritten; FR-1 still names the `dotnet list` check. |
| PPW-719 | fixed | bc4aa21 | Q3 and story 004's technical note now record Caddy's own address. The sweep found two more sites — story 004's acceptance criterion and the unit brief's `(CIDR)` row — corrected in 23d99d3. PPW-715 makes the drift unexploitable. |
| PPW-720 | fixed | 23d99d3, 8ae0953 | Booted host, observability on, `ScrapePort=0`, trusted proxies set: throws `OptionsValidationException` naming `Observability:Metrics:ScrapePort`. No PPW-742 dependency — at port 0 the guard returns null, so the validator is the only abort. |
| PPW-721 | fixed | 23d99d3 | Booted host, trusted proxy, multi-entry header: the identity is the rightmost entry and there are zero untrusted warnings; the untrusted companion emits exactly one. Warnings are read by re-registering the host's `ILoggerFactory`. |
| PPW-722 | fixed | 23d99d3 | Two tests split the port and path conjuncts — a non-metrics path on 9090, `/metrics` on 8080 — each asserting the client resolves to 203.0.113.9; replacing either with `true` reddens exactly one. The third conjunct is I2's. |
| PPW-723 | fixed | b29fb2c | `docker-compose.prod.yml` reserves `ip_range: 172.28.0.128/25` so Caddy's pinned `.2` sits outside the dynamic pool; §16.2 states the constraint. `api` starts first, so without it Docker hands `.2` to `api`. |

## Scope

| Cluster | Findings | Files | Protocol |
|---|---|---|---|
| A — limiter ordering + partitioning | PPW-711, PPW-712 | `src/PhotoPrint.API/Extensions/SecurityExtensions.cs`, `src/PhotoPrint.API/Extensions/AuthExtensions.cs`, `src/PhotoPrint.API/Program.cs` | — |
| B — shipped deployment defaults | PPW-713, PPW-714, PPW-717, PPW-723 | `.env.example`, `docker-compose.prod.yml`, `src/PhotoPrint.API/appsettings.json`, `src/PhotoPrint.API/appsettings.Production.json`, `docs/DEPLOYMENT.md`, `memory-bank/bolts/054-dependency-and-boot-hardening/implementation-walkthrough.md`, `src/PhotoPrint.Tests/Unit/Configuration/DeploymentDefaultsTests.cs` | — |
| C — proxy-trust decision | PPW-715, PPW-716, PPW-719, PPW-720, PPW-721, PPW-722 | `src/PhotoPrint.API/Validators/ForwardedHeadersSettingsValidator.cs`, `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs`, `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs`, `src/PhotoPrint.API/Configuration/TrustedProxyList.cs`, `src/PhotoPrint.Tests/Unit/Configuration/ForwardedHeadersSettingsValidatorTests.cs`, `src/PhotoPrint.Tests/Unit/Configuration/ForwardedHeadersOptionsTests.cs`, `src/PhotoPrint.Tests/Unit/Middleware/UntrustedForwardedPeerMiddlewareTests.cs`, `src/PhotoPrint.Tests/Integration/ForwardedHeadersIntegrationTests.cs`, `src/PhotoPrint.Tests/Helpers/LogCapture.cs`, `docs/DEPLOYMENT.md` §16.1–§16.5, `memory-bank/bolts/054-dependency-and-boot-hardening/test-walkthrough.md`, `memory-bank/intents/025-security-dependency-hygiene/**` | Proxy trust |
| D — dependency-audit gate | PPW-718 | `Directory.Packages.props`, `.github/workflows/ci.yml`, `memory-bank/intents/025-security-dependency-hygiene/requirements.md`, `src/PhotoPrint.Tests/Unit/Configuration/DeploymentDefaultsTests.cs` | — |

## Decisions

### Protocol — Proxy trust

Surface: the **configured trusted-proxy list** and the one decision every request makes against it — *may this TCP peer tell the API who the client is?* PPW-715 admits the list, PPW-716 judges a peer against it, PPW-719 records what to put in it, PPW-720/721/722 pin the wiring.
States at the forwarded-headers branch: **S1** trusted peer, no header ⇒ identity is the peer. **S2** trusted, rightmost entry parses ⇒ identity is that entry. **S3** trusted, header present but no entry parses (`unknown`, empty value, all-empty segments) ⇒ identity is the peer. **S4/S5** untrusted, header absent / present ⇒ identity is the peer. **S6** scrape listener + metrics path, observability on ⇒ identity is the peer, always.
- I1 — the identity is **never** attacker-chosen: it comes from `X-Forwarded-For` in S2 only, and only from the rightmost entry (`ForwardLimit = 1`).
- I2 — in S6 the identity is **always** the real peer; the exclusion predicate has **three** conjuncts — scrape port configured, local port equal to it, request path under the metrics path — and none alone may decide it. `scrapePort != 0` is redundant in production, where a configured port is never 0, but load-bearing under `TestServer`, whose `Connection.LocalPort` is 0 for every request.
- I3 — the list **never** admits a CIDR wider than one address pair (IPv4 `/31`, IPv6 `/127`), nor an IPv6 link-local entry; a bare address is always allowed. Every host of a wider range reaches the API's exposed ports directly.
- I4 — a peer in the list is **never** reported `untrusted_peer`, whatever its forwarded value says and whatever the registration order is.
- I5 — **at most one** warning per (peer, cause) per process, each cause with its own message *and its own log cap*: `untrusted_peer` for S5, `unparseable_forwarded_for` for S3. Neither cause may starve the other.
- I6 — trust is decided **only** from the bound `ForwardedHeadersSettings`, and **exactly one** parse feeds both the middleware's trust set and `KnownProxies`/`KnownNetworks`, so the two oracles cannot disagree and no rule re-reads the peer after `UseForwardedHeaders`.
- I7 — boot **never** succeeds with trusted proxies set, observability on and `ScrapePort=0` — the one combination in which S6 cannot happen.
Rules, in order. (1) Boot admits the list: one `IValidateOptions` gate, made eager by the `.Value` read in `UseTrustedProxyForwardedHeaders`, enforcing I3 and I7. (2) Boot builds the trust set from the bound options and fills `KnownProxies`/`KnownNetworks` from that same parse (I6); the eager `configuration.Get<string[]>()` read and its throw go. (3) Per request, `IsMetricsScrape` = scrape port set **and** local port equals it **and** path under the metrics path ⇒ neither middleware runs (I2). (4) Per request, **before** `next(context)`, judge the peer against the parsed list: untrusted with a declared header ⇒ warn `untrusted_peer` once; trusted with a declared header whose rightmost entry fails `IPEndPoint.TryParse` (the framework's own predicate) ⇒ warn `unparseable_forwarded_for` once; otherwise silent. (5) `UseForwardedHeaders` then rewrites the identity, in S2 only; rules 4 and 5 are order-independent (I6).
The cluster's invariant test drives one booted host through the composed flow — trusted peer with a multi-entry header, trusted peer with an unparseable one, untrusted peer with a valid one — asserting the resolved identity and the warning set together, not one mechanism at a time.

### Revert proofs

One lever each, restored with `git checkout --` after the run; the two `revert-and-rerun` runs each
paired two levers whose failing tests are disjoint, so every red line below is attributable.

- **PPW-715 / PPW-716** — the pre-fix tree is the proof: the red run (32 passed, 7 failed) failed
  the four width/link-local validator cases and the three cause cases (`…unparseable_forwarded_value…`,
  `…an_empty_forwarded_value…`, `An_exhausted_untrusted_cap_does_not_silence_parse_failures`).
- **PPW-720** — delete the `AddSingleton<IValidateOptions<ForwardedHeadersSettings>>` registration
  (2 lines): `Trusted_proxies_with_observability_on_and_no_scrape_listener_aborts_boot` and
  `An_unparseable_trusted_proxy_aborts_boot` fail (run 36/3).
- **PPW-721** — negate the `!_trustedProxies.Trusts(peer)` condition:
  `Trusted_proxy_with_multi_entry_forwarded_for_emits_no_untrusted_warning` fails, with
  `An_untrusted_peer_cannot_name_the_client` and 7 middleware cases (run 28/11).
- **PPW-722** — replace one `IsMetricsScrape` conjunct with `true`: the port conjunct reddens
  `The_metrics_path_on_the_public_port_still_resolves_its_client` (36/3), the path conjunct
  `A_non_metrics_path_on_the_scrape_port_still_resolves_its_client` (28/11) — one test per conjunct.

### Known limits of this round's evidence

- `LogCapture` replaces the host's `ILoggerFactory` with a provider whose `IsEnabled` is always
  true, so Serilog's own `MinimumLevel` is bypassed rather than observed: the `Warnings(...) == 0`
  halves of `Trusted_proxy_with_multi_entry_forwarded_for_emits_no_untrusted_warning` and
  `A_single_pair_cidr_entry_trusts_both_of_its_addresses` would still pass if a production level
  filter suppressed the line. `An_untrusted_peer_cannot_name_the_client` is the positive control on
  the same factory, so the seam is proven to carry a warning that is emitted.
- PPW-718's restore gate is proven by configuration assertions plus `CiRestore_HardFailsOnAuditWarnings`,
  not by pinning a package with a live advisory on a scratch branch and watching CI fail — that needs
  a real advisory and a CI run, neither available here.
- PPW-721's test name names only the warning half; the identity half is asserted in the same test
  (`resolved.ClientIp`).

### Round review — folded in, and recorded without a code change

Fixed from the round review: the scrape example narrowed to the `ip_range` pool (its test now asserts
pool containment and Caddy's exclusion), the File sink moved out of `appsettings.json` into
`appsettings.Production.json` with a theory pinning that dev and test hosts write no log files, and the
validator `continue`s after a width failure so one entry is never named twice. Recorded only:

- `.env.example`'s empty `TrustedProxies__0=` binds to `[""]`, which the validator rejects, so filling
  nothing in aborts boot while the file never says to delete the line instead — a doc gap, and touching
  the shipped defaults again after PPW-713 pinned them is a wider change than this round.
- `UntrustedForwardedPeerMiddleware` re-parses the rightmost forwarded entry on every request that
  declares the header; the per-(peer, cause) cap bounds the logging, not the parse. No change: that parse
  is what makes the cause distinction correct, and it is one `IPEndPoint.TryParse` on a span.
- The round reviewer ran no build and no test, so the revert proofs above and both Parked blocks were
  taken at face value, and it did not read `ScrapeListenerCheck`'s port-0 branch. The final scoped run is
  the only execution evidence for the follow-up commit.

### Parked (unattended round) — PPW-711 + PPW-712, the limiter change

`docs/DEPLOYMENT.md` §16.7 item 3 discloses the ordering defect and defers it to intent 029 /
bolt 063. **Question for the owner: is that deferral upheld, or does the limiter redesign
move into this bolt?** Under the unattended variant both rows are parked `deferred` rather
than fixed. Default taken: uphold the recorded deferral, change no limiter code. Fixing
PPW-711 alone is forbidden — it arms three hourly caps that PPW-712 shows are un-partitioned,
turning them into site-wide budgets (5 registrations/hour for the whole internet). The two
must land in one change or neither.

### Parked (noticed outside the finding set) — four CI gaps

The PPW-718 approach pre-check named four gaps that no finding covers. They are recorded here
rather than minted as backlog rows, because routing them is the owner's call, and PPW-718 is
**not** widened to cover them:

1. No `global.json`, so the SDK — and with it the NuGet-audit defaults — drifts with the
   GitHub runner image.
2. `ci.yml` never runs on pushes to `main` (`branches-ignore: [main]`), which makes
   `deploy.yml`'s `workflow_run` gate dead and leaves `main` unaudited.
3. `.github/renovate.json` needs `osvVulnerabilityAlerts` so it does not depend on GitHub
   Dependabot alerts, which intent Q1 still records as Pending.
4. The `web` CI job runs no `npm audit`, so the frontend closure has no advisory gate at all.

**Question for the owner: file these four as backlog rows, or drop them?** Default taken:
recorded here only.
