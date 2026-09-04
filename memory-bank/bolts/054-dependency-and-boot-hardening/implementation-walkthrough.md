---
stage: implement
bolt: 054-dependency-and-boot-hardening
created: 2026-09-04T00:10:00Z
---

## Implementation Walkthrough: Dependency & Boot Hardening

### Summary

Four stories, in the order the bolt required. The OpenTelemetry suite moved to 1.15.x and the
known advisory is gone. Package versions left the project files for a single central manifest,
where Stripe.net is now pinned to the version both projects were already silently resolving.
Renovate will open grouped, scheduled upgrade PRs against that manifest. And the API now resolves
the real client IP behind a configured reverse proxy — everywhere except the metrics scrape
listener, which is deliberately excluded so the allow-list keeps judging the address a request
actually came from.

### Structure Overview

Stories 001–003 are dependency and build-configuration work: two project files, one new central
manifest, one Renovate config, and the two places that had to learn where versions now live (the
Docker restore stage and the CI cache key).

Story 004 adds one new mechanism, shaped like every other optional integration in this codebase:
a settings class bound to a config section, an `IValidateOptions` implementation that fails boot
on a bad value, and an extension pair — one call in the service registration, one in the
pipeline. It is off by default: an empty trusted-proxy list means the middleware is never
registered at all, so no existing environment changes behaviour until an operator opts in. The
deployment side gives that operator a fixed address to trust rather than a whole container
subnet.

### Completed Work

- [x] `Directory.Packages.props` — the single home for every package version, with transitive
      pinning on and the one restore warning that mattered promoted to an error.
- [x] `src/PhotoPrint.API/PhotoPrint.API.csproj` — versions removed; OTel references bumped.
- [x] `src/PhotoPrint.Tests/PhotoPrint.Tests.csproj` — versions removed.
- [x] `Dockerfile` — the API restore stage copies the central manifest, without which the image
      build fails.
- [x] `.github/workflows/ci.yml` — the NuGet cache key covers the manifest, so a version bump
      no longer reuses a stale cache.
- [x] `.github/renovate.json` — dependency dashboard, three package groups, monthly routine and
      quarterly major schedules, security advisories labelled and never auto-merged.
- [x] `src/PhotoPrint.API/Extensions/ObservabilityExtensions.cs` — dropped the EF instrumentation
      option the 1.15 package removed; the SQL text it used to opt into is emitted by default now.
- [x] `src/PhotoPrint.API/Configuration/ForwardedHeadersSettings.cs` — the trusted-proxy list.
- [x] `src/PhotoPrint.API/Validators/ForwardedHeadersSettingsValidator.cs` — rejects a malformed
      entry, and rejects a trusted proxy declared without a metrics scrape listener.
- [x] `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs` — builds the middleware
      options and registers the middleware everywhere except the scrape listener.
- [x] `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs` — warns, once per
      distinct peer and capped, when something sends `X-Forwarded-For` from an address that is
      not trusted. This is how a drifted proxy address becomes visible instead of silently
      reverting the whole feature.
- [x] `src/PhotoPrint.API/Observability/ScrapeIpAllowList.cs` — exposes its parsed addresses and
      networks so the new options reuse the existing IP/CIDR rules instead of re-implementing
      them, handing out a read-only view rather than the internal array.
- [x] `src/PhotoPrint.API/Program.cs` — two added lines, nothing moved.
- [x] `src/PhotoPrint.API/appsettings.json` — the section, defaulting to trusting nothing.
- [x] `docker-compose.prod.yml` — an explicit network subnet and a fixed address for Caddy.
- [x] `Caddyfile` — replaces the forwarded-for header rather than appending to it.
- [x] `.env.example` — ships the trusted-proxy value, so a deploy that follows the runbook gets
      the fix switched on.
- [x] `docs/DEPLOYMENT.md` — §14.3 narrowed, new §16, contents and repo inventory updated.
- [x] `memory-bank/bolts/044-tracing-and-metrics/adr-018-*.md` — a third invariant.
- [x] `memory-bank/standards/decision-index.md` — the ADR-018 entry says the same thing.
- [x] `memory-bank/intents/.../stories/004-forwarded-headers-metrics.md` — the acceptance
      criterion this bolt refuses, recorded as superseded with its replacement.
- [x] `src/PhotoPrint.Tests/Unit/Configuration/ForwardedHeadersSettingsValidatorTests.cs`
- [x] `src/PhotoPrint.Tests/Unit/Configuration/ForwardedHeadersOptionsTests.cs`
- [x] `src/PhotoPrint.Tests/Integration/ForwardedHeadersIntegrationTests.cs`
- [x] `src/PhotoPrint.Tests/Integration/MetricsEndpointIntegrationTests.cs` — two cases pinning
      that forwarded headers cannot open the scrape gate.

### Key Decisions

- **The scrape exclusion needs both the listener port and the metrics path.** Neither alone is
  safe, and the two failure modes point in opposite directions. Path-only is a trap: the
  configured path may be `/`, and an env var set to empty yields `""`, either of which matches
  every request and would silently disable forwarded headers site-wide. Port-only is also a
  trap, found by the fresh-eyes gate: an operator who satisfies the new boot validator by
  setting the scrape port to the port the API actually serves would exclude every request on
  that listener, killing the feature while the boot log still claimed it was on. Requiring both
  bounds each failure to something harmless.
- **Boot fails when a trusted proxy is declared without a scrape listener.** That combination is
  the one case where the exclusion cannot fire. ADR-018 already required a proxied deployment to
  bind a listener the proxy does not route; nothing enforced it until now.
- **Trust the proxy's address, not the container subnet.** The API's ports are exposed on that
  network, so trusting the range would let any container on it name the client — an unlimited
  rate-limit budget for itself, a targeted denial of service against one customer's partition,
  and forged addresses in the audit trail. The compose file pins a subnet so a `/32` is knowable.
- **Off by default, but shipped switched on.** An empty list registers nothing, so no existing
  environment changes; `.env.example` carries the real value so a fresh deploy is correct.
- **Pin what the API already resolves.** Where the two projects disagreed on a shared package,
  the central version is the one the API was resolving, so the production closure does not move.
  Verified: the only change to the API's resolved packages is the intended security pin.
- **Stripe.net pinned at 47.0.0, not 52.x.** 47.0.0 is what both projects were already running;
  majors are the Renovate cadence's job.

### Corrections made by the fresh-eyes gate

Two agents read the whole diff with fresh context. What they changed:

- **The audit-trail claim was false.** The plan and the runbook said this bolt fixes the
  security-audit log's client IP. It does not: `AuthController` computes the address and passes
  it to `AuthService`/`SocialAuthService`, which accept the argument and **never use it** — no
  log line, no column. The change makes the input correct; nothing consumes it. Every claim to
  the contrary is corrected, and the backlog row for it stays open rather than being claimed.
- **The exclusion predicate gained the path conjunct** (see Key Decisions), because the
  port-only form could be disabled by a plausible misconfiguration.
- **The new mechanism had no runtime signal** — only a boot-time state log, which cannot tell
  you the proxy address later drifted. Hence `UntrustedForwardedPeerMiddleware`.
- **Smaller fixes**: the boot log printed a permit limit of 0 when the `RateLimit` section was
  absent, where the limiter itself falls back to 100; parse errors were discarded when building
  the middleware options; `Networks` handed out its internal array; the "feature is off" warning
  only fired in Production, not Staging.

### Deviations from Plan

- **`OpenTelemetry.Api` is not pinned separately.** The plan's design check predicted the suite
  bump would carry it, and the scan confirms it did. A standalone pin would have put one member
  of a lockstep suite outside the set.
- **The rate-limiter-based wiring test was replaced** by a direct probe that reads the resolved
  client identity after the pipeline has run. It asserts the thing itself rather than a 429 as a
  proxy for it, and does not couple the test to the limiter's budget.
- **`Directory.Build.props` was not created.** Story 002's note suggests it; Central Package
  Management needs only `Directory.Packages.props`, and the `WarningsAsErrors` property in it was
  verified to reach both projects (an intentionally bad version was rejected as an error, not a
  warning).
- **One acceptance criterion of story 004 is refused**, not implemented — recorded in the story
  file, the ADR, and the runbook. Approved by the wave coordinator.

### Dependencies Added

- [x] No new package references. Existing ones moved to the central manifest; three transitive
      packages gained pins to clear advisories (`System.Security.Cryptography.Xml`, which ships,
      plus two netstandard shims that exist only in the test graph).

### Developer Notes

- The EF instrumentation option `SetDbStatementForText` no longer exists. The SQL text it used to
  opt into is now emitted by default, so the OTLP-side scrubbing gap recorded in the backlog is
  unchanged in effect but no longer has that switch as a lever.
- `Program.cs`'s pipeline comments number the middleware `1st` … `5th`. Forwarded headers now run
  ahead of the one labelled `1st`. Renumbering them was left alone deliberately: this bolt is
  restricted to adding its own two lines, and bolt 055 is restructuring that block.
- The two beta OTel pins are not a compromise — neither package has ever had a stable release.
- Anything that later reads a client IP, scheme or host must ask whether it wants the resolved
  client or the true peer. The metrics gate wants the peer, and is excluded for that reason.
