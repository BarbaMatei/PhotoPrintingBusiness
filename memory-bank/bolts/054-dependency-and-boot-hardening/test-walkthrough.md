---
stage: test
bolt: 054-dependency-and-boot-hardening
created: 2026-09-04T01:10:00Z
---

## Test Report: Dependency & Boot Hardening

### Summary

- **New tests**: 24 (13 unit, 11 integration — 9 new forwarded-header cases plus 2 added to the
  existing metrics-gate suite)
- **Scoped runs**: all green; the full `Integration` namespace passes 240/240 with 10 MinIO
  tests skipping, as they do without `STORAGE_TEST_*`
- **Vulnerability scan**: clean on both projects, direct **and** transitive
- **Build**: 0 errors, 4 warnings — all four pre-existing, in files this bolt does not touch
  (`PostgresInvoiceNumberingService.cs` EF1002, `RazorTemplateServiceTests.cs` CS1998,
  `UploadCleanupJobTests.cs` CS8604 ×2)

### Commands run, and what they returned

| Command | Result |
|---|---|
| `dotnet restore PhotoPrint.sln` | Restored, no warnings. The two `NU1603` warnings that were there before this bolt (both projects requesting a Stripe.net version that does not exist) are gone |
| `dotnet list package --vulnerable` | `PhotoPrint.API`: none. `PhotoPrint.Tests`: none |
| `dotnet list package --vulnerable --include-transitive` | `PhotoPrint.API`: none. `PhotoPrint.Tests`: none |
| `dotnet build PhotoPrint.sln -c Debug` | Build succeeded, 0 errors, 4 pre-existing warnings |
| `dotnet test … --filter "FullyQualifiedName~PhotoPrint.Tests.Unit.Controllers"` | 59/59 passed |
| `dotnet test … --filter "…~Metrics\|…~Observability\|…~Sentry"` | 223/223 passed |
| `dotnet test … --filter "…~Configuration\|…~Payment\|…~Webhook\|…~Stripe"` | 248/248 passed |
| `dotnet test … --filter "…~ForwardedHeaders\|…~Metrics\|…~Observability\|…~Unit.Controllers"` | 221/221 passed |
| `dotnet test … --filter "FullyQualifiedName~PhotoPrint.Tests.Integration"` | 240 passed, 10 skipped, 0 failed |

No `npm test`: no frontend file is touched. No full-suite run.

### Test Files

- [x] `src/PhotoPrint.Tests/Unit/Configuration/ForwardedHeadersSettingsValidatorTests.cs` — the
      boot-time rejections: malformed entries, host bits set on a CIDR, octal-looking addresses,
      and the trusted-proxy-without-a-scrape-listener conflict.
- [x] `src/PhotoPrint.Tests/Unit/Configuration/ForwardedHeadersOptionsTests.cs` — the shape of
      the options the extension builds: framework loopback defaults cleared, one hop, only the
      two headers honoured, and a CIDR entry that really matches its members.
- [x] `src/PhotoPrint.Tests/Integration/ForwardedHeadersIntegrationTests.cs` — what the pipeline
      resolves for a real request, read after the whole pipeline has run.
- [x] `src/PhotoPrint.Tests/Integration/MetricsEndpointIntegrationTests.cs` — two cases added,
      pinning that forwarded headers cannot open the scrape gate and do not break the scraper.

### Failure-mode table, with the tests that prove each row

Carried from `implementation-plan.md` with the real names filled in. No empty cells.

| What can fail | What should happen | Test that proves it | Log line |
|---|---|---|---|
| A `TrustedProxies` entry is unparseable | Boot aborts naming the entry | `ForwardedHeadersSettingsValidatorTests.An_unparseable_entry_fails_validation` | `OptionsValidationException` |
| A CIDR entry has host bits set | Boot aborts, suggesting the masked form | `…A_cidr_range_with_host_bits_set_fails_validation` | `OptionsValidationException` |
| A leading-zero address is read as octal | Boot aborts rather than silently trusting a different network | `…A_leading_zero_form_fails_rather_than_becoming_an_octal_address` | `OptionsValidationException` |
| Trusted proxies set while the scrape path is served on every listener | Boot aborts naming both keys | `…Trusted_proxies_without_a_scrape_listener_fails_validation` | `OptionsValidationException` |
| …and the two cases that must **not** abort | Boot proceeds | `…Trusted_proxies_with_a_scrape_listener_is_valid`, `…Trusted_proxies_with_observability_off_does_not_require_a_scrape_listener` | — |
| `TrustedProxies` empty | Nothing is trusted; behaviour identical to before the bolt | `ForwardedHeadersOptionsTests.Nothing_is_trusted_when_no_proxy_is_configured` + `ForwardedHeadersIntegrationTests.An_empty_trusted_proxy_list_leaves_the_peer_as_the_client` | `forwarded_headers.disabled` (Warning, any non-Development boot) |
| The framework's loopback defaults are left in place | Only configured entries are trusted | `ForwardedHeadersOptionsTests.The_framework_loopback_defaults_are_cleared` | — |
| More than one forwarded hop is read | Exactly one is read | `ForwardedHeadersOptionsTests.Only_one_hop_is_read_from_the_forwarded_chain` | — |
| A header beyond For/Proto is honoured | Only those two | `ForwardedHeadersOptionsTests.Only_the_for_and_proto_headers_are_honoured` | — |
| A CIDR entry is converted to the wrong type and matches nothing | It matches its members and not its neighbours | `ForwardedHeadersOptionsTests.A_cidr_entry_becomes_a_known_network_that_matches_its_members` + `ForwardedHeadersIntegrationTests.A_cidr_entry_trusts_every_proxy_in_the_range` | — |
| A client spoofs `X-Forwarded-For` from an untrusted peer | Ignored; the peer stays the client | `ForwardedHeadersIntegrationTests.An_untrusted_peer_cannot_name_the_client` | — |
| A client injects entries through the trusted proxy | Only the rightmost is honoured | `…Only_the_rightmost_entry_the_proxy_appended_is_honoured` | — |
| A trusted proxy reports TLS termination | The request reads as HTTPS, so the refresh cookie gets `Secure` | `…A_trusted_proxy_reporting_https_makes_the_request_secure` | — |
| A client claims HTTPS from an untrusted peer | Ignored | `…An_untrusted_peer_cannot_claim_https` | — |
| `X-Forwarded-For` claims an allow-listed scraper address | `/metrics` still 403 | `MetricsEndpointIntegrationTests.Forwarded_for_cannot_open_the_scrape_gate` | `metrics.scrape.denied ip=<true peer>` |
| Forwarded headers configured, real scraper allow-listed | `/metrics` still 200 | `…An_allow_listed_peer_still_scrapes_when_forwarded_headers_are_configured` | — |
| The scrape port is set but the request is ordinary traffic on the public listener — the shape a real deployment runs in | Forwarded headers still apply; the exclusion is narrow | `ForwardedHeadersWithObservabilityTests.A_request_on_the_public_listener_still_resolves_its_client` | — |
| A bad trusted-proxy entry reaches a real host rather than just the validator | Boot throws, naming the entry | `ForwardedHeadersIntegrationTests.An_unparseable_trusted_proxy_aborts_boot` | `OptionsValidationException` |
| The trusted proxy's address drifts, so the header is silently ignored for every request | A capped, deduplicated Warning names the peer | **Untested** — the middleware ships without a test; recorded as a gap below | `forwarded_headers.untrusted_peer ip=…` |
| An inline `Version=` returns to a csproj under CPM | Restore fails | Build gate — probed deliberately: `error NU1008` | MSBuild error |
| A requested package version does not exist | Restore fails, not warns | Build gate — probed deliberately: `error NU1603: Warning As Error` | MSBuild error |
| A package version that exists nowhere | Restore fails | Build gate — probed deliberately: `error NU1102` | MSBuild error |
| CPM moves a package in the API's shipped closure | Caught before merge | Assets diff, below | — |
| An OTel 1.15 package changes exporter registration | `/metrics` stops serving an exposition | Existing `MetricsEndpointIntegrationTests` (9 cases, incl. a business counter reaching the exposition) | — |
| Stripe.net pin diverges from what the webhook code compiles against | Webhook suite reddens | `PhotoPrint.Tests.Unit.Controllers` (59 cases) | — |

### Mutation checks — the two tests that matter most were proven to fail

"Green" proves nothing unless the test reddens when the behaviour is removed. Both load-bearing
behaviours were reverted deliberately and the suite re-run:

1. **Scrape-listener exclusion removed** (predicate replaced with "always"):
   `Forwarded_for_cannot_open_the_scrape_gate` fails with
   *"Expected response.StatusCode to be Forbidden {value: 403}, but found OK {value: 200}"* —
   i.e. a spoofed header returns the full metric store. This is the exposure ADR-018 warned
   about, and it is why story 004's third acceptance criterion is refused rather than met.
2. **Pipeline registration removed** (`app.UseTrustedProxyForwardedHeaders()` deleted):
   4 of the forwarded-header integration tests fail. The 3 that still pass are the ones
   asserting that nothing happens — correct, and a useful check that they are not vacuous.

Both mutations were reverted and the suite re-run green.

### Package-closure evidence

The API's resolved package list (139 entries) was captured before and after the CPM conversion.
The only difference is the intended security pin:

```
< System.Security.Cryptography.Xml 8.0.2
> System.Security.Cryptography.Xml 8.0.4
```

Nothing else in the shipped closure moved, which is what decision D7 required.

### Acceptance-criteria validation

| Criterion | Status |
|---|---|
| `dotnet list package --vulnerable` reports zero | ✅ direct and transitive, both projects |
| Restore succeeds, no `NU1603`, no `Version=` left in either csproj | ✅ probed both failure modes |
| Stripe.net resolves to exactly one version | ✅ 47.0.0 in both projects |
| The API's shipped closure is unchanged or every moved row explained | ✅ one row, explained |
| `Dockerfile` and `ci.yml` reconciled with the manifest's new home | ✅ (Docker build itself unverified — see gaps) |
| Renovate: three groups, two schedules, dashboard, labelled non-auto-merged security PRs | ✅ config present and valid JSON; behaviour unverified — see gaps |
| `UseForwardedHeaders` before `UseCorrelationId`; lists cleared; `ForwardLimit = 1` | ✅ |
| Spoofed `X-Forwarded-For` from an untrusted peer changes nothing | ✅ |
| `X-Forwarded-For` cannot open the `/metrics` gate; allow-listed peer still scrapes | ✅ and mutation-proven |
| Trusted proxies + observability on + `ScrapePort = 0` fails boot | ✅ |
| Refresh cookie carries `Secure` behind a TLS-terminating proxy | ⚠️ proven at its input (`Request.IsHttps`), not end-to-end — see gaps |
| Scoped suites green | ✅ |
| `DEPLOYMENT.md` §14.3 amended, §16 added; ADR-018 amended | ✅ |
| **Story 004 AC3** — an `X-Forwarded-For` case makes an allow-listed IP return 200 on `/metrics` | ❌ **deliberately not met.** Superseded by ADR-018's amendment; recorded in the story file, the ADR, `decision-index.md` and §14.3, and replaced by its inverse, which ships and is mutation-proven. Approved by the wave coordinator |

### What this suite cannot prove

0. **`UntrustedForwardedPeerMiddleware` has no test.** It was added late, in response to the
   stage-4 gate, and the session was stopped before a test for it was written. Its dedup cap and
   its "header present but address unchanged" condition are unproven. This is the one deliberate
   coverage gap in the bolt and should be closed before review.
1. **The Docker image build.** No Docker daemon is available here, so the `Dockerfile` change
   that copies the central manifest before restoring is verified by inspection only. It is the
   one change in this bolt that fails the *deploy* rather than the test suite if it is wrong.
   CI's image build on the PR is the real check.
2. **Renovate's actual behaviour.** The config is valid JSON with the required keys, but no
   Renovate run has happened — the GitHub App is not installed. Whether the groups and cron
   schedules do what they claim is unproven until the first dashboard issue appears.
3. **The refresh cookie's `Secure` flag end-to-end.** The tests assert `Request.IsHttps` is true
   behind a trusted proxy reporting TLS; the cookie line reads exactly that property, so the
   link is by inspection rather than by a login test.
4. **Real reverse-proxy behaviour.** Caddy's `header_up X-Forwarded-For {remote_host}` and the
   fixed container address are config, exercised by no test here. §16.6 gives the operator
   commands to verify both after a deploy.
5. **The `System.Security.Cryptography.Xml` pin under load.** The advisory is cleared per the
   scanner; the package reaches the API through the `Microsoft.AspNetCore.Identity` 2.x shim and
   is not exercised by any code path in this repo's tests.
6. **Any behavioural difference in the OTel 1.15 packages beyond `/metrics` still working.**
   The bump crosses four minor versions; the existing suite proves the exposition and a business
   counter, not span shape or exporter semantics.

### Issues found during implementation

1. **The OTel bump broke the build.** `EntityFrameworkInstrumentationOptions.SetDbStatementForText`
   no longer exists in 1.15.1-beta.1. The SQL text it used to opt into is emitted by default in
   the new package, so dropping the call preserves behaviour — but the option is gone as a lever,
   which matters to whoever eventually fixes the unscrubbed-SQL row in the backlog.
2. **`Stripe.net 46.3.0` does not exist on nuget.org.** Both projects requested it and both
   silently ran 47.0.0. The bolt's own note to "keep a rollback PR ready" for a 46→47 break is
   therefore void, and CPM plus `NU1603`-as-error is what stops the class recurring.
3. **CPM would have moved the API's shipped closure** if the central version had been taken from
   the test project, which pinned `Microsoft.Extensions.Configuration` 10.0.8 against the API's
   10.0.0. Resolved by pinning what the API resolves; the test project moved instead.
4. **`Program.cs`'s pipeline comments are now off by one.** They label the middleware `1st`…`5th`
   and forwarded headers now runs ahead of the one labelled `1st`. Left alone deliberately: this
   bolt is restricted to adding its own two lines and bolt 055 is restructuring that block.

### Correction: PPW-462 is not fixed by this bolt

The plan originally claimed this change fixes the security-audit log's client IP. The stage-4
gate disproved it: `AuthController.cs:54,72,160` compute the address and hand it to
`AuthService.cs:95,163` and `SocialAuthService.cs:36`, all three of which accept `ipAddress` and
**never reference it again** — there is no log call and no column. So there is no audit trail to
be wrong or right. This bolt makes the input correct; PPW-462 stays open until something records
it, and the runbook, the ADR and the backlog sweep were corrected to say so.

### Proposed findings (outside this bolt)

Recorded here at the coordinator's instruction rather than written to any `reviews/state/` file.

1. **Every `[EnableRateLimiting]` attribute in the codebase is inert — `/api/auth/login` has no
   brute-force limit.** `UseRateLimiter()` is registered inside `UseSecurityBaselines()`
   (`src/PhotoPrint.API/Extensions/SecurityExtensions.cs:122`, called from
   `src/PhotoPrint.API/Program.cs:378`) which runs **before** `app.UseRouting()`
   (`src/PhotoPrint.API/Program.cs:391`). `RateLimitingMiddleware` resolves its policy from
   `context.GetEndpoint()`, which is null before routing, so only the `GlobalLimiter` ever
   applies. The named policies on `AuthController` (register 5/hour, auth 10/min) and in
   `AuthExtensions` (resend-confirmation, forgot-password) never run. **Routed to bolt 055 by
   owner ruling 2026-09-04** — the Program.cs rewrite in Wave 2, not this wave and not 063. This
   also sharpens backlog row PPW-461, which currently says the limiters are *unpartitioned*; the
   sharper truth is that they never execute.
2. **The public rate-limit budget counts static assets, and has never been exercised per client.**
   `UseSecurityBaselines()` (`Program.cs:378`) runs before `UseStaticFiles()` (`:388`), so every
   SPA chunk and every gallery thumbnail spends a permit against
   `RateLimit:Public:PermitLimit = 100`/60s. Behind Caddy this has always been one shared bucket,
   so the per-client budget is untested in production. Documented as a required pre-enable step
   in `DEPLOYMENT.md` §16.3; the number itself belongs to intent 029 P08.
3. **Per-IP partitions key on the full IPv6 address.** A client with a routed `/64` can rotate
   source addresses and evade the limit, and each new address allocates a partition.
   `SecurityExtensions.cs:61` should truncate IPv6 keys to a prefix. Intent 029 P08.
4. **`Microsoft.AspNetCore.Identity` 2.3.1 is the legacy 2.x shim** and is the only reason
   `System.Security.Cryptography.Xml` is in the API's graph at all. Only `IPasswordHasher<T>` /
   `PasswordHasher<T>` are used, which live in `Microsoft.Extensions.Identity.Core`. Swapping it
   would remove the transitive pin — but moving from the 2.3.0 hasher to the 8.0.x one changes
   the PBKDF2 iteration-count default (10k → 100k), which is a change on the password path and
   outside this bolt's stories.
5. **xunit 2.5.3 drags in `NETStandard.Library` 1.6.1**, which is the origin of the two
   test-only advisories now silenced by pins (`System.Net.Http`, `System.Text.RegularExpressions`).
   The durable fix is a xunit upgrade; the pins are cosmetic for the audit and load nothing on
   `net8.0`.

### Notes

- The Renovate GitHub App install remains an open question for the repo owner: it is a one-time
  repo-admin action, not code, and until it happens `.github/renovate.json` does nothing.
- `dotnet list package --vulnerable` reads live advisory data from nuget.org, so "clean" is a
  statement about 2026-09-04, not a permanent property. That is precisely what story 003 exists
  to keep true.
